# Namespace: MicroM.Core

The `MicroM.Core` namespace contains the most fundamental and foundational classes of the framework. These components provide the basic building blocks upon which all other layers and functionalities are built. They are abstract and not tied to any specific application logic.

---

## `CRC32`
A utility class for calculating CRC-32/ISO-HDLC checksums.

### `CRC32FromByteArray(byte[] bytes)`
**Returns:** `UInt32`  
Computes the CRC-32 checksum for a byte array.

### `CRCFromString(string string_to_hash)`
**Returns:** `UInt32`  
Computes the CRC-32 checksum for a UTF-8 encoded string.

---

## `ClassNotInitilizedException`
A custom exception thrown when a method is called on a class that has not yet been initialized via its `Init()` method.

### Constructors
*   `ClassNotInitilizedException()`
*   `ClassNotInitilizedException(string message)`

---

## `CryptClass`
A static utility class providing a wide range of cryptographic functions.

### Hashing and Keys
*   `GetSecurityKey(string password, string salt, int keySize = 256)`: Creates a `SymmetricSecurityKey` using the PBKDF2 algorithm.

### Symmetric Encryption
*   `TempEncryptString(string to_encrypt)`: Encrypts a string using a temporary, session-specific AES key and IV.
*   `EncryptText(string text, byte[] key, byte[] iv)`: Encrypts text using a provided AES key and IV.

### X.509 Certificate Management
*   `CreateSelfSignedCertificate(string distinguished_name, int expires_years)`: Creates a new self-signed RSA X.509 certificate.
*   `StoreCertificate(X509Certificate2 certificate, string certificate_password)`: Stores a certificate in the current user's personal certificate store (`StoreName.My`).
*   `CreateSelfSignedCertificateAndStoreInUser(string certificate_password, string distinguished_name, int expires_years)`: A helper that combines creation and storage.
*   `ExportCertificate(X509Certificate2 cert, string certificate_full_path, string export_password, CancellationToken ct)`: Exports a certificate to a `.pfx` file.
*   `DeleteCertificate(string subject_name)`: Deletes a certificate from the user store by its subject name.
*   `FindCertificateByName(string subject_name)`: Finds a certificate in the user store by its subject name.
*   `FindCertificate(string thumbprint)`: Finds a certificate in the user store by its thumbprint.

### X.509 Asymmetric Encryption
*   `X509Encrypt(string plainText, X509Certificate2 cert)`: Encrypts a string using the public key of an X.509 certificate.
*   `X509Decrypt(string encryptedText, X509Certificate2 cert)`: Decrypts a string using the private key of an X.509 certificate.

### Object Encryption (RSA+AES)
*   `EncryptObject<T>(T obj, X509Certificate2 cert)`: Serializes an object to JSON and encrypts it using a hybrid RSA+AESGCM scheme.
*   `DecryptObject<T>(string encryptedString, X509Certificate2 cert)`: Decrypts a string that was encrypted with `EncryptObject` and deserializes it back to an object.

### Random Data Generation
*   `GenerateRandomBase64String(int count = 8)`: Generates a cryptographically secure random Base64 string.
*   `CreateRandomPassword(int length, int minSymbols, int minNumbers, int minUppercase, int minLowercase)`: Creates a cryptographically secure random password that meets specified complexity requirements.

---

## `CustomOrderedDictionary<T>`
A dictionary that preserves the insertion order of its elements. It wraps `System.Collections.Specialized.OrderedDictionary` and provides a generic, strongly-typed interface.

### Properties
*   `Count`: Gets the number of items.
*   `this[string key]`: Gets the value associated with a specific key.
*   `this[int index]`: Gets the value at a specific index.
*   `Values`: Gets a collection of the values in the dictionary.
*   `Keys`: Gets a collection of the keys in the dictionary.

### Methods
*   `Add(string key, T value)`
*   `Remove(string key)`
*   `RemoveAt(int index)`
*   `Clear()`
*   `Contains(string key)`
*   `TryGetValue(string key, out T? value)`
*   `TryAdd(string key, T value)`

---

## `DefaultProcedureNames`
An `enum` that defines the standard suffixes for stored procedures generated and used by the framework.
*   `_update`
*   `_drop`
*   `_get`
*   `_lookup`
*   `_brwStandard`

---

## `EntityDefinition`
An abstract base class that holds the metadata for a data entity. It is the foundation of the Data Dictionary pattern. Developers create subclasses of this to define the structure of their database tables.

