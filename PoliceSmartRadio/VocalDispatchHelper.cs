using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoliceSmartRadio
{
    internal class VocalDispatchHelper
    {
        public delegate bool VocalDispatchEventDelegate();
        //ALL references to ANYTHING VocalDispatch related must be done in a separate class and not used until you're sure VocalDispatch is available.
        //Doing this will prevent crashes and allow you to degrade gracefully if it is not.    
        //The RegisterForAPI event in VocalDispatch returns Guid.Empty if, for some reason, it could not register your event handler. Store the Guid here so you can clean up later.        
        private Guid vocaldispatchapiguid = Guid.Empty;
        private VocalDispatchEventDelegate safeeventhandler = null;
        /// <summary>
        /// This function will be called directly by VocalDispatch.
        /// It then calls the function you specify in SetupVocalDispatchAPI.
        /// The function you specify has no knowledge of VocalDispatch and can therefore exist safely in your code.
        /// This function cannot exist safely in other classes and must be hidden away here, instead, to provide a safe middleman between VocalDispatch and your code.
        /// </summary>
        public bool MiddleManEventHandler()
        {
            if (safeeventhandler != null)
                return safeeventhandler(); //Calls the function we specify when we call SetupVocalDispatchAPI
            return false;
        }
        public void SetupVocalDispatchAPI(string eventtohandle, VocalDispatchEventDelegate specifiedsafeeventhandler)
        {
            //Setup our notification function and tell VocalDispatch to call it when it hears the appropriate phrase
            safeeventhandler = specifiedsafeeventhandler;

            vocaldispatchapiguid = VocalDispatch.APIv1.RegisterEventHandler(eventtohandle, new VocalDispatch.APIv1.VocalDispatchPhraseNotificationEventHandlerFunction(MiddleManEventHandler));
        }
        public void ReleaseVocalDispatchAPI()
        {
            if (vocaldispatchapiguid != Guid.Empty)
                VocalDispatch.APIv1.UnregisterEventHandler(vocaldispatchapiguid);
            safeeventhandler = null;
            vocaldispatchapiguid = Guid.Empty;
        }
    }

}
