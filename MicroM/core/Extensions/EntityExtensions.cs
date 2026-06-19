using MicroM.Core;
using MicroM.Data;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.Extensions;

public static class EntityExtensions
{
    /// <summary>
    /// Get the column names of an entity type
    /// </summary>
    /// <param name="entity_type"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<string> GetColumnNames(this Type entity_type)
    {
        ArgumentNullException.ThrowIfNull(entity_type);
        if (!typeof(EntityBase).IsAssignableFrom(entity_type))
        {
            throw new ArgumentException($"The type {entity_type.Name} is not an EntityBase type.");
        }

        // get the the Def property
        var def_prop = entity_type.GetFirstPropertyInHierarchy(nameof(EntityBase.Def), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) ?? throw new ArgumentException($"The type {entity_type.Name} does not have a Def property.");
        var def_type = def_prop.PropertyType;

        Type filter_type = typeof(ColumnBase);
        var ret = new List<string>();

        MemberInfo[] instance_members = def_type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var prop in instance_members)
        {
            if (prop.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && prop.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
            {
                if (prop.GetMemberType().IsAssignableTo(filter_type))
                {
                    ret.Add(prop.Name);
                }
            }
        }

        return ret;
    }

    public static async Task<bool> HasDataInTable(this EntityBase entity, CancellationToken ct)
    {
        if (entity.Def.Fake) return false;
        var data_exists = false;
        try
        {
            var ec = entity.Client;
            var result = await ec.ExecuteSQLSingleColumn<int>($"SELECT TOP 1 1 FROM {entity.Def.FullTableName}", ct);
            return result == 1;
        }
        catch { }

        return data_exists;
    }

}
