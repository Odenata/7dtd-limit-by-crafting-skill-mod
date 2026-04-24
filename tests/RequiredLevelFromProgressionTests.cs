using System;
using System.Collections;
using Xunit;

namespace LimitByCraftingSkillMod.Tests
{
    /// <summary>
    /// Fake progression graph matching what <see cref="GameReflection"/> reads via reflection
    /// (GetProgressionValue → ProgressionClass.DisplayDataList → ItemName / QualityStarts).
    /// </summary>
    public sealed class FakeProgressionForRequiredLevel
    {
        public object GetProgressionValue(string name)
        {
            if (string.Equals(name, "craftingharvestingtools", System.StringComparison.OrdinalIgnoreCase))
                return new FakePv { ProgressionClass = new FakePcHarvesting() };
            if (string.Equals(name, "craftingworkstations", System.StringComparison.OrdinalIgnoreCase))
                return new FakePv { ProgressionClass = new FakePcWorkstations() };
            return null;
        }
    }

    public sealed class FakePv
    {
        public object ProgressionClass { get; set; }
    }

    public sealed class FakePcHarvesting
    {
        public ArrayList DisplayDataList = new ArrayList
        {
            new FakeDisplayData
            {
                ItemName = "meleeToolPickT1IronPickaxe",
                QualityStarts = new[] { 3, 17, 31, 55, 71, 88 }
            }
        };
    }

    public sealed class FakePcWorkstations
    {
        public ArrayList DisplayDataList = new ArrayList
        {
            new FakeDisplayData
            {
                ItemName = "workbench",
                QualityStarts = new[] { 2, 9, 18 }
            }
        };
    }

    public sealed class FakeDisplayData
    {
        public string ItemName;
        public int[] QualityStarts;
    }

    /// <summary>craftingelectrician tree with two rows: parent unlock row (tier 0) + specific display row (tier 30).</summary>
    public sealed class FakeProgressionPoweredGarageElectrician
    {
        public object GetProgressionValue(string name)
        {
            if (string.Equals(name, "craftingelectrician", System.StringComparison.OrdinalIgnoreCase))
                return new FakePv { ProgressionClass = new FakePcPoweredGarageElectrician() };
            return null;
        }
    }

    public sealed class FakePcPoweredGarageElectrician
    {
        public ArrayList DisplayDataList = new ArrayList
        {
            new FakeGarageDisplayData
            {
                ItemName = "ironGarageDoorParentRow",
                QualityStarts = new[] { 0 },
                UnlockItemNameForSlot0 = "ironGarageDoor01_PoweredWhite"
            },
            new FakeGarageDisplayData
            {
                ItemName = "ironGarageDoor01_PoweredWhite",
                QualityStarts = new[] { 30 }
            }
        };
    }

    public sealed class FakeGarageDisplayData
    {
        public string ItemName;
        public int[] QualityStarts;
        public string UnlockItemNameForSlot0;

        public ItemClass GetUnlockItem(int u)
        {
            if (u != 0 || string.IsNullOrEmpty(UnlockItemNameForSlot0)) return null;
            return new ItemClass { Name = UnlockItemNameForSlot0 };
        }
    }

    public class RequiredLevelFromProgressionTests
    {
        private static bool MapHasPickaxe =>
            GameReflection.GetCraftingSkillGroup(new ItemClass { Name = "meleeToolPickT1IronPickaxe" }) != null;

        [Fact]
        public void GetRequiredLevelForItemForUnitTest_HarvestingSyntheticTier1_UsesFirstQualityBand()
        {
            if (!MapHasPickaxe) return;
            var itemClass = new ItemClass { Name = "meleeToolPickT1IronPickaxe" };
            var itemValue = new ItemValue { ItemClass = itemClass };
            var prog = new FakeProgressionForRequiredLevel();
            var level = GameReflection.GetRequiredLevelForItemForUnitTest(itemClass, itemValue, prog);
            Assert.Equal(3, level);
        }

        [Fact]
        public void GetRequiredLevelForItemForUnitTest_WithQuality_UsesMatchingBand()
        {
            if (!MapHasPickaxe) return;
            var itemClass = new ItemClass { Name = "meleeToolPickT1IronPickaxe" };
            var itemValue = new TestItemValueWithQuality { ItemClass = itemClass, Quality = 3 };
            var prog = new FakeProgressionForRequiredLevel();
            var level = GameReflection.GetRequiredLevelForItemForUnitTest(itemClass, itemValue, prog);
            Assert.Equal(31, level);
        }

