# Class: MicroM.DataDictionary.Entities.MicromUsers.MicromUsers

## Overview
Entity handling user accounts, password hashes, login attempts, and recovery operations.

## Constructors
| Constructor | Description |
|:--|:--|
| MicromUsers() | Initializes the entity. |
| MicromUsers(IEntityClient ec, IMicroMEncryption? encryptor = null) | Uses a client and optional encryptor. |

## Remarks
Exposes procedures for authentication, password management, and claim retrieval.
