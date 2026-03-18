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
