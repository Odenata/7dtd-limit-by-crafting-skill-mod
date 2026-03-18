using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Xunit;

namespace LimitByCraftingSkillMod.Tests
{
    /// <summary>
    /// Set PROGRESSION_XML_PATH or 7DTD_PROGRESSION_XML to game Data/Config/progression.xml to validate
    /// map entries against unlock lists. Set ASSEMBLY_CSHARP_DLL to Managed/Assembly-CSharp.dll for schema smoke.
    /// These tests no-op when env vars are unset (nothing is checked in from decompiled sources).
    /// </summary>
    public sealed class ProgressionAndAssemblyIntegrationTests
    {
        private static string ProgressionPath =>
            Environment.GetEnvironmentVariable("PROGRESSION_XML_PATH")
            ?? Environment.GetEnvironmentVariable("7DTD_PROGRESSION_XML");

        private static string AssemblyCSharpPath =>
            Environment.GetEnvironmentVariable("ASSEMBLY_CSHARP_DLL");

        private static readonly Dictionary<string, string[]> GroupToSkillKeys = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Blades"] = new[] { "craftingBlades" },
            ["Bows"] = new[] { "craftingBows" },
            ["Clubs"] = new[] { "craftingClubs" },
            ["Clothing"] = new[] { "craftingArmor" },
            ["Electrician"] = new[] { "craftingElectrician", "craftingWorkstations" },
            ["Explosives"] = new[] { "craftingExplosives" },
            ["Food"] = new[] { "craftingFood" },
            ["Handguns"] = new[] { "craftingHandguns" },
            ["HarvestingTools"] = new[] { "craftingHarvestingTools" },
            ["Knuckles"] = new[] { "craftingKnuckles" },
            ["MachineGuns"] = new[] { "craftingMachineGuns" },
            ["Medical"] = new[] { "craftingMedical" },
            ["RepairTools"] = new[] { "craftingRepairTools" },
            ["Rifles"] = new[] { "craftingRifles" },
            ["Robotics"] = new[] { "craftingRobotics" },
            ["SalvageTools"] = new[] { "craftingSalvageTools" },
            ["Seeds"] = new[] { "craftingSeeds" },
            ["Shotguns"] = new[] { "craftingShotguns" },
            ["Sledgehammers"] = new[] { "craftingSledgehammers" },
            ["Spears"] = new[] { "craftingSpears" },
            ["Tools"] = new[] { "craftingHarvestingTools" },
            ["Traps"] = new[] { "craftingTraps" },
            ["Vehicles"] = new[] { "craftingVehicles" },
            ["Workstations"] = new[] { "craftingWorkstations" },
            ["Ammo"] = new[] { "craftingHandguns", "craftingShotguns", "craftingRifles", "craftingMachineGuns", "craftingBows", "craftingExplosives" },
            ["Weapons"] = new[] { "craftingHandguns", "craftingShotguns", "craftingRifles", "craftingMachineGuns", "craftingBows", "craftingExplosives" },
        };

        [Fact]
        public void ProgressionXml_MapEntriesResolveAgainstUnlocks_WhenEnvSet()
        {
            var prog = ProgressionPath;
            if (string.IsNullOrWhiteSpace(prog) || !File.Exists(prog))
                return;

            var mapPath = ResolveClassNameMapPath();
            if (string.IsNullOrEmpty(mapPath) || !File.Exists(mapPath))
                return;

            var bySkill = LoadProgressionUnlockIndex(prog);
            var doc = new XmlDocument();
            doc.Load(mapPath);
            var failures = new List<string>();
            foreach (XmlElement item in doc.GetElementsByTagName("Item"))
            {
                var cn = item.GetAttribute("className");
                var grp = item.GetAttribute("craftingSkillGroup");
                var pm = item.GetAttribute("progressionMatchName");
                if (string.IsNullOrEmpty(cn) || string.IsNullOrEmpty(grp)) continue;
                if (!GroupToSkillKeys.TryGetValue(grp, out var keys)) continue;

                var union = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var k in keys)
                {
                    if (bySkill.TryGetValue(k, out var set))
                        foreach (var s in set) union.Add(s);
                }

                if (ResolvesAgainstUnion(cn, pm, union)) continue;
                failures.Add($"{cn} ({grp}) effective={pm ?? cn}");
            }

