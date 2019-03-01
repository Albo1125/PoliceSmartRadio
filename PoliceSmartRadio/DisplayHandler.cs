using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Albo1125.Common.CommonLibrary;
using System.Drawing;
using System.Media;

namespace PoliceSmartRadio
{
    internal static class DisplayHandler
    {
        public enum UIPositions { TopRight, TopLeft, BottomRight, BottomLeft, CentreRight, CentreLeft, CentreBottom, CentreTop };
        public static bool DoneLoadingTextures = false;

        private static SoundPlayer ButtonSelectSound = new SoundPlayer("Plugins/LSPDFR/PoliceSmartRadio/Audio/ButtonSelect.wav");
        private static SoundPlayer ButtonScrollSound = new SoundPlayer("Plugins/LSPDFR/PoliceSmartRadio/Audio/ButtonScroll.wav");

        private static bool PlayAnimations = true;
        private static bool PlayRadioButtonSounds = true;

        private const string PathModifier = "Plugins/LSPDFR/PoliceSmartRadio/Display/";
        private const string DisplayExtension = ".png";

        private const string onFootXML = "Plugins/LSPDFR/PoliceSmartRadio/ButtonSetup/OnFoot.xml";
        private const string inVehicleXML = "Plugins/LSPDFR/PoliceSmartRadio/ButtonSetup/InVehicle.xml";

        private static Texture RadioBackgroundTexture;

        private static List<ButtonPage> CurrentButtonPages = new List<ButtonPage>();
        private static List<ButtonPage> AllButtonPages = new List<ButtonPage>();
        private static List<ButtonPage> OnFootButtonPages = new List<ButtonPage>();
        private static List<ButtonPage> InVehicleButtonPages = new List<ButtonPage>();
        private static ButtonPage CurrentPage;
        private static bool OnFootPagesActive = true;

        public static Keys ToggleRadioKey = Keys.C;
        public static Keys ToggleRadioModifierKey = Keys.None;
        public static Keys NextButtonKey = Keys.G;
        public static Keys PreviousButtonKey = Keys.T;
        public static Keys SelectButtonKey = Keys.Z;
        public static Keys NextPageKey = Keys.Right;
        public static Keys PreviousPageKey = Keys.Left;

        public static ControllerButtons ToggleRadioButton = ControllerButtons.DPadLeft;
        public static ControllerButtons ToggleRadioModifierButton = ControllerButtons.None;
        public static ControllerButtons NextButtonButton = ControllerButtons.DPadDown;
        public static ControllerButtons PreviousButtonButton = ControllerButtons.DPadUp;
        public static ControllerButtons SelectButtonButton = ControllerButtons.X;
        public static ControllerButtons NextPageButton = ControllerButtons.None;
        public static ControllerButtons PreviousPageButton = ControllerButtons.None;

        private static bool RadioShowing = false;
        private static int CurrentButtonIndex = 0;
        private static int CurrentPageIndex = 0;
        private static int ButtonsPerPage = 4;
        private static bool ResetRadioWhenOpening = false;
        private static bool AlwaysDisplayButtons = false;

        private static float MasterScalingFactor = 1.0f;
        private static int BaseX = 0;
        private static int BaseY = 0;

        private static List<Point> buttonOffsets = new List<Point>();

        private static InitializationFile PositioningIni = new InitializationFile(PathModifier + "DisplayPositioning.ini");
        private static InitializationFile GeneralIni = new InitializationFile("Plugins/LSPDFR/PoliceSmartRadio/Config/GeneralConfig.ini");
        private static InitializationFile KeyboardIni = new InitializationFile("Plugins/LSPDFR/PoliceSmartRadio/Config/KeyboardConfig.ini");
        private static InitializationFile ControllerIni = new InitializationFile("Plugins/LSPDFR/PoliceSmartRadio/Config/ControllerConfig.ini");
        private static InitializationFile DisplayIni = new InitializationFile("Plugins/LSPDFR/PoliceSmartRadio/Config/DisplayConfig.ini");


        public static void InitialiseTextures (bool FirstTime)
        {
            createButtonActionQueue();
            SetupTextures();
            if (FirstTime)
            {
                Game.RawFrameRender += DrawImage;
            }
        }

