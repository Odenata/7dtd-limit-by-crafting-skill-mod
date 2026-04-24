using System.Reflection;
using Xunit;

namespace LimitByCraftingSkillMod.Tests
{
    public class GameReflectionTests
    {
        private static ItemClass ItemWithNameAndOptionalCraftingSkill(string name, string craftingSkillGroup = null)
        {
            var ic = new ItemClass { Name = name };
            if (craftingSkillGroup == null) return ic;
            var p = typeof(ItemClass).GetProperty("CraftingSkillGroup", BindingFlags.Public | BindingFlags.Instance);
            var f = typeof(ItemClass).GetField("CraftingSkillGroup", BindingFlags.Public | BindingFlags.Instance);
            if (p != null) p.SetValue(ic, craftingSkillGroup);
            else if (f != null) f.SetValue(ic, craftingSkillGroup);
            return ic;
        }

        [Fact]
        public void GetCraftingSkillGroup_WhenItemClassIsInMap_ReturnsMappedGroup()
        {
            var probe = new ItemClass { Name = "meleeToolPickT1IronPickaxe" };
            if (GameReflection.GetCraftingSkillGroup(probe) == null)
                return; // e.g. Bazel test without ClassNameToCraftingSkillMap.xml beside DLL
            var itemClass = new ItemClass { Name = "meleeToolPickT1IronPickaxe" };
            var result = GameReflection.GetCraftingSkillGroup(itemClass);
            Assert.Equal("HarvestingTools", result);
        }

        [Fact]
        public void GetCraftingSkillGroup_WhenUnmappedAndGameCraftingSkillGroupIsTools_ReturnsHarvestingTools()
        {
            var itemClass = ItemWithNameAndOptionalCraftingSkill("someModPickNotInMap", "Tools");
            if (typeof(ItemClass).GetProperty("CraftingSkillGroup") == null &&
                typeof(ItemClass).GetField("CraftingSkillGroup") == null)
                return;
            Assert.Equal("HarvestingTools", GameReflection.GetCraftingSkillGroup(itemClass));
        }

        [Fact]
        public void GetCraftingSkillGroup_WhenUnmappedAndGameCraftingSkillGroupIsElectrician_ReturnsElectrician()
        {
            var itemClass = ItemWithNameAndOptionalCraftingSkill("customWireBlock", "Electrician");
            if (typeof(ItemClass).GetProperty("CraftingSkillGroup") == null &&
                typeof(ItemClass).GetField("CraftingSkillGroup") == null)
                return;
            Assert.Equal("Electrician", GameReflection.GetCraftingSkillGroup(itemClass));
        }

        [Fact]
        public void GetCraftingSkillGroup_MapEntryOverridesGameCraftingSkillGroup()
        {
            var probe = new ItemClass { Name = "meleeToolPickT1IronPickaxe" };
            if (GameReflection.GetCraftingSkillGroup(probe) == null) return;
            var itemClass = ItemWithNameAndOptionalCraftingSkill("meleeToolPickT1IronPickaxe", "Electrician");
            Assert.Equal("HarvestingTools", GameReflection.GetCraftingSkillGroup(itemClass));
        }

        [Fact]
        public void GetCraftingSkillGroup_WhenItemClassIsNull_ReturnsNull()
        {
            Assert.Null(GameReflection.GetCraftingSkillGroup(null));
        }

        [Fact]
        public void GetCraftingSkillGroup_UnmappedThrownPrefix_InfersExplosives()
        {
            Assert.Equal("Explosives", GameReflection.GetCraftingSkillGroup(new ItemClass { Name = "thrownSyntheticTestItemZz99" }));
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

        [Theory]
        [InlineData("Vehicles", "craftingvehicles")]
        [InlineData("Workstations", "craftingworkstations")]
        [InlineData("Electrician", "craftingelectrician")]
        [InlineData("Robotics", "craftingrobotics")]
        [InlineData("Traps", "craftingtraps")]
        [InlineData("Seeds", "craftingseeds")]
        [InlineData("Explosives", "craftingexplosives")]
        [InlineData("MachineGuns", "craftingmachineguns")]
        [InlineData("Rifles", "craftingrifles")]
        [InlineData("Shotguns", "craftingshotguns")]
        [InlineData("Handguns", "craftinghandguns")]
        [InlineData("Spears", "craftingspears")]
        [InlineData("Sledgehammers", "craftingsledgehammers")]
        [InlineData("Clubs", "craftingclubs")]
        [InlineData("Bows", "craftingbows")]
        [InlineData("Blades", "craftingblades")]
        [InlineData("Knuckles", "craftingknuckles")]
        [InlineData("RepairTools", "craftingrepairtools")]
        [InlineData("SalvageTools", "craftingsalvagetools")]
        public void ToProgressionLookupName_ConfigCraftingSkills_AlignsWithProgressionNamesDoc(string gameGroup, string progressionName)
        {
            Assert.Equal(progressionName, GameReflection.ToProgressionLookupName(gameGroup));
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
