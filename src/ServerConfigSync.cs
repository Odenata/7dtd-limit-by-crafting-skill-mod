using System;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace LimitByCraftingSkillMod
{
    internal static class ServerConfigSync
    {
        internal const string ConfigPackageName = "LimitByCraftingSkillMod/Config.xml";

        internal static void Register(Harmony harmony)
        {
            TryRegisterModEvent("PlayerSpawnedInWorld", "OnPlayerSpawnedInWorld");
            TryRegisterModEvent("PlayerJoinedGame", "OnPlayerJoinedGame");
            TryRegisterModEvent("PlayerDisconnected", "OnPlayerDisconnected");
            TryRegisterModEvent("MainMenuOpening", "OnMainMenuOpening");
            TryPatchNetPackageConfigFile(harmony);
        }

        internal static void OnPlayerSpawnedInWorld(object data)
        {
            var clientInfo = GetMemberValue(data, "ClientInfo");
            SendConfigToClient(clientInfo, "PlayerSpawnedInWorld");
        }

        internal static void OnPlayerJoinedGame(object data)
        {
            var clientInfo = GetMemberValue(data, "ClientInfo");
            SendConfigToClient(clientInfo, "PlayerJoinedGame");
        }

        internal static void OnPlayerDisconnected(object data)
        {
            ClearClientSnapshot("PlayerDisconnected");
        }

        internal static void OnMainMenuOpening(object data)
        {
            ClearClientSnapshot("MainMenuOpening");
        }

        internal static void OnNetPackageConfigFileProcessed(object package)
        {
            try
            {
                var name = GetMemberValue(package, "name") as string;
                if (!string.Equals(name, ConfigPackageName, StringComparison.Ordinal))
                    return;

                var data = GetMemberValue(package, "data") as byte[];
                if (data == null || data.Length == 0)
                {
                    ModApi.DebugLog("Received empty server Config.xml package.");
                    return;
                }

                var xml = Encoding.UTF8.GetString(data);
                if (ModConfig.TryApplyServerXml(xml, "server sync", out var error))
                {
                    var config = ModConfig.Instance;
                    ModApi.DebugLog("Applied server Config.xml sync; hash=" + config.Hash);
                }
                else
                {
                    ModApi.DebugLog("Rejected server Config.xml sync: " + error);
                }
            }
            catch (Exception ex)
            {
                ModApi.DebugLog("Server config package processing failed: " + ex.Message);
            }
        }

        private static void SendConfigToClient(object clientInfo, string reason)
        {
            if (clientInfo == null) return;
            try
            {
                var xml = ModConfig.GetLocalConfigXmlForSync();
                var bytes = Encoding.UTF8.GetBytes(xml);
                var gameAssembly = typeof(GameManager).Assembly;
                var packageType = gameAssembly.GetType("NetPackageConfigFile");
                if (packageType == null)
                {
                    ModApi.DebugLog("NetPackageConfigFile not found; server Config.xml sync skipped.");
                    return;
                }

                var package = Activator.CreateInstance(packageType);
                var setup = packageType.GetMethod("Setup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(string), typeof(byte[]) }, null);
                package = setup?.Invoke(package, new object[] { ConfigPackageName, bytes }) ?? package;

                var sendPackage = clientInfo.GetType().GetMethod("SendPackage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (sendPackage == null)
                {
                    ModApi.DebugLog("ClientInfo.SendPackage not found; server Config.xml sync skipped.");
                    return;
                }

                sendPackage.Invoke(clientInfo, new[] { package });
                ModApi.DebugLog("Sent server Config.xml sync via " + reason + "; bytes=" + bytes.Length);
            }
            catch (Exception ex)
            {
                ModApi.DebugLog("Server Config.xml sync send failed: " + ex.Message);
            }
        }

        private static void TryPatchNetPackageConfigFile(Harmony harmony)
        {
            try
            {
                var gameAssembly = typeof(GameManager).Assembly;
                var packageType = gameAssembly.GetType("NetPackageConfigFile");
                if (packageType == null)
                {
                    ModApi.DebugLog("NetPackageConfigFile not found; server Config.xml receive hook skipped.");
                    return;
                }

                var process = packageType.GetMethod("ProcessPackage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var postfix = typeof(ServerConfigSync).GetMethod(nameof(PostfixNetPackageConfigFileProcessPackage), BindingFlags.Static | BindingFlags.Public);
                if (process == null || postfix == null)
                {
                    ModApi.DebugLog("NetPackageConfigFile.ProcessPackage hook not found.");
                    return;
                }

                harmony.Patch(process, postfix: new HarmonyMethod(postfix));
                ModApi.DebugLog("NetPackageConfigFile.ProcessPackage server config sync Postfix applied.");
            }
            catch (Exception ex)
            {
                ModApi.DebugLog("NetPackageConfigFile server config sync patch failed: " + ex.Message);
            }
        }

        private static void ClearClientSnapshot(string reason)
        {
            try
            {
                if (!ModConfig.Instance.IsServerProvided)
                    return;
                ModConfig.ClearServerSnapshot();
                ModApi.DebugLog("Cleared server Config.xml sync via " + reason + ".");
            }
            catch (Exception ex)
            {
                ModApi.DebugLog("Clearing server Config.xml sync failed: " + ex.Message);
            }
        }

        public static void PostfixNetPackageConfigFileProcessPackage(object __instance)
        {
            OnNetPackageConfigFileProcessed(__instance);
        }

        private static void TryRegisterModEvent(string eventName, string handlerName)
        {
            try
            {
                var modEventsType = typeof(GameManager).Assembly.GetType("ModEvents");
                if (modEventsType == null)
                {
                    ModApi.DebugLog("ModEvents type not found; server config sync hook skipped.");
                    return;
                }
                var eventMember = modEventsType.GetProperty(eventName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null, null)
                    ?? (object)modEventsType.GetField(eventName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
                if (eventMember == null)
                {
                    ModApi.DebugLog("ModEvents." + eventName + " not found; server config sync hook skipped.");
                    return;
                }

                var register = eventMember.GetType().GetMethod("RegisterHandler", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (register == null)
                {
                    ModApi.DebugLog("ModEvents." + eventName + ".RegisterHandler not found; server config sync hook skipped.");
                    return;
                }

                var handlerType = register.GetParameters()[0].ParameterType;
                var invoke = handlerType.GetMethod("Invoke");
                var parameters = invoke?.GetParameters();
                if (parameters == null || parameters.Length != 1 || !parameters[0].ParameterType.IsByRef)
                {
                    ModApi.DebugLog("ModEvents." + eventName + " handler signature not supported.");
                    return;
                }

                var dataType = parameters[0].ParameterType.GetElementType();
                var adapterType = typeof(ModEventRefAdapter<>).MakeGenericType(dataType);
                var adapter = Activator.CreateInstance(adapterType, handlerName);
                var method = adapterType.GetMethod("Handle", BindingFlags.Instance | BindingFlags.Public);
                if (invoke?.ReturnType != typeof(void))
                {
                    method = adapterType
                        .GetMethod("HandleInterruptible", BindingFlags.Instance | BindingFlags.Public)
                        ?.MakeGenericMethod(invoke.ReturnType);
                }
                if (method == null)
                {
                    ModApi.DebugLog("ModEvents." + eventName + " adapter method not found.");
                    return;
                }
                var del = Delegate.CreateDelegate(handlerType, adapter, method);
                register.Invoke(eventMember, new object[] { del });
                ModApi.DebugLog("Registered server config sync handler for ModEvents." + eventName + ".");
            }
            catch (Exception ex)
            {
                ModApi.DebugLog("Register server config sync " + eventName + " failed: " + ex.Message);
            }
        }

        private static object GetMemberValue(object instance, string name)
        {
            if (instance == null) return null;
            var type = instance.GetType();
            var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null) return prop.GetValue(instance, null);
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(instance);
        }

        private sealed class ModEventRefAdapter<TData>
        {
            private readonly string _handlerName;

            public ModEventRefAdapter(string handlerName)
            {
                _handlerName = handlerName;
            }

            public void Handle(ref TData data)
            {
                if (string.Equals(_handlerName, "OnPlayerSpawnedInWorld", StringComparison.Ordinal))
                    OnPlayerSpawnedInWorld(data);
                else if (string.Equals(_handlerName, "OnPlayerJoinedGame", StringComparison.Ordinal))
                    OnPlayerJoinedGame(data);
                else if (string.Equals(_handlerName, "OnPlayerDisconnected", StringComparison.Ordinal))
                    OnPlayerDisconnected(data);
                else if (string.Equals(_handlerName, "OnMainMenuOpening", StringComparison.Ordinal))
                    OnMainMenuOpening(data);
            }

            public TResult HandleInterruptible<TResult>(ref TData data)
            {
                Handle(ref data);
                return ResolveContinueResult<TResult>();
            }

            private static TResult ResolveContinueResult<TResult>()
            {
                var modEventsType = typeof(GameManager).Assembly.GetType("ModEvents");
                var resultType = modEventsType?.GetNestedType("EModEventResult", BindingFlags.Public | BindingFlags.NonPublic);
                if (resultType == null)
                    throw new InvalidOperationException("ModEvents.EModEventResult type not found.");

                var continueName = Enum.IsDefined(resultType, "Continue")
                    ? "Continue"
                    : Enum.GetNames(resultType)[0];
                return (TResult)Enum.Parse(resultType, continueName);
            }
        }
    }

}
