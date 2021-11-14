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
    public class OnlinePlayers
    {
        public List<int> PlayersWaypointList = new List<int>();

        // Menu variable, will be defined in CreateMenu()
        private Menu menu;

        Menu playerMenu = new Menu("Joueurs en ligne", "Joueur :");
        IPlayer currentPlayer = new NativePlayer(Game.Player);


        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            // Create the menu.
            menu = new Menu(Game.Player.Name, "Joueurs en ligne") { };
            menu.CounterPreText = "Joueurs: ";

            MenuController.AddSubmenu(menu, playerMenu);

            MenuItem sendMessage = new MenuItem("Envoyer un message privé", "Envoie un message privé à ce joueur.");
            MenuItem teleport = new MenuItem("Téléportation vers le joueur", "Téléportation vers ce joueur.");
            MenuItem teleportVeh = new MenuItem("Téléportation dans le véhicule du joueur", "Se téléporter dans le véhicule du joueur.");
            MenuItem summon = new MenuItem("Convoquer le joueur", "Téléportez le joueur vers vous.");
            MenuItem toggleGPS = new MenuItem("Activer/Désactiver le GPS", "Active ou désactive l'itinéraire GPS de votre radar vers ce joueur. ");
            MenuItem spectate = new MenuItem("Observer le joueur", "Observer ce joueur. Cliquez à nouveau sur ce bouton pour arrêter de le regarder.");
            MenuItem printIdentifiers = new MenuItem("Afficher les identifiants", "Cela imprimera les identifiants du joueur dans la console du client (F8). Et aussi le sauvegarder dans le fichier CitizenFX.log.");
            MenuItem kill = new MenuItem("~r~Tuer le joueur", "Tuez ce joueur, notez qu'il recevra une notification indiquant que vous l'avez tué.");
            MenuItem kick = new MenuItem("~r~Expulser le joueur", "Kick the player from the server.");
            MenuItem ban = new MenuItem("~r~Bannir le joueur de façon permanente", "Bannir ce joueur définitivement du serveur. Êtes-vous sûr de vouloir faire cela ? Vous pouvez spécifier la raison du bannissement après avoir cliqué sur ce bouton.");
            MenuItem tempban = new MenuItem("~r~Bannir temporairement le joueur", "Donnez à ce joueur un bannissement temporaire de 30 jours (maximum). Vous pouvez spécifier la durée et la raison du bannissement après avoir cliqué sur ce bouton.");

            // always allowed
            playerMenu.AddMenuItem(sendMessage);
            // permissions specific
            if (IsAllowed(Permission.OPTeleport))
            {
                playerMenu.AddMenuItem(teleport);
                playerMenu.AddMenuItem(teleportVeh);
            }
            if (IsAllowed(Permission.OPSummon))
            {
                playerMenu.AddMenuItem(summon);
            }
            if (IsAllowed(Permission.OPSpectate))
            {
                playerMenu.AddMenuItem(spectate);
            }
            if (IsAllowed(Permission.OPWaypoint))
            {
                playerMenu.AddMenuItem(toggleGPS);
            }
            if (IsAllowed(Permission.OPIdentifiers))
            {
                playerMenu.AddMenuItem(printIdentifiers);
            }
            if (IsAllowed(Permission.OPKill))
            {
                playerMenu.AddMenuItem(kill);
            }
            if (IsAllowed(Permission.OPKick))
            {
                playerMenu.AddMenuItem(kick);
            }
            if (IsAllowed(Permission.OPTempBan))
            {
                playerMenu.AddMenuItem(tempban);
            }
            if (IsAllowed(Permission.OPPermBan))
            {
                playerMenu.AddMenuItem(ban);
                ban.LeftIcon = MenuItem.Icon.WARNING;
            }

            playerMenu.OnMenuClose += (sender) =>
            {
                playerMenu.RefreshIndex();
                ban.Label = "";
            };

            playerMenu.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) =>
            {
                ban.Label = "";
            };

            // handle button presses for the specific player's menu.
            playerMenu.OnItemSelect += async (sender, item, index) =>
            {
                // send message
                if (item == sendMessage)
                {
                    if (MainMenu.MiscSettingsMenu != null && !MainMenu.MiscSettingsMenu.MiscDisablePrivateMessages)
                    {
                        string message = await GetUserInput($"Message privé à {currentPlayer.Name}", 200);
                        if (string.IsNullOrEmpty(message))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                        }
                        else
                        {
                            TriggerServerEvent("vMenu:SendMessageToPlayer", currentPlayer.ServerId, message);
                            PrivateMessage(currentPlayer.ServerId.ToString(), message, true);
                        }
                    }
                    else
                    {
                        Notify.Error("Vous ne pouvez pas envoyer de message privé si vous avez vous-même désactivé les messages privés. Activez-les dans le menu 'Paramètres divers' et réessayez.");
                    }

                }
                // teleport (in vehicle) button
                else if (item == teleport || item == teleportVeh)
                {
                    if (!currentPlayer.IsLocal)
                        _ = TeleportToPlayer(currentPlayer, item == teleportVeh); // teleport to the player. optionally in the player's vehicle if that button was pressed.
                    else
                        Notify.Error("Vous ne pouvez pas vous téléporter à vous-même !");
                }
                // summon button
                else if (item == summon)
                {
                    if (Game.Player.Handle != currentPlayer.Handle)
                        SummonPlayer(currentPlayer);
                    else
                        Notify.Error("Vous ne pouvez pas vous convoquer vous-même.");
                }
                // spectating
                else if (item == spectate)
                {
                    SpectatePlayer(currentPlayer);
                }
                // kill button
                else if (item == kill)
                {
                    KillPlayer(currentPlayer);
                }
                // manage the gps route being clicked.
                else if (item == toggleGPS)
                {
                    bool selectedPedRouteAlreadyActive = false;
                    if (PlayersWaypointList.Count > 0)
                    {
                        if (PlayersWaypointList.Contains(currentPlayer.Handle))
                        {
                            selectedPedRouteAlreadyActive = true;
                        }
                        foreach (int playerId in PlayersWaypointList)
                        {
                            int playerPed = GetPlayerPed(playerId);
                            if (DoesEntityExist(playerPed) && DoesBlipExist(GetBlipFromEntity(playerPed)))
                            {
                                int oldBlip = GetBlipFromEntity(playerPed);
                                SetBlipRoute(oldBlip, false);
                                RemoveBlip(ref oldBlip);
                                Notify.Custom($"~g~Itinéraire GPS vers ~s~<C>{GetSafePlayerName(currentPlayer.Name)}</C>~g~ est maintenant désactivé.");
                            }
                        }
                        PlayersWaypointList.Clear();
                    }

                    if (!selectedPedRouteAlreadyActive)
                    {
                        if (currentPlayer.Handle != Game.Player.Handle)
                        {
                            int ped = GetPlayerPed(currentPlayer.Handle);
                            int blip = GetBlipFromEntity(ped);
                            if (DoesBlipExist(blip))
                            {
                                SetBlipColour(blip, 58);
                                SetBlipRouteColour(blip, 58);
                                SetBlipRoute(blip, true);
                            }
                            else
                            {
                                blip = AddBlipForEntity(ped);
                                SetBlipColour(blip, 58);
                                SetBlipRouteColour(blip, 58);
                                SetBlipRoute(blip, true);
                            }
                            PlayersWaypointList.Add(currentPlayer.Handle);
                            Notify.Custom($"~g~Itinéraire GPS vers ~s~<C>{GetSafePlayerName(currentPlayer.Name)}</C>~g~ est maintenant actif, appuyez sur le bouton ~s~Activer/Désactiver le GPS~g~ pour désactiver l'itinéraire.");
                        }
                        else
                        {
                            Notify.Error("Vous ne pouvez pas définir un waypoint pour vous-même.");
                        }
                    }
                }
                else if (item == printIdentifiers)
                {
                    Func<string, string> CallbackFunction = (data) =>
                    {
                        Debug.WriteLine(data);
                        string ids = "~s~";
                        foreach (string s in JsonConvert.DeserializeObject<string[]>(data))
                        {
                            ids += "~n~" + s;
                        }
                        Notify.Custom($"~g~Identifiants de ~y~<C>{GetSafePlayerName(currentPlayer.Name)}</C>~g~: {ids}", false);
                        return data;
                    };
                    BaseScript.TriggerServerEvent("vMenu:GetPlayerIdentifiers", currentPlayer.ServerId, CallbackFunction);
                }
                // kick button
                else if (item == kick)
                {
                    if (currentPlayer.Handle != Game.Player.Handle)
                        KickPlayer(currentPlayer, true);
                    else
                        Notify.Error("Vous ne pouvez pas vous expulser vous-même.");
                }
                // temp ban
                else if (item == tempban)
                {
                    BanPlayer(currentPlayer, false);
                }
                // perm ban
                else if (item == ban)
                {
                    if (ban.Label == "Etes-vous sûr ?")
                    {
                        ban.Label = "";
                        _ = UpdatePlayerlist();
                        playerMenu.GoBack();
                        BanPlayer(currentPlayer, true);
                    }
                    else
                    {
                        ban.Label = "Etes-vous sûr ?";
                    }
                }
            };

            // handle button presses in the player list.
            menu.OnItemSelect += (sender, item, index) =>
                {
                    var baseId = int.Parse(item.Label.Replace(" →→→", "").Replace("Server #", ""));
                    var player = MainMenu.PlayersList.FirstOrDefault(p => p.ServerId == baseId);

                    if (player != null)
                    {
                        currentPlayer = player;
                        playerMenu.MenuSubtitle = $"~s~Joueur: ~y~{GetSafePlayerName(currentPlayer.Name)}";
                        playerMenu.CounterPreText = $"[ID Serveur: ~y~{currentPlayer.ServerId}~s~] ";
                    }
                    else
                    {
                        playerMenu.GoBack();
                    }
                };
        }

        /// <summary>
        /// Updates the player items.
        /// </summary>
        public async Task UpdatePlayerlist()
        {
            void UpdateStuff()
            {
                menu.ClearMenuItems();

                foreach (IPlayer p in MainMenu.PlayersList.OrderBy(a => a.Name))
                {
                    MenuItem pItem = new MenuItem($"{GetSafePlayerName(p.Name)}", $"Cliquez pour afficher les options de ce joueur. ID Serveur: {p.ServerId}. ID Local: {p.Handle}.")
                    {
                        Label = $"Server #{p.ServerId} →→→"
                    };
                    menu.AddMenuItem(pItem);
                    MenuController.BindMenuItem(menu, playerMenu, pItem);
                }

                menu.RefreshIndex();
                //menu.UpdateScaleform();
                playerMenu.RefreshIndex();
                //playerMenu.UpdateScaleform();
            }

            // First, update *before* waiting - so we get all local players.
            UpdateStuff();
            await MainMenu.PlayersList.WaitRequested();

            // Update after waiting too so we have all remote players.
            UpdateStuff();
        }

        /// <summary>
        /// Checks if the menu exists, if not then it creates it first.
        /// Then returns the menu.
        /// </summary>
        /// <returns>The Online Players Menu</returns>
        public Menu GetMenu()
        {
            if (menu == null)
            {
                CreateMenu();
                return menu;
            }
            else
            {
                _ = UpdatePlayerlist();
                return menu;
            }
        }
    }
}
