using System;
using System.Reflection;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    public class ModApi : IModApi
    {
        // Patch strategy:
        // - Prefer UI/input chokepoints that run before the game commits an item move (toolbelt, equipment, vehicle spawn).
        // - Avoid returning false from low-level setters such as Inventory.SetItem / Equipment.SetSlotItem; those are unsafe item-loss points.
        // - Workstations: BlockWorkstation / BlockCollector OnBlockActivated Prefix only (popup, no UI open).
        //   Do NOT register SetTileEntity / OnOpen / GUIWindowManager close postfixes — they race vanilla TE↔UI sync,
        //   corrupt tile entities, and can destroy chunk saves on write (observed: "Wrong chunk header" on load).
        // - Dew Collector / Apiary are the narrow BlockCollector exception because their shared Collector UI can bypass generic workstation hooks.
        // - Reflection patching targets the live game assembly whenever possible so mock-reference types do not hide runtime signature drift.
        public void InitMod(Mod modInstance)
        {
            ModContentRoot.ApplyFromModApi(modInstance);

            SafeLog("Loading LimitByCraftingSkillMod");
            SafeLog("Mod folder (Config + ClassName map): " + (ModContentRoot.ResolveModDirectory() ?? "(unknown — map may be empty)"));

            var config = ModConfig.Instance;
            SafeLog($"Config loaded: DebugMode={config.DebugMode}");
            BridgeHarmonyLogger(config.DebugMode);
            try
            {
                SafeLog("[LimitByCraftingSkill] ClassName map entries: " + ClassNameToCraftingSkillMapLoader.GetMap().Count);
            }
            catch
            {
                // ignored
            }

            try
            {
                var harmony = new Harmony("com.learninggrounds.limitbycraftingskillmod");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                ApplyEquipItemPatchFromGameAssembly(harmony);
                ApplyEquipmentHandleStackSwapPatchFromGameAssembly(harmony);
                ApplyToolbeltHandleStackSwapPatchFromGameAssembly(harmony);
                ApplyWorkstationVehicleStackSwapPatchesFromGameAssembly(harmony);
                ApplyWorkstationOpenPatchFromGameAssembly(harmony);
                ApplyWorkstationActivationCommandsSafePatchFromGameAssembly(harmony);
                ApplyCollectorOpenRestrictionPatchFromGameAssembly(harmony);
                ApplyVehicleDrivePatchFromGameAssembly(harmony);
                ApplyItemActionSpawnVehiclePatchFromGameAssembly(harmony);
                ApplyItemActionExecuteRestrictionPatchesFromGameAssembly(harmony);
                ApplyAddItemToToolbeltPatchFromGameAssembly(harmony);
                ApplyItemActionEntryEquipPatchFromGameAssembly(harmony);
                ApplyProgressionLevelUpPatchFromGameAssembly(harmony);
                ApplyInventoryGridPatchesFromGameAssembly(harmony);
                ApplyPopupToolTipDisplayTooltipPostfix(harmony);
                ApplyPopupToolTipUpdateTintPostfix(harmony);
                ApplyGameManagerShowTooltipTintPostfixes(harmony);
                ServerConfigSync.Register(harmony);
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


        private static void BridgeHarmonyLogger(bool debugMode)
        {
            try
            {
                HarmonyLib.Tools.Logger.MessageReceived += (sender, args) =>
                {
                    if (args.LogChannel == HarmonyLib.Tools.Logger.LogChannel.Error
                        || args.LogChannel == HarmonyLib.Tools.Logger.LogChannel.Warn
                        || debugMode)
                    {
                        SafeLog($"[Harmony] {args.LogChannel}: {args.Message}");
                    }
                };
            }
            catch
            {
                // HarmonyX logger unavailable in some test environments
            }
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


        /// <summary>HarmonyX: wrap patch body in try/catch; treat as optional soft-skip (missing targets already logged).</summary>
        private static HarmonyMethod SoftPatch(MethodInfo method)
        {
            if (method == null) return null;
            return new HarmonyMethod(method) { wrapTryCatch = true, optional = true };
        }


        private static Type[] GetLoadableTypes(Assembly assembly, string context)
        {
            if (assembly == null) return Type.EmptyTypes;
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                SafeLog("[LimitByCraftingSkill] Partial type load while scanning " + context + ": " + ex.Message);
                if (ex.LoaderExceptions != null)
                {
                    foreach (var loaderEx in ex.LoaderExceptions)
                    {
                        if (loaderEx != null)
                            SafeLog("[LimitByCraftingSkill]   loader: " + loaderEx.Message);
                    }
                }
                if (ex.Types == null) return Type.EmptyTypes;
                var list = new System.Collections.Generic.List<Type>();
                foreach (var t in ex.Types)
                {
                    if (t != null) list.Add(t);
                }
                return list.ToArray();
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
                harmony.Patch(equipItemMethod, prefix: SoftPatch(prefix));
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
                harmony.Patch(addCurrencyMethod, postfix: SoftPatch(postfix));
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
                harmony.Patch(handleStackSwap, prefix: SoftPatch(prefix));
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
                var prefix = typeof(ItemStackHandleStackSwapRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(handleStackSwap, prefix: SoftPatch(prefix));
                SafeLog("[LimitByCraftingSkill] ItemStack.HandleStackSwap (toolbelt + workstation tool drag-drop block) patch applied.");
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
                // XUiC_ItemStack.HandleStackSwap is patched once in ApplyToolbeltHandleStackSwapPatchFromGameAssembly.
                // That combined prefix dispatches both toolbelt and workstation-tool behavior in a deterministic order.
                var partStackType = gameAssembly.GetType("XUiC_BasePartStack");
                if (partStackType != null)
                {
                    var hss2 = partStackType.GetMethod("HandleStackSwap", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (hss2 != null)
                    {
                        var vp = typeof(VehiclePartHandleStackSwapPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(hss2, prefix: SoftPatch(vp));
                        SafeLog("[LimitByCraftingSkill] BasePartStack.HandleStackSwap (vehicle mods) patch applied.");
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] Workstation/vehicle stack swap patches failed: {ex.Message}");
            }
        }

        /// <summary>
        /// v3.0: Block at OnBlockActivated (popup only). UI SetTileEntity/OnOpen/GUIWindowManager postfixes were removed —
        /// they raced vanilla sync, corrupted TEs, and caused chunk save failures.
        /// </summary>
        private static void ApplyWorkstationOpenPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var workstationType = gameAssembly.GetType("BlockWorkstation");
                if (workstationType == null)
                {
                    SafeLog("[LimitByCraftingSkill] BlockWorkstation not found, OnBlockActivated Prefix skipped.");
                    return;
                }

                var wb = gameAssembly.GetType("WorldBase");
                var v3i = gameAssembly.GetType("Vector3i");
                var bv = gameAssembly.GetType("BlockValue");
                var epl = gameAssembly.GetType("EntityPlayerLocal");
                if (wb == null || v3i == null || bv == null || epl == null)
                {
                    SafeLog("[LimitByCraftingSkill] Required game types missing for BlockWorkstation.OnBlockActivated patch.");
                    return;
                }

                var prefix = typeof(WorkstationOpenRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                var prefixWithCommand = typeof(WorkstationOpenRestrictionPatch).GetMethod("PrefixWithCommand", BindingFlags.Static | BindingFlags.Public);
                if (prefix == null || prefixWithCommand == null)
                {
                    SafeLog("[LimitByCraftingSkill] WorkstationOpenRestrictionPatch.Prefix / PrefixWithCommand not found.");
                    return;
                }

                var methodNoCommand = workstationType.GetMethod(
                    "OnBlockActivated",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { wb, v3i, bv, epl },
                    null);
                if (methodNoCommand != null && methodNoCommand.DeclaringType == workstationType)
                {
                    harmony.Patch(methodNoCommand, prefix: SoftPatch(prefix));
                }
                else
                {
                    SafeLog("[LimitByCraftingSkill] BlockWorkstation.OnBlockActivated(WorldBase,Vector3i,BlockValue,EntityPlayerLocal) not found, Prefix skipped.");
                }

                var methodWithCommand = workstationType.GetMethod(
                    "OnBlockActivated",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string), wb, v3i, bv, epl },
                    null);
                if (methodWithCommand != null && methodWithCommand.DeclaringType == workstationType)
                {
                    harmony.Patch(methodWithCommand, prefix: SoftPatch(prefixWithCommand));
                    SafeLog("[LimitByCraftingSkill] BlockWorkstation.OnBlockActivated (command + no-command) Prefix (Workstations gate) applied.");
                }
                else if (methodNoCommand != null)
                {
                    SafeLog("[LimitByCraftingSkill] BlockWorkstation.OnBlockActivated(WorldBase,...) Prefix applied (no String overload).");
                }
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] BlockWorkstation OnBlockActivated Prefix failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Forge and other workstations share <c>BlockWorkstation.GetBlockActivationCommands</c>, which calls
        /// <c>TileEntityWorkstation.InputIsEmpty()</c> without guarding null <c>input</c> arrays on corrupted TEs.
        /// </summary>
        private static void ApplyWorkstationActivationCommandsSafePatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var workstationType = gameAssembly.GetType("BlockWorkstation");
                if (workstationType == null)
                {
                    SafeLog("[LimitByCraftingSkill] BlockWorkstation not found, GetBlockActivationCommands Finalizer skipped.");
                    return;
                }

                var wb = gameAssembly.GetType("WorldBase");
                var v3i = gameAssembly.GetType("Vector3i");
                var bv = gameAssembly.GetType("BlockValue");
                var entityAlive = gameAssembly.GetType("EntityAlive");
                if (wb == null || v3i == null || bv == null || entityAlive == null)
                    return;

                var finalizer = typeof(WorkstationActivationCommandsSafePatch).GetMethod(
                    "FinalizerGetBlockActivationCommands",
                    BindingFlags.Static | BindingFlags.Public);
                if (finalizer == null)
                    return;

                var getCommands = workstationType.GetMethod(
                    "GetBlockActivationCommands",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { wb, bv, v3i, entityAlive },
                    null);
                if (getCommands != null && getCommands.DeclaringType == workstationType)
                {
                    harmony.Patch(getCommands, finalizer: SoftPatch(finalizer));
                    SafeLog("[LimitByCraftingSkill] BlockWorkstation.GetBlockActivationCommands Finalizer (forge-safe) applied.");
                }

                var forgeType = gameAssembly.GetType("BlockForge");
                var forgeFinalizer = typeof(WorkstationActivationCommandsSafePatch).GetMethod(
                    "FinalizerForgeGetActivationText",
                    BindingFlags.Static | BindingFlags.Public);
                if (forgeType != null && forgeFinalizer != null)
                {
                    var getActivationText = forgeType.GetMethod(
                        "GetActivationText",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { wb, bv, v3i, entityAlive },
                        null);
                    if (getActivationText != null && getActivationText.DeclaringType == forgeType)
                    {
                        harmony.Patch(getActivationText, finalizer: SoftPatch(forgeFinalizer));
                        SafeLog("[LimitByCraftingSkill] BlockForge.GetActivationText Finalizer (forge-safe) applied.");
                    }
                }
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] Workstation activation-commands safe patch failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Dew Collector and Apiary use <c>BlockCollector</c> and the <c>dewcollector</c> XUi window group, not <c>WorkstationWindowGroup</c>.
        /// UI-only postfixes can miss some open paths. Prefixing only <c>BlockCollector.OnBlockActivated</c> applies the same Workstations
        /// gate as <see cref="WorkstationOpenRestrictionPatch"/> without re-enabling broad <c>BlockWorkstation</c> prefixes (those correlated with UI lock).
        /// </summary>
        private static void ApplyCollectorOpenRestrictionPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var collectorType = gameAssembly.GetType("BlockCollector");
                if (collectorType == null)
                {
                    SafeLog("[LimitByCraftingSkill] BlockCollector not found, collector OnBlockActivated Prefix skipped.");
                    return;
                }

                var wb = gameAssembly.GetType("WorldBase");
                var v3i = gameAssembly.GetType("Vector3i");
                var bv = gameAssembly.GetType("BlockValue");
                var epl = gameAssembly.GetType("EntityPlayerLocal");
                if (wb == null || v3i == null || bv == null || epl == null)
                {
                    SafeLog("[LimitByCraftingSkill] Required game types missing for BlockCollector.OnBlockActivated patch.");
                    return;
                }

                var prefix = typeof(WorkstationOpenRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                var prefixWithCommand = typeof(WorkstationOpenRestrictionPatch).GetMethod("PrefixWithCommand", BindingFlags.Static | BindingFlags.Public);
                if (prefix == null || prefixWithCommand == null)
                {
                    SafeLog("[LimitByCraftingSkill] WorkstationOpenRestrictionPatch.Prefix / PrefixWithCommand not found.");
                    return;
                }

                var methodNoCommand = collectorType.GetMethod(
                    "OnBlockActivated",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { wb, v3i, bv, epl },
                    null);
                if (methodNoCommand == null)
                {
                    // Back-compat for older game builds that included an activation index parameter.
                    methodNoCommand = collectorType.GetMethod(
                        "OnBlockActivated",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { wb, typeof(int), v3i, bv, epl },
                        null);
                }
                if (methodNoCommand == null || methodNoCommand.DeclaringType != collectorType)
                {
                    SafeLog("[LimitByCraftingSkill] BlockCollector.OnBlockActivated(WorldBase,Vector3i,BlockValue,EntityPlayerLocal) not found, Prefix skipped.");
                    return;
                }

                harmony.Patch(methodNoCommand, prefix: SoftPatch(prefix));

                var methodWithCommand = collectorType.GetMethod(
                    "OnBlockActivated",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string), wb, v3i, bv, epl },
                    null);
                if (methodWithCommand == null)
                {
                    // Back-compat for older game builds that included an activation index parameter.
                    methodWithCommand = collectorType.GetMethod(
                        "OnBlockActivated",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null,
                        new[] { typeof(string), wb, typeof(int), v3i, bv, epl },
                        null);
                }
                if (methodWithCommand != null && methodWithCommand.DeclaringType == collectorType)
                {
                    harmony.Patch(methodWithCommand, prefix: SoftPatch(prefixWithCommand));
                    SafeLog("[LimitByCraftingSkill] BlockCollector.OnBlockActivated (command + no-command) Prefix (Workstations gate) applied.");
                }
                else
                {
                    SafeLog("[LimitByCraftingSkill] BlockCollector.OnBlockActivated(WorldBase,...) Prefix applied (no String overload on BlockCollector).");
                }
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] BlockCollector OnBlockActivated Prefix failed: " + ex.Message);
            }
        }

        /// <summary>
        /// <c>cntDewCollector</c> and <c>cntApiary</c> use <c>XUiC_DewCollectorWindowGroup.SetTileEntity(TileEntityCollector)</c> (XUi
        /// <c>window_group</c> <c>dewcollector</c>, not <c>WorkstationWindowGroup</c>). <c>OnOpen</c> may run before <c>te</c> is set, so
        /// we postfix <c>SetTileEntity</c> and reuse <see cref="WorkstationRestrictionUi.PostfixSetTileEntity"/> (block id from the TE).
        /// </summary>
        private static void ApplyDewCollectorWindowGroupSetTileEntityPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var windowType = gameAssembly.GetType("XUiC_DewCollectorWindowGroup");
                var teType = gameAssembly.GetType("TileEntityCollector");
                if (windowType == null || teType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_DewCollectorWindowGroup or TileEntityCollector not found, SetTileEntity Postfix (collector) skipped.");
                    return;
                }

                var postfix = typeof(WorkstationRestrictionUi).GetMethod("PostfixSetTileEntity", BindingFlags.Static | BindingFlags.Public);
                if (postfix == null)
                    return;

                var method = windowType.GetMethod("SetTileEntity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { teType }, null);
                if (method == null || method.DeclaringType != windowType)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_DewCollectorWindowGroup.SetTileEntity(TileEntityCollector) not found, collector SetTileEntity Postfix skipped.");
                    return;
                }

                harmony.Patch(method, postfix: SoftPatch(postfix));
                SafeLog("[LimitByCraftingSkill] XUiC_DewCollectorWindowGroup.SetTileEntity(TileEntityCollector) Postfix (collector) applied.");
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] DewCollectorWindowGroup SetTileEntity Postfix failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Collector windows can reopen with an already-bound TE, so enforce on OnOpen in addition to SetTileEntity.
        /// </summary>
        private static void ApplyDewCollectorWindowGroupOnOpenPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var windowType = gameAssembly.GetType("XUiC_DewCollectorWindowGroup");
                if (windowType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_DewCollectorWindowGroup not found, OnOpen Postfix (collector) skipped.");
                    return;
                }

                var method = windowType.GetMethod("OnOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (method == null || method.DeclaringType != windowType)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_DewCollectorWindowGroup.OnOpen() not found, collector OnOpen Postfix skipped.");
                    return;
                }

                var postfix = typeof(WorkstationRestrictionUi).GetMethod("PostfixDewCollectorWindowGroupOnOpen", BindingFlags.Static | BindingFlags.Public);
                if (postfix == null)
                    return;

                harmony.Patch(method, postfix: SoftPatch(postfix));
                SafeLog("[LimitByCraftingSkill] XUiC_DewCollectorWindowGroup.OnOpen Postfix (collector) applied.");
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] DewCollectorWindowGroup OnOpen Postfix failed: " + ex.Message);
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
                    return;
                }

                var postfixSt = typeof(WorkstationRestrictionUi).GetMethod("PostfixSetTileEntity", BindingFlags.Static | BindingFlags.Public);
                if (postfixSt == null)
                    return;

                var method = windowType.GetMethod("SetTileEntity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { teType }, null);
                if (method != null)
                {
                    harmony.Patch(method, postfix: SoftPatch(postfixSt));
                    SafeLog("[LimitByCraftingSkill] XUiC_WorkstationWindowGroup.SetTileEntity Postfix patch applied.");
                }

                foreach (var subType in GetLoadableTypes(gameAssembly, "workstation SetTileEntity subtypes"))
                {
                    if (subType == null || !subType.IsClass || subType == windowType || !windowType.IsAssignableFrom(subType))
                        continue;
                    var subMethod = subType.GetMethod("SetTileEntity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new Type[] { teType }, null);
                    if (subMethod == null || subMethod.DeclaringType != subType)
                        continue;
                    try
                    {
                        harmony.Patch(subMethod, postfix: SoftPatch(postfixSt));
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
                        harmony.Patch(craftOnOpen, postfix: SoftPatch(postfixFiltered));
                        SafeLog("[LimitByCraftingSkill] XUiC_CraftingWindowGroup.OnOpen Postfix (filtered to workstation) applied.");
                    }

                    for (var midType = wsType.BaseType; midType != null && midType != craftType; midType = midType.BaseType)
                    {
                        var midOnOpen = midType.GetMethod("OnOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                        if (midOnOpen == null || midOnOpen.DeclaringType != midType)
                            continue;
                        try
                        {
                            harmony.Patch(midOnOpen, postfix: SoftPatch(postfixWs));
                            SafeLog("[LimitByCraftingSkill] " + midType.Name + ".OnOpen Postfix (between Crafting and WorkstationWindowGroup) applied.");
                        }
                        catch (Exception midEx)
                        {
                            SafeLog("[LimitByCraftingSkill] OnOpen postfix on " + midType.Name + " skip: " + midEx.Message);
                        }
                    }
                }

                foreach (var subType in GetLoadableTypes(gameAssembly, "workstation OnOpen subtypes"))
                {
                    if (subType == null || !subType.IsClass || !wsType.IsAssignableFrom(subType))
                        continue;
                    var sm = subType.GetMethod("OnOpen", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (sm == null || sm.DeclaringType != subType)
                        continue;
                    try
                    {
                        harmony.Patch(sm, postfix: SoftPatch(postfixWs));
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

                harmony.Patch(method, postfix: SoftPatch(postfix));
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
                    return;
                }
                var prefix = typeof(VehicleDriveRestrictionPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(enterVehicle, prefix: SoftPatch(prefix));
                SafeLog("[LimitByCraftingSkill] EntityVehicle.EnterVehicle (vehicle drive) patch applied.");
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] Vehicle drive patch failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Workstation <c>workstation_*</c> GUI: Postfix on Open / OpenIfNotOpen / SwitchVisible — close + popup if Workstations level too low.
        /// Prefix+skip caused UI lock; Postfix runs after vanilla Open.
        /// </summary>
        private static void ApplyGUIWindowManagerWorkstationWindowPatchFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var wmType = gameAssembly.GetType("GUIWindowManager");
                if (wmType == null)
                {
                    SafeLog("[LimitByCraftingSkill] GUIWindowManager not found, workstation window Postfix skipped.");
                    return;
                }

                var patchType = typeof(GUIWindowManagerWorkstationWindowPatch);
                var pxGeneric = patchType.GetMethod("PostfixAny_StringFirstArg", BindingFlags.Static | BindingFlags.Public);
                var pxUpdate = patchType.GetMethod("PostfixUpdate_Float", BindingFlags.Static | BindingFlags.Public);
                if (pxGeneric == null || pxUpdate == null)
                    return;

                var patched = 0;
                foreach (var method in wmType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method == null || method.IsAbstract || method.IsGenericMethodDefinition)
                        continue;
                    var n = method.Name;
                    if (!string.Equals(n, "Open", StringComparison.Ordinal) &&
                        !string.Equals(n, "OpenIfNotOpen", StringComparison.Ordinal) &&
                        !string.Equals(n, "SwitchVisible", StringComparison.Ordinal))
                        continue;
                    var ps = method.GetParameters();
                    if (ps == null || ps.Length == 0 || ps[0].ParameterType != typeof(string))
                        continue;
                    try
                    {
                        harmony.Patch(method, postfix: SoftPatch(pxGeneric));
                        patched++;
                        SafeLog("[LimitByCraftingSkill] GUIWindowManager." + n + "(...) workstation Postfix applied.");
                    }
                    catch (Exception exMethod)
                    {
                        SafeLog("[LimitByCraftingSkill] GUIWindowManager." + n + " workstation Postfix skip: " + exMethod.Message);
                    }
                }

                var update = wmType.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(float) }, null);
                if (update != null)
                {
                    try
                    {
                        harmony.Patch(update, postfix: SoftPatch(pxUpdate));
                        patched++;
                        SafeLog("[LimitByCraftingSkill] GUIWindowManager.Update(float) workstation watchdog Postfix applied.");
                    }
                    catch (Exception exUpdate)
                    {
                        SafeLog("[LimitByCraftingSkill] GUIWindowManager.Update(float) workstation watchdog Postfix skip: " + exUpdate.Message);
                    }
                }
                if (patched == 0)
                    SafeLog("[LimitByCraftingSkill] No GUIWindowManager workstation-compatible overloads found.");
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] GUIWindowManager workstation Postfix failed: " + ex.Message);
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
                harmony.Patch(execute, prefix: SoftPatch(prefix));
                SafeLog("[LimitByCraftingSkill] ItemActionSpawnVehicle.ExecuteAction patch applied.");
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] ItemActionSpawnVehicle patch failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Throws, seed placement, rockets — <see cref="ItemAction.ExecuteAction"/> on concrete game types.
        /// </summary>
        private static void ApplyItemActionExecuteRestrictionPatchesFromGameAssembly(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var itemActionDataType = gameAssembly.GetType("ItemActionData");
                if (itemActionDataType == null)
                {
                    SafeLog("[LimitByCraftingSkill] ItemActionData not found — throw/use patches skipped.");
                    return;
                }

                var patched = 0;

                void TryPatch(string gameTypeName, string prefixMethodName)
                {
                    var t = gameAssembly.GetType(gameTypeName);
                    if (t == null) return;

                    MethodInfo execute = null;
                    foreach (var m in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (m.Name != "ExecuteAction") continue;
                        var p = m.GetParameters();
                        if (p.Length != 2) continue;
                        if (!itemActionDataType.IsAssignableFrom(p[0].ParameterType)) continue;
                        var p1 = p[1].ParameterType;
                        if (p1 == typeof(bool) || p1.IsByRef && p1.GetElementType() == typeof(bool))
                        {
                            execute = m;
                            break;
                        }
                    }

                    if (execute == null)
                    {
                        SafeLog($"[LimitByCraftingSkill] {gameTypeName}.ExecuteAction(ItemActionData, bool) not found.");
                        return;
                    }

                    var prefix = typeof(ItemActionExecuteRestrictionPatch).GetMethod(prefixMethodName, BindingFlags.Static | BindingFlags.NonPublic);
                    if (prefix == null)
                    {
                        SafeLog($"[LimitByCraftingSkill] Prefix {prefixMethodName} missing.");
                        return;
                    }

                    harmony.Patch(execute, prefix: SoftPatch(prefix));
                    patched++;
                    SafeLog($"[LimitByCraftingSkill] {gameTypeName}.ExecuteAction restriction prefix applied.");
                }

                TryPatch("ItemActionThrowAway", nameof(ItemActionExecuteRestrictionPatch.PrefixThrowAway));
                TryPatch("ItemActionThrownWeapon", nameof(ItemActionExecuteRestrictionPatch.PrefixThrownWeapon));
                TryPatch("ItemActionPlaceAsBlock", nameof(ItemActionExecuteRestrictionPatch.PrefixPlaceAsBlock));
                TryPatch("ItemActionProjectile", nameof(ItemActionExecuteRestrictionPatch.PrefixProjectile));
                TryPatch("ItemActionEat", nameof(ItemActionExecuteRestrictionPatch.PrefixEat));

                try
                {
                    var eatType = gameAssembly.GetType("ItemActionEat");
                    var xUiItemStackType = gameAssembly.GetType("XUiC_ItemStack");
                    if (eatType != null && xUiItemStackType != null)
                    {
                        var instant = eatType.GetMethod("ExecuteInstantAction", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                            null, new[] { typeof(EntityAlive), typeof(ItemStack), typeof(bool), xUiItemStackType }, null);
                        if (instant != null)
                        {
                            var prefixInstant = typeof(ItemActionExecuteRestrictionPatch).GetMethod(nameof(ItemActionExecuteRestrictionPatch.PrefixEatExecuteInstant), BindingFlags.Static | BindingFlags.Public);
                            if (prefixInstant != null)
                            {
                                harmony.Patch(instant, prefix: SoftPatch(prefixInstant));
                                patched++;
                                SafeLog("[LimitByCraftingSkill] ItemActionEat.ExecuteInstantAction restriction prefix applied.");
                            }
                        }
                    }
                }
                catch (Exception ex2)
                {
                    SafeLog($"[LimitByCraftingSkill] ItemActionEat.ExecuteInstantAction patch failed: {ex2.Message}");
                }

                if (patched == 0)
                    SafeLog("[LimitByCraftingSkill] No ItemAction ExecuteAction restriction patches applied.");
            }
            catch (Exception ex)
            {
                SafeLog($"[LimitByCraftingSkill] ItemAction execute restriction patches failed: {ex.Message}");
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
                    harmony.Patch(addToToolbelt, prefix: SoftPatch(prefix));
                    SafeLog("[LimitByCraftingSkill] XUiM_PlayerInventory.AddItemToToolbelt (equip key block) patch applied.");
                }
                var addToPreferred = playerInvType.GetMethod("AddItemToPreferredToolbeltSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(ItemStack), typeof(int) }, null);
                if (addToPreferred != null)
                {
                    var prefixPref = typeof(AddItemToToolbeltRestrictionPatch).GetMethod("PrefixPreferredSlot", BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(addToPreferred, prefix: SoftPatch(prefixPref));
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
                harmony.Patch(onActivated, prefix: SoftPatch(prefix));
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
                        harmony.Patch(onOpen, postfix: SoftPatch(postfix));
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
                        harmony.Patch(onOpen, postfix: SoftPatch(postfix));
                        SafeLog("[LimitByCraftingSkill] EquipmentStackGrid.OnOpen patch applied.");
                    }
                }
            }
            catch (Exception ex) { SafeLog($"[LimitByCraftingSkill] EquipmentStackGrid OnOpen patch failed: {ex.Message}"); }

            // Slot changes: vanilla redraws labels and clears our red tint; mark dirty so Update re-applies.
            try
            {
                if (itemStackGridType != null)
                {
                    var slotChanged = itemStackGridType.GetMethod("HandleSlotChangedEvent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (slotChanged != null)
                    {
                        var postfix = typeof(ItemStackGridSlotChangedRestrictionColorPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(slotChanged, postfix: SoftPatch(postfix));
                        SafeLog("[LimitByCraftingSkill] ItemStackGrid.HandleSlotChangedEvent (restriction refresh) patch applied.");
                    }
                }
            }
            catch (Exception ex) { SafeLog($"[LimitByCraftingSkill] ItemStackGrid HandleSlotChangedEvent patch failed: {ex.Message}"); }

            try
            {
                if (equipmentStackGridType != null)
                {
                    var slotChanged = equipmentStackGridType.GetMethod("HandleSlotChangedEvent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (slotChanged != null)
                    {
                        var postfix = typeof(EquipmentStackGridSlotChangedRestrictionColorPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(slotChanged, postfix: SoftPatch(postfix));
                        SafeLog("[LimitByCraftingSkill] EquipmentStackGrid.HandleSlotChangedEvent (restriction refresh) patch applied.");
                    }
                }
            }
            catch (Exception ex) { SafeLog($"[LimitByCraftingSkill] EquipmentStackGrid HandleSlotChangedEvent patch failed: {ex.Message}"); }

            // ForceSetItemStack: backpack/loot often sync slots here after a move, redrawing labels after HandleSlotChangedEvent.
            try
            {
                var itemStackType = gameAssembly.GetType("XUiC_ItemStack");
                if (itemStackType != null)
                {
                    var forceSet = itemStackType.GetMethod("ForceSetItemStack", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (forceSet != null)
                    {
                        var postfix = typeof(ItemStackForceSetRestrictionColorPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(forceSet, postfix: SoftPatch(postfix));
                        SafeLog("[LimitByCraftingSkill] ItemStack.ForceSetItemStack (restriction refresh) patch applied.");
                    }
                }
            }
            catch (Exception ex) { SafeLog($"[LimitByCraftingSkill] ItemStack ForceSetItemStack patch failed: {ex.Message}"); }

            // UpdateBackend / SetStacks overrides on common grids (Harmony does not redirect base patches to overrides).
            var backendPostfix = typeof(ItemStackGridBackendRestrictionColorPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
            foreach (var typeName in new[]
            {
                "XUiC_ItemStackGrid",
                "XUiC_Backpack",
                "XUiC_LootContainer",
                "XUiC_BagContainer",
                "XUiC_Toolbelt"
            })
            {
                try
                {
                    var gridType = gameAssembly.GetType(typeName);
                    if (gridType == null) continue;
                    foreach (var methodName in new[] { "UpdateBackend", "SetStacks" })
                    {
                        var method = gridType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                        if (method == null) continue;
                        harmony.Patch(method, postfix: SoftPatch(backendPostfix));
                        SafeLog($"[LimitByCraftingSkill] {typeName}.{methodName} (restriction refresh) patch applied.");
                    }
                }
                catch (Exception ex) { SafeLog($"[LimitByCraftingSkill] {typeName} backend restriction patch failed: {ex.Message}"); }
            }

            try
            {
                var backpackType = gameAssembly.GetType("XUiC_Backpack");
                if (backpackType != null)
                {
                    foreach (var methodName in new[] { "RefreshBackpackSlots", "PlayerInventory_OnBackpackItemsChanged" })
                    {
                        var method = backpackType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method == null) continue;
                        harmony.Patch(method, postfix: SoftPatch(backendPostfix));
                        SafeLog($"[LimitByCraftingSkill] XUiC_Backpack.{methodName} (restriction refresh) patch applied.");
                    }
                }
            }
            catch (Exception ex) { SafeLog($"[LimitByCraftingSkill] Backpack refresh restriction patch failed: {ex.Message}"); }

            // Update: declared on XUiController, so patch the base type once; our Postfix filters by instance type
            if (xuiControllerType != null)
            {
                try
                {
                    var update = xuiControllerType.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
                    if (update != null)
                    {
                        var postfix = typeof(GridUpdateRestrictionColorPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(update, postfix: SoftPatch(postfix));
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
                    harmony.Patch(display, postfix: SoftPatch(postfix));
                    SafeLog("[LimitByCraftingSkill] XUiC_PopupToolTip.DisplayTooltipText restriction-color Postfix applied.");
                }
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] PopupToolTip DisplayTooltipText postfix failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Postfix XUiC_PopupToolTip.Update — keep restriction tint after vanilla refreshes widgets each frame.
        /// </summary>
        private static void ApplyPopupToolTipUpdateTintPostfix(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(Equipment).Assembly;
                var popupType = gameAssembly.GetType("XUiC_PopupToolTip");
                if (popupType == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_PopupToolTip not found, Update tint postfix skipped.");
                    return;
                }

                var update = popupType.GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(float) }, null);
                if (update == null)
                {
                    SafeLog("[LimitByCraftingSkill] XUiC_PopupToolTip.Update not found, tint postfix skipped.");
                    return;
                }

                var postfix = typeof(PopupToolTipUpdateTintPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                if (postfix != null)
                {
                    harmony.Patch(update, postfix: SoftPatch(postfix));
                    SafeLog("[LimitByCraftingSkill] XUiC_PopupToolTip.Update restriction-color Postfix applied.");
                }
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] PopupToolTip Update postfix failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Postfix GameManager.ShowTooltip / ShowTooltipMP — Schedule restriction label tint using the exact tooltip string.
        /// </summary>
        private static void ApplyGameManagerShowTooltipTintPostfixes(Harmony harmony)
        {
            try
            {
                var gmType = typeof(GameManager);
                var gameAssembly = gmType.Assembly;
                var eplType = gameAssembly.GetType("EntityPlayerLocal");
                var epType = gameAssembly.GetType("EntityPlayer");
                var toolTipEventType = gameAssembly.GetType("ToolTipEvent");
                var postfix = typeof(GameManagerShowTooltipTintPatch).GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public);
                if (postfix == null)
                    return;

                var patched = 0;

                if (eplType != null && toolTipEventType != null)
                {
                    var sig9 = new[] { eplType, typeof(string), typeof(string), typeof(string), toolTipEventType, typeof(bool), typeof(bool), typeof(float) };
                    var m = gmType.GetMethod("ShowTooltip", BindingFlags.Static | BindingFlags.Public, null, sig9, null);
                    if (m != null)
                    {
                        harmony.Patch(m, postfix: SoftPatch(postfix));
                        patched++;
                    }

                    var sig9Arr = new[] { eplType, typeof(string), typeof(string[]), typeof(string), toolTipEventType, typeof(bool), typeof(bool), typeof(float) };
                    m = gmType.GetMethod("ShowTooltip", BindingFlags.Static | BindingFlags.Public, null, sig9Arr, null);
                    if (m != null)
                    {
                        harmony.Patch(m, postfix: SoftPatch(postfix));
                        patched++;
                    }
                }

                if (eplType != null)
                {
                    var sig5 = new[] { eplType, typeof(string), typeof(bool), typeof(bool), typeof(float) };
                    var m5 = gmType.GetMethod("ShowTooltip", BindingFlags.Static | BindingFlags.Public, null, sig5, null);
                    if (m5 != null)
                    {
                        harmony.Patch(m5, postfix: SoftPatch(postfix));
                        patched++;
                    }
                }

                if (epType != null)
                {
                    var sigMp = new[] { epType, typeof(string), typeof(string) };
                    var mMp = gmType.GetMethod("ShowTooltipMP", BindingFlags.Static | BindingFlags.Public, null, sigMp, null);
                    if (mMp != null)
                    {
                        harmony.Patch(mMp, postfix: SoftPatch(postfix));
                        patched++;
                    }
                }

                if (patched > 0)
                    SafeLog($"[LimitByCraftingSkill] GameManager.ShowTooltip tint Postfix applied to {patched} overload(s).");
                else
                    SafeLog("[LimitByCraftingSkill] GameManager ShowTooltip/ShowTooltipMP overloads not found; tint postfix skipped.");
            }
            catch (Exception ex)
            {
                SafeLog("[LimitByCraftingSkill] GameManager ShowTooltip tint postfix failed: " + ex.Message);
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
