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
using static vMenuShared.PermissionsManager;

namespace vMenuClient
{
    public class PlayerOptions
    {
        // Menu variable, will be defined in CreateMenu()
        private Menu menu;

        // Public variables (getters only), return the private variables.
        public bool PlayerGodMode { get; private set; } = UserDefaults.PlayerGodMode;
        public bool PlayerInvisible { get; private set; } = false;
        public bool PlayerStamina { get; private set; } = UserDefaults.UnlimitedStamina;
        public bool PlayerFastRun { get; private set; } = UserDefaults.FastRun;
        public bool PlayerFastSwim { get; private set; } = UserDefaults.FastSwim;
        public bool PlayerSuperJump { get; private set; } = UserDefaults.SuperJump;
        public bool PlayerNoRagdoll { get; private set; } = UserDefaults.NoRagdoll;
        public bool PlayerNeverWanted { get; private set; } = UserDefaults.NeverWanted;
        public bool PlayerIsIgnored { get; private set; } = UserDefaults.EveryoneIgnorePlayer;
        public bool PlayerStayInVehicle { get; private set; } = UserDefaults.PlayerStayInVehicle;
        public bool PlayerFrozen { get; private set; } = false;
        private Menu CustomDrivingStyleMenu = new Menu("Driving Style", "Custom Driving Style");

        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            #region create menu and menu items
            // Create the menu.
            menu = new Menu(Game.Player.Name, "Options du joueur");

            // Create all checkboxes.
            MenuCheckboxItem playerGodModeCheckbox = new MenuCheckboxItem("Godmode", "Ça vous rend invincible.", PlayerGodMode);
            MenuCheckboxItem invisibleCheckbox = new MenuCheckboxItem("Invisible", "Vous rend invisible à vous-même et aux autres.", PlayerInvisible);
            MenuCheckboxItem unlimitedStaminaCheckbox = new MenuCheckboxItem("Endurance illimitée", "Permet de courir éternellement sans ralentir ni subir de dégâts.", PlayerStamina);
            MenuCheckboxItem fastRunCheckbox = new MenuCheckboxItem("Course rapide", "Obtiens les pouvoirs de ~g~Serpent~s et cours très vite !", PlayerFastRun);
            SetRunSprintMultiplierForPlayer(Game.Player.Handle, (PlayerFastRun && IsAllowed(Permission.POFastRun) ? 1.49f : 1f));
            MenuCheckboxItem fastSwimCheckbox = new MenuCheckboxItem("Nage rapide", "Utilisez les pouvoirs de ~g~Serpent 2.0~s et nagez super vite !", PlayerFastSwim);
            SetSwimMultiplierForPlayer(Game.Player.Handle, (PlayerFastSwim && IsAllowed(Permission.POFastSwim) ? 1.49f : 1f));
            MenuCheckboxItem superJumpCheckbox = new MenuCheckboxItem("Super Saut", "Obtenez les pouvoirs de ~g~Serpent 3.0~s et sautez comme un champion !", PlayerSuperJump);
            MenuCheckboxItem noRagdollCheckbox = new MenuCheckboxItem("Aucun Ragdoll", "Désactive le ragdoll du joueur, fait que vous ne tombez plus de votre vélo.", PlayerNoRagdoll);
            MenuCheckboxItem neverWantedCheckbox = new MenuCheckboxItem("Jamais recherché", "Désactive tous les niveaux de recherche.", PlayerNeverWanted);
            MenuCheckboxItem everyoneIgnoresPlayerCheckbox = new MenuCheckboxItem("Tout le monde ignore le joueur", "Tout le monde vous laissera tranquille.", PlayerIsIgnored);
            MenuCheckboxItem playerStayInVehicleCheckbox = new MenuCheckboxItem("Rester dans le véhicule", "Lorsque cette option est activée, les PNJ ne pourront pas vous traîner hors de votre véhicule s'ils se mettent en colère contre vous.", PlayerStayInVehicle);
            MenuCheckboxItem playerFrozenCheckbox = new MenuCheckboxItem("Geler le joueur", "Gèle votre position actuelle.", PlayerFrozen);

