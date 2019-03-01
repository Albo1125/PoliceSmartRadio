using Albo1125.Common.CommonLibrary;
using LSPD_First_Response.Mod.API;
using Rage;
using Rage.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoliceSmartRadio.Actions
{
    // Unfinished as of the moment
    internal static class K9
    {
        private static bool onlyDuringTrafficStops = false;
        private static bool k9Busy = false;
        private static List<Model> cityK9VehicleModels = new List<Model>() {"POLICE"};
        private static List<Model> lsCountyK9VehicleModels = new List<Model>() { "SHERIFF2" };
        private static List<Model> blaineK9VehicleModels = new List<Model>() { "SHERIFF2" };
        private static List<Model> cityHandlerModels = new List<Model>() { "s_m_y_cop_01" };
        private static List<Model> lsCountyHandlerModels = new List<Model>() { "s_m_y_sheriff_01" };
        private static List<Model> blaineHandlerModels = new List<Model>() { "s_m_y_sheriff_01" };

        private static float spawnDistance = 70f;

        private static Blip k9VehicleBlip;
        private static Model k9VehicleModel;
        private static Model dogHandlerModel;
        private static Model k9Model = "a_c_shepherd";
        private static Vehicle suspectVehicle;
        private static Vehicle k9Vehicle;
        private static Ped k9;
        private static Ped dogHandler;
        private static Vector3 k9OutOfVehicle;
        private static int k9SeatIndex;

        public static bool available()
        {
            return !k9Busy && (!onlyDuringTrafficStops || Functions.GetCurrentPullover() != null);
        }

        public static void Main()
        {
            if (!available()) { return; }
            GameFiber.StartNew(k9MainLogic);
        }

        private static void k9MainLogic()
        {
            suspectVehicle = Game.LocalPlayer.Character.IsInAnyVehicle(false) ? InCar() : OnFoot();
            if (!suspectVehicle.Exists()) { Game.DisplaySubtitle("No vehicle detected."); return; }
            Zones.WorldDistricts currentdistrict = Zones.GetWorldDistrict(Game.LocalPlayer.Character.Position);
            if (currentdistrict == Zones.WorldDistricts.City)
            {
                k9VehicleModel = cityK9VehicleModels.PickRandom();
                dogHandlerModel = cityHandlerModels.PickRandom();
            }
            else if (currentdistrict == Zones.WorldDistricts.BlaineCounty)
            {
                k9VehicleModel = blaineK9VehicleModels.PickRandom();
                dogHandlerModel = blaineHandlerModels.PickRandom();
            }
            else if (currentdistrict == Zones.WorldDistricts.LosSantosCountryside)
            {
                k9VehicleModel = lsCountyK9VehicleModels.PickRandom();
                dogHandlerModel = lsCountyHandlerModels.PickRandom();
            }

            SpawnPoint sp = SpawnPointExtensions.GetVehicleSpawnPointTowardsPositionWithChecks(suspectVehicle.Position, spawnDistance);
            k9Vehicle = new Vehicle(k9VehicleModel, sp.Position, sp.Heading);
            k9Vehicle.IsPersistent = true;

            dogHandler = new Ped(dogHandlerModel, Vector3.Zero, 0);
            dogHandler.MakeMissionPed();
            dogHandler.WarpIntoVehicle(k9Vehicle, -1);

            k9 = new Ped(k9Model, Vector3.Zero, 0);
            k9.MakeMissionPed();
            
            if (k9Vehicle.FreePassengerSeatsCount > 2 && k9Vehicle.GetFreeSeatIndex(1, 2) != null)
            {
                k9.WarpIntoVehicle(k9Vehicle, k9Vehicle.GetFreeSeatIndex(1, 2).GetValueOrDefault());
            }
            else
            {
                k9.WarpIntoVehicle(k9Vehicle, 0);
            }
            k9VehicleBlip = k9Vehicle.AttachBlip();
            k9VehicleBlip.Color = System.Drawing.Color.Blue;
            k9VehicleBlip.Flash(1000, 20000);
            k9SeatIndex = k9.SeatIndex;
            driveToPosition(dogHandler, k9Vehicle, suspectVehicle.Position);
            dogHandler.Tasks.LeaveVehicle(LeaveVehicleFlags.None);
            k9.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(6000);
            k9OutOfVehicle = k9.Position;

            dogHandler.Tasks.AchieveHeading(ExtensionMethods.CalculateHeadingTowardsEntity(dogHandler, suspectVehicle)).WaitForCompletion(1000);
            GameFiber.Wait(1000);
            k9VehicleBlip.Delete();
            inspectVehicle();
            dogHandler.Tasks.EnterVehicle(k9Vehicle, 5000, -1).WaitForCompletion();
            dogHandler.Dismiss();
            k9Vehicle.Dismiss();

        }
        private static TupleList<float, float> K9_SuspectCarOffsets = new TupleList<float, float>()
        {
            new Tuple<float, float>(-2.3f, 0),
            new Tuple<float, float>(2, -100),
            new Tuple<float, float>(2.3f, 0),
            new Tuple<float, float>(2, 100),

        };
        private static void inspectVehicle()
        {
            Game.DisplaySubtitle("~b~K9 Handler: We'll let the dog do its thing. Please step back for a minute.");
            bool hasIndicated = false;            
            
            k9.Tasks.GoToOffsetFromEntity(suspectVehicle, -2.3f, 0, 2.5f).WaitForCompletion(10000);
            foreach (Tuple<float, float> off in K9_SuspectCarOffsets)
            {
                k9.Tasks.GoToOffsetFromEntity(suspectVehicle, off.Item1, off.Item2, 2.5f).WaitForCompletion(3000);
                if (suspectVehicle.Metadata.hasNarcotics == 1 && PoliceSmartRadio.rnd.Next(4) == 0)
                {
                    k9.Tasks.PlayAnimation("creatures@rottweiler@indication@", "indicate_high", 8.0f, AnimationFlags.None).WaitForCompletion();
                    hasIndicated = true;
                }
            }
            if (suspectVehicle.Metadata.hasNarcotics == 1 && !hasIndicated)
            {
                k9.Tasks.PlayAnimation("creatures@rottweiler@indication@", "indicate_high", 8.0f, AnimationFlags.None).WaitForCompletion();
                hasIndicated = true;
            }
            k9.Tasks.FollowNavigationMeshToPosition(k9OutOfVehicle, k9Vehicle.Heading, 2.5f).WaitForCompletion(8000);
            k9.Tasks.EnterVehicle(k9Vehicle, k9SeatIndex);
            Game.DisplaySubtitle("~b~Handler: The dog " + (hasIndicated ? "indicated" : "did not indicate") + " for drugs.");
        }

        private static Vehicle InCar()
        {
            if (Functions.GetCurrentPullover() != null)
            {
                return Functions.GetPulloverSuspect(Functions.GetCurrentPullover()).CurrentVehicle;
            }
            Vector3 offSetPos = Game.LocalPlayer.Character.CurrentVehicle.GetOffsetPosition(Vector3.RelativeFront * 9f);
            Vehicle[] vehicleList = Game.LocalPlayer.Character.GetNearbyVehicles(10);
            foreach (Vehicle veh in vehicleList)
            {
                if (!veh.HasSiren)
                {
                    if (Vector3.Distance(offSetPos, veh.Position) < 7.5f)
                    {
                        return veh;
                    }
                }
            }
            return null;
        }
        private static Vehicle OnFoot()
        {
            if (Functions.GetCurrentPullover() != null)
            {
                return Functions.GetPulloverSuspect(Functions.GetCurrentPullover()).CurrentVehicle;
            }
            Vector3 offSetPos = Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront * 2.5f);
            Vehicle[] vehicleList = Game.LocalPlayer.Character.GetNearbyVehicles(10);
            foreach (Vehicle veh in vehicleList)
            {
                if (!veh.HasSiren)
                {
                    if (Vector3.Distance(offSetPos, veh.Position) < 4.0f)
                    {
                        return veh;
                    }
                }
            }
            return null;
        }

        private static void driveToPosition(Ped driver, Vehicle veh, Vector3 pos)
        {

            Ped playerPed = Game.LocalPlayer.Character;
            int drivingLoopCount = 0;
            bool transportVanTeleported = false;
            int waitCount = 0;
            bool forceCloseSpawn = false;

            GameFiber.StartNew(delegate
            {
                while (!forceCloseSpawn)
                {
                    GameFiber.Yield();
                    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(System.Windows.Forms.Keys.H)) // || Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(multiTransportKey))
                    {
                        GameFiber.Sleep(500);
                        if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(System.Windows.Forms.Keys.H))// || Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(multiTransportKey))
                        {
                            GameFiber.Sleep(500);
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(System.Windows.Forms.Keys.H))// || Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(multiTransportKey))
                            {
                                forceCloseSpawn = true;
                            }
                            else
                            {
                                Game.DisplayNotification("Hold down the ~b~H ~s~to force a close spawn.");
                            }
                        }
                    }
                }
            });
            Rage.Task driveToPed = null;
            NativeFunction.Natives.SET_DRIVER_AGGRESSIVENESS(driver, 0f);
            NativeFunction.Natives.SET_DRIVER_ABILITY(1f);
            while (Vector3.Distance(veh.Position, pos) > 35f)
            {

                veh.Repair();
                if (driveToPed != null && !driveToPed.IsActive)
                {
                    driveToPed = driver.Tasks.DriveToPosition(pos, MathHelper.ConvertKilometersPerHourToMetersPerSecond(60f), VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.AllowMedianCrossing | VehicleDrivingFlags.YieldToCrossingPedestrians);
                }
                GameFiber.Wait(600);

                waitCount++;
                //If van isn't moving
                if (!veh.IsBoat)
                {
                    if (veh.Speed < 0.2f)
                    {
                        driver.Tasks.PerformDrivingManeuver(veh, VehicleManeuver.ReverseStraight, 700).WaitForCompletion();
                        drivingLoopCount += 2;
                        driver.Tasks.DriveToPosition(pos, MathHelper.ConvertKilometersPerHourToMetersPerSecond(60f), VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.AllowMedianCrossing | VehicleDrivingFlags.YieldToCrossingPedestrians).WaitForCompletion(100);
                    }
                    if (veh.Speed < 2f)
                    {
                        drivingLoopCount++;
                    }
                    //if van is very far away
                    if ((Vector3.Distance(pos, veh.Position) > spawnDistance + 70f))
                    {
                        drivingLoopCount++;
                    }
                    //If Van is stuck, relocate it

                    if ((drivingLoopCount == 30) || (drivingLoopCount == 31) || (drivingLoopCount == 32) || (drivingLoopCount == 33))
                    {
                        SpawnPoint sp = SpawnPointExtensions.GetVehicleSpawnPointTowardsPositionWithChecks(suspectVehicle.Position, spawnDistance);
                        Game.Console.Print("Relocating because k9 was stuck...");
                        veh.Position = sp;

                        veh.Heading = sp;
                        drivingLoopCount = 34;
                        Game.DisplayHelp("K9 taking too long? Hold down ~b~H ~s~to speed it up.", 5000);


                    }
                    // if van is stuck for a 2nd time or takes too long, spawn it very near to the suspect
                    else if ((drivingLoopCount >= 55) || waitCount >= 90 || forceCloseSpawn)
                    {
                        Game.Console.Print("Relocating to a closer position");

                        Vector3 SpawnPoint = World.GetNextPositionOnStreet(pos.Around2D(15f));

                        int waitCounter = 0;
                        while ((SpawnPoint.Z - pos.Z < -3f) || (SpawnPoint.Z - pos.Z > 3f) || (Vector3.Distance(SpawnPoint, pos) > 25f))
                        {
                            waitCounter++;
                            SpawnPoint = World.GetNextPositionOnStreet(pos.Around2D(15f));
                            GameFiber.Yield();
                            if (waitCounter >= 500)
                            {
                                SpawnPoint = pos.Around2D(15f);
                                break;
                            }
                        }
                        veh.Position = SpawnPoint;
                        Vector3 directionFromVehicleToPed = (pos - SpawnPoint);
                        directionFromVehicleToPed.Normalize();

                        float vehicleHeading = MathHelper.ConvertDirectionToHeading(directionFromVehicleToPed);
                        veh.Heading = vehicleHeading;
                        transportVanTeleported = true;

                        break;
                    }
                }
                else
                {
                    NativeFunction.Natives.REQUEST_COLLISION_AT_COORD(veh.Position.X, veh.Position.Y, veh.Position.Z);
                    NativeFunction.Natives.REQUEST_ADDITIONAL_COLLISION_AT_COORD(veh.Position.X, veh.Position.Y, veh.Position.Z);

                    if (waitCount > 85)
                    {
                        break;
                    }
                    if (veh.Speed < 0.2f)
                    {
                        NativeFunction.Natives.SET_VEHICLE_FORWARD_SPEED(veh, 15f);
                        GameFiber.Wait(700);
                    }
                }

            }

            forceCloseSpawn = true;
            //park the van
            int reverseCount = 0;
            Game.HideHelp();
            while ((Vector3.Distance(pos, veh.Position) > 21f) && !transportVanTeleported)
            {
                Rage.Task parkNearSuspect = driver.Tasks.DriveToPosition(pos, MathHelper.ConvertKilometersPerHourToMetersPerSecond(30f), VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.DriveAroundObjects | VehicleDrivingFlags.AllowMedianCrossing | VehicleDrivingFlags.YieldToCrossingPedestrians);
                parkNearSuspect.WaitForCompletion(800);
                transportVanTeleported = false;
                if (Vector3.Distance(pos, veh.Position) > 75f)
                {
                    Vector3 SpawnPoint = World.GetNextPositionOnStreet(pos.Around(12f));
                    veh.Position = SpawnPoint;
                }
                if (veh.Speed < 0.2f)
                {
                    reverseCount++;
                    if (reverseCount == 3)
                    {
                        driver.Tasks.PerformDrivingManeuver(veh, VehicleManeuver.ReverseStraight, 1700).WaitForCompletion();
                        reverseCount = 0;
                    }
                }

                if ((veh.IsBoat && veh.DistanceTo(pos) < 25) || ExtensionMethods.IsPointOnWater(pos))
                {
                    break;
                }

            }
            GameFiber.Wait(600);



        }
    }
}
