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
using WorkIssuesList = System.Collections.Generic.List<ScheduleOne.Employees.Employee.NoWorkReason>;
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
using WorkIssuesList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Employees.Employee.NoWorkReason>;
#endif


namespace LateShift
{
    public static class Utils
    {
        private static LateShiftMod Mod;

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

        public static Treturn GetField<Ttarget, Treturn>(string fieldName, object target) where Treturn : class
        {
            return CastTo<Treturn>(GetField<Ttarget>(fieldName, target));
        }

        public static object GetField<Ttarget>(string fieldName, object target)
        {
#if MONO_BUILD
            return AccessTools.Field(typeof(Ttarget), fieldName).GetValue(target);
#else
            return AccessTools.Property(typeof(Ttarget), fieldName).GetValue(target);
#endif
        }

        public static void SetField<Ttarget>(string fieldName, object target, object value)
        {
#if MONO_BUILD
            AccessTools.Field(typeof(Ttarget), fieldName).SetValue(target, value);
#else
            AccessTools.Property(typeof(Ttarget), fieldName).SetValue(target, value);
#endif
        }

        public static Treturn GetProperty<Ttarget, Treturn>(string fieldName, object target) where Treturn : class
        {
            return CastTo<Treturn>(GetProperty<Ttarget>(fieldName, target));
        }

        public static object GetProperty<Ttarget>(string fieldName, object target)
        {
            return AccessTools.Property(typeof(Ttarget), fieldName).GetValue(target);
        }

        public static void SetProperty<Ttarget>(string fieldName, object target, object value)
        {
            AccessTools.Property(typeof(Ttarget), fieldName).SetValue(target, value);
        }

        public static Treturn CallMethod<Ttarget, Treturn>(string methodName, object target) where Treturn : class
        {
            return CastTo<Treturn>(CallMethod<Ttarget>(methodName, target, []));
        }

        public static Treturn CallMethod<Ttarget, Treturn>(string methodName, object target, object[] args) where Treturn : class
        {
            return CastTo<Treturn>(CallMethod<Ttarget>(methodName, target, args));
        }

        public static Treturn CallMethod<Ttarget, Treturn>(string methodName, Type[] argTypes, object target, object[] args) where Treturn : class
        {
            return CastTo<Treturn>(CallMethod<Ttarget>(methodName, argTypes, target, args));
        }

        public static object CallMethod<Ttarget>(string methodName, object target)
        {
            return AccessTools.Method(typeof(Ttarget), methodName).Invoke(target, []);
        }

        public static object CallMethod<Ttarget>(string methodName, object target, object[] args)
        {
            return AccessTools.Method(typeof(Ttarget), methodName).Invoke(target, args);
        }

        public static object CallMethod<Ttarget>(string methodName, Type[] argTypes, object target, object[] args)
        {
            return AccessTools.Method(typeof(Ttarget), methodName, argTypes).Invoke(target, args);
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
        public static T CastTo<T>(Il2CppObjectBase o) where T : Il2CppObjectBase
        { 
            return o.TryCast<T>();
        }

        public static bool Is<T>(Il2CppObjectBase o) where T : Il2CppObjectBase
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

        public static UnityAction<T> ToUnityAction<T>(System.Action<T> action)
        {
#if MONO_BUILD
            return new UnityAction<T>(action);
#else
            return DelegateSupport.ConvertDelegate<UnityAction<T>>(action);
#endif
        }

#if MONO_BUILD
        public static T ToInterface<T>(object o)
        {
            return (T)o;
        }
#else
        public static T ToInterface<T>(Il2CppSystem.Object o) where T : Il2CppObjectBase
        {
            return CastTo<T>(System.Activator.CreateInstance(typeof(T), [o.Pointer]));
        }
#endif

        public static void Log(string message)
        {
            Utils.Mod.LoggerInstance.Msg(message);
        }

        public static void Warn(string message)
        {
            Utils.Mod.LoggerInstance.Warning(message);
        }

        // Compare unity objects by their instance ID
        public class UnityObjectComparer : IEqualityComparer<UnityEngine.Object>
        {
            public bool Equals(UnityEngine.Object a, UnityEngine.Object b)
            {
                return a.GetInstanceID() == b.GetInstanceID();
            }

            public int GetHashCode(UnityEngine.Object item)
            {
                return item.GetInstanceID();
            }
        }

        public static MelonPreferences_Category GetMelonPrefs()
        {
            return Mod.melonPrefs;
        }

        public static T GetMelonPrefEntry<T>(string entryName)
        {
            return GetMelonPrefs().GetEntry<T>(entryName).Value;
        }

