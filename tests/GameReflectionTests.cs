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
        public void ToProgressionOrConfigName_WhenClothing_ReturnsArmor()
        {
            Assert.Equal("Armor", GameReflection.ToProgressionOrConfigName("Clothing"));
            Assert.Equal("Armor", GameReflection.ToProgressionOrConfigName("clothing"));
        }

        [Fact]
        public void ToProgressionLookupName_WhenClothing_ReturnsCraftingarmor()
        {
            Assert.Equal("craftingarmor", GameReflection.ToProgressionLookupName("Clothing"));
            Assert.Equal("craftingarmor", GameReflection.ToProgressionLookupName("clothing"));
        }

        [Fact]
        public void ToProgressionLookupName_WhenOtherSkill_ReturnsSame()
        {
            Assert.Equal("HarvestingTools", GameReflection.ToProgressionLookupName("HarvestingTools"));
        }

        [Fact]
        public void ToProgressionOrConfigName_WhenOtherSkill_ReturnsSame()
        {
            Assert.Equal("HarvestingTools", GameReflection.ToProgressionOrConfigName("HarvestingTools"));
            Assert.Equal("Weapons", GameReflection.ToProgressionOrConfigName("Weapons"));
        }

        [Fact]
        public void ToProgressionOrConfigName_WhenNullOrEmpty_ReturnsInput()
        {
            Assert.Null(GameReflection.ToProgressionOrConfigName(null));
            Assert.Equal("", GameReflection.ToProgressionOrConfigName(""));
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
