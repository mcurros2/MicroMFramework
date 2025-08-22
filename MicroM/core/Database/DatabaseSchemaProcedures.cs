using MicroM.Core;
using MicroM.Data;
using MicroM.Extensions;
using MicroM.Generators.SQLGenerator;
using static MicroM.Data.SystemStandardProceduresSuffixs;
using static MicroM.Database.DatabaseSchemaCustomScripts;


namespace MicroM.Database;

/// <summary>
/// Helpers for generating and executing database stored procedures.
/// </summary>
public static class DatabaseSchemaProcedures
{
    /// <summary>
    /// Creates custom procedures defined for an entity.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="ent">Optional initialized entity instance.</param>
    /// <param name="ec">Entity client.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task CreateCustomProcs<T>(T? ent, IEntityClient ec, CancellationToken ct) where T : EntityBase, new()
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            T new_ent;

            if (ent == null)
            {
                new_ent = new();
                new_ent.Init(ec);
            }
            else
            {
                new_ent = ent;
            }

            foreach (string script in await new_ent.GetAllCustomProcs(new_ent.Def.Mneo, ct))
            {
                await ec.ExecuteSQLNonQuery(script, ct);
            }
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }
    }

    /// <summary>
    /// Generates and executes standard procedures for an entity.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="ent">Entity instance.</param>
    /// <param name="ec">Entity client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="classified_custom_procs">Custom scripts that may replace generated procedures.</param>
    /// <param name="create_or_alter">Indicates if procedures should be created or altered.</param>
    public static async Task CreateGeneratedProcs<T>(
        T ent,
        IEntityClient ec,
        CancellationToken ct,
        CustomOrderedDictionary<CustomScript>? classified_custom_procs,
        bool create_or_alter

        ) where T : EntityBase
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            string update_proc_name = $"{ent.Def.Mneo}{_update}";
            string iupdate_proc_name = $"{ent.Def.Mneo}{_iupdate}";
            string get_proc_name = $"{ent.Def.Mneo}{_get}";
            string drop_proc_name = $"{ent.Def.Mneo}{_drop}";
            string idrop_proc_name = $"{ent.Def.Mneo}{_idrop}";
            string lookup_proc_name = $"{ent.Def.Mneo}{_lookup}";
            string view_proc_name = $"{ent.Def.Mneo}{_brwStandard}";

            bool with_iupdate = ent.Def.SQLCreationOptions.HasFlag(SQLCreationOptionsMetadata.WithIUpdate);
            bool with_idrop = ent.Def.SQLCreationOptions.HasFlag(SQLCreationOptionsMetadata.WithIDrop);

            CustomOrderedDictionary<string> generated_scripts = new();

            var generated_update_scripts = ent.AsCreateUpdateProc(create_or_alter, with_iupdate);
            if (generated_update_scripts?.Count > 0)
            {
                if (with_iupdate)
                {
                    generated_scripts.Add(iupdate_proc_name, generated_update_scripts[0]);
                    generated_scripts.Add(update_proc_name, generated_update_scripts[1]);
                }
                else
                {
                    generated_scripts.Add(update_proc_name, generated_update_scripts[0]);
                }
            }

            var get_proc_script = ent.AsCreateGetProc(create_or_alter);
            if (get_proc_script?.Length > 0) generated_scripts.Add(get_proc_name, get_proc_script);

            var drop_proc_scripts = ent.AsCreateDropProc(create_or_alter, with_idrop);
            if (drop_proc_scripts?.Count > 0)
            {
                if (with_idrop)
                {
                    generated_scripts.Add(idrop_proc_name, drop_proc_scripts[0]);
                    generated_scripts.Add(drop_proc_name, drop_proc_scripts[1]);
                }
                else
                {
                    generated_scripts.Add(drop_proc_name, drop_proc_scripts[0]);
                }
            }

            var lookup_proc_script = ent.AsCreateLookupProc(create_or_alter);
            if (lookup_proc_script?.Length > 0) generated_scripts.Add(lookup_proc_name, lookup_proc_script);

            var view_proc_script = ent.AsCreateViewProc(create_or_alter);
            if (view_proc_script?.Length > 0) generated_scripts.Add(view_proc_name, view_proc_script);

            foreach (var script_key in generated_scripts.Keys)
            {
                // Skip the proc is it has a custom option available
                if (classified_custom_procs != null && classified_custom_procs.Contains(script_key) == false)
                {
                    await ec.ExecuteSQLNonQuery(generated_scripts[script_key]!, ct);
                }
            }

        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

    }

    /// <summary>
    /// Creates both generated and custom procedures for an entity.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="ent">Entity instance.</param>
    /// <param name="ec">Entity client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="create_or_alter">Indicates if procedures should be created or altered.</param>
    /// <param name="create_custom_procs">Whether to include custom procedures.</param>
    public static async Task CreateProcs<T>(
        T ent,
        IEntityClient ec,
        CancellationToken ct,
        bool create_or_alter,
        bool create_custom_procs = true
        ) where T : EntityBase
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {

            // get custom procs
            var custom_procs = await ent.GetAllCustomProcs(ent.Def.Mneo, ct);

            string update_proc_name = $"{ent.Def.Mneo}{_update}";
            string iupdate_proc_name = $"{ent.Def.Mneo}{_iupdate}";
            string get_proc_name = $"{ent.Def.Mneo}{_get}";
            string drop_proc_name = $"{ent.Def.Mneo}{_drop}";
            string idrop_proc_name = $"{ent.Def.Mneo}{_idrop}";
            string lookup_proc_name = $"{ent.Def.Mneo}{_lookup}";
            string view_proc_name = $"{ent.Def.Mneo}{_brwStandard}";

            bool with_iupdate = ent.Def.SQLCreationOptions.HasFlag(SQLCreationOptionsMetadata.WithIUpdate);
            bool with_idrop = ent.Def.SQLCreationOptions.HasFlag(SQLCreationOptionsMetadata.WithIDrop);

            CustomOrderedDictionary<string> generated_scripts = new();

            var generated_update_scripts = ent.AsCreateUpdateProc(create_or_alter, with_iupdate);
            if (generated_update_scripts?.Count > 0)
            {
                if (with_iupdate)
                {
                    generated_scripts.Add(iupdate_proc_name, generated_update_scripts[0]);
                    generated_scripts.Add(update_proc_name, generated_update_scripts[1]);
                }
                else
                {
                    generated_scripts.Add(update_proc_name, generated_update_scripts[0]);
                }
            }

            var get_proc_script = ent.AsCreateGetProc(create_or_alter);
            if (get_proc_script?.Length > 0) generated_scripts.Add(get_proc_name, get_proc_script);

            var drop_proc_scripts = ent.AsCreateDropProc(create_or_alter, with_idrop);
            if (drop_proc_scripts?.Count > 0)
            {
                if (with_idrop)
                {
                    generated_scripts.Add(idrop_proc_name, drop_proc_scripts[0]);
                    generated_scripts.Add(drop_proc_name, drop_proc_scripts[1]);
                }
                else
                {
                    generated_scripts.Add(drop_proc_name, drop_proc_scripts[0]);
                }
            }

            var lookup_proc_script = ent.AsCreateLookupProc(create_or_alter);
            if (lookup_proc_script?.Length > 0) generated_scripts.Add(lookup_proc_name, lookup_proc_script);

            var view_proc_script = ent.AsCreateViewProc(create_or_alter);
            if (view_proc_script?.Length > 0) generated_scripts.Add(view_proc_name, view_proc_script);

            if (custom_procs != null)
            {
                var custom_proc_scripts = ClassifyCustomSQLScripts(custom_procs);

                // We don't use a foreach here as there may be update and iupdate / drop and idrop custom procs but the flag with_iupdate and with_idrop
                // can be false. Custom procs should alwayes be created no matter what.

                // update
                if (custom_proc_scripts.Contains(iupdate_proc_name))
                {
                    await ec.ExecuteSQLNonQuery(custom_proc_scripts[iupdate_proc_name].SQLText, ct);
                }
                else if (with_iupdate)
                {
                    await ec.ExecuteSQLNonQuery(generated_scripts[iupdate_proc_name], ct);
                }

                if (custom_proc_scripts.Contains(update_proc_name))
                {
                    await ec.ExecuteSQLNonQuery(custom_proc_scripts[update_proc_name].SQLText, ct);
                }
                else
                {
                    await ec.ExecuteSQLNonQuery(generated_scripts[update_proc_name], ct);
                }

                // get
                if (custom_proc_scripts.Contains(get_proc_name))
                {
                    await ec.ExecuteSQLNonQuery(custom_proc_scripts[get_proc_name].SQLText, ct);
                }
                else
                {
                    await ec.ExecuteSQLNonQuery(generated_scripts[get_proc_name], ct);
                }

                // drop
                if (custom_proc_scripts.Contains(idrop_proc_name))
                {
                    await ec.ExecuteSQLNonQuery(custom_proc_scripts[idrop_proc_name].SQLText, ct);
                }
                else if (with_idrop)
                {
                    await ec.ExecuteSQLNonQuery(generated_scripts[idrop_proc_name], ct);
                }

                if (custom_proc_scripts.Contains(drop_proc_name))
                {
                    await ec.ExecuteSQLNonQuery(custom_proc_scripts[drop_proc_name].SQLText, ct);
                }
                else
                {
                    await ec.ExecuteSQLNonQuery(generated_scripts[drop_proc_name], ct);
                }

                // lookup
                if (custom_proc_scripts.Contains(lookup_proc_name))
                {
                    await ec.ExecuteSQLNonQuery(custom_proc_scripts[lookup_proc_name].SQLText, ct);
                }
                else
                {
                    await ec.ExecuteSQLNonQuery(generated_scripts[lookup_proc_name], ct);
                }

                // view
                if (custom_proc_scripts.Contains(view_proc_name))
                {
                    await ec.ExecuteSQLNonQuery(custom_proc_scripts[view_proc_name].SQLText, ct);
                }
                else
                {
                    await ec.ExecuteSQLNonQuery(generated_scripts[view_proc_name], ct);
                }

                // create the rest of custom procs if requested
                if (create_custom_procs)
                {
                    foreach (var key in custom_proc_scripts.Keys)
                    {
                        if (!generated_scripts.Contains(key))
                        {
                            await ec.ExecuteSQLNonQuery(custom_proc_scripts[key].SQLText, ct);
                        }
                    }
                }

            }
            else
            {
                foreach (var script in generated_scripts.Values)
                {
                    await ec.ExecuteSQLNonQuery(script, ct);
                }
            }

        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

    }

    /// <summary>
    /// Creates the schema and procedures for an entity and returns the initialized instance.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="ent">Optional existing entity instance.</param>
    /// <param name="ec">Entity client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="create_or_alter">Indicates if objects should be created or altered.</param>
    /// <param name="create_custom_procs">Whether to include custom procedures.</param>
    /// <returns>The entity after creation.</returns>
    public static async Task<T> CreateEntityAndProcs<T>(
    T? ent,
    IEntityClient ec,
    CancellationToken ct,
    bool create_or_alter,
    bool create_custom_procs = true
    ) where T : EntityBase, new()
    {
        bool should_close = !(ec.ConnectionState == System.Data.ConnectionState.Open);
        try
        {
            T new_ent;

            if (ent == null)
            {
                new_ent = new();
                new_ent.Init(ec);
            }
            else
            {
                new_ent = ent;
            }

            await CreateProcs(new_ent, ec, ct, create_or_alter, create_custom_procs);

            return new_ent;
        }
        finally
        {
            if (should_close) await ec.Disconnect();
        }

    }



}
