using MicroM.Core;
using System.Globalization;
using System.Text;

namespace MicroM.Generators.SQLGenerator
{
    internal static class SecurityExtensions
    {
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
