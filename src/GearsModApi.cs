using System;
using System.Collections.Generic;
using GearsAPI.Settings;
using GearsAPI.Settings.Global;
using GearsAPI.Settings.World;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Soft-dep Gears bridge. Discovered by Gears when present; ignored when Gears is not installed.
    /// World settings = crafting-skill restriction toggles (synced). Global = DebugMode (per client).
    /// </summary>
    public class GearsModApi : IGearsModApi
    {
        private const string WorldCategory = "CraftingSkills";
        private const string GlobalTab = "Client";
        private const string GlobalCategoryDebug = "Debug";

        private static IModWorldSettings worldSettings;
        private static bool globalListenersAttached;

        public void InitMod(IGearsMod modInstance)
        {
            SafeLog("GearsModApi.InitMod");
        }

        public void OnGlobalSettingsLoaded(IModGlobalSettings modSettings)
        {
            if (modSettings == null)
                return;

            try
            {
                ApplyGlobalFromGears(modSettings);
                if (!globalListenersAttached)
                {
                    AttachGlobalListeners(modSettings);
                    globalListenersAttached = true;
                }
                ModConfig.Instance.Save();
                SafeLog("Gears global settings applied");
            }
            catch (Exception ex)
            {
                SafeLog($"Gears OnGlobalSettingsLoaded failed: {ex.Message}");
            }
        }

        public void OnWorldSettingsLoaded(IModWorldSettings settings)
        {
            if (settings == null)
                return;

            try
            {
                worldSettings = settings;
                ApplyWorldFromGears(settings);
                ModConfig.Instance.Save();
                SafeLog("Gears world settings applied");
            }
            catch (Exception ex)
            {
                SafeLog($"Gears OnWorldSettingsLoaded failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Re-read World CurrentValues into ModConfig. World settings have no OnSettingChanged
        /// in GearsAPI; call before restriction checks so host mid-session UI edits apply.
        /// </summary>
        public static void SyncWorldSettingsIfLoaded()
        {
            if (worldSettings == null)
                return;

            try
            {
                ApplyWorldFromGears(worldSettings);
            }
            catch (Exception ex)
            {
                SafeLog($"Gears SyncWorldSettingsIfLoaded failed: {ex.Message}");
            }
        }

        private static void ApplyWorldFromGears(IModWorldSettings settings)
        {
            var category = settings.GetCategory(WorldCategory);
            if (category == null)
                return;

            var config = ModConfig.Instance;
            var skills = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in ModConfig.CraftingSkillNames)
            {
                var current = config.GetSkillRestrictionEnabled(name);
                skills[name] = ReadWorldBool(category, name, current);
            }

            config.ApplyWorldSettings(skills);
        }

        private static void ApplyGlobalFromGears(IModGlobalSettings settings)
        {
            var tab = settings.GetTab(GlobalTab);
            if (tab == null)
                return;

            var debugCat = tab.GetCategory(GlobalCategoryDebug);
            if (debugCat == null)
                return;

            var config = ModConfig.Instance;
            var debugMode = ReadGlobalBool(debugCat, "DebugMode", config.DebugMode);
            config.ApplyGlobalSettings(debugMode);
        }

        private static void AttachGlobalListeners(IModGlobalSettings settings)
        {
            foreach (var setting in settings.GetAllGlobalSettings())
            {
                if (setting == null)
                    continue;
                setting.OnSettingChanged -= OnGlobalSettingChanged;
                setting.OnSettingChanged += OnGlobalSettingChanged;
            }
        }

        private static void OnGlobalSettingChanged(IGlobalModSetting setting, string newValue)
        {
            try
            {
                var config = ModConfig.Instance;
                var name = setting?.Name ?? "";
                var category = setting?.Category?.Name ?? "";

                if (category == GlobalCategoryDebug && name == "DebugMode")
                {
                    config.ApplyGlobalSettings(ModConfig.ParseBoolSetting(newValue, config.DebugMode));
                    config.Save();
                }
            }
            catch (Exception ex)
            {
                SafeLog($"Gears OnGlobalSettingChanged failed: {ex.Message}");
            }
        }

        private static bool ReadWorldBool(IWorldModSettingsCategory category, string name, bool defaultValue)
        {
            var setting = category.GetSetting(name);
            if (setting == null)
                return defaultValue;
            return ModConfig.ParseBoolSetting(setting.CurrentValue, defaultValue);
        }

        private static bool ReadGlobalBool(IGlobalModSettingsCategory category, string name, bool defaultValue)
        {
            var setting = category.GetSetting(name) as IGlobalValueSetting;
            if (setting == null)
                return defaultValue;
            return ModConfig.ParseBoolSetting(setting.CurrentValue, defaultValue);
        }

        private static void SafeLog(string message)
        {
            try
            {
                UnityEngine.Debug.Log($"[LimitByCraftingSkillMod] {message}");
            }
            catch
            {
                // Unity not available (test environment)
            }
        }
    }
}
