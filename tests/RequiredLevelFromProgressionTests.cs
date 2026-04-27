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

    public sealed class FakePcHarvestingAxeAndShovel
    {
        public ArrayList DisplayDataList = new ArrayList
        {
            new FakeDisplayData
            {
                ItemName = "meleeToolAxeT1IronFireaxe",
                QualityStarts = new[] { 3, 17, 31, 55, 71, 88 }
            },
            new FakeDisplayData
            {
                ItemName = "meleeToolShovelT1IronShovel",
                QualityStarts = new[] { 3, 17, 31, 55, 71, 88 }
            }
        };
    }

    public sealed class FakeProgressionForStoneHarvestingOverride
    {
        public object GetProgressionValue(string name)
        {
            if (string.Equals(name, "craftingharvestingtools", System.StringComparison.OrdinalIgnoreCase))
                return new FakePv { ProgressionClass = new FakePcHarvestingAxeAndShovel() };
            return null;
        }
    }

    public sealed class FakePcHarvestingStoneRowsPlusIronRows
    {
        public ArrayList DisplayDataList = new ArrayList
        {
            new FakeDisplayData
            {
                ItemName = "meleeToolRepairT0StoneAxe",
                QualityStarts = new[] { 1, 1, 1, 1, 1, 1 }
            },
            new FakeDisplayData
            {
                ItemName = "meleeToolShovelT0StoneShovel",
                QualityStarts = new[] { 1, 1, 1, 1, 1, 1 }
            },
            new FakeDisplayData
            {
                ItemName = "meleeToolAxeT1IronFireaxe",
                QualityStarts = new[] { 3, 17, 31, 55, 71, 88 }
            },
            new FakeDisplayData
            {
                ItemName = "meleeToolShovelT1IronShovel",
                QualityStarts = new[] { 3, 17, 31, 55, 71, 88 }
            }
        };
    }

    public sealed class FakeProgressionStoneRowsPlusIronRows
    {
        public object GetProgressionValue(string name)
        {
            if (string.Equals(name, "craftingharvestingtools", System.StringComparison.OrdinalIgnoreCase))
                return new FakePv { ProgressionClass = new FakePcHarvestingStoneRowsPlusIronRows() };
            return null;
        }
    }

    /// <summary>Mirrors vanilla craftingHarvestingTools stone unlock_level 1,2,4,6,8,10 vs iron 11,13,...</summary>
    public sealed class FakePcHarvestingVanillaLikeStoneAndIronBands
    {
        public ArrayList DisplayDataList = new ArrayList
        {
            new FakeDisplayData
            {
                ItemName = "meleeToolShovelT0StoneShovel",
                QualityStarts = new[] { 1, 2, 4, 6, 8, 10 }
            },
            new FakeDisplayData
            {
                ItemName = "meleeToolRepairT0StoneAxe",
                QualityStarts = new[] { 1, 2, 4, 6, 8, 10 }
            },
            new FakeDisplayData
            {
                ItemName = "meleeToolShovelT1IronShovel",
                QualityStarts = new[] { 11, 13, 16, 19, 22, 25 }
            },
            new FakeDisplayData
            {
                ItemName = "meleeToolAxeT1IronFireaxe",
                QualityStarts = new[] { 11, 13, 16, 19, 22, 25 }
            }
        };
    }

    public sealed class FakeProgressionVanillaLikeStoneIronHarvesting
    {
        public object GetProgressionValue(string name)
        {
            if (string.Equals(name, "craftingharvestingtools", System.StringComparison.OrdinalIgnoreCase))
                return new FakePv { ProgressionClass = new FakePcHarvestingVanillaLikeStoneAndIronBands() };
            return null;
        }
    }

    public sealed class FakeUnlockEntryStoneHarvestChild
    {
        public string ItemName;
        public int UnlockTier;
    }

    /// <summary>Vanilla stone <c>display_entry</c> shape: two <c>unlock_entry</c> siblings sharing <c>unlock_tier</c> (stored 0).</summary>
    public sealed class FakeDisplayStoneHarvestTwoUnlockSameTier
    {
        public string unlock_level = "1,2,4,6,8,10";
        public ArrayList UnlockDataList = new ArrayList
        {
            new FakeUnlockEntryStoneHarvestChild { ItemName = "meleeToolRepairT0StoneAxe", UnlockTier = 0 },
            new FakeUnlockEntryStoneHarvestChild { ItemName = "meleeToolShovelT0StoneShovel", UnlockTier = 0 },
        };
    }

    public sealed class FakePcHarvestingStoneTwoUnlockChildren
    {
        public ArrayList DisplayDataList = new ArrayList { new FakeDisplayStoneHarvestTwoUnlockSameTier() };
    }

    public sealed class FakeProgressionStoneRowTwoUnlockChildren
    {
        public object GetProgressionValue(string name)
        {
            if (string.Equals(name, "craftingharvestingtools", System.StringComparison.OrdinalIgnoreCase))
                return new FakePv { ProgressionClass = new FakePcHarvestingStoneTwoUnlockChildren() };
            return null;
        }
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

        private static void AssertMapHasSkill(string itemName, string expectedSkill)
        {
            Assert.Equal(expectedSkill, GameReflection.GetCraftingSkillGroup(new ItemClass { Name = itemName }));
        }

        [Fact]
        public void GetRequiredLevelForItemForUnitTest_HarvestingSyntheticTier1_UsesFirstQualityBand()
        {
            Assert.True(MapHasPickaxe, "Embedded ClassNameToCraftingSkillMap.xml should map iron pickaxe in tests.");
            var itemClass = new ItemClass { Name = "meleeToolPickT1IronPickaxe" };
            var itemValue = new ItemValue { ItemClass = itemClass };
            var prog = new FakeProgressionForRequiredLevel();
            var level = GameReflection.GetRequiredLevelForItemForUnitTest(itemClass, itemValue, prog);
            Assert.Equal(3, level);
        }

        [Fact]
        public void GetRequiredLevelForItemForUnitTest_WithQuality_UsesMatchingBand()
        {
            Assert.True(MapHasPickaxe, "Embedded ClassNameToCraftingSkillMap.xml should map iron pickaxe in tests.");
            var itemClass = new ItemClass { Name = "meleeToolPickT1IronPickaxe" };
            var itemValue = new TestItemValueWithQuality { ItemClass = itemClass, Quality = 3 };
            var prog = new FakeProgressionForRequiredLevel();
            var level = GameReflection.GetRequiredLevelForItemForUnitTest(itemClass, itemValue, prog);
            Assert.Equal(31, level);
        }

        [Fact]
        public void GetRequiredLevelForItemForUnitTest_StoneAxeAndShovelQuality_UseProgressionMatchOverrideBands()
        {
            AssertMapHasSkill("meleeToolAxeT0StoneAxe", "HarvestingTools");

            var prog = new FakeProgressionForStoneHarvestingOverride();

            var stoneAxe = new ItemClass { Name = "meleeToolAxeT0StoneAxe" };
            var axeValue = new TestItemValueWithQuality { ItemClass = stoneAxe, Quality = 3 };
            Assert.Equal(31, GameReflection.GetRequiredLevelForItemForUnitTest(stoneAxe, axeValue, prog));

            var stoneShovel = new ItemClass { Name = "meleeToolShovelT0StoneShovel" };
            var shovelValue = new TestItemValueWithQuality { ItemClass = stoneShovel, Quality = 3 };
            Assert.Equal(31, GameReflection.GetRequiredLevelForItemForUnitTest(stoneShovel, shovelValue, prog));
        }

        [Fact]
        public void GetRequiredLevelForItemForUnitTest_StoneRowsLockedToOne_QualityUsesIronProxyBands()
        {
            AssertMapHasSkill("meleeToolRepairT0StoneAxe", "HarvestingTools");

            var prog = new FakeProgressionStoneRowsPlusIronRows();

            var stoneRepairAxe = new ItemClass { Name = "meleeToolRepairT0StoneAxe" };
            var axeValue = new TestItemValueWithQuality { ItemClass = stoneRepairAxe, Quality = 3 };
            Assert.Equal(31, GameReflection.GetRequiredLevelForItemForUnitTest(stoneRepairAxe, axeValue, prog));

            var stoneShovel = new ItemClass { Name = "meleeToolShovelT0StoneShovel" };
            var shovelValue = new TestItemValueWithQuality { ItemClass = stoneShovel, Quality = 3 };
            Assert.Equal(31, GameReflection.GetRequiredLevelForItemForUnitTest(stoneShovel, shovelValue, prog));
        }

        [Fact]
        public void GetRequiredLevelForItemForUnitTest_StoneShovelAndAxe_Q2UsesStoneHarvestBandsNotIronFromProgressionMatchName()
        {
            AssertMapHasSkill("meleeToolShovelT0StoneShovel", "HarvestingTools");

            var prog = new FakeProgressionVanillaLikeStoneIronHarvesting();
            var shovel = new ItemClass { Name = "meleeToolShovelT0StoneShovel" };
            var shovelQ2 = new TestItemValueWithQuality { ItemClass = shovel, Quality = 2 };
            Assert.Equal(2, GameReflection.GetRequiredLevelForItemForUnitTest(shovel, shovelQ2, prog));

            var stoneAxe = new ItemClass { Name = "meleeToolAxeT0StoneAxe" };
            var axeQ2 = new TestItemValueWithQuality { ItemClass = stoneAxe, Quality = 2 };
            Assert.Equal(2, GameReflection.GetRequiredLevelForItemForUnitTest(stoneAxe, axeQ2, prog));
        }

        [Fact]
        public void HarvestingStoneRow_TwoUnlockChildrenSameStoredTier_IndexesUnlockLevelByItemQuality()
        {
            AssertMapHasSkill("meleeToolShovelT0StoneShovel", "HarvestingTools");

            var prog = new FakeProgressionStoneRowTwoUnlockChildren();
            var shovel = new ItemClass { Name = "meleeToolShovelT0StoneShovel" };
            Assert.Equal(4, GameReflection.GetRequiredLevelForItemForUnitTest(shovel, new TestItemValueWithQuality { ItemClass = shovel, Quality = 3 }, prog));
            Assert.Equal(10, GameReflection.GetRequiredLevelForItemForUnitTest(shovel, new TestItemValueWithQuality { ItemClass = shovel, Quality = 6 }, prog));
        }

        [Fact]
        public void GetRequiredLevelForItemForUnitTest_WorkstationsSyntheticTier_MatchesDisplayData()
        {
            var itemClass = new ItemClass { Name = "workbench" };
            var itemValue = new ItemValue { ItemClass = itemClass };
            Assert.Equal("Workstations", GameReflection.GetCraftingSkillGroup(itemClass));
            var prog = new FakeProgressionForRequiredLevel();
            var level = GameReflection.GetRequiredLevelForItemForUnitTest(itemClass, itemValue, prog);
            // ClassNameToCraftingSkillMap.xml requiredLevelOverride="10" for workbench must floor above fake progression tier-1 (2).
            Assert.Equal(10, level);
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

        /// <summary>Vanilla craftingVehicles: icon is the placeable; unlock lists chassis parts only.</summary>
        private sealed class FakeDisplayVehicleTruckRow
        {
            public string Icon = "vehicleTruck4x4Placeable";
            public string unlock_level = "70";
            public ArrayList UnlockDataList = new ArrayList
            {
                new FakeUnlockEntryGameTier { ItemName = "vehicleTruck4x4Chassis,vehicleTruck4x4Accessories", UnlockTier = 0 },
            };
        }

        private sealed class FakePcVehiclesTruck
        {
            public ArrayList DisplayDataList = new ArrayList { new FakeDisplayVehicleTruckRow() };
        }

        private sealed class FakeProgressionMapKeyVehicles
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingvehicles", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcVehiclesTruck() };
                return null;
            }
        }

        [Fact]
        public void MapKeyOnly_Vehicles_PlaceableIdMatchesProgressionIcon_ResolvesUnlockLevel()
        {
            var prog = new FakeProgressionMapKeyVehicles();
            var lvl = GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Vehicles", "vehicleTruck4x4Placeable", 1);
            Assert.Equal(70, lvl);
        }

        /// <summary>Stable builds: multiple unlock children under one vehicle icon row — must not skip icon-based resolution.</summary>
        private sealed class FakeDisplayVehicleMinibikeMultiUnlock
        {
            public string Icon = "vehicleMinibikePlaceable";
            public string unlock_level = "25";
            public ArrayList UnlockDataList = new ArrayList
            {
                new FakeUnlockEntryGameTier { ItemName = "vehicleMinibikeChassis", UnlockTier = 0 },
                new FakeUnlockEntryGameTier { ItemName = "vehicleMinibikeHandlebars", UnlockTier = 1 },
                new FakeUnlockEntryGameTier { ItemName = "vehicleMinibikeAccessories", UnlockTier = 2 },
            };
        }

        private sealed class FakePcVehiclesMinibikeMulti
        {
            public ArrayList DisplayDataList = new ArrayList { new FakeDisplayVehicleMinibikeMultiUnlock() };
        }

        private sealed class FakeProgressionMapKeyVehiclesMinibikeMulti
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingvehicles", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcVehiclesMinibikeMulti() };
                return null;
            }
        }

        [Fact]
        public void MapKeyOnly_Vehicles_MultiPartUnlockChildren_IconRowStillResolves()
        {
            var prog = new FakeProgressionMapKeyVehiclesMinibikeMulti();
            var lvl = GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Vehicles", "vehicleMinibikePlaceable", 1);
            Assert.Equal(25, lvl);
        }

        private sealed class FakeDisplayVehicleBicycleCustomIcon
        {
            public string[] CustomIcon = { "vehicleBicyclePlaceable" };
            public string unlock_level = "5";
        }

        private sealed class FakePcVehiclesBicycleCustomIcon
        {
            public ArrayList DisplayDataList = new ArrayList { new FakeDisplayVehicleBicycleCustomIcon() };
        }

        private sealed class FakeProgressionMapKeyVehiclesBicycleCustomIcon
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingvehicles", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcVehiclesBicycleCustomIcon() };
                return null;
            }
        }

        [Fact]
        public void MapKeyOnly_Vehicles_CustomIconStringArray_ResolvesUnlockLevel()
        {
            var prog = new FakeProgressionMapKeyVehiclesBicycleCustomIcon();
            var lvl = GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Vehicles", "vehicleBicyclePlaceable", 1);
            Assert.Equal(5, lvl);
        }

        private sealed class FakeDisplayWorkstationsForgeIconOnly
        {
            public string Icon = "forge";
            public string unlock_level = "4";
        }

        private sealed class FakePcWorkstationsForgeIconOnly
        {
            public ArrayList DisplayDataList = new ArrayList { new FakeDisplayWorkstationsForgeIconOnly() };
        }

        private sealed class FakeProgressionMapKeyWorkstationsForgeIcon
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingworkstations", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcWorkstationsForgeIconOnly() };
                return null;
            }
        }

        [Fact]
        public void MapKeyOnly_Workstations_IconOnlyRow_ResolvesUnlockLevel()
        {
            var prog = new FakeProgressionMapKeyWorkstationsForgeIcon();
            var lvl = GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Workstations", "forge", 1);
            Assert.Equal(4, lvl);
        }

        /// <summary>Stable craftingWorkstations: display_entry ItemName is often a tool id (toolForge), block id is forge.</summary>
        private sealed class FakeDisplayWorkstationsLooseForgeRow
        {
            public string ItemName = "toolForge";
            public string unlock_level = "4";
        }

        private sealed class FakePcWorkstationsLooseForge
        {
            public ArrayList DisplayDataList = new ArrayList { new FakeDisplayWorkstationsLooseForgeRow() };
        }

        private sealed class FakeProgressionMapKeyWorkstationsLooseForge
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingworkstations", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcWorkstationsLooseForge() };
                return null;
            }
        }

        [Fact]
        public void MapKeyOnly_Workstations_LooseItemName_toolForge_MatchesBlockKeyForge()
        {
            var prog = new FakeProgressionMapKeyWorkstationsLooseForge();
            var lvl = GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Workstations", "forge", 1);
            Assert.Equal(4, lvl);
        }

        private sealed class FakeDisplayWorkstationsRootItemWorkbench
        {
            public ItemClass item = new ItemClass { Name = "toolWorkbenchPlaceable" };
            public string unlock_level = "10";
        }

        private sealed class FakePcWorkstationsRootWorkbench
        {
            public ArrayList DisplayDataList = new ArrayList { new FakeDisplayWorkstationsRootItemWorkbench() };
        }

        private sealed class FakeProgressionMapKeyWorkstationsRootWorkbench
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingworkstations", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcWorkstationsRootWorkbench() };
                return null;
            }
        }

        [Fact]
        public void MapKeyOnly_Workstations_RootDisplayItem_toolWorkbenchPlaceable_MatchesWorkbench()
        {
            var prog = new FakeProgressionMapKeyWorkstationsRootWorkbench();
            var lvl = GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Workstations", "workbench", 1);
            Assert.Equal(10, lvl);
        }

        /// <summary>Vanilla craftingWorkstations T1-1 composite row (unlock_level CSV + per-child UnlockTier).</summary>
        private sealed class FakeDisplayWorkstationsT11Composite
        {
            public string unlock_level = "4,8,10,12";
            public ArrayList UnlockDataList = new ArrayList
            {
                new FakeUnlockEntryGameTier { ItemName = "forge", UnlockTier = 0 },
                new FakeUnlockEntryGameTier { ItemName = "toolBellows", UnlockTier = 1 },
                new FakeUnlockEntryGameTier { ItemName = "workbench", UnlockTier = 2 },
                new FakeUnlockEntryGameTier { ItemName = "resourceLockPick", UnlockTier = 3 },
            };
        }

        private sealed class FakeDisplayWorkstationsT4Composite
        {
            public string unlock_level = "40,44,48,50";
            public ArrayList UnlockDataList = new ArrayList
            {
                new FakeUnlockEntryGameTier { ItemName = "toolApiaryExtractor", UnlockTier = 0 },
                new FakeUnlockEntryGameTier { ItemName = "toolApiaryBroodBox", UnlockTier = 1 },
                new FakeUnlockEntryGameTier { ItemName = "toolDewFilter", UnlockTier = 2 },
                new FakeUnlockEntryGameTier { ItemName = "chemistryStation", UnlockTier = 3 },
            };
        }

        private sealed class FakePcWorkstationsMapKeyComposite
        {
            public ArrayList DisplayDataList = new ArrayList
            {
                new FakeDisplayWorkstationsT11Composite(),
                new FakeDisplayWorkstationsT4Composite(),
            };
        }

        private sealed class FakeProgressionMapKeyWorkstations
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingworkstations", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcWorkstationsMapKeyComposite() };
                return null;
            }
        }

        [Fact]
        public void MapKeyOnly_Workstations_CompositeRow_UsesMatchedUnlockTierColumn()
        {
            var prog = new FakeProgressionMapKeyWorkstations();
            Assert.Equal(4, GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Workstations", "forge", 1));
            Assert.Equal(10, GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Workstations", "workbench", 1));
            Assert.Equal(50, GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Workstations", "chemistryStation", 1));
        }

        [Fact]
        public void MapKeyOnly_Workstations_VanillaApiaryToolIds_MatchCompositeUnlockColumns()
        {
            var prog = new FakeProgressionMapKeyWorkstations();
            Assert.Equal(40, GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Workstations", "toolApiaryExtractor", 1));
            Assert.Equal(44, GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Workstations", "toolApiaryBroodBox", 1));
            Assert.Equal(48, GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Workstations", "toolDewFilter", 1));
        }

        /// <summary>In-game <c>UnlockData</c> may leave <c>ItemName</c> empty while <c>DisplayData.GetUnlockItem</c> holds the block id.</summary>
        private sealed class FakeDisplayWorkstationForgeUnlockItemOnly
        {
            public string unlock_level = "4,8,10,12";
            public ArrayList UnlockDataList = new ArrayList
            {
                new FakeUnlockEntryGameTier { ItemName = null, UnlockTier = 0 },
            };

            public ItemClass GetUnlockItem(int u)
            {
                if (u == 0) return new ItemClass { Name = "forge" };
                return null;
            }
        }

        private sealed class FakePcWorkstationsUnlockItemOnly
        {
            public ArrayList DisplayDataList = new ArrayList { new FakeDisplayWorkstationForgeUnlockItemOnly() };
        }

        private sealed class FakeProgressionMapKeyWorkstationsUnlockItem
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingworkstations", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcWorkstationsUnlockItemOnly() };
                return null;
            }
        }

        [Fact]
        public void MapKeyOnly_Workstations_MatchesBlockIdViaGetUnlockItemWhenUnlockStringsEmpty()
        {
            var prog = new FakeProgressionMapKeyWorkstationsUnlockItem();
            Assert.Equal(4, GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Workstations", "forge", 1));
        }

        /// <summary>Game <c>UnlockData</c> / <c>DisplayData</c> expose <c>ItemName</c> as properties — no public field named <c>ItemName</c>.</summary>
        private sealed class FakeUnlockPropOnly
        {
            public string ItemName { get; set; } = "forge";
            public int UnlockTier { get; set; }
        }

        private sealed class FakeDisplayCompositePropUnlockOnly
        {
            public string unlock_level = "4,8,10,12";
            public ArrayList UnlockDataList = new ArrayList { new FakeUnlockPropOnly() };
        }

        private sealed class FakePcWorkstationsPropUnlock
        {
            public ArrayList DisplayDataList = new ArrayList { new FakeDisplayCompositePropUnlockOnly() };
        }

        private sealed class FakeProgressionMapKeyWorkstationsPropUnlock
        {
            public object GetProgressionValue(string name)
            {
                if (string.Equals(name, "craftingworkstations", StringComparison.OrdinalIgnoreCase))
                    return new FakePv { ProgressionClass = new FakePcWorkstationsPropUnlock() };
                return null;
            }
        }

        [Fact]
        public void MapKeyOnly_Workstations_UnlockItemNameViaPropertyNotField()
        {
            var prog = new FakeProgressionMapKeyWorkstationsPropUnlock();
            Assert.Equal(4, GameReflection.TestHooks.TryResolveRequiredLevelByMapKeyOnlyForTests(prog, "Workstations", "forge", 1));
        }
    }
}
