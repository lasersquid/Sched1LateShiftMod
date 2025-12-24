using HarmonyLib;
using MelonLoader;
using UnityEngine.Events;
using System.Runtime.CompilerServices;
using System.Reflection;




#if MONO_BUILD
using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.Dialogue;
using ScheduleOne.Employees;
using ScheduleOne.GameTime;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.Money;
using ScheduleOne.ObjectScripts;
using Collections = System.Collections.Generic;
#else
using Il2CppFishNet;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.ObjectScripts;
using Collections = Il2CppSystem.Collections.Generic;
#endif


namespace LateShift
{
    public static class Utils
    {
        private static LateShiftMod Mod;

        public static void SetMod(LateShiftMod mod)
        {
            Mod = mod;
        }

        public static MelonPreferences_Category GetMelonPrefs()
        {
            return Mod.melonPrefs;
        }

        public static void PrintException(Exception e)
        {
            Utils.Warn($"Exception: {e.GetType().Name} - {e.Message}");
            Utils.Warn($"Source: {e.Source}");
            Utils.Warn($"{e.StackTrace}");
            if (e.InnerException != null)
            {
                Utils.Warn($"Inner exception: {e.InnerException.GetType().Name} - {e.InnerException.Message}");
                Utils.Warn($"Source: {e.InnerException.Source}");
                Utils.Warn($"{e.InnerException.StackTrace}");
                if (e.InnerException.InnerException != null)
                {
                    Utils.Warn($"Inner inner exception: {e.InnerException.InnerException.GetType().Name} - {e.InnerException.InnerException.Message}");
                    Utils.Warn($"Source: {e.InnerException.InnerException.Source}");
                    Utils.Warn($"{e.InnerException.InnerException.StackTrace}");
                }
            }
        }

        public static void Log(string message)
        {
            Mod.LoggerInstance.Msg(message);
        }

        public static void Warn(string message)
        {
            Mod.LoggerInstance.Warning(message);
        }

        public static void Debug(string message)
        {
            if (Utils.GetMelonPrefs().GetEntry<bool>("debugLogs").Value)
            {
                Mod.LoggerInstance.Msg($"DEBUG: {message}");
            }
        }

        public static void VerboseLog(string message)
        {
            if (Utils.GetMelonPrefs().GetEntry<bool>("verboseLogs").Value)
            {
                Mod.LoggerInstance.Msg(message);
            }
        }

        public static object GetField(Type type, string fieldName, object target)
        {
#if MONO_BUILD
            return AccessTools.Field(type, fieldName).GetValue(target);
#else
            return AccessTools.Property(type, fieldName).GetValue(target);
#endif
        }

        public static void SetField(Type type, string fieldName, object target, object value)
        {
#if MONO_BUILD
            AccessTools.Field(type, fieldName).SetValue(target, value);
#else
            AccessTools.Property(type, fieldName).SetValue(target, value);
#endif
        }

        public static object GetProperty(Type type, string fieldName, object target)
        {
            return AccessTools.Property(type, fieldName).GetValue(target);
        }

        public static void SetProperty(Type type, string fieldName, object target, object value)
        {
            AccessTools.Property(type, fieldName).SetValue(target, value);
        }

        public static object CallMethod(Type type, string methodName, object target, object[] args)
        {
            return AccessTools.Method(type, methodName).Invoke(target, args);
        }

        public static object CallMethod(Type type, string methodName, Type[] argTypes, object target, object[] args)
        {
            return AccessTools.Method(type, methodName, argTypes).Invoke(target, args);
        }

        public static T CastTo<T>(object o) where T : class
        {
            if (o is T)
            {
                return (T)o;
            }
            else
            {
                return null;
            }
        }

        public static bool Is<T>(object o)
        {
            return o is T;
        }

#if !MONO_BUILD
        public static T CastTo<T>(Il2CppSystem.Object o) where T : Il2CppObjectBase
        {
            return o.TryCast<T>();
        }

        public static bool Is<T>(Il2CppSystem.Object o) where T : Il2CppObjectBase
        {
            return o.TryCast<T>() != null;
        }
#endif

