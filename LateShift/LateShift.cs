using HarmonyLib;
using MelonLoader;
using UnityEngine.Events;

#if MONO_BUILD
using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.Dialogue;
using ScheduleOne.Employees;
using ScheduleOne.Money;
using ScheduleOne.GameTime;
#else
using Il2CppFishNet;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Management;
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
        [HarmonyPostfix]
        public static void CanWorkPostfix(Employee __instance, ref bool __result)
        {
            bool hasHome = __instance.GetHome() != null;
            bool paidForToday = __instance.PaidForToday;
            bool isEndOfDay = NetworkSingleton<TimeManager>.Instance.IsEndOfDay;
            bool workWithoutBeds = Mod.melonPrefs.GetEntry<bool>("workWithoutBeds").Value;
            bool employeesAlwaysWork = Mod.melonPrefs.GetEntry<bool>("employeesAlwaysWork").Value;

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

        [HarmonyPatch(typeof(Employee), "UpdateBehaviour")]
        [HarmonyPrefix]
        public static bool UpdateBehaviourPrefix(Employee __instance)
        {
            bool hasHome = __instance.GetHome() != null;
            bool workWithoutBeds = Mod.melonPrefs.GetEntry<bool>("workWithoutBeds").Value;
            bool employeesAlwaysWork = Mod.melonPrefs.GetEntry<bool>("employeesAlwaysWork").Value;
            bool isEndOfDay = NetworkSingleton<TimeManager>.Instance.IsEndOfDay;

            // Only intercept the "no home but beds are optional" case;
            // otherwise let vanilla UpdateBehaviour run unchanged.
            if (!hasHome && workWithoutBeds)
            {
                if (__instance.Fired)
                {
                    return false;
                }

                bool flag = false;
                bool flag2 = false;

                // End-of-day handling, unless "always work" is enabled.
                if (isEndOfDay && !employeesAlwaysWork)
                {
                    flag = true;
                    __instance.SubmitNoWorkReason("Sorry boss, my shift ends at 4AM.", string.Empty, 0);
                }
                // Pay handling, even without a home (uses our IsPayAvailable/RemoveDailyWage patches).
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
                    CallMethod(typeof(Employee), "SetWaitOutside", __instance, new object[] { true });
                    return false;
                }

                if (InstanceFinder.IsServer && flag2 && __instance.IsPayAvailable())
                {
                    __instance.RemoveDailyWage();
                    __instance.SetIsPaid();
                }

                // Skip vanilla here; it would otherwise reintroduce the "must have home" restriction.
                return false;
            }

            // For all other cases, allow vanilla UpdateBehaviour to run.
            return true;
        }

