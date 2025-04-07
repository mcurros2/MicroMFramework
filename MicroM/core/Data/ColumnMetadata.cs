namespace MicroM.Data
{
    [Flags]
    public enum ColumnFlags : byte
    {
        None = 0,
        Get = 1,
        Insert = 2,
        Update = 4,
        Delete = 8,
        PK = 16,
        FK = 32,
        Autonum = 64,
        Fake = 128,
        All = 255
    }

}
