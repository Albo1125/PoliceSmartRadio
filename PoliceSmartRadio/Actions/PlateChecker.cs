using LSPD_First_Response.Engine.Scripting.Entities;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Albo1125.Common.CommonLibrary;
using static Albo1125.Common.CommonLibrary.CommonVariables;
using System.Media;
using LSPD_First_Response.Mod.API;

namespace PoliceSmartRadio.Actions
{
    internal class PlateChecker
    {
        private const string PlateAudioPathModifier = "Plugins/LSPDFR/PoliceSmartRadio/Audio/PlateCheck/";
        private const string AudioPathExtension = ".wav";

        private static string[] vehYears = { "2004", "2005", "2006", "2007", "2008", "2009", "2010", "2011", "2012", "2013", "2014", "2015", "2016", "2017" };

        private static char[] vowels = new char[] { 'a', 'e', 'o', 'i', 'u' };

        private static Dictionary<Vehicle, PlateChecker> AllVehiclesChecked = new Dictionary<Vehicle, PlateChecker>();
        private static Vehicle vehCurrentlyBeingChecked;

        public string Flags = "Flags: ~r~";
        public Persona DriverPersona;
        public string LicencePlate;
        public string vehModel;
        public string modelArticle;
        public string vehicleYear;

        private List<string> AudioFlags = new List<string>();

