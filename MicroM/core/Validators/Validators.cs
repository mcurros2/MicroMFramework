using System.Text.RegularExpressions;

namespace MicroM.Validators
{
    public partial class Expressions
    {
        [GeneratedRegex("^[A-Za-z0-9_]*$")]
        public static partial Regex OnlyDigitNumbersAndUnderscore();

        [GeneratedRegex("^(?:[a-zA-Z0-9_#$]{1,127}|([a-zA-Z0-9_#$]{1,127}\\[a-zA-Z0-9_#$]{1,127}))$")]
        public static partial Regex ValidSQLServerLogin();

        [GeneratedRegex("^[a-zA-Z0-9_@*^%!$#&]*$")]
        public static partial Regex ValidSQLServerPassword();

    }

}
