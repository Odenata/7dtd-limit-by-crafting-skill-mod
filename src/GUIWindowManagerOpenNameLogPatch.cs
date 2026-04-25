using System;
using System.Reflection;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Many workstations open a <see cref="GUIWindowManager"/> panel named <c>workstation_{className}</c> (e.g. forge, chemistry
    /// station) where XUi hooks never resolve a <c>BlockValue</c> — close + popup from Postfix, same as chemistry.
    /// </summary>
    internal static class GUIWindowManagerOpenNameLogPatch
    {
        private const string WorkstationWindowPrefix = "workstation_";
        private const int WorkstationBlockedRecloseMs = 1200;

        /// <summary>Harmony Postfix for GUIWindowManager.Open(string, bool, bool, bool).</summary>
        public static void PostfixOpen_String_Bool_Bool_Bool(object __instance, string _windowName, bool _bModal, bool _bIsNotEscClosable, bool _bCloseAllOpenWindows)
        {
            AfterWorkstationWindowMayHaveOpened(__instance, _windowName);
        }

        /// <summary>Harmony Postfix for GUIWindowManager.Open(string, int, int, bool, bool).</summary>
        public static void PostfixOpen_String_Int_Int_Bool_Bool(object __instance, string _windowName, int _x, int _y, bool _bModal, bool _bIsNotEscClosable)
        {
            AfterWorkstationWindowMayHaveOpened(__instance, _windowName);
        }

        /// <summary>Harmony Postfix for GUIWindowManager.OpenIfNotOpen(string, bool, bool, bool).</summary>
        public static void PostfixOpenIfNotOpen_String_Bool_Bool_Bool(object __instance, string _windowName, bool _bModal, bool _bIsNotEscClosable, bool _bCloseAllOpenWindows)
        {
            AfterWorkstationWindowMayHaveOpened(__instance, _windowName);
        }

        /// <summary>Harmony Postfix — some builds use SwitchVisible instead of Open for workstation panels.</summary>
        public static void PostfixSwitchVisible_String_Bool_Bool(object __instance, string _windowName, bool _bIsNotEscClosable, bool _modal)
        {
            AfterWorkstationWindowMayHaveOpened(__instance, _windowName);
        }

        /// <summary>
        /// GUIWindowManager.Update watchdog: briefly re-close recently blocked workstation windows to prevent interact-spam bypass.
        /// </summary>
        public static void PostfixUpdate_Float(object __instance, float _dt)
        {
            TickCloseBlockedWorkstationWindows(__instance);
        }

        /// <summary>
        /// Generic postfix for GUIWindowManager methods where first arg is window name; used for overload coverage.
        /// </summary>
        public static void PostfixAny_StringFirstArg(object __instance, object[] __args)
        {
            if (__args == null || __args.Length == 0)
                return;
            if (!(__args[0] is string windowName))
                return;
            AfterWorkstationWindowMayHaveOpened(__instance, windowName);
        }

        private static readonly object WorkstationGuiDedupeLock = new object();
        private static string _lastDedupeWindow;
        private static int _lastWorkstationGuiCloseTicks;
        private static readonly object BlockedWindowsLock = new object();
        private static readonly System.Collections.Generic.Dictionary<string, int> _blockedWorkstationWindowsUntilTicks =
            new System.Collections.Generic.Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static void AfterWorkstationWindowMayHaveOpened(object guiWindowManager, string windowName)
        {
            try
            {
                if (guiWindowManager == null || string.IsNullOrWhiteSpace(windowName))
                    return;
                if (!TryResolveWorkstationMapKeyFromWindowName(windowName, out var mapKey))
                    return;

                if (ModConfig.Instance == null || !ModConfig.Instance.IsRestrictionEnabledForSkill("Workstations"))
                    return;

                var requiredLevel = GameReflection.GetWorkstationsGatedLevelForMapKey(mapKey);
                if (requiredLevel <= 0)
                    return;

                var player = GameReflection.GetLocalPlayer() as EntityAlive;
                if (player == null)
                    return;

                var playerLevel = GameReflection.GetPlayerCraftingLevel(player, "Workstations");
                if (!LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
                    return;

                var suppressPopup = false;
                lock (WorkstationGuiDedupeLock)
                {
                    var now = Environment.TickCount;
                    var dt = now - _lastWorkstationGuiCloseTicks;
                    if (string.Equals(_lastDedupeWindow, windowName, StringComparison.Ordinal) &&
                        dt >= 0 && dt < 400)
                        suppressPopup = true;
                    _lastDedupeWindow = windowName;
                    _lastWorkstationGuiCloseTicks = now;
                }

                var closeIfOpen = guiWindowManager.GetType().GetMethod("CloseIfOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(string) }, null);
                closeIfOpen?.Invoke(guiWindowManager, new object[] { windowName });
                MarkBlockedWindowForReclose(windowName);

                if (!suppressPopup)
                {
                    var display = WorkstationMapKeyToDisplayLabel(mapKey);
                    RestrictionFeedback.ShowRestrictionPopup(player, display, "Workstations", playerLevel, requiredLevel);
                }

                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Workstation GUI blocked (GUIWindowManager Postfix) " + WorkstationMapKeyToDisplayLabel(mapKey) + " Workstations " + playerLevel + "/" + requiredLevel + (suppressPopup ? " [dedupe_popup]" : ""));
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] GUIWindowManager workstation Postfix: " + ex.Message);
            }
        }

        private static void MarkBlockedWindowForReclose(string windowName)
        {
            if (string.IsNullOrWhiteSpace(windowName))
                return;
            lock (BlockedWindowsLock)
            {
                _blockedWorkstationWindowsUntilTicks[windowName] = Environment.TickCount + WorkstationBlockedRecloseMs;
            }
        }

        private static void TickCloseBlockedWorkstationWindows(object guiWindowManager)
        {
            if (guiWindowManager == null)
                return;
            string[] toClose;
            lock (BlockedWindowsLock)
            {
                if (_blockedWorkstationWindowsUntilTicks.Count == 0)
                    return;
                var now = Environment.TickCount;
                var active = new System.Collections.Generic.List<string>();
                var expired = new System.Collections.Generic.List<string>();
                foreach (var kv in _blockedWorkstationWindowsUntilTicks)
                {
                    if (now <= kv.Value)
                        active.Add(kv.Key);
                    else
                        expired.Add(kv.Key);
                }
                foreach (var k in expired)
                    _blockedWorkstationWindowsUntilTicks.Remove(k);
                if (active.Count == 0)
                    return;
                toClose = active.ToArray();
            }

            var closeIfOpen = guiWindowManager.GetType().GetMethod(
                "CloseIfOpen",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string) },
                null);
            if (closeIfOpen == null)
                return;
            foreach (var windowName in toClose)
            {
                try
                {
                    closeIfOpen.Invoke(guiWindowManager, new object[] { windowName });
                }
                catch
                {
                    // best effort
                }
            }
        }

        private static bool TryResolveWorkstationMapKeyFromWindowName(string windowName, out string mapKey)
        {
            mapKey = null;
            if (string.IsNullOrWhiteSpace(windowName))
                return false;

            var name = windowName.Trim();
            if (name.StartsWith(WorkstationWindowPrefix, StringComparison.OrdinalIgnoreCase) &&
                name.Length > WorkstationWindowPrefix.Length)
            {
                mapKey = name.Substring(WorkstationWindowPrefix.Length);
                return !string.IsNullOrWhiteSpace(mapKey);
            }

            var compact = name.Replace("_", "").Replace("-", "").Replace(" ", "").ToLowerInvariant();
            if (compact.Contains("dewcollector"))
            {
                mapKey = "cntDewCollector";
                return true;
            }
            if (compact.Contains("apiary"))
            {
                mapKey = "cntApiary";
                return true;
            }

            return false;
        }

        private static string WorkstationMapKeyToDisplayLabel(string mapKey)
        {
            if (string.IsNullOrWhiteSpace(mapKey)) return "workstation";
            if (string.Equals(mapKey, "chemistryStation", StringComparison.OrdinalIgnoreCase))
                return "Chemistry Station";
            if (string.Equals(mapKey, "cementMixer", StringComparison.OrdinalIgnoreCase))
                return "Cement mixer";
            if (string.Equals(mapKey, "dewCollector", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mapKey, "cntDewCollector", StringComparison.OrdinalIgnoreCase))
                return "Dew collector";
            if (string.Equals(mapKey, "apiary", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mapKey, "cntApiary", StringComparison.OrdinalIgnoreCase))
                return "Apiary";
            return char.ToUpperInvariant(mapKey[0]) + (mapKey.Length > 1 ? mapKey.Substring(1) : "");
        }
    }
}
