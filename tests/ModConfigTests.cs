using Xunit;

namespace LimitByCraftingSkillMod.Tests
{
    public class ModConfigTests
    {
        [Fact]
        public void Instance_IsNotNull()
        {
            Assert.NotNull(ModConfig.Instance);
        }

        [Fact]
        public void IsRestrictionEnabledForSkill_UnknownSkill_ReturnsTrue()
        {
            Assert.True(ModConfig.Instance.IsRestrictionEnabledForSkill("UnknownSkill"));
        }

        [Fact]
        public void IsRestrictionEnabledForSkill_EmptyOrNull_ReturnsFalse()
        {
            Assert.False(ModConfig.Instance.IsRestrictionEnabledForSkill(null));
            Assert.False(ModConfig.Instance.IsRestrictionEnabledForSkill(""));
        }

        [Fact]
        public void ServerSnapshot_OverridesLocalInstance()
        {
            ModConfig.ClearServerSnapshot();
            try
            {
                var xml =
                    "<LimitByCraftingSkillModConfig>" +
                    "<CraftingSkills><Food>true</Food><Vehicles>false</Vehicles></CraftingSkills>" +
                    "<DebugMode>true</DebugMode>" +
                    "</LimitByCraftingSkillModConfig>";

                Assert.True(ModConfig.TryApplyServerXml(xml, "test server", out var error), error);

                Assert.True(ModConfig.Instance.IsServerProvided);
                Assert.True(ModConfig.Instance.DebugMode);
                Assert.True(ModConfig.Instance.IsRestrictionEnabledForSkill("Food"));
                Assert.False(ModConfig.Instance.IsRestrictionEnabledForSkill("Vehicles"));
                Assert.True(ModConfig.Instance.IsRestrictionEnabledForSkill("UnknownSkill"));
                Assert.False(string.IsNullOrEmpty(ModConfig.Instance.Hash));
            }
            finally
            {
                ModConfig.ClearServerSnapshot();
            }
        }

        [Fact]
        public void TryApplyServerXml_InvalidXml_LeavesExistingConfig()
        {
            ModConfig.ClearServerSnapshot();
            var before = ModConfig.Instance;

            Assert.False(ModConfig.TryApplyServerXml("<LimitByCraftingSkillModConfig>", "bad server", out var error));

            Assert.False(string.IsNullOrEmpty(error));
            Assert.Same(before, ModConfig.Instance);
            Assert.False(ModConfig.Instance.IsServerProvided);
        }

        [Fact]
        public void LocalConfigXmlForSync_RoundTrips()
        {
            var xml = ModConfig.GetLocalConfigXmlForSync();

            Assert.True(ModConfig.TryApplyServerXml(xml, "roundtrip", out var error), error);
            Assert.Equal(ModConfig.Instance.Hash, ModConfig.LoadLocalSnapshot().Hash);

            ModConfig.ClearServerSnapshot();
        }
    }
}
