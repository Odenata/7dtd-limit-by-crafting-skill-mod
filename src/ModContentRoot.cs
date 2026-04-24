using System;
using System.IO;
using System.Reflection;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Resolves the folder containing the mod DLL, Config.xml, and ClassNameToCraftingSkillMap.xml.
    /// Under Unity/Mono, <see cref="Assembly.Location"/> is often empty; the game passes <c>Mod.Path</c> in <see cref="IModApi.InitMod"/>.
    /// </summary>
    internal static class ModContentRoot
    {
        private static string _fromModApi;
        private static readonly object Sync = new object();

        internal static void ApplyFromModApi(object modInstance)
        {
            if (modInstance == null) return;
            string path = TryExtractModPath(modInstance);
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                path = Path.GetFullPath(path.Trim());
                if (!Directory.Exists(path)) return;

                lock (Sync)
                {
                    _fromModApi = path;
                }

                ClassNameToCraftingSkillMapLoader.InvalidateLoadedData();
                ModConfig.InvalidateReloadableInstance();
            }
            catch
            {
                // ignored
            }
        }

        private static string TryExtractModPath(object modInstance)
        {
            try
            {
                var t = modInstance.GetType();
                const BindingFlags inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                foreach (var name in new[] { "Path", "ModPath", "Directory", "DirectoryPath" })
                {
                    var prop = t.GetProperty(name, inst);
                    if (prop != null && prop.PropertyType == typeof(string))
                    {
                        var v = prop.GetValue(modInstance, null) as string;
                        if (!string.IsNullOrWhiteSpace(v)) return v;
                    }
                    var field = t.GetField(name, inst);
                    if (field != null && field.FieldType == typeof(string))
                    {
                        var v = field.GetValue(modInstance) as string;
                        if (!string.IsNullOrWhiteSpace(v)) return v;
                    }
                }

                foreach (var prop in t.GetProperties(inst))
                {
                    if (prop.PropertyType != typeof(string)) continue;
                    var pn = prop.Name;
                    if (pn.IndexOf("path", StringComparison.OrdinalIgnoreCase) < 0 &&
                        pn.IndexOf("directory", StringComparison.OrdinalIgnoreCase) < 0 &&
                        pn.IndexOf("folder", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    try
                    {
                        var v = prop.GetValue(modInstance, null) as string;
                        if (!string.IsNullOrWhiteSpace(v) && Directory.Exists(Path.GetFullPath(v.Trim()))) return v;
                    }
                    catch { }
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        internal static string ResolveModDirectory()
        {
            lock (Sync)
            {
                if (!string.IsNullOrEmpty(_fromModApi))
                    return _fromModApi;
            }

            try
            {
                var asm = typeof(ModContentRoot).Assembly;
                var loc = asm.Location;
                if (!string.IsNullOrEmpty(loc))
                {
                    var dir = Path.GetDirectoryName(loc);
                    if (!string.IsNullOrEmpty(dir))
                        return dir;
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}