        private static void SetupUserGeneralSettings()
        {
            KeysConverter kc = new KeysConverter();
            PlayAnimations = GeneralIni.ReadBoolean("GeneralConfig", "PlayAnimations", true);
            PlayRadioButtonSounds = GeneralIni.ReadBoolean("GeneralConfig", "PlayRadioButtonSounds", true);
            PoliceSmartRadio.PlayerName = GeneralIni.ReadString("GeneralConfig", "PlayerName", "NoNameSet");
            ResetRadioWhenOpening = GeneralIni.ReadBoolean("GeneralConfig", "ResetToPageOneOnOpen", false);
            AlwaysDisplayButtons = GeneralIni.ReadBoolean("GeneralConfig", "AlwaysDisplayButtons", false);

            ToggleRadioKey = (Keys)kc.ConvertFromString(KeyboardIni.ReadString("KeyboardConfig", "ToggleRadioKey", "C"));
            ToggleRadioModifierKey = (Keys)kc.ConvertFromString(KeyboardIni.ReadString("KeyboardConfig", "ToggleRadioModifierKey", "None"));
            NextButtonKey = (Keys)kc.ConvertFromString(KeyboardIni.ReadString("KeyboardConfig", "NextButtonKey", "G"));
            PreviousButtonKey = (Keys)kc.ConvertFromString(KeyboardIni.ReadString("KeyboardConfig", "PreviousButtonKey", "T"));
            SelectButtonKey = (Keys)kc.ConvertFromString(KeyboardIni.ReadString("KeyboardConfig", "SelectButtonKey", "Z"));
            NextPageKey = (Keys)kc.ConvertFromString(KeyboardIni.ReadString("KeyboardConfig", "NextPageKey", "Right"));
            PreviousPageKey = (Keys)kc.ConvertFromString(KeyboardIni.ReadString("KeyboardConfig", "PreviousPageKey", "Left"));

            ToggleRadioButton = ControllerIni.ReadEnum<ControllerButtons>("ControllerConfig", "ToggleRadioButton", ControllerButtons.DPadLeft);
            ToggleRadioModifierButton = ControllerIni.ReadEnum<ControllerButtons>("ControllerConfig", "ToggleRadioModifierButton", ControllerButtons.None);
            NextButtonButton = ControllerIni.ReadEnum<ControllerButtons>("ControllerConfig", "NextButtonButton", ControllerButtons.DPadDown);
            PreviousButtonButton = ControllerIni.ReadEnum<ControllerButtons>("ControllerConfig", "PreviousButtonButton", ControllerButtons.DPadUp);
            SelectButtonButton = ControllerIni.ReadEnum<ControllerButtons>("ControllerConfig", "SelectButtonButton", ControllerButtons.X);
            NextPageButton = ControllerIni.ReadEnum<ControllerButtons>("ControllerConfig", "NextPageButton", ControllerButtons.None);
            PreviousPageButton = ControllerIni.ReadEnum<ControllerButtons>("ControllerConfig", "PreviousPageButton", ControllerButtons.None);
        }

        private static void SetupTextures()
        {
            GeneralIni.Create();
            SetupBackgroundSettings();
            ButtonsPerPage = PositioningIni.ReadInt32("General", "ButtonsPerPage", 4);
            for (int i = 1; i <= ButtonsPerPage; i++)
            {
                if (PositioningIni.DoesKeyExist("Button" + i, "XOffset") && PositioningIni.DoesKeyExist("Button" + i, "YOffset"))
                {
                    int XOffset = PositioningIni.ReadInt32("Button" + i, "XOffset", 0);
                    int YOffset = PositioningIni.ReadInt32("Button" + i, "YOffset", 0);
                    buttonOffsets.Add(new Point(XOffset, YOffset));
                }
                else
                {
                    Albo1125.Common.CommonLibrary.ExtensionMethods.DisplayPopupTextBoxWithConfirmation("PoliceSmartRadio DisplayPositioning.ini Error", "Your UI has set " + ButtonsPerPage + " buttons per page, but has not specified XOffsets and YOffsets for so many buttons. Aborting.", true);
                    return;
                }      
            }

            OnFootButtonPages = SetupButtonPage(onFootXML);
            InVehicleButtonPages = SetupButtonPage(inVehicleXML);
            if (firstTimeLaunch)
            {
                HandleFirstLaunch();
            }
            CurrentButtonPages = OnFootButtonPages;
            CurrentPage = CurrentButtonPages.Count > 0 ? CurrentButtonPages[0] : new ButtonPage();

            DoneLoadingTextures = true;
            Game.LogTrivial("Police SmartRadio is done loading. Button actions ready to be added.");
            SetupUserGeneralSettings();
            ButtonSelectSound.Load();
            ButtonScrollSound.Load();
            MainLogic();
        }
        private static bool firstTimeLaunch = false;
        private static List<ButtonPage> SetupButtonPage(string file)
        {
            int ItemCount = 1;
            int Page = 1;
            List<Button> AllButtons = new List<Button>();

            try
            {
                if(!File.Exists(file))
                {
                    generateButtonXML(file);
                    firstTimeLaunch = true;
                }
                XDocument xdoc = XDocument.Load(file);
                
                foreach (XElement x in xdoc.Root.Descendants("Button"))
                {
                    if (!string.IsNullOrWhiteSpace((string)x.Element("Plugin")) && !string.IsNullOrWhiteSpace((string)x.Element("Name")) && !string.IsNullOrWhiteSpace((string)x.Element("Enabled")))
                    {
                        Button b = new Button(x.Element("Plugin").Value, x.Element("Name").Value, ItemCount, bool.Parse(x.Element("Enabled").Value));
                        if (!AllButtons.Contains(b))
                        {
                            AllButtons.Add(b);
                            ItemCount++;
                            if (ItemCount > ButtonsPerPage)
                            {
                                Page++;
                                ItemCount = 1;
                            }
                        }

                    }
                    else
                    {
                        Game.LogTrivial("PoliceSmartRadio: button in " + file + " has no Plugin or Name. Skipping.");
                    }
                    
                }
                Game.LogTrivial("Allbuttons ln: " + AllButtons.Count);
            }
            catch (Exception e)
            {
                Game.LogTrivial(e.ToString());
            }

            List<ButtonPage> ButtonsPages = new List<ButtonPage>();
            int addingToPage = 0;
            int itemsOnPage = 0;
            foreach (Button b in AllButtons)
            {
                if (ButtonsPages.Count <= addingToPage)
                {
                    ButtonsPages.Add(new ButtonPage());
                }
                ButtonsPages[addingToPage].Buttons.Add(b);
                itemsOnPage++;
                if (itemsOnPage == ButtonsPerPage)
                {
                    addingToPage++;
                    itemsOnPage = 0;
                }
            }
            AllButtonPages.AddRange(ButtonsPages);
            if (ButtonsPages.Count == 0) { ButtonsPages.Add(new ButtonPage()); }
            return ButtonsPages;           
        }

