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
    public class BannedPlayers
    {
        // Variables
        private Menu menu;

        /// <summary>
        /// Struct used to store bans.
        /// </summary>
        public struct BanRecord
        {
            public string playerName;
            public List<string> identifiers;
            public DateTime bannedUntil;
            public string banReason;
            public string bannedBy;
            public string uuid;
        }

        BanRecord currentRecord = new BanRecord();

        public List<BanRecord> banlist = new List<BanRecord>();

        Menu bannedPlayer = new Menu("Joueur bannis", "Ban Record: ");

        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            menu = new Menu(Game.Player.Name, "Gestion des joueurs bannis");

            menu.InstructionalButtons.Add(Control.Jump, "Options de filtrage");
            menu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.Jump, Menu.ControlPressCheckType.JUST_RELEASED, new Action<Menu, Control>(async (a, b) =>
            {
                if (banlist.Count > 1)
                {
                    string filterText = await GetUserInput("Nom d'utilisateur ou ID de bannissement du filtre (laissez ce champ vide pour réinitialiser le filtre).");
                    if (string.IsNullOrEmpty(filterText))
                    {
                        Subtitle.Custom("Les filtres ont été effacés.");
                        menu.ResetFilter();
                        UpdateBans();
                    }
                    else
                    {
                        menu.FilterMenuItems(item => item.ItemData is BanRecord br && (br.playerName.ToLower().Contains(filterText.ToLower()) || br.uuid.ToLower().Contains(filterText.ToLower())));
                        Subtitle.Custom("Le filtre a été appliqué.");
                    }
                }
                else
                {
                    Notify.Error("Il faut qu'au moins 2 joueurs soient bannis pour pouvoir utiliser la fonction de filtrage.");
                }

                Log($"Button pressed: {a} {b}");
            }), true));

            bannedPlayer.AddMenuItem(new MenuItem("Nom du joueur"));
            bannedPlayer.AddMenuItem(new MenuItem("Banni par"));
            bannedPlayer.AddMenuItem(new MenuItem("Interdit jusqu'à"));
            bannedPlayer.AddMenuItem(new MenuItem("Identifiants du joueur"));
            bannedPlayer.AddMenuItem(new MenuItem("Banni pour"));
            bannedPlayer.AddMenuItem(new MenuItem("~r~Unban", "~r~Attention, le bannissement d'un joueur ne peut pas être annulé. Vous ne pourrez pas le bannir à nouveau avant qu'il ne rejoigne le serveur. Êtes-vous absolument sûr de vouloir débannir ce joueur ? ~s~Tip: Les joueurs bannis temporairement seront automatiquement débanalisés s'ils se connectent au serveur après l'expiration de leur date de bannissement."));

            // should be enough for now to cover all possible identifiers.
            List<string> colors = new List<string>() { "~r~", "~g~", "~b~", "~o~", "~y~", "~p~", "~s~", "~t~", };

            bannedPlayer.OnMenuClose += (sender) =>
            {
                BaseScript.TriggerServerEvent("vMenu:RequestBanList", Game.Player.Handle);
                bannedPlayer.GetMenuItems()[5].Label = "";
                UpdateBans();
            };

            bannedPlayer.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) =>
            {
                bannedPlayer.GetMenuItems()[5].Label = "";
            };

            bannedPlayer.OnItemSelect += (sender, item, index) =>
            {
                if (index == 5 && IsAllowed(Permission.OPUnban))
                {
                    if (item.Label == "Etes-vous sûr ?")
                    {
                        if (banlist.Contains(currentRecord))
                        {
                            UnbanPlayer(banlist.IndexOf(currentRecord));
                            bannedPlayer.GetMenuItems()[5].Label = "";
                            bannedPlayer.GoBack();
                        }
                        else
                        {
                            Notify.Error("D'une manière ou d'une autre, vous avez réussi à cliquer sur le bouton de débannissement, mais l'enregistrement de bannissement que vous consultez apparemment n'existe même pas. C'est bizarre...");
                        }
                    }
                    else
                    {
                        item.Label = "Etes-vous sûr ?";
                    }
                }
                else
                {
                    bannedPlayer.GetMenuItems()[5].Label = "";
                }

            };

            menu.OnItemSelect += (sender, item, index) =>
            {
                currentRecord = item.ItemData;

                bannedPlayer.MenuSubtitle = "Ban Record: ~y~" + currentRecord.playerName;
                var nameItem = bannedPlayer.GetMenuItems()[0];
                var bannedByItem = bannedPlayer.GetMenuItems()[1];
                var bannedUntilItem = bannedPlayer.GetMenuItems()[2];
                var playerIdentifiersItem = bannedPlayer.GetMenuItems()[3];
                var banReasonItem = bannedPlayer.GetMenuItems()[4];
                nameItem.Label = currentRecord.playerName;
                nameItem.Description = "Nom du joueur : ~y~" + currentRecord.playerName;
                bannedByItem.Label = currentRecord.bannedBy;
                bannedByItem.Description = "Joueur banni par : ~y~" + currentRecord.bannedBy;
                if (currentRecord.bannedUntil.Date.Year == 3000)
                    bannedUntilItem.Label = "Permanent";
                else
                    bannedUntilItem.Label = currentRecord.bannedUntil.Date.ToString();
                bannedUntilItem.Description = "Ce joueur est banni jusqu'à : " + currentRecord.bannedUntil.Date.ToString();
                playerIdentifiersItem.Description = "";

                int i = 0;
                foreach (string id in currentRecord.identifiers)
                {
                    // only (admins) people that can unban players are allowed to view IP's.
                    // this is just a slight 'safety' feature in case someone who doesn't know what they're doing
                    // gave builtin.everyone access to view the banlist.
                    if (id.StartsWith("ip:") && !IsAllowed(Permission.OPUnban))
                    {
                        playerIdentifiersItem.Description += $"{colors[i]}ip: (hidden) ";
                    }
                    else
                    {
                        playerIdentifiersItem.Description += $"{colors[i]}{id.Replace(":", ": ")} ";
                    }
                    i++;
                }
                banReasonItem.Description = "Banni pour : " + currentRecord.banReason;

                var unbanPlayerBtn = bannedPlayer.GetMenuItems()[5];
                unbanPlayerBtn.Label = "";
                if (!IsAllowed(Permission.OPUnban))
                {
                    unbanPlayerBtn.Enabled = false;
                    unbanPlayerBtn.Description = "Vous n'êtes pas autorisé à débannir des joueurs. Vous pouvez uniquement consulter leur dossier de bannissement.";
                    unbanPlayerBtn.LeftIcon = MenuItem.Icon.LOCK;
                }

                bannedPlayer.RefreshIndex();
            };
            MenuController.AddMenu(bannedPlayer);

        }

        /// <summary>
        /// Updates the ban list menu.
        /// </summary>
        public void UpdateBans()
        {
            menu.ResetFilter();
            menu.ClearMenuItems();

            foreach (BanRecord ban in banlist)
            {
                MenuItem recordBtn = new MenuItem(ban.playerName, $"~y~{ban.playerName}~s~ a été banni par ~y~{ban.bannedBy}~s~ jusqu'à ~y~{ban.bannedUntil}~s~ pour ~y~{ban.banReason}~s~.")
                {
                    Label = "→→→",
                    ItemData = ban
                };
                menu.AddMenuItem(recordBtn);
                MenuController.BindMenuItem(menu, bannedPlayer, recordBtn);
            }
            menu.RefreshIndex();
        }

        /// <summary>
        /// Updates the list of ban records.
        /// </summary>
        /// <param name="banJsonString"></param>
        public void UpdateBanList(string banJsonString)
        {
            banlist.Clear();
            banlist = JsonConvert.DeserializeObject<List<BanRecord>>(banJsonString);
            UpdateBans();
        }

        /// <summary>
        /// Create the menu if it doesn't exist, and then returns it.
        /// </summary>
        /// <returns>The Menu</returns>
        public Menu GetMenu()
        {
            if (menu == null)
            {
                CreateMenu();
            }

            return menu;
        }

        /// <summary>
        /// Sends an event to the server requesting the player to be unbanned.
        /// We'll just assume that worked fine, so remove the item from our local list, we'll re-sync once the menu is re-opened.
        /// </summary>
        /// <param name="index"></param>
        private void UnbanPlayer(int index)
        {
            BanRecord record = banlist[index];
            banlist.Remove(record);
            BaseScript.TriggerServerEvent("vMenu:RequestPlayerUnban", record.uuid);
        }

        /// <summary>
        /// Converts the ban record (json object) into a BanRecord struct.
        /// </summary>
        /// <param name="banRecordJsonObject"></param>
        /// <returns></returns>
        public static BanRecord JsonToBanRecord(dynamic banRecordJsonObject)
        {
            var newBr = new BanRecord();
            foreach (Newtonsoft.Json.Linq.JProperty brValue in banRecordJsonObject)
            {
                string key = brValue.Name.ToString();
                var value = brValue.Value;
                if (key == "playerName")
                {
                    newBr.playerName = value.ToString();
                }
                else if (key == "identifiers")
                {
                    var tmpList = new List<string>();
                    foreach (string identifier in value)
                    {
                        tmpList.Add(identifier);
                    }
                    newBr.identifiers = tmpList;
                }
                else if (key == "bannedUntil")
                {
                    newBr.bannedUntil = DateTime.Parse(value.ToString());
                }
                else if (key == "banReason")
                {
                    newBr.banReason = value.ToString();
                }
                else if (key == "bannedBy")
                {
                    newBr.bannedBy = value.ToString();
                }
            }
            return newBr;
        }
    }
}