            // Wanted level options
            List<string> wantedLevelList = new List<string> { "Non recherché", "1", "2", "3", "4", "5" };
            MenuListItem setWantedLevel = new MenuListItem("Définir le niveau souhaité", wantedLevelList, GetPlayerWantedLevel(Game.Player.Handle), "Définissez votre niveau souhaité en sélectionnant une valeur et en appuyant sur la touche Entrée.");
            MenuListItem setArmorItem = new MenuListItem("Définir le type d'armure", new List<string> { "Pas d'armure", GetLabelText("WT_BA_0"), GetLabelText("WT_BA_1"), GetLabelText("WT_BA_2"), GetLabelText("WT_BA_3"), GetLabelText("WT_BA_4"), }, 0, "Définissez le niveau/type d'armure de votre joueur.");

            MenuItem healPlayerBtn = new MenuItem("Soigner le joueur", "Donnez au joueur une santé maximale.");
            MenuItem cleanPlayerBtn = new MenuItem("Nettoyer les vêtements du joueur", "Nettoyez les vêtements du joueur.");
            MenuItem dryPlayerBtn = new MenuItem("Vêtements de joueur à sec", "Faites sécher les vêtements du joueur.");
            MenuItem wetPlayerBtn = new MenuItem("Vêtements de joueur mouillés", "Mouillez les vêtements du joueur.");
            MenuItem suicidePlayerBtn = new MenuItem("~r~Se suicider", "Tuez-vous en prenant la pilule. Ou en utilisant un pistolet si vous en avez un.");

            Menu vehicleAutoPilot = new Menu("Pilote automatique", "Options de pilotage automatique du véhicule.");

            MenuController.AddSubmenu(menu, vehicleAutoPilot);

            MenuItem vehicleAutoPilotBtn = new MenuItem("Menu du pilote automatique du véhicule", "Gérer les options de pilotage automatique des véhicules.")
            {
                Label = "→→→"
            };

            List<string> drivingStyles = new List<string>() { "Normal", "Pressé", "Éviter les autoroutes", "Conduire en marche arrière", "Custom" };
            MenuListItem drivingStyle = new MenuListItem("Style de conduite", drivingStyles, 0, "Définissez le style de conduite utilisé pour les fonctions Conduire vers un point de passage et Conduire de façon aléatoire.");

            // Scenarios (list can be found in the PedScenarios class)
            MenuListItem playerScenarios = new MenuListItem("Scénarios", PedScenarios.Scenarios, 0, "Sélectionnez un scénario et appuyez sur la touche Entrée pour le lancer. La sélection d'un autre scénario remplacera le scénario en cours. Si vous êtes déjà en train de jouer le scénario sélectionné, le sélectionner à nouveau arrêtera le scénario.");
            MenuItem stopScenario = new MenuItem("Arrêt forcé du scénario", "Cela forcera un scénario à s'arrêter immédiatement, sans attendre qu'il termine son animation d'arrêt.");
            #endregion