        [Fact]
        public void GetRequiredLevelForItemForUnitTest_WorkstationsSyntheticTier_MatchesDisplayData()
        {
            var itemClass = new ItemClass { Name = "workbench" };
            if (GameReflection.GetCraftingSkillGroup(itemClass) == null) return;
            var itemValue = new ItemValue { ItemClass = itemClass };
            Assert.Equal("Workstations", GameReflection.GetCraftingSkillGroup(itemClass));
            var prog = new FakeProgressionForRequiredLevel();
            var level = GameReflection.GetRequiredLevelForItemForUnitTest(itemClass, itemValue, prog);
            Assert.Equal(2, level);
        }

        [Fact]
        public void GetRequiredLevelForItemForUnitTest_NullProgression_ReturnsZero()
        {
            var itemClass = new ItemClass { Name = "meleeToolPickT1IronPickaxe" };
            var itemValue = new ItemValue { ItemClass = itemClass };
            Assert.Equal(0, GameReflection.GetRequiredLevelForItemForUnitTest(itemClass, itemValue, null));
        }

        [Fact]
        public void PoweredIronGarage_UsesMaxRequiredLevelAcrossMatchingDisplayDataRows()
        {
            var itemClass = new ItemClass { Name = "ironGarageDoor_PoweredWhite" };
            var prog = new FakeProgressionPoweredGarageElectrician();
            var level = GameReflection.TestHooks.TryResolveCraftingElectricianRequiredLevel(prog, itemClass, itemClass.Name, 1);
            Assert.Equal(30, level);
        }

        private sealed class TestItemValueWithQuality : ItemValue
        {
            public ushort Quality;
        }

        private sealed class FakeDisplayDataZerosPlusUnlockCsv
        {
            public int[] QualityStarts = new[] { 0, 0, 0 };
            public string unlock_level = "2,5,10";
        }

        [Fact]
        public void GetRequiredLevelFromDisplayData_QualityStartsZero_FallsBackToUnlockLevelCsv()
        {
            var dd = new FakeDisplayDataZerosPlusUnlockCsv();
            Assert.Equal(2, GameReflection.TestHooks.GetRequiredLevelFromDisplayDataForTests(dd, 1));
            Assert.Equal(5, GameReflection.TestHooks.GetRequiredLevelFromDisplayDataForTests(dd, 2));
            Assert.Equal(10, GameReflection.TestHooks.GetRequiredLevelFromDisplayDataForTests(dd, 3));
        }

        /// <summary>Matches vanilla <c>craftingSeeds</c> tier-1 <c>display_entry</c> unlock_level shape.</summary>
        private sealed class FakeDisplayDataVanillaSeedsTier1
        {
            public string unlock_level = "2,4,6,8,10";
        }

        [Fact]
        public void GetRequiredLevelFromDisplayData_VanillaSeedsUnlockCsv_UsesUnlockColumnNotSmallQualityHeuristic()
        {
            var dd = new FakeDisplayDataVanillaSeedsTier1();
            Assert.Equal(2, GameReflection.TestHooks.GetRequiredLevelFromDisplayDataForTests(dd, 1));
            Assert.Equal(10, GameReflection.TestHooks.GetRequiredLevelFromDisplayDataForTests(dd, 5));
        }

        private sealed class FakeUnlockEntry
        {
            public string ItemName;
            public int UnlockTier = 1;
        }

        private sealed class FakeCompositeExplosivesDisplayData
        {
            public string ItemName = "craftingExplosivesCompositeRow";
            public int[] QualityStarts = new[] { 0, 0, 0 };
            public string unlock_level = "5,12,16";
            public ArrayList UnlockDataList = new ArrayList
            {
                new FakeUnlockEntry { ItemName = "thrownGrenade", UnlockTier = 0 },
                new FakeUnlockEntry { ItemName = "thrownDynamite", UnlockTier = 1 },
                new FakeUnlockEntry { ItemName = "thrownGrenadeContact", UnlockTier = 2 },
            };
        }

        private sealed class FakePcExplosivesComposite
        {
            public ArrayList DisplayDataList = new ArrayList
            {
                new FakeCompositeExplosivesDisplayData()
            };
        }

