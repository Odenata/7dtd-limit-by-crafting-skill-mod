namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Postfix on GameManager.ShowTooltip / ShowTooltipMP — we get the exact message string even when
    /// XUiC_PopupToolTip state does not match (binding, immediate tip, or non-popup path).
    /// </summary>
    internal static class GameManagerShowTooltipTintPatch
    {
        public static void Postfix(object[] __args)
        {
            if (__args == null || __args.Length < 2)
                return;
            var text = __args[1] as string;
            RestrictionFeedback.OnGameManagerShowTooltipAfter(__args[0], text);
        }
    }
}
