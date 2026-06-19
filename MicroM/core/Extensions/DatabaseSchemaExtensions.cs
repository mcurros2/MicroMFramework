using MicroM.Data;
using MicroM.DataDictionary.Configuration;
using System.Reflection;

namespace MicroM.Extensions;

public static class DatabaseSchemaExtensions
{
    public static async Task CreateAssemblyCustomProcs(this Assembly assembly, IEntityClient ec, CancellationToken ct, string? mneo = null, string? starts_with = null, string? schema_name = null)
    {
        foreach (string script in await assembly.GetAssemblyCustomProcs(mneo, starts_with, ct, schema_name))
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

    public async static Task CreateAllCategories(this Assembly asm, IEntityClient ec, CancellationToken ct, string? schema_name = null)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        var categories = asm.GetCategoriesTypes();
        try
        {
            await ec.Connect(ct);
            foreach (var category_type in categories.Values)
            {
                CategoryDefinition? cat = (CategoryDefinition?)Activator.CreateInstance(category_type);
                if (cat != null) await cat.AddCategory(ec, ct, schema_name);
            }
        }
        finally
        {
            if (categories != null && categories.Count > 0) categories.Clear();
            if (should_close) await ec.Disconnect();
        }
    }

    public async static Task CreateAllStatus(this Assembly asm, IEntityClient ec, CancellationToken ct, string? schema_name = null)
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        var status = asm.GetStatusTypes();
        try
        {
            await ec.Connect(ct);
            foreach (var status_type in status.Values)
            {
                StatusDefinition? stat = (StatusDefinition?)Activator.CreateInstance(status_type);
                if (stat != null) await stat.AddStatus(ec, ct, schema_name);
            }
        }
        finally
        {
            if (status != null && status.Count > 0) status.Clear();
            if (should_close) await ec.Disconnect();
        }
    }

}
