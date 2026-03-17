using System.Reflection;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// When the local player's crafting skill level changes (addProgressionCurrency), sets RestrictionColorsDirty
    /// so open inventory/equipment grids refresh their restriction label colors on next Update.
    /// Applied manually in ModApi from the game assembly (Progression type not in mock).
    /// </summary>
    internal static class ProgressionLevelUpPatch
    {
        public static void Postfix(object __instance, int _currencyAmount, object _pv)
        {
            if (_pv == null) return;
            if (!GameReflection.IsCraftingProgressionValue(_pv)) return;
            try
            {
                var progression = __instance;
                if (progression == null) return;
                var parent = GetParent(progression);
                if (parent == null) return;
                var localPlayer = GameReflection.GetLocalPlayer();
                if (localPlayer == null || !ReferenceEquals(parent, localPlayer)) return;
                RestrictionLabelColor.RestrictionColorsDirty = true;
            }
            catch
            {
                // Non-critical
            }
        }

        private static object GetParent(object progression)
        {
            if (progression == null) return null;
            var t = progression.GetType();
            var prop = t.GetProperty("parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null) return prop.GetValue(progression, null);
            var field = t.GetField("parent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(progression);
        }
    }
}
