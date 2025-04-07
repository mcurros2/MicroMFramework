using System.Reflection;

namespace MicroM.Extensions
{
    public static class EmbeddedResourcesExtensions
    {
        public async static Task<List<string>> GetAllCustomProcs<T>(this T assembly_class, string? mneo, CancellationToken ct) where T : class
        {
            var assembly = typeof(T).Assembly;
            List<string> ret = new();
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && ((name.Contains($".{mneo}_") || name.StartsWith($"{mneo}_", StringComparison.OrdinalIgnoreCase)) || mneo == null))
                {
                    var manifest = assembly.GetManifestResourceStream(name);
                    if (manifest != null)
                    {
                        StreamReader reader = new(manifest);
                        ret.Add(await reader.ReadToEndAsync(ct));
                    }
                }
            }
            return ret;
        }

        public async static Task<List<string>> GetAssemblyCustomProcs(this Assembly assembly, string? mneo, string? starts_with, CancellationToken ct)
        {
            List<string> ret = new();
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (!name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;

                if (starts_with != null && !name.StartsWith(starts_with, StringComparison.OrdinalIgnoreCase) && !name.Contains($".{starts_with}", StringComparison.OrdinalIgnoreCase)) continue;

                if (mneo != null && !name.Contains($".{mneo}_") && !name.StartsWith($"{mneo}_", StringComparison.OrdinalIgnoreCase)) continue;

                var manifest = assembly.GetManifestResourceStream(name);
                if (manifest != null)
                {
                    StreamReader reader = new(manifest);
                    ret.Add(await reader.ReadToEndAsync(ct));
                }
            }
            return ret;
        }


    }
}