        public static void Debug(string message)
        {
            if (Utils.GetMelonPrefEntry<bool>("debugLogs"))
            {
                Mod.LoggerInstance.Msg($"DEBUG: {message}");
            }
        }

        public static void VerboseLog(string message)
        {
            if (Utils.GetMelonPrefEntry<bool>("verboseLogs"))
            {
                Mod.LoggerInstance.Msg(message);
            }
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
            EmployeeHome home = __instance.GetHome();

            if (Utils.GetMelonPrefEntry<bool>("payEmployeesFromBank"))
            {
                __result = moneyManager.onlineBalance >= __instance.DailyWage;
                return false;
            }

            if (Utils.GetMelonPrefEntry<bool>("workWithoutBeds"))
            {
                if (home == null)
                {
                    __result = moneyManager.cashBalance >= __instance.DailyWage;
                }
                else
                {
                    __result = home.GetCashSum() >= __instance.DailyWage;
                }
            }
            else
            {
                if (home == null)
                {
                    __result = false;
                }
                else
                {
                    __result = home.GetCashSum() >= __instance.DailyWage;
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(Employee), "RemoveDailyWage")]
        [HarmonyPrefix]
        public static bool RemoveDailyWagePrefix(Employee __instance)
        {
            if (!InstanceFinder.IsServer)
            {
                return false;
            }

            MoneyManager moneyManager = NetworkSingleton<MoneyManager>.Instance;
            EmployeeHome home = __instance.GetHome();

            if (Utils.GetMelonPrefEntry<bool>("payEmployeesFromBank"))
            {
                if (moneyManager.onlineBalance >= __instance.DailyWage)
                {
                    // Record employee pay as a debit from the online balance.
                    moneyManager.CreateOnlineTransaction("Employee Pay", -__instance.DailyWage, 1f, $"{__instance.fullName}, employeetype, location");
                }
            }
            else
            {
                if (Utils.GetMelonPrefEntry<bool>("workWithoutBeds"))
                {
                    if (home == null)
                    {
                        if (moneyManager.cashBalance >= __instance.DailyWage)
                        {
                            moneyManager.ChangeCashBalance(-__instance.DailyWage);
                        }
                    }
                    else
                    {
                        if (home.GetCashSum() >= __instance.DailyWage)
                        {
                            moneyManager.ChangeCashBalance(-__instance.DailyWage);
                            home.RemoveCash(__instance.DailyWage);
                        }
                    }
                }
                else
                {
                    if (home == null)
                    {
                        return false;
                    }
                    if (home.GetCashSum() >= __instance.DailyWage)
                    {
                        moneyManager.ChangeCashBalance(-__instance.DailyWage);
                        home.RemoveCash(__instance.DailyWage);
                    }
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(Employee), "GetWorkIssue")]
        [HarmonyPrefix]
        public static bool GetWorkIssuePrefix(Employee __instance, ref bool __result, ref DialogueContainer notWorkingReason)
        {
            if (__instance.GetHome() == null && !Utils.GetMelonPrefEntry<bool>("workWithoutBeds"))
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
            WorkIssuesList workIssues = Utils.GetField<Employee, WorkIssuesList>("WorkIssues", __instance);
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
        [HarmonyPostfix]
        public static void CanWorkPostfix(Employee __instance, ref bool __result)
        {
            bool hasHome = __instance.GetHome() != null;
            bool workWithoutBeds = Utils.GetMelonPrefEntry<bool>("workWithoutBeds");
            bool employeesAlwaysWork = Utils.GetMelonPrefEntry<bool>("employeesAlwaysWork");
            bool isEndOfDay = NetworkSingleton<TimeManager>.Instance.IsEndOfDay;
            bool paidForToday = __instance.PaidForToday;

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

        // We could accomplish what we want to in Employee.UpdateBehaviour with just a postfix, but...
        // Since CanWork is inlined in Packager.UpdateBehaviour, we need to replace the entire method.
        // And since Packager.UpdateBehaviour calls base.UpdateBehaviour, we need a copy of Employee.UpdateBehaviour.
        // Why a copy and not use reflection? Methods that have been overridden in subclasses are not accessible through reflection.
        // If we weren't supporting IL2CPP we could use a reverse patch to keep things tidier, but alas.
        [HarmonyPatch(typeof(Employee), "UpdateBehaviour")]
        [HarmonyPrefix]
        public static bool UpdateBehaviourPrefix(Employee __instance)
        {
            bool workWithoutBeds = Utils.GetMelonPrefEntry<bool>("workWithoutBeds");
            bool employeesAlwaysWork = Utils.GetMelonPrefEntry<bool>("employeesAlwaysWork");

            if (__instance.Fired)
            {
                return false;
            }
            if (__instance.Behaviour.activeBehaviour == null || __instance.Behaviour.activeBehaviour == __instance.WaitOutside)
            {
                bool shouldNotWork = false;
                bool shouldBePaid = false;
                if (__instance.GetHome() == null && !workWithoutBeds)
                {
                    shouldNotWork = true;
                    __instance.SubmitNoWorkReason("I haven't been assigned a locker", "You can use your management clipboard to assign me a locker.", 0);
                }
                else if (NetworkSingleton<TimeManager>.Instance.IsEndOfDay && !employeesAlwaysWork)
                {
                    shouldNotWork = true;
                    __instance.SubmitNoWorkReason("Sorry boss, my shift ends at 4AM.", string.Empty, 0);
                }
                else if (!__instance.PaidForToday)
                {
                    if (__instance.IsPayAvailable())
                    {
                        shouldBePaid = true;
                    }
                    else
                    {
                        shouldNotWork = true;
                        __instance.SubmitNoWorkReason("I haven't been paid yet", "You can place cash in my locker.", 0);
                    }
                }
                else if (!shouldNotWork && __instance.Behaviour.activeBehaviour == __instance.WaitOutside && !__instance.WaitOutside.Active)
                {
                    Utils.Warn($"Active behaviour is waitoutside, but it's disabled. enabling.");
                    Utils.CallMethod<Employee>("SetWaitOutside", __instance, [true]);
                }
                if (shouldNotWork)
                {
                    Utils.CallMethod<Employee>("SetWaitOutside", __instance, [true]);
                    return false;
                }
                if (InstanceFinder.IsServer && shouldBePaid && __instance.IsPayAvailable())
                {
                    __instance.RemoveDailyWage();
                    __instance.SetIsPaid();
                }
            }

            return false;
        }

        // CanWork is probably inlined here. Replace with original method body.
        [HarmonyPatch(typeof(Packager), "UpdateBehaviour")]
        [HarmonyPrefix]
        public static bool PackagerUpdateBehaviourPrefix(Packager __instance)
        {
            UpdateBehaviourPrefix(__instance);
            if (__instance.PackagingBehaviour.Active)
            {
                Utils.CallMethod<Packager>("MarkIsWorking", __instance);
                return false;
            }
            if (__instance.MoveItemBehaviour.Active)
            {
                Utils.CallMethod<Packager>("MarkIsWorking", __instance);
                return false;
            }
            if (__instance.Fired)
            {
                Utils.CallMethod<Packager>("LeavePropertyAndDespawn", __instance);
                return false;
            }
            // This was probably inlined
            if (!(bool)Utils.CallMethod<Packager>("CanWork", __instance))
            {
                return false;
            }
            PackagerConfiguration configuration = Utils.GetProperty<Packager, PackagerConfiguration>("configuration", __instance);
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
            PackagingStation stationToAttend = Utils.CallMethod<Packager, PackagingStation>("GetStationToAttend", __instance);
            if (stationToAttend != null)
            {
                Utils.CallMethod<Packager>("StartPackaging", __instance, [stationToAttend]);
                return false;
            }
            BrickPress brickPress = Utils.CallMethod<Packager, BrickPress>("GetBrickPress", __instance);
            if (brickPress != null)
            {
                Utils.CallMethod<Packager>("StartPress", __instance, [brickPress]);
                return false;
            }
            PackagingStation stationMoveItems = Utils.CallMethod<Packager, PackagingStation>("GetStationMoveItems", __instance);
            if (stationMoveItems != null)
            {
                Utils.CallMethod<Packager>("StartMoveItem", [typeof(PackagingStation)], __instance, [stationMoveItems]);
                return false;
            }
            BrickPress brickPressMoveItems = Utils.CallMethod<Packager, BrickPress>("GetBrickPressMoveItems", __instance);
            if (brickPressMoveItems != null)
            {
                Utils.CallMethod<Packager>("StartMoveItem", [typeof(BrickPress)], __instance, [brickPressMoveItems]);
                return false;
            }

            // GetTransitRouteReady uses an out parameter. Changes to out parameters are captured in
            // the args array, so keep a handle to it and copy the value back out after.
            object[] args = new object[1] { null };
            AdvancedTransitRoute transitRouteReady = Utils.CallMethod<Packager,AdvancedTransitRoute>("GetTransitRouteReady", __instance, args);
            ItemInstance itemInstance = Utils.CastTo<ItemInstance>(args[0]);
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
