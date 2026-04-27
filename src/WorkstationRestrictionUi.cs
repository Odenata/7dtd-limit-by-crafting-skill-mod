using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace LimitByCraftingSkillMod
{
    /// <summary>
    /// Shared workstation UI restriction: after vanilla UI code runs, close and show popup if Workstations level is too low.
    /// Hooks: CraftingWindowGroup.OnOpen (filtered to workstation windows), <c>WorkstationWindowGroup.SetTileEntity</c>, and
    /// <c>XUiC_DewCollectorWindowGroup.SetTileEntity(TileEntityCollector)</c> (Dew/Apiary share <c>window_group</c> <c>dewcollector</c>;
    /// <c>OnOpen</c> is too early — the tile entity is bound in <c>SetTileEntity</c>).
    /// </summary>
    internal static class WorkstationRestrictionUi
    {
        /// <summary>Suppress duplicate close+popup when SetTileEntity / OnOpen / workstationOpened fire for the same open.</summary>
        private static readonly object DedupeLock = new object();
        private static WeakReference<object> _lastDedupeKey;
        private static int _lastDedupeTicks;

        private static readonly object WorkstationWindowGroupTypeLock = new object();
        private static Type _cachedWorkstationWindowGroupType;

        private static Type GetWorkstationWindowGroupType()
        {
            lock (WorkstationWindowGroupTypeLock)
            {
                if (_cachedWorkstationWindowGroupType != null)
                    return _cachedWorkstationWindowGroupType;
                _cachedWorkstationWindowGroupType = typeof(Equipment).Assembly.GetType("XUiC_WorkstationWindowGroup");
                return _cachedWorkstationWindowGroupType;
            }
        }

        /// <summary>
        /// Postfix for <see cref="XUiC_CraftingWindowGroup"/>.OnOpen — attempts workstation restriction for any crafting window
        /// that can resolve a <see cref="BlockValue"/> (forge, workbench, cement mixer, etc.).
        /// </summary>
        /// <remarks>
        /// Do not require <c>XUiC_WorkstationWindowGroup</c>: vanilla forge UI can sit on <c>CraftingWindowGroup</c> subclasses that are
        /// not assignable from <c>WorkstationWindowGroup</c>; <see cref="TileEntityForge"/> is not a <c>TileEntityWorkstation</c>, so
        /// <c>SetTileEntity</c> / <c>GameManager.workstationOpened</c> hooks alone miss it. Chemistry still has an extra
        /// <see cref="GUIWindowManagerWorkstationWindowPatch"/> for its window id.
        /// </remarks>
        public static void PostfixFilteredCraftingOnOpen(object __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                TryEnforceRestrictedWorkstation(__instance, null, null, "OnOpen(CraftingFiltered)");
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] WorkstationRestrictionUi PostfixFilteredCraftingOnOpen: " + ex.Message);
            }
        }

        /// <summary>
        /// Postfix for XUiC_WorkstationWindowGroup.OnOpen when that type declares its own override.
        /// </summary>
        public static void PostfixWorkstationWindowGroupOnOpen(object __instance)
        {
            try
            {
                if (__instance == null)
                    return;
                TryEnforceRestrictedWorkstation(__instance, null, null, "OnOpen(WSGroup)");
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] WorkstationRestrictionUi PostfixWorkstationWindowGroupOnOpen: " + ex.Message);
            }
        }

        /// <summary>
        /// Postfix for XUiC_DewCollectorWindowGroup.OnOpen. Collector windows can reopen without rebinding TE each time, so
        /// keep an OnOpen gate in addition to SetTileEntity.
        /// </summary>
        public static void PostfixDewCollectorWindowGroupOnOpen(object __instance)
        {
            try
            {
                if (__instance == null)
                    return;
                TryEnforceRestrictedWorkstation(__instance, null, null, "OnOpen(DewCollectorGroup)");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LimitByCraftingSkill] WorkstationRestrictionUi PostfixDewCollectorWindowGroupOnOpen: " + ex);
            }
        }

        /// <summary>
        /// Postfix for XUiC_WorkstationWindowGroup.SetTileEntity(TileEntityWorkstation) — runs whenever the TE is bound (strong signal).
        /// </summary>
        public static void PostfixSetTileEntity(object __instance, object _te)
        {
            try
            {
                if (__instance == null || _te == null)
                    return;
                TryEnforceRestrictedWorkstation(__instance, _te, null, "SetTileEntity");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LimitByCraftingSkill] WorkstationRestrictionUi PostfixSetTileEntity: " + ex);
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] WorkstationRestrictionUi PostfixSetTileEntity: " + ex.Message);
            }
        }

        /// <summary>
        /// Fallback when UI controller hooks miss: GameManager notifies when a workstation TE is opened for a local UI.
        /// </summary>
        public static void PostfixGameManagerWorkstationOpened(object _te, object _playerUI)
        {
            try
            {
                if (_te == null)
                    return;
                TryEnforceRestrictedWorkstation(null, _te, null, "GameManager.workstationOpened", _playerUI);
            }
            catch (Exception ex)
            {
                if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] WorkstationRestrictionUi PostfixGameManagerWorkstationOpened: " + ex.Message);
            }
        }

        private static void TryEnforceRestrictedWorkstation(object workstationWindowInstance, object tileEntityWorkstation, object blockValueDirect, string traceTag, object explicitPlayerUi = null)
        {
            if (ModConfig.Instance == null || !ModConfig.Instance.IsRestrictionEnabledForSkill("Workstations"))
                return;

            object blockValue = blockValueDirect;
            if (blockValue == null && tileEntityWorkstation != null)
                blockValue = GameReflection.GetBlockValueFromTileEntity(tileEntityWorkstation);
            if (blockValue == null)
                blockValue = ResolveWorkstationBlockValueFromWindow(workstationWindowInstance);

            if (blockValue == null)
            {
                if (ModConfig.Instance.DebugMode)
                    ModApi.DebugLog("[LimitByCraftingSkill] WorkstationRestrictionUi: no BlockValue (" + traceTag + "), skip enforce.");
                return;
            }

            var requiredLevel = GameReflection.GetRequiredLevelForWorkstationBlock(blockValue);
            if (requiredLevel <= 0)
                return;

            var player = GameReflection.GetLocalPlayer() as EntityAlive;
            if (player == null)
                return;

            var playerLevel = GameReflection.GetPlayerCraftingLevel(player, "Workstations");
            if (!LimitByCraftingSkillLogic.IsRestricted(playerLevel, requiredLevel, true))
                return;

            var suppressPopup = false;
            object dedupeKey = workstationWindowInstance ?? tileEntityWorkstation;
            lock (DedupeLock)
            {
                object prev = null;
                _lastDedupeKey?.TryGetTarget(out prev);
                var now = Environment.TickCount;
                if (dedupeKey != null && prev != null && ReferenceEquals(prev, dedupeKey))
                {
                    var delta = now - _lastDedupeTicks;
                    if (delta >= 0 && delta < 450)
                        suppressPopup = true;
                }

                _lastDedupeKey = dedupeKey != null ? new WeakReference<object>(dedupeKey) : null;
                _lastDedupeTicks = now;
            }

            var playerUi = explicitPlayerUi ?? TryGetLocalPlayerUiFromController(workstationWindowInstance);
            var windowId = TryResolveWorkstationWindowIdFromBlock(blockValue);

            CloseWorkstationAfterUnauthorizedOpen(workstationWindowInstance, playerUi, windowId);

            if (!suppressPopup)
            {
                var block = GameReflection.GetBlockFromBlockValue(blockValue);
                var blockNameForMap = GameReflection.GetBlockNameForMap(block);
                var displayName = string.IsNullOrWhiteSpace(blockNameForMap) ? "workstation" : blockNameForMap;
                RestrictionFeedback.ShowRestrictionPopup(player, displayName, "Workstations", playerLevel, requiredLevel);
            }

            if (ModConfig.Instance.DebugMode)
                ModApi.DebugLog("[LimitByCraftingSkill] Workstation restricted (" + traceTag + ") Workstations " + playerLevel + "/" + requiredLevel);
        }

        private static object ResolveWorkstationBlockValueFromWindow(object window)
        {
            if (window == null)
                return null;
            try
            {
                var bv = GetWorkstationBlockFromWindow(window);
                if (bv != null)
                    return bv;

                var fromTe = TryGetBlockValueFromTileEntityFieldOnController(window);
                if (fromTe != null)
                    return fromTe;

                var t = window.GetType();
                foreach (var name in new[] { "workstationData", "WorkstationData" })
                {
                    object model = null;
                    var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null)
                        model = p.GetValue(window, null);
                    if (model == null)
                    {
                        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (f != null)
                            model = f.GetValue(window);
                    }
                    if (model == null)
                        continue;

                    object te = null;
                    var tp = model.GetType().GetProperty("TileEntity", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (tp != null)
                        te = tp.GetValue(model, null);
                    if (te == null)
                    {
                        var tf = model.GetType().GetField("tileEntity", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                        te = tf?.GetValue(model);
                    }
                    if (te != null)
                        return GameReflection.GetBlockValueFromTileEntity(te);
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        /// <summary>
        /// Dew/Apiary collector UIs hold <c>te</c> (TileEntityCollector) directly on <c>XUiC_DewCollectorWindowGroup</c>, not workstationData.
        /// </summary>
        private static object TryGetBlockValueFromTileEntityFieldOnController(object window)
        {
            if (window == null)
                return null;
            try
            {
                for (var t = window.GetType(); t != null && t != typeof(object); t = t.BaseType)
                {
                    foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (f.FieldType == null || f.FieldType.Name == null) continue;
                        if (!f.FieldType.Name.StartsWith("TileEntity", StringComparison.Ordinal)) continue;
                        var te = f.GetValue(window);
                        if (te == null) continue;
                        var bv = GameReflection.GetBlockValueFromTileEntity(te);
                        if (bv != null)
                            return bv;
                    }

                    foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (p.GetIndexParameters() != null && p.GetIndexParameters().Length > 0) continue;
                        var pt = p.PropertyType;
                        if (pt == null || !pt.Name.StartsWith("TileEntity", StringComparison.Ordinal)) continue;
                        if (!p.CanRead) continue;
                        object te;
                        try { te = p.GetValue(window, null); }
                        catch { continue; }
                        if (te == null) continue;
                        var bv = GameReflection.GetBlockValueFromTileEntity(te);
                        if (bv != null)
                            return bv;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private static object TryGetLocalPlayerUiFromController(object controller)
        {
            if (controller == null)
                return null;
            try
            {
                var xuiProp = controller.GetType().GetProperty("xui", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                var xui = xuiProp?.GetValue(controller, null);
                if (xui == null)
                    return null;
                var uiProp = xui.GetType().GetProperty("playerUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                return uiProp?.GetValue(xui, null);
            }
            catch
            {
                return null;
            }
        }

        private static object GetWorkstationBlockFromWindow(object window)
        {
            try
            {
                var t = window.GetType();
                foreach (var member in new[] { "workstationBlock", "WorkstationBlock" })
                {
                    var prop = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (prop != null)
                    {
                        var v = prop.GetValue(window, null);
                        if (v != null) return v;
                    }
                    var field = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        var v = field.GetValue(window);
                        if (v != null) return v;
                    }
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }

        private static string TryResolveWorkstationWindowIdFromBlock(object blockValue)
        {
            try
            {
                var block = GameReflection.GetBlockFromBlockValue(blockValue);
                if (block == null)
                    return null;

                object stationData = null;
                foreach (var name in new[] { "WorkstationData", "workstationData" })
                {
                    var p = block.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null)
                        stationData = p.GetValue(block, null);
                    if (stationData == null)
                    {
                        var f = block.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (f != null)
                            stationData = f.GetValue(block);
                    }
                    if (stationData != null)
                        break;
                }

                if (stationData == null)
                    return null;

                foreach (var wn in new[] { "WorkstationWindow", "workstationWindow" })
                {
                    var p = stationData.GetType().GetProperty(wn, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null)
                    {
                        var s = p.GetValue(stationData, null) as string;
                        if (!string.IsNullOrEmpty(s))
                            return s;
                    }
                    var f = stationData.GetType().GetField(wn, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null)
                    {
                        var s = f.GetValue(stationData) as string;
                        if (!string.IsNullOrEmpty(s))
                            return s;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private static void CloseWorkstationAfterUnauthorizedOpen(object workstationController, object playerUi, string windowId)
        {
            try
            {
                if (playerUi != null && !string.IsNullOrEmpty(windowId))
                {
                    var wmProp = playerUi.GetType().GetProperty("windowManager", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                                 ?? playerUi.GetType().GetProperty("WindowManager", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    var wm = wmProp?.GetValue(playerUi, null);
                    if (wm != null)
                    {
                        var closeIfOpen = wm.GetType().GetMethod("CloseIfOpen", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                        closeIfOpen?.Invoke(wm, new object[] { windowId });
                    }
                }

                TryCloseWorkstationWindowGroupsOnXUi(playerUi);
                TryCloseXUiController(workstationController);
            }
            catch
            {
                // best-effort
            }
        }

        private static void TryCloseWorkstationWindowGroupsOnXUi(object playerUi)
        {
            try
            {
                if (playerUi == null)
                    return;
                var xuiProp = playerUi.GetType().GetProperty("xui", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                var xui = xuiProp?.GetValue(playerUi, null);
                if (xui == null)
                    return;

                var gameAssembly = xui.GetType().Assembly;
                var wsgType = gameAssembly.GetType("XUiC_WorkstationWindowGroup");
                if (wsgType == null)
                    return;

                var xuiType = xui.GetType();
                foreach (var m in xuiType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (m.Name != "GetWindowsByType" || !m.IsGenericMethodDefinition || m.GetParameters().Length != 0)
                        continue;
                    MethodInfo gm;
                    try
                    {
                        gm = m.MakeGenericMethod(wsgType);
                    }
                    catch
                    {
                        continue;
                    }

                    var listObj = gm.Invoke(xui, null) as IEnumerable;
                    if (listObj == null)
                        continue;

                    foreach (var ctrl in listObj)
                    {
                        if (ctrl == null)
                            continue;
                        TryCloseXUiController(ctrl);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private static void TryCloseXUiController(object ctrl)
        {
            if (ctrl == null)
                return;
            try
            {
                var t = ctrl.GetType();
                var isOpen = t.GetProperty("IsOpen", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (isOpen != null && isOpen.CanWrite && isOpen.PropertyType == typeof(bool))
                    isOpen.SetValue(ctrl, false, null);

                var onClose = t.GetMethod("OnClose", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                onClose?.Invoke(ctrl, null);
            }
            catch
            {
                // ignored
            }
        }
    }
}
