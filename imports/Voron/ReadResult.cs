namespace Voron
{
    public class ReadResult
    {
        public ReadResult(ValueReader reader, ushort version)
        {
            Reader = reader;
            Version = version;
        }
         
        public ValueReader Reader { get; private set; }

        public ushort Version { get; private set; }
    }
}
