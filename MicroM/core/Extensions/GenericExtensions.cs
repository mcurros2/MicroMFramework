using System.ComponentModel;
using System.Globalization;

namespace MicroM.Extensions
{
    public static class GenericExtensions
    {

        public static bool IsIn<T>(this T value, T[] parms, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            if (parms == null) return false;
            foreach (T v in parms)
            {
                if (comparer.Equals(v, value)) return true;
            }
            return false;
        }

        public static bool AreAllEqual<T>(this T value, T[] parms, bool ignore_null = false, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            if (parms == null) return false;
            foreach (T v in parms)
            {
                if (ignore_null && v == null) continue;
                if (comparer.Equals(v, value) == false) return false;
            }
            return true;
        }

        public static bool IsIn<T>(this T value, params T[] parms)
        {
            if (parms == null) return false;
            foreach (T v in parms)
            {
                if (v != null && v.Equals(value)) return true;
            }
            return false;
        }

        public static bool HasAnyFlag<T>(this T value, T flags) where T : Enum
        {
            int intValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            int intFlags = Convert.ToInt32(flags, CultureInfo.InvariantCulture);

            return (intValue & intFlags) > 0 || intValue == intFlags;
        }

        public static bool HasAllFlags<T>(this T value, T flags) where T : Enum
        {
            int intValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            int intFlags = Convert.ToInt32(flags, CultureInfo.InvariantCulture);

            return (intValue & intFlags) == intFlags;
        }

        public static bool TryConvertFromString<T>(this string source, out T? result) where T : struct
        {
            var type = typeof(T);
            var converter = TypeDescriptor.GetConverter(type);
            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                try
                {
                    result = (T)converter.ConvertFromInvariantString(source)!;
                    return true;
                }
                catch (Exception)
                {
                    // Conversion failed
                }
            }

            result = null;
            return false;
        }

        public static bool IsNullOrEmpty<T>(this T? collection) where T : ICollection<T>
        {
            return collection?.Count > 0;
        }
        public static bool IsNullOrEmpty<T>(this IList<T>? collection)
        {
            return collection?.Count > 0;
        }

    }


}

