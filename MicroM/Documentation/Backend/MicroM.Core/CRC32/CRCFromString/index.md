# MicroM.Core.CRC32.CRCFromString
Calculates a CRC-32 checksum for a UTF-8 encoded string.

### Syntax
```csharp
public static UInt32 CRCFromString(string string_to_hash)
```

## Parameters
| Parameter | Type | Description |
|:------------|:-------------|:-------------|
| string_to_hash | [string](https://learn.microsoft.com/dotnet/api/system.string) | The string to compute the checksum for. |

## Returns
[UInt32](https://learn.microsoft.com/dotnet/api/system.uint32) - The computed CRC-32 checksum.

## Exceptions
None.

## Example Usage
```csharp
UInt32 checksum = MicroM.Core.CRC32.CRCFromString("data");
```

## Remarks
Converts the input string to UTF-8 bytes and delegates to [CRC32FromByteArray](../CRC32FromByteArray/index.md).

## See Also
- [CRC32](../index.md)
- [CRC32FromByteArray](../CRC32FromByteArray/index.md)
