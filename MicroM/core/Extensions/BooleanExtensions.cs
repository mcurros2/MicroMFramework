namespace MicroM.Extensions;

public static class BooleanExtensions
{
    /// <summary>
    /// Returns <paramref name="true_value"/> when the boolean is true and <paramref name="false_value"/> otherwise.
    /// </summary>
    /// <param name="value">Boolean value to evaluate.</param>
    /// <param name="true_value">String returned when <paramref name="value"/> is true.</param>
    /// <param name="false_value">String returned when <paramref name="value"/> is false.</param>
    /// <returns>The appropriate string based on the boolean value.</returns>
    public static string True(this bool value, string true_value, string false_value = "")
    {
        return value ? true_value : false_value;
    }

    /// <summary>
    /// Returns <paramref name="false_value"/> when the boolean is false and <paramref name="true_value"/> otherwise.
    /// </summary>
    /// <param name="value">Boolean value to evaluate.</param>
    /// <param name="false_value">String returned when <paramref name="value"/> is false.</param>
    /// <param name="true_value">String returned when <paramref name="value"/> is true.</param>
    /// <returns>The appropriate string based on the boolean value.</returns>
    public static string False(this bool value, string false_value, string true_value = "")
    {
        return value ? true_value : false_value;
    }
}
