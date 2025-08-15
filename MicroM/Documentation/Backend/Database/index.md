# Database

This document describes backend classes responsible for database creation, migration, and permission management. It also covers how these components interact with DataDictionary entities.

## ApplicationDatabase

`ApplicationDatabase` coordinates creation and initialization of application-specific databases. It loads schema providers from configured assemblies, runs migrations, creates schema and procedures when no migration is required, grants permissions, clears routing tables, and rebuilds menus【F:core/Database/ApplicationDatabase.cs†L16-L67】. Status helper methods check server connectivity, admin rights, and existence of the database and login【F:core/Database/ApplicationDatabase.cs†L95-L105】.

## DatabaseManagement

`DatabaseManagement` contains low-level administrative helpers. It checks rights and existence of server objects【F:core/Database/DatabaseManagement.cs†L7-L24】, creates and drops databases and logins【F:core/Database/DatabaseManagement.cs†L27-L75】, and verifies table presence【F:core/Database/DatabaseManagement.cs†L93-L97】.

## DatabaseSchemaCreationOptions

`DatabaseSchemaCreationOptions` wraps an entity instance and options indicating whether to create or alter existing structures, exposing the entity type and mnemonic used in script generation【F:core/Database/DatabaseSchemaCreationOptions.cs†L5-L10】.

## DatabaseSchemaTables

`DatabaseSchemaTables` builds physical structures for entity models. It discovers missing tables and creates them【F:core/Database/DatabaseSchemaTables.cs†L11-L69】, executes custom SQL types, tables, and views from classified scripts【F:core/Database/DatabaseSchemaTables.cs†L71-L142】, manages indexes【F:core/Database/DatabaseSchemaTables.cs†L144-L198】, and handles dropping and recreating constraints and indexes as needed【F:core/Database/DatabaseSchemaTables.cs†L200-L265】.

## DatabaseSchemaProcedures

`DatabaseSchemaProcedures` generates or executes stored procedures for entities. It runs custom SQL scripts when provided【F:core/Database/DatabaseSchemaProcedures.cs†L13-L33】, builds standard CRUD and lookup procedures while avoiding duplicates of custom scripts【F:core/Database/DatabaseSchemaProcedures.cs†L41-L109】, and orchestrates combining generated and custom procedures when creating full sets【F:core/Database/DatabaseSchemaProcedures.cs†L120-L200】.

## CustomScript and Custom SQL

`CustomScript` represents metadata for extracted SQL scripts, including procedure type and standard role. Classified scripts allow distinguishing procedures, types, sequences, views, and other SQL objects for tailored creation steps【F:core/Database/CustomScript.cs†L1-L21】【F:core/Database/DatabaseSchemaCustomScripts.cs†L8-L44】.

## DatabaseSchemaPermissions

`DatabaseSchemaPermissions` creates route metadata for entities and grants execution permissions for all procedures to a specified login or group【F:core/Database/DatabaseSchemaPermissions.cs†L11-L44】.

## Interaction with DataDictionary

`DataDictionarySchema` uses the above components to provision core framework tables and scripts. It enumerates built‑in entity types such as Objects, Categories, Statuses, FileStore, and MicromUsers【F:core/Database/DataDictionarySchema.cs†L51-L89】, creates required tables, constraints, views, and procedures, and registers these entities in the DataDictionary【F:core/Database/DataDictionarySchema.cs†L92-L134】. Entities can also be individually added to the dictionary after table creation【F:core/Database/DataDictionarySchema.cs†L34-L44】.