            #region add items to menu based on permissions
            // Add all checkboxes to the menu. (keeping permissions in mind)
            if (IsAllowed(Permission.POGod))
            {
                menu.AddMenuItem(playerGodModeCheckbox);
            }
            if (IsAllowed(Permission.POInvisible))
            {
                menu.AddMenuItem(invisibleCheckbox);
            }
            if (IsAllowed(Permission.POUnlimitedStamina))
            {
                menu.AddMenuItem(unlimitedStaminaCheckbox);
            }
            if (IsAllowed(Permission.POFastRun))
            {
                menu.AddMenuItem(fastRunCheckbox);
            }
            if (IsAllowed(Permission.POFastSwim))
            {
                menu.AddMenuItem(fastSwimCheckbox);
            }
            if (IsAllowed(Permission.POSuperjump))
            {
                menu.AddMenuItem(superJumpCheckbox);
            }
            if (IsAllowed(Permission.PONoRagdoll))
            {
                menu.AddMenuItem(noRagdollCheckbox);
            }
            if (IsAllowed(Permission.PONeverWanted))
            {
                menu.AddMenuItem(neverWantedCheckbox);
            }
            if (IsAllowed(Permission.POSetWanted))
            {
                menu.AddMenuItem(setWantedLevel);
            }
            if (IsAllowed(Permission.POIgnored))
            {
                menu.AddMenuItem(everyoneIgnoresPlayerCheckbox);
            }
            if (IsAllowed(Permission.POStayInVehicle))
            {
                menu.AddMenuItem(playerStayInVehicleCheckbox);
            }
            if (IsAllowed(Permission.POMaxHealth))
            {
                menu.AddMenuItem(healPlayerBtn);
            }
            if (IsAllowed(Permission.POMaxArmor))
            {
                menu.AddMenuItem(setArmorItem);
            }
            if (IsAllowed(Permission.POCleanPlayer))
            {
                menu.AddMenuItem(cleanPlayerBtn);
            }
            if (IsAllowed(Permission.PODryPlayer))
            {
                menu.AddMenuItem(dryPlayerBtn);
            }
            if (IsAllowed(Permission.POWetPlayer))
            {
                menu.AddMenuItem(wetPlayerBtn);
            }

            menu.AddMenuItem(suicidePlayerBtn);

