using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace PoliceSmartRadio.Actions
{
    internal static class Panic
    {
        public static int minStandardUnits = 3;
        public static int maxStandardUnits = 5;

        public static int minSwatUnits = 0;
        public static int maxSwatUnits = 2;
        private static SoundPlayer panicButtonSound = new SoundPlayer("Plugins/LSPDFR/PoliceSmartRadio/Audio/PanicButton.wav");
        private static InitializationFile panicIni = new InitializationFile("Plugins/LSPDFR/PoliceSmartRadio/Config/PanicButton.ini");
        private static bool playSound = true;
        private static int soundDuration = 450;

        public static bool vc_main() { Main(); return true; }
        public static void Main()
        {
            if (playSound)
            {
                for (int i = 0; i < 3; i++)
                {
                    panicButtonSound.Play();
                    GameFiber.Wait(soundDuration);
                }
            }
            int standards = PoliceSmartRadio.rnd.Next(minStandardUnits, maxStandardUnits + 1);
            for (int i = 0; i < standards; i++)
            {
                Game.LogTrivial("Spawning standard panic unit.");
                Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Code3, PoliceSmartRadio.rnd.Next(6) < 4 ? LSPD_First_Response.EBackupUnitType.LocalUnit : LSPD_First_Response.EBackupUnitType.StateUnit);
                GameFiber.Wait(500);
            }

            int swat = PoliceSmartRadio.rnd.Next(minSwatUnits, maxSwatUnits + 1);
            for (int i = 0; i < swat; i++)
            {
                Game.LogTrivial("Spawning SWAT panic unit.");
                Functions.RequestBackup(Game.LocalPlayer.Character.Position, LSPD_First_Response.EBackupResponseType.Code3, PoliceSmartRadio.rnd.Next(6) < 4 ? LSPD_First_Response.EBackupUnitType.SwatTeam : LSPD_First_Response.EBackupUnitType.NooseTeam);
                GameFiber.Wait(500);
                
            }
        }

        public static void IniSetup()
        {
            minStandardUnits = panicIni.ReadInt32("PanicButtonConfig", "MinStandardUnits", 3);
            maxStandardUnits = panicIni.ReadInt32("PanicButtonConfig", "MaxStandardUnits", 5);

            minSwatUnits = panicIni.ReadInt32("PanicButtonConfig", "MinSwatUnits", 0);
            maxSwatUnits = panicIni.ReadInt32("PanicButtonConfig", "MaxSwatUnits", 2);
            soundDuration = panicIni.ReadInt32("PanicButtonConfig", "SoundDuration", 450);
            playSound = panicIni.ReadBoolean("PanicButtonConfig", "PlaySound", true);
        }
    }
}