        public static UnityAction ToUnityAction(Action action)
        {
#if MONO_BUILD
            return new UnityAction(action);
#else
            return DelegateSupport.ConvertDelegate<UnityAction>(action);
#endif
        }

        public static UnityAction<T> ToUnityAction<T>(Action<T> action)
        {
#if MONO_BUILD
            return new UnityAction<T>(action);
#else
            return DelegateSupport.ConvertDelegate<UnityAction<T>>(action);
#endif
        }
    }

    [HarmonyPatch]
    public class NoBedsPatches
    {
        [HarmonyPatch(typeof(Employee), "IsPayAvailable")]
        [HarmonyPrefix]
        public static bool IsPayAvailablePrefix(Employee __instance, ref bool __result)
        {
            MoneyManager moneyManager = NetworkSingleton<MoneyManager>.Instance;
            if (Utils.GetMelonPrefs().GetEntry<bool>("workWithoutBeds").Value)
            {
                if (Utils.GetMelonPrefs().GetEntry<bool>("payEmployeesFromBank").Value)
                {
                    __result = moneyManager.onlineBalance >= __instance.DailyWage;
                    return false;
                }
                else
                {
                    __result = moneyManager.cashBalance >= __instance.DailyWage;
                }
            }
            else
            {
                EmployeeHome home = __instance.GetHome();
                if (home == null)
                {
                    __result = false;
                    return false;
                }
                __result = home.GetCashSum() >= __instance.DailyWage;
                return false;
            }
            return false;
        }

