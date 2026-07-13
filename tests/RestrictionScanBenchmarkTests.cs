using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace LimitByCraftingSkillMod.Tests
{
    /// <summary>
    /// Hermetic Stopwatch benchmarks for <see cref="RestrictionScan"/>. Manual Bazel target only — not CI-gated.
    /// </summary>
    public sealed class RestrictionScanBenchmarkTests
    {
        public static IEnumerable<object[]> StackCounts()
        {
            yield return new object[] { 8 };
            yield return new object[] { 45 };
            yield return new object[] { 90 };
            yield return new object[] { 180 };
        }

        [Theory]
        [MemberData(nameof(StackCounts))]
        public void RestrictionScan_Evaluate_Baseline(int stackCount)
        {
            RunBench(stackCount, useRequiredLevelMemo: false, label: "baseline");
        }

        [Theory]
        [MemberData(nameof(StackCounts))]
        public void RestrictionScan_Evaluate_WithCrossFrameMemo(int stackCount)
        {
            RestrictionScan.ClearCaches();
            try
            {
                RunBench(stackCount, useRequiredLevelMemo: true, label: "memo");
            }
            finally
            {
                RestrictionScan.ClearCaches();
            }
        }

        private static void RunBench(int stackCount, bool useRequiredLevelMemo, string label)
        {
            var map = ClassNameToCraftingSkillMapLoader.GetMap();
            Assert.True(map.Count > 0, "ClassNameToCraftingSkillMap must load for benchmarks");

            var keys = map.Keys.Take(Math.Max(stackCount, 32)).ToList();
            var progression = BuildRichProgression(keys, map);
            var stacks = BuildStacks(keys, stackCount);
            // Low player levels so a non-trivial fraction is restricted when progression resolves.
            Func<string, int> getLevel = _ => 1;

            // Warmup
            var warm = RestrictionScan.Evaluate(stacks, progression, getLevel, useRequiredLevelMemo);
            Assert.True(warm.Evaluated > 0);

            const int iterations = 200;
            var sw = Stopwatch.StartNew();
            RestrictionScan.Result last = default;
            for (int i = 0; i < iterations; i++)
                last = RestrictionScan.Evaluate(stacks, progression, getLevel, useRequiredLevelMemo);
            sw.Stop();

            double msPerScan = sw.Elapsed.TotalMilliseconds / iterations;
            double usPerItem = (msPerScan * 1000.0) / Math.Max(1, last.Evaluated);

            Console.WriteLine(
                $"RestrictionScan {label}: N={stackCount} evaluated={last.Evaluated} restricted={last.Restricted} " +
                $"ms/scan={msPerScan:F3} µs/item={usPerItem:F2} iterations={iterations}");

            // Smoke: at least some restricted with playerLevel=1 and non-trivial quality bands.
            Assert.True(last.Restricted > 0, "Expected some restricted items at playerLevel=1");
            Assert.Equal(stackCount, last.Evaluated);
        }

        private static List<ItemStack> BuildStacks(IReadOnlyList<string> keys, int stackCount)
        {
            var stacks = new List<ItemStack>(stackCount);
            for (int i = 0; i < stackCount; i++)
            {
                var name = keys[i % keys.Count];
                var itemClass = new ItemClass { Name = name };
                // Cycle qualities 1..6 so cache keys differ within a scan.
                int quality = (i % 6) + 1;
                var itemValue = new BenchItemValueWithQuality
                {
                    ItemClass = itemClass,
                    Quality = quality,
                    type = 1 + (i % 1000)
                };
                stacks.Add(new ItemStack(itemValue, 1));
            }
            return stacks;
        }

        private static FakeBenchProgression BuildRichProgression(
            IReadOnlyList<string> keys,
            IReadOnlyDictionary<string, string> map)
        {
            var byLookup = new Dictionary<string, ArrayList>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
            {
                if (!map.TryGetValue(key, out var skillGroup) || string.IsNullOrWhiteSpace(skillGroup))
                    continue;
                var lookup = GameReflection.ToProgressionLookupName(skillGroup);
                if (string.IsNullOrWhiteSpace(lookup))
                    continue;
                if (!byLookup.TryGetValue(lookup, out var list))
                {
                    list = new ArrayList();
                    byLookup[lookup] = list;
                }
                list.Add(new FakeDisplayData
                {
                    ItemName = key,
                    QualityStarts = new[] { 3, 17, 31, 55, 71, 88 }
                });
            }

            var pcs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in byLookup)
                pcs[kv.Key] = new FakeBenchPc { DisplayDataList = kv.Value };

            return new FakeBenchProgression(pcs);
        }

        private sealed class BenchItemValueWithQuality : ItemValue
        {
            public int Quality { get; set; }
            public bool HasQuality => Quality > 0;
        }

        private sealed class FakeBenchPc
        {
            public ArrayList DisplayDataList;
        }

        private sealed class FakeBenchProgression
        {
            private readonly Dictionary<string, object> _pcs;

            public FakeBenchProgression(Dictionary<string, object> pcs)
            {
                _pcs = pcs;
            }

            public object GetProgressionValue(string name)
            {
                if (string.IsNullOrEmpty(name)) return null;
                if (_pcs.TryGetValue(name, out var pc))
                    return new FakePv { ProgressionClass = pc };
                foreach (var kv in _pcs)
                {
                    if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                        return new FakePv { ProgressionClass = kv.Value };
                }
                return null;
            }
        }
    }
}
