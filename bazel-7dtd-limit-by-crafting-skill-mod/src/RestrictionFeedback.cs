using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Shows in-world feedback when the player is blocked from using an item (workstation, vehicle, etc.).
    /// Message: "You don't know how to use [item name]" and "[Crafting Skill Name] [player level]/[required level]".
    /// Uses GameManager.ShowTooltip with "ui_denied"; popup text color is set on the tooltip XUiV_Label after show
    /// (NGUI hex markup in the string is not reliable for popup tooltips). Amber tint for readability; see docs/GAME_API_NOTES.md.
    /// </summary>
    internal static class RestrictionFeedback
    {
        /// <summary>First words of restriction body; used to detect our tooltip after DisplayTooltipText runs.</summary>
        internal const string RestrictionTooltipBodyMarker = "You don't know how to use";

        /// <summary>Warning-style amber: readable on typical HUD backgrounds; distinct from success/neutral UI.</summary>
        private static readonly Color TooltipRestrictionAmber = new Color(0.96f, 0.76f, 0.33f, 1f);

        /// <summary>Keep tint through fade-out after fields clear (singleton popup; time + alpha gated).</summary>
        private static float restrictionTooltipTintFadeTailUntilTime;

        private const float PopupTooltipTextAlphaFadeDone = 0.02f;
        private const float PopupTooltipFadeTailSeconds = 0.34f;

        /// <summary>
        /// Called from Harmony Postfix on XUiC_PopupToolTip.DisplayTooltipText after vanilla assigns text.
        /// </summary>
        internal static void OnPopupToolTipDisplayed(object popupInstance)
        {
            try
            {
                if (popupInstance == null || !PopupShowsRestrictionMessage(popupInstance))
                    return;
                BumpRestrictionTooltipFadeTail();
                TintPopupTooltipHierarchy(popupInstance);
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Harmony postfix on XUiC_PopupToolTip.Update — keeps restriction amber through fade-out (fields can clear before alpha).
        /// </summary>
        internal static void OnPopupToolTipUpdate(object popupInstance)
        {
            try
            {
                if (popupInstance == null)
                    return;

                if (PopupShowsRestrictionMessage(popupInstance))
                    BumpRestrictionTooltipFadeTail();

                if (!ShouldApplyRestrictionTooltipTint(popupInstance))
                    return;

                TintPopupTooltipHierarchy(popupInstance);
                TryGlobalRestrictionLabelScanForPlayer(GameReflection.GetLocalPlayer());

                if (TryGetPopupTextAlpha(popupInstance, out var a) && a <= PopupTooltipTextAlphaFadeDone)
                    restrictionTooltipTintFadeTailUntilTime = 0f;
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Harmony postfix on GameManager.ShowTooltip* — runs with the real message body (not binding fields).
        /// </summary>
        internal static void OnGameManagerShowTooltipAfter(object player, string text)
        {
            try
            {
                if (player == null || string.IsNullOrEmpty(text))
                    return;
                if (text.IndexOf(RestrictionTooltipBodyMarker, StringComparison.OrdinalIgnoreCase) < 0)
                    return;
                BumpRestrictionTooltipFadeTail();
                ScheduleApplyRestrictionTooltipLabelRed(player);
            }
            catch
            {
                // ignored
            }
        }

        private static bool PopupShowsRestrictionMessage(object popup)
        {
            var s = GetMemberValue(popup, "tooltipText", "TooltipText") as string;
            if (!string.IsNullOrEmpty(s) && s.IndexOf(RestrictionTooltipBodyMarker, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            var immediate = GetMemberValue(popup, "immediateTip", "ImmediateTip");
            if (immediate != null)
            {
                var txt = GetMemberValue(immediate, "Text", "text") as string;
                if (!string.IsNullOrEmpty(txt) && txt.IndexOf(RestrictionTooltipBodyMarker, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            // tooltipText may be empty or localized key while UILabel.text already shows our line.
            return AnyUILabelUnderPopupHierarchyContainsMarker(popup);
        }

        private static bool AnyUILabelUnderPopupHierarchyContainsMarker(object popupRoot)
        {
            var asm = typeof(GameManager).Assembly;
            var uiLabelType = asm.GetType("UILabel");
            if (uiLabelType == null)
                return false;

            var q = new Queue<object>();
            q.Enqueue(popupRoot);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c == null)
                    continue;

                var go = RestrictionLabelColor.GetViewGameObject(c) as GameObject;
                if (go != null)
                {
                    foreach (var comp in GetComponentsInChildrenObjects(go, uiLabelType))
                    {
                        if (ComponentTextContainsMarker(comp, RestrictionTooltipBodyMarker))
                            return true;
                    }
                }

                var children = GetMemberValue(c, "children", "Children") as IList;
                if (children != null)
                {
                    foreach (var ch in children)
                        q.Enqueue(ch);
                }
            }

            return false;
        }

        private static void BumpRestrictionTooltipFadeTail()
        {
            try
            {
                restrictionTooltipTintFadeTailUntilTime = Time.time + PopupTooltipFadeTailSeconds;
            }
            catch
            {
                restrictionTooltipTintFadeTailUntilTime = float.MaxValue;
            }
        }

        /// <summary>
        /// Popup controller is typically a singleton; do not rely on reference identity. Extend tint while fading after content fields clear.
        /// </summary>
        private static bool ShouldApplyRestrictionTooltipTint(object popupInstance)
        {
            if (PopupShowsRestrictionMessage(popupInstance))
                return true;

            if (IsNonRestrictionTooltipReplacementShowing(popupInstance))
                return false;

            if (Time.time >= restrictionTooltipTintFadeTailUntilTime)
                return false;

            if (TryGetPopupTextAlpha(popupInstance, out var alpha))
                return alpha > PopupTooltipTextAlphaFadeDone;

            return true;
        }

        /// <summary>Another tooltip filled the binding while our restriction message may still be visible one frame.</summary>
        private static bool IsNonRestrictionTooltipReplacementShowing(object popup)
        {
            var s = GetMemberValue(popup, "tooltipText", "TooltipText") as string;
            if (!string.IsNullOrEmpty(s) && s.IndexOf(RestrictionTooltipBodyMarker, StringComparison.OrdinalIgnoreCase) < 0)
                return true;

            var immediate = GetMemberValue(popup, "immediateTip", "ImmediateTip");
            if (immediate != null)
            {
                var txt = GetMemberValue(immediate, "Text", "text") as string;
                if (!string.IsNullOrEmpty(txt) && txt.IndexOf(RestrictionTooltipBodyMarker, StringComparison.OrdinalIgnoreCase) < 0)
                    return true;
            }

            return false;
        }

        private static bool TryGetPopupTextAlpha(object popup, out float alpha)
        {
            alpha = 0f;
            if (popup == null)
                return false;
            try
            {
                var t = popup.GetType();
                foreach (var name in new[] { "textAlphaCurrent", "TextAlphaCurrent" })
                {
                    var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (p != null && p.PropertyType == typeof(float))
                    {
                        alpha = (float)p.GetValue(popup, null);
                        return true;
                    }

                    var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f != null && f.FieldType == typeof(float))
                    {
                        alpha = (float)f.GetValue(popup);
                        return true;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        /// <summary>BFS controller tree + GameObject UILabel tree; effect colors for NGUI outline.</summary>
        private static void TintPopupTooltipHierarchy(object rootController)
        {
            var red = TooltipRestrictionAmber;
            var q = new Queue<object>();
            q.Enqueue(rootController);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                if (c == null)
                    continue;

                var go = RestrictionLabelColor.GetViewGameObject(c);
                if (go != null)
                    RestrictionLabelColor.SetLabelColorOnEntry(go, red, includeEffectColors: true);

                var children = GetMemberValue(c, "children", "Children") as IList;
                if (children != null)
                {
                    foreach (var ch in children)
                        q.Enqueue(ch);
                }
            }
        }

        /// <summary>
        /// Shows the restriction (amber) tooltip to the local player using the game's tooltip API.
        /// </summary>
        /// <param name="localPlayer">EntityPlayerLocal (or null to fall back to log and try GetLocalPlayer).</param>
        /// <param name="itemDisplayName">Display name for the item/block/vehicle (e.g. "Forge", "Bicycle").</param>
        /// <param name="craftingSkillDisplayName">Display name for the skill (e.g. "Workstations", "Vehicles").</param>
        /// <param name="playerLevel">Player's current level in that skill.</param>
        /// <param name="requiredLevel">Required level to use the item.</param>
        internal static void ShowRestrictionPopup(object localPlayer, string itemDisplayName, string craftingSkillDisplayName, int playerLevel, int requiredLevel)
        {
            var line1 = RestrictionTooltipBodyMarker + " " + (itemDisplayName ?? "this item");
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
                    BumpRestrictionTooltipFadeTail();
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

        /// <summary>
        /// Shows the same denied tooltip as workstations when a blocked inventory/hotbar/equip drag or key action applies to this stack.
        /// Dedupes burst calls (same item type within a short window, e.g. Equip UI + EquipItem).
        /// </summary>
        internal static void ShowRestrictionPopupForBlockedItemStack(ItemStack stack)
        {
            try
            {
                if (stack == null || stack.IsEmpty())
                    return;
                if (!RestrictionHelper.IsItemRestricted(stack))
                    return;

                var iv = RestrictionHelper.GetItemValue(stack);
                var ic = iv?.ItemClass;
                if (ic == null)
                    return;

                var skillGroup = GameReflection.GetCraftingSkillGroup(ic, iv);
                if (string.IsNullOrWhiteSpace(skillGroup))
                    return;

                var mapKey = GameReflection.GetItemClassNameForMap(ic, iv) ?? "";
                var now = GetNowForBlockedPopupDedupe();
                if (now - s_lastBlockedInventoryPopupRealtime < BlockedInventoryPopupDedupeSeconds
                    && string.Equals(mapKey, s_lastBlockedInventoryPopupMapKey, StringComparison.Ordinal))
                    return;
                s_lastBlockedInventoryPopupRealtime = now;
                s_lastBlockedInventoryPopupMapKey = mapKey;

                var player = GameReflection.GetLocalPlayer();
                if (player == null)
                    return;

                var requiredLevel = GameReflection.GetRequiredLevelForItem(ic, iv);
                var playerLevel = GameReflection.GetPlayerCraftingLevel(player, skillGroup);
                var itemName = TryGetLocalizedItemDisplayName(ic);
                var skillLabel = HumanizeSkillGroupLabel(skillGroup);

                ShowRestrictionPopup(player, itemName, skillLabel, playerLevel, requiredLevel);
            }
            catch
            {
                // ignored
            }
        }

        private static float s_lastBlockedInventoryPopupRealtime;
        private static string s_lastBlockedInventoryPopupMapKey = "";
        private const float BlockedInventoryPopupDedupeSeconds = 0.35f;

        /// <summary>
        /// Mock Time may omit realtimeSinceStartup; reflection keeps hermetic builds working.
        /// </summary>
        private static float GetNowForBlockedPopupDedupe()
        {
            try
            {
                var timeType = typeof(UnityEngine.Time);
                var realtime = timeType.GetProperty("realtimeSinceStartup", BindingFlags.Static | BindingFlags.Public);
                if (realtime != null && realtime.PropertyType == typeof(float))
                    return (float)realtime.GetValue(null, null);
                var tt = timeType.GetProperty("time", BindingFlags.Static | BindingFlags.Public);
                if (tt != null && tt.PropertyType == typeof(float))
                    return (float)tt.GetValue(null, null);
            }
            catch
            {
                // ignored
            }

            return 0f;
        }

        private static string TryGetLocalizedItemDisplayName(ItemClass itemClass)
        {
            if (itemClass == null)
                return "this item";
            try
            {
                var t = itemClass.GetType();
                var m = t.GetMethod("GetLocalizedItemName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (m != null)
                {
                    var r = m.Invoke(itemClass, null) as string;
                    if (!string.IsNullOrWhiteSpace(r))
                        return r.Trim();
                }

                foreach (var name in new[] { "LocalizedName", "Name" })
                {
                    var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null && p.PropertyType == typeof(string))
                    {
                        var r = p.GetValue(itemClass, null) as string;
                        if (!string.IsNullOrWhiteSpace(r))
                            return r.Trim();
                    }
                }
            }
            catch
            {
                // ignored
            }

            return "this item";
        }

        private static string HumanizeSkillGroupLabel(string skillGroup)
        {
            if (string.IsNullOrWhiteSpace(skillGroup))
                return "Skill";
            var s = skillGroup.Trim();
            if (s.IndexOf(' ') >= 0)
                return s;
            if (string.Equals(s, "Tools", StringComparison.OrdinalIgnoreCase))
                return "Harvesting Tools";

            var sb = new StringBuilder(s.Length + 4);
            sb.Append(s[0]);
            for (var i = 1; i < s.Length; i++)
            {
                if (char.IsUpper(s[i]) && char.IsLetter(s[i - 1]))
                    sb.Append(' ');
                sb.Append(s[i]);
            }

            return sb.ToString();
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
            // Do not stop early: TryTintXUiToolTipController used to return true whenever a view existed,
            // before body text/widgets were ready — vanilla also assigns color after our first pass.
            for (var i = 0; i < 24; i++)
            {
                TryApplyRestrictionTooltipLabelRed(player);
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
                var red = TooltipRestrictionAmber;
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

                any |= TryGlobalRestrictionLabelScanForPlayer(player);

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
            var go = RestrictionLabelColor.GetViewGameObject(toolTipCtrl);
            if (go != null)
            {
                RestrictionLabelColor.SetLabelColorOnEntry(go, red, includeEffectColors: true);
                return true;
            }

            var label = GetMemberValue(toolTipCtrl, "label", "Label");
            if (label == null)
                return false;
            foreach (var propName in new[] { "Color", "color" })
            {
                var p = label.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null && p.PropertyType == typeof(Color) && p.CanWrite)
                {
                    try { p.SetValue(label, red, null); } catch { }
                    break;
                }
            }

            var uiLabel = GetMemberValue(label, "Label", "label");
            if (uiLabel != null)
                TrySetUILabelDeniedColors(uiLabel, red);
            return true;
        }

        private static void TrySetUILabelDeniedColors(object uiLabel, Color red)
        {
            try
            {
                var t = uiLabel.GetType();
                foreach (var name in new[] { "color", "mColor", "Color" })
                {
                    var prop = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(Color) && prop.CanWrite)
                    {
                        prop.SetValue(uiLabel, red, null);
                        break;
                    }
                }

                var fx = new Color(red.r * 0.55f, red.g * 0.55f, red.b * 0.55f, 1f);
                foreach (var name in new[] { "effectColor", "EffectColor" })
                {
                    var prop = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(Color) && prop.CanWrite)
                    {
                        prop.SetValue(uiLabel, fx, null);
                        break;
                    }
                }

                foreach (var name in new[] { "applyGradient", "ApplyGradient" })
                {
                    var prop = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(bool) && prop.CanWrite)
                    {
                        try { prop.SetValue(uiLabel, false, null); } catch { }
                        break;
                    }
                }

                foreach (var name in new[] { "gradientTop", "GradientTop", "gradientBottom", "GradientBottom" })
                {
                    var prop = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(Color) && prop.CanWrite)
                    {
                        try { prop.SetValue(uiLabel, red, null); } catch { }
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Finds UILabel / TMP instances whose text contains the restriction marker (tooltip may not live under XUi controller views).
        /// </summary>
        private static bool TryGlobalRestrictionLabelScanForPlayer(object player)
        {
            var marker = RestrictionTooltipBodyMarker;
            var red = TooltipRestrictionAmber;
            var asm = typeof(GameManager).Assembly;
            var uiLabelType = asm.GetType("UILabel");
            var any = false;

            var root = TryGetLocalPlayerUiRootGo(player);
            if (root != null && uiLabelType != null)
            {
                foreach (var comp in GetComponentsInChildrenObjects(root, uiLabelType))
                {
                    if (!ComponentTextContainsMarker(comp, marker))
                        continue;
                    TrySetUILabelDeniedColors(comp, red);
                    any = true;
                }

                foreach (var tmpType in ResolveTmpTextTypes())
                {
                    if (tmpType == null)
                        continue;
                    foreach (var comp in GetComponentsInChildrenObjects(root, tmpType))
                    {
                        if (!ComponentTextContainsMarker(comp, marker))
                            continue;
                        TrySetGenericTextComponentColor(comp, red);
                        any = true;
                    }
                }

                var unityUiTextType = Type.GetType("UnityEngine.UI.Text, UnityEngine.UI");
                if (unityUiTextType != null)
                {
                    foreach (var comp in GetComponentsInChildrenObjects(root, unityUiTextType))
                    {
                        if (!ComponentTextContainsMarker(comp, marker))
                            continue;
                        TrySetGenericTextComponentColor(comp, red);
                        any = true;
                    }
                }
            }

            if (!any && uiLabelType != null)
            {
                foreach (var obj in FindAllObjectsOfType(uiLabelType))
                {
                    if (obj == null)
                        continue;
                    var go = GetGameObjectFromUnityEngineObject(obj);
                    if (go != null && !IsGameObjectActiveInHierarchy(go))
                        continue;
                    if (!ComponentTextContainsMarker(obj, marker))
                        continue;
                    TrySetUILabelDeniedColors(obj, red);
                    any = true;
                }
            }

            return any;
        }

        private static GameObject TryGetLocalPlayerUiRootGo(object player)
        {
            if (player == null)
                return null;
            try
            {
                var lpui = GetMemberValue(player, "PlayerUI", "playerUI");
                if (lpui == null)
                    return null;
                return GetMemberValue(lpui, "gameObject") as GameObject;
            }
            catch
            {
                return null;
            }
        }

        private static List<object> GetComponentsInChildrenObjects(GameObject root, Type componentType)
        {
            var list = new List<object>();
            if (root == null || componentType == null)
                return list;
            try
            {
                var mi = typeof(GameObject).GetMethod("GetComponentsInChildren", new[] { typeof(Type), typeof(bool) });
                var raw = mi?.Invoke(root, new object[] { componentType, true });
                if (raw is Array arr)
                {
                    foreach (var c in arr)
                    {
                        if (c != null)
                            list.Add(c);
                    }
                }
            }
            catch
            {
                // ignored
            }

            return list;
        }

        private static bool ComponentTextContainsMarker(object component, string marker)
        {
            if (component == null || string.IsNullOrEmpty(marker))
                return false;
            try
            {
                var t = component.GetType();
                foreach (var name in new[] { "text", "Text" })
                {
                    var prop = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop == null || prop.PropertyType != typeof(string))
                        continue;
                    var s = prop.GetValue(component, null) as string;
                    if (!string.IsNullOrEmpty(s) && s.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }

        private static void TrySetGenericTextComponentColor(object comp, Color red)
        {
            try
            {
                var t = comp.GetType();
                foreach (var name in new[] { "color", "Color" })
                {
                    var prop = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null && prop.PropertyType == typeof(Color) && prop.CanWrite)
                    {
                        prop.SetValue(comp, red, null);
                        return;
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private static Type[] ResolveTmpTextTypes()
        {
            var list = new List<Type>();
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var name in new[] { "TMPro.TextMeshProUGUI", "TMPro.TMP_Text" })
                        {
                            var t = asm.GetType(name, false);
                            if (t != null)
                                list.Add(t);
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch
            {
                // ignored
            }

            return list.Count > 0 ? list.ToArray() : Array.Empty<Type>();
        }

        private static Object[] FindAllObjectsOfType(Type type)
        {
            if (type == null)
                return Array.Empty<Object>();
            foreach (var owner in new[] { typeof(Object), Type.GetType("UnityEngine.Resources, UnityEngine.CoreModule") })
            {
                if (owner == null)
                    continue;
                try
                {
                    var mi = owner.GetMethod("FindObjectsOfTypeAll", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                        new[] { typeof(Type) }, null);
                    if (mi == null)
                        continue;
                    var r = mi.Invoke(null, new object[] { type }) as Object[];
                    if (r != null)
                        return r;
                }
                catch
                {
                    // ignored
                }
            }

            return Array.Empty<Object>();
        }

        private static GameObject GetGameObjectFromUnityEngineObject(object unityObj)
        {
            if (unityObj == null)
                return null;
            try
            {
                var p = unityObj.GetType().GetProperty("gameObject", BindingFlags.Public | BindingFlags.Instance);
                return p?.GetValue(unityObj, null) as GameObject;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsGameObjectActiveInHierarchy(GameObject go)
        {
            if (go == null)
                return false;
            try
            {
                var p = typeof(GameObject).GetProperty("activeInHierarchy");
                if (p != null && p.PropertyType == typeof(bool))
                    return (bool)p.GetValue(go);
            }
            catch
            {
                // ignored
            }

            return true;
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
