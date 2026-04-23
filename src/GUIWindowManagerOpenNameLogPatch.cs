using System;
using System.Reflection;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Chemistry Station often opens via <see cref="GUIWindowManager"/> before / without the same XUi hooks as forge.
    /// Uses Postfix (never Prefix+skip) so we close immediately after Open + popup — avoids client UI lock from skipping Open.
    /// </summary>
    internal static class GUIWindowManagerOpenNameLogPatch
    {
        internal const string ChemistryStationWindowName = "workstation_chemistryStation";
        private const string ChemistryStationMapKey = "chemistryStation";

        private static int GetChemistryStationRequiredLevel()
        {
            int lvl;
            if (ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelOverride(ChemistryStationMapKey, out lvl))
                return lvl;
            if (ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelMin(ChemistryStationMapKey, out lvl))
                return lvl;
            return 0;
        }

        /// <summary>Harmony Postfix for GUIWindowManager.Open(string, bool, bool, bool).</summary>
        public static void PostfixOpen_String_Bool_Bool_Bool(object __instance, string _windowName, bool _bModal, bool _bIsNotEscClosable, bool _bCloseAllOpenWindows)
        {
            AfterChemistryWindowMayHaveOpened(__instance, _windowName);
        }

        /// <summary>Harmony Postfix for GUIWindowManager.Open(string, int, int, bool, bool).</summary>
        public static void PostfixOpen_String_Int_Int_Bool_Bool(object __instance, string _windowName, int _x, int _y, bool _bModal, bool _bIsNotEscClosable)
        {
            AfterChemistryWindowMayHaveOpened(__instance, _windowName);
        }

        /// <summary>Harmony Postfix for GUIWindowManager.OpenIfNotOpen(string, bool, bool, bool).</summary>
        public static void PostfixOpenIfNotOpen_String_Bool_Bool_Bool(object __instance, string _windowName, bool _bModal, bool _bIsNotEscClosable, bool _bCloseAllOpenWindows)
        {
            AfterChemistryWindowMayHaveOpened(__instance, _windowName);
        }

        /// <summary>Harmony Postfix — some builds use SwitchVisible instead of Open for workstation panels.</summary>
        public static void PostfixSwitchVisible_String_Bool_Bool(object __instance, string _windowName, bool _bIsNotEscClosable, bool _modal)
        {
            AfterChemistryWindowMayHaveOpened(__instance, _windowName);
        }

        private static readonly object ChemistryUiDedupeLock = new object();
        private static int _lastChemistryUiCloseTicks;

        private static void AfterChemistryWindowMayHaveOpened(object guiWindowManager, string windowName)
        {
            try
            {
                if (guiWindowManager == null || string.IsNullOrWhiteSpace(windowName))
                    return;
                if (!string.Equals(windowName, ChemistryStationWindowName, StringComparison.OrdinalIgnoreCase))
                    return;
                if (ModConfig.Instance == null || !ModConfig.Instance.IsRestrictionEnabledForSkill("Workstations"))
                    return;

                var requiredLevel = GetChemistryStationRequiredLevel();
                if (requiredLevel <= 0)
                    return;

                var player = GameReflection.GetLocalPlayer() as EntityAlive;
                if (player == null)
                    return;

                var playerLevel = GameReflection.GetPlayerCraftingLevel(player, "Workstations");
                if (!LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
                    return;

                lock (ChemistryUiDedupeLock)
                {
                    var now = Environment.TickCount;
                    var dt = now - _lastChemistryUiCloseTicks;
                    if (dt >= 0 && dt < 400)
                        return;
                    _lastChemistryUiCloseTicks = now;
                }

                var closeIfOpen = guiWindowManager.GetType().GetMethod("CloseIfOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(string) }, null);
                closeIfOpen?.Invoke(guiWindowManager, new object[] { ChemistryStationWindowName });

                RestrictionFeedback.ShowRestrictionPopup(player, "Chemistry Station", "Workstations", playerLevel, requiredLevel);

                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] Chemistry Station GUI blocked (GUIWindowManager Postfix) Workstations " + playerLevel + "/" + requiredLevel);
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] GUIWindowManager chemistry Postfix: " + ex.Message);
            }
        }
    }
}
