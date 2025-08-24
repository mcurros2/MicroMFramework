using MicroM.Core;
using MicroM.DataDictionary.Configuration;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.Extensions;

public static class ReflectionExtensions
{

    public static void TryAddType<T>(this Dictionary<string, Type> dict)
    {
        var entity_type = typeof(T);
        dict.TryAdd(entity_type.Name, entity_type);
    }

    public static void TryAddType<T>(this CustomOrderedDictionary<Type> dict)
    {
        var entity_type = typeof(T);
        dict.TryAdd(entity_type.Name, entity_type);
    }

    public static object? GetMemberValue(this MemberInfo member, object instance)
    {
        if (member.MemberType == MemberTypes.Field)
        {
            return ((FieldInfo)member).GetValue(instance);
        }
        else if (member.MemberType == MemberTypes.Property)
        {
            return ((PropertyInfo)member).GetValue(instance);

        }
        else throw new InvalidOperationException($"GetValue can be called only on Fields or Properties");

    }

    public static Type GetMemberType(this MemberInfo member)
    {
        if (member.MemberType == MemberTypes.Field)
        {
            return ((FieldInfo)member).FieldType;
        }
        else if (member.MemberType == MemberTypes.Property)
        {
            return ((PropertyInfo)member).PropertyType;
        }
        else throw new InvalidOperationException($"GetValue can be called only on Fields or Properties");

    }

    public static Dictionary<string, Type> GetEntitiesTypes(this Assembly asm)
    {
        Dictionary<string, Type> entitiesTypes = new(StringComparer.OrdinalIgnoreCase);
        foreach (Type type in asm.GetTypes())
        {
            if (typeof(Core.EntityBase).IsAssignableFrom(type))
            {
                entitiesTypes.Add(type.Name, type);
            }
        }
        return entitiesTypes;
    }

    public static Dictionary<string, Type> GetCategoriesTypes(this Assembly asm)
    {
        Dictionary<string, Type> categoriesTypes = new(StringComparer.OrdinalIgnoreCase);
        foreach (Type type in asm.GetTypes())
        {
            if (typeof(CategoryDefinition).IsAssignableFrom(type))
            {
                categoriesTypes.Add(type.Name, type);
            }
        }
        return categoriesTypes;
    }

    public static Dictionary<string, Type> GetAllCategoriesTypes(this List<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(CategoryDefinition).IsAssignableFrom(type) && type != typeof(CategoryDefinition))
            .ToDictionary(type => type.Name, type => type);
    }

    public static Dictionary<string, Type> GetStatusTypes(this Assembly asm)
    {
        Dictionary<string, Type> statusTypes = new(StringComparer.OrdinalIgnoreCase);
        foreach (Type type in asm.GetTypes())
        {
            if (typeof(StatusDefinition).IsAssignableFrom(type))
            {
                statusTypes.Add(type.Name, type);
            }
        }
        return statusTypes;
    }

    public static List<Type> GetInterfaceTypes<T>(this Assembly asm)
    {
        var types = asm.GetTypes();

        return types.Where(t => t.GetInterfaces().Contains(typeof(T)) && t.IsClass).ToList();
    }

    public static List<PropertyInfo> GetPublicPropertiesOrFields(this Type obj_type)
    {
        List<PropertyInfo> members = [];
        var props = obj_type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props)
        {
            if (prop.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && prop.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                members.Add((PropertyInfo)prop);
        }
        return members;
    }

    /// <summary>
    /// Retrieves all properties and fields of a specified type from the given object.
    /// </summary>
    /// <typeparam name="TFilter">The type of properties or fields to filter.</typeparam>
    /// <typeparam name="TObject">The type of the object to inspect.</typeparam>
    /// <param name="obj">The object from which to retrieve properties or fields.</param>
    /// <returns>A list of properties or fields of the specified type.</returns>
    public static List<TFilter> GetPropertiesOrFields<TFilter, TObject>(this TObject obj) where TFilter : class
    {
        if (obj == null) return [];
        Type filter_type = typeof(TFilter);
        Type obj_type = obj.GetType();
        var ret = new List<TFilter>();

        MemberInfo[] instance_members = obj_type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var prop in instance_members)
        {
            if (prop.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && prop.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
            {
                if (prop.GetMemberType().Equals(filter_type))
                {
                    ret.Add((TFilter)prop.GetMemberValue(obj)!);
                }
            }
        }

        return ret;
    }

    /// <summary>
    /// Get the first property in the hierarchy of the type.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="property_name"></param>
    /// <param name="binding_flags"></param>
    /// <returns></returns>
    public static PropertyInfo? GetFirstPropertyInHierarchy(this Type type, string property_name, BindingFlags binding_flags)
    {
        // Ensure binding are set to declared only
        binding_flags |= BindingFlags.DeclaredOnly;
        PropertyInfo? prop = type.GetProperty(property_name, binding_flags);
        while (prop == null && type.BaseType != null)
        {
            prop = type.BaseType.GetProperty(property_name, binding_flags);
        }
        return prop;
    }

    public static IOrderedEnumerable<MemberInfo> GetMembersInDeclarationOrder(this Type instanceType)
    {
        ArgumentNullException.ThrowIfNull(instanceType);

        // Build a list of types in the inheritance chain, starting at instanceType.
        // The most-derived type gets index 0, its immediate base gets 1, etc.
        var inheritanceChain = new List<Type>();
        for (Type? t = instanceType; t != null; t = t?.BaseType)
        {
            if (t != null) inheritanceChain.Add(t);
        }

        // Create a lookup dictionary for quick access to each type's depth.
        var typeDepth = inheritanceChain
                        .Select((t, depth) => new { t, depth })
                        .ToDictionary(x => x.t, x => x.depth);

        // Get all instance members (public and non-public) and order them:
        // 1. By the depth of the declaring type (most-derived first).
        // 2. By MetadataToken to preserve the declaration order within each type.
        return instanceType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .OrderBy(member => typeDepth.TryGetValue(member.DeclaringType ?? typeof(int), out int value) ? value : int.MaxValue)
            .ThenBy(member => member.MetadataToken);
    }


    private static readonly ConcurrentDictionary<string, IOrderedEnumerable<MemberInfo>> _classMembers = new();

    public static IOrderedEnumerable<MemberInfo> GetAndCacheInstanceMembers(this Type instance_type)
    {
        string instance_typename = instance_type.ToString();
        IOrderedEnumerable<MemberInfo> instance_members;
        if (!_classMembers.TryGetValue(instance_typename, out IOrderedEnumerable<MemberInfo>? value))
        {
            // MMC: ordering by metadataToken is the trick to add the columns ordered as they are defined
            // for backward compatibility we still depend that the _get stored procedure return the columns in the order that are defined
            instance_members = instance_type.GetMembersInDeclarationOrder();
            _classMembers.TryAdd(instance_typename, instance_members);
        }
        else
        {
            instance_members = value;
        }

        return instance_members;
    }

}
