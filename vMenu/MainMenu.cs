using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MenuAPI;
using Newtonsoft.Json;
using CitizenFX.Core;
using static CitizenFX.Core.UI.Screen;
using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.ConfigManager;
using static vMenuShared.PermissionsManager;

namespace vMenuClient
{
    public class MainMenu : BaseScript
    {
        #region Variables

        public static bool PermissionsSetupComplete => ArePermissionsSetup;
        public static bool ConfigOptionsSetupComplete = false;

        public static Control MenuToggleKey { get { return MenuController.MenuToggleKey; } private set { MenuController.MenuToggleKey = value; } } // M by default (InteractionMenu)
        public static int NoClipKey { get; private set; } = 289; // F2 by default (ReplayStartStopRecordingSecondary)
        public static Menu Menu { get; private set; }
        public static Menu PlayerSubmenu { get; private set; }
        public static Menu VehicleSubmenu { get; private set; }
        public static Menu WorldSubmenu { get; private set; }

        public static PlayerOptions PlayerOptionsMenu { get; private set; }
        public static OnlinePlayers OnlinePlayersMenu { get; private set; }
        public static BannedPlayers BannedPlayersMenu { get; private set; }
        public static SavedVehicles SavedVehiclesMenu { get; private set; }
        public static PersonalVehicle PersonalVehicleMenu { get; private set; }
        public static VehicleOptions VehicleOptionsMenu { get; private set; }
        public static VehicleSpawner VehicleSpawnerMenu { get; private set; }
        public static PlayerAppearance PlayerAppearanceMenu { get; private set; }
        public static MpPedCustomization MpPedCustomizationMenu { get; private set; }
        public static TimeOptions TimeOptionsMenu { get; private set; }
        public static WeatherOptions WeatherOptionsMenu { get; private set; }
        public static WeaponOptions WeaponOptionsMenu { get; private set; }
        public static WeaponLoadouts WeaponLoadoutsMenu { get; private set; }
        public static Recording RecordingMenu { get; private set; }
        public static MiscSettings MiscSettingsMenu { get; private set; }
        public static VoiceChat VoiceChatSettingsMenu { get; private set; }
        public static bool NoClipEnabled { get { return NoClip.IsNoclipActive(); } set { NoClip.SetNoclipActive(value); } }
        public static IPlayerList PlayersList;

        public static bool DebugMode = GetResourceMetadata(GetCurrentResourceName(), "client_debug_mode", 0) == "true" ? true : false;
        public static bool EnableExperimentalFeatures = (GetResourceMetadata(GetCurrentResourceName(), "experimental_features_enabled", 0) ?? "0") == "1";
        public static string Version { get { return GetResourceMetadata(GetCurrentResourceName(), "version", 0); } }

        public static bool DontOpenMenus { get { return MenuController.DontOpenAnyMenu; } set { MenuController.DontOpenAnyMenu = value; } }
        public static bool DisableControls { get { return MenuController.DisableMenuButtons; } set { MenuController.DisableMenuButtons = value; } }

        private const int currentCleanupVersion = 2;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainMenu()
        {
            PlayersList = new NativePlayerList(Players);

            #region cleanup unused kvps
            int tmp_kvp_handle = StartFindKvp("");
            bool cleanupVersionChecked = false;
            List<string> tmp_kvp_names = new List<string>();
            while (true)
            {
                string k = FindKvp(tmp_kvp_handle);
                if (string.IsNullOrEmpty(k))
                {
                    break;
                }
                if (k == "vmenu_cleanup_version")
                {
                    if (GetResourceKvpInt("vmenu_cleanup_version") >= currentCleanupVersion)
                    {
                        cleanupVersionChecked = true;
                    }
                }
                tmp_kvp_names.Add(k);
            }
            EndFindKvp(tmp_kvp_handle);

            if (!cleanupVersionChecked)
            {
                SetResourceKvpInt("vmenu_cleanup_version", currentCleanupVersion);
                foreach (string kvp in tmp_kvp_names)
                {
                    if (currentCleanupVersion == 1 || currentCleanupVersion == 2)
                    {
                        if (!kvp.StartsWith("settings_") && !kvp.StartsWith("vmenu") && !kvp.StartsWith("veh_") && !kvp.StartsWith("ped_") && !kvp.StartsWith("mp_ped_"))
                        {
                            DeleteResourceKvp(kvp);
                            Debug.WriteLine($"[vMenu] [cleanup id: 1] Removed unused (old) KVP: {kvp}.");
                        }
                    }
                    if (currentCleanupVersion == 2)
                    {
                        if (kvp.StartsWith("mp_char"))
                        {
                            DeleteResourceKvp(kvp);
                            Debug.WriteLine($"[vMenu] [cleanup id: 2] Removed unused (old) KVP: {kvp}.");
                        }
                    }
                }
                Debug.WriteLine("[vMenu] Cleanup of old unused KVP items completed.");
            }
            #endregion

            if (EnableExperimentalFeatures)
            {
                RegisterCommand("testped", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    PedHeadBlendData data = Game.PlayerPed.GetHeadBlendData();
                    Debug.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                }), false);

