# Class: MicroM.Extensions.ReflectionExtensions
## Overview
Reflection utilities for discovering types, members, and values.

**Inheritance**
object -> ReflectionExtensions

**Implements**
None

## Example Usage
```csharp
var members = typeof(MyEntity).GetPublicPropertiesOrFields();
```
## Methods
| Method | Description |
|:------------|:-------------|
| TryAddType<T>(Dictionary<string, Type> dict) | Adds a type to a dictionary if not already present. |
| TryAddType<T>(CustomOrderedDictionary<Type> dict) | Adds a type to a custom ordered dictionary if absent. |
| GetMemberValue(MemberInfo member, object instance) | Retrieves the value of a member from an instance. |
| GetMemberType(MemberInfo member) | Determines the type of a member. |
| GetEntitiesTypes(Assembly asm) | Gets entity types from an assembly. |
| GetCategoriesTypes(Assembly asm) | Gets category definition types from an assembly. |
| GetAllCategoriesTypes(List<Assembly> assemblies) | Gets category definition types from multiple assemblies. |
| GetStatusTypes(Assembly asm) | Gets status definition types from an assembly. |
| GetInterfaceTypes<T>(Assembly asm) | Retrieves types implementing an interface. |
| GetPublicPropertiesOrFields(Type obj_type) | Returns public properties or fields of a type. |
| GetPropertiesOrFields<TFilter, TObject>(TObject obj) where TFilter : class | Returns members matching the filter type from an object. |
| GetFirstPropertyInHierarchy(Type type, string property_name, BindingFlags binding_flags) | Finds a property in the type hierarchy. |
| GetMembersInDeclarationOrder(Type instanceType) | Returns members in source declaration order. |
| GetAndCacheInstanceMembers(Type instance_type) | Retrieves and caches instance members. |

## Remarks
None.

