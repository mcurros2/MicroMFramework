namespace MicroM.Data;

/// <summary>
/// Options that control inclusion of metadata when creating SQL objects.
/// </summary>
[Flags]
public enum SQLCreationOptionsMetadata : byte
{
    /// <summary>
    /// No metadata options specified.
    /// </summary>
    None = 0,
    /// <summary>
    /// Include metadata for the update interface.
    /// </summary>
    WithIUpdate = 1,
    /// <summary>
    /// Include metadata for the drop interface.
    /// </summary>
    WithIDrop = 2,
    /// <summary>
    /// Include metadata for both update and drop interfaces.
    /// </summary>
    WithIUpdateAndIDrop = 3
}