            Assert.True(failures.Count == 0,
                "Map entries not found in progression unlocks (set progressionMatchName or fix map): " +
                string.Join("; ", failures.GetRange(0, Math.Min(20, failures.Count))) +
                (failures.Count > 20 ? $" ... +{failures.Count - 20} more" : ""));
        }

        private static bool ResolvesAgainstUnion(string className, string progressionMatch, HashSet<string> union)
        {
            if (union.Contains(className)) return true;
            if (!string.IsNullOrEmpty(progressionMatch) && union.Contains(progressionMatch)) return true;

            if (className.StartsWith("ironGarageDoor_", StringComparison.OrdinalIgnoreCase) &&
                !className.StartsWith("ironGarageDoor01_", StringComparison.OrdinalIgnoreCase))
            {
                var t = "ironGarageDoor01_" + className.Substring("ironGarageDoor_".Length);
                if (union.Contains(t)) return true;
            }

            if (className.StartsWith("woodenGarageDoor3x3_", StringComparison.OrdinalIgnoreCase) &&
                className.IndexOf("woodenGarageDoor01_3x3_", StringComparison.OrdinalIgnoreCase) < 0)
            {
                var t = "woodenGarageDoor01_3x3_" + className.Substring("woodenGarageDoor3x3_".Length);
                if (union.Contains(t)) return true;
            }

            // Powered iron garage: progression uses helpers; generator may suggest ironGarageDoor01_* (in-game parity).
            if (className.StartsWith("ironGarageDoor_Powered", StringComparison.OrdinalIgnoreCase) &&
                className.IndexOf("BlockVariantHelper", StringComparison.OrdinalIgnoreCase) < 0)
            {
                var t = "ironGarageDoor01_" + className.Substring("ironGarageDoor_".Length);
                if (!string.IsNullOrEmpty(progressionMatch) &&
                    string.Equals(progressionMatch, t, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static Dictionary<string, HashSet<string>> LoadProgressionUnlockIndex(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);
            var index = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlElement cs in doc.GetElementsByTagName("crafting_skill"))
            {
                var name = cs.GetAttribute("name");
                if (string.IsNullOrEmpty(name)) continue;
                if (!index.TryGetValue(name, out var acc))
                {
                    acc = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    index[name] = acc;
                }
                foreach (XmlElement sub in cs.GetElementsByTagName("display_entry"))
                    AddCsv(acc, sub.GetAttribute("item"));
                foreach (XmlElement sub in cs.GetElementsByTagName("unlock_entry"))
                    AddCsv(acc, sub.GetAttribute("item"));
            }
            return index;
        }

        private static void AddCsv(HashSet<string> acc, string raw)
        {
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var part in raw.Split(','))
            {
                var s = part.Trim();
                if (s.Length > 0) acc.Add(s);
            }
        }

        private static string ResolveClassNameMapPath()
        {
            var e = Environment.GetEnvironmentVariable("CLASSNAME_MAP_XML");
            if (!string.IsNullOrEmpty(e) && File.Exists(e)) return e;
            var rd = Environment.GetEnvironmentVariable("TEST_SRCDIR");
            if (!string.IsNullOrEmpty(rd))
            {
                var p = Path.Combine(rd, "_main", "src", "ClassNameToCraftingSkillMap.xml");
                if (File.Exists(p)) return p;
            }
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (var i = 0; i < 12 && dir != null; i++, dir = dir.Parent)
            {
                var p = Path.Combine(dir.FullName, "src", "ClassNameToCraftingSkillMap.xml");
                if (File.Exists(p)) return p;
            }
            return null;
        }

        [Fact]
        public void AssemblyCSharp_HasProgressionClassDisplayData_WhenDllPathSet()
        {
            var dll = AssemblyCSharpPath;
            if (string.IsNullOrWhiteSpace(dll) || !File.Exists(dll))
                return;

            var asm = Assembly.LoadFrom(dll);
            var pc = asm.GetType("ProgressionClass");
            Assert.NotNull(pc);
            var f = pc.GetField("DisplayDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(f);
            var nested = pc.GetNestedType("DisplayData", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(nested);
        }
    }
}
