using MelonLoader;



[assembly: MelonInfo(typeof(LateShift.LateShiftMod), "LateShift", "1.0.7", "lasersquid", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace LateShift
{
    public class LateShiftMod : MelonMod
    {
        public MelonPreferences_Category melonPrefs;
        public HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.lasersquid.lateshift");

        public override void OnInitializeMelon()
        { 
            CreateMelonPreferences();
            Utils.Initialize(this);
            LoggerInstance.Msg("Initialized.");
        }

        private void CreateMelonPreferences()
        {
            melonPrefs = MelonPreferences.CreateCategory("LateShift");
            melonPrefs.SetFilePath("UserData/LateShift.cfg", true, false);

            melonPrefs.CreateEntry<bool>("employeesAlwaysWork", true, "Employees always work", "Employees keep working at 4am");
            melonPrefs.CreateEntry<bool>("workWithoutBeds", true, "Employees work without beds", "Employees work without beds or lockers");
            melonPrefs.CreateEntry<bool>("payEmployeesFromBank", false, "Autopay employees from bank account", "Autopay employees from bank account if true; otherwise pay with cash");

            melonPrefs.SaveToFile(false);
        }
    }
}

// todo
// shrooms update - done
