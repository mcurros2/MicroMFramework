using System.Text.RegularExpressions;

namespace MicroM.Validators
{
    /// <summary>
    /// Provides regular expression validators for common MicroM inputs.
    /// </summary>
    public partial class Expressions
    {
        /// <summary>
        /// Creates a regex that matches alphanumeric characters and underscores.
        /// </summary>
        /// <returns>A <see cref="Regex"/> for validating digits, letters, and underscores.</returns>
        [GeneratedRegex("^[A-Za-z0-9_]*$")]
        public static partial Regex OnlyDigitNumbersAndUnderscore();

        /// <summary>
        /// Creates a regex that validates SQL Server login names including optional domain notation.
        /// </summary>
        /// <returns>A <see cref="Regex"/> for SQL Server login validation.</returns>
        [GeneratedRegex("^(?:[a-zA-Z0-9_#$]{1,127}|([a-zA-Z0-9_#$]{1,127}\\[a-zA-Z0-9_#$]{1,127}))$")]
        public static partial Regex ValidSQLServerLogin();

        /// <summary>
        /// Creates a regex that validates SQL Server passwords with allowed special characters.
        /// </summary>
        /// <returns>A <see cref="Regex"/> for SQL Server password validation.</returns>
        [GeneratedRegex("^[a-zA-Z0-9_@*^%!$#&]*$")]
        public static partial Regex ValidSQLServerPassword();
    }
}
