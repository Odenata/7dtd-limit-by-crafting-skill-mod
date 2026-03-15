namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Pure logic: given player level, item required level, and config, determines if the item is restricted.
    /// Used by patches and unit tests.
    /// </summary>
    public static class LimitByCraftingSkillLogic
    {
        /// <summary>
        /// Returns true if the player should be restricted from using/equipping the item.
        /// </summary>
        /// <param name="playerSkillLevel">Player's current level in the relevant crafting skill.</param>
        /// <param name="requiredLevel">Minimum level required to use this item (from progression/quality).</param>
        /// <param name="restrictionEnabledForSkill">Whether config has restriction enabled for this crafting skill.</param>
        /// <returns>True if the item is restricted.</returns>
        public static bool IsRestricted(int playerSkillLevel, int requiredLevel, bool restrictionEnabledForSkill)
        {
            if (!restrictionEnabledForSkill) return false;
            return requiredLevel > 0 && playerSkillLevel < requiredLevel;
        }
    }
}
