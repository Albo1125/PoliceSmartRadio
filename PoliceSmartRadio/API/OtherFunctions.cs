using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoliceSmartRadio.API
{
    internal static class BritishPolicingScriptFunctions
    {
        public static void RunLicencePlateCheck(Vehicle veh)
        {
            British_Policing_Script.VehicleRecords recs = British_Policing_Script.API.Functions.GetVehicleRecords(veh);
            recs.RunPlateCheck();
        }

        public static void RunPedCheck(Ped p, int delay = 0)
        {
            British_Policing_Script.BritishPersona recs = British_Policing_Script.API.Functions.GetBritishPersona(p);
            GameFiber.Wait(delay);
            recs.RunLicenceCheck();
        }
    }
}
