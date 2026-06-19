using System.DirectoryServices.Protocols;

namespace MicroM.Extensions;

public static class ActiveDirectoryExtensions
{
    public static T[]? GetADAttributes<T>(this SearchResultEntry? entry, string attributeName)
    {
        if (entry is null)
            return null;

        if (!entry.Attributes.Contains(attributeName))
            return null;

        var attribute = entry.Attributes[attributeName];
        if (attribute is null || attribute.Count == 0)
            return null;

        var result = new T[attribute.Count];

        for (int i = 0; i < attribute.Count; i++)
        {
            object value = attribute[i]!;

            if (value is T typedValue)
            {
                result[i] = typedValue;
            }
            else
            {
                throw new InvalidCastException($"The value of attribute '{attributeName}' at index {i} is of type '{value.GetType().FullName}' and cannot be cast to type {typeof(T).FullName}.");
            }
        }

        return result;
    }

    public static T? GetADAttribute<T>(this SearchResultEntry? entry, string attributeName)
    {
        var values = entry.GetADAttributes<T>(attributeName);
        return values is { Length: > 0 } ? values[0] : default;
    }

}