#if !MONO_BUILD
        // IL2CPP-only helper: when handlers/packagers are homeless but allowed
        // to work (workWithoutBeds = true), the IL2CPP backend sometimes fails
        // to schedule new tasks even though CanWork() is true. This postfix
        // mirrors the vanilla station/press selection chain for homeless
        // handlers on the server and starts work if something is ready,
        // using strongly-typed calls instead of reflection.
        [HarmonyPatch(typeof(Packager), "UpdateBehaviour")]
        [HarmonyPostfix]
        public static void PackagerUpdateBehaviourPostfix(Packager __instance)
        {
            bool workWithoutBeds = Mod.melonPrefs.GetEntry<bool>("workWithoutBeds").Value;
            bool hasHome = __instance.GetHome() != null;

            if (!workWithoutBeds || hasHome)
            {
                return;
            }

            // Only the server should schedule work.
            if (!InstanceFinder.IsServer)
            {
                return;
            }

            // Respect current behaviours; don't interfere if already busy.
            if ((__instance.PackagingBehaviour != null && __instance.PackagingBehaviour.Active) ||
                (__instance.MoveItemBehaviour != null && __instance.MoveItemBehaviour.Active))
            {
                return;
            }

            // Only try to help if the game thinks this handler can work.
            if (!__instance.PaidForToday)
            {
                return;
            }

            // Access the packager configuration in a strongly-typed way.
            var configBase = __instance.Configuration;
            var config = configBase.TryCast<PackagerConfiguration>();
            if (config == null)
            {
                return;
            }

            // 1) Try to start packaging at an assigned packaging station.
            if (__instance.PackagingBehaviour != null)
            {
                foreach (var station in config.AssignedStations)
                {
                    if (station == null)
                    {
                        continue;
                    }

                    if (__instance.PackagingBehaviour.IsStationReady(station))
                    {
                        __instance.PackagingBehaviour.AssignStation(station);
                        __instance.PackagingBehaviour.Enable_Networked();
                        return;
                    }
                }
            }

            // 2) Try to start pressing bricks at an assigned press.
            if (__instance.BrickPressBehaviour != null)
            {
                foreach (var press in config.AssignedBrickPresses)
                {
                    if (press == null)
                    {
                        continue;
                    }

                    if (__instance.BrickPressBehaviour.IsStationReady(press))
                    {
                        __instance.BrickPressBehaviour.AssignStation(press);
                        __instance.BrickPressBehaviour.Enable_Networked();
                        return;
                    }
                }
            }

            // 3) Try to move items from packaging stations.
            if (__instance.MoveItemBehaviour != null)
            {
                foreach (var station in config.AssignedStations)
                {
                    if (station == null)
                    {
                        continue;
                    }

                    ItemSlot outputSlot = station.OutputSlot;
                    if (outputSlot == null || outputSlot.Quantity == 0)
                    {
                        continue;
                    }

                    var stationConfig = station.Configuration.TryCast<PackagingStationConfiguration>();
                    if (stationConfig == null)
                    {
                        continue;
                    }

                    var destRoute = stationConfig.DestinationRoute;
                    if (destRoute == null || outputSlot.ItemInstance == null)
                    {
                        continue;
                    }

                    if (!__instance.MoveItemBehaviour.IsTransitRouteValid(destRoute, outputSlot.ItemInstance.ID))
                    {
                        continue;
                    }

                    __instance.MoveItemBehaviour.Initialize(destRoute, outputSlot.ItemInstance, -1, false);
                    __instance.MoveItemBehaviour.Enable_Networked();
                    return;
                }

                // 4) Try to move items from brick presses.
                foreach (var press in config.AssignedBrickPresses)
                {
                    if (press == null)
                    {
                        continue;
                    }

                    ItemSlot outputSlot = press.OutputSlot;
                    if (outputSlot == null || outputSlot.Quantity == 0)
                    {
                        continue;
                    }

                    var pressConfig = press.Configuration.TryCast<BrickPressConfiguration>();
                    if (pressConfig == null)
                    {
                        continue;
                    }

                    var destRoute = pressConfig.DestinationRoute;
                    if (destRoute == null || outputSlot.ItemInstance == null)
                    {
                        continue;
                    }

                    if (!__instance.MoveItemBehaviour.IsTransitRouteValid(destRoute, outputSlot.ItemInstance.ID))
                    {
                        continue;
                    }

                    __instance.MoveItemBehaviour.Initialize(destRoute, outputSlot.ItemInstance, -1, false);
                    __instance.MoveItemBehaviour.Enable_Networked();
                    return;
                }

                // 5) Try to move items via advanced transit routes (shelves -> stations/presses).
                if (config.Routes != null && config.Routes.Routes != null)
                {
                    foreach (AdvancedTransitRoute route in config.Routes.Routes)
                    {
                        if (route == null)
                        {
                            continue;
                        }

                        ItemInstance item = route.GetItemReadyToMove();
                        if (item == null)
                        {
                            continue;
                        }

                        bool canSource = false;
                        bool canDestination = false;
                        int capacity = 0;

                        try
                        {
                            canSource = __instance.Movement != null && __instance.Movement.CanGetTo(route.Source, 1f);
                            canDestination = __instance.Movement != null && __instance.Movement.CanGetTo(route.Destination, 1f);
                            capacity = __instance.Inventory != null ? __instance.Inventory.GetCapacityForItem(item) : 0;
                        }
                        catch
                        {
                            // If movement or inventory checks throw, skip this route.
                            continue;
                        }

                        if (!canSource || !canDestination || capacity <= 0)
                        {
                            continue;
                        }

                        __instance.MoveItemBehaviour.Initialize(route, item, item.Quantity, false);
                        __instance.MoveItemBehaviour.Enable_Networked();
                        return;
                    }
                }
            }

            // If we reach this point, no work was started in this tick for a
            // homeless handler. Mirror vanilla's "nothing to do" behaviour by
            // explicitly idling so they return to their idle point instead of
            // freezing at the last task location.
            if ((__instance.PackagingBehaviour == null || !__instance.PackagingBehaviour.Active) &&
                (__instance.MoveItemBehaviour == null || !__instance.MoveItemBehaviour.Active) &&
                !__instance.Fired)
            {
                try
                {
                    CallMethod(typeof(Employee), "SetIdle", __instance, new object[] { true });
                }
                catch
                {
                    // Best-effort; failures here should not break work logic.
                }
            }
        }
#endif


        public static new void RestoreDefaults()
        {
            // empty
        }
    }
}
