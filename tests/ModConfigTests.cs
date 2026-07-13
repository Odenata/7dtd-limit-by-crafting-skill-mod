using System.Collections.Generic;
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

        [Theory]
        [InlineData("true", true)]
        [InlineData("True", true)]
        [InlineData("1", true)]
        [InlineData("enabled", true)]
        [InlineData("on", true)]
        [InlineData("false", false)]
        [InlineData("0", false)]
        [InlineData("disabled", false)]
        [InlineData("off", false)]
        [InlineData("", false)]
        [InlineData("nope", false)]
        public void ParseBoolSetting_MapsGearsStyleValues(string value, bool expected)
        {
            Assert.Equal(expected, ModConfig.ParseBoolSetting(value, defaultValue: false));
        }

        [Fact]
        public void ParseBoolSetting_FallsBackToDefault_WhenUnrecognized()
        {
            Assert.True(ModConfig.ParseBoolSetting("maybe", defaultValue: true));
        }

        [Fact]
        public void ApplyWorldSettings_UpdatesSkills_LeavesDebugMode()
        {
            ModConfig.ClearServerSnapshot();
            try
            {
                var config = ModConfig.Instance;
                var previousDebug = config.DebugMode;
                config.ApplyGlobalSettings(!previousDebug);

                config.ApplyWorldSettings(new Dictionary<string, bool>
                {
                    ["Vehicles"] = false,
                    ["Food"] = true,
                });

                Assert.False(config.IsRestrictionEnabledForSkill("Vehicles"));
                Assert.True(config.IsRestrictionEnabledForSkill("Food"));
                Assert.Equal(!previousDebug, config.DebugMode);
            }
            finally
            {
                ModConfig.ClearServerSnapshot();
            }
        }

        [Fact]
        public void ApplyGlobalSettings_UpdatesDebugMode_LeavesSkills()
        {
            ModConfig.ClearServerSnapshot();
            try
            {
                var config = ModConfig.Instance;
                config.ApplyWorldSettings(new Dictionary<string, bool> { ["Vehicles"] = false });
                Assert.False(config.IsRestrictionEnabledForSkill("Vehicles"));

                config.ApplyGlobalSettings(true);
                Assert.True(config.DebugMode);
                Assert.False(config.IsRestrictionEnabledForSkill("Vehicles"));

                config.ApplyGlobalSettings(false);
                Assert.False(config.DebugMode);
                Assert.False(config.IsRestrictionEnabledForSkill("Vehicles"));
            }
            finally
            {
                ModConfig.ClearServerSnapshot();
            }
        }

        [Fact]
        public void ApplyWorldSettings_AfterServerSnapshot_MutatesLiveToggles()
        {
            ModConfig.ClearServerSnapshot();
            try
            {
                var xml =
                    "<LimitByCraftingSkillModConfig>" +
                    "<CraftingSkills><Vehicles>true</Vehicles><Food>false</Food></CraftingSkills>" +
                    "<DebugMode>false</DebugMode>" +
                    "</LimitByCraftingSkillModConfig>";

                Assert.True(ModConfig.TryApplyServerXml(xml, "test server", out var error), error);
                Assert.True(ModConfig.Instance.IsServerProvided);
                Assert.True(ModConfig.Instance.IsRestrictionEnabledForSkill("Vehicles"));

                // Gears World prefer: mutate live instance even after server snapshot.
                ModConfig.Instance.ApplyWorldSettings(new Dictionary<string, bool>
                {
                    ["Vehicles"] = false,
                    ["Food"] = true,
                });

                Assert.True(ModConfig.Instance.IsServerProvided);
                Assert.False(ModConfig.Instance.IsRestrictionEnabledForSkill("Vehicles"));
                Assert.True(ModConfig.Instance.IsRestrictionEnabledForSkill("Food"));
            }
            finally
            {
                ModConfig.ClearServerSnapshot();
            }
        }
    }
}
