using Xunit;

namespace LimitByCraftingSkillMod.Tests
{
    public class LimitByCraftingSkillLogicTests
    {
        [Fact]
        public void IsRestricted_WhenRestrictionDisabled_ReturnsFalse()
        {
            Assert.False(LimitByCraftingSkillLogic.IsRestricted(0, 5, false));
            Assert.False(LimitByCraftingSkillLogic.IsRestricted(3, 5, false));
        }

        [Fact]
        public void IsRestricted_WhenPlayerLevelBelowRequired_ReturnsTrue()
        {
            Assert.True(LimitByCraftingSkillLogic.IsRestricted(1, 3, true));
            Assert.True(LimitByCraftingSkillLogic.IsRestricted(0, 1, true));
        }

        [Fact]
        public void IsRestricted_WhenPlayerLevelAtOrAboveRequired_ReturnsFalse()
        {
            Assert.False(LimitByCraftingSkillLogic.IsRestricted(3, 3, true));
            Assert.False(LimitByCraftingSkillLogic.IsRestricted(5, 3, true));
        }

        [Fact]
        public void IsRestricted_WhenRequiredLevelZero_ReturnsFalse()
        {
            Assert.False(LimitByCraftingSkillLogic.IsRestricted(0, 0, true));
            Assert.False(LimitByCraftingSkillLogic.IsRestricted(5, 0, true));
            Assert.True(LimitByCraftingSkillLogic.IsRestricted(5, 10, true));
        }
    }
}
