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
                ApplyEquipItemPatchFromGameAssembly(harmony);
                ApplyEquipmentHandleStackSwapPatchFromGameAssembly(harmony);
                ApplyToolbeltHandleStackSwapPatchFromGameAssembly(harmony);
                ApplyWorkstationVehicleStackSwapPatchesFromGameAssembly(harmony);
                ApplyWorkstationOpenPatchFromGameAssembly(harmony);
                ApplyWorkstationOpenedChokePointPatchFromGameAssembly(harmony);
                ApplyWorkstationWindowSetTileEntityPatchFromGameAssembly(harmony);
                ApplyWorkstationWindowOnOpenRestrictionPatchFromGameAssembly(harmony);
                ApplyGUIWindowManagerOpenNameLoggingPatchFromGameAssembly(harmony);
                ApplyVehicleDrivePatchFromGameAssembly(harmony);
                ApplyItemActionSpawnVehiclePatchFromGameAssembly(harmony);
                ApplyAddItemToToolbeltPatchFromGameAssembly(harmony);
                ApplyItemActionEntryEquipPatchFromGameAssembly(harmony);
                ApplyProgressionLevelUpPatchFromGameAssembly(harmony);
                ApplyInventoryGridPatchesFromGameAssembly(harmony);
                SafeLog("Harmony patches applied");
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                SafeLog($"Harmony patch failed: {inner.Message}");
                SafeLog(ex.StackTrace ?? "");
            }

            SafeLog("[LimitByCraftingSkill] Mod loaded; DebugMode=" + config.DebugMode + ". When restrictions trigger, look for 'BLOCKED' or errors in this log.");
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

        /// <summary>
        /// Apply EquipItem patch using the game's Assembly-CSharp so we target the same assembly as Equipment.
        /// PatchAll uses types from our references which may resolve to the game at runtime; this ensures we patch the game type.
        /// </summary>
        private static void ApplyEquipItemPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var playerEquipmentType = gameAssembly.GetType("XUiM_PlayerEquipment");
                if (playerEquipmentType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiM_PlayerEquipment not found in game assembly, EquipItem patch skipped.");
                    return;
                }
                var equipItemMethod = playerEquipmentType.GetMethod("EquipItem", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(ItemStack) }, null);
                if (equipItemMethod == null)
                {
                    SafeLog("[LimitByCraftingSkill] EquipItem(ItemStack) not found on XUiM_PlayerEquipment, patch skipped.");
                    return;
                }
                var prefix = typeof(EquipItemRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(equipItemMethod, prefix: new HarmonyMethod(prefix));
                SafeLog("[LimitByCraftingSkill] EquipItem patch applied from game assembly.");
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] EquipItem manual patch failed: {ex.Message}");
            }
        }

        private static void ApplyProgressionLevelUpPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var progressionType = gameAssembly.GetType("Progression");
                if (progressionType == null)
                {
                    SafeLog("[LimitByCraftingSkill] Progression type not found, level-up color refresh patch skipped.");
                    return;
                }
                var addCurrencyMethod = progressionType.GetMethod("addProgressionCurrency", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (addCurrencyMethod == null)
                {
                    SafeLog("[LimitByCraftingSkill] addProgressionCurrency not found, level-up patch skipped.");
                    return;
                }
                var postfix = typeof(ProgressionLevelUpPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(addCurrencyMethod, postfix: new HarmonyMethod(postfix));
                SafeLog("[LimitByCraftingSkill] Progression addProgressionCurrency (level-up) patch applied.");
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] Progression level-up patch failed: {ex.Message}");
            }
        }

        private static void ApplyEquipmentHandleStackSwapPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var equipmentStackType = gameAssembly.GetType("XUiC_EquipmentStack");
                if (equipmentStackType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_EquipmentStack not found, HandleStackSwap patch skipped.");
                    return;
                }
                var handleStackSwap = equipmentStackType.GetMethod("HandleStackSwap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (handleStackSwap == null)
                {
                    SafeLog("[LimitByCraftingSkill] HandleStackSwap not found on XUiC_EquipmentStack, patch skipped.");
                    return;
                }
                var prefix = typeof(EquipmentStackHandleStackSwapPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(handleStackSwap, prefix: new HarmonyMethod(prefix));
                SafeLog("[LimitByCraftingSkill] EquipmentStack.HandleStackSwap (drag-drop block) patch applied.");
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] HandleStackSwap patch failed: {ex.Message}");
            }
        }

        private static void ApplyToolbeltHandleStackSwapPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var itemStackType = gameAssembly.GetType("XUiC_ItemStack");
                if (itemStackType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_ItemStack not found, toolbelt HandleStackSwap patch skipped.");
                    return;
                }
                var handleStackSwap = itemStackType.GetMethod("HandleStackSwap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (handleStackSwap == null)
                {
                    SafeLog("[LimitByCraftingSkill] HandleStackSwap not found on XUiC_ItemStack, toolbelt patch skipped.");
                    return;
                }
                var prefix = typeof(ToolbeltHandleStackSwapPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(handleStackSwap, prefix: new HarmonyMethod(prefix));
                SafeLog("[LimitByCraftingSkill] ItemStack.HandleStackSwap (toolbelt drag-drop block) patch applied.");
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] Toolbelt HandleStackSwap patch failed: {ex.Message}");
            }
        }

        private static void ApplyWorkstationVehicleStackSwapPatchesFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var itemStackType = gameAssembly.GetType("XUiC_ItemStack");
                if (itemStackType != null)
                {
                    var hss = itemStackType.GetMethod("HandleStackSwap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (hss != null)
                    {
                        var ws = typeof(WorkstationToolHandleStackSwapPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(hss, prefix: new HarmonyMethod(ws));
                        SafeLog("[LimitByCraftingSkill] Workstation tool grid HandleStackSwap patch applied.");
                    }
                }
                var partStackType = gameAssembly.GetType("XUiC_ItemPartStack");
                if (partStackType != null)
                {
                    var hss2 = partStackType.GetMethod("HandleStackSwap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (hss2 != null)
                    {
                        var vp = typeof(VehiclePartHandleStackSwapPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(hss2, prefix: new HarmonyMethod(vp));
                        SafeLog("[LimitByCraftingSkill] ItemPartStack.HandleStackSwap (vehicle mods) patch applied.");
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] Workstation/vehicle stack swap patches failed: {ex.Message}");
            }
        }

        private static void ApplyWorkstationOpenPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var worldBaseType = gameAssembly.GetType("WorldBase");
                var vector3iType = gameAssembly.GetType("Vector3i");
                var blockValueType = gameAssembly.GetType("BlockValue");
                var entityPlayerLocalType = gameAssembly.GetType("EntityPlayerLocal");
                if (worldBaseType == null || vector3iType == null || blockValueType == null || entityPlayerLocalType == null)
                {
                    SafeLog("[LimitByCraftingSkill] Required types for BlockWorkstation.OnBlockActivated not found, patch skipped.");
                    try { InWorldRestrictionDebugLog.Write("H1", "ModApi:ApplyWorkstationOpenPatch", "skip", "{\"reason\":\"required_types_not_found\"}", null); } catch { }
                    return;
                }
                var argTypesNoString = new Type[] { worldBaseType, typeof(int), vector3iType, blockValueType, entityPlayerLocalType };
                var prefix = typeof(WorkstationOpenRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                // Patch base Block.OnBlockActivated(WorldBase,...) so Chemistry Station (and any block not inheriting BlockWorkstation) is restricted when activated.
                var blockType = gameAssembly.GetType("Block");
                // Patch Block.ActivateBlock and Block.ActivateBlockOnce so Chemistry Station (and any workstation) is restricted
                // even when the game does not call OnBlockActivated (e.g. composite block path).
                if (blockType != null)
                {
                    var actArgTypes = new Type[] { worldBaseType, typeof(int), vector3iType, blockValueType };
                    var actBlock = blockType.GetMethod("ActivateBlock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new Type[] { worldBaseType, typeof(int), vector3iType, blockValueType, typeof(bool), typeof(bool) }, null);
                    if (actBlock != null)
                    {
                        var prefixAct = typeof(BlockActivateRestrictionPatch).GetMethod("PrefixActivateBlock", BindingFlags.Static | BindingFlags.Public);
                        if (prefixAct != null)
                        {
                            try
                            {
                                harmony.Patch(actBlock, prefix: new HarmonyMethod(prefixAct));
                                SafeLog("[LimitByCraftingSkill] Block.ActivateBlock (workstation restrict) patch applied.");
                                try { InWorldRestrictionDebugLog.Write("H_ACT", "ModApi:ApplyWorkstationOpenPatch", "ActivateBlock_patch_applied", "{}", null); } catch { }
                            }
                            catch (Exception ex) { SafeLog("[LimitByCraftingSkill] Block.ActivateBlock patch skip: " + ex.Message); }
                        }
                    }
                    var actOnce = blockType.GetMethod("ActivateBlockOnce", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, actArgTypes, null);
                    if (actOnce != null)
                    {
                        var prefixActOnce = typeof(BlockActivateRestrictionPatch).GetMethod("PrefixActivateBlockOnce", BindingFlags.Static | BindingFlags.Public);
                        if (prefixActOnce != null)
                        {
                            try
                            {
                                harmony.Patch(actOnce, prefix: new HarmonyMethod(prefixActOnce));
                                SafeLog("[LimitByCraftingSkill] Block.ActivateBlockOnce (workstation restrict) patch applied.");
                                try { InWorldRestrictionDebugLog.Write("H_ACT", "ModApi:ApplyWorkstationOpenPatch", "ActivateBlockOnce_patch_applied", "{}", null); } catch { }
                            }
                            catch (Exception ex) { SafeLog("[LimitByCraftingSkill] Block.ActivateBlockOnce patch skip: " + ex.Message); }
                        }
                    }
                }
                if (blockType != null && prefix != null)
                {
                    var blockOnActivated = blockType.GetMethod("OnBlockActivated", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, argTypesNoString, null);
                    if (blockOnActivated != null)
                    {
                        try
                        {
                            harmony.Patch(blockOnActivated, prefix: new HarmonyMethod(prefix));
                            SafeLog("[LimitByCraftingSkill] Block.OnBlockActivated(WorldBase,...) (workstation open) patch applied.");
                            try { InWorldRestrictionDebugLog.Write("H_BLK", "ModApi:ApplyWorkstationOpenPatch", "block_base_patch_applied", "{}", null); } catch { }
                        }
                        catch (Exception ex) { SafeLog("[LimitByCraftingSkill] Block base patch skip: " + ex.Message); }
                    }
                }
                // Also patch the base Block.OnBlockActivated(string, ...) overload.
                // Chemistry Station may route through the command-name based activation path which we weren't covering.
                var prefixWithCmdClrIdx = typeof(WorkstationOpenRestrictionPatch).GetMethod("PrefixWithCommandClrIdx", BindingFlags.Static | BindingFlags.Public);
                if (blockType != null && prefixWithCmdClrIdx != null)
                {
                    var cmdStringType = typeof(string);
                    var argTypesWithCommandBase = new Type[] { cmdStringType, worldBaseType, typeof(int), vector3iType, blockValueType, entityPlayerLocalType };
                    var blockOnActivatedWithCommand = blockType.GetMethod("OnBlockActivated", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, argTypesWithCommandBase, null);
                    if (blockOnActivatedWithCommand != null)
                    {
                        try
                        {
                            harmony.Patch(blockOnActivatedWithCommand, prefix: new HarmonyMethod(prefixWithCmdClrIdx));
                            SafeLog("[LimitByCraftingSkill] Block.OnBlockActivated(string, ...) (workstation open) patch applied.");
                            try { InWorldRestrictionDebugLog.Write("H_BLK_CMD", "ModApi:ApplyWorkstationOpenPatch", "block_base_string_patch_applied", "{}", null); } catch { }
                        }
                        catch (Exception ex)
                        {
                            SafeLog("[LimitByCraftingSkill] Block base string patch skip: " + ex.Message);
                        }
                    }
                }
                var blockWorkstationType = gameAssembly.GetType("BlockWorkstation");
                if (blockWorkstationType == null)
                {
                    SafeLog("[LimitByCraftingSkill] BlockWorkstation not found, workstation open patch skipped.");
                    try { InWorldRestrictionDebugLog.Write("H1", "ModApi:ApplyWorkstationOpenPatch", "skip", "{\"reason\":\"BlockWorkstation_not_found\"}", null); } catch { }
                    return;
                }
                var onBlockActivated = blockWorkstationType.GetMethod("OnBlockActivated", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, argTypesNoString, null);
                if (onBlockActivated == null)
                {
                    SafeLog("[LimitByCraftingSkill] BlockWorkstation.OnBlockActivated(WorldBase,int,Vector3i,BlockValue,EntityPlayerLocal) not found.");
                    try { InWorldRestrictionDebugLog.Write("H1", "ModApi:ApplyWorkstationOpenPatch", "skip", "{\"reason\":\"OnBlockActivated_not_found\"}", null); } catch { }
                    return;
                }
                harmony.Patch(onBlockActivated, prefix: new HarmonyMethod(prefix));
                var stringType = typeof(string);
                var onBlockActivatedWithCommand = blockWorkstationType.GetMethod("OnBlockActivated", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { stringType, worldBaseType, typeof(int), vector3iType, blockValueType, entityPlayerLocalType }, null);
                if (onBlockActivatedWithCommand != null)
                {
                    var prefixWithCommand = typeof(WorkstationOpenRestrictionPatch).GetMethod("PrefixWithCommand", BindingFlags.Static | BindingFlags.Public);
                    if (prefixWithCommand != null)
                    {
                        harmony.Patch(onBlockActivatedWithCommand, prefix: new HarmonyMethod(prefixWithCommand));
                        SafeLog("[LimitByCraftingSkill] BlockWorkstation.OnBlockActivated(string, ...) (workstation open) patch applied.");
                    }
                }
                var argTypesWithCommand = new Type[] { stringType, worldBaseType, typeof(int), vector3iType, blockValueType, entityPlayerLocalType };
                var patchedSubtypes = new System.Collections.Generic.List<string>();
                foreach (var subType in gameAssembly.GetTypes())
                {
                    if (subType == null || !subType.IsClass || subType == blockWorkstationType || !blockWorkstationType.IsAssignableFrom(subType))
                        continue;
                    var subMethod = subType.GetMethod("OnBlockActivated", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, argTypesWithCommand, null);
                    if (subMethod == null || subMethod.DeclaringType != subType)
                        continue;
                    var prefixWithCmd = typeof(WorkstationOpenRestrictionPatch).GetMethod("PrefixWithCommand", BindingFlags.Static | BindingFlags.Public);
                    if (prefixWithCmd != null)
                    {
                        try
                        {
                            harmony.Patch(subMethod, prefix: new HarmonyMethod(prefixWithCmd));
                            patchedSubtypes.Add(subType.Name);
                            SafeLog("[LimitByCraftingSkill] " + subType.Name + ".OnBlockActivated(string, ...) (workstation open) patch applied.");
                        }
                        catch (Exception subEx) { SafeLog("[LimitByCraftingSkill] " + subType.Name + " patch skip: " + subEx.Message); }
                    }
                }
                SafeLog("[LimitByCraftingSkill] BlockWorkstation.OnBlockActivated (workstation open) patch applied.");
                // #region agent log
                try
                {
                    var subtypesJson = string.Join(",", patchedSubtypes.ConvertAll(s => "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""));
                    InWorldRestrictionDebugLog.Write("H1", "ModApi:ApplyWorkstationOpenPatch", "patch_applied", "{\"applied\":true,\"blockWorkstationFound\":true,\"onBlockActivatedFound\":true,\"subtypesPatched\":[" + subtypesJson + "]}", null);
                }
                catch { }
                // #endregion
                // Patch BlockCompositeTileEntity so Chemistry Station (and other composite workstations) are restricted when opened via activation command.
                var blockCompositeType = gameAssembly.GetType("BlockCompositeTileEntity");
                if (blockCompositeType == null)
                    try { InWorldRestrictionDebugLog.Write("H_CMD", "ModApi:ApplyWorkstationOpenPatch", "composite_skip", "{\"reason\":\"BlockCompositeTileEntity_not_found\"}", null); } catch { }
                else
                {
                    var compositeNoString = blockCompositeType.GetMethod("OnBlockActivated", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, argTypesNoString, null);
                    if (compositeNoString != null && prefix != null)
                    {
                        try
                        {
                            harmony.Patch(compositeNoString, prefix: new HarmonyMethod(prefix));
                            SafeLog("[LimitByCraftingSkill] BlockCompositeTileEntity.OnBlockActivated(WorldBase,...) (workstation open) patch applied.");
                            try { InWorldRestrictionDebugLog.Write("H_CMD", "ModApi:ApplyWorkstationOpenPatch", "composite_no_string_patch_applied", "{}", null); } catch { }
                        }
                        catch (Exception ex) { SafeLog("[LimitByCraftingSkill] BlockCompositeTileEntity (WorldBase) patch skip: " + ex.Message); }
                    }
                    var compositeMethod = blockCompositeType.GetMethod("OnBlockActivated", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, argTypesWithCommand, null);
                    if (compositeMethod == null)
                        try { InWorldRestrictionDebugLog.Write("H_CMD", "ModApi:ApplyWorkstationOpenPatch", "composite_skip", "{\"reason\":\"OnBlockActivated_string_not_found\"}", null); } catch { }
                    else
                    {
                        var prefixWithCmd2 = typeof(WorkstationOpenRestrictionPatch).GetMethod("PrefixWithCommandClrIdx", BindingFlags.Static | BindingFlags.Public);
                        if (prefixWithCmd2 != null)
                        {
                            try
                            {
                                harmony.Patch(compositeMethod, prefix: new HarmonyMethod(prefixWithCmd2));
                                SafeLog("[LimitByCraftingSkill] BlockCompositeTileEntity.OnBlockActivated(string, ...) (workstation open) patch applied.");
                                try { InWorldRestrictionDebugLog.Write("H_CMD", "ModApi:ApplyWorkstationOpenPatch", "composite_patch_applied", "{}", null); } catch { }
                            }
                            catch (Exception ex)
                            {
                                SafeLog("[LimitByCraftingSkill] BlockCompositeTileEntity patch skip: " + ex.Message);
                                try { InWorldRestrictionDebugLog.Write("H_CMD", "ModApi:ApplyWorkstationOpenPatch", "composite_skip", "{\"reason\":\"patch_failed\",\"error\":\"" + (ex.Message ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}", null); } catch { }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] Workstation open patch failed: {ex.Message}");
                // #region agent log
                try { InWorldRestrictionDebugLog.Write("H1", "ModApi:ApplyWorkstationOpenPatch", "patch_failed", "{\"error\":\"" + (ex.Message ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}", null); } catch { }
                // #endregion
            }
        }

        /// <summary>
        /// Patches GameManager.workstationOpened so we can block opening the UI for any workstation (including Chemistry Bench) in one place.
        /// </summary>
        private static void ApplyWorkstationOpenedChokePointPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var gameManagerType = gameAssembly.GetType("GameManager");
                if (gameManagerType == null)
                {
                    SafeLog("[LimitByCraftingSkill] GameManager not found, workstationOpened choke point patch skipped.");
                    return;
                }
                var teType = gameAssembly.GetType("TileEntityWorkstation");
                var uiType = gameAssembly.GetType("LocalPlayerUI");
                if (teType == null || uiType == null)
                {
                    SafeLog("[LimitByCraftingSkill] TileEntityWorkstation or LocalPlayerUI not found, workstationOpened patch skipped.");
                    return;
                }
                var method = gameManagerType.GetMethod("workstationOpened", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { teType, uiType }, null);
                if (method == null)
                {
                    SafeLog("[LimitByCraftingSkill] GameManager.workstationOpened not found.");
                    return;
                }
                var prefix = typeof(WorkstationOpenedRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                if (prefix != null)
                {
                    harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                    SafeLog("[LimitByCraftingSkill] GameManager.workstationOpened (workstation UI choke point) patch applied.");
                    try { InWorldRestrictionDebugLog.Write("H_WS", "ModApi:ApplyWorkstationOpenedChokePoint", "patch_applied", "{}", null); } catch { }
                }
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] WorkstationOpened choke point patch failed: {ex.Message}");
                try { InWorldRestrictionDebugLog.Write("H_WS", "ModApi:ApplyWorkstationOpenedChokePoint", "patch_failed", "{\"error\":\"" + (ex.Message ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}", null); } catch { }
            }
        }

        /// <summary>
        /// Patches XUiC_WorkstationWindowGroup.SetTileEntity so we block binding the workstation TE when restricted (UI path used when workstationOpened is not called).
        /// </summary>
        private static void ApplyWorkstationWindowSetTileEntityPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var windowType = gameAssembly.GetType("XUiC_WorkstationWindowGroup");
                if (windowType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_WorkstationWindowGroup not found, SetTileEntity patch skipped.");
                    try { InWorldRestrictionDebugLog.Write("H_SET", "ModApi:SetTileEntityPatch", "skip", "{\"reason\":\"XUiC_WorkstationWindowGroup_not_found\"}", null); } catch { }
                    return;
                }
                var teType = gameAssembly.GetType("TileEntityWorkstation");
                if (teType == null)
                {
                    SafeLog("[LimitByCraftingSkill] TileEntityWorkstation not found, SetTileEntity patch skipped.");
                    try { InWorldRestrictionDebugLog.Write("H_SET", "ModApi:SetTileEntityPatch", "skip", "{\"reason\":\"TileEntityWorkstation_not_found\"}", null); } catch { }
                    return;
                }
                var method = windowType.GetMethod("SetTileEntity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { teType }, null);
                if (method == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_WorkstationWindowGroup.SetTileEntity not found.");
                    try { InWorldRestrictionDebugLog.Write("H_SET", "ModApi:SetTileEntityPatch", "skip", "{\"reason\":\"SetTileEntity_not_found\"}", null); } catch { }
                    return;
                }
                var prefix = typeof(WorkstationWindowSetTileEntityRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                if (prefix != null)
                {
                    harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                    SafeLog("[LimitByCraftingSkill] XUiC_WorkstationWindowGroup.SetTileEntity (workstation UI) patch applied.");
                }
                var argTypes = new Type[] { teType };
                foreach (var subType in gameAssembly.GetTypes())
                {
                    if (subType == null || !subType.IsClass || subType == windowType || !windowType.IsAssignableFrom(subType))
                        continue;
                    var subMethod = subType.GetMethod("SetTileEntity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, argTypes, null);
                    if (subMethod == null || subMethod.DeclaringType != subType)
                        continue;
                    var prefixSub = typeof(WorkstationWindowSetTileEntityRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                    if (prefixSub != null)
                    {
                        try
                        {
                            harmony.Patch(subMethod, prefix: new HarmonyMethod(prefixSub));
                            SafeLog("[LimitByCraftingSkill] " + subType.Name + ".SetTileEntity (workstation UI) patch applied.");
                        }
                        catch (Exception subEx) { SafeLog("[LimitByCraftingSkill] " + subType.Name + " SetTileEntity patch skip: " + subEx.Message); }
                    }
                }
                try { InWorldRestrictionDebugLog.Write("H_SET", "ModApi:SetTileEntityPatch", "patch_applied", "{}", null); } catch { }
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] Workstation SetTileEntity patch failed: {ex.Message}");
                try { InWorldRestrictionDebugLog.Write("H_SET", "ModApi:SetTileEntityPatch", "patch_failed", "{\"error\":\"" + (ex.Message ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}", null); } catch { }
            }
        }

        /// <summary>
        /// Patch XUiC_WorkstationWindowGroup.OnOpen() so we can enforce restrictions even when
        /// block activation / workstationOpened / SetTileEntity hooks are bypassed.
        /// </summary>
        private static void ApplyWorkstationWindowOnOpenRestrictionPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var windowType = gameAssembly.GetType("XUiC_WorkstationWindowGroup");
                if (windowType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_WorkstationWindowGroup not found, OnOpen patch skipped.");
                    try { InWorldRestrictionDebugLog.Write("H_UIOPEN", "ModApi:ApplyWorkstationWindowOnOpen", "skip", "{\"reason\":\"XUiC_WorkstationWindowGroup_not_found\"}", null); } catch { }
                    return;
                }

                var method = windowType.GetMethod("OnOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (method == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_WorkstationWindowGroup.OnOpen not found, OnOpen patch skipped.");
                    try { InWorldRestrictionDebugLog.Write("H_UIOPEN", "ModApi:ApplyWorkstationWindowOnOpen", "skip", "{\"reason\":\"OnOpen_not_found\"}", null); } catch { }
                    return;
                }

                var prefix = typeof(WorkstationWindowOnOpenRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                if (prefix != null)
                {
                    harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                    try { InWorldRestrictionDebugLog.Write("H_UIOPEN", "ModApi:ApplyWorkstationWindowOnOpen", "patch_applied_base", "{}", null); } catch { }
                }

                // Patch derived types too (same method signature: OnOpen()).
                var argTypes = Type.EmptyTypes;
                foreach (var subType in gameAssembly.GetTypes())
                {
                    if (subType == null || !subType.IsClass || subType == windowType || !windowType.IsAssignableFrom(subType))
                        continue;
                    var subMethod = subType.GetMethod("OnOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, argTypes, null);
                    if (subMethod == null || subMethod.DeclaringType != subType)
                        continue;
                    if (prefix == null)
                        continue;
                    try
                    {
                        harmony.Patch(subMethod, prefix: new HarmonyMethod(prefix));
                        try { InWorldRestrictionDebugLog.Write("H_UIOPEN", "ModApi:ApplyWorkstationWindowOnOpen", "patch_applied", "{\"windowSubtype\":\"" + subType.Name.Replace("\"", "\\\"") + "\"}", null); } catch { }
                    }
                    catch (Exception subEx)
                    {
                        SafeLog("[LimitByCraftingSkill] OnOpen patch skip: " + subEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] OnOpen restriction patch failed: " + ex.Message);
            }
        }

        private static void ApplyVehicleDrivePatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                // Harmony requires patching the type that declares the method. EnterVehicle is declared on EntityVehicle, not EntityDriveable.
                var entityVehicleType = gameAssembly.GetType("EntityVehicle");
                if (entityVehicleType == null)
                {
                    SafeLog("[LimitByCraftingSkill] EntityVehicle not found, vehicle drive patch skipped.");
                    try { InWorldRestrictionDebugLog.Write("H6", "ModApi:ApplyVehicleDrivePatch", "skip", "{\"reason\":\"EntityVehicle_not_found\"}", null); } catch { }
                    return;
                }
                var entityAliveType = gameAssembly.GetType("EntityAlive");
                if (entityAliveType == null)
                {
                    SafeLog("[LimitByCraftingSkill] EntityAlive not found, vehicle drive patch skipped.");
                    return;
                }
                var enterVehicle = entityVehicleType.GetMethod("EnterVehicle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { entityAliveType }, null);
                if (enterVehicle == null)
                {
                    SafeLog("[LimitByCraftingSkill] EntityVehicle.EnterVehicle(EntityAlive) not found.");
                    try { InWorldRestrictionDebugLog.Write("H6", "ModApi:ApplyVehicleDrivePatch", "skip", "{\"reason\":\"EnterVehicle_not_found\"}", null); } catch { }
                    return;
                }
                var prefix = typeof(VehicleDriveRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(enterVehicle, prefix: new HarmonyMethod(prefix));
                SafeLog("[LimitByCraftingSkill] EntityVehicle.EnterVehicle (vehicle drive) patch applied.");
                // #region agent log
                try { InWorldRestrictionDebugLog.Write("H6", "ModApi:ApplyVehicleDrivePatch", "patch_applied", "{\"applied\":true,\"entityVehicleFound\":true,\"enterVehicleFound\":true}", null); } catch { }
                // #endregion
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] Vehicle drive patch failed: {ex.Message}");
                // #region agent log
                try { InWorldRestrictionDebugLog.Write("H6", "ModApi:ApplyVehicleDrivePatch", "patch_failed", "{\"error\":\"" + (ex.Message ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}", null); } catch { }
                // #endregion
            }
        }

        /// <summary>
        /// Debug-only instrumentation: logs GUI window names opened via GUIWindowManager.Open/OpenIfNotOpen.
        /// Helps identify which UI Chemistry Station uses.
        /// </summary>
        private static void ApplyGUIWindowManagerOpenNameLoggingPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var guiWindowManagerType = gameAssembly.GetType("GUIWindowManager");
                if (guiWindowManagerType == null)
                    return;

                var prefixOpen4 = typeof(GUIWindowManagerOpenNameLogPatch).GetMethod("PrefixOpen_String_Bool_Bool_Bool", BindingFlags.Static | BindingFlags.Public);
                var prefixOpen5 = typeof(GUIWindowManagerOpenNameLogPatch).GetMethod("PrefixOpen_String_Int_Int_Bool_Bool", BindingFlags.Static | BindingFlags.Public);
                var prefixOpenIfNotOpen4 = typeof(GUIWindowManagerOpenNameLogPatch).GetMethod("PrefixOpenIfNotOpen_String_Bool_Bool_Bool", BindingFlags.Static | BindingFlags.Public);

                if (prefixOpen5 != null)
                {
                    var m = guiWindowManagerType.GetMethod("Open", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { typeof(string), typeof(int), typeof(int), typeof(bool), typeof(bool) }, null);
                    if (m != null)
                        harmony.Patch(m, prefix: new HarmonyMethod(prefixOpen5));
                }

                if (prefixOpen4 != null)
                {
                    var m = guiWindowManagerType.GetMethod("Open", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { typeof(string), typeof(bool), typeof(bool), typeof(bool) }, null);
                    if (m != null)
                        harmony.Patch(m, prefix: new HarmonyMethod(prefixOpen4));
                }

                if (prefixOpenIfNotOpen4 != null)
                {
                    var m = guiWindowManagerType.GetMethod("OpenIfNotOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { typeof(string), typeof(bool), typeof(bool), typeof(bool) }, null);
                    if (m != null)
                        harmony.Patch(m, prefix: new HarmonyMethod(prefixOpenIfNotOpen4));
                }
            }
            catch
            {
                // best-effort instrumentation only
            }
        }

        private static void ApplyItemActionSpawnVehiclePatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var spawnType = gameAssembly.GetType("ItemActionSpawnVehicle");
                if (spawnType == null)
                {
                    SafeLog("[LimitByCraftingSkill] ItemActionSpawnVehicle not found, spawn patch skipped.");
                    return;
                }
                var execute = spawnType.GetMethod("ExecuteAction", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (execute == null)
                {
                    SafeLog("[LimitByCraftingSkill] ItemActionSpawnVehicle.ExecuteAction not found.");
                    return;
                }
                var prefix = typeof(ItemActionSpawnVehicleRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(execute, prefix: new HarmonyMethod(prefix));
                SafeLog("[LimitByCraftingSkill] ItemActionSpawnVehicle.ExecuteAction patch applied.");
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] ItemActionSpawnVehicle patch failed: {ex.Message}");
            }
        }

        private static void ApplyAddItemToToolbeltPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var playerInvType = gameAssembly.GetType("XUiM_PlayerInventory");
                if (playerInvType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiM_PlayerInventory not found, AddItemToToolbelt patch skipped.");
                    return;
                }
                var addToToolbelt = playerInvType.GetMethod("AddItemToToolbelt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(ItemStack) }, null);
                if (addToToolbelt != null)
                {
                    var prefix = typeof(AddItemToToolbeltRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(addToToolbelt, prefix: new HarmonyMethod(prefix));
                    SafeLog("[LimitByCraftingSkill] XUiM_PlayerInventory.AddItemToToolbelt (equip key block) patch applied.");
                }
                var addToPreferred = playerInvType.GetMethod("AddItemToPreferredToolbeltSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(ItemStack), typeof(int) }, null);
                if (addToPreferred != null)
                {
                    var prefixPref = typeof(AddItemToToolbeltRestrictionPatch).GetMethod("PrefixPreferredSlot", BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(addToPreferred, prefix: new HarmonyMethod(prefixPref));
                    SafeLog("[LimitByCraftingSkill] XUiM_PlayerInventory.AddItemToPreferredToolbeltSlot (equip key block) patch applied.");
                }
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] AddItemToToolbelt patch failed: {ex.Message}");
            }
        }

        private static void ApplyItemActionEntryEquipPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var equipEntryType = gameAssembly.GetType("ItemActionEntryEquip");
                if (equipEntryType == null)
                {
                    SafeLog("[LimitByCraftingSkill] ItemActionEntryEquip not found, equip key patch skipped.");
                    return;
                }
                var onActivated = equipEntryType.GetMethod("OnActivated", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (onActivated == null)
                {
                    SafeLog("[LimitByCraftingSkill] OnActivated not found on ItemActionEntryEquip, patch skipped.");
                    return;
                }
                var prefix = typeof(ItemActionEntryEquipPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(onActivated, prefix: new HarmonyMethod(prefix));
                SafeLog("[LimitByCraftingSkill] ItemActionEntryEquip.OnActivated (equip key/action block) patch applied.");
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] ItemActionEntryEquip patch failed: {ex.Message}");
            }
        }

        private static void ApplyInventoryGridPatchesFromGameAssembly(Harmony harmony)
        {
            var gameAssembly = typeof(Equipment).Assembly;
            var itemStackGridType = gameAssembly.GetType("XUiC_ItemStackGrid");
            var equipmentStackGridType = gameAssembly.GetType("XUiC_EquipmentStackGrid");
            var xuiControllerType = gameAssembly.GetType("XUiController");

            // OnOpen: patch on the grid types (they declare OnOpen)
            try
            {
                if (itemStackGridType != null)
                {
                    var onOpen = itemStackGridType.GetMethod("OnOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (onOpen != null)
                    {
                        var postfix = typeof(ItemStackGridOnOpenPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(onOpen, postfix: new HarmonyMethod(postfix));
                        SafeLog("[LimitByCraftingSkill] ItemStackGrid.OnOpen patch applied.");
                    }
                }
            }
            catch (Exception ex) { SafeLog($"[LimitByCraftingSkill] ItemStackGrid OnOpen patch failed: {ex.Message}"); }

            try
            {
                if (equipmentStackGridType != null)
                {
                    var onOpen = equipmentStackGridType.GetMethod("OnOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (onOpen != null)
                    {
                        var postfix = typeof(EquipmentStackGridOnOpenPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(onOpen, postfix: new HarmonyMethod(postfix));
                        SafeLog("[LimitByCraftingSkill] EquipmentStackGrid.OnOpen patch applied.");
                    }
                }
            }
            catch (Exception ex) { SafeLog($"[LimitByCraftingSkill] EquipmentStackGrid OnOpen patch failed: {ex.Message}"); }

            // Update: declared on XUiController, so patch the base type once; our Postfix filters by instance type
            if (xuiControllerType != null)
            {
                try
                {
                    var update = xuiControllerType.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
                    if (update != null)
                    {
                        var postfix = typeof(GridUpdateRestrictionColorPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(update, postfix: new HarmonyMethod(postfix));
                        SafeLog("[LimitByCraftingSkill] XUiController.Update (grid restriction refresh) patch applied.");
                    }
                }
                catch (Exception ex) { SafeLog($"[LimitByCraftingSkill] Grid Update patch failed: {ex.Message}"); }
            }
        }

        /// <summary>
        /// Logs to game output only when DebugMode is true. Used by patches for troubleshooting.
        /// </summary>
        internal static void DebugLog(string message)
        {
            if (ModConfig.Instance != null && ModConfig.Instance.DebugMode)
                SafeLog("[LimitByCraftingSkill] " + message);
        }
    }
}
