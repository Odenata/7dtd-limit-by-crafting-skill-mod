using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Shows in-world feedback when the player is blocked from using an item (workstation, vehicle, etc.).
    /// Message: "You don't know how to use [item name]" and "[Crafting Skill Name] [player level]/[required level]".
    /// Uses GameManager.ShowTooltip with "ui_denied"; popup text color is set on the tooltip XUiV_Label after show
    /// (NGUI hex markup in the string is not reliable for popup tooltips). See docs/GAME_API_NOTES.md.
    /// </summary>
    internal static class RestrictionFeedback
    {
        /// <summary>Matches inventory restriction emphasis; visible on dark HUD.</summary>
        private static readonly Color TooltipRestrictionRed = new Color(0.95f, 0.12f, 0.12f, 1f);

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
                try { Debug.Log($"[LimitByCraftingSkill] {line1} | {line2}"); } catch { }
                return;
            }

            try
            {
                if (TryInvokeShowTooltip(player, fullText))
                {
                    ScheduleApplyRestrictionTooltipLabelRed(player);
                    return;
                }
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] ShowRestrictionPopup: " + ex.Message);
            }

            try { Debug.Log($"[LimitByCraftingSkill] {line1} | {line2}"); } catch { }
        }

        private static void ScheduleApplyRestrictionTooltipLabelRed(object player)
        {
            var host = TryGetCoroutineHost();
            if (host != null)
            {
                try
                {
                    // StartCoroutine is on UnityEngine.MonoBehaviour; use reflection so the mock compiles.
                    var m = host.GetType().GetMethod("StartCoroutine", new[] { typeof(IEnumerator) });
                    m?.Invoke(host, new object[] { CoApplyRestrictionTooltipRed(player) });
                    if (m != null)
                        return;
                }
                catch
                {
                    // fall through
                }
            }

            TryApplyRestrictionTooltipLabelRed(player);
        }

        private static object TryGetCoroutineHost()
        {
            try
            {
                var gm = typeof(GameManager);
                object inst = gm.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
                if (inst == null)
                {
                    var f = gm.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    inst = f?.GetValue(null);
                }
                return inst;
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerator CoApplyRestrictionTooltipRed(object player)
        {
            for (var i = 0; i < 8; i++)
            {
                if (TryApplyRestrictionTooltipLabelRed(player))
                    yield break;
                yield return null;
            }
        }

        /// <summary>
        /// Sets <see cref="XUiV_Label"/> / UILabel color on the active popup tooltip after ShowTooltip (text may refresh same frame).
        /// </summary>
        private static bool TryApplyRestrictionTooltipLabelRed(object player)
        {
            try
            {
                var xui = TryGetXUiFromLocalPlayer(player);
                if (xui == null)
                    return false;

                var asm = xui.GetType().Assembly;
                var toolTipControllerType = asm.GetType("XUiC_ToolTip");
                var red = TooltipRestrictionRed;
                var any = false;

                var cur = GetMemberValue(xui, "currentToolTip", "CurrentToolTip");
                if (cur != null && toolTipControllerType != null && toolTipControllerType.IsInstanceOfType(cur))
                    any |= TryTintXUiToolTipController(cur, red);

                var popupType = asm.GetType("XUiC_PopupToolTip");
                if (popupType != null)
                {
                    var getInst = popupType.GetMethod("GetInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { xui.GetType() }, null);
                    var popup = getInst?.Invoke(null, new object[] { xui });
                    if (popup != null)
                        any |= TryTintToolTipControllersUnderPopup(popup, toolTipControllerType, red);
                }

                return any;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryTintToolTipControllersUnderPopup(object popupRoot, Type toolTipControllerType, Color red)
        {
            if (popupRoot == null || toolTipControllerType == null)
                return false;
            var q = new Queue<object>();
            q.Enqueue(popupRoot);
            var any = false;
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c == null)
                    continue;
                var t = c.GetType();
                if (toolTipControllerType.IsAssignableFrom(t))
                    any |= TryTintXUiToolTipController(c, red);

                var children = GetMemberValue(c, "children", "Children") as IList;
                if (children != null)
                {
                    foreach (var ch in children)
                        q.Enqueue(ch);
                }
            }

            return any;
        }

        private static bool TryTintXUiToolTipController(object toolTipCtrl, Color red)
        {
            var label = GetMemberValue(toolTipCtrl, "label", "Label");
            if (label == null)
                return false;
            TrySetViewAndNgUiLabelColor(label, red);
            return true;
        }

        private static void TrySetViewAndNgUiLabelColor(object xUiVLabel, Color red)
        {
            foreach (var propName in new[] { "Color", "color" })
            {
                var p = xUiVLabel.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null && p.PropertyType == typeof(Color) && p.CanWrite)
                {
                    try { p.SetValue(xUiVLabel, red, null); } catch { }
                }
            }

            var uiLabel = GetMemberValue(xUiVLabel, "Label", "label");
            if (uiLabel != null)
            {
                var cp = uiLabel.GetType().GetProperty("color", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (cp != null && cp.PropertyType == typeof(Color) && cp.CanWrite)
                {
                    try { cp.SetValue(uiLabel, red, null); } catch { }
                }
            }
        }

        private static object TryGetXUiFromLocalPlayer(object player)
        {
            if (player == null)
                return null;
            try
            {
                var t = player.GetType();
                object lpui = null;
                foreach (var name in new[] { "PlayerUI", "playerUI" })
                {
                    var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (p != null)
                    {
                        lpui = p.GetValue(player);
                        break;
                    }

                    var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null)
                    {
                        lpui = f.GetValue(player);
                        break;
                    }
                }

                if (lpui == null)
                    return null;
                var lpT = lpui.GetType();
                foreach (var name in new[] { "xui", "XUi" })
                {
                    var p = lpT.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (p != null)
                        return p.GetValue(lpui);
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private static object GetMemberValue(object target, params string[] names)
        {
            if (target == null)
                return null;
            var t = target.GetType();
            foreach (var name in names)
            {
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null)
                    return p.GetValue(target);
                var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null)
                    return f.GetValue(target);
            }

            return null;
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
                Debug.Log($"[LimitByCraftingSkillMod] {line1} | {line2}");
            }
            catch
            {
                // Unity not available
            }
        }
    }
}
