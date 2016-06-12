﻿using System.Globalization;

namespace Sparrow.Json
{
    public class LazyDoubleValue
    {
        public readonly LazyStringValue Inner;
        private double? _val;

        public LazyDoubleValue(LazyStringValue inner)
        {
            Inner = inner;
        }

        public static implicit operator double(LazyDoubleValue self)
        {
            if (self._val != null)
                return self._val.Value;

            var val = double.Parse(self.Inner, NumberStyles.Any, CultureInfo.InvariantCulture);
            self._val = val;
            return val;
        }
    }
}