        [HarmonyPatch(typeof(Employee), "RemoveDailyWage")]
        [HarmonyPrefix]
        public static bool RemoveDailyWagePrefix(Employee __instance)
        {
            MoneyManager moneyManager = NetworkSingleton<MoneyManager>.Instance;
            if (Utils.GetMelonPrefs().GetEntry<bool>("workWithoutBeds").Value)
            {
                if (Utils.GetMelonPrefs().GetEntry<bool>("payEmployeesFromBank").Value)
                {
                    if (moneyManager.onlineBalance >= __instance.DailyWage)
                    {
                        // Record employee pay as a debit from the online balance.
                        moneyManager.CreateOnlineTransaction("Employee Pay", -__instance.DailyWage, 1f, $"{__instance.fullName}, employeetype, location");
                    }
                }
                else
                {
                    if (moneyManager.cashBalance >= __instance.DailyWage)
                    {
                        moneyManager.ChangeCashBalance(-__instance.DailyWage);
                    }
                }
            }
            else
            {
                EmployeeHome home = __instance.GetHome();
                if (home == null)
                {
                    return false;
                }
                if (home.GetCashSum() >= __instance.DailyWage)
                {
                    home.RemoveCash(__instance.DailyWage);
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(Employee), "GetWorkIssue")]
        [HarmonyPrefix]
        public static bool GetWorkIssuePrefix(Employee __instance, ref bool __result, ref DialogueContainer notWorkingReason)
        {
            if (__instance.GetHome() == null && !Utils.GetMelonPrefs().GetEntry<bool>("workWithoutBeds").Value)
            {
                notWorkingReason = __instance.BedNotAssignedDialogue;
                __result = true;
                return false;
            }

            if (!__instance.PaidForToday)
            {
                notWorkingReason = __instance.NotPaidDialogue;
                __result = true;
                return false;
            }
            var workIssues = Utils.CastTo<Collections.List<Employee.NoWorkReason>>(Utils.GetField(typeof(Employee), "WorkIssues", __instance));
            if (__instance.TimeSinceLastWorked >= 5 && workIssues.Count > 0)
            {
                notWorkingReason = UnityEngine.Object.Instantiate<DialogueContainer>(__instance.WorkIssueDialogueTemplate);
                notWorkingReason.GetDialogueNodeByLabel("ENTRY").DialogueText = workIssues[0].Reason;
                if (!string.IsNullOrEmpty(workIssues[0].Fix))
                {
                    notWorkingReason.GetDialogueNodeByLabel("FIX").DialogueText = workIssues[0].Fix;
                }
                else
                {
                    notWorkingReason.GetDialogueNodeByLabel("ENTRY").choices = new DialogueChoiceData[0];
                }
                __result = true;
                return false;
            }
            notWorkingReason = null;
            __result = false;

            return false;
        }

        // Convert prefix/replacement with postfix, and modify result only in cases we're interested in.
        // This should reduce overall fragility.
        [HarmonyPatch(typeof(Employee), "CanWork")]
        [HarmonyPostfix]
        public static void CanWorkPostfix(Employee __instance, ref bool __result)
        {
            bool hasHome = __instance.GetHome() != null;
            bool paidForToday = __instance.PaidForToday;
            bool isEndOfDay = NetworkSingleton<TimeManager>.Instance.IsEndOfDay;
            bool workWithoutBeds = Utils.GetMelonPrefs().GetEntry<bool>("workWithoutBeds").Value;
            bool employeesAlwaysWork = Utils.GetMelonPrefs().GetEntry<bool>("employeesAlwaysWork").Value;

            // Relax bed/locker requirement when enabled
            if (!__result && !hasHome && paidForToday && !isEndOfDay && workWithoutBeds)
            {
                __result = true;
            }

            // Relax 4AM cutoff when enabled (has home)
            if (!__result && hasHome && paidForToday && isEndOfDay && employeesAlwaysWork)
            {
                __result = true;
            }

            // Relax both for bedless workers at end-of-day when both prefs allow it
            if (!__result && !hasHome && paidForToday && isEndOfDay && workWithoutBeds && employeesAlwaysWork)
            {
                __result = true;
            }
        }


        // Completely replace original method body.
        // While a postfix that only activates in applicable conditions would be less fragile,
        // we need to keep a copy of Employee.UpdateBehaviour available for Packager.UpdateBehaviour to call.
        // Unfortunately, it's not possible for reflection to access overridden virtual instance methods.
        // If we didn't need to support IL2CPP, a reverse patch could grab the modified Employee.UpdateBehaviour, but alas.
        // Since we need an accessible copy for the packager anyway, just completely replace the target method.
        [HarmonyPatch(typeof(Employee), "UpdateBehaviour")]
        [HarmonyPrefix]
        public static bool UpdateBehaviourPrefix(Employee __instance)
        {
            if (__instance.Fired)
            {
                return false;
            }
            if (__instance.Behaviour.activeBehaviour == null || __instance.Behaviour.activeBehaviour == __instance.WaitOutside)
            {
                bool notWorking = false;
                bool canBePaid = false;
                if (__instance.GetHome() == null && !Utils.GetMelonPrefs().GetEntry<bool>("workWithoutBeds").Value)
                {
                    notWorking = true;
                    __instance.SubmitNoWorkReason("I haven't been assigned a locker", "You can use your management clipboard to assign me a locker.", 0);
                }
                else if (NetworkSingleton<TimeManager>.Instance.IsEndOfDay && !Utils.GetMelonPrefs().GetEntry<bool>("employeesAlwaysWork").Value)
                {
                    notWorking = true;
                    __instance.SubmitNoWorkReason("Sorry boss, my shift ends at 4AM.", string.Empty, 0);
                }
                else if (!__instance.PaidForToday)
                {
                    if (__instance.IsPayAvailable())
                    {
                        canBePaid = true;
                    }
                    else
                    {
                        notWorking = true;
                        __instance.SubmitNoWorkReason("I haven't been paid yet", "You can place cash in my locker.", 0);
                    }
                }
                if (notWorking)
                {
                    Utils.CallMethod(typeof(Employee), "SetWaitOutside", __instance, [true]);
                    return false;
                }
                if (InstanceFinder.IsServer && canBePaid && __instance.IsPayAvailable())
                {
                    __instance.RemoveDailyWage();
                    __instance.SetIsPaid();
                }
            }
            return false;
        }



        // CanWork is probably inlined. Replace Packager.UpdateBehaviour with original method body.
        [HarmonyPatch(typeof(Packager), "UpdateBehaviour")]
        [HarmonyPrefix]
        public static bool PackagerUpdateBehaviourPrefix(Packager __instance)
        {
            // original: call base.UpdateBehaviour()
            // since Employee.UpdateBehaviour is virtual and overridden in Packager,
            // it's not accessible through reflection.
            // Just call our local modified replacement method instead.
            UpdateBehaviourPrefix(__instance);
            if (__instance.PackagingBehaviour.Active)
            {
                Utils.CallMethod(typeof(Packager), "MarkIsWorking", __instance, []);
                return false;
            }
            if (__instance.MoveItemBehaviour.Active)
            {
                Utils.CallMethod(typeof(Packager), "MarkIsWorking", __instance, []);
                return false;
            }
            if (__instance.Fired)
            {
                Utils.CallMethod(typeof(Packager), "LeavePropertyAndDespawn", __instance, []);
                return false;
            }
            // This was probably inlined
            if (!(bool)Utils.CallMethod(typeof(Packager), "CanWork", __instance, []))
            {
                return false;
            }
            PackagerConfiguration configuration = Utils.CastTo<PackagerConfiguration>(Utils.GetProperty(typeof(Packager), "configuration", __instance));
            if (configuration.AssignedStationCount + configuration.Routes.Routes.Count == 0)
            {
                __instance.SubmitNoWorkReason("I haven't been assigned to any stations or routes.", "You can use your management clipboards to assign stations or routes to me.", 0);
                __instance.SetIdle(true);
                return false;
            }
            if (!InstanceFinder.IsServer)
            {
                return false;
            }
            PackagingStation stationToAttend = Utils.CastTo<PackagingStation>(Utils.CallMethod(typeof(Packager), "GetStationToAttend", __instance, []));
            if (stationToAttend != null)
            {
                Utils.CallMethod(typeof(Packager), "StartPackaging", __instance, [stationToAttend]);
                return false;
            }
            BrickPress brickPress = Utils.CastTo<BrickPress>(Utils.CallMethod(typeof(Packager), "GetBrickPress", __instance, []));
            if (brickPress != null)
            {
                Utils.CallMethod(typeof(Packager), "StartPress", __instance, [brickPress]);
                return false;
            }
            PackagingStation stationMoveItems = Utils.CastTo<PackagingStation>(Utils.CallMethod(typeof(Packager), "GetStationMoveItems", __instance, []));
            if (stationMoveItems != null)
            {
                Utils.CallMethod(typeof(Packager), "StartMoveItem", [typeof(PackagingStation)], __instance, [stationMoveItems]);
                return false;
            }
            BrickPress brickPressMoveItems = Utils.CastTo<BrickPress>(Utils.CallMethod(typeof(Packager), "GetBrickPressMoveItems", __instance, []));
            if (brickPressMoveItems != null)
            {
                Utils.CallMethod(typeof(Packager), "StartMoveItem", [typeof(BrickPress)], __instance, [brickPressMoveItems]);
                return false;
            }

            // GetTransitRouteReady uses an out parameter. Changes to out parameters are captured in
            // the args array, so keep a handle to it and copy the value back out after.
            ItemInstance itemInstance = null;
            object[] args = new object[1] { itemInstance };
            AdvancedTransitRoute transitRouteReady = Utils.CastTo<AdvancedTransitRoute>(Utils.CallMethod(typeof(Packager), "GetTransitRouteReady", __instance, args));
            itemInstance = Utils.CastTo<ItemInstance>(args[0]);
            if (transitRouteReady != null)
            {
                __instance.MoveItemBehaviour.Initialize(transitRouteReady, itemInstance, itemInstance.Quantity, false);
                __instance.MoveItemBehaviour.Enable_Networked();
                return false;
            }
            __instance.SubmitNoWorkReason("There's nothing for me to do right now.", "I need one of my assigned stations to have enough product and packaging to get to work.", 0);
            __instance.SetIdle(true);
            return false;
        }
    }
}

