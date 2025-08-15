# Core Backend Classes

This section introduces the primary classes that make up the MicroM backend core. These types provide the foundation for entity modelling, initialization and utilities such as encryption and hashing.

## Entity

`Entity<TDefinition>` is a generic base for concrete domain models. It instantiates the associated `EntityDefinition` and optionally receives an `IEntityClient` and `IMicroMEncryption` to access data stores. Instances expose the definition through the `Def` property and rely on `EntityBase` for data operations.

### Helper Classes
- **EntityData** – internal helper that performs CRUD operations and procedure calls on behalf of an entity.
- **EntityActionResult** – base record returned by custom actions.

## EntityBase

`EntityBase` supplies lazy initialization and database access for entities. It holds the `IEntityClient`, `IMicroMEncryption` and exposes methods such as `GetData`, `InsertData`, `UpdateData` and `ExecuteProc` for interacting with the underlying database.

### Helper Classes
- **ClassNotInitilizedException** – thrown when methods are invoked before `Init` has been called.
- **EntityData** – shared data access helper used by `EntityBase`.

## EntityActionBase

`EntityActionBase` represents an executable action tied to an entity definition. Implementations override `Execute` to receive the current entity, request parameters and optional services and return an `EntityActionResult`.

### Helper Classes
- **EntityActionResult** – abstract record used as the return type for actions. `EmptyActionResult` provides a default no-op result.

## EntityDefinition

`EntityDefinition` describes the structure of an entity including its mnemonic (`Mneo`), table name, columns, views, stored procedures and relationships. It also manages default metadata like audit columns and related categories or status codes.

### Helper Classes
- **ColumnBase** and related column types – capture column metadata and flags.
- **EntityForeignKeyBase**, **EntityUniqueConstraint** and **EntityIndex** – model relationships, constraints and indexes.
- **ViewDefinition** and **ProcedureDefinition** – describe stored procedures used for reads and writes.

## InitBase

`InitBase` provides a simple pattern for lazy initialization. It exposes an `IsInitialized` property and a `CheckInit` method used by derived classes to ensure required setup is complete.

### Helper Classes
- **ClassNotInitilizedException** – signals usage of an object prior to initialization.
- **IInit** – interface variant for classes that implement the same pattern.

## X509Encryptor

`X509Encryptor` implements `IMicroMEncryption` using an `X509Certificate2`. It can encrypt and decrypt strings or arbitrary objects and exposes the certificate thumbprint when available.

### Helper Classes
- **CryptClass** – static utility with methods for finding certificates and performing lower level cryptographic operations.

## CRC32

`CRC32` is a utility class implementing the CRC-32/ISO-HDLC algorithm. It offers static methods to compute checksums from byte arrays or strings.

### Helper Classes
- None.