        public PlateChecker(Vehicle vehicleToCheck)
        {
            if (vehicleToCheck.Exists())
            {
                string Flags = "";
                if (vehicleToCheck.IsStolen)
                {
                    Flags += "STOLEN ";
                    DriverPersona = LSPD_First_Response.Mod.API.Functions.GetPersonaForPed(World.GetAllPeds()[0]);
                    //determine persona to use
                    if (vehicleToCheck.HasDriver && vehicleToCheck.Driver.Exists())
                    {
                        if (LSPD_First_Response.Mod.API.Functions.GetPersonaForPed(vehicleToCheck.Driver).FullName.ToLower() == Functions.GetVehicleOwnerName(vehicleToCheck).ToLower())
                        {
                            DriverPersona = Functions.GetPersonaForPed(World.GetAllPeds()[0]);
                        }
                        else
                        {
                            foreach (Ped p in World.EnumeratePeds())
                            {
                                if (Functions.GetPersonaForPed(p).FullName.ToLower() == Functions.GetVehicleOwnerName(vehicleToCheck).ToLower())
                                {
                                    DriverPersona = Functions.GetPersonaForPed(p);
                                    break;
                                }
                            }
                            
                        }
                    }
                    if (DriverPersona == null)
                    {
                        //create new persona
                        Persona tempPers = Functions.GetPersonaForPed(World.GetAllPeds()[0]);
                        //DriverPersona = new Persona(World.GetAllPeds()[0], tempPers.Gender, tempPers.BirthDay, tempPers.Citations, tempPers.Forename, tempPers.Surname, tempPers.LicenseState, tempPers.TimesStopped, tempPers.Wanted, tempPers.IsAgent, tempPers.IsCop);
                    }
                    AudioFlags.Add("Stolen");
                }
                else
                {
                    //determine persona to use
                    if (vehicleToCheck.HasDriver && vehicleToCheck.Driver.Exists())
                    {
                        if (LSPD_First_Response.Mod.API.Functions.GetPersonaForPed(vehicleToCheck.Driver).FullName.ToLower() == Functions.GetVehicleOwnerName(vehicleToCheck).ToLower())
                        {
                            DriverPersona = LSPD_First_Response.Mod.API.Functions.GetPersonaForPed(vehicleToCheck.Driver);
                        }
                        else
                        {
                            foreach (Ped p in World.EnumeratePeds())
                            {
                                if (Functions.GetPersonaForPed(p).FullName.ToLower() == Functions.GetVehicleOwnerName(vehicleToCheck).ToLower())
                                {
                                    DriverPersona = Functions.GetPersonaForPed(p);
                                    break;
                                }
                            }
                        }
                    }
                    if (DriverPersona == null)
                    {
                        DriverPersona = PersonaHelper.GenerateNewPersona();                      
                    }
                    //DriverPersona = vehicleToCheck.HasDriver && Albo1125.Common.CommonLibrary.CommonVariables.rnd.Next(100) < 80 ? LSPD_First_Response.Mod.API.Functions.GetPersonaForPed(vehicleToCheck.Driver) : LSPD_First_Response.Mod.API.Functions.GetPersonaForPed(World.GetAllPeds()[0]);
                }
                LSPD_First_Response.Mod.API.Functions.SetVehicleOwnerName(vehicleToCheck, DriverPersona.FullName);
                //determine flags on the vehicle
                if (!PoliceSmartRadio.IsLSPDFRPluginRunning("British Policing Script", new Version("0.9.0.0")))
                {
                    if (Traffic_Policer.API.Functions.GetVehicleRegistrationStatus(vehicleToCheck) == Traffic_Policer.EVehicleDetailsStatus.Expired)
                    {
                        Flags += "EXPIRED REGISTRATION ";
                        AudioFlags.Add("TrafficViolation");
                    }
                    else if (Traffic_Policer.API.Functions.GetVehicleRegistrationStatus(vehicleToCheck) == Traffic_Policer.EVehicleDetailsStatus.None)
                    {
                        Flags += "NO REGISTRATION ";
                        AudioFlags.Add("TrafficViolation");
                    }

                    if (Traffic_Policer.API.Functions.GetVehicleInsuranceStatus(vehicleToCheck) == Traffic_Policer.EVehicleDetailsStatus.Expired)
                    {
                        Flags += "EXPIRED INSURANCE ";
                        AudioFlags.Add("TrafficViolation");
                    }
                    else if (Traffic_Policer.API.Functions.GetVehicleInsuranceStatus(vehicleToCheck) == Traffic_Policer.EVehicleDetailsStatus.None)
                    {
                        Flags += "NO INSURANCE ";
                        AudioFlags.Add("TrafficViolation");
                    }

                    if (DriverPersona.Wanted)
                    {
                        AudioFlags.Add("Warrant");
                        if (rnd.Next(100) < 75)
                        {
                            Flags += "FELONY WARRANT FOR REGISTERED OWNER ";

                        }
                        else
                        {
                            Flags += "BENCH WARRANT FOR REGISTERED OWNER ";
                        }
                    }

                    if (DriverPersona.ELicenseState == ELicenseState.Suspended)
                    {
                        Flags += "OWNER'S LICENCE SUSPENDED ";
                        AudioFlags.Add("TrafficFelony");
                    }
                    else if (DriverPersona.ELicenseState == ELicenseState.Expired)
                    {
                        Flags += "OWNER'S LICENCE EXPIRED ";
                        AudioFlags.Add("TrafficViolation");
                    }
                    else if (DriverPersona.ELicenseState == ELicenseState.None)
                    {
                        Flags += "OWNER'S LICENCE INVALID ";
                        AudioFlags.Add("TrafficViolation");
                    }

                    if (DriverPersona.Birthday.Date == DateTime.Now.Date)
                    {
                        Flags += "~g~OWNER'S BIRTHDAY ";
                    }

                    if (string.IsNullOrWhiteSpace(Flags))
                    {
                        AudioFlags.Add("No1099");
                        this.Flags += "~g~NONE";
                    }
                    else
                    {
                        AudioFlags.Add("ProceedCaution");
                        this.Flags += Flags;
                    }
                }

                //read in strings from the vehicle information
                LicencePlate = vehicleToCheck.LicensePlate;

                vehModel = vehicleToCheck.Model.Name.ToLower();
                vehModel = char.ToUpper(vehModel[0]) + vehModel.Substring(1);
                if (vowels.Contains(vehModel.ToLower()[0]))
                {
                    modelArticle = "an";
                }
                else { modelArticle = "a"; }
                vehicleYear = vehYears.PickRandom();
                AllVehiclesChecked.Add(vehicleToCheck, this);
            }
            
        }

        public void PlayAudio()
        {
            try
            {
                SoundPlayer TgtLicencePlate = new SoundPlayer(PlateAudioPathModifier + "TargetPlate" + rnd.Next(1, 4) + AudioPathExtension);
                TgtLicencePlate.Play(); //Target licence plate
                GameFiber.Wait(1900);
                //read out the plate characters
                foreach (char character in LicencePlate.ToUpper())
                {

                    SoundPlayer plyr = new SoundPlayer(PlateAudioPathModifier + character + AudioPathExtension);
                    plyr.Play();

                    GameFiber.Wait(500);
                    plyr.Dispose();
                }
                //read out the flag information
                foreach (string flag in AudioFlags)
                {
                    SoundPlayer plyr = new SoundPlayer(PlateAudioPathModifier + flag + AudioPathExtension);
                    plyr.Play();

                    GameFiber.Wait(1900);
                    plyr.Dispose();
                }
            }
            catch(Exception e)
            {
                Game.LogTrivial(e.ToString());
                Game.LogTrivial("PoliceSmartRadio handled the exception in PlayAudio for platecheck.");
            }
        }

