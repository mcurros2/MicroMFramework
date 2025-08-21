using MicroM.Data;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace MicroM.Core
{
    /// <summary>
    /// Provides utilities to validate entity definitions.
    /// </summary>
    public class EntityChecker
    {

        /// <summary>
        /// Validates a single entity type.
        /// </summary>
        /// <param name="entity_type">The entity type to check.</param>
        /// <returns>A string describing problems, if any.</returns>
        public static string CheckEntity(Type entity_type)
        {
            StringBuilder problems = new();
            var (cols, views, procs) = GetProperties(entity_type);
            EntityBase entity = (EntityBase?)Activator.CreateInstance(entity_type) ?? throw new Exception($"Could not create instance of {entity_type}.");

            // check: all columns have corresponding named properties
            var columns_without_properties = entity.Def.Columns.Keys.Where(key => !cols.ContainsKey(key));
            foreach (string property in columns_without_properties) problems.AppendFormat(CultureInfo.InvariantCulture, "{0} Property not found for column {1}.\n", entity_type, property);

            // check: all properties are in the columns dictionary
            var properties_without_columns = cols.Keys.Where(key => !entity.Def.Columns.Contains(key));
            foreach (string column in properties_without_columns) problems.AppendFormat(CultureInfo.InvariantCulture, "{0} Column has been defined as property but not found in Def.Columns {1}.\n", entity_type, column);

            // check: all views have corresponding named properties
            var views_without_properties = entity.Def.Views.Keys.Where(key => !views.ContainsKey(key));
            foreach (string property in views_without_properties) problems.AppendFormat(CultureInfo.InvariantCulture, "{0} Property not found for view {1}.\n", entity_type, property);

            // check: all views are in the views dictionary
            var properties_without_views = views.Keys.Where(key => !entity.Def.Views.ContainsKey(key));
            foreach (string view in properties_without_views) problems.AppendFormat(CultureInfo.InvariantCulture, "{0} View has been defined as property but not found in Def.Views {1}.\n", entity_type, view);


            // check: all procs have corresponding named properties
            var procs_without_properties = entity.Def.Procs.Keys.Where(key => !views.ContainsKey(key));
            foreach (string property in procs_without_properties) problems.AppendFormat(CultureInfo.InvariantCulture, "{0} Property not found for procedure {1}.\n", entity_type, property);
            //procs_without_properties = entity.Def.Queries.Keys.Where(key => !props.views.ContainsKey(key));
            //foreach (string property in procs_without_properties) problems.AppendFormat("{0} Property not found for procedure {1}.\n", entity_type, property);

            // check: all procs are in the procs dictionary
            //var properties_without_procs = props.cols.Keys.Where(key => (!entity.Def.Procs.ContainsKey(key) && !entity.Def.Queries.ContainsKey(key)));
            //foreach (string proc in properties_without_procs) problems.AppendFormat("{0} Procedure has been defined as property but not found in Def.Procs/Def.Queries {1}.\n", entity_type, proc);

            /*
            if (string.IsNullOrEmpty(compound_key_group))
            {
                if (view.CompoundKeyGroups.Values.Count > 1)
                {
                    throw new ArgumentException($"{name}: The view \"{view.Proc.destination_name}\" has more than one CompoundKeyGroup. You must specify a compound key group name to use.", nameof(compound_key_group));
                }
            }
            else
            {
                if (!view.CompoundKeyGroups.ContainsKey(compound_key_group))
                {
                    throw new ArgumentException($"{name}: Compound key group \"{compound_key_group}\" not found in view \"{view.Proc.destination_name}\".", nameof(compound_key_group));
                }
            }

            if (!string.IsNullOrEmpty(key_parameter))
            {
                if (!view.Proc.Parms.ContainsKey(key_parameter.destination_name))
                {
                    throw new ArgumentException($"{name}: The key parameter specified \"{key_parameter.destination_name}\" not found in view \"{view.Proc.destination_name}\".", nameof(key_parameter));
                }
            }
            else
            {
                int mappings = 0;
                foreach (var mapping in view.DSColumnMappings.Values)
                {
                    if (mapping > -1) mappings++;
                    if (mappings > 1)
                    {
                        throw new ArgumentException($"{name}: There is more than one key mapped for view \"{view.Proc.destination_name}\". You must specify a value.", nameof(key_parameter));
                    }
                }

             */

            return problems.ToString();
        }

        /// <summary>
        /// Gets the column, view and procedure properties defined on an entity type.
        /// </summary>
        public static
            (
            Dictionary<string, PropertyInfo> cols,
            Dictionary<string, PropertyInfo> views,
            Dictionary<string, PropertyInfo> procs
            ) GetProperties(Type entity)
        {
            (
            Dictionary<string, PropertyInfo> cols,
            Dictionary<string, PropertyInfo> views,
            Dictionary<string, PropertyInfo> procs
            ) ret;
            ret.cols = [];
            ret.views = [];
            ret.procs = [];
            foreach (var prop in entity.GetProperties(BindingFlags.Public))
            {
                if (prop.PropertyType.IsAssignableFrom(typeof(ColumnBase))) ret.cols.Add(prop.Name, prop);
                else if (prop.PropertyType.IsAssignableFrom(typeof(ViewDefinition))) ret.views.Add(prop.Name, prop);
                else if (prop.PropertyType.IsAssignableFrom(typeof(ProcedureDefinition))) ret.procs.Add(prop.Name, prop);
            }
            return ret;
        }

        /// <summary>
        /// Validates all entities in the specified assembly.
        /// </summary>
        /// <param name="asm">The assembly containing entities. If null, the executing assembly is used.</param>
        /// <param name="assembly_name">Optional assembly name to load.</param>
        public static void CheckEntities(Assembly? asm = null, string? assembly_name = null)
        {
            if (asm == null && !string.IsNullOrEmpty(assembly_name)) asm = Assembly.Load(assembly_name);
            if (asm == null) asm = Assembly.GetExecutingAssembly();

            StringBuilder problems = new();
            foreach (Type type in asm.GetTypes())
            {
                if (typeof(EntityBase).IsAssignableFrom(type))
                {
                    problems.Append(CheckEntity(type));
                }
            }

            string ret = problems.ToString();
            if (!string.IsNullOrEmpty(ret))
            {
                throw new Exception(ret);
            }

        }

    }
}
