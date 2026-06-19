namespace MicroM.Data;

[Flags]
public enum ColumnFlags : ushort
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
    APIReadOnly = 256,
    All = 512,
}
