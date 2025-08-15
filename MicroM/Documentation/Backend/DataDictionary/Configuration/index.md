# Configuration Namespace

Classes in `MicroM.DataDictionary.Configuration` provide reusable building blocks used by entities and categories.

## CategoryDefinition
Base type for defining categories. Each specific category derives from this class and exposes `CategoryValuesDefinition` instances.

## CategoryValuesDefinition
Represents a single value inside a category.

## StatusDefinition
Abstract base class for status enumerations.

## StatusValuesDefinition
Defines a specific status value.

## IDDescriptionDefinition
Helper structure containing an identifier and descriptive text.

## MenuDefinition & MenuItemDefinition
Describe application menus and the routes each menu item allows.

## UsersGroupDefinition
Describes a security group and its allowed routes.

## TableSuffix
Static helper that exposes common table name suffixes.
