namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Postfix on XUiC_PopupToolTip.DisplayTooltipText — runs after the game assigns tooltip text/binding,
    /// so we can tint UILabel (including effect/outline) for restriction messages.
    /// </summary>
    internal static class PopupToolTipDisplayTooltipTextPatch
    {
        public static void Postfix(object __instance)
        {
            RestrictionFeedback.OnPopupToolTipDisplayed(__instance);
        }
    }
}
