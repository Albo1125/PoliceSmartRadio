using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoliceSmartRadio.Actions
{
    internal static class RequestPit
    {
        public static bool vc_main() { Main(); return true; }
        public static void Main()
        {
            //if (pitRequestActive) { Game.LogTrivial("PIT request already active"); return; }
            GameFiber.StartNew(delegate
            {
                
                if (!Game.LocalPlayer.Character.IsInAnyVehicle(false)) { return; }
                if (Functions.GetActivePursuit() == null) { return; }
                List<Ped> eligiblePeds = Functions.GetPursuitPeds(Functions.GetActivePursuit()).Where(x => x.DistanceTo(Game.LocalPlayer.Character) < 55f && x.IsInAnyVehicle(false)).ToList();
                Game.LogTrivial(eligiblePeds.Count.ToString());
                if (eligiblePeds.Count > 0)
                {
                    Vehicle targetVeh = eligiblePeds.OrderBy(x => x.DistanceTo(Game.LocalPlayer.Character.GetOffsetPositionFront(4f))).First().CurrentVehicle;
                    Ped targetPed = targetVeh.Driver;
                    Vehicle[] nearbyVehs = targetPed.Exists() ? targetPed.GetNearbyVehicles(16) : Game.LocalPlayer.Character.GetNearbyVehicles(16);

                    int NearbyOccupiedCivilianVehsCount = 0;
                    foreach (Vehicle veh in nearbyVehs)
                    {
                        if (veh.Exists())
                        {
                            if (veh.HasDriver)
                            {
                                if (!veh.HasSiren)
                                {
                                    NearbyOccupiedCivilianVehsCount++;
                                }
                            }
                        }
                    }
                    Game.DisplayNotification("~b~" + PoliceSmartRadio.PlayerName + "~s~: Dispatch, requesting to perform ~r~PIT~s~.");

                    if (NearbyOccupiedCivilianVehsCount > 7)
                    {
                        GameFiber.Wait(4000);
                        Game.DisplayNotification("~b~Dispatch ~w~: " + PoliceSmartRadio.PlayerName + ", traffic is ~r~too busy.~s~ You ~r~aren't clear~s~ to ~r~PIT, ~w~over.");
                        return;
                    }
                    else
                    {
                        GameFiber.Wait(4000);
                        Game.DisplayNotification("~b~Dispatch ~w~: " + PoliceSmartRadio.PlayerName + " you are clear at this time to ~r~PIT, ~w~over.");
                    }

                    bool pitComplete = false;
                    Stopwatch standstillStopwatch = new Stopwatch();
                    standstillStopwatch.Start();
                    while (!pitComplete)
                    {
                        GameFiber.Yield();
                        if ((targetVeh.Speed <= 0.8f) && (Vector3.Distance(targetVeh.Position, Game.LocalPlayer.Character.Position) < 18f))
                        {
                            if (standstillStopwatch.ElapsedMilliseconds > 3500)
                            {
                                targetVeh.EngineHealth = 0.0f;
                                Game.DisplayNotification("~b~" + PoliceSmartRadio.PlayerName + "~s~: PIT successful, located at~b~ " + World.GetStreetName(Game.LocalPlayer.Character.Position));
                                pitComplete = true;
                            }
                        }
                        else
                        {
                            standstillStopwatch.Restart();
                        }

                        if (!targetVeh.HasDriver)
                        {
                            break;
                        }
                        
                    }
                }
            });
        }

        public static bool available()
        {
            return Functions.GetActivePursuit() != null;
        }
    }
}