        private static void SetupBackgroundSettings()
        {
            RadioBackgroundTexture = Game.CreateTextureFromFile(PathModifier + "Background" + DisplayExtension);
            MasterScalingFactor = DisplayIni.ReadSingle("DisplayConfig", "DisplayScalingFactor", 1.0f);
            RadioBackgroundTextureWidth = RadioBackgroundTexture.Size.Width * MasterScalingFactor;
            RadioBackgroundTextureHeight = RadioBackgroundTexture.Size.Height * MasterScalingFactor;

            UIPositions Position = DisplayIni.ReadEnum<UIPositions>("DisplayConfig", "DisplayPosition", UIPositions.BottomRight);
            if (!string.IsNullOrWhiteSpace(DisplayIni.ReadString("DisplaySpecificPositioning", "X", "")) && !string.IsNullOrWhiteSpace(DisplayIni.ReadString("DisplaySpecificPositioning", "Y", "")))
            {
                BaseX = DisplayIni.ReadInt32("DisplaySpecificPositioning", "X");
                BaseY = DisplayIni.ReadInt32("DisplaySpecificPositioning", "Y");
                Game.LogTrivial("Setting Display position to specifics: " + BaseX + ":" + BaseY);
            }
            else
            {
                switch (Position)
                {
                    case UIPositions.BottomRight:
                        BaseX = Game.Resolution.Width - (Int32)RadioBackgroundTextureWidth;
                        BaseY = Game.Resolution.Height - (Int32)RadioBackgroundTextureHeight;
                        break;
                    case UIPositions.BottomLeft:
                        BaseX = 0;
                        BaseY = Game.Resolution.Height - (Int32)RadioBackgroundTextureHeight;
                        break;
                    case UIPositions.TopLeft:
                        BaseX = 0;
                        BaseY = 0;
                        break;
                    case UIPositions.TopRight:
                        BaseX = Game.Resolution.Width - (Int32)RadioBackgroundTextureWidth;
                        BaseY = 0;
                        break;
                    case UIPositions.CentreLeft:
                        BaseX = 0;
                        BaseY = Game.Resolution.Height / 2 - (Int32)RadioBackgroundTextureHeight / 2;
                        break;
                    case UIPositions.CentreRight:
                        BaseX = Game.Resolution.Width - (Int32)RadioBackgroundTextureWidth;
                        BaseY = Game.Resolution.Height / 2 - (Int32)RadioBackgroundTextureHeight / 2;
                        break;
                    case UIPositions.CentreBottom:
                        BaseX = Game.Resolution.Width / 2 - (Int32)RadioBackgroundTextureWidth / 2;
                        BaseY = Game.Resolution.Height - (Int32)RadioBackgroundTextureHeight;
                        break;
                    case UIPositions.CentreTop:
                        BaseX = Game.Resolution.Width / 2 - (Int32)RadioBackgroundTextureWidth / 2;
                        BaseY = 0;
                        break;
                }
                Game.LogTrivial("Setting Display position to " + Position.ToString());
                Game.LogTrivial(BaseX + ":" + BaseY);
            }
        }

