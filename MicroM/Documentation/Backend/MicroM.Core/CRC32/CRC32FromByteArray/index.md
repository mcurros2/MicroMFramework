# MicroM.Core.CRC32.CRC32FromByteArray
Calculates a CRC-32 checksum for a byte array.

### Syntax
```csharp
public static UInt32 CRC32FromByteArray(byte[] bytes)
```

## Parameters
| Parameter | Type | Description |
|:------------|:-------------|:-------------|
| bytes | [byte[]](https://learn.microsoft.com/dotnet/api/system.byte) | The bytes to compute the checksum for. |

## Returns
[UInt32](https://learn.microsoft.com/dotnet/api/system.uint32) - The computed CRC-32 checksum.

## Exceptions
None.

## Example Usage
```csharp
byte[] data = Encoding.UTF8.GetBytes("data");
UInt32 checksum = MicroM.Core.CRC32.CRC32FromByteArray(data);
```

## Remarks
Iterates over each byte and updates the checksum using a precomputed table.

## See Also
- [CRC32](../index.md)
- [CRCFromString](../CRCFromString/index.md)
