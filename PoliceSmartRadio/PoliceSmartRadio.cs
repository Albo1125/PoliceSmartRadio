using LSPD_First_Response.Mod.API;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PoliceSmartRadio
{
    internal static class PoliceSmartRadio
    {
        public static string PlayerName = "NoNameSet";
        public static Random rnd = new Random();
        public static KeysConverter kc = new KeysConverter();
        public static bool buttonspassed = false;
        private static void MainLogic()
        {
            
            DisplayHandler.InitialiseTextures(true);
            Actions.Panic.IniSetup();
            registerActions();      
        }

        private static void registerActions()
        {          
            API.Functions.AddActionToButton(Actions.RequestPit.Main, Actions.RequestPit.available, "pit");
            API.Functions.AddActionToButton(Actions.PlateChecker.Main, "platecheck");
            API.Functions.AddActionToButton(Actions.Panic.Main, "panic");
            API.Functions.AddActionToButton(Actions.RunPedName.Main, Actions.RunPedName.IsAvailable, "pedcheck");
            API.Functions.AddActionToButton(Actions.EndCall.Main, Functions.IsCalloutRunning, "endcall");
            //API.Functions.AddActionToButton(Actions.K9.Main, "k9");
            buttonspassed = true;
            Game.LogTrivial("All PoliceSmartRadio default buttons have been assigned actions.");
            if (IsLSPDFRPluginRunning("VocalDispatch", new Version("1.6.0.0"))) {
                VocalDispatchHelper vc_platecheck = new VocalDispatchHelper();
                vc_platecheck.SetupVocalDispatchAPI("PoliceSmartRadio.PlateCheck", new VocalDispatchHelper.VocalDispatchEventDelegate(Actions.PlateChecker.vc_main));
                VocalDispatchHelper vc_requestpit = new VocalDispatchHelper();
                vc_requestpit.SetupVocalDispatchAPI("PoliceSmartRadio.RequestPIT", new VocalDispatchHelper.VocalDispatchEventDelegate(Actions.RequestPit.vc_main));
                VocalDispatchHelper vc_panic = new VocalDispatchHelper();
                vc_panic.SetupVocalDispatchAPI("PoliceSmartRadio.Panic", new VocalDispatchHelper.VocalDispatchEventDelegate(Actions.Panic.vc_main));
                VocalDispatchHelper vc_pedcheck = new VocalDispatchHelper();
                vc_pedcheck.SetupVocalDispatchAPI("PoliceSmartRadio.PedCheck", new VocalDispatchHelper.VocalDispatchEventDelegate(Actions.RunPedName.vc_main));
                VocalDispatchHelper vc_endcall = new VocalDispatchHelper();
                vc_endcall.SetupVocalDispatchAPI("PoliceSmartRadio.EndCall", new VocalDispatchHelper.VocalDispatchEventDelegate(Actions.EndCall.vc_main));
                Game.LogTrivial("PoliceSmartRadio Vocal Dispatch Integration complete.");
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

        internal static void Initialise()
        {
            Game.LogTrivial("PoliceSmartRadio, developed by Albo1125, has been loaded successfully!");
            GameFiber.StartNew(delegate
            {                
                GameFiber.Wait(6000);
                Game.DisplayNotification("~b~PoliceSmartRadio~s~, developed by ~b~Albo1125, ~s~has been loaded ~g~successfully.");

            });
            MainLogic();
        }        
    }
}
