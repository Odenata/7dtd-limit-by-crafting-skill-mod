using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    public class ModApi : IModApi
    {
        public void InitMod(Mod modInstance)
        {
            SafeLog("Loading LimitByCraftingSkillMod");

            var config = ModConfig.Instance;
            SafeLog($"Config loaded: DebugMode={config.DebugMode}");

            try
            {
                var harmony = new Harmony("com.learninggrounds.limitbycraftingskillmod");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                SafeLog("Harmony patches applied");
            }
            catch (Exception ex)
            {
                SafeLog($"Harmony patch failed: {ex.Message}");
                SafeLog(ex.StackTrace ?? "");
            }

            SafeLog("LimitByCraftingSkillMod loaded successfully");
        }

        private static void SafeLog(string message)
        {
            try
            {
                UnityEngine.Debug.Log(message);
            }
            catch
            {
                // Unity not available (test environment)
            }
        }
    }
}
