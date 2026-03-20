using System;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Debug-only: log GUI window names opened via GUIWindowManager.Open.
    /// Useful to identify what UI Chemistry Station uses when workstation hooks don't fire.
    /// </summary>
    internal static class GUIWindowManagerOpenNameLogPatch
    {
        private const string ChemistryStationWindowName = "workstation_chemistryStation";
        private const string ChemistryStationMapKey = "chemistryStation";

        // No logging in final build: this patch is only responsible for enforcing restrictions.

        private static int GetChemistryStationRequiredLevel()
        {
            int lvl;
            if (ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelOverride(ChemistryStationMapKey, out lvl))
                return lvl;
            if (ClassNameToCraftingSkillMapLoader.TryGetRequiredLevelMin(ChemistryStationMapKey, out lvl))
                return lvl;
            return 0;
        }

        private static bool MaybeBlockChemistryStationOpen(string windowName)
        {
            if (string.IsNullOrWhiteSpace(windowName))
                return true;
            if (!string.Equals(windowName, ChemistryStationWindowName, StringComparison.OrdinalIgnoreCase))
                return true;
            if (ModConfig.Instance == null || !ModConfig.Instance.IsRestrictionEnabledForSkill("Workstations"))
                return true;

            var requiredLevel = GetChemistryStationRequiredLevel();
            if (requiredLevel <= 0)
                return true;

            var player = GameReflection.GetLocalPlayer() as EntityAlive;
            if (player == null)
                return true;

            var playerLevel = GameReflection.GetPlayerCraftingLevel(player, "Workstations");
            if (!LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
                return true;

            // Use a friendly hardcoded display name; map key may not match UI capitalization.
            RestrictionFeedback.ShowRestrictionPopup(player, "Chemistry Station", "Workstations", playerLevel, requiredLevel);
            return false;
        }

        // Prefix methods (wired in via ModApi reflection patching).
        public static bool PrefixOpen_String_Bool_Bool_Bool(string _windowName, bool _bModal, bool _bIsNotEscClosable, bool _bCloseAllOpenWindows)
        {
            return MaybeBlockChemistryStationOpen(_windowName);
        }

        public static bool PrefixOpen_String_Int_Int_Bool_Bool(string _windowName, int _x, int _y, bool _bModal, bool _bIsNotEscClosable)
        {
            return MaybeBlockChemistryStationOpen(_windowName);
        }

        public static bool PrefixOpenIfNotOpen_String_Bool_Bool_Bool(string _windowName, bool _bModal, bool _bIsNotEscClosable, bool _bCloseAllOpenWindows)
        {
            return MaybeBlockChemistryStationOpen(_windowName);
        }
    }
}

