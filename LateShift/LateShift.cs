using HarmonyLib;
using MelonLoader;
using UnityEngine.Events;
using System.Reflection;

#if MONO_BUILD
using FishNet;
using ScheduleOne.DevUtilities;
using ScheduleOne.Dialogue;
using ScheduleOne.Employees;
using ScheduleOne.GameTime;
using ScheduleOne.Growing;
using ScheduleOne.ItemFramework;
using ScheduleOne.Management;
using ScheduleOne.Money;
using ScheduleOne.NPCs.Behaviour;
using ScheduleOne.ObjectScripts;
using ScheduleOne.StationFramework;
using WorkIssuesList = System.Collections.Generic.List<ScheduleOne.Employees.Employee.NoWorkReason>;
using MushroomBedList = System.Collections.Generic.List<ScheduleOne.ObjectScripts.MushroomBed>;
using PotList = System.Collections.Generic.List<ScheduleOne.ObjectScripts.Pot>;
using Il2CppObject = System.Object;
using Console = ScheduleOne.Console;
#else
using Il2CppFishNet;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.Employees;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.StationFramework;
using MushroomBedList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.ObjectScripts.MushroomBed>;
using PotList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.ObjectScripts.Pot>;
using WorkIssuesList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Employees.Employee.NoWorkReason>;
using Il2CppObject = Il2CppSystem.Object;
using Console = Il2CppScheduleOne.Console;
#endif


namespace LateShift
{
    public static class Utils
    {
        private static LateShiftMod Mod;

        private static Assembly S1Assembly;

        public static void Initialize(LateShiftMod mod)
        {
            Mod = mod;
#if !MONO_BUILD
            S1Assembly = AppDomain.CurrentDomain.GetAssemblies().First((Assembly a) => a.GetName().Name == "Assembly-CSharp");
#endif
        }

        // Reflection convenience methods.
        // Needed to access private members in mono.
        // Also handles the property-fying of fields in IL2CPP.
        // Treturn cannot be an interface type in IL2CPP; use ToInterface for that.

        public static Treturn GetField<Ttarget, Treturn>(string fieldName, object target) where Treturn : class
        {
            return (Treturn)GetField<Ttarget>(fieldName, target);
        }

        public static object GetField<Ttarget>(string fieldName, object target)
        {
#if MONO_BUILD
            return AccessTools.Field(typeof(Ttarget), fieldName).GetValue(target);
#else
            return AccessTools.Property(typeof(Ttarget), fieldName).GetValue(target);
#endif
        }

        public static Treturn GetProperty<Ttarget, Treturn>(string fieldName, object target)
        {
            return (Treturn)GetProperty<Ttarget>(fieldName, target);
        }

        public static object GetProperty<Ttarget>(string fieldName, object target)
        {
            return AccessTools.Property(typeof(Ttarget), fieldName).GetValue(target);
        }

        public static void SetProperty<Ttarget>(string fieldName, object target, object value)
        {
            AccessTools.Property(typeof(Ttarget), fieldName).SetValue(target, value);
        }

        public static Treturn CallMethod<Ttarget, Treturn>(string methodName, object target)
        {
            return (Treturn)CallMethod<Ttarget>(methodName, target, []);
        }

        public static Treturn CallMethod<Ttarget, Treturn>(string methodName, object target, object[] args)
        {
            return (Treturn)CallMethod<Ttarget>(methodName, target, args);
        }

