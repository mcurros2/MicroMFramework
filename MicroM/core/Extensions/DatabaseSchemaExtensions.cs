using MicroM.Data;
using MicroM.DataDictionary.Configuration;
using System.Reflection;

namespace MicroM.Extensions
{
    public static class DatabaseSchemaExtensions
    {
        /// <summary>
        /// Executes custom SQL procedures embedded in the assembly resources.
        /// </summary>
        /// <param name="assembly">Assembly containing resources.</param>
        /// <param name="ec">Entity client used to execute scripts.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="mneo">Optional mneo filter.</param>
        /// <param name="starts_with">Optional name prefix filter.</param>
        public static async Task CreateAssemblyCustomProcs(this Assembly assembly, IEntityClient ec, CancellationToken ct, string? mneo = null, string? starts_with = null)
        {
            foreach (string script in await assembly.GetAssemblyCustomProcs(mneo, starts_with, ct))
            {
                try
                {
                    await ec.ExecuteSQLNonQuery(script, ct);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error executing script: {script}: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Creates all category definitions discovered in the assembly.
        /// </summary>
        /// <param name="asm">Assembly to scan.</param>
        /// <param name="ec">Entity client.</param>
        /// <param name="ct">Cancellation token.</param>
        public async static Task CreateAllCategories(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var categories = asm.GetCategoriesTypes();
            try
            {
                await ec.Connect(ct);
                foreach (var category_type in categories.Values)
                {
                    CategoryDefinition? cat = (CategoryDefinition?)Activator.CreateInstance(category_type);
                    if (cat != null) await cat.AddCategory(ec, ct);
                }
            }
            finally
            {
                if (categories != null && categories.Count > 0) categories.Clear();
                if (should_close) await ec.Disconnect();
            }
        }

        /// <summary>
        /// Creates all status definitions discovered in the assembly.
        /// </summary>
        /// <param name="asm">Assembly to scan.</param>
        /// <param name="ec">Entity client.</param>
        /// <param name="ct">Cancellation token.</param>
        public async static Task CreateAllStatus(this Assembly asm, IEntityClient ec, CancellationToken ct)
        {
            bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
            var status = asm.GetStatusTypes();
            try
            {
                await ec.Connect(ct);
                foreach (var status_type in status.Values)
                {
                    StatusDefinition? stat = (StatusDefinition?)Activator.CreateInstance(status_type);
                    if (stat != null) await stat.AddStatus(ec, ct);
                }
            }
            finally
            {
                if (status != null && status.Count > 0) status.Clear();
                if (should_close) await ec.Disconnect();
            }
        }

    }
}
