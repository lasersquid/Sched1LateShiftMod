using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using System.Reflection;



[assembly: MelonInfo(typeof(LateShift.LateShiftMod), "LateShift", "1.0.3", "lasersquid", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace LateShift
{
    public class LateShiftMod : MelonMod
    {
        private bool needsReset = false;
        public MelonPreferences_Category melonPrefs;
        public HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.lasersquid.lateshift");

        public override void OnInitializeMelon()
        {
            CreateMelonPreferences();
            SetMod();
            LoggerInstance.Msg("Initialized.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName.ToLower().Contains("main") || sceneName.ToLower().Contains("tutorial"))
            {
                needsReset = true;
            }

            if (sceneName.ToLower().Contains("menu"))
            {
                if (needsReset)
                {
                    LoggerInstance.Msg("Menu loaded, resetting state.");
                    ResetState();
                }
            }
        }

        private void ResetState()
        {
            RestoreDefaults();
            needsReset = false;
        }

        private void CreateMelonPreferences()
        {
            melonPrefs = MelonPreferences.CreateCategory("LateShift");
            melonPrefs.SetFilePath("UserData/LateShift.cfg");

            melonPrefs.CreateEntry<bool>("employeesAlwaysWork", true, "Employees always work", "Employees keep working at 4am");
            melonPrefs.CreateEntry<bool>("workWithoutBeds", true, "Employees work without beds", "Employees work without beds or lockers");
            melonPrefs.CreateEntry<bool>("payEmployeesFromBank", false, "Autopay employees from bank account", "Autopay employees from bank account if true; otherwise pay with cash");

            melonPrefs.SaveToFile();
        }

        private List<Type> GetPatchTypes()
        {
            return System.Reflection.Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.Name.EndsWith("Patches"))
                .ToList<Type>();
        }

        private void SetMod()
        {
            foreach (var t in GetPatchTypes())
            {
                MethodInfo method = t.GetMethod("SetMod", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                method.Invoke(null, [this]);
            }
        }

        public void RestoreDefaults()
        {
            foreach (var t in GetPatchTypes())
            {
                try
                {
                    MethodInfo method = t.GetMethod("RestoreDefaults", BindingFlags.Public | BindingFlags.Static);
                    method.Invoke(null, null);
                }
                catch (Exception e)
                {
                    LoggerInstance.Warning($"Couldn't restore defaults for class {t.Name}: {e.GetType().Name} - {e.Message}");
                    LoggerInstance.Warning($"Source: {e.Source}");
                    LoggerInstance.Warning($"{e.StackTrace}");
                }
            }
        }
    }
}
