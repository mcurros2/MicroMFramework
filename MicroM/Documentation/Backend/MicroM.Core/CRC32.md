# Class: MicroM.Core.CRC32

## Overview
Implements the CRC-32/ISO-HDLC algorithm for computing checksums.

## Methods
| Method | Description |
|:------------|:-------------|
| CRC32FromByteArray(byte[] bytes) | Computes a CRC-32 checksum from the given bytes. |
| CRCFromString(string string_to_hash) | Computes a CRC-32 checksum from the given string. |

## Remarks
Used to generate deterministic hashes for data verification.

## See Also
- [CryptClass](CryptClass.md)
