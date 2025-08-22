using System.Reflection;

namespace MicroM.Extensions;

public static class EmbeddedResourcesExtensions
{
    /// <summary>
    /// Retrieves all embedded SQL procedure scripts for the specified assembly type.
    /// </summary>
    /// <typeparam name="T">Type whose assembly is scanned.</typeparam>
    /// <param name="assembly_class">Instance used to obtain the assembly.</param>
    /// <param name="mneo">Optional mneo filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of SQL script contents.</returns>
    public async static Task<List<string>> GetAllCustomProcs<T>(this T assembly_class, string? mneo, CancellationToken ct) where T : class
    {
        var assembly = typeof(T).Assembly;
        List<string> ret = [];
        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && ((name.Contains($".{mneo}_") || name.StartsWith($"{mneo}_", StringComparison.OrdinalIgnoreCase)) || mneo == null))
            {
                using var manifest = assembly.GetManifestResourceStream(name);
                if (manifest != null)
                {
                    using StreamReader reader = new(manifest);
                    ret.Add(await reader.ReadToEndAsync(ct));
                    reader.Close();
                }
            }
        }
        return ret;
    }

    /// <summary>
    /// Retrieves embedded SQL procedure scripts from the given assembly.
    /// </summary>
    /// <param name="assembly">Assembly to inspect.</param>
    /// <param name="mneo">Optional mneo filter.</param>
    /// <param name="starts_with">Optional prefix filter for resource names.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of SQL script contents.</returns>
    public async static Task<List<string>> GetAssemblyCustomProcs(this Assembly assembly, string? mneo, string? starts_with, CancellationToken ct)
    {
        List<string> ret = [];
        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (!name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;

            if (starts_with != null && !name.StartsWith(starts_with, StringComparison.OrdinalIgnoreCase) && !name.Contains($".{starts_with}", StringComparison.OrdinalIgnoreCase)) continue;

            if (mneo != null && !name.Contains($".{mneo}_") && !name.StartsWith($"{mneo}_", StringComparison.OrdinalIgnoreCase)) continue;

            using var manifest = assembly.GetManifestResourceStream(name);
            if (manifest != null)
            {
                using StreamReader reader = new(manifest);
                ret.Add(await reader.ReadToEndAsync(ct));
                reader.Close();
            }
        }
        return ret;
    }


}
