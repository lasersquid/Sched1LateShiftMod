using HarmonyLib;
using MelonLoader;
using UnityEngine.Events;


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
#endif


namespace LateShift
{

    public class Sched1PatchesBase
    {
        protected static LateShiftMod Mod;

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

        public static void SetMod(LateShiftMod mod)
        {
            Mod = mod;
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

        public static void Log(string message)
        {
            Mod.LoggerInstance.Msg(message);
        }

        public static void Warn(string message)
        {
            Mod.LoggerInstance.Warning(message);
        }

        public static void RestoreDefaults()
        {
            throw new NotImplementedException();
        }
    }


    [HarmonyPatch]
    public class NoBedsPatches : Sched1PatchesBase
    {

        [HarmonyPatch(typeof(Employee), "IsPayAvailable")]
        [HarmonyPrefix]
        public static bool IsPayAvailablePrefix(Employee __instance, ref bool __result)
        {
            MoneyManager moneyManager = NetworkSingleton<MoneyManager>.Instance;
            if (Mod.melonPrefs.GetEntry<bool>("workWithoutBeds").Value)
            {
                if (Mod.melonPrefs.GetEntry<bool>("payEmployeesFromBank").Value)
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
            if (Mod.melonPrefs.GetEntry<bool>("workWithoutBeds").Value)
            {
                if (Mod.melonPrefs.GetEntry<bool>("payEmployeesFromBank").Value)
                {
                    if (moneyManager.onlineBalance >= __instance.DailyWage)
                    {
                        moneyManager.CreateOnlineTransaction("Employee Pay", __instance.DailyWage, 1f, $"{__instance.fullName}, employeetype, location");
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
            if (__instance.GetHome() == null && !Mod.melonPrefs.GetEntry<bool>("workWithoutBeds").Value)
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
#if MONO_BUILD
            var workIssues = CastTo<List<Employee.NoWorkReason>>(GetField(typeof(Employee), "WorkIssues", __instance));
#else
            var workIssues = CastTo<Il2CppSystem.Collections.Generic.List<Employee.NoWorkReason>>(GetField(typeof(Employee), "WorkIssues", __instance));
#endif
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

        [HarmonyPatch(typeof(Employee), "CanWork")]
        [HarmonyPrefix]
        public static bool CanWorkPrefix(Employee __instance, ref bool __result)
        {
            __result = ((__instance.GetHome() != null) || Mod.melonPrefs.GetEntry<bool>("workWithoutBeds").Value) &&
                (!NetworkSingleton<TimeManager>.Instance.IsEndOfDay || Mod.melonPrefs.GetEntry<bool>("employeesAlwaysWork").Value) &&
                __instance.PaidForToday;

            return false;
        }

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
                bool flag = false;
                bool flag2 = false;
                if (__instance.GetHome() == null && !Mod.melonPrefs.GetEntry<bool>("workWithoutBeds").Value)
                {
                    flag = true;
                    __instance.SubmitNoWorkReason("I haven't been assigned a locker", "You can use your management clipboard to assign me a locker.", 0);
                }
                else if (NetworkSingleton<TimeManager>.Instance.IsEndOfDay && !Mod.melonPrefs.GetEntry<bool>("employeesAlwaysWork").Value)
                {
                    flag = true;
                    __instance.SubmitNoWorkReason("Sorry boss, my shift ends at 4AM.", string.Empty, 0);
                }
                else if (!__instance.PaidForToday)
                {
                    if (__instance.IsPayAvailable())
                    {
                        flag2 = true;
                    }
                    else
                    {
                        flag = true;
                        __instance.SubmitNoWorkReason("I haven't been paid yet", "You can place cash in my locker.", 0);
                    }
                }
                if (flag)
                {
                    CallMethod(typeof(Employee), "SetWaitOutside", __instance, [true]);
                    return false;
                }
                if (InstanceFinder.IsServer && flag2 && __instance.IsPayAvailable())
                {
                    __instance.RemoveDailyWage();
                    __instance.SetIsPaid();
                }
            }
            return false;
        }

        // CanWork is probably inlined. Replace with original method body.
        [HarmonyPatch(typeof(Packager), "UpdateBehaviour")]
        [HarmonyPrefix]
        public static bool PackagerUpdateBehaviourPrefix(Packager __instance)
        {
            UpdateBehaviourPrefix(__instance);
            if (__instance.PackagingBehaviour.Active)
            {
                CallMethod(typeof(Packager), "MarkIsWorking", __instance, []);
                return false;
            }
            if (__instance.MoveItemBehaviour.Active)
            {
                CallMethod(typeof(Packager), "MarkIsWorking", __instance, []);
                return false;
            }
            if (__instance.Fired)
            {
                CallMethod(typeof(Packager), "LeavePropertyAndDespawn", __instance, []);
                return false;
            }
            // This was probably inlined
            if (!(bool)CallMethod(typeof(Packager), "CanWork", __instance, []))
            {
                return false;
            }
            PackagerConfiguration configuration = (PackagerConfiguration)GetProperty(typeof(Packager), "configuration", __instance);
            if (configuration.AssignedStationCount +  configuration.Routes.Routes.Count == 0)
            {
                __instance.SubmitNoWorkReason("I haven't been assigned to any stations or routes.", "You can use your management clipboards to assign stations or routes to me.", 0);
                __instance.SetIdle(true);
                return false;
            }
            if (!InstanceFinder.IsServer)
            {
                return false;
            }
            PackagingStation stationToAttend = (PackagingStation)CallMethod(typeof(Packager), "GetStationToAttend", __instance, []);
            if (stationToAttend != null)
            {
                CallMethod(typeof(Packager), "StartPackaging", __instance, [stationToAttend]);
                return false;
            }
            BrickPress brickPress = (BrickPress)CallMethod(typeof(Packager), "GetBrickPress", __instance, []);
            if (brickPress != null)
            {
                CallMethod(typeof(Packager), "StartPress", __instance, [brickPress]);
                return false;
            }
            PackagingStation stationMoveItems = (PackagingStation)CallMethod(typeof(Packager), "GetStationMoveItems", __instance, []);
            if (stationMoveItems != null)
            {
                CallMethod(typeof(Packager), "StartMoveItem", __instance, [stationMoveItems]);
                return false;
            }
            BrickPress brickPressMoveItems = (BrickPress)CallMethod(typeof(Packager), "GetBrickPressMoveItems", __instance, []);
            if (brickPressMoveItems != null)
            {
                CallMethod(typeof(Packager), "StartMoveItem", __instance, [brickPressMoveItems]);
                return false;
            }
            ItemInstance itemInstance = null;
            object[] args = new object[1] { itemInstance };
            AdvancedTransitRoute transitRouteReady = (AdvancedTransitRoute)CallMethod(typeof(Packager), "GetTransitRouteReady", __instance, args);
            itemInstance = (ItemInstance)args[0];
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


        public static new void RestoreDefaults()
        {
            // empty
        }
    }
}
