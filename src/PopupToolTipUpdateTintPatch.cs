namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Postfix XUiC_PopupToolTip.Update — vanilla refreshes tooltip styling each frame; re-apply restriction amber after it runs.
    /// </summary>
    internal static class PopupToolTipUpdateTintPatch
    {
        public static void Postfix(object __instance, float _dt)
        {
            RestrictionFeedback.OnPopupToolTipUpdate(__instance);
        }
    }
}
