# Backend Extension Helpers

MicroMCore exposes a rich set of extension methods that simplify backend development.  Extensions are organized by domain so you can quickly locate helpers for your task.

## Strings

Utility methods to manipulate string values.

```csharp
using MicroM.Extensions;

var name = "The MicroM Framework".Truncate(12);      // "The MicroM Fr"
var clean = "\"quoted\"".Unquote();                 // "quoted"
var pieces = new[]{ " one ", " two "}.Trim();       // ["one", "two"]
var fallback = maybeNull.IfNullOrEmpty("default");
var required = maybeNull.ThrowIfNullOrEmpty(nameof(maybeNull));
```

## Data Dictionary

Helpers for populating and relating entries in the MicroM data dictionary.

```csharp
using MicroM.Extensions;
using MicroM.DataDictionary.Configuration;

// Add a category definition and its values
var cat = new CategoryDefinition { CategoryID = "ROLE", Description = "User Role" };
await cat.AddCategory(ec, ct);

// Register an entity and its relations in the dictionary
var person = new PersonEntity(ec);
await person.AddToDataDictionary(ct);

// Menus and user groups
await menuDefinition.AddMenu(ec, ct);
await groupDefinition.AddUserGroup(ec, ct);
```

## Database Schema

Create database objects declared in your assemblies.

```csharp
using MicroM.Extensions;
using System.Reflection;

var asm = Assembly.GetExecutingAssembly();
await asm.CreateAllCategories(ec, ct);
await asm.CreateAllStatus(ec, ct);
await asm.CreateAssemblyCustomProcs(ec, ct);
```

## Reflection

Introspect assemblies and types to drive dynamic behaviour.

```csharp
using MicroM.Extensions;
using System.Reflection;

var entityTypes = Assembly.GetExecutingAssembly().GetEntitiesTypes();
var orderedMembers = typeof(PersonEntity).GetMembersInDeclarationOrder();
var props = person.GetPropertiesOrFields<StatusValuesDefinition, StatusDefinition>();
```

These extensions provide concise, expressive ways to work with common tasks throughout the MicroM backend.