                RegisterCommand("tattoo", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    if (args != null && args[0] != null && args[1] != null)
                    {
                        Debug.WriteLine(args[0].ToString() + " " + args[1].ToString());
                        TattooCollectionData d = Game.GetTattooCollectionData(int.Parse(args[0].ToString()), int.Parse(args[1].ToString()));
                        Debug.WriteLine("check");
                        Debug.Write(JsonConvert.SerializeObject(d, Formatting.Indented) + "\n");
                    }
                }), false);

                RegisterCommand("clearfocus", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    SetNuiFocus(false, false);
                }), false);
            }

            RegisterCommand("vmenuclient", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
            {
                if (args != null)
                {
                    if (args.Count > 0)
                    {
                        if (args[0].ToString().ToLower() == "debug")
                        {
                            DebugMode = !DebugMode;
                            Notify.Custom($"Debug mode is now set to: {DebugMode}.");
                            // Set discord rich precense once, allowing it to be overruled by other resources once those load.
                            if (DebugMode)
                            {
                                SetRichPresence($"Debugging vMenu {Version}!");
                            }
                            else
                            {
                                SetRichPresence($"Enjoying FiveM!");
                            }
                        }
                        else if (args[0].ToString().ToLower() == "gc")
                        {
                            GC.Collect();
                            Debug.Write("Cleared memory.\n");
                        }
                        else if (args[0].ToString().ToLower() == "dump")
                        {
                            Notify.Info("A full config dump will be made to the console. Check the log file. This can cause lag!");
                            Debug.WriteLine("\n\n\n########################### vMenu ###########################");
                            Debug.WriteLine($"Running vMenu Version: {Version}, Experimental features: {EnableExperimentalFeatures}, Debug mode: {DebugMode}.");
                            Debug.WriteLine("\nDumping a list of all KVPs:");
                            int handle = StartFindKvp("");
                            List<string> names = new List<string>();
                            while (true)
                            {
                                string k = FindKvp(handle);
                                if (string.IsNullOrEmpty(k))
                                {
                                    break;
                                }
                                //if (!k.StartsWith("settings_") && !k.StartsWith("vmenu") && !k.StartsWith("veh_") && !k.StartsWith("ped_") && !k.StartsWith("mp_ped_"))
                                //{
                                //    DeleteResourceKvp(k);
                                //}
                                names.Add(k);
                            }
                            EndFindKvp(handle);

                            Dictionary<string, dynamic> kvps = new Dictionary<string, dynamic>();
                            foreach (var kvp in names)
                            {
                                int type = 0; // 0 = string, 1 = float, 2 = int.
                                if (kvp.StartsWith("settings_"))
                                {
                                    if (kvp == "settings_voiceChatProximity") // float
                                    {
                                        type = 1;
                                    }
                                    else if (kvp == "settings_clothingAnimationType") // int
                                    {
                                        type = 2;
                                    }
                                    else if (kvp == "settings_miscLastTimeCycleModifierIndex") // int
                                    {
                                        type = 2;
                                    }
                                    else if (kvp == "settings_miscLastTimeCycleModifierStrength") // int
                                    {
                                        type = 2;
                                    }
                                }
                                else if (kvp == "vmenu_cleanup_version") // int
                                {
                                    type = 2;
                                }
                                switch (type)
                                {
                                    case 0:
                                        var s = GetResourceKvpString(kvp);
                                        if (s.StartsWith("{") || s.StartsWith("["))
                                        {
                                            kvps.Add(kvp, JsonConvert.DeserializeObject(s));
                                        }
                                        else
                                        {
                                            kvps.Add(kvp, GetResourceKvpString(kvp));
                                        }
                                        break;
                                    case 1:
                                        kvps.Add(kvp, GetResourceKvpFloat(kvp));
                                        break;
                                    case 2:
                                        kvps.Add(kvp, GetResourceKvpInt(kvp));
                                        break;
                                }
                            }
                            Debug.WriteLine(@JsonConvert.SerializeObject(kvps, Formatting.None) + "\n");

                            Debug.WriteLine("\n\nDumping a list of allowed permissions:");
                            Debug.WriteLine(@JsonConvert.SerializeObject(Permissions, Formatting.None));

                            Debug.WriteLine("\n\nDumping vmenu server configuration settings:");
                            var settings = new Dictionary<string, string>();
                            foreach (var a in Enum.GetValues(typeof(Setting)))
                            {
                                settings.Add(a.ToString(), GetSettingsString((Setting)a));
                            }
                            Debug.WriteLine(@JsonConvert.SerializeObject(settings, Formatting.None));
                            Debug.WriteLine("\nEnd of vMenu dump!");
                            Debug.WriteLine("\n########################### vMenu ###########################");
                        }
                    }
                    else
                    {
                        Notify.Custom($"vMenu is currently running version: {Version}.");
                    }
                }
            }), false);

            if (GetCurrentResourceName() != "vMenu")
            {
                MenuController.MainMenu = null;
                MenuController.DontOpenAnyMenu = true;
                MenuController.DisableMenuButtons = true;
                throw new Exception("\n[vMenu] INSTALLATION ERROR!\nThe name of the resource is not valid. Please change the folder name from '" + GetCurrentResourceName() + "' to 'vMenu' (case sensitive)!\n");
            }
            else
            {
                Tick += OnTick;
            }
            try
            {
                SetClockDate(DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year);
            }
            catch (InvalidTimeZoneException timeEx)
            {
                Debug.WriteLine($"[vMenu] [Error] Could not set the in-game day, month and year because of an invalid timezone(?).");
                Debug.WriteLine($"[vMenu] [Error] InvalidTimeZoneException: {timeEx.Message}");
                Debug.WriteLine($"[vMenu] [Error] vMenu will continue to work normally.");
            }

            // Clear all previous pause menu info/brief messages on resource start.
            ClearBrief();

            // Request the permissions data from the server.
            TriggerServerEvent("vMenu:RequestPermissions");

            // Request server state from the server.
            TriggerServerEvent("vMenu:RequestServerState");
        }

        #region Infinity bits
        [EventHandler("vMenu:SetServerState")]
        public void SetServerState(IDictionary<string, object> data)
        {
            if (data.TryGetValue("IsInfinity", out var isInfinity))
            {
                if (isInfinity is bool isInfinityBool)
                {
                    if (isInfinityBool)
                    {
                        PlayersList = new InfinityPlayerList(Players);
                    }
                }
            }
        }

        [EventHandler("vMenu:ReceivePlayerList")]
        public void ReceivedPlayerList(IList<object> players)
        {
            PlayersList?.ReceivedPlayerList(players);
        }

        public static async Task<Vector3> RequestPlayerCoordinates(int serverId)
        {
            Vector3 coords = Vector3.Zero;
            bool completed = false;

            // TODO: replace with client<->server RPC once implemented in CitizenFX!
            Func<Vector3, bool> CallbackFunction = (data) =>
            {
                coords = data;
                completed = true;
                return true;
            };

            TriggerServerEvent("vMenu:GetPlayerCoords", serverId, CallbackFunction);

            while (!completed)
            {
                await Delay(0);
            }

            return coords;
        }
        #endregion

        #region Set Permissions function
        /// <summary>
        /// Set the permissions for this client.
        /// </summary>
        /// <param name="dict"></param>
        public static async void SetPermissions(string permissionsList)
        {
            vMenuShared.PermissionsManager.SetPermissions(permissionsList);

            VehicleSpawner.allowedCategories = new List<bool>()
            {
                IsAllowed(Permission.VSCompacts, checkAnyway: true),
                IsAllowed(Permission.VSSedans, checkAnyway: true),
                IsAllowed(Permission.VSSUVs, checkAnyway: true),
                IsAllowed(Permission.VSCoupes, checkAnyway: true),
                IsAllowed(Permission.VSMuscle, checkAnyway: true),
                IsAllowed(Permission.VSSportsClassic, checkAnyway: true),
                IsAllowed(Permission.VSSports, checkAnyway: true),
                IsAllowed(Permission.VSSuper, checkAnyway: true),
                IsAllowed(Permission.VSMotorcycles, checkAnyway: true),
                IsAllowed(Permission.VSOffRoad, checkAnyway: true),
                IsAllowed(Permission.VSIndustrial, checkAnyway: true),
                IsAllowed(Permission.VSUtility, checkAnyway: true),
                IsAllowed(Permission.VSVans, checkAnyway: true),
                IsAllowed(Permission.VSCycles, checkAnyway: true),
                IsAllowed(Permission.VSBoats, checkAnyway: true),
                IsAllowed(Permission.VSHelicopters, checkAnyway: true),
                IsAllowed(Permission.VSPlanes, checkAnyway: true),
                IsAllowed(Permission.VSService, checkAnyway: true),
                IsAllowed(Permission.VSEmergency, checkAnyway: true),
                IsAllowed(Permission.VSMilitary, checkAnyway: true),
                IsAllowed(Permission.VSCommercial, checkAnyway: true),
                IsAllowed(Permission.VSTrains, checkAnyway: true),
                IsAllowed(Permission.VSOpenWheel, checkAnyway: true)
            };
            ArePermissionsSetup = true;
            while (!ConfigOptionsSetupComplete)
            {
                await Delay(100);
            }
            PostPermissionsSetup();
            TriggerEvent("vMenu:SetupTickFunctions");
        }
        #endregion

        /// <summary>
        /// This setups things as soon as the permissions are loaded.
        /// It triggers the menu creations, setting of initial flags like PVP, player stats,
        /// and triggers the creation of Tick functions from the FunctionsController class.
        /// </summary>
        private static void PostPermissionsSetup()
        {
            switch (GetSettingsInt(Setting.vmenu_pvp_mode))
            {
                case 1:
                    NetworkSetFriendlyFireOption(true);
                    SetCanAttackFriendly(Game.PlayerPed.Handle, true, false);
                    break;
                case 2:
                    NetworkSetFriendlyFireOption(false);
                    SetCanAttackFriendly(Game.PlayerPed.Handle, false, false);
                    break;
                case 0:
                default:
                    break;
            }

            if ((IsAllowed(Permission.Staff) && GetSettingsBool(Setting.vmenu_menu_staff_only)) || GetSettingsBool(Setting.vmenu_menu_staff_only) == false)
            {
                if (GetSettingsInt(Setting.vmenu_menu_toggle_key) != -1)
                {
                    MenuToggleKey = (Control)GetSettingsInt(Setting.vmenu_menu_toggle_key);
                    //MenuToggleKey = GetSettingsInt(Setting.vmenu_menu_toggle_key);
                }
                if (GetSettingsInt(Setting.vmenu_noclip_toggle_key) != -1)
                {
                    NoClipKey = GetSettingsInt(Setting.vmenu_noclip_toggle_key);
                }

                // Create the main menu.
                Menu = new Menu(Game.Player.Name, "Menu principal");
                PlayerSubmenu = new Menu(Game.Player.Name, "Options relatives au joueur");
                VehicleSubmenu = new Menu(Game.Player.Name, "Options relatives au v??hicule");
                WorldSubmenu = new Menu(Game.Player.Name, "Options du monde");

                // Add the main menu to the menu pool.
                MenuController.AddMenu(Menu);
                MenuController.MainMenu = Menu;

                MenuController.AddSubmenu(Menu, PlayerSubmenu);
                MenuController.AddSubmenu(Menu, VehicleSubmenu);
                MenuController.AddSubmenu(Menu, WorldSubmenu);

                // Create all (sub)menus.
                CreateSubmenus();

                if (!GetSettingsBool(Setting.vmenu_disable_player_stats_setup))
                {
                    // Manage Stamina
                    if (PlayerOptionsMenu != null && PlayerOptionsMenu.PlayerStamina && IsAllowed(Permission.POUnlimitedStamina))
                        StatSetInt((uint)GetHashKey("MP0_STAMINA"), 100, true);
                    else
                        StatSetInt((uint)GetHashKey("MP0_STAMINA"), 0, true);

                    // Manage other stats, in order of appearance in the pause menu (stats) page.
                    StatSetInt((uint)GetHashKey("MP0_SHOOTING_ABILITY"), 100, true);        // Shooting
                    StatSetInt((uint)GetHashKey("MP0_STRENGTH"), 100, true);                // Strength
                    StatSetInt((uint)GetHashKey("MP0_STEALTH_ABILITY"), 100, true);         // Stealth
                    StatSetInt((uint)GetHashKey("MP0_FLYING_ABILITY"), 100, true);          // Flying
                    StatSetInt((uint)GetHashKey("MP0_WHEELIE_ABILITY"), 100, true);         // Driving
                    StatSetInt((uint)GetHashKey("MP0_LUNG_CAPACITY"), 100, true);           // Lung Capacity
                    StatSetFloat((uint)GetHashKey("MP0_PLAYER_MENTAL_STATE"), 0f, true);    // Mental State
                }
            }
            else
            {
                MenuController.MainMenu = null;
                MenuController.DisableMenuButtons = true;
                MenuController.DontOpenAnyMenu = true;
                MenuController.MenuToggleKey = (Control)(-1); // disables the menu toggle key
            }
        }

        /// <summary>
        /// Main OnTick task runs every game tick and handles all the menu stuff.
        /// </summary>
        /// <returns></returns>
        private async Task OnTick()
        {
            // If the setup (permissions) is done, and it's not the first tick, then do this:
            if (ConfigOptionsSetupComplete)
            {
                #region Handle Opening/Closing of the menu.
                var tmpMenu = GetOpenMenu();
                if (MpPedCustomizationMenu != null)
                {
                    bool IsOpen()
                    {
                        return
                            MpPedCustomizationMenu.appearanceMenu.Visible ||
                            MpPedCustomizationMenu.faceShapeMenu.Visible ||
                            MpPedCustomizationMenu.createCharacterMenu.Visible ||
                            MpPedCustomizationMenu.inheritanceMenu.Visible ||
                            MpPedCustomizationMenu.propsMenu.Visible ||
                            MpPedCustomizationMenu.clothesMenu.Visible ||
                            MpPedCustomizationMenu.tattoosMenu.Visible;
                    }

                    if (IsOpen())
                    {
                        if (tmpMenu == MpPedCustomizationMenu.createCharacterMenu)
                        {
                            MpPedCustomization.DisableBackButton = true;
                        }
                        else
                        {
                            MpPedCustomization.DisableBackButton = false;
                        }
                        MpPedCustomization.DontCloseMenus = true;
                    }
                    else
                    {
                        MpPedCustomization.DisableBackButton = false;
                        MpPedCustomization.DontCloseMenus = false;
                    }
                }

                if (Game.IsDisabledControlJustReleased(0, Control.PhoneCancel) && MpPedCustomization.DisableBackButton)
                {
                    await Delay(0);
                    Notify.Alert("Vous devez d'abord enregistrer votre ped avant de quitter, ou cliquer sur le bouton ~r~Quitter sans sauvegarder~s~.");
                }

                if (Game.CurrentInputMode == InputMode.MouseAndKeyboard)
                {
                    if (Game.IsControlJustPressed(0, (Control)NoClipKey) && IsAllowed(Permission.NoClip) && UpdateOnscreenKeyboard() != 0)
                    {
                        if (Game.PlayerPed.IsInVehicle())
                        {
                            Vehicle veh = GetVehicle();
                            if (veh != null && veh.Exists() && veh.Driver == Game.PlayerPed)
                            {
                                NoClipEnabled = !NoClipEnabled;
                            }
                            else
                            {
                                NoClipEnabled = false;
                                Notify.Error("Ce v??hicule n'existe pas (en quelque sorte) ou vous devez ??tre le conducteur de ce v??hicule pour activer noclip !");
                            }
                        }
                        else
                        {
                            NoClipEnabled = !NoClipEnabled;
                        }
                    }
                }

                #endregion

                // Menu toggle button.
                Game.DisableControlThisFrame(0, MenuToggleKey);
            }
        }

        #region Add Menu Function
        /// <summary>
        /// Add the menu to the menu pool and set it up correctly.
        /// Also add and bind the menu buttons.
        /// </summary>
        /// <param name="submenu"></param>
        /// <param name="menuButton"></param>
        private static void AddMenu(Menu parentMenu, Menu submenu, MenuItem menuButton)
        {
            parentMenu.AddMenuItem(menuButton);
            MenuController.AddSubmenu(parentMenu, submenu);
            MenuController.BindMenuItem(parentMenu, submenu, menuButton);
            submenu.RefreshIndex();
        }
        #endregion

        #region Create Submenus
        /// <summary>
        /// Creates all the submenus depending on the permissions of the user.
        /// </summary>
        private static void CreateSubmenus()
        {
            // Add the online players menu.
            if (IsAllowed(Permission.OPMenu))
            {
                OnlinePlayersMenu = new OnlinePlayers();
                Menu menu = OnlinePlayersMenu.GetMenu();
                MenuItem button = new MenuItem("Joueurs en ligne", "Tous les joueurs actuellement connect??s.")
                {
                    Label = "?????????"
                };
                AddMenu(Menu, menu, button);
                Menu.OnItemSelect += async (sender, item, index) =>
                {
                    if (item == button)
                    {
                        PlayersList.RequestPlayerList();

                        await OnlinePlayersMenu.UpdatePlayerlist();
                        menu.RefreshIndex();
                    }
                };
            }
            if (IsAllowed(Permission.OPUnban) || IsAllowed(Permission.OPViewBannedPlayers))
            {
                BannedPlayersMenu = new BannedPlayers();
                Menu menu = BannedPlayersMenu.GetMenu();
                MenuItem button = new MenuItem("Joueurs bannis", "Visualisez et g??rez tous les joueurs bannis dans ce menu.")
                {
                    Label = "?????????"
                };
                AddMenu(Menu, menu, button);
                Menu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == button)
                    {
                        TriggerServerEvent("vMenu:RequestBanList", Game.Player.Handle);
                        menu.RefreshIndex();
                    }
                };
            }

            MenuItem playerSubmenuBtn = new MenuItem("Options relatives au joueur", "Ouvrez ce sous-menu pour les sous-cat??gories li??es au joueur.") { Label = "?????????" };
            Menu.AddMenuItem(playerSubmenuBtn);

            // Add the player options menu.
            if (IsAllowed(Permission.POMenu))
            {
                PlayerOptionsMenu = new PlayerOptions();
                Menu menu = PlayerOptionsMenu.GetMenu();
                MenuItem button = new MenuItem("Options du joueur", "Les options courantes du joueur sont accessibles ici.")
                {
                    Label = "?????????"
                };
                AddMenu(PlayerSubmenu, menu, button);
            }

            MenuItem vehicleSubmenuBtn = new MenuItem("Options li??es au v??hicule", "Ouvrez ce sous-menu pour les sous-cat??gories relatives au v??hicule.") { Label = "?????????" };
            Menu.AddMenuItem(vehicleSubmenuBtn);
            // Add the vehicle options Menu.
            if (IsAllowed(Permission.VOMenu))
            {
                VehicleOptionsMenu = new VehicleOptions();
                Menu menu = VehicleOptionsMenu.GetMenu();
                MenuItem button = new MenuItem("Options du v??hicule", "Vous pouvez y modifier les options courantes du v??hicule, ainsi que le r??gler et le styliser.")
                {
                    Label = "?????????"
                };
                AddMenu(VehicleSubmenu, menu, button);
            }

            // Add the vehicle spawner menu.
            if (IsAllowed(Permission.VSMenu))
            {
                VehicleSpawnerMenu = new VehicleSpawner();
                Menu menu = VehicleSpawnerMenu.GetMenu();
                MenuItem button = new MenuItem("G??n??rateur de v??hicules", "Cr??ez un v??hicule par son nom ou choisissez-en un dans une cat??gorie sp??cifique.")
                {
                    Label = "?????????"
                };
                AddMenu(VehicleSubmenu, menu, button);
            }

            // Add Saved Vehicles menu.
            if (IsAllowed(Permission.SVMenu))
            {
                SavedVehiclesMenu = new SavedVehicles();
                Menu menu = SavedVehiclesMenu.GetMenu();
                MenuItem button = new MenuItem("V??hicules sauvegard??s", "Enregistrez de nouveaux v??hicules, ou cr??ez ou supprimez des v??hicules d??j?? enregistr??s.")
                {
                    Label = "?????????"
                };
                AddMenu(VehicleSubmenu, menu, button);
                VehicleSubmenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == button)
                    {
                        SavedVehiclesMenu.UpdateMenuAvailableCategories();
                    }
                };
            }

            // Add the Personal Vehicle menu.
            if (IsAllowed(Permission.PVMenu))
            {
                PersonalVehicleMenu = new PersonalVehicle();
                Menu menu = PersonalVehicleMenu.GetMenu();
                MenuItem button = new MenuItem("V??hicule personnel", "D??finissez un v??hicule comme votre v??hicule personnel, et contr??lez certaines choses concernant ce v??hicule lorsque vous n'??tes pas ?? l'int??rieur.")
                {
                    Label = "?????????"
                };
                AddMenu(VehicleSubmenu, menu, button);
            }

            // Add the player appearance menu.
            if (IsAllowed(Permission.PAMenu))
            {
                PlayerAppearanceMenu = new PlayerAppearance();
                Menu menu = PlayerAppearanceMenu.GetMenu();
                MenuItem button = new MenuItem("Apparence du joueur", "Choisissez un mod??le de ped, personnalisez-le et sauvegardez et chargez vos personnages personnalis??s.")
                {
                    Label = "?????????"
                };
                AddMenu(PlayerSubmenu, menu, button);

                MpPedCustomizationMenu = new MpPedCustomization();
                Menu menu2 = MpPedCustomizationMenu.GetMenu();
                MenuItem button2 = new MenuItem("Personnalisation du Ped", "Cr??er, ??diter, sauvegarder et charger des peds multi-joueurs. ~r~Attention, vous ne pouvez sauvegarder que les peds cr????s dans ce sous-menu. Le vMenu ne peut PAS d??tecter les peds cr????s en dehors de ce sous-menu.")
                {
                    Label = "?????????"
                };
                AddMenu(PlayerSubmenu, menu2, button2);
            }

            MenuItem worldSubmenuBtn = new MenuItem("Options relatives au monde", "Ouvrez ce sous-menu pour les sous-cat??gories li??es au monde.") { Label = "?????????" };
            Menu.AddMenuItem(worldSubmenuBtn);

            // Add the time options menu.
            // check for 'not true' to make sure that it _ONLY_ gets disabled if the owner _REALLY_ wants it disabled, not if they accidentally spelled "false" wrong or whatever.
            if (IsAllowed(Permission.TOMenu) && GetSettingsBool(Setting.vmenu_enable_time_sync))
            {
                TimeOptionsMenu = new TimeOptions();
                Menu menu = TimeOptionsMenu.GetMenu();
                MenuItem button = new MenuItem("Options de temps", "Changez l'heure et modifiez d'autres options li??es ?? l'heure.")
                {
                    Label = "?????????"
                };
                AddMenu(WorldSubmenu, menu, button);
            }

            // Add the weather options menu.
            // check for 'not true' to make sure that it _ONLY_ gets disabled if the owner _REALLY_ wants it disabled, not if they accidentally spelled "false" wrong or whatever.
            if (IsAllowed(Permission.WOMenu) && GetSettingsBool(Setting.vmenu_enable_weather_sync))
            {
                WeatherOptionsMenu = new WeatherOptions();
                Menu menu = WeatherOptionsMenu.GetMenu();
                MenuItem button = new MenuItem("Options m??t??o", "Modifiez toutes les options li??es ?? la m??t??o ici.")
                {
                    Label = "?????????"
                };
                AddMenu(WorldSubmenu, menu, button);
            }

            // Add the weapons menu.
            if (IsAllowed(Permission.WPMenu))
            {
                WeaponOptionsMenu = new WeaponOptions();
                Menu menu = WeaponOptionsMenu.GetMenu();
                MenuItem button = new MenuItem("Options d'armement", "Ajoutez/supprimez des armes, modifiez-les et d??finissez les options de munitions.")
                {
                    Label = "?????????"
                };
                AddMenu(PlayerSubmenu, menu, button);
            }

            // Add Weapon Loadouts menu.
            if (IsAllowed(Permission.WLMenu))
            {
                WeaponLoadoutsMenu = new WeaponLoadouts();
                Menu menu = WeaponLoadoutsMenu.GetMenu();
                MenuItem button = new MenuItem("Configurations d'armes", "G??rez, et sauvegardez vos configurations d'armes.")
                {
                    Label = "?????????"
                };
                AddMenu(PlayerSubmenu, menu, button);
            }

            if (IsAllowed(Permission.NoClip))
            {
                MenuItem toggleNoclip = new MenuItem("Basculer le NoClip", "Active ou d??sactive le NoClip.");
                PlayerSubmenu.AddMenuItem(toggleNoclip);
                PlayerSubmenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == toggleNoclip)
                    {
                        NoClipEnabled = !NoClipEnabled;
                    }
                };
            }

            // Add Voice Chat Menu.
            if (IsAllowed(Permission.VCMenu))
            {
                VoiceChatSettingsMenu = new VoiceChat();
                Menu menu = VoiceChatSettingsMenu.GetMenu();
                MenuItem button = new MenuItem("Param??tres du chat vocal", "Modifiez les options de chat vocal ici.")
                {
                    Label = "?????????"
                };
                AddMenu(Menu, menu, button);
            }

            {
                RecordingMenu = new Recording();
                Menu menu = RecordingMenu.GetMenu();
                MenuItem button = new MenuItem("Options d'enregistrement", "Options d'enregistrement dans le jeu.")
                {
                    Label = "?????????"
                };
                AddMenu(Menu, menu, button);
            }

            // Add misc settings menu.
            {
                MiscSettingsMenu = new MiscSettings();
                Menu menu = MiscSettingsMenu.GetMenu();
                MenuItem button = new MenuItem("Param??tres divers", "Diverses options/r??glages du vMenu peuvent ??tre configur??s ici. Vous pouvez ??galement enregistrer vos param??tres dans ce menu.")
                {
                    Label = "?????????"
                };
                AddMenu(Menu, menu, button);
            }

            // Refresh everything.
            MenuController.Menus.ForEach((m) => m.RefreshIndex());

            if (!GetSettingsBool(Setting.vmenu_use_permissions))
            {
                Notify.Alert("Le vMenu est configur?? pour ignorer les permissions, les permissions par d??faut seront utilis??es.");
            }

            if (PlayerSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, PlayerSubmenu, playerSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(playerSubmenuBtn);
            }

            if (VehicleSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, VehicleSubmenu, vehicleSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(vehicleSubmenuBtn);
            }

            if (WorldSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, WorldSubmenu, worldSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(worldSubmenuBtn);
            }

            if (MiscSettingsMenu != null)
            {
                MenuController.EnableMenuToggleKeyOnController = !MiscSettingsMenu.MiscDisableControllerSupport;
            }
        }
        #endregion
    }
}
