# Class: MicroM.Core.EntityActionBase

## Overview
Abstract base class for defining actions that can be executed by an entity.

## Methods
| Method | Description |
|:------------|:-------------|
| Execute(EntityBase entity, DataWebAPIRequest parms, EntityDefinition def, MicroMOptions? Options, IWebAPIServices? API, IMicroMEncryption? encryptor, CancellationToken ct, string? app_id) | Executes the action and returns an `EntityActionResult`. |

## Remarks
Custom actions inherit from this class and implement the `Execute` method.

## See Also
- [EntityActionResult](EntityActionResult.md)
- [EntityBase](EntityBase.md)
