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
    }
}
