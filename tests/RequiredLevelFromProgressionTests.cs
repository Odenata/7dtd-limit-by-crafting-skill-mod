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
                new FakeUnlockEntry { ItemName = "thrownGrenade", UnlockTier = 1 },
                new FakeUnlockEntry { ItemName = "thrownDynamite", UnlockTier = 1 },
                new FakeUnlockEntry { ItemName = "thrownGrenadeContact", UnlockTier = 1 },
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
        public void CompositeExplosivesRow_SameUnlockTier_UsesPositionalUnlockLevelColumn()
        {
            var prog = new FakeProgressionExplosivesComposite();
            var contact = new ItemClass { Name = "thrownGrenadeContact" };
            var baseGrenade = new ItemClass { Name = "thrownGrenade" };

            var lvlContact = GameReflection.TestHooks.TryResolveCraftingExplosivesRequiredLevel(prog, contact, contact.Name, 1);
            var lvlBase = GameReflection.TestHooks.TryResolveCraftingExplosivesRequiredLevel(prog, baseGrenade, baseGrenade.Name, 1);

            Assert.Equal(16, lvlContact);
            Assert.Equal(5, lvlBase);
        }
    }
}
