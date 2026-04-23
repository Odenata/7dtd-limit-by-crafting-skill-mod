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
                if (TryInvokeShowTooltip(player, fullText))
                    return;
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] ShowRestrictionPopup: " + ex.Message);
            }

            try { UnityEngine.Debug.Log($"[LimitByCraftingSkill] {line1} | {line2}"); } catch { }
        }

        /// <summary>
        /// GameManager.ShowTooltip overloads differ by game version; resolve from Assembly-CSharp at runtime.
        /// Prefer 9-arg with ui_denied, then string[] variant, then 5-arg without sound, then ShowTooltipMP.
        /// </summary>
        private static bool TryInvokeShowTooltip(object player, string fullText)
        {
            var gmType = typeof(GameManager);
            var gameAssembly = gmType.Assembly;
            var eplType = gameAssembly.GetType("EntityPlayerLocal");
            if (eplType == null || !eplType.IsInstanceOfType(player))
                return false;

            var toolTipEventType = gameAssembly.GetType("ToolTipEvent");

            const string deniedSound = "ui_denied";
            const bool showImmediately = true;
            const bool pinTooltip = false;
            const float timeout = 8f;

            // ShowTooltip(EntityPlayerLocal, string, string, string, ToolTipEvent, bool, bool, float)
            if (toolTipEventType != null)
            {
                var sig9 = new[]
                {
                    eplType, typeof(string), typeof(string), typeof(string),
                    toolTipEventType, typeof(bool), typeof(bool), typeof(float)
                };
                var m9 = gmType.GetMethod("ShowTooltip", BindingFlags.Static | BindingFlags.Public, null, sig9, null);
                if (m9 != null)
                {
                    m9.Invoke(null, new object[]
                    {
                        player, fullText, string.Empty, deniedSound, null,
                        showImmediately, pinTooltip, timeout
                    });
                    return true;
                }

                // ShowTooltip(EntityPlayerLocal, string, string[], string, ToolTipEvent, bool, bool, float)
                var sig9Arr = new[]
                {
                    eplType, typeof(string), typeof(string[]), typeof(string),
                    toolTipEventType, typeof(bool), typeof(bool), typeof(float)
                };
                var m9a = gmType.GetMethod("ShowTooltip", BindingFlags.Static | BindingFlags.Public, null, sig9Arr, null);
                if (m9a != null)
                {
                    m9a.Invoke(null, new object[]
                    {
                        player, fullText, System.Array.Empty<string>(), deniedSound, null,
                        showImmediately, pinTooltip, timeout
                    });
                    return true;
                }
            }

            // ShowTooltip(EntityPlayerLocal, string, bool, bool, float) — no alert sound (still visible tooltip)
            var sig5 = new[] { eplType, typeof(string), typeof(bool), typeof(bool), typeof(float) };
            var m5 = gmType.GetMethod("ShowTooltip", BindingFlags.Static | BindingFlags.Public, null, sig5, null);
            if (m5 != null)
            {
                m5.Invoke(null, new object[] { player, fullText, showImmediately, pinTooltip, timeout });
                return true;
            }

            // ShowTooltipMP(EntityPlayer, string, string)
            var epType = gameAssembly.GetType("EntityPlayer");
            if (epType != null && epType.IsInstanceOfType(player))
            {
                var sigMp = new[] { epType, typeof(string), typeof(string) };
                var mMp = gmType.GetMethod("ShowTooltipMP", BindingFlags.Static | BindingFlags.Public, null, sigMp, null);
                if (mMp != null)
                {
                    mMp.Invoke(null, new object[] { player, fullText, deniedSound });
                    return true;
                }
            }

            return false;
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
