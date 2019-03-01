using LSPD_First_Response.Engine.Scripting.Entities;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoliceSmartRadio.Actions
{
    internal static class RunPedName
    {
        public static bool IsAvailable()
        {
            return GetNearestValidPed(allowPursuitPeds:true, subtitleDisplayTime:-1).Exists();
        }
        public static bool vc_main() { Main(); return true; }
        public static void Main()
        {
            Ped p = GetNearestValidPed();
            if (p.Exists())
            {
                Persona pers = Functions.GetPersonaForPed(p);
                Game.DisplayNotification("~b~" + PoliceSmartRadio.PlayerName + "~s~: Dispatch, could you run a person check through for me? It's ~b~" + pers.FullName + "~s~, born on ~b~" + pers.Birthday.ToShortDateString() + "~s~.");
                if (PoliceSmartRadio.IsLSPDFRPluginRunning("British Policing Script", new Version("0.8.2.0")))
                {
                    API.BritishPolicingScriptFunctions.RunPedCheck(p, 4000);
                }
                else {
                    displayRecords(pers);
                }
            }
        }

        private static void displayRecords(Persona pers)
        {
            
            string msg = "Record for: ~b~" + pers.FullName + "~n~~y~" + pers.Gender.ToString() + "~s~, Born: ~y~" + pers.Birthday.ToShortDateString() + "~n~~s~- License is " + licenceStateString(pers.ELicenseState) + ".~n~~s~- " + wantedString(pers.Wanted) + ".";
            GameFiber.Wait(4000);

            Game.DisplayNotification("3dtextures", "mpgroundlogo_cops", "~b~DISPATCH", pers.FullName, msg);
        }

        private static string licenceStateString(ELicenseState state)
        {
            if (state == ELicenseState.Valid)
            {
                return "~s~valid";
            }
            else if (state == ELicenseState.Suspended)
            {
                return "~r~suspended";
            }
            else if (state == ELicenseState.Expired)
            {
                return "~o~expired";
            }
            else
            {
                return "~s~no records";
            }
        }
        private static string wantedString(bool wanted)
        {
            return wanted ? "~r~Suspect is wanted" : "~s~No active warrants";
        }

        private static Ped GetNearestValidPed(float Radius = 3.5f, bool allowPursuitPeds = false, int subtitleDisplayTime = 3000)
        {
            if (Game.LocalPlayer.Character.IsInAnyVehicle(false) || Game.LocalPlayer.Character.GetNearbyPeds(1).Length == 0) { return null; }
            Ped nearestped = Game.LocalPlayer.Character.GetNearbyPeds(1)[0];

            if (nearestped.RelationshipGroup == "COP")
            {
                if (Game.LocalPlayer.Character.GetNearbyPeds(2).Length >= 2) { nearestped = Game.LocalPlayer.Character.GetNearbyPeds(2)[1]; }
                if (nearestped.RelationshipGroup == "COP")
                {
                    return null;
                }
            }
            if (Vector3.Distance(Game.LocalPlayer.Character.Position, nearestped.Position) > Radius) { Game.DisplaySubtitle("Get closer to the ped", subtitleDisplayTime); return null; }
            if (!allowPursuitPeds && Functions.GetActivePursuit() != null)
            {
                if (Functions.GetPursuitPeds(Functions.GetActivePursuit()).Contains(nearestped)) { return null; }
            }
            if (!nearestped.IsHuman) { Game.DisplaySubtitle("Ped isn't human...", subtitleDisplayTime); return null; }
            if (Functions.IsPedGettingArrested(nearestped) && !Functions.IsPedArrested(nearestped)) { return null; }
            return nearestped;
        }
    }
}
