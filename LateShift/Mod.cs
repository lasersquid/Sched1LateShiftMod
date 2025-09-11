using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using System.Reflection;



[assembly: MelonInfo(typeof(LateShift.LateShiftMod), "LateShift", "1.0.0", "lasersquid", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace LateShift
{
    public class LateShiftMod : MelonMod
    {
        private bool needsReset = false;

        public LateShiftSettings settings;
        public const string settingsFileName = "LateShiftSettings.json";
        public string settingsFilePath = Path.Combine(MelonEnvironment.UserDataDirectory, settingsFileName);

        public HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.lasersquid.lateshift");

        public override void OnInitializeMelon()
        {
            LoadSettings();
            SaveSettings();
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

        private void LoadSettings()
        {
            LoggerInstance.Msg($"Loading settings from {settingsFilePath}");
            settings = LateShiftSettings.LoadSettings(settingsFilePath);
            if (LateShiftSettings.UpdateSettings(settings) || !File.Exists(settingsFilePath))
            {
                SaveSettings();
            }
        }

        private void SaveSettings()
        {
            if (settings != null)
            {
                settings.SaveSettings(settingsFilePath);
            }
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

    public class LateShiftSettings
    {
        public bool employeesAlwaysWork;
        public bool employeesWorkWithoutBeds;
        public bool payEmployeesWithCredit;

        // version, for upgrading purposes
        public const string CurrentVersion = "1.0.0";
        public string version;
        private static bool VersionGreaterThan(string version, string other)
        {
            // if other is null, empty string, or malformed, return true
            if (other == null)
            {
                return true;
            }

            string[] versionStrings = version.Split(['.']);
            int versionMajor = Convert.ToInt32(versionStrings[0]);
            int versionMinor = Convert.ToInt32(versionStrings[1]);
            int versionPatch = Convert.ToInt32(versionStrings[2]);

            string[] otherStrings = other.Split(['.']);
            int otherMajor = Convert.ToInt32(otherStrings[0]);
            int otherMinor = Convert.ToInt32(otherStrings[1]);
            int otherPatch = Convert.ToInt32(otherStrings[2]);

            if (versionMajor > otherMajor)
            {
                return true;
            }
            else if (versionMajor == otherMajor && versionMinor > otherMinor)
            {
                return true;
            }
            else if (versionMajor == otherMajor && versionMinor == otherMinor && versionPatch > otherPatch)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        // return true if settings were modified
        public static bool UpdateSettings(LateShiftSettings settings)
        {
            bool changed = false;

            return changed;
        }


        public static LateShiftSettings LoadSettings(string jsonPath)
        {
            if (File.Exists(jsonPath))
            {
                string json = File.ReadAllText(jsonPath);
                LateShiftSettings fromFile = JsonConvert.DeserializeObject<LateShiftSettings>(json);

                return fromFile;
            }

            return new LateShiftSettings();
        }

        public LateShiftSettings()
        {
            employeesAlwaysWork = true;
            employeesWorkWithoutBeds = true;
            payEmployeesWithCredit = false;

            version = CurrentVersion;
        }

        public void SaveSettings(string jsonPath)
        {
            File.WriteAllText(jsonPath, this.ToString());
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public void PrintSettings()
        {
            MelonLogger.Msg("Settings:");
            MelonLogger.Msg($"{this}");
        }
    }
}