        private static void MoveButtonIndex (bool Next)
        {
            if (PlayRadioButtonSounds)
            {
                ButtonScrollSound.Play();
            }

            if (Next)
            {
                if (CurrentPage.Buttons.Count <= CurrentButtonIndex + 1)
                {
                    goToNextPage();                 
                }
                else
                {
                    CurrentButtonIndex++;
                }
            }

            else
            {
          
                if (CurrentButtonIndex == 0)
                {
                    goToPreviousPage();
                }
                else
                {                
                    CurrentButtonIndex--;
                }
            }
        }

        private static void goToNextPage(bool playSounds = false)
        {
            if (PlayRadioButtonSounds && playSounds)
            {
                ButtonScrollSound.Play();
            }
            cascadeCurrentButtonPages(false);
            CurrentButtonIndex = 0;
            if (CurrentButtonPages.Count <= CurrentPageIndex + 1)
            {
                CurrentPage = CurrentButtonPages[0];
                CurrentPageIndex = 0;

            }
            else
            {
                CurrentPage = CurrentButtonPages[CurrentPageIndex + 1];
                CurrentPageIndex++;
            }
        }

        private static void goToPreviousPage(bool playSounds = false)
        {
            if (PlayRadioButtonSounds && playSounds)
            {
                ButtonScrollSound.Play();
            }
            cascadeCurrentButtonPages(false);
            if (CurrentPageIndex == 0)
            {

                CurrentPage = CurrentButtonPages.Last();
                CurrentPageIndex = CurrentButtonPages.Count - 1;
            }
            else
            {

                CurrentPage = CurrentButtonPages[CurrentPageIndex - 1];
                CurrentPageIndex--;
            }
            CurrentButtonIndex = CurrentPage.Buttons.Count - 1;
        }
        
