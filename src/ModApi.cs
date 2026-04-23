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
                ApplyWorkstationWindowSetTileEntityPatchFromGameAssembly(harmony);
                ApplyWorkstationWindowOnOpenRestrictionPatchFromGameAssembly(harmony);
                ApplyGameManagerWorkstationOpenedPatchFromGameAssembly(harmony);
                ApplyGUIWindowManagerOpenNameLoggingPatchFromGameAssembly(harmony);
                ApplyVehicleDrivePatchFromGameAssembly(harmony);
                ApplyItemActionSpawnVehiclePatchFromGameAssembly(harmony);
                ApplyAddItemToToolbeltPatchFromGameAssembly(harmony);
                ApplyItemActionEntryEquipPatchFromGameAssembly(harmony);
                ApplyProgressionLevelUpPatchFromGameAssembly(harmony);
                ApplyInventoryGridPatchesFromGameAssembly(harmony);
                ApplyPopupToolTipDisplayTooltipPostfix(harmony);
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

        /// <summary>
        /// Previously patched Block.OnBlockActivated / BlockWorkstation / composite paths with Prefix+skip.
        /// Any Harmony prefix that skips these instance methods correlated with client UI lock until rejoin.
        /// Workstation restriction is enforced in <see cref="ApplyWorkstationWindowOnOpenRestrictionPatchFromGameAssembly"/> (OnOpen Postfix).
        /// Types like WorkstationOpenRestrictionPatch remain in the repo for reference.
        /// </summary>
        private static void ApplyWorkstationOpenPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                SafeLog("[LimitByCraftingSkill] OnBlockActivated workstation prefixes not applied (avoid UI lock). Restriction uses WorkstationWindowGroup.OnOpen Postfix.");
                try { InWorldRestrictionDebugLog.Write("H1", "ModApi:ApplyWorkstationOpenPatch", "disabled_on_block_activated", "{}", null); } catch { }
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] Workstation open patch hook failed: {ex.Message}");
                try { InWorldRestrictionDebugLog.Write("H1", "ModApi:ApplyWorkstationOpenPatch", "patch_failed", "{\"error\":\"" + (ex.Message ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}", null); } catch { }
            }
        }

        /// <summary>
        /// Postfix SetTileEntity — runs after TE bind (always has TileEntity); Prefix+skip caused UI lock.
        /// </summary>
        private static void ApplyWorkstationWindowSetTileEntityPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var windowType = gameAssembly.GetType("XUiC_WorkstationWindowGroup");
                var teType = gameAssembly.GetType("TileEntityWorkstation");
                if (windowType == null || teType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_WorkstationWindowGroup or TileEntityWorkstation not found, SetTileEntity postfix skipped.");
                    try { InWorldRestrictionDebugLog.Write("H_SET", "ModApi:SetTileEntityPatch", "skip", "{\"reason\":\"types_not_found\"}", null); } catch { }
                    return;
                }

                var postfixSt = typeof(WorkstationRestrictionUi).GetMethod("PostfixSetTileEntity", BindingFlags.Static | BindingFlags.Public);
                if (postfixSt == null)
                    return;

                var method = windowType.GetMethod("SetTileEntity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { teType }, null);
                if (method != null)
                {
                    harmony.Patch(method, postfix: new HarmonyMethod(postfixSt));
                    SafeLog("[LimitByCraftingSkill] XUiC_WorkstationWindowGroup.SetTileEntity Postfix patch applied.");
                    try { InWorldRestrictionDebugLog.Write("H_SET", "ModApi:SetTileEntityPatch", "patch_applied", "{}", null); } catch { }
                }

                foreach (var subType in gameAssembly.GetTypes())
                {
                    if (subType == null || !subType.IsClass || subType == windowType || !windowType.IsAssignableFrom(subType))
                        continue;
                    var subMethod = subType.GetMethod("SetTileEntity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new Type[] { teType }, null);
                    if (subMethod == null || subMethod.DeclaringType != subType)
                        continue;
                    try
                    {
                        harmony.Patch(subMethod, postfix: new HarmonyMethod(postfixSt));
                        SafeLog("[LimitByCraftingSkill] " + subType.Name + ".SetTileEntity Postfix patch applied.");
                    }
                    catch (Exception subEx)
                    {
                        SafeLog("[LimitByCraftingSkill] SetTileEntity postfix on " + subType.Name + " skip: " + subEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] Workstation SetTileEntity postfix failed: {ex.Message}");
                try { InWorldRestrictionDebugLog.Write("H_SET", "ModApi:SetTileEntityPatch", "patch_failed", "{\"error\":\"" + (ex.Message ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}", null); } catch { }
            }
        }

        /// <summary>
        /// Postfix OnOpen: always patch CraftingWindowGroup (filtered to workstation controllers) so base.OnOpen() inside
        /// WorkstationWindowGroup.OnOpen is covered; additionally patch every OnOpen declared on subtypes of WorkstationWindowGroup
        /// (covers intermediate overrides when DeclaringType is not XUiC_WorkstationWindowGroup).
        /// </summary>
        private static void ApplyWorkstationWindowOnOpenRestrictionPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var wsType = gameAssembly.GetType("XUiC_WorkstationWindowGroup");
                var craftType = gameAssembly.GetType("XUiC_CraftingWindowGroup");
                if (wsType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_WorkstationWindowGroup not found, OnOpen postfix skipped.");
                    try { InWorldRestrictionDebugLog.Write("H_UIOPEN", "ModApi:ApplyWorkstationWindowOnOpen", "skip", "{\"reason\":\"WS_not_found\"}", null); } catch { }
                    return;
                }

                var postfixWs = typeof(WorkstationRestrictionUi).GetMethod("PostfixWorkstationWindowGroupOnOpen", BindingFlags.Static | BindingFlags.Public);
                var postfixFiltered = typeof(WorkstationRestrictionUi).GetMethod("PostfixFilteredCraftingOnOpen", BindingFlags.Static | BindingFlags.Public);
                if (postfixWs == null || postfixFiltered == null)
                    return;

                if (craftType != null)
                {
                    var craftOnOpen = craftType.GetMethod("OnOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (craftOnOpen != null)
                    {
                        harmony.Patch(craftOnOpen, postfix: new HarmonyMethod(postfixFiltered));
                        SafeLog("[LimitByCraftingSkill] XUiC_CraftingWindowGroup.OnOpen Postfix (filtered to workstation) applied.");
                        try { InWorldRestrictionDebugLog.Write("H_UIOPEN", "ModApi:ApplyWorkstationWindowOnOpen", "patch_applied_crafting_base", "{}", null); } catch { }
                    }

                    for (var midType = wsType.BaseType; midType != null && midType != craftType; midType = midType.BaseType)
                    {
                        var midOnOpen = midType.GetMethod("OnOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                        if (midOnOpen == null || midOnOpen.DeclaringType != midType)
                            continue;
                        try
                        {
                            harmony.Patch(midOnOpen, postfix: new HarmonyMethod(postfixWs));
                            SafeLog("[LimitByCraftingSkill] " + midType.Name + ".OnOpen Postfix (between Crafting and WorkstationWindowGroup) applied.");
                        }
                        catch (Exception midEx)
                        {
                            SafeLog("[LimitByCraftingSkill] OnOpen postfix on " + midType.Name + " skip: " + midEx.Message);
                        }
                    }
                }

                foreach (var subType in gameAssembly.GetTypes())
                {
                    if (subType == null || !subType.IsClass || !wsType.IsAssignableFrom(subType))
                        continue;
                    var sm = subType.GetMethod("OnOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (sm == null || sm.DeclaringType != subType)
                        continue;
                    try
                    {
                        harmony.Patch(sm, postfix: new HarmonyMethod(postfixWs));
                        SafeLog("[LimitByCraftingSkill] " + subType.Name + ".OnOpen Postfix applied.");
                    }
                    catch (Exception subEx)
                    {
                        SafeLog("[LimitByCraftingSkill] OnOpen postfix on " + subType.Name + " skip: " + subEx.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] OnOpen restriction postfix failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Postfix GameManager.workstationOpened — TE + LocalPlayerUI when the central open path runs.
        /// </summary>
        private static void ApplyGameManagerWorkstationOpenedPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var gmType = gameAssembly.GetType("GameManager");
                var teType = gameAssembly.GetType("TileEntityWorkstation");
                var lpuiType = gameAssembly.GetType("LocalPlayerUI");
                if (gmType == null || teType == null || lpuiType == null)
                {
                    SafeLog("[LimitByCraftingSkill] GameManager / TileEntityWorkstation / LocalPlayerUI not found, workstationOpened postfix skipped.");
                    return;
                }

                var postfix = typeof(WorkstationRestrictionUi).GetMethod("PostfixGameManagerWorkstationOpened", BindingFlags.Static | BindingFlags.Public);
                if (postfix == null)
                    return;

                var method = gmType.GetMethod("workstationOpened", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { teType, lpuiType }, null);
                if (method == null)
                {
                    SafeLog("[LimitByCraftingSkill] GameManager.workstationOpened(TileEntityWorkstation, LocalPlayerUI) not found, postfix skipped.");
                    return;
                }

                harmony.Patch(method, postfix: new HarmonyMethod(postfix));
                SafeLog("[LimitByCraftingSkill] GameManager.workstationOpened Postfix applied.");
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] GameManager.workstationOpened postfix failed: " + ex.Message);
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
        /// Chemistry Station: Postfix on GUIWindowManager Open / OpenIfNotOpen — close + popup if Workstations level too low.
        /// Prefix+skip caused UI lock; Postfix runs after vanilla Open.
        /// </summary>
        private static void ApplyGUIWindowManagerOpenNameLoggingPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var wmType = gameAssembly.GetType("GUIWindowManager");
                if (wmType == null)
                {
                    SafeLog("[LimitByCraftingSkill] GUIWindowManager not found, chemistry Postfix skipped.");
                    return;
                }

                var patchType = typeof(GUIWindowManagerOpenNameLogPatch);

                var px1 = patchType.GetMethod("PostfixOpen_String_Bool_Bool_Bool", BindingFlags.Static | BindingFlags.Public);
                var px2 = patchType.GetMethod("PostfixOpen_String_Int_Int_Bool_Bool", BindingFlags.Static | BindingFlags.Public);
                var px3 = patchType.GetMethod("PostfixOpenIfNotOpen_String_Bool_Bool_Bool", BindingFlags.Static | BindingFlags.Public);
                if (px1 == null || px2 == null || px3 == null)
                    return;

                var open1 = wmType.GetMethod("Open", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { typeof(string), typeof(bool), typeof(bool), typeof(bool) }, null);
                if (open1 != null)
                {
                    harmony.Patch(open1, postfix: new HarmonyMethod(px1));
                    SafeLog("[LimitByCraftingSkill] GUIWindowManager.Open(string,bool,bool,bool) chemistry Postfix applied.");
                }

                var open2 = wmType.GetMethod("Open", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { typeof(string), typeof(int), typeof(int), typeof(bool), typeof(bool) }, null);
                if (open2 != null)
                {
                    harmony.Patch(open2, postfix: new HarmonyMethod(px2));
                    SafeLog("[LimitByCraftingSkill] GUIWindowManager.Open(string,int,int,bool,bool) chemistry Postfix applied.");
                }

                var openIfNotOpen = wmType.GetMethod("OpenIfNotOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { typeof(string), typeof(bool), typeof(bool), typeof(bool) }, null);
                if (openIfNotOpen != null)
                {
                    harmony.Patch(openIfNotOpen, postfix: new HarmonyMethod(px3));
                    SafeLog("[LimitByCraftingSkill] GUIWindowManager.OpenIfNotOpen chemistry Postfix applied.");
                }

                var px4 = patchType.GetMethod("PostfixSwitchVisible_String_Bool_Bool", BindingFlags.Static | BindingFlags.Public);
                if (px4 != null)
                {
                    var switchVisible = wmType.GetMethod("SwitchVisible", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new Type[] { typeof(string), typeof(bool), typeof(bool) }, null);
                    if (switchVisible != null)
                    {
                        harmony.Patch(switchVisible, postfix: new HarmonyMethod(px4));
                        SafeLog("[LimitByCraftingSkill] GUIWindowManager.SwitchVisible chemistry Postfix applied.");
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] GUIWindowManager chemistry Postfix failed: " + ex.Message);
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
        /// Postfix XUiC_PopupToolTip.DisplayTooltipText — tint UILabel red for restriction messages after vanilla sets text.
        /// </summary>
        private static void ApplyPopupToolTipDisplayTooltipPostfix(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var popupType = gameAssembly.GetType("XUiC_PopupToolTip");
                if (popupType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_PopupToolTip not found, DisplayTooltipText postfix skipped.");
                    return;
                }

                var display = popupType.GetMethod("DisplayTooltipText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);
                if (display == null)
                {
                    SafeLog("[LimitByCraftingSkill] DisplayTooltipText not found, postfix skipped.");
                    return;
                }

                var postfix = typeof(PopupToolTipDisplayTooltipTextPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                if (postfix != null)
                {
                    harmony.Patch(display, postfix: new HarmonyMethod(postfix));
                    SafeLog("[LimitByCraftingSkill] XUiC_PopupToolTip.DisplayTooltipText restriction-color Postfix applied.");
                }
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] PopupToolTip DisplayTooltipText postfix failed: " + ex.Message);
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
