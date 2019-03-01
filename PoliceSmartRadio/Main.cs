using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LSPD_First_Response.Mod.API;
using Rage;
using System.Reflection;

namespace PoliceSmartRadio
{
    internal class Main : Plugin
    {
        public Main()
        {
            Albo1125.Common.UpdateChecker.VerifyXmlNodeExists(PluginName, FileID, DownloadURL, Path);
            Albo1125.Common.DependencyChecker.RegisterPluginForDependencyChecks(PluginName);
        }

        public override void Finally()
        {

        }

        public override void Initialize()
        {
            Game.Console.Print("PoliceSmartRadio " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + ", developed by Albo1125, loaded successfully!");
            Game.Console.Print("Special thanks to FinKone for the inspiration and OfficerSquare for the default UI.");
            Game.Console.Print("Please go on duty to start Police SmartRadio.");

            Functions.OnOnDutyStateChanged += Functions_OnOnDutyStateChanged;
        }

        internal static Version Albo1125CommonVer = new Version("6.6.3.0");
        internal static Version MadeForGTAVersion = new Version("1.0.1604.1");
        internal static float MinimumRPHVersion = 0.51f;
        internal static string[] AudioFilesToCheckFor = new string[] { "Plugins/LSPDFR/PoliceSmartRadio/Audio/ButtonScroll.wav", "Plugins/LSPDFR/PoliceSmartRadio/Audio/ButtonSelect.wav",
            "Plugins/LSPDFR/PoliceSmartRadio/Audio/PlateCheck/TargetPlate1.wav", "Plugins/LSPDFR/PoliceSmartRadio/Audio/PanicButton.wav" };
        internal static Version MadeForLSPDFRVersion = new Version("0.4.39.22580");
        internal static string[] OtherFilesToCheckFor = new string[] { "Plugins/LSPDFR/Traffic Policer.dll", "Plugins/LSPDFR/PoliceSmartRadio/Config/GeneralConfig.ini",
            "Plugins/LSPDFR/PoliceSmartRadio/Config/ControllerConfig.ini", "Plugins/LSPDFR/PoliceSmartRadio/Config/KeyboardConfig.ini", "Plugins/LSPDFR/PoliceSmartRadio/Config/DisplayConfig.ini",
            "Plugins/LSPDFR/PoliceSmartRadio/Config/PanicButton.ini"};

        internal static Version TrafficPolicerVersion = new Version("6.14.0.0");
        internal static Version ArrestManagerVersion = new Version("7.9.1.0");
        internal static string[] conflictingFiles = new string[] { "Plugins/LSPDFR/PoliceRadio.dll" };

        internal static string FileID = "15354";
        internal static string DownloadURL = "http://www.lcpdfr.com/files/file/15354-police-smartradio-the-successor-to-police-radio/";
        internal static string PluginName = "Police SmartRadio";
        internal static string Path = "Plugins/LSPDFR/PoliceSmartRadio.dll";

        public static void Functions_OnOnDutyStateChanged(bool onDuty)
        {
            if (onDuty)
            {
                Albo1125.Common.UpdateChecker.InitialiseUpdateCheckingProcess();
                if (Albo1125.Common.DependencyChecker.DependencyCheckMain(PluginName, Albo1125CommonVer, MinimumRPHVersion, MadeForGTAVersion, MadeForLSPDFRVersion, AudioFilesToCheckFor : AudioFilesToCheckFor, OtherRequiredFilesToCheckFor : OtherFilesToCheckFor))
                {
                    if (!Albo1125.Common.DependencyChecker.CheckIfThereAreNoConflictingFiles(PluginName, conflictingFiles))
                    {
                        Game.LogTrivial("Old Police Radio still installed.");
                        Game.DisplayNotification("~r~~h~Police SmartRadio detected the old PoliceRadio modification. You must delete it before using Police SmartRadio.");
                        Albo1125.Common.CommonLibrary.ExtensionMethods.DisplayPopupTextBoxWithConfirmation("Police SmartRadio Dependencies", "Police SmartRadio detected the old PoliceRadio modification. You must delete it before using Police SmartRadio. Unloading...", true);
                        return;
                    }
                    if (!Albo1125.Common.DependencyChecker.CheckIfFileExists("Plugins/LSPDFR/Traffic Policer.dll", TrafficPolicerVersion))
                    {
                        Game.LogTrivial("Traffic Policer is out of date for LSPDR+. Aborting. Required version: " + TrafficPolicerVersion.ToString());
                        Game.DisplayNotification("~r~~h~Police SmartRadio detected Traffic Policer version lower than ~b~" + TrafficPolicerVersion.ToString());
                        Albo1125.Common.CommonLibrary.ExtensionMethods.DisplayPopupTextBoxWithConfirmation("Police SmartRadio Dependencies", "Police SmartRadio did not detect Traffic Policer or detected Traffic Policer version lower than " + TrafficPolicerVersion.ToString() + ". Please install the appropriate version of Traffic Policer (link under Requirements on the download page). Unloading Police SmartRadio...", true);
                        return;
                    }
                    if (!Albo1125.Common.DependencyChecker.CheckIfFileExists("Plugins/LSPDFR/Arrest Manager.dll", ArrestManagerVersion))
                    {
                        //Game.LogTrivial("Arrest Manager is out of date for Police SmartRadio. Aborting. Required version: " + ArrestManagerVersion.ToString());
                        Game.DisplayNotification("~r~Police SmartRadio is optimized for use with the latest Arrest Manager - you are advised to install it.");
                        //Albo1125.Common.CommonLibrary.ExtensionMethods.DisplayPopupTextBoxWithConfirmation("Police SmartRadio Dependencies", "Police SmartRadio did not detect Arrest Manager or detected Arrest Manager version lower than " + ArrestManagerVersion.ToString() + ". Please install the appropriate version of Arrest Manager (link under Requirements on the download page). Unloading Police SmartRadio...", true);
                    }
                    GameFiber.StartNew(delegate
                    {
                        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveAssemblyEventHandler);
                        while (!IsLSPDFRPluginRunning("Traffic Policer"))
                        {
                            GameFiber.Yield();
                        }
                        PoliceSmartRadio.Initialise();

                    });
                }
            }
        }

        public static bool IsLSPDFRPluginRunning(string Plugin, Version minversion = null)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName an = assembly.GetName();
                if (an.Name.ToLower() == Plugin.ToLower())
                {
                    if (minversion == null || an.Version.CompareTo(minversion) >= 0) { return true; }
                }
            }
            return false;
        }

        public static Assembly ResolveAssemblyEventHandler(object sender, ResolveEventArgs args)
        {
            foreach (Assembly assembly in LSPD_First_Response.Mod.API.Functions.GetAllUserPlugins())
            {
                if (args.Name.ToLower().Contains(assembly.GetName().Name.ToLower()))
                {
                    return assembly;
                }
            }
            return null;
        }
    }
}