        private sealed class FakeProgressionExplosivesComposite
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingexplosives", System.StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcExplosivesComposite() };
                return null;
            }
        }

        [Fact]
        public void CompositeExplosivesRow_ThreeUnlockTiers_UsesUnlockLevelCsvByStoredTier()
        {
            var prog = new FakeProgressionExplosivesComposite();
            var contact = new ItemClass { Name = "thrownGrenadeContact" };
            var baseGrenade = new ItemClass { Name = "thrownGrenade" };

            var lvlContact = GameReflection.TestHooks.TryResolveCraftingExplosivesRequiredLevel(prog, contact, contact.Name, 1);
            var lvlBase = GameReflection.TestHooks.TryResolveCraftingExplosivesRequiredLevel(prog, baseGrenade, baseGrenade.Name, 1);

            Assert.Equal(16, lvlContact);
            Assert.Equal(5, lvlBase);
        }

        /// <summary>Matches in-game <c>UnlockData.UnlockTier</c> after XML <c>unlock_tier</c> minus one.</summary>
        private sealed class FakeUnlockEntryGameTier
        {
            public string ItemName;
            public int UnlockTier;
        }

        /// <summary>
        /// Shape where <c>UnlockDataList.Count</c> is 1 but <c>QualityStarts</c> still has the full vanilla row — forces
        /// <see cref="GameReflection"/> through the single-child branch (same bug class as mis-counted lists).
        /// </summary>
        private sealed class FakeDisplayDataSeedsSingleChildList
        {
            public int[] QualityStarts = new[] { 2, 4, 6, 8, 10 };
            public ArrayList UnlockDataList = new ArrayList
            {
                new FakeUnlockEntryGameTier { ItemName = "plantedAloe1", UnlockTier = 4 },
            };
        }

        private sealed class FakePcSeedsSingleChildList
        {
            public ArrayList DisplayDataList = new ArrayList { new FakeDisplayDataSeedsSingleChildList() };
        }

        private sealed class FakeProgressionSeedsSingleChildList
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingseeds", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcSeedsSingleChildList() };
                return null;
            }
        }

        [Fact]
        public void Seeds_SingleUnlockListEntry_GameStyleZeroBasedUnlockTier_UsesFifthQualityStartsBand()
        {
            var prog = new FakeProgressionSeedsSingleChildList();
            var aloe = new ItemClass { Name = "plantedAloe1" };
            var iv = new ItemValue { ItemClass = aloe };
            var level = GameReflection.GetRequiredLevelForItemForUnitTest(aloe, iv, prog);
            Assert.Equal(10, level);
        }

        [Fact]
        public void ResolveTierForUnlockChild_SiblingCountOne_MapsGameZeroBasedUnlockTierToOneBasedColumn()
        {
            var aloeUd = new FakeUnlockEntryGameTier { UnlockTier = 4 };
            Assert.Equal(5, GameReflection.TestHooks.ResolveTierForUnlockChildForTests(1, aloeUd, 0));
            var cottonUd = new FakeUnlockEntryGameTier { UnlockTier = 0 };
            Assert.Equal(1, GameReflection.TestHooks.ResolveTierForUnlockChildForTests(1, cottonUd, 0));
        }

        /// <summary>Same stored <c>UnlockTier</c> on every child → positional columns (explosives-style tests).</summary>
        private sealed class FakeDisplayThreeDuplicateTiers
        {
            public ArrayList UnlockDataList = new ArrayList
            {
                new FakeUnlockEntryGameTier { UnlockTier = 1 },
                new FakeUnlockEntryGameTier { UnlockTier = 1 },
                new FakeUnlockEntryGameTier { UnlockTier = 1 },
            };
        }

        [Fact]
        public void ResolveTierForUnlockChild_MultiSiblingSameUnlockTier_UsesStoredColumnForEachIndex()
        {
            var dd = new FakeDisplayThreeDuplicateTiers();
            var a = (FakeUnlockEntryGameTier)dd.UnlockDataList[0];
            var b = (FakeUnlockEntryGameTier)dd.UnlockDataList[1];
            var c = (FakeUnlockEntryGameTier)dd.UnlockDataList[2];
            // 0-based UnlockTier 1 => column 2; matches vanilla rows where the engine stores the same band for
            // expanded siblings, not 1,2,3 from list order.
            Assert.Equal(2, GameReflection.TestHooks.ResolveTierForUnlockChildForTests(dd, 3, a, 0));
            Assert.Equal(2, GameReflection.TestHooks.ResolveTierForUnlockChildForTests(dd, 3, b, 1));
            Assert.Equal(2, GameReflection.TestHooks.ResolveTierForUnlockChildForTests(dd, 3, c, 2));
        }

        /// <summary>Simulates game list order differing from <c>unlock_level</c> column order while <c>UnlockTier</c> stays correct.</summary>
        private sealed class FakeDisplayFoodTier1DistinctShuffled
        {
            public int[] QualityStarts = new[] { 2, 4, 6, 8, 10 };
            public ArrayList UnlockDataList = new ArrayList
            {
                new FakeUnlockEntryGameTier { ItemName = "foodCornOnTheCob", UnlockTier = 0 },
                new FakeUnlockEntryGameTier { ItemName = "foodGrilledMeat", UnlockTier = 2 },
                new FakeUnlockEntryGameTier { ItemName = "foodCornBread", UnlockTier = 1 },
                new FakeUnlockEntryGameTier { ItemName = "foodBoiledMeat", UnlockTier = 3 },
                new FakeUnlockEntryGameTier { ItemName = "foodBaconAndEggs", UnlockTier = 4 },
            };
        }

        [Fact]
        public void ResolveTierForUnlockChild_MultiSiblingDistinctShuffledTiers_UsesStoredUnlockTier()
        {
            var dd = new FakeDisplayFoodTier1DistinctShuffled();
            var cornUd = (FakeUnlockEntryGameTier)dd.UnlockDataList[2];
            Assert.Equal(2, GameReflection.TestHooks.ResolveTierForUnlockChildForTests(dd, 5, cornUd, 2));
        }

        /// <summary>Game reflection often boxes <c>UnlockTier</c> as <c>short</c>/<c>byte</c>, not <c>int</c>.</summary>
        private sealed class FakeUnlockEntryShortGameTier
        {
            public string ItemName;
            public short UnlockTier;
        }

        private sealed class FakeDisplayFoodShortShuffled
        {
            public int[] QualityStarts = new[] { 2, 4, 6, 8, 10 };
            public ArrayList UnlockDataList = new ArrayList
            {
                new FakeUnlockEntryShortGameTier { ItemName = "foodCornOnTheCob", UnlockTier = 0 },
                new FakeUnlockEntryShortGameTier { ItemName = "foodGrilledMeat", UnlockTier = 2 },
                new FakeUnlockEntryShortGameTier { ItemName = "foodCornBread", UnlockTier = 1 },
                new FakeUnlockEntryShortGameTier { ItemName = "foodBoiledMeat", UnlockTier = 3 },
                new FakeUnlockEntryShortGameTier { ItemName = "foodBaconAndEggs", UnlockTier = 4 },
            };
        }

        [Fact]
        public void ResolveTierForUnlockChild_MultiSiblingDistinctShuffledShortTiers_CoercesToStoredColumn()
        {
            var dd = new FakeDisplayFoodShortShuffled();
            var cornUd = (FakeUnlockEntryShortGameTier)dd.UnlockDataList[2];
            Assert.Equal(2, GameReflection.TestHooks.ResolveTierForUnlockChildForTests(dd, 5, cornUd, 2));
        }

        private sealed class FakePcFoodShuffle
        {
            public ArrayList DisplayDataList = new ArrayList { new FakeDisplayFoodTier1DistinctShuffled() };
        }

        private sealed class FakeProgressionFoodShuffle
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingfood", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcFoodShuffle() };
                return null;
            }
        }

        [Fact]
        public void Food_ShuffledUnlockList_ResolvesRequiredLevelFromUnlockTierNotListIndex()
        {
            var prog = new FakeProgressionFoodShuffle();
            var cornBread = new ItemClass { Name = "foodCornBread" };
            var iv = new ItemValue { ItemClass = cornBread };
            Assert.Equal(4, GameReflection.GetRequiredLevelForItemForUnitTest(cornBread, iv, prog));
        }

        /// <summary>Vanilla T1-1 <c>progression.xml</c>: one <c>unlock_entry</c> with <c>item="foodCornBread,drinkJarGoldenRodTea"</c>.</summary>
        private sealed class FakeDisplayFoodVanillaCommaInOneEntry
        {
            public int[] QualityStarts = new[] { 2, 4, 6, 8, 10 };
            public ArrayList UnlockDataList = new ArrayList
            {
                new FakeUnlockEntryGameTier { ItemName = "foodCornOnTheCob,foodBakedPotato", UnlockTier = 0 },
                new FakeUnlockEntryGameTier { ItemName = "foodCornBread,drinkJarGoldenRodTea", UnlockTier = 1 },
                new FakeUnlockEntryGameTier { ItemName = "foodGrilledMeat,drinkJarRedTea", UnlockTier = 2 },
            };
        }

        private sealed class FakePcFoodVanillaComma
        {
            public ArrayList DisplayDataList = new ArrayList { new FakeDisplayFoodVanillaCommaInOneEntry() };
        }

        private sealed class FakeProgressionFoodVanillaComma
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingfood", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcFoodVanillaComma() };
                return null;
            }
        }

        [Fact]
        public void Food_CommaSeparatedItemInSingleUnlock_EntryMatchesByToken_ResolvesBand4()
        {
            var prog = new FakeProgressionFoodVanillaComma();
            var cornBread = new ItemClass { Name = "foodCornBread" };
            var tea = new ItemClass { Name = "drinkJarGoldenRodTea" };
            var iv1 = new ItemValue { ItemClass = cornBread };
            var iv2 = new ItemValue { ItemClass = tea };
            Assert.Equal(4, GameReflection.GetRequiredLevelForItemForUnitTest(cornBread, iv1, prog));
            Assert.Equal(4, GameReflection.GetRequiredLevelForItemForUnitTest(tea, iv2, prog));
        }
    }
}
