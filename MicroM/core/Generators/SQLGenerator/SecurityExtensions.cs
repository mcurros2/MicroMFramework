using MicroM.Core;
using System.Globalization;
using System.Text;

namespace MicroM.Generators.SQLGenerator
{
    /// <summary>
    /// Provides extension methods for generating SQL security statements.
    /// </summary>
    internal static class SecurityExtensions
    {
        /// <summary>
        /// Generates SQL that grants execute permissions on all stored procedures related to the specified entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity.</typeparam>
        /// <param name="entity">The entity whose procedures will be granted execution rights.</param>
        /// <param name="login_or_group_name">The login or group receiving the execute permissions.</param>
        /// <returns>A SQL script granting execute permissions on all procedures for the entity.</returns>
        public static string AsGrantExecutionToAllProcs<T>(this T entity, string login_or_group_name) where T : EntityBase
        {
            StringBuilder sb = new();

            sb.AppendFormat(CultureInfo.InvariantCulture, "grant exec on [{0}_update] to [{1}]\n", entity.Def.Mneo, login_or_group_name);
            sb.AppendFormat(CultureInfo.InvariantCulture, "grant exec on [{0}_get] to [{1}]\n", entity.Def.Mneo, login_or_group_name);
            sb.AppendFormat(CultureInfo.InvariantCulture, "grant exec on [{0}_drop] to [{1}]\n", entity.Def.Mneo, login_or_group_name);
            sb.AppendFormat(CultureInfo.InvariantCulture, "grant exec on [{0}_lookup] to [{1}]\n", entity.Def.Mneo, login_or_group_name);

            foreach (var proc in entity.Def.Procs.Values)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "grant exec on [{0}] to [{1}]\n", proc.Name, login_or_group_name);
            }

            foreach (var view in entity.Def.Views.Values)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "grant exec on [{0}] to [{1}]\n", view.Proc.Name, login_or_group_name);
            }

            return sb.ToString();
        }

    }
}
