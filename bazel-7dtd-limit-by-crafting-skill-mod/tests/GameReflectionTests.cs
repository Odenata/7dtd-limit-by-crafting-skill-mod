using Xunit;

namespace LimitByCraftingSkillMod.Tests
{
    public class GameReflectionTests
    {
        [Fact]
        public void GetCraftingSkillGroup_WhenItemClassIsInMap_ReturnsMappedGroup()
        {
            var itemClass = new ItemClass { Name = "meleeToolPickT1IronPickaxe" };
            var result = GameReflection.GetCraftingSkillGroup(itemClass);
            Assert.Equal("HarvestingTools", result);
        }

        [Fact]
        public void GetCraftingSkillGroup_WhenUnmappedAndGameCraftingSkillGroupIsTools_ReturnsHarvestingTools()
        {
            var itemClass = new ItemClass { Name = "someModPickNotInMap", CraftingSkillGroup = "Tools" };
            Assert.Equal("HarvestingTools", GameReflection.GetCraftingSkillGroup(itemClass));
        }

        [Fact]
        public void GetCraftingSkillGroup_WhenUnmappedAndGameCraftingSkillGroupIsElectrician_ReturnsElectrician()
        {
            var itemClass = new ItemClass { Name = "customWireBlock", CraftingSkillGroup = "Electrician" };
            Assert.Equal("Electrician", GameReflection.GetCraftingSkillGroup(itemClass));
        }

        [Fact]
        public void GetCraftingSkillGroup_MapEntryOverridesGameCraftingSkillGroup()
        {
            var itemClass = new ItemClass
            {
                Name = "meleeToolPickT1IronPickaxe",
                CraftingSkillGroup = "Electrician"
            };
            Assert.Equal("HarvestingTools", GameReflection.GetCraftingSkillGroup(itemClass));
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
        public void ToProgressionLookupName_WhenHarvestingTools_ReturnsCraftingharvestingtools()
        {
            Assert.Equal("craftingharvestingtools", GameReflection.ToProgressionLookupName("HarvestingTools"));
        }

        [Fact]
        public void UsesSyntheticQualityTierForRequiredLevel_ElectricianWorkstationsHarvesting_ReturnsTrue()
        {
            Assert.True(GameReflection.UsesSyntheticQualityTierForRequiredLevel("Electrician"));
            Assert.True(GameReflection.UsesSyntheticQualityTierForRequiredLevel("Workstations"));
            Assert.True(GameReflection.UsesSyntheticQualityTierForRequiredLevel("HarvestingTools"));
        }

        [Fact]
        public void UsesSyntheticQualityTierForRequiredLevel_Weapons_ReturnsFalse()
        {
            Assert.False(GameReflection.UsesSyntheticQualityTierForRequiredLevel("Weapons"));
            Assert.False(GameReflection.UsesSyntheticQualityTierForRequiredLevel("Blades"));
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
        public void GetRequiredLevelForItem_WhenNotInMap_ReturnsZero()
        {
            var itemClass = new ItemClass { Name = "unmappedTestItem_xyz" };
            var itemValue = new ItemValue { ItemClass = itemClass };
            Assert.Equal(0, GameReflection.GetRequiredLevelForItem(itemClass, itemValue));
        }

        [Fact]
        public void GetRequiredLevelForItem_WhenBladesNoQuality_ReturnsZero()
        {
            var itemClass = new ItemClass { Name = "meleeWpnBladeT0BoneKnife" };
            var itemValue = new ItemValue { ItemClass = itemClass };
            if (GameReflection.GetCraftingSkillGroup(itemClass) == null) return;
            var result = GameReflection.GetRequiredLevelForItem(itemClass, itemValue);
            Assert.Equal(0, result);
        }
    }
}