        private static void MainLogic()
        {
            GameFiber.StartNew(delegate
            {
                while (true)
                {
                    GameFiber.Yield();
                    if (ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.LControlKey) && ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.LShiftKey) && ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.Z))
                    {
                        handleEditMode();
                    }
                    else if (CurrentButtonPages.Count > 0)
                    {
                        if ((Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(ToggleRadioKey) && (ToggleRadioModifierKey == Keys.None || Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(ToggleRadioModifierKey)))
                        || (Game.IsControllerButtonDown(ToggleRadioButton) && (Game.IsControllerButtonDownRightNow(ToggleRadioModifierButton) || ToggleRadioModifierButton == ControllerButtons.None)))
                        {
                            CurrentButtonIndex = 0;
                            if (!RadioShowing) { cascadeCurrentButtonPages(); }
                            if (ResetRadioWhenOpening) { CurrentPageIndex = 0; }
                            RadioShowing = !RadioShowing;
                        }
                        if (RadioShowing)
                        {
                            if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(NextButtonKey) || Game.IsControllerButtonDown(NextButtonButton))
                            {
                                MoveButtonIndex(true);
                            }
                            else if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(PreviousButtonKey) || Game.IsControllerButtonDown(PreviousButtonButton))
                            {
                                MoveButtonIndex(false);
                            }
                            else if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownComputerCheck(SelectButtonKey) || Game.IsControllerButtonDown(SelectButtonButton))
                            {
                                RadioShowing = false;
                                if (CurrentPage.Buttons.Count > CurrentButtonIndex)
                                {
                                    if (CurrentPage.Buttons[CurrentButtonIndex].IsAvailable)
                                    {
                                        handleButtonPress(CurrentPage.Buttons[CurrentButtonIndex]);
                                    }
                                    else
                                    {
                                        Game.LogTrivial("This PoliceSmartRadio button was set as currently not available.");
                                    }
                                }
                                else
                                {
                                    Game.LogTrivial("Current button out of range on select (blank page)?");
                                    Game.DisplayNotification("You'll need to add buttons to your SmartRadio first.");
                                }
                            }
                            else if (ExtensionMethods.IsKeyDownComputerCheck(NextPageKey) || Game.IsControllerButtonDown(NextPageButton))
                            {
                                goToNextPage(true);
                            }
                            else if (ExtensionMethods.IsKeyDownComputerCheck(PreviousPageKey) || Game.IsControllerButtonDown(PreviousPageButton))
                            {
                                goToPreviousPage(true);
                                CurrentButtonIndex = 0;
                            }
                        }
                        else
                        {
                            if (Game.LocalPlayer.Character.IsInAnyVehicle(false) && OnFootPagesActive)
                            {
                                CurrentButtonPages = InVehicleButtonPages;
                                cascadeCurrentButtonPages();
                                CurrentPage = CurrentButtonPages[0];
                                CurrentPageIndex = 0;
                                CurrentButtonIndex = 0;
                                OnFootPagesActive = false;

                            }
                            else if (!Game.LocalPlayer.Character.IsInAnyVehicle(false) && !OnFootPagesActive)
                            {
                                CurrentButtonPages = OnFootButtonPages;
                                cascadeCurrentButtonPages();
                                CurrentPage = CurrentButtonPages[0];
                                CurrentPageIndex = 0;
                                CurrentButtonIndex = 0;
                                OnFootPagesActive = true;

                            }
                        }
                    }

                }
            });
        }

        public static void handleButtonPress(Button b)
        {
            if (PlayAnimations)
            {
                Game.LocalPlayer.Character.Tasks.PlayAnimation("random@arrests", "generic_radio_chatter", 1.5f, AnimationFlags.UpperBodyOnly | AnimationFlags.SecondaryTask);
            }
            GameFiber.Wait(750);
            if (PlayRadioButtonSounds)
            {
                ButtonSelectSound.Play();
            }
            API.Functions.OnButtonSelected();
            b.SelectAction();
        }

        private static void handleEditMode()
        {
            GameFiber.Wait(200);

            ExtensionMethods.DisplayPopupTextBoxWithConfirmation("Police SmartRadio", "Edit mode is now active. w,a,s,d - Move. q,e - Scale. Press LCtrl LShift Z again to save and deactivate edit mode. Press Escape to discard.", false);
            while (true)
            {
                GameFiber.Yield();
                Game.DisplayHelp("Police SmartRadio-Edit Mode Active. LCtrl LShift Z to save. Escape to discard.");
                Game.LocalPlayer.HasControl = false;
                RadioShowing = true;
                CurrentButtonIndex = 0;
                CurrentPageIndex = 0;
                if (ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.W))
                {
                    BaseY -= 1;
                    cascadeCurrentButtonPages();
                    GameFiber.Sleep(30);
                }
                else if (ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.S))
                {
                    BaseY += 1;
                    cascadeCurrentButtonPages();
                    GameFiber.Sleep(30);
                }
                else if (ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.A))
                {
                    BaseX -= 1;
                    cascadeCurrentButtonPages();
                    GameFiber.Sleep(30);
                }
                else if (ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.D))
                {
                    BaseX += 1;
                    cascadeCurrentButtonPages();
                    GameFiber.Sleep(30);
                }
                else if (ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.Q))
                {
                    MasterScalingFactor += 0.005f;
                    RadioBackgroundTextureWidth = RadioBackgroundTexture.Size.Width * MasterScalingFactor;
                    RadioBackgroundTextureHeight = RadioBackgroundTexture.Size.Height * MasterScalingFactor;
                    cascadeCurrentButtonPages();
                    GameFiber.Sleep(30);
                }
                else if (ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.E))
                {
                    MasterScalingFactor -= 0.005f;
                    RadioBackgroundTextureWidth = RadioBackgroundTexture.Size.Width * MasterScalingFactor;
                    RadioBackgroundTextureHeight = RadioBackgroundTexture.Size.Height * MasterScalingFactor;
                    if (MasterScalingFactor < 0) { MasterScalingFactor = 0; }
                    cascadeCurrentButtonPages();
                    GameFiber.Sleep(30);
                }
                if (ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.LControlKey) && ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.LShiftKey) && ExtensionMethods.IsKeyDownRightNowComputerCheck(Keys.Z))
                {
                    DisplayIni.Write("DisplaySpecificPositioning", "X", BaseX);
                    DisplayIni.Write("DisplaySpecificPositioning", "Y", BaseY);
                    DisplayIni.Write("DisplayConfig", "DisplayScalingFactor", MasterScalingFactor);
                    Game.DisplayHelp("Police Radio Edit Mode Deactivated - Changes saved to config files.");
                    break;
                }
                if (ExtensionMethods.IsKeyDownComputerCheck(Keys.Escape))
                {
                    SetupBackgroundSettings();
                    RadioBackgroundTextureWidth = RadioBackgroundTexture.Size.Width * MasterScalingFactor;
                    RadioBackgroundTextureHeight = RadioBackgroundTexture.Size.Height * MasterScalingFactor;
                    cascadeCurrentButtonPages();
                    Game.DisplayHelp("Police Radio Edit Mode Deactivated - Changes discarded.");
                    break;
                }
            }
                        
            GameFiber.Wait(200);
            Game.LocalPlayer.HasControl = true;
        }

        private static float RadioBackgroundTextureWidth = 0;
        private static float RadioBackgroundTextureHeight = 0;
        private static void DrawImage(System.Object sender, Rage.GraphicsEventArgs e)
        {
            if (RadioShowing && DoneLoadingTextures)
            {
                
                e.Graphics.DrawTexture(RadioBackgroundTexture, new RectangleF(BaseX, BaseY, RadioBackgroundTextureWidth, RadioBackgroundTextureHeight));
                foreach (Button item in CurrentPage.Buttons)
                {

                    if (item.CurrentTexture != null)
                    {
                        e.Graphics.DrawTexture(item.CurrentTexture, item.TextureRectangle);
                    }
                }
            }
        }

        public static bool AddActionToButton(Action action, Func<bool> isAvailable, string Plugin, string ButtonName)
        {
            bool successful = false;
            string fullName = (Plugin + "/" + ButtonName).ToLower();
            //Game.LogTrivial("ALLBUTTONPAGES LENGTH: " + AllButtonPages.Count);
            if (DoneLoadingTextures)
            {
                foreach (ButtonPage page in AllButtonPages)
                {
                   
                    Button desiredItem = page.Buttons.Where(x => x == fullName).FirstOrDefault();

                    if (desiredItem != null)
                    {

                        desiredItem.SelectAction = action;
                        desiredItem._isAvailableFunc = isAvailable;
                        successful = true;
                        
                    }
                }
                if (!successful)
                {
                    HandleNewButtonAdding(Plugin, ButtonName, onFootXML);
                    HandleNewButtonAdding(Plugin, ButtonName, inVehicleXML);
                    Game.LogTrivial("Your new button action will be taken into account by PoliceSmartRadio from the next time you go on duty.");
                }
                else
                {
                    Game.LogTrivial("PoliceSmartRadio: Adding action for " + Plugin + " to " + ButtonName + " successful.");
                }
            }
            else
            {
                Game.LogTrivial("Police SmartRadio is not done setting up. Adding to queue. ");
                buttonActionAddProfile.buttonActionAddQueue.Add(new buttonActionAddProfile(action, isAvailable, Plugin, ButtonName));
                Game.LogTrivial("Added to queue. Length: " + buttonActionAddProfile.buttonActionAddQueue.Count);
            }
            return successful;
        }
        
        private class buttonActionAddProfile
        {
            internal static List<buttonActionAddProfile> buttonActionAddQueue = new List<buttonActionAddProfile>();
            internal Action act;
            internal Func<bool> isAvailable;
            internal string plugin;
            internal string buttonname;
            public buttonActionAddProfile(Action act, Func<bool> isAvailable, string Plugin, string buttonname)
            {
                this.act = act;
                this.isAvailable = isAvailable;
                this.plugin = Plugin;
                this.buttonname = buttonname;
            }
        }

        private static void createButtonActionQueue()
        {
            Game.LogTrivial("Police SmartRadio queue created. Waiting...");
            GameFiber.StartNew(delegate
            {
                
                while (!DoneLoadingTextures)
                {
                    GameFiber.Yield();
                }
                Game.LogTrivial("PoliceSmartRadio done loading. Dequeueing actions... (" + buttonActionAddProfile.buttonActionAddQueue.Count + ")");
                foreach (buttonActionAddProfile prof in buttonActionAddProfile.buttonActionAddQueue.ToArray())
                {
                    AddActionToButton(prof.act, prof.isAvailable, prof.plugin, prof.buttonname);
                }
            });
        }

        private static void cascadeCurrentButtonPages(bool pageRangeCheck = true)
        {
            
            List<ButtonPage> cascadedButtonPages = new List<ButtonPage>();
            List<Button> availableButtons = new List<Button>();
            CurrentButtonPages = OnFootPagesActive ? OnFootButtonPages : InVehicleButtonPages;
            foreach (ButtonPage bp in CurrentButtonPages)
            {
                foreach (Button button in bp.Buttons)
                { 

                    if (button.IsAvailable && button.Enabled)
                    {
                        availableButtons.Add(button);
                        
                    }
                }
            }

            int addingToPage = 0;
            int itemsOnPage = 0;
            foreach (Button b in availableButtons)
            {
                if (cascadedButtonPages.Count <= addingToPage)
                {
                    cascadedButtonPages.Add(new ButtonPage());
                }
                cascadedButtonPages[addingToPage].Buttons.Add(b);
                b.updateRectangle(itemsOnPage);
                itemsOnPage++;
                if (itemsOnPage == ButtonsPerPage)
                {
                    addingToPage++;
                    itemsOnPage = 0;
                }
            }
            CurrentButtonPages = cascadedButtonPages;
            if (CurrentPageIndex >= CurrentButtonPages.Count && pageRangeCheck)
            {
                CurrentPageIndex = CurrentButtonPages.Count - 1;    
                if (CurrentPageIndex < 0) { CurrentPageIndex = 0; }            
            }
            if (CurrentButtonPages.Count > 0)
            {
                CurrentPage = CurrentButtonPages[CurrentPageIndex];
            }
            else
            {
                CurrentButtonPages.Add(new ButtonPage());
                CurrentPage = CurrentButtonPages[0];
            }
        }

        private static void generateButtonXML(string file)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            XDocument xdoc = new XDocument(
                new XElement("PoliceSmartRadio",
                    new XComment(@"PoliceSmartRadio by Albo1125.
PoliceSmartRadio Customisation Video Tutorial: https://www.youtube.com/watch?v=aJPA_nIEZxo
<Button> tags will automatically be added if a plugin tries to assign itself to a button. You can choose whether to enable that button or not.
The buttons are shown ingame in the order entered here.
You can add as many <Button> tags as you like, they'll be sorted into pages based on the value in the DisplayPositioning.ini file.
The <Plugin> value MUST correspond to the plugin name assigning an action to the button. It also determines which folder the textures are loaded from (see <Name> explanation).
The <Name> value MUST correspond to the name of a png file in either the PoliceSmartRadio/Display/On/Plugin or PoliceSmartRadio/Display/Off/Plugin folders.
The <Enabled> values MUST be either 'true' or 'false' (without quotation marks). It determines if the button is shown ingame.")
                    ));
            xdoc.Save(file);
        }

        private class ButtonPage
        {
            public List<Button> Buttons = new List<Button>();
            public Button SelectedItem
            {
                get
                {
                    return Buttons[CurrentButtonIndex];
                }
            }

            public ButtonPage(List<Button> items)
            {
                this.Buttons = items;
            }
            public ButtonPage()
            {

            }
            public override string ToString()
            {
                string output = "";
                Buttons.ForEach(x => output += x.FullName);
                return output;
            }
        }

        internal class Button
        {
            public static Button GetButtonByFullName(string fullname)
            {
                Button desiredItem = null;
                foreach (ButtonPage page in AllButtonPages)
                {

                    desiredItem = page.Buttons.Where(x => x == fullname).FirstOrDefault();

                    if (desiredItem != null)
                    {
                        break;
                    }
                }
                return desiredItem;
            }

            public static Button GetButtonByFullName(string pluginname, string buttonname)
            {
                return GetButtonByFullName((pluginname + "/" + buttonname).ToLower());
            }

            public string Plugin;
            public string ButtonName;
            public string FullName
            {
                get
                {
                    return (Plugin + "/" + ButtonName).ToLower();
                }
            }
            public bool On
            {
                get
                {
                    if (CurrentPage == null)
                    {
                        return true;
                    }
                    else
                    {
                        return CurrentPage.Buttons[CurrentButtonIndex] == this;
                    }  
                }
            }
            public bool Enabled = true;

            public Func<bool> _isAvailableFunc;
            public bool IsAvailable
            {
                get
                {
                    if (ValidTexture == null)
                    {
                        return false;
                    }
                    if (_isAvailableFunc == null || AlwaysDisplayButtons)
                    {
                        return true;
                    }
                    else
                    {
                        return _isAvailableFunc();                      
                    }
                }
            }
            private Texture OnTexture;
            private Texture OffTexture;

            public Action SelectAction; //called when this item is selected by the user.

            public RectangleF TextureRectangle;
            public Texture ValidTexture
            {
                get
                {
                    if (OnTexture == null)
                    {
                        return OffTexture;
                    }
                    else
                    {
                        return OnTexture;
                    }
                }
            }
            public Texture CurrentTexture
            {
                get
                {
                    return On ? OnTexture : OffTexture;
                }
            }

            public Button(string Plugin, string ButtonName, int ItemCount, bool Enabled)
            {
                this.Plugin = Plugin;
                this.ButtonName = ButtonName;
                
                this.SelectAction = DefaultAction;

                if (File.Exists(PathModifier + "On/" + this.FullName + DisplayExtension) || File.Exists(PathModifier + "Off/" + this.FullName + DisplayExtension))
                {

                    if (File.Exists(PathModifier + "On/" + this.FullName + DisplayExtension))
                    {
                        OnTexture = Game.CreateTextureFromFile(PathModifier + "On/" + this.FullName + DisplayExtension);
                    }
                    else
                    {
                        Game.LogTrivial("Ontexture for " + this.FullName + " doesn't exist.");
                    }
                    if (File.Exists(PathModifier + "Off/" + this.FullName + DisplayExtension))
                    {
                        OffTexture = Game.CreateTextureFromFile(PathModifier + "Off/" + this.FullName + DisplayExtension);
                    }
                    else
                    {
                        Game.LogTrivial("Offtexture for " + this.FullName + " doesn't exist.");
                    }
                    this.Enabled = Enabled;
                    updateRectangle(ItemCount - 1);
                    
                }
                else
                {
                    Game.LogTrivial(this.FullName + " display image files don't exist for Police SmartRadio. Skipping.");
                }
            }

            public static implicit operator string(Button x)
            {
                return x.FullName;
            }

            public void DefaultAction()
            {
                Game.LogTrivial("Default action for " + FullName);
                Game.DisplayNotification("No action has (yet) been assigned to ~b~" + FullName + ". ~s~No plugin has assigned itself to the button, or you may have gone on duty twice - make sure only to go on duty once.");
            }

            public void updateRectangle(int buttonNumber)
            {
                if (this.ValidTexture != null)
                {
                    this.TextureRectangle = new RectangleF(BaseX + buttonOffsets[buttonNumber].X * MasterScalingFactor, BaseY + buttonOffsets[buttonNumber].Y * MasterScalingFactor, this.CurrentTexture.Size.Width * MasterScalingFactor, this.CurrentTexture.Size.Height * MasterScalingFactor);
                }
            }

            public override bool Equals(object obj)
            {
                if (obj is Button)
                {
                    return this.FullName == ((Button)obj).FullName;
                }
                return base.Equals(obj);
            }
        }


        private static bool newButtonsInQueue = false;
        private static string lastButtonName = "";
        private static Popup currentNewButtonPopup = new Popup();
        private static List<string> newButtonAnswers = new List<string>() { "Enable this new button", "Disable this new button" };
        private static void HandleNewButtonAdding(string Plugin, string ButtonName, string XMLFile)
        {
            
            GameFiber.StartNew(delegate
            {
                
                while (!currentNewButtonPopup.hasDisplayed  && !string.IsNullOrWhiteSpace(currentNewButtonPopup.PopupTitle))
                {
                    newButtonsInQueue = true;
                    GameFiber.Sleep(10);
                    if (ButtonName != lastButtonName)
                    {
                        GameFiber.Sleep(100);
                    }
                }
                lastButtonName = ButtonName;
                newButtonsInQueue = false;

                currentNewButtonPopup = new Popup("PoliceSmartRadio Setup: New Button (" + ButtonName + ")",
                    Plugin + " is adding a new button, " + ButtonName + ", to " + Path.GetFileName(XMLFile) + ". Would you like to enable it in " + Path.GetFileName(XMLFile) + "? You can change this at any time by editing the XML file manually.",
                    newButtonAnswers, false, true);
                currentNewButtonPopup.Display();
                while (currentNewButtonPopup.IndexOfGivenAnswer == -1)
                {
                    GameFiber.Yield();
                }
                XDocument xdoc = XDocument.Load(XMLFile);
                xdoc.Root.Add(new XElement("Button", new XElement("Plugin", Plugin), new XElement("Name", ButtonName), new XElement("Enabled", (currentNewButtonPopup.IndexOfGivenAnswer == 0).ToString())));
                xdoc.Save(XMLFile);

                if (!newButtonsInQueue)
                {
                    
                    Game.DisplayNotification("This was the last new button in the queue. LSPDFR will now reload so ~b~PoliceSmartRadio~s~ can refresh itself. Type 'forceduty' in console once it's done.");
                    GameFiber.Sleep(3000);
                    Game.ReloadActivePlugin();
                }
            });
        }
        private static Popup firstLaunchPopup1 = new Popup("PoliceSmartRadio by Albo1125: First Launch", "Welcome to PoliceSmartRadio! First time launch setup will now proceed. " + Environment.NewLine + " It is important to fully understand how to walk through this - you only have one opportunity to do the easy first time setup. I have created a video that guides you through the process. Would you like to open this now?",
                new List<string>() { "Yes, please help me set up PoliceSmartRadio the way I want it.", "No, I have already watched the video or have read the documentation and I know what to do." }, false, true, firstLaunchPopupCb);
        private static Popup firstLaunchPopup2 = new Popup("PoliceSmartRadio by Albo1125: First Launch", "Are you sure? You will only get one opportunity to do the first time setup easily ingame. If you make a mistake and change your mind, you will have to edit the XML files manually.",
                        new List<string>() { "Yes, please help me set up PoliceSmartRadio the way I want it.", "No, I have already watched the video or have read the documentation and I know what to do." }, false, true);

        private static void HandleFirstLaunch()
        {

            firstLaunchPopup1.Display();
            //firstLaunchPopup2.Display();
            
        }

        private static void firstLaunchPopupCb(Popup p)
        {
            if (p.IndexOfGivenAnswer == 0)
            {
                System.Diagnostics.Process.Start("https://www.youtube.com/watch?v=aJPA_nIEZxo");
            }
            
        }

    }
}
