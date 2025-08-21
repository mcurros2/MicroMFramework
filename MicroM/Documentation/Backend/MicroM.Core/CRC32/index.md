# Class: MicroM.Core.CRC32
## Overview
Utilities for computing CRC-32 checksums.

**Inheritance**
object -> CRC32

**Implements**
None

## Example Usage
```csharp
var checksum = MicroM.Core.CRC32.CRCFromString("data");
```
## Methods
| Method | Description |
|:------------|:-------------|
| CRC32FromByteArray(byte[] bytes) | Calculates a CRC-32 checksum from the provided byte array. |
| CRCFromString(string string_to_hash) | Calculates a CRC-32 checksum from the provided string. |

## Remarks
None.

## See Also
-
