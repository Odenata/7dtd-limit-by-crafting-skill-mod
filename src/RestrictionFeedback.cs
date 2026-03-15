namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Shows in-world feedback when the player is blocked from using an item (workstation, vehicle, etc.).
    /// Message: "You don't know how to use [item name]" and "[Crafting Skill Name] [player level]/[required level]" in red.
    /// Hook into the game's popup/notification API when identified; see docs/GAME_API_NOTES.md.
    /// </summary>
    internal static class RestrictionFeedback
    {
        internal static void ShowRestrictionMessage(string itemName, string craftingSkillName, int playerLevel, int requiredLevel)
        {
            try
            {
                var line1 = "You don't know how to use " + (itemName ?? "this item");
                var line2 = $"{craftingSkillName ?? "Skill"} {playerLevel}/{requiredLevel}";
                UnityEngine.Debug.Log($"[LimitByCraftingSkillMod] {line1} | {line2}");
            }
            catch
            {
                // Unity not available or popup API not yet hooked
            }
        }
    }
}
