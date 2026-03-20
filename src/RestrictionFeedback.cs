using System;
using System.Reflection;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Shows in-world feedback when the player is blocked from using an item (workstation, vehicle, etc.).
    /// Message: "You don't know how to use [item name]" and "[Crafting Skill Name] [player level]/[required level]" in red.
    /// Uses GameManager.ShowTooltip with "ui_denied" for the red popup style; see docs/GAME_API_NOTES.md.
    /// </summary>
    internal static class RestrictionFeedback
    {
        /// <summary>
        /// Shows the red restriction popup to the local player using the game's tooltip API.
        /// </summary>
        /// <param name="localPlayer">EntityPlayerLocal (or null to fall back to log and try GetLocalPlayer).</param>
        /// <param name="itemDisplayName">Display name for the item/block/vehicle (e.g. "Forge", "Bicycle").</param>
        /// <param name="craftingSkillDisplayName">Display name for the skill (e.g. "Workstations", "Vehicles").</param>
        /// <param name="playerLevel">Player's current level in that skill.</param>
        /// <param name="requiredLevel">Required level to use the item.</param>
        internal static void ShowRestrictionPopup(object localPlayer, string itemDisplayName, string craftingSkillDisplayName, int playerLevel, int requiredLevel)
        {
            var line1 = "You don't know how to use " + (itemDisplayName ?? "this item");
            var line2 = $"{craftingSkillDisplayName ?? "Skill"} {playerLevel}/{requiredLevel}";
            var fullText = line1 + "\n" + line2;

            var player = localPlayer ?? GameReflection.GetLocalPlayer();
            if (player == null)
            {
                try { UnityEngine.Debug.Log($"[LimitByCraftingSkill] {line1} | {line2}"); } catch { }
                return;
            }

            try
            {
                var gmType = typeof(GameManager);
                // ShowTooltip(EntityPlayerLocal _player, string _text, string _arg, string _alertSound = null, ...)
                var method = gmType.GetMethod("ShowTooltip", BindingFlags.Static | BindingFlags.Public,
                    null, new Type[] { player.GetType(), typeof(string), typeof(string), typeof(string) }, null);
                if (method != null)
                {
                    method.Invoke(null, new object[] { player, fullText, string.Empty, "ui_denied" });
                    return;
                }
                // Fallback: overload with just (player, text)
                method = gmType.GetMethod("ShowTooltip", BindingFlags.Static | BindingFlags.Public,
                    null, new Type[] { player.GetType(), typeof(string) }, null);
                if (method != null)
                {
                    method.Invoke(null, new object[] { player, fullText });
                    return;
                }
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] ShowRestrictionPopup: " + ex.Message);
            }

            try { UnityEngine.Debug.Log($"[LimitByCraftingSkill] {line1} | {line2}"); } catch { }
        }

        /// <summary>
        /// Legacy: logs the restriction message when popup is not used.
        /// </summary>
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
                // Unity not available
            }
        }
    }
}
