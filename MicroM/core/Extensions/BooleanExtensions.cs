namespace MicroM.Extensions;

public static class BooleanExtensions
{
    public static string True(this bool value, string true_value, string false_value = "")
    {
        return value ? true_value : false_value;
    }

    public static string False(this bool value, string false_value, string true_value = "")
    {
        return value ? true_value : false_value;
    }
}
