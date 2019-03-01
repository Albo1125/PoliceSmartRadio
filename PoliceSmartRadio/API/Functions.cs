using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PoliceSmartRadio.API
{
    public static class Functions
    {
        /// <summary>
        /// Adds an Action to the specified button. Only buttons contained in a folder matching your plugin's name can be manipulated.
        /// </summary>
        /// <param name="action">The action to execute if the button is selected.</param>
        /// <param name="buttonName">The texture file name of the button, excluding any directories or file extensions.</param>
        /// <returns>Returns whether the button was successfully added or not. If false, a reason is logged to the console.</returns>
        [Obfuscation(Exclude = false, Feature = "-ref proxy")]
        public static bool AddActionToButton(Action action, string buttonName)
        {
           
            Game.LogTrivial(Assembly.GetCallingAssembly().GetName().Name + " requesting a PoliceSmartRadio action to be added to " + buttonName);
            //Game.LogTrivial("DoneLoading?" + DisplayHandler.DoneLoadingTextures);
            return DisplayHandler.AddActionToButton(action, null, Assembly.GetCallingAssembly().GetName().Name, buttonName);
        }

        /// <summary>
        /// Adds an Action and an availability check to the specified button. Only buttons contained in a folder matching your plugin's name can be manipulated.
        /// </summary>
        /// <param name="action">The action to execute if the button is selected.</param>
        /// <param name="isAvailable">Function returning a bool indicating whether the button is currently available (if false, button is hidden). This is often called, so try making this light-weight (e.g. simply return the value of a boolean property). Make sure to do proper checking in your Action too, as the user can forcefully display all buttons via a setting in their config file.</param>
        /// <param name="buttonName">The texture file name of the button, excluding any directories or file extensions.</param>
        /// <returns>Returns whether the button was successfully added or not. If false, a reason is logged to the console.</returns>
        [Obfuscation(Exclude = false, Feature = "-ref proxy")]
        public static bool AddActionToButton(Action action, Func<bool> isAvailable, string buttonName)
        {
            Game.LogTrivial(Assembly.GetCallingAssembly().GetName().Name + " requesting a PoliceSmartRadio action to be added to " + buttonName);
            //Game.LogTrivial("DoneLoading?" + DisplayHandler.DoneLoadingTextures);
            return DisplayHandler.AddActionToButton(action, isAvailable, Assembly.GetCallingAssembly().GetName().Name, buttonName);
        }

        /// <summary>
        /// Raised whenever the player selects a button on the SmartRadio.
        /// </summary>
        public static event Action ButtonSelected;

        internal static void OnButtonSelected()
        {

            if (ButtonSelected != null)
            {
                ButtonSelected();
            }
        }
    }
}
