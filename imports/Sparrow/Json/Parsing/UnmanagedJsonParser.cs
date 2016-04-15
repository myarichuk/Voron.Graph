﻿using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Sparrow.Json.Parsing
{
    public unsafe class UnmanagedJsonParser : IJsonParser
    {
        private static readonly byte[] NaN = { (byte)'N', (byte)'a', (byte)'N' };
        public static readonly byte[] Utf8Preamble = Encoding.UTF8.GetPreamble();

        private readonly UnmanagedWriteBuffer _stringBuffer;
        private string _doubleStringBuffer;
        private int _currentStrStart;
        private readonly JsonParserState _state;
        private int _pos;
        private int _bufSize;
        private int _line;
        private int _charPos = 1;

        private byte[] _inputBuffer;
        private int _prevEscapePosition;
        private byte _currentQuote;

        private byte[] _expectedTokenBuffer;
        private int _expectedTokenBufferPosition;
        private string _expectedTokenString;
        private bool _zeroPrefix;
        private bool _isNegative;
        private bool _isDouble;
        private bool _isExponent;
        private bool _escapeMode;
        private int _initialPos;

        public UnmanagedJsonParser(JsonOperationContext ctx, JsonParserState state, string debugTag)
        {
            _state = state;
            _stringBuffer = ctx.GetStream(debugTag);
        }

        public void SetBuffer(byte[] inputBuffer, int size)
        {
            _inputBuffer = inputBuffer;
            _bufSize = size;
            _pos = 0;
            _initialPos = 0;
        }

        public void SetBuffer(ArraySegment<byte> segment)
        {
            _inputBuffer = segment.Array;
            _bufSize = segment.Count;
            _initialPos = segment.Offset;
            _pos = segment.Offset;
        }


        public bool Read()
        {
            if (_state.Continuation != JsonParserTokenContinuation.None) // parse normally
            {
                bool read;
                if (ContinueParsingValue(out read))
                    return read;
            }
            
            _state.Continuation = JsonParserTokenContinuation.None;
            if (_line == 0)
            {
                // first time, need to check preamble
                _line++;
                if (_pos >= _initialPos + _bufSize)
                {
                    return false;
                }
                if (_inputBuffer[_pos] == Utf8Preamble[0])
                {
                    _pos++;
                    _expectedTokenBuffer = Utf8Preamble;
                    _expectedTokenBufferPosition = 1;
                    _expectedTokenString = "UTF8 Preamble";
                    if (EnsureRestOfToken() == false)
                    {
                        _state.Continuation = JsonParserTokenContinuation.PartialPreamble;
                        return false;
                    }
                }
            }

            while (true)
            {
                if (_pos >= _initialPos + _bufSize)
                    return false;

                var b = _inputBuffer[_pos++];
                _charPos++;
                switch (b)
                {
                    case (byte)'\r':
                        if (_pos >= _bufSize)
                            return false;
                        if (_inputBuffer[_pos] == (byte)'\n')
                            continue;
                        goto case (byte)'\n';
                    case (byte)'\n':
                        _line++;
                        _charPos = 1;
                        break;
                    case (byte)' ':
                    case (byte)'\t':
                    case (byte)'\v':
                    case (byte)'\f':
                        //white space, we can safely ignore
                        break;
                    case (byte)':':
                    case (byte)',':
                        switch (_state.CurrentTokenType)
                        {
                            case JsonParserToken.Separator:
                            case JsonParserToken.StartObject:
                            case JsonParserToken.StartArray:
                                throw CreateException("Cannot have a '" + (char)b + "' in this position");
                        }
                        _state.CurrentTokenType = JsonParserToken.Separator;
                        break;
                    case (byte)'N':
                        _state.CurrentTokenType = JsonParserToken.Float;
                        _expectedTokenBuffer = NaN;
                        _expectedTokenBufferPosition = 1;
                        _expectedTokenString = "NaN";
                        if (EnsureRestOfToken() == false)
                        {
                            _state.Continuation = JsonParserTokenContinuation.PartialNaN;
                            return false;
                        }

                        return true;
                    case (byte)'n':
                        _state.CurrentTokenType = JsonParserToken.Null;
                        _expectedTokenBuffer = BlittableJsonTextWriter.NullBuffer;
                        _expectedTokenBufferPosition = 1;
                        _expectedTokenString = "null";
                        if (EnsureRestOfToken() == false)
                        {
                            _state.Continuation = JsonParserTokenContinuation.PartialNull;
                            return false;
                        }
                        return true;
                    case (byte)'t':
                        _state.CurrentTokenType = JsonParserToken.True;
                        _expectedTokenBuffer = BlittableJsonTextWriter.TrueBuffer;
                        _expectedTokenBufferPosition = 1;
                        _expectedTokenString = "true";
                        if (EnsureRestOfToken() == false)
                        {
                            _state.Continuation = JsonParserTokenContinuation.PartialTrue;
                            return false;
                        }
                        return true;
                    case (byte)'f':
                        _state.CurrentTokenType = JsonParserToken.False;
                        _expectedTokenBuffer = BlittableJsonTextWriter.FalseBuffer;
                        _expectedTokenBufferPosition = 1;
                        _expectedTokenString = "false";
                        if (EnsureRestOfToken() == false)
                        {
                            _state.Continuation = JsonParserTokenContinuation.PartialFalse;
                            return false;
                        }
                        return true;
                    case (byte)'"':
                    case (byte)'\'':
                        _state.EscapePositions.Clear();
                        _stringBuffer.Clear();
                        _prevEscapePosition = 0;
                        _currentQuote = b;
                        _state.CurrentTokenType = JsonParserToken.String;
                        if (ParseString() == false)
                        {
                            _state.Continuation = JsonParserTokenContinuation.PartialString;
                            return false;
                        }
                        _stringBuffer.EnsureSingleChunk(_state);
                        return true;
                    case (byte)'{':
                        _state.CurrentTokenType = JsonParserToken.StartObject;
                        return true;
                    case (byte)'[':
                        _state.CurrentTokenType = JsonParserToken.StartArray;
                        return true;
                    case (byte)'}':
                        _state.CurrentTokenType = JsonParserToken.EndObject;
                        return true;
                    case (byte)']':
                        _state.CurrentTokenType = JsonParserToken.EndArray;
                        return true;
                    //numbers

                    case (byte)'0':
                    case (byte)'1':
                    case (byte)'2':
                    case (byte)'3':
                    case (byte)'4':
                    case (byte)'5':
                    case (byte)'6':
                    case (byte)'7':
                    case (byte)'8':
                    case (byte)'9':
                    case (byte)'-': // negative number

                        _stringBuffer.Clear();
                        _state.EscapePositions.Clear();
                        _state.Long = 0;
                        _zeroPrefix = b == '0';
                        _isNegative = false;
                        _isDouble = false;
                        _isExponent = false;

                        // ParseNumber need to call _charPos++ & _pos++, so we'll reset them for the first char
                        _pos--;
                        _charPos--;

                        if (ParseNumber() == false)
                        {
                            _state.Continuation = JsonParserTokenContinuation.PartialNumber;
                            return false;
                        }
                        if (_state.CurrentTokenType == JsonParserToken.Float)
                            _stringBuffer.EnsureSingleChunk(_state);
                        return true;
                }
            }
        }

        private bool ContinueParsingValue(out bool read)
        {
            read = false;
            switch (_state.Continuation)
            {
                case JsonParserTokenContinuation.PartialNaN:
                {
                    if (EnsureRestOfToken() == false)
                        return true;

                    _state.Continuation = JsonParserTokenContinuation.None;
                    _state.CurrentTokenType = JsonParserToken.Float;
                    _stringBuffer.EnsureSingleChunk(_state);

                    read = true;
                    return true;
                }
                case JsonParserTokenContinuation.PartialNumber:
                {
                    if (ParseNumber() == false)
                        return true;

                    if (_state.CurrentTokenType == JsonParserToken.Float)
                        _stringBuffer.EnsureSingleChunk(_state);

                    _state.Continuation = JsonParserTokenContinuation.None;

                    read = true;
                    return true;

                }
                case JsonParserTokenContinuation.PartialPreamble:
                {
                    if (EnsureRestOfToken() == false)
                        return true;

                    _state.Continuation = JsonParserTokenContinuation.None;

                    break; // single case where we don't return 
                }
                case JsonParserTokenContinuation.PartialString:
                {
                    if (ParseString() == false)
                        return true;

                    _stringBuffer.EnsureSingleChunk(_state);
                    _state.CurrentTokenType = JsonParserToken.String;
                    _state.Continuation = JsonParserTokenContinuation.None;
  
                    read = true;
                    return true;

                }
                case JsonParserTokenContinuation.PartialFalse:
                {
                    if (EnsureRestOfToken() == false)
                        return true;

                    _state.CurrentTokenType = JsonParserToken.False;
                    _state.Continuation = JsonParserTokenContinuation.None;

                    read = true;
                    return true;

                }
                case JsonParserTokenContinuation.PartialTrue:
                {
                    if (EnsureRestOfToken() == false)
                        return true;

                    _state.CurrentTokenType = JsonParserToken.True;
                    _state.Continuation = JsonParserTokenContinuation.None;

                    read = true;
                    return true;
                }
                case JsonParserTokenContinuation.PartialNull:
                {
                    if (EnsureRestOfToken() == false)
                        return true;

                    _state.CurrentTokenType = JsonParserToken.Null;
                    _state.Continuation = JsonParserTokenContinuation.None;

                    read = true;
                    return true;
                }
                default:
                    throw CreateException("Somehow got continuation for single byte token " + _state.Continuation);
            }

            return false;
        }


        private bool ParseNumber()
        {
            while (true)
            {
                if (_pos >= _bufSize)
                    return false;
                _charPos++;
                var b = _inputBuffer[_pos++];

                switch (b)
                {
                    case (byte)'.':
                        if (_isDouble)
                            throw CreateException("Already got '.' in this number value");
                        _zeroPrefix = false; // 0.5, frex
                        _isDouble = true;
                        break;
                    case (byte)'+':
                        break; // just record, appears in 1.4e+3
                    case (byte)'e':
                    case (byte)'E':
                        if (_isExponent)
                            throw CreateException("Already got 'e' in this number value");
                        _isExponent = true;
                        _isDouble = true;
                        break;
                    case (byte)'-':
                        if (_isNegative)
                            throw CreateException("Already got '-' in this number value");
                        _isNegative = true;
                        break;
                    case (byte)'0':
                    case (byte)'1':
                    case (byte)'2':
                    case (byte)'3':
                    case (byte)'4':
                    case (byte)'5':
                    case (byte)'6':
                    case (byte)'7':
                    case (byte)'8':
                    case (byte)'9':
                        _state.Long *= 10;
                        _state.Long += b - (byte)'0';
                        break;
                    default:
                        switch (b)
                        {
                            case (byte)'\r':
                            case (byte)'\n':
                                _line++;
                                _charPos = 1;
                                goto case (byte)' ';
                            case (byte)' ':
                            case (byte)'\t':
                            case (byte)'\v':
                            case (byte)'\f':
                            case (byte)',':
                            case (byte)']':
                            case (byte)'}':
                                if (_zeroPrefix && _stringBuffer.SizeInBytes != 1)
                                    throw CreateException("Invalid number with zero prefix");
                                if (_isNegative)
                                    _state.Long *= -1;
                                _state.CurrentTokenType = _isDouble ? JsonParserToken.Float : JsonParserToken.Integer;
                                _pos--; _charPos--;// need to re-read this char
                                return true;
                            default:
                                throw CreateException("Number cannot end with char with: '" + (char)b + "' (" + b + ")");
                        }
                }
                _stringBuffer.WriteByte(b);

            }
        }

        public bool EnsureRestOfToken()
        {
            for (int i = _expectedTokenBufferPosition; i < _expectedTokenBuffer.Length; i++)
            {
                if (_pos >= _bufSize)
                    return false;
                if (_inputBuffer[_pos++] != _expectedTokenBuffer[i])
                    throw CreateException("Invalid token found, expected: " + _expectedTokenString);
                _expectedTokenBufferPosition++;
                _charPos++;
            }
            return true;
        }

        private bool ParseString()
        {
            fixed (byte* inputBufferPtr = _inputBuffer)
            {
                while (true)
                {
                    _currentStrStart = _pos;
                    while (_pos < _bufSize)
                    {
                        var b = inputBufferPtr[_pos++];
                        _charPos++;
                        if (_escapeMode == false)
                        {
                            if (b == _currentQuote)
                            {
                                _stringBuffer.Write(inputBufferPtr + _currentStrStart, _pos - _currentStrStart - 1
                                    /*don't include the last quote*/);
                                return true;
                            }
                            if (b == (byte)'\\')
                            {
                                _escapeMode = true;
                                _stringBuffer.Write(inputBufferPtr + _currentStrStart, _pos - _currentStrStart - 1
                                    /*don't include the escape */);
                                _currentStrStart = _pos;
                            }
                        }
                        else
                        {
                            _currentStrStart++;
                            _escapeMode = false;
                            _charPos++;
                            if (b != (byte)'u')
                            {
                                _state.EscapePositions.Add(_stringBuffer.SizeInBytes - _prevEscapePosition);
                                _prevEscapePosition = _stringBuffer.SizeInBytes + 1;
                            }

                            switch (b)
                            {
                                case (byte)'r':
                                    _stringBuffer.WriteByte((byte)'\r');
                                    break;
                                case (byte)'n':
                                    _stringBuffer.WriteByte((byte)'\n');
                                    break;
                                case (byte)'b':
                                    _stringBuffer.WriteByte((byte)'\b');
                                    break;
                                case (byte)'f':
                                    _stringBuffer.WriteByte((byte)'\f');
                                    break;
                                case (byte)'t':
                                    _stringBuffer.WriteByte((byte)'\t');
                                    break;
                                case (byte)'"':
                                case (byte)'\\':
                                case (byte)'/':
                                    _stringBuffer.WriteByte(b);
                                    break;
                                case (byte)'\r':// line continuation, skip
                                                // flush the buffer, but skip the \,\r chars
                                    if (_pos >= _bufSize)
                                        return false;

                                    _line++;
                                    _charPos = 1;
                                    if (_pos >= _bufSize)
                                        return false;

                                    if (inputBufferPtr[_pos] == (byte)'\n')
                                        _pos++; // consume the \,\r,\n
                                    break;
                                case (byte)'\n':
                                    _line++;
                                    _charPos = 1;
                                    break;// line continuation, skip
                                case (byte)'u':// unicode value
                                    if (ParseUnicodeValue() == false)
                                        return false;

                                    break;
                                default:
                                    throw new InvalidOperationException("Invalid escape char, numeric value is " + b);
                            }
                        }
                    }
                    // copy the buffer to the native code, then refill
                    _stringBuffer.Write(inputBufferPtr + _currentStrStart, _pos - _currentStrStart);
                    if (_pos >= _bufSize)
                        return false;
                }
            }
        }


        private bool ParseUnicodeValue()
        {
            byte b;
            int val = 0;
            for (int i = 0; i < 4; i++)
            {
                if (_pos >= _bufSize)
                    return false;

                b = _inputBuffer[_pos++];
                _currentStrStart++;
                if (b >= (byte)'0' && b <= (byte)'9')
                {
                    val = (val << 4) | (b - (byte)'0');
                }
                else if (b >= 'a' && b <= (byte)'f')
                {
                    val = (val << 4) | (10 + (b - (byte)'a'));
                }
                else if (b >= 'A' && b <= (byte)'F')
                {
                    val = (val << 4) | (10 + (b - (byte)'A'));
                }
                else
                {
                    throw CreateException("Invalid hex value , numeric value is: " + b);
                }
            }
            WriteUnicodeCharacterToStringBuffer(val);
            return true;
        }

        private void WriteUnicodeCharacterToStringBuffer(int val)
        {
            var smallBuffer = stackalloc byte[8];
            var chars = stackalloc char[1];
            try
            {
                chars[0] = Convert.ToChar(val);
            }
            catch (Exception e)
            {
                throw new FormatException("Could not convert value " + val + " to char", e);
            }
            var byteCount = Encoding.UTF8.GetBytes(chars, 1, smallBuffer, 8);
            _stringBuffer.Write(smallBuffer, byteCount);
        }


        public void ValidateFloat()
        {
            if (_doubleStringBuffer == null)
                _doubleStringBuffer = new string(' ', 25);
            if (_stringBuffer.SizeInBytes > 25)
                throw CreateException("Too many characters in double: " + _stringBuffer.SizeInBytes);

            var tmpBuff = stackalloc byte[_stringBuffer.SizeInBytes];
            // here we assume a clear char <- -> byte conversion, we only support
            // utf8, and those cleanly transfer
            fixed (char* pChars = _doubleStringBuffer)
            {
                int i = 0;
                _stringBuffer.CopyTo(tmpBuff);
                for (; i < _stringBuffer.SizeInBytes; i++)
                {
                    pChars[i] = (char)tmpBuff[i];
                }
                for (; i < _doubleStringBuffer.Length; i++)
                {
                    pChars[i] = ' ';
                }
            }
            try
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                double.Parse(_doubleStringBuffer, NumberStyles.Any, CultureInfo.InvariantCulture);
            }
            catch (Exception e)
            {
                throw CreateException("Could not parse double", e);
            }
        }


        protected InvalidDataException CreateException(string message, Exception inner = null)
        {
            var start = Math.Max(0, _pos - 25);
            var count = Math.Min(_pos, _bufSize) - start;
            var s = Encoding.UTF8.GetString(_inputBuffer, start, count);
            return new InvalidDataException(message + " at (" + _line + "," + _charPos + ") around: " + s, inner);
        }

        public void Dispose()
        {
            _stringBuffer?.Dispose();
        }

    }
}