using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoliceSmartRadio.Actions
{
    internal static class EndCall
    {
        public static bool vc_main() { Main(); return true; }
        public static void Main()
        {
            Game.DisplayNotification("~b~" + PoliceSmartRadio.PlayerName + "~s~: Dispatch, you can clear my last call, over.");
            LSPD_First_Response.Mod.API.Functions.StopCurrentCallout();
        }
    }
}