            if (IsAllowed(Permission.POVehicleAutoPilotMenu))
            {
                menu.AddMenuItem(vehicleAutoPilotBtn);
                MenuController.BindMenuItem(menu, vehicleAutoPilot, vehicleAutoPilotBtn);

                vehicleAutoPilot.AddMenuItem(drivingStyle);

                MenuItem startDrivingWaypoint = new MenuItem("Drive To Waypoint", "Make your player ped drive your vehicle to your waypoint.");
                MenuItem startDrivingRandomly = new MenuItem("Drive Around Randomly", "Make your player ped drive your vehicle randomly around the map.");
                MenuItem stopDriving = new MenuItem("Stop Driving", "The player ped will find a suitable place to stop the vehicle. The task will be stopped once the vehicle has reached the suitable stop location.");
                MenuItem forceStopDriving = new MenuItem("Force Stop Driving", "This will stop the driving task immediately without finding a suitable place to stop.");
                MenuItem customDrivingStyle = new MenuItem("Custom Driving Style", "Select a custom driving style. Make sure to also enable it by selecting the 'Custom' driving style in the driving styles list.") { Label = "→→→" };
                MenuController.AddSubmenu(vehicleAutoPilot, CustomDrivingStyleMenu);
                vehicleAutoPilot.AddMenuItem(customDrivingStyle);
                MenuController.BindMenuItem(vehicleAutoPilot, CustomDrivingStyleMenu, customDrivingStyle);
                Dictionary<int, string> knownNames = new Dictionary<int, string>()
                {
                    { 0, "Stop before vehicles" },
                    { 1, "Stop before peds" },
                    { 2, "Avoid vehicles" },
                    { 3, "Avoid empty vehicles" },
                    { 4, "Avoid peds" },
                    { 5, "Avoid objects" },

                    { 7, "Stop at traffic lights" },
                    { 8, "Use blinkers" },
                    { 9, "Allow going wrong way" },
                    { 10, "Go in reverse gear" },

                    { 18, "Use shortest path" },

                    { 22, "Ignore roads" },

                    { 24, "Ignore all pathing" },

                    { 29, "Avoid highways (if possible)" },
                };
                for (var i = 0; i < 31; i++)
                {
                    string name = "~r~Unknown Flag";
                    if (knownNames.ContainsKey(i))
                    {
                        name = knownNames[i];
                    }
                    MenuCheckboxItem checkbox = new MenuCheckboxItem(name, "Toggle this driving style flag.", false);
                    CustomDrivingStyleMenu.AddMenuItem(checkbox);
                }
                CustomDrivingStyleMenu.OnCheckboxChange += (sender, item, index, _checked) =>
                {
                    int style = GetStyleFromIndex(drivingStyle.ListIndex);
                    CustomDrivingStyleMenu.MenuSubtitle = $"custom style: {style}";
                    if (drivingStyle.ListIndex == 4)
                    {
                        Notify.Custom("Driving style updated.");
                        SetDriveTaskDrivingStyle(Game.PlayerPed.Handle, style);
                    }
                    else
                    {
                        Notify.Custom("Driving style NOT updated because you haven't enabled the Custom driving style in the previous menu.");
                    }
                };

                vehicleAutoPilot.AddMenuItem(startDrivingWaypoint);
                vehicleAutoPilot.AddMenuItem(startDrivingRandomly);
                vehicleAutoPilot.AddMenuItem(stopDriving);
                vehicleAutoPilot.AddMenuItem(forceStopDriving);

                vehicleAutoPilot.RefreshIndex();

                vehicleAutoPilot.OnItemSelect += async (sender, item, index) =>
                {
                    if (Game.PlayerPed.IsInVehicle() && item != stopDriving && item != forceStopDriving)
                    {
                        if (Game.PlayerPed.CurrentVehicle != null && Game.PlayerPed.CurrentVehicle.Exists() && !Game.PlayerPed.CurrentVehicle.IsDead && Game.PlayerPed.CurrentVehicle.IsDriveable)
                        {
                            if (Game.PlayerPed.CurrentVehicle.Driver == Game.PlayerPed)
                            {
                                if (item == startDrivingWaypoint)
                                {
                                    if (IsWaypointActive())
                                    {
                                        int style = GetStyleFromIndex(drivingStyle.ListIndex);
                                        DriveToWp(style);
                                        Notify.Info("Votre Ped conduit maintenant le véhicule à votre place. Vous pouvez annuler à tout moment en appuyant sur le bouton Arrêter la conduite. Le véhicule s'arrêtera lorsqu'il aura atteint sa destination.");
                                    }
                                    else
                                    {
                                        Notify.Error("Vous avez besoin d'un waypoint avant de pouvoir vous y rendre !");
                                    }

                                }
                                else if (item == startDrivingRandomly)
                                {
                                    int style = GetStyleFromIndex(drivingStyle.ListIndex);
                                    DriveWander(style);
                                    Notify.Info("Votre Ped conduit maintenant le véhicule à votre place. Vous pouvez annuler à tout moment en appuyant sur le bouton 'Arrêtez de conduire'.");
                                }
                            }
                            else
                            {
                                Notify.Error("Vous devez être le conducteur de ce véhicule !");
                            }
                        }
                        else
                        {
                            Notify.Error("Votre véhicule est en panne ou il n'existe pas !");
                        }
                    }
                    else if (item != stopDriving && item != forceStopDriving)
                    {
                        Notify.Error("Vous devez d'abord être dans un véhicule !");
                    }
                    if (item == stopDriving)
                    {
                        if (Game.PlayerPed.IsInVehicle())
                        {
                            Vehicle veh = GetVehicle();
                            if (veh != null && veh.Exists() && !veh.IsDead)
                            {
                                Vector3 outPos = new Vector3();
                                if (GetNthClosestVehicleNode(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, 3, ref outPos, 0, 0, 0))
                                {
                                    Notify.Info("Le Ped du joueur trouvera un endroit approprié pour garer la voiture et s'arrêtera ensuite de conduire. Veuillez patienter.");
                                    ClearPedTasks(Game.PlayerPed.Handle);
                                    TaskVehiclePark(Game.PlayerPed.Handle, veh.Handle, outPos.X, outPos.Y, outPos.Z, Game.PlayerPed.Heading, 3, 60f, true);
                                    while (Game.PlayerPed.Position.DistanceToSquared2D(outPos) > 3f)
                                    {
                                        await BaseScript.Delay(0);
                                    }
                                    SetVehicleHalt(veh.Handle, 3f, 0, false);
                                    ClearPedTasks(Game.PlayerPed.Handle);
                                    Notify.Info("Le ped a arrêté de conduire.");
                                }
                            }
                        }
                        else
                        {
                            ClearPedTasks(Game.PlayerPed.Handle);
                            Notify.Alert("Votre Ped n'est pas dans un véhicule.");
                        }
                    }
                    else if (item == forceStopDriving)
                    {
                        ClearPedTasks(Game.PlayerPed.Handle);
                        Notify.Info("Conduite automatique annulée.");
                    }
                };

                vehicleAutoPilot.OnListItemSelect += (sender, item, listIndex, itemIndex) =>
                {
                    if (item == drivingStyle)
                    {
                        int style = GetStyleFromIndex(listIndex);
                        SetDriveTaskDrivingStyle(Game.PlayerPed.Handle, style);
                        Notify.Info($"Le style de conduite est maintenant réglé sur : ~r~{drivingStyles[listIndex]}~s~.");
                    }
                };
            }

