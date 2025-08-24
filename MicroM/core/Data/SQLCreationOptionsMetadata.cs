namespace MicroM.Data;

[Flags]
public enum SQLCreationOptionsMetadata : byte
{
    None = 0,
    WithIUpdate = 1,
    WithIDrop = 2,
    WithIUpdateAndIDrop = 3
}