        public static bool vc_main() { Main(); return true; }
        public static void Main()
        {
            GameFiber.StartNew(delegate
            {
                try
                {
                    Vehicle vehicleToCheck = Game.LocalPlayer.Character.IsInAnyVehicle(false) ? InCar() : OnFoot();
                    if (vehicleToCheck.Exists())
                    {
                        if (vehicleToCheck.IsTrailer)
                        {
                            vehicleToCheck = ((Vehicle[])World.GetEntities(vehicleToCheck.GetOffsetPositionFront(5), 15, GetEntitiesFlags.ConsiderAllVehicles | GetEntitiesFlags.ExcludePlayerVehicle)).
                                FirstOrDefault(x => x.HasTrailer && x.Trailer == vehicleToCheck);
                            if (!vehicleToCheck) { return; }
                        }
                        vehCurrentlyBeingChecked = vehicleToCheck;
                        PlateChecker checker;
                        if (AllVehiclesChecked.ContainsKey(vehicleToCheck))
                        {
                            checker = AllVehiclesChecked[vehicleToCheck];
                        }
                        else
                        {
                            checker = new PlateChecker(vehicleToCheck);
                        }
                        Game.DisplayNotification("~b~" + PoliceSmartRadio.PlayerName + ": ~s~Dispatch, can you do a plate check for me? It's " + checker.modelArticle + " ~b~" + checker.vehModel + "~s~, plate ~b~" + checker.LicencePlate + ".");
                        GameFiber.Wait(2000);
                        Game.DisplayNotification("~b~Dispatch: ~s~Copy that, stand by for the plate check...");
                        GameFiber.Wait(4000);
                        GameFiber.StartNew(checker.PlayAudio);
                        GameFiber.Wait(500);
                        if (PoliceSmartRadio.IsLSPDFRPluginRunning("British Policing Script", new Version("0.8.0.0")) && vehicleToCheck.Exists())
                        {
                            API.BritishPolicingScriptFunctions.RunLicencePlateCheck(vehicleToCheck);
                        }
                        else
                        {
                            Game.DisplayNotification("~b~Dispatch: ~s~" + PoliceSmartRadio.PlayerName + ", plate check: ~n~~b~Plate: " + checker.LicencePlate + "~n~Model: " + checker.vehModel
                                + "~n~Reg. Year: " + checker.vehicleYear + "~n~Registered Owner: ~y~" + checker.DriverPersona.FullName + "~b~~n~Citations: " + checker.DriverPersona.Citations);
                            GameFiber.Wait(2000);
                            Game.DisplayNotification(checker.Flags);
                        }
                        if (vehCurrentlyBeingChecked == vehicleToCheck)
                        {
                            vehCurrentlyBeingChecked = null;
                        }
                    }
                }
                catch (Exception e)
                {
                    Game.LogTrivial(e.ToString());
                    Game.DisplayNotification("Whoops! Police SmartRadio plate check encountered an error. Please send your RAGEPluginHook.log file to the author (Albo1125).");
                    Game.LogTrivial("PoliceSmartRadio handled the PlateCheck exception.");
                }
            });
        }

        private static Vehicle InCar()
        {
            Vector3 offSetPos = Game.LocalPlayer.Character.CurrentVehicle.GetOffsetPosition(Vector3.RelativeFront * 9f);
            Vehicle[] vehicleList = Game.LocalPlayer.Character.GetNearbyVehicles(16);
            foreach (Vehicle veh in vehicleList)
            {
                if (veh != vehCurrentlyBeingChecked && !veh.HasSiren)
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

            Vector3 offSetPos = Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront * 4.2f);
            Vehicle[] vehicleList = Game.LocalPlayer.Character.GetNearbyVehicles(10);
            foreach (Vehicle veh in vehicleList)
            {
                if (veh != vehCurrentlyBeingChecked && !veh.HasSiren)
                {

                    if (Vector3.Distance(offSetPos, veh.Position) < 4.0f)
                    {
                        return veh;
                    }
                }
            }
            return null;
        }
    }
}