            if (IsAllowed(Permission.POFreeze))
            {
                menu.AddMenuItem(playerFrozenCheckbox);
            }
            if (IsAllowed(Permission.POScenarios))
            {
                menu.AddMenuItem(playerScenarios);
                menu.AddMenuItem(stopScenario);
            }
            #endregion

            #region handle all events
            // Checkbox changes.
            menu.OnCheckboxChange += (sender, item, itemIndex, _checked) =>
            {
                // God Mode toggled.
                if (item == playerGodModeCheckbox)
                {
                    PlayerGodMode = _checked;
                }
                // Invisibility toggled.
                else if (item == invisibleCheckbox)
                {
                    PlayerInvisible = _checked;
                    SetEntityVisible(Game.PlayerPed.Handle, !PlayerInvisible, false);
                }
                // Unlimited Stamina toggled.
                else if (item == unlimitedStaminaCheckbox)
                {
                    PlayerStamina = _checked;
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), _checked ? 100 : 0, true);
                }
                // Fast run toggled.
                else if (item == fastRunCheckbox)
                {
                    PlayerFastRun = _checked;
                    SetRunSprintMultiplierForPlayer(Game.Player.Handle, (_checked ? 1.49f : 1f));
                }
                // Fast swim toggled.
                else if (item == fastSwimCheckbox)
                {
                    PlayerFastSwim = _checked;
                    SetSwimMultiplierForPlayer(Game.Player.Handle, (_checked ? 1.49f : 1f));
                }
                // Super jump toggled.
                else if (item == superJumpCheckbox)
                {
                    PlayerSuperJump = _checked;
                }
                // No ragdoll toggled.
                else if (item == noRagdollCheckbox)
                {
                    PlayerNoRagdoll = _checked;
                }
                // Never wanted toggled.
                else if (item == neverWantedCheckbox)
                {
                    PlayerNeverWanted = _checked;
                    if (!_checked)
                    {
                        SetMaxWantedLevel(5);
                    }
                    else
                    {
                        SetMaxWantedLevel(0);
                    }
                }
                // Everyone ignores player toggled.
                else if (item == everyoneIgnoresPlayerCheckbox)
                {
                    PlayerIsIgnored = _checked;

                    // Manage player is ignored by everyone.
                    SetEveryoneIgnorePlayer(Game.Player.Handle, PlayerIsIgnored);
                    SetPoliceIgnorePlayer(Game.Player.Handle, PlayerIsIgnored);
                    SetPlayerCanBeHassledByGangs(Game.Player.Handle, !PlayerIsIgnored);
                }
                else if (item == playerStayInVehicleCheckbox)
                {
                    PlayerStayInVehicle = _checked;
                }
                // Freeze player toggled.
                else if (item == playerFrozenCheckbox)
                {
                    PlayerFrozen = _checked;

                    if (!MainMenu.NoClipEnabled)
                    {
                        FreezeEntityPosition(Game.PlayerPed.Handle, PlayerFrozen);
                    }
                    else if (!MainMenu.NoClipEnabled)
                    {
                        FreezeEntityPosition(Game.PlayerPed.Handle, PlayerFrozen);
                    }
                }
            };

            // List selections
            menu.OnListItemSelect += (sender, listItem, listIndex, itemIndex) =>
            {
                // Set wanted Level
                if (listItem == setWantedLevel)
                {
                    SetPlayerWantedLevel(Game.Player.Handle, listIndex, false);
                    SetPlayerWantedLevelNow(Game.Player.Handle, false);
                }
                // Player Scenarios 
                else if (listItem == playerScenarios)
                {
                    PlayScenario(PedScenarios.ScenarioNames[PedScenarios.Scenarios[listIndex]]);
                }
                else if (listItem == setArmorItem)
                {
                    Game.PlayerPed.Armor = (listItem.ListIndex) * 20;
                }
            };

            // button presses
            menu.OnItemSelect += (sender, item, index) =>
            {
                // Force Stop Scenario button
                if (item == stopScenario)
                {
                    // Play a new scenario named "forcestop" (this scenario doesn't exist, but the "Play" function checks
                    // for the string "forcestop", if that's provided as th scenario name then it will forcefully clear the player task.
                    PlayScenario("forcestop");
                }
                else if (item == healPlayerBtn)
                {
                    Game.PlayerPed.Health = Game.PlayerPed.MaxHealth;
                    Notify.Success("Joueur soigné.");
                }
                else if (item == cleanPlayerBtn)
                {
                    Game.PlayerPed.ClearBloodDamage();
                    Notify.Success("Les vêtements des joueurs ont été nettoyés.");
                }
                else if (item == dryPlayerBtn)
                {
                    Game.PlayerPed.WetnessHeight = 0f;
                    Notify.Success("Le joueur est maintenant sec.");
                }
                else if (item == wetPlayerBtn)
                {
                    Game.PlayerPed.WetnessHeight = 2f;
                    Notify.Success("Le joueur est maintenant mouillé.");
                }
                else if (item == suicidePlayerBtn)
                {
                    CommitSuicide();
                }
            };
            #endregion

        }

        private int GetCustomDrivingStyle()
        {
            var items = CustomDrivingStyleMenu.GetMenuItems();
            var flags = new int[items.Count];
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item is MenuCheckboxItem checkbox)
                {
                    flags[i] = checkbox.Checked ? 1 : 0;
                }
            }
            string binaryString = "";
            var reverseFlags = flags.Reverse();
            foreach (int i in reverseFlags)
            {
                binaryString += i;
            }
            var binaryNumber = Convert.ToUInt32(binaryString, 2);
            return (int)binaryNumber;
        }

        private int GetStyleFromIndex(int index)
        {
            int style;
            switch (index)
            {
                case 0:
                    style = 443; // normal
                    break;
                case 1:
                    style = 575; // rushed
                    break;
                case 2:
                    style = 536871355; // Avoid highways
                    break;
                case 3:
                    style = 1467; // Go in reverse
                    break;
                case 4:
                    style = GetCustomDrivingStyle(); // custom driving style;
                    break;
                default:
                    style = 0; // no style (impossible, but oh well)
                    break;
            }
            return style;
        }

        /// <summary>
        /// Checks if the menu exists, if not then it creates it first.
        /// Then returns the menu.
        /// </summary>
        /// <returns>The Player Options Menu</returns>
        public Menu GetMenu()
        {
            if (menu == null)
            {
                CreateMenu();
            }
            return menu;
        }

    }
}
