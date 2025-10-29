using MicroM.Extensions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MicroM.Data;

public enum AutoMapperMode
{
    /// <summary>
    /// This mode maps the header names from the query results to existing properties in the mapped object.
    /// It is case sensitive and it will throw an exception if a property of the mapped object is missing from the headers.
    /// </summary>
    ByName,
    /// <summary>
    /// This mode maps the header names from the query results to existing properties in the mapped object.
    /// If column names contain spaces, spaces will be replaced by underscores.
    /// It is case sensitive and it will throw an exception if a property of the mapped object is missing from the headers.
    /// </summary>
    ByNameSpacesToUnderscore,
    /// <summary>
    /// This mode maps the header names from the query results to existing properties in the mapped object.
    /// It will not throw an exception if a property of the mapped object has not been returned from the query.
    /// Option returned headers must become nullable properties in the mapped object or it will throw a null reference exception
    /// </summary>
    ByNameLaxNotThrow,
    /// <summary>
    /// This mode maps the returned values from the query results to existing properties in the mapped object by position.
    /// It will loop through the objects properties in declared order and assing each value it the order returned by the query
    /// </summary>
    ByPosition
}

public static class AutoMapper
{
    public async static IAsyncEnumerable<T> AutoMapperGetResultByPosition<T>(ValueReader vr, [EnumeratorCancellation] CancellationToken ct) where T : new()
    {

        var members = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(p => p.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && p.GetCustomAttribute<CompilerGeneratedAttribute>() == null).OrderBy(p => p.MetadataToken);

        if (await vr._reader.ReadAsync(ct))
        {
            do
            {
                ct.ThrowIfCancellationRequested();
                T record = new();
                int x = 0;
                foreach (var member in members)
                {
                    var val = await vr.GetFieldValueAsync<object>(x++, ct);
                    if (val == null || val?.GetType() == typeof(DBNull)) val = null;

                    if (member is PropertyInfo prop)
                    {
                        prop.SetValue(record, val);
                    }
                    else if (member is FieldInfo field)
                    {
                        field.SetValue(record, val);
                    }
                }
                yield return record;
            }
            while (await vr._reader.ReadAsync(ct));
        }
    }

    public async static IAsyncEnumerable<T> AutoMapperGetResultByName<T>(ValueReader vr, AutoMapperMode mode, [EnumeratorCancellation] CancellationToken ct) where T : new()
    {
        var members = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(p => p.MemberType.IsIn(MemberTypes.Property, MemberTypes.Field) && p.GetCustomAttribute<CompilerGeneratedAttribute>() == null);

        if (await vr._reader.ReadAsync(ct))
        {
            var headers = DataMappingProvider.GetHeadersHashSet(vr._reader, mode == AutoMapperMode.ByNameSpacesToUnderscore);
            do
            {
                ct.ThrowIfCancellationRequested();
                T record = new();
                List<string> headerErrors = [];
                foreach (var member in members)
                {
                    if (!headers.Contains(member.Name))
                    {
                        headerErrors.Add(member.Name);
                    }
                    else
                    {
                        var val = await vr.GetFieldValueAsync<object>(member.Name, ct);
                        if (val == null || val?.GetType() == typeof(DBNull)) val = null;
                        if (member is PropertyInfo prop)
                        {
                            prop.SetValue(record, val);
                        }
                        else if (member is FieldInfo field)
                        {
                            field.SetValue(record, val);
                        }
                    }

                }
                if (headerErrors.Count > 0 && mode == AutoMapperMode.ByName) throw new MissingMemberException($"Missing expected columns: {string.Join(", ", headerErrors)}");

                yield return record;
            }
            while (await vr._reader.ReadAsync(ct));
        }
    }

}