### Properties
*   `Mneo`: The mnemonic string prefix for the entity's database objects.
*   `Name`: The full name of the entity class.
*   `TableName`: The corresponding table name in the database.
*   `Fake`: A boolean indicating if the entity is "fake" (i.e., does not have a corresponding table but may have procedures).
*   `SQLCreationOptions`: Flags that control how the SQL objects for this entity are generated.
*   `Columns`: A readonly ordered dictionary of all `ColumnBase` objects defined for the entity.
*   `KeyColumnName`: The name of the column that is the most significant key, used in lookups.
*   `Views`: A readonly dictionary of `ViewDefinition` objects.
*   `Procs`: A readonly dictionary of `ProcedureDefinition` objects.
*   `ForeignKeys`: A readonly dictionary of `EntityForeignKeyBase` objects.
*   `Actions`: A readonly dictionary of `EntityActionBase` objects.
*   `UniqueConstraints`: A readonly dictionary of `EntityUniqueConstraint` objects.
*   `Indexes`: A readonly dictionary of `EntityIndex` objects.
*   `AutonumColumn`: Gets the column defined with the `Autonum` flag.
*   `dt_inserttime`, `dt_lu`, `vc_webinsuser`, etc.: Properties for the default audit columns.
*   `RelatedCategories`: A readonly set of related category IDs.
*   `RelatedStatus`: A readonly set of related status IDs.

### Methods
*   `GetForeignKey<T>(T parent_entity)`: Gets the foreign key definition that relates this entity to a given parent entity type.
*   `AddCategoryID(string category_id)`
*   `AddStatusID(string status_id)`

### Protected Methods
*   `DefineActions()`: Virtual method to be overridden to add `EntityActionBase` definitions.
*   `AddCategoriesRelations()`: Virtual method to add category relationships.
*   `AddStatusRelations()`: Virtual method to add status relationships.
*   `DefineViews()`: Virtual method to define `ViewDefinition`s.
*   `DefineProcs()`: Virtual method to define `ProcedureDefinition`s.
*   `DefineConstraints()`: Virtual method to define `EntityForeignKeyBase` and `EntityUniqueConstraint`s.

---

## `EntityBase`
An abstract base class that provides the functionality to interact with an entity's data.

### Properties
*   `Def`: Gets the `EntityDefinition` associated with this entity.
*   `Client`: Gets the `IEntityClient` used for database communication.
*   `Encryptor`: Gets the `IMicroMEncryption` service, if one is provided.
*   `Actions`: A dictionary of custom actions defined for the entity.

### Methods
*   `Init(IEntityClient? ec, IMicroMEncryption? encryptor)`: Initializes the entity with a database client, making it ready for data operations.
*   `ExecuteAction(...)`: Executes a named `EntityActionBase` associated with this entity.
*   `DeleteData(...)`, `ExecuteView(...)`, `GetData(...)`, `InsertData(...)`, `UpdateData(...)`, `LookupData(...)`, etc.: A suite of methods that provide a high-level API for performing standard CRUD operations. These methods call the underlying `IEntityData` implementation.

---

## `Entity<T>`
The generic base class that developers inherit from to create their interactive entity classes. It links a specific `EntityDefinition` (`TDefinition`) to the `EntityBase` functionality.

### Constructors
*   `Entity()`
*   `Entity(string table_name)`
*   `Entity(IEntityClient ec, IMicroMEncryption? encryptor)`

---

## `EntityActionBase`
An abstract base class for defining custom, reusable business logic actions that can be associated with an entity.

### Methods
*   `Execute(...)`: The abstract method to be implemented, which contains the logic for the action.

---

## `EntityActionArguments`
A `record` used to pass arguments to an `EntityActionBase`.

### Properties
*   `WebParms`: A `DataWebAPIRequest` object containing parameters from a web request.

---

## `EntityActionResult`
An abstract `record` that serves as the base for all possible return types from an `EntityActionBase`.

---

## `EmptyActionResult`
A concrete implementation of `EntityActionResult` that represents an action that returns no data.

---

## `EntityChecker`
A utility class used for validating the integrity and correctness of `EntityDefinition` classes, typically during development or testing.

### Methods
*   `CheckEntity(Type entity_type)`: Checks a single entity definition for consistency between its defined properties and its internal collections.
*   `GetProperties(Type entity)`: Uses reflection to get dictionaries of the Column, View, and Procedure properties defined on an entity.
*   `CheckEntities(Assembly? asm, string? assembly_name)`: Runs checks on all `EntityBase` types within a given assembly.

---

## `IInit`
An interface for components that require explicit, lazy initialization.

### Properties
*   `IsInitialized`: A boolean that indicates if `Init()` has been called.

### Methods
*   `Init()`: The method that performs the initialization logic.

---

## `InitBase`
An abstract base class that provides a basic implementation of the `IInit` pattern.

### Properties
*   `IsInitialized`: A protected-set boolean that tracks the initialization state.

### Methods
*   `CheckInit()`: A protected method that throws a `ClassNotInitilizedException` if the class is not initialized.

---

## `IReadonlyOrderedDictionary<T>`
An interface defining a readonly, ordered dictionary.

---

## `X509Encryptor`
A class that implements the `IMicroMEncryption` interface using an X.509 certificate for cryptographic operations.

### Constructors
*   `X509Encryptor(string? certificate_thumbprint, string? certificate_name)`

### Properties
*   `Certificate`: The underlying `X509Certificate2` object.
*   `CertificateThumbprint`: The thumbprint of the certificate.

### Methods
*   `Encrypt(string plaintext)`
*   `Decrypt(string base64_encrypted)`
*   `EncryptObject<T>(T obj)`
*   `DecryptObject<T>(string encryptedString)`
*   `Dispose()`