        public static Treturn CallMethod<Ttarget, Treturn>(string methodName, Type[] argTypes, object target, object[] args)
        {
            return (Treturn)CallMethod<Ttarget>(methodName, argTypes, target, args);
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


        // Type checking and conversion methods

        // In IL2CPP, do a type check before performing a forced cast, returning default (usually null) on failure.
        // In Mono, do a type check before a regular cast, returning default on type check failure.
        // You can't use CastTo with T as an Il2Cpp interface; use ToInterface for that.
#if MONO_BUILD
        public static T CastTo<T>(object o)
        {
            if (o is T)
            {
                return (T)o;
            }
            else
            {
                return default(T);
            }
        }
#else
        public static T CastTo<T>(Il2CppObjectBase o) where T : Il2CppObjectBase
        {
            if (typeof(T).IsAssignableFrom(GetType(o)))
            {
                return (T)System.Activator.CreateInstance(typeof(T), [o.Pointer]);
            }
            return default(T);
        }
#endif

        // Under Il2Cpp, "is" operator only looks at local scope for type info,
        // instead of checking object identity. 
        // Check against actual object type obtained via GetType.
        // In Mono, use standard "is" operator.
        // Will always return false for Il2Cpp interfaces.
#if MONO_BUILD
        public static bool Is<T>(object o)
        {
            return o is T;
        }
#else
        public static bool Is<T>(Il2CppObjectBase o) where T : Il2CppObjectBase
        {
            return typeof(T).IsAssignableFrom(GetType(o));
        }
#endif

        // You can't cast to an interface type in IL2CPP, since interface info is stripped.
        // Use this method to perform a blind cast without type checking.
        // In Mono, just do a regular cast.
#if MONO_BUILD
        public static T ToInterface<T>(object o)
        {
            return (T)o;
        }
#else
        public static T ToInterface<T>(Il2CppObjectBase o) where T : Il2CppObjectBase
        {
            return (T)System.Activator.CreateInstance(typeof(T), [o.Pointer]);
        }
#endif

        // Get actual identity of Il2Cpp objects based on their ObjectClass, and
        // convert between Il2CppScheduleOne and ScheduleOne namespaces.
        // In Mono, return object.GetType or null.
#if MONO_BUILD
        public static Type GetType(object o)
        {
            if (o == null)
            {
                return null;
            }
            return o.GetType();
        }
#else
        public static Type GetType(Il2CppObjectBase o)
        {
            if (o == null)
            {
                return null;
            }

            string typeName = Il2CppType.TypeFromPointer(o.ObjectClass).FullName;
            return S1Assembly.GetType($"Il2Cpp{typeName}");
        }
#endif

        // Convert a System.Action to a unity action.
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

        // Convert a delegate to a predicate that IL2CPP ienumerable functions can actually use.
#if MONO_BUILD
        public static Predicate<T> ToPredicate<T>(Func<T, bool> func)
        {
            return new Predicate<T>(func);
        }
#else
        public static Il2CppSystem.Predicate<T> ToPredicate<T>(Func<T, bool> func)
        {
            return DelegateSupport.ConvertDelegate<Il2CppSystem.Predicate<T>>(func);
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
            if (__instance.TicksSinceLastWork >= 5 && workIssues.Count > 0)
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
        
        [HarmonyPatch(typeof(Employee), "UpdateBehaviour")]
        [HarmonyPrefix]
        public static bool UpdateBehaviourPrefix(Employee __instance)
        {
            bool workWithoutBeds = Utils.GetMelonPrefEntry<bool>("workWithoutBeds");
            bool employeesAlwaysWork = Utils.GetMelonPrefEntry<bool>("employeesAlwaysWork");
            bool isEndOfDay = NetworkSingleton<TimeManager>.Instance.IsEndOfDay;
            bool hasHome = __instance.GetHome() != null;

            // Only intercept the "no home but beds are optional" case;
            // otherwise let vanilla UpdateBehaviour run unchanged.
            if (!hasHome && workWithoutBeds)
            {
                if (__instance.Fired)
                {
                    return true;
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
                    Utils.CallMethod<Employee>("SetWaitOutside", __instance, new object[] { true });
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

        // CanWork has been inlined.
        [HarmonyPatch(typeof(Botanist), "UpdateBehaviour")]
        [HarmonyPostfix]
        public static void BotanistUpdateBehaviourPostfix(Botanist __instance)
        {
            bool workWithoutBeds = Utils.GetMelonPrefEntry<bool>("workWithoutBeds");
            bool employeesAlwaysWork = Utils.GetMelonPrefEntry<bool>("employeesAlwaysWork");
            bool isEndOfDay = NetworkSingleton<TimeManager>.Instance.IsEndOfDay;
            bool hasHome = __instance.GetHome() != null;

            // catch cases: canwork, and either of isendofday but employeesalwayswork; or gethome==null but workWithoutBeds
            if (__instance.CanWork() && (isEndOfDay && employeesAlwaysWork || !hasHome && workWithoutBeds))
            {
                // Bail if we're already working.
                if (__instance._workBehaviours.Exists(Utils.ToPredicate((Behaviour b) => b.Active)))
                {
                    return;
                }

                // Bail if we got fired or aren't the server.
                if (__instance.Fired || !__instance.IsServer)
                {
                    return;
                }

                // Bail if this employee has a reason not to work.
                if (isEndOfDay && !employeesAlwaysWork || !hasHome && !workWithoutBeds)
                {
                    return;
                }

                // Vanilla behaviour selection
                if (__instance.configuration.Assigns.SelectedObjects.Count == 0)
                {
                    __instance.SubmitNoWorkReason("I haven't been assigned anything", "You can use your management clipboards to assign me pots, growing racks, etc.", 0);
                    __instance.SetIdle(true);
                    return;
                }

                if (!InstanceFinder.IsServer)
                {
                    return;
                }

                Pot potForWatering = __instance.GetPotForWatering(0.2f);
                if (potForWatering != null)
                {
                    __instance._waterPotBehaviour.AssignAndEnable(potForWatering);
                    return;
                }

                MushroomBed mushroomBedForMisting = __instance.GetMushroomBedForMisting(0.2f);
                if (mushroomBedForMisting != null)
                {
                    __instance._mistMushroomBedBehaviour.AssignAndEnable(mushroomBedForMisting);
                    return;
                }

                foreach (GrowContainer growContainer in __instance.GetGrowContainersForAdditives())
                {
                    if (growContainer != null && __instance._applyAdditiveToGrowContainerBehaviour.DoesBotanistHaveAccessToRequiredSupplies(growContainer))
                    {
                        __instance._applyAdditiveToGrowContainerBehaviour.AssignAndEnable(growContainer);
                        return;
                    }
                }
                
                foreach (GrowContainer growContainer2 in __instance.GetGrowContainersForSoilPour())
                {
                    if (__instance._addSoilToGrowContainerBehaviour.DoesBotanistHaveAccessToRequiredSupplies(growContainer2))
                    {
                        __instance._addSoilToGrowContainerBehaviour.AssignAndEnable(growContainer2);
                        return;
                    }
                    string text = "Make sure there's soil in my supplies stash.";
                    if (__instance.configuration.Supplies.SelectedObject == null)
                    {
                        text = "Use your management clipboard to assign a supplies stash to me, then make sure there's soil in it.";
                    }
                    __instance.SubmitNoWorkReason("There are empty pots, but I don't have any soil to pour.", text, 0);
                }
                
                bool flag = false;
                foreach (Pot pot in __instance.GetPotsReadyForSeed())
                {
                    if (!__instance._sowSeedInPotBehaviour.DoesBotanistHaveAccessToRequiredSupplies(pot))
                    {
                        if (!flag)
                        {
                            flag = true;
                            string text2 = "Make sure I have the right seeds in my supplies stash.";
                            if (__instance.configuration.Supplies.SelectedObject == null)
                            {
                                text2 = "Use your management clipboards to assign a supplies stash to me, and make sure it contains the right seeds.";
                            }
                            __instance.SubmitNoWorkReason("There is a pot ready for sowing, but I don't have any seeds for it.", text2, 1);
                        }
                    }
                    else if (__instance.IsEntityAccessible(Utils.ToInterface<ITransitEntity>(pot)))
                    {
                        __instance._sowSeedInPotBehaviour.AssignAndEnable(pot);
                        return;
                    }
                }
                
                flag = false;
                foreach (MushroomBed mushroomBed in __instance.GetBedsReadyForSpawn())
                {
                    if (!__instance._applySpawnToMushroomBedBehaviour.DoesBotanistHaveAccessToRequiredSupplies(mushroomBed))
                    {
                        if (!flag)
                        {
                            flag = true;
                            string text3 = "Make sure I have shroom spawn my supplies stash.";
                            if (__instance.configuration.Supplies.SelectedObject == null)
                            {
                                text3 = "Use your management clipboards to assign a supplies stash to me, and make sure it contains shroom spawn.";
                            }
                            __instance.SubmitNoWorkReason("I don't have any shroom spawn to mix into my assigned mushroom beds.", text3, 1);
                        }
                    }
                    else if (__instance.IsEntityAccessible(Utils.ToInterface<ITransitEntity>(mushroomBed)))
                    {
                        __instance._applySpawnToMushroomBedBehaviour.AssignAndEnable(mushroomBed);
                        return;
                    }
                }
                
                PotList potsForHarvest = __instance.GetPotsForHarvest();
                if (potsForHarvest != null && potsForHarvest.Count > 0)
                {
                    __instance._harvestPotBehaviour.AssignAndEnable(potsForHarvest[0]);
                    return;
                }
                
                MushroomBedList mushroomBedsForHarvest = __instance.GetMushroomBedsForHarvest();
                if (mushroomBedsForHarvest != null && mushroomBedsForHarvest.Count > 0)
                {
                    __instance._harvestMushroomBedBehaviour.AssignAndEnable(mushroomBedsForHarvest[0]);
                    return;
                }
                
                foreach (DryingRack dryingRack in __instance.GetRacksToStop())
                {
                    if (__instance.IsEntityAccessible(Utils.ToInterface<ITransitEntity>(dryingRack)))
                    {
                        __instance.StopDryingRack(dryingRack);
                        return;
                    }
                }
                
                foreach (DryingRack dryingRack2 in __instance.GetRacksReadyToMove())
                {
                    if (__instance.IsEntityAccessible(Utils.ToInterface<ITransitEntity>(dryingRack2)))                    {
                        __instance.MoveItemBehaviour.Initialize(Utils.CastTo<DryingRackConfiguration>(dryingRack2.Configuration).DestinationRoute, dryingRack2.OutputSlot.ItemInstance, -1, false);
                        __instance.MoveItemBehaviour.Enable_Networked();
                        return;
                    }
                }
                
                foreach (MushroomSpawnStation mushroomSpawnStation in __instance.GetSpawnStationsReadyToUse())
                {
                    if (__instance.IsEntityAccessible(Utils.ToInterface<ITransitEntity>(mushroomSpawnStation)))
                    {
                        __instance._useSpawnStationBehaviour.AssignStation(mushroomSpawnStation);
                        __instance._useSpawnStationBehaviour.Enable_Networked();
                        return;
                    }
                }
                
                foreach (MushroomSpawnStation mushroomSpawnStation2 in __instance.GetSpawnStationsReadyToMove())
                {
                    if (__instance.IsEntityAccessible(Utils.ToInterface<ITransitEntity>(mushroomSpawnStation2)))
                    {
                        __instance.MoveItemBehaviour.Initialize((mushroomSpawnStation2.Configuration as SpawnStationConfiguration).DestinationRoute, mushroomSpawnStation2.OutputSlot.ItemInstance, -1, false);
                        __instance.MoveItemBehaviour.Enable_Networked();
                        return;
                    }
                }
                
                Pot potForWatering2 = __instance.GetPotForWatering(0.3f);
                if (potForWatering2 != null)
                {
                    __instance._waterPotBehaviour.AssignAndEnable(potForWatering2);
                    return;
                }
                
                MushroomBed mushroomBedForMisting2 = __instance.GetMushroomBedForMisting(0.3f);
                if (mushroomBedForMisting2 != null)
                {
                    __instance._mistMushroomBedBehaviour.AssignAndEnable(mushroomBedForMisting2);
                    return;
                }
                
                QualityItemInstance qualityItemInstance;
                DryingRack dryingRack3;
                int num;
                if (__instance.CanMoveDryableToRack(out qualityItemInstance, out dryingRack3, out num))
                {
                    TransitRoute transitRoute = new TransitRoute(Utils.ToInterface<ITransitEntity>(__instance.configuration.Supplies.SelectedObject), Utils.ToInterface<ITransitEntity>(dryingRack3));
                    if (__instance.MoveItemBehaviour.IsTransitRouteValid(transitRoute, qualityItemInstance.ID))
                    {
                        __instance.MoveItemBehaviour.Initialize(transitRoute, qualityItemInstance, num, false);
                        __instance.MoveItemBehaviour.Enable_Networked();
                        Console.Log(string.Concat(new string[]
                        {
                            "Moving ",
                            num.ToString(),
                            " ",
                            qualityItemInstance.ID,
                            " to drying rack"
                        }), null);
                        return;
                    }
                }
                
                foreach (DryingRack dryingRack4 in __instance.GetRacksToStart())
                {
                    if (__instance.IsEntityAccessible(Utils.ToInterface<ITransitEntity>(dryingRack4)))
                    {
                        __instance.StartDryingRack(dryingRack4);
                        return;
                    }
                }
                __instance.SubmitNoWorkReason("There's nothing for me to do right now.", string.Empty, 0);
                __instance.SetIdle(true);
            }
            return;
        }

        // CanWork is probably inlined in this function.
        [HarmonyPatch(typeof(Packager), "UpdateBehaviour")]
        [HarmonyPostfix]
        public static void PackagerUpdateBehaviourPostfix(Packager __instance)
        {
            bool workWithoutBeds = Utils.GetMelonPrefEntry<bool>("workWithoutBeds");
            bool employeesAlwaysWork = Utils.GetMelonPrefEntry<bool>("employeesAlwaysWork");
            bool isEndOfDay = NetworkSingleton<TimeManager>.Instance.IsEndOfDay;
            bool hasHome = __instance.GetHome() != null;

            // catch cases: canwork, and either of isendofday but employeesalwayswork; or gethome==null but noBeds            
            if (__instance.CanWork() && (isEndOfDay && employeesAlwaysWork || !hasHome && workWithoutBeds))
            {
                // Bail if we're currently working, or got fired.
                if (__instance.PackagingBehaviour.Active ||  __instance.MoveItemBehaviour.Active || __instance.Fired)
                {
                    return;
                }

                // Bail if this employee has a reason not to work.
                if (isEndOfDay && !employeesAlwaysWork || !hasHome && !workWithoutBeds)
                {
                    return;
                }

                // Vanilla behaviour selection
                PackagerConfiguration configuration = Utils.GetProperty<Packager, PackagerConfiguration>("configuration", __instance);
                if (configuration.AssignedStationCount +  configuration.Routes.Routes.Count == 0)
                {
                    __instance.SubmitNoWorkReason("I haven't been assigned to any stations or routes.", "You can use your management clipboards to assign stations or routes to me.", 0);
                    __instance.SetIdle(true);
                    return;
                }
                
                if (!InstanceFinder.IsServer)
                {
                    return;
                }
                
                PackagingStation stationToAttend = Utils.CallMethod<Packager, PackagingStation>("GetStationToAttend", __instance);
                if (stationToAttend != null)
                {
                    Utils.CallMethod<Packager>("StartPackaging", __instance, [stationToAttend]);
                    return;
                }
                
                BrickPress brickPress = Utils.CallMethod<Packager, BrickPress>("GetBrickPress", __instance);
                if (brickPress != null)
                {
                    Utils.CallMethod<Packager>("StartPress", __instance, [brickPress]);
                    return;
                }
                
                PackagingStation stationMoveItems = Utils.CallMethod<Packager, PackagingStation>("GetStationMoveItems", __instance);
                if (stationMoveItems != null)
                {
                    Utils.CallMethod<Packager>("StartMoveItem", [typeof(PackagingStation)], __instance, [stationMoveItems]);
                    return;
                }
                
                BrickPress brickPressMoveItems = Utils.CallMethod<Packager, BrickPress>("GetBrickPressMoveItems", __instance);
                if (brickPressMoveItems != null)
                {
                    Utils.CallMethod<Packager>("StartMoveItem", [typeof(BrickPress)], __instance, [brickPressMoveItems]);
                    return;
                }

                // GetTransitRouteReady uses an out parameter. Changes to out parameters are captured in
                // the args array, so keep a handle to it and copy the value back out after.
                Il2CppObject[] args = new Il2CppObject[1] { null };
                AdvancedTransitRoute transitRouteReady = Utils.CallMethod<Packager,AdvancedTransitRoute>("GetTransitRouteReady", __instance, args);
                ItemInstance itemInstance = Utils.CastTo<ItemInstance>(args[0]);
                if (transitRouteReady != null)
                {
                    __instance.MoveItemBehaviour.Initialize(transitRouteReady, itemInstance, itemInstance.Quantity, false);
                    __instance.MoveItemBehaviour.Enable_Networked();
                    return;
                }
            }
            __instance.SubmitNoWorkReason("There's nothing for me to do right now.", "I need one of my assigned stations to have enough product and packaging to get to work.", 0);
            __instance.SetIdle(true);
            return;
        }
    
        [HarmonyPatch(typeof(Chemist), "UpdateBehaviour")]
        [HarmonyPostfix]
        public static void ChemistUpdateBehaviourPostfix(Chemist __instance)
        {
            bool workWithoutBeds = Utils.GetMelonPrefEntry<bool>("workWithoutBeds");
            bool employeesAlwaysWork = Utils.GetMelonPrefEntry<bool>("employeesAlwaysWork");
            bool isEndOfDay = NetworkSingleton<TimeManager>.Instance.IsEndOfDay;
            bool hasHome = __instance.GetHome() != null;

            // catch cases: canwork, and either of isendofday but employeesalwayswork; or gethome==null but noBeds            
            if (__instance.CanWork() && (isEndOfDay && employeesAlwaysWork || !hasHome && workWithoutBeds))
            {
                // Bail if we're currently working, or got fired, or we aren't the server.
                if (__instance.AnyWorkInProgress() || __instance.Fired || !InstanceFinder.IsServer)
                {
                    return;
                }

                // Bail if this employee has a reason not to work.
                if (isEndOfDay && !employeesAlwaysWork || !hasHome && !workWithoutBeds)
                {
                    return;
                }

                // Vanilla behaviour selection
                if (__instance.configuration.TotalStations == 0)
                {
                    __instance.SubmitNoWorkReason("I haven't been assigned any stations", "You can use your management clipboards to assign stations to me.", 0);
                    __instance.SetIdle(true);
                    return;
                }
                if (InstanceFinder.IsServer)
                {
                    __instance.TryStartNewTask();
                }
            }
        }

        [HarmonyPatch(typeof(Cleaner), "UpdateBehaviour")]
        [HarmonyPostfix]
        public static void CleanerUpdateBehaviourPostfix(Cleaner __instance)
        {
            bool workWithoutBeds = Utils.GetMelonPrefEntry<bool>("workWithoutBeds");
            bool employeesAlwaysWork = Utils.GetMelonPrefEntry<bool>("employeesAlwaysWork");
            bool isEndOfDay = NetworkSingleton<TimeManager>.Instance.IsEndOfDay;
            bool hasHome = __instance.GetHome() != null;

            // catch cases: canwork, and either of isendofday but employeesalwayswork; or gethome==null but noBeds            
            if (__instance.CanWork() && (isEndOfDay && employeesAlwaysWork || !hasHome && workWithoutBeds))
            {
                // Bail if we're currently working, or got fired.
                if (__instance.AnyWorkInProgress() || __instance.Fired)
                {
                    return;
                }

                // Bail if this employee has a reason not to work.
                if (isEndOfDay && !employeesAlwaysWork || !hasHome && !workWithoutBeds)
                {
                    return;
                }

                // Vanilla behaviour selection
                if (__instance.configuration.binItems.Count == 0)
                {
                    __instance.SubmitNoWorkReason("I haven't been assigned any trash cans", "You can use your management clipboards to assign trash cans to me.", 0);
                    __instance.SetIdle(true);
                    return;
                }
                if (!InstanceFinder.IsServer)
                {
                    return;
                }
                __instance.TryStartNewTask();
            }
        }
    }
}
