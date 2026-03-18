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

    public class RequiredLevelFromProgressionTests
    {
        [Fact]
        public void GetRequiredLevelForItemForUnitTest_HarvestingSyntheticTier1_UsesFirstQualityBand()
        {
            var itemClass = new ItemClass { Name = "meleeToolPickT1IronPickaxe" };
            var itemValue = new ItemValue { ItemClass = itemClass };
            var prog = new FakeProgressionForRequiredLevel();
            var level = GameReflection.GetRequiredLevelForItemForUnitTest(itemClass, itemValue, prog);
            Assert.Equal(3, level);
        }

        [Fact]
        public void GetRequiredLevelForItemForUnitTest_WithQuality_UsesMatchingBand()
        {
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

        private sealed class TestItemValueWithQuality : ItemValue
        {
            public ushort Quality;
        }
    }
}
