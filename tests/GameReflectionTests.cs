using Xunit;

namespace LimitByCraftingSkillMod.Tests
{
    public class GameReflectionTests
    {
        [Fact]
        public void GetCraftingSkillGroup_WhenItemClassHasGroup_ReturnsGroup()
        {
            var itemClass = new ItemClass { Group = "HarvestingTools" };
            var result = GameReflection.GetCraftingSkillGroup(itemClass);
            Assert.Equal("HarvestingTools", result);
        }

        [Fact]
        public void GetCraftingSkillGroup_WhenItemClassIsNull_ReturnsNull()
        {
            Assert.Null(GameReflection.GetCraftingSkillGroup(null));
        }

        [Fact]
        public void GetQuality_WhenItemValueHasNoQuality_ReturnsZero()
        {
            var itemValue = new ItemValue();
            var result = GameReflection.GetQuality(itemValue);
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetRequiredLevelForItem_WhenItemHasNoQuality_ReturnsZero()
        {
            var itemClass = new ItemClass { Group = "Weapons" };
            var itemValue = new ItemValue { ItemClass = itemClass };
            var result = GameReflection.GetRequiredLevelForItem(itemClass, itemValue);
            Assert.Equal(0, result);
        }
    }
}
