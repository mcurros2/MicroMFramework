using System.ComponentModel;
using System.Globalization;

namespace MicroM.Extensions
{
    public static class GenericExtensions
    {
        /// <summary>
        /// Determines if a value exists within the provided array using an optional comparer.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="value">Value to search for.</param>
        /// <param name="parms">Array of values.</param>
        /// <param name="comparer">Comparer to use.</param>
        /// <returns><c>true</c> if the value is found.</returns>
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

        /// <summary>
        /// Determines if all values in the array are equal to the specified value.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="value">Value to compare.</param>
        /// <param name="parms">Array of values.</param>
        /// <param name="ignore_null">Whether to ignore null values.</param>
        /// <param name="comparer">Comparer to use.</param>
        /// <returns><c>true</c> if all values are equal.</returns>
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

        /// <summary>
        /// Determines if a value exists within the provided array using default comparison.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="value">Value to search for.</param>
        /// <param name="parms">Array of values.</param>
        /// <returns><c>true</c> if the value is found.</returns>
        public static bool IsIn<T>(this T value, params T[] parms)
        {
            if (parms == null) return false;
            foreach (T v in parms)
            {
                if (v != null && v.Equals(value)) return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether any of the specified flags are set on the value.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="value">Value to check.</param>
        /// <param name="flags">Flags to evaluate.</param>
        /// <returns><c>true</c> if any flag is set.</returns>
        public static bool HasAnyFlag<T>(this T value, T flags) where T : Enum
        {
            int intValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            int intFlags = Convert.ToInt32(flags, CultureInfo.InvariantCulture);

            return (intValue & intFlags) > 0 || intValue == intFlags;
        }

        /// <summary>
        /// Checks whether all specified flags are set on the value.
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="value">Value to check.</param>
        /// <param name="flags">Flags to evaluate.</param>
        /// <returns><c>true</c> if all flags are set.</returns>
        public static bool HasAllFlags<T>(this T value, T flags) where T : Enum
        {
            int intValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            int intFlags = Convert.ToInt32(flags, CultureInfo.InvariantCulture);

            return (intValue & intFlags) == intFlags;
        }

        /// <summary>
        /// Attempts to convert a string into the specified value type.
        /// </summary>
        /// <typeparam name="T">Value type.</typeparam>
        /// <param name="source">Source string.</param>
        /// <param name="result">Converted result when successful.</param>
        /// <returns><c>true</c> if conversion succeeded.</returns>
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

        /// <summary>
        /// Determines whether a collection is null or empty.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="collection">Collection to test.</param>
        /// <returns><c>true</c> if the collection is null or empty.</returns>
        public static bool IsNullOrEmpty<T>(this T? collection) where T : ICollection<T>
        {
            return collection?.Count > 0;
        }

        /// <summary>
        /// Determines whether a list is null or empty.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="collection">List to test.</param>
        /// <returns><c>true</c> if the list is null or empty.</returns>
        public static bool IsNullOrEmpty<T>(this IList<T>? collection)
        {
            return collection?.Count > 0;
        }

    }


}

