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
    public class WeaponOptions
    {
        // Variables
        private Menu menu;

        public bool UnlimitedAmmo { get; private set; } = UserDefaults.WeaponsUnlimitedAmmo;
        public bool NoReload { get; private set; } = UserDefaults.WeaponsNoReload;
        public bool AutoEquipChute { get; private set; } = UserDefaults.AutoEquipChute;
        public bool UnlimitedParachutes { get; private set; } = UserDefaults.WeaponsUnlimitedParachutes;

        public static Dictionary<string, uint> AddonWeapons = new Dictionary<string, uint>();

        private Dictionary<Menu, ValidWeapon> weaponInfo;
        private Dictionary<MenuItem, string> weaponComponents;

        #region Create Menu
        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            // Setup weapon dictionaries.
            weaponInfo = new Dictionary<Menu, ValidWeapon>();
            weaponComponents = new Dictionary<MenuItem, string>();

            #region create main weapon options menu and add items
            // Create the menu.
            menu = new Menu(Game.Player.Name, "Options d'armement");

            MenuItem getAllWeapons = new MenuItem("Obtenir toutes les armes", "Obtenir toutes les armes.");
            MenuItem removeAllWeapons = new MenuItem("Retirer toutes les armes", "Supprime toutes les armes de votre inventaire.");
            MenuCheckboxItem unlimitedAmmo = new MenuCheckboxItem("Munitions illimitées", "Approvisionnement illimité en munitions.", UnlimitedAmmo);
            MenuCheckboxItem noReload = new MenuCheckboxItem("Pas de rechargement", "Ne jamais recharger.", NoReload);
            MenuItem setAmmo = new MenuItem("Définir le nombre de munitions", "Définissez la quantité de munitions de toutes vos armes.");
            MenuItem refillMaxAmmo = new MenuItem("Rechargez toutes les munitions", "Donnez à toutes vos armes un maximum de munitions.");
            MenuItem spawnByName = new MenuItem("Se donner une arme par son nom", "Entrez un nom d'arme à se donner.");

            // Add items based on permissions
            if (IsAllowed(Permission.WPGetAll))
            {
                menu.AddMenuItem(getAllWeapons);
            }
            if (IsAllowed(Permission.WPRemoveAll))
            {
                menu.AddMenuItem(removeAllWeapons);
            }
            if (IsAllowed(Permission.WPUnlimitedAmmo))
            {
                menu.AddMenuItem(unlimitedAmmo);
            }
            if (IsAllowed(Permission.WPNoReload))
            {
                menu.AddMenuItem(noReload);
            }
            if (IsAllowed(Permission.WPSetAllAmmo))
            {
                menu.AddMenuItem(setAmmo);
                menu.AddMenuItem(refillMaxAmmo);
            }
            if (IsAllowed(Permission.WPSpawnByName))
            {
                menu.AddMenuItem(spawnByName);
            }
            #endregion

            #region addonweapons submenu
            MenuItem addonWeaponsBtn = new MenuItem("Armes ajoutées", "Équiper / retirer Armes ajoutées disponibles sur ce serveur.");
            Menu addonWeaponsMenu = new Menu("Armes ajoutées", "Équiper/retirer Armes ajoutées");
            menu.AddMenuItem(addonWeaponsBtn);

            #region manage creating and accessing Armes ajoutées menu
            if (IsAllowed(Permission.WPSpawn) && AddonWeapons != null && AddonWeapons.Count > 0)
            {
                MenuController.BindMenuItem(menu, addonWeaponsMenu, addonWeaponsBtn);
                foreach (KeyValuePair<string, uint> weapon in AddonWeapons)
                {
                    string name = weapon.Key.ToString();
                    uint model = weapon.Value;
                    var item = new MenuItem(name, $"Cliquez pour ajouter/supprimer ({name}) vers/depuis votre inventaire.");
                    addonWeaponsMenu.AddMenuItem(item);
                    if (!IsWeaponValid(model))
                    {
                        item.Enabled = false;
                        item.LeftIcon = MenuItem.Icon.LOCK;
                        item.Description = "Ce modèle n'est pas disponible. Veuillez demander au propriétaire du serveur de vérifier qu'il est diffusé correctement.";
                    }
                }
                addonWeaponsMenu.OnItemSelect += (sender, item, index) =>
                {
                    var weapon = AddonWeapons.ElementAt(index);
                    if (HasPedGotWeapon(Game.PlayerPed.Handle, weapon.Value, false))
                    {
                        RemoveWeaponFromPed(Game.PlayerPed.Handle, weapon.Value);
                    }
                    else
                    {
                        var maxAmmo = 200;
                        GetMaxAmmo(Game.PlayerPed.Handle, weapon.Value, ref maxAmmo);
                        GiveWeaponToPed(Game.PlayerPed.Handle, weapon.Value, maxAmmo, false, true);
                    }
                };
                addonWeaponsBtn.Label = "→→→";
            }
            else
            {
                addonWeaponsBtn.LeftIcon = MenuItem.Icon.LOCK;
                addonWeaponsBtn.Enabled = false;
                addonWeaponsBtn.Description = "Cette option n'est pas disponible sur ce serveur car vous n'avez pas la permission de l'utiliser ou elle n'est pas configurée correctement.";
            }
            #endregion
            addonWeaponsMenu.RefreshIndex();
            #endregion

            #region Options de parachute menu

            if (IsAllowed(Permission.WPParachute))
            {
                // main Options de parachute menu setup
                Menu parachuteMenu = new Menu("Options de parachute", "Options de parachute");
                MenuItem parachuteBtn = new MenuItem("Options de parachute", "Toutes les options relatives au parachute peuvent être modifiées ici.") { Label = "→→→" };

                MenuController.AddSubmenu(menu, parachuteMenu);
                menu.AddMenuItem(parachuteBtn);
                MenuController.BindMenuItem(menu, parachuteMenu, parachuteBtn);

                List<string> chutes = new List<string>()
                {
                    GetLabelText("PM_TINT0"),
                    GetLabelText("PM_TINT1"),
                    GetLabelText("PM_TINT2"),
                    GetLabelText("PM_TINT3"),
                    GetLabelText("PM_TINT4"),
                    GetLabelText("PM_TINT5"),
                    GetLabelText("PM_TINT6"),
                    GetLabelText("PM_TINT7"),

                    // broken in FiveM for some weird reason:
                    GetLabelText("PS_CAN_0"),
                    GetLabelText("PS_CAN_1"),
                    GetLabelText("PS_CAN_2"),
                    GetLabelText("PS_CAN_3"),
                    GetLabelText("PS_CAN_4"),
                    GetLabelText("PS_CAN_5")
                };
                List<string> chuteDescriptions = new List<string>()
                {
                    GetLabelText("PD_TINT0"),
                    GetLabelText("PD_TINT1"),
                    GetLabelText("PD_TINT2"),
                    GetLabelText("PD_TINT3"),
                    GetLabelText("PD_TINT4"),
                    GetLabelText("PD_TINT5"),
                    GetLabelText("PD_TINT6"),
                    GetLabelText("PD_TINT7"),

                    // broken in FiveM for some weird reason:
                    GetLabelText("PSD_CAN_0") + " ~r~Pour une raison quelconque, celui-ci ne semble pas fonctionner dans FiveM..",
                    GetLabelText("PSD_CAN_1") + " ~r~Pour une raison quelconque, celui-ci ne semble pas fonctionner dans FiveM..",
                    GetLabelText("PSD_CAN_2") + " ~r~Pour une raison quelconque, celui-ci ne semble pas fonctionner dans FiveM..",
                    GetLabelText("PSD_CAN_3") + " ~r~Pour une raison quelconque, celui-ci ne semble pas fonctionner dans FiveM..",
                    GetLabelText("PSD_CAN_4") + " ~r~Pour une raison quelconque, celui-ci ne semble pas fonctionner dans FiveM..",
                    GetLabelText("PSD_CAN_5") + " ~r~Pour une raison quelconque, celui-ci ne semble pas fonctionner dans FiveM.."
                };

                MenuItem togglePrimary = new MenuItem("Activer/Désactiver Parachute primaire", "Equiper ou retirer le parachute primaire");
                MenuItem toggleReserve = new MenuItem("Activer Parachute de secours", "Active le parachute de secours. Ne fonctionne que si vous avez activé le parachute principal en premier. Le parachute de secours ne peut pas être retiré du joueur une fois qu'il est activé.");
                MenuListItem primaryChutes = new MenuListItem("Chute primaire Style", chutes, 0, $"Chute primaire: {chuteDescriptions[0]}");
                MenuListItem secondaryChutes = new MenuListItem("Reserve Chute Style", chutes, 0, $"Chute de secours: {chuteDescriptions[0]}");
                MenuCheckboxItem unlimitedParachutes = new MenuCheckboxItem("Unlimited Parachutes", "Enable unlimited parachutes and reserve parachutes.", UnlimitedParachutes);
                MenuCheckboxItem autoEquipParachutes = new MenuCheckboxItem("Auto Equip Parachutes", "Automatically equip a parachute and reserve parachute when entering planes/helicopters.", AutoEquipChute);

                // smoke color list
                List<string> smokeColorsList = new List<string>()
                {
                    GetLabelText("PM_TINT8"), // no smoke
                    GetLabelText("PM_TINT9"), // red
                    GetLabelText("PM_TINT10"), // orange
                    GetLabelText("PM_TINT11"), // yellow
                    GetLabelText("PM_TINT12"), // blue
                    GetLabelText("PM_TINT13"), // black
                };
                List<int[]> colors = new List<int[]>()
                {
                    new int[3] { 255, 255, 255 },
                    new int[3] { 255, 0, 0 },
                    new int[3] { 255, 165, 0 },
                    new int[3] { 255, 255, 0 },
                    new int[3] { 0, 0, 255 },
                    new int[3] { 20, 20, 20 },
                };

                MenuListItem smokeColors = new MenuListItem("Couleur de la traînée de fumée", smokeColorsList, 0, "Choisissez une couleur de piste de fumée, puis appuyez sur select pour la changer. Le changement de couleur prend 4 secondes, vous ne pouvez pas utiliser votre fumée pendant que la couleur est en train de changer.");

                parachuteMenu.AddMenuItem(togglePrimary);
                parachuteMenu.AddMenuItem(toggleReserve);
                parachuteMenu.AddMenuItem(autoEquipParachutes);
                parachuteMenu.AddMenuItem(unlimitedParachutes);
                parachuteMenu.AddMenuItem(smokeColors);
                parachuteMenu.AddMenuItem(primaryChutes);
                parachuteMenu.AddMenuItem(secondaryChutes);

                parachuteMenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == togglePrimary)
                    {
                        if (HasPedGotWeapon(Game.PlayerPed.Handle, (uint)GetHashKey("gadget_parachute"), false))
                        {
                            Subtitle.Custom("Parachute primaire enlevé.");
                            RemoveWeaponFromPed(Game.PlayerPed.Handle, (uint)GetHashKey("gadget_parachute"));
                        }
                        else
                        {
                            Subtitle.Custom("Parachute primaire ajouté.");
                            GiveWeaponToPed(Game.PlayerPed.Handle, (uint)GetHashKey("gadget_parachute"), 0, false, false);
                        }
                    }
                    else if (item == toggleReserve)
                    {
                        SetPlayerHasReserveParachute(Game.Player.Handle);
                        Subtitle.Custom("Le parachute de secours a été ajouté.");

                    }
                };

                parachuteMenu.OnCheckboxChange += (sender, item, index, _checked) =>
                {
                    if (item == unlimitedParachutes)
                    {
                        UnlimitedParachutes = _checked;
                    }
                    else if (item == autoEquipParachutes)
                    {
                        AutoEquipChute = _checked;
                    }
                };

                bool switching = false;
                async void IndexChangedEventHandler(Menu sender, MenuListItem item, int oldIndex, int newIndex, int itemIndex)
                {
                    if (item == smokeColors && oldIndex == -1)
                    {
                        if (!switching)
                        {
                            switching = true;
                            SetPlayerCanLeaveParachuteSmokeTrail(Game.Player.Handle, false);
                            await Delay(4000);
                            int[] color = colors[newIndex];
                            SetPlayerParachuteSmokeTrailColor(Game.Player.Handle, color[0], color[1], color[2]);
                            SetPlayerCanLeaveParachuteSmokeTrail(Game.Player.Handle, newIndex != 0);
                            switching = false;
                        }
                    }
                    else if (item == primaryChutes)
                    {
                        item.Description = $"Chute primaire: {chuteDescriptions[newIndex]}";
                        SetPlayerParachuteTintIndex(Game.Player.Handle, newIndex);
                    }
                    else if (item == secondaryChutes)
                    {
                        item.Description = $"Chute de secours: {chuteDescriptions[newIndex]}";
                        SetPlayerReserveParachuteTintIndex(Game.Player.Handle, newIndex);
                    }
                }

                parachuteMenu.OnListItemSelect += (sender, item, index, itemIndex) => IndexChangedEventHandler(sender, item, -1, index, itemIndex);
                parachuteMenu.OnListIndexChange += IndexChangedEventHandler;
            }
            #endregion

            #region Create Weapon Category Submenus
            MenuItem spacer = GetSpacerMenuItem("↓ Catégories d'armes ↓");
            menu.AddMenuItem(spacer);

            Menu handGuns = new Menu("Armes", "Armes de poing");
            MenuItem handGunsBtn = new MenuItem("Armes de poing");

            Menu rifles = new Menu("Armes", "Fusils d'assaut");
            MenuItem riflesBtn = new MenuItem("Fusils d'assaut");

            Menu shotguns = new Menu("Armes", "Fusils à pompe");
            MenuItem shotgunsBtn = new MenuItem("Fusils à pompe");

            Menu smgs = new Menu("Armes", "Mitrailleuses sub-légères");
            MenuItem smgsBtn = new MenuItem("Mitrailleuses sub-légères");

            Menu throwables = new Menu("Armes", "Objets jetables");
            MenuItem throwablesBtn = new MenuItem("Objets jetables");

            Menu melee = new Menu("Armes", "Mêlée");
            MenuItem meleeBtn = new MenuItem("Mêlée");

            Menu heavy = new Menu("Armes", "Armes lourdes");
            MenuItem heavyBtn = new MenuItem("Armes lourdes");

            Menu snipers = new Menu("Armes", "Fusils de sniper");
            MenuItem snipersBtn = new MenuItem("Fusils de sniper");

            MenuController.AddSubmenu(menu, handGuns);
            MenuController.AddSubmenu(menu, rifles);
            MenuController.AddSubmenu(menu, shotguns);
            MenuController.AddSubmenu(menu, smgs);
            MenuController.AddSubmenu(menu, throwables);
            MenuController.AddSubmenu(menu, melee);
            MenuController.AddSubmenu(menu, heavy);
            MenuController.AddSubmenu(menu, snipers);
            #endregion

            #region Setup weapon category buttons and submenus.
            handGunsBtn.Label = "→→→";
            menu.AddMenuItem(handGunsBtn);
            MenuController.BindMenuItem(menu, handGuns, handGunsBtn);

            riflesBtn.Label = "→→→";
            menu.AddMenuItem(riflesBtn);
            MenuController.BindMenuItem(menu, rifles, riflesBtn);

            shotgunsBtn.Label = "→→→";
            menu.AddMenuItem(shotgunsBtn);
            MenuController.BindMenuItem(menu, shotguns, shotgunsBtn);

            smgsBtn.Label = "→→→";
            menu.AddMenuItem(smgsBtn);
            MenuController.BindMenuItem(menu, smgs, smgsBtn);

            throwablesBtn.Label = "→→→";
            menu.AddMenuItem(throwablesBtn);
            MenuController.BindMenuItem(menu, throwables, throwablesBtn);

            meleeBtn.Label = "→→→";
            menu.AddMenuItem(meleeBtn);
            MenuController.BindMenuItem(menu, melee, meleeBtn);

            heavyBtn.Label = "→→→";
            menu.AddMenuItem(heavyBtn);
            MenuController.BindMenuItem(menu, heavy, heavyBtn);

            snipersBtn.Label = "→→→";
            menu.AddMenuItem(snipersBtn);
            MenuController.BindMenuItem(menu, snipers, snipersBtn);
            #endregion

            #region Loop through all weapons, create menus for them and add all menu items and handle events.
            foreach (ValidWeapon weapon in ValidWeapons.WeaponList)
            {
                uint cat = (uint)GetWeapontypeGroup(weapon.Hash);
                if (!string.IsNullOrEmpty(weapon.Name) && IsAllowed(weapon.Perm))
                {
                    //Log($"[DEBUG LOG] [WEAPON-BUG] {weapon.Name} - {weapon.Perm} = {IsAllowed(weapon.Perm)} & All = {IsAllowed(Permission.WPGetAll)}");
                    #region Create menu for this weapon and add buttons
                    Menu weaponMenu = new Menu("Options d'armement", weapon.Name)
                    {
                        ShowWeaponStatsPanel = true
                    };
                    var stats = new Game.WeaponHudStats();
                    Game.GetWeaponHudStats(weapon.Hash, ref stats);
                    weaponMenu.SetWeaponStats((float)stats.hudDamage / 100f, (float)stats.hudSpeed / 100f, (float)stats.hudAccuracy / 100f, (float)stats.hudRange / 100f);
                    MenuItem weaponItem = new MenuItem(weapon.Name, $"Ouvrir les options pour ~y~{weapon.Name}~s~.")
                    {
                        Label = "→→→",
                        LeftIcon = MenuItem.Icon.GUN,
                        ItemData = stats
                    };

                    weaponInfo.Add(weaponMenu, weapon);

                    MenuItem getOrRemoveWeapon = new MenuItem("Équiper/enlever une arme", "Ajoutez ou supprimez cette arme de votre inventaire.")
                    {
                        LeftIcon = MenuItem.Icon.GUN
                    };
                    weaponMenu.AddMenuItem(getOrRemoveWeapon);
                    if (!IsAllowed(Permission.WPSpawn))
                    {
                        getOrRemoveWeapon.Enabled = false;
                        getOrRemoveWeapon.Description = "Vous n'avez pas la permission d'utiliser cette option.";
                        getOrRemoveWeapon.LeftIcon = MenuItem.Icon.LOCK;
                    }

                    MenuItem fillAmmo = new MenuItem("Remplir les munitions", "Obtenez le maximum de munitions pour cette arme.")
                    {
                        LeftIcon = MenuItem.Icon.AMMO
                    };
                    weaponMenu.AddMenuItem(fillAmmo);

                    List<string> tints = new List<string>();
                    if (weapon.Name.Contains(" Mk II"))
                    {
                        foreach (var tint in ValidWeapons.WeaponTintsMkII)
                        {
                            tints.Add(tint.Key);
                        }
                    }
                    else
                    {
                        foreach (var tint in ValidWeapons.WeaponTints)
                        {
                            tints.Add(tint.Key);
                        }
                    }

                    MenuListItem weaponTints = new MenuListItem("Teintes", tints, 0, "");
                    weaponMenu.AddMenuItem(weaponTints);
                    #endregion

                    #region Handle weapon specific list changes
                    weaponMenu.OnListIndexChange += (sender, item, oldIndex, newIndex, itemIndex) =>
                    {
                        if (item == weaponTints)
                        {
                            if (HasPedGotWeapon(Game.PlayerPed.Handle, weaponInfo[sender].Hash, false))
                            {
                                SetPedWeaponTintIndex(Game.PlayerPed.Handle, weaponInfo[sender].Hash, newIndex);
                            }
                            else
                            {
                                Notify.Error("Vous devez d'abord récupérer l'arme !");
                            }
                        }
                    };
                    #endregion

                    #region Handle weapon specific button presses
                    weaponMenu.OnItemSelect += (sender, item, index) =>
                    {
                        var info = weaponInfo[sender];
                        uint hash = info.Hash;

                        SetCurrentPedWeapon(Game.PlayerPed.Handle, hash, true);

                        if (item == getOrRemoveWeapon)
                        {
                            if (HasPedGotWeapon(Game.PlayerPed.Handle, hash, false))
                            {
                                RemoveWeaponFromPed(Game.PlayerPed.Handle, hash);
                                Subtitle.Custom("Arme retirée.");
                            }
                            else
                            {
                                var ammo = 255;
                                GetMaxAmmo(Game.PlayerPed.Handle, hash, ref ammo);
                                GiveWeaponToPed(Game.PlayerPed.Handle, hash, ammo, false, true);
                                Subtitle.Custom("Arme ajoutée.");
                            }
                        }
                        else if (item == fillAmmo)
                        {
                            if (HasPedGotWeapon(Game.PlayerPed.Handle, hash, false))
                            {
                                var ammo = 900;
                                GetMaxAmmo(Game.PlayerPed.Handle, hash, ref ammo);
                                SetPedAmmo(Game.PlayerPed.Handle, hash, ammo);
                            }
                            else
                            {
                                Notify.Error("Vous devez d'abord obtenir l'arme avant de recharger les munitions !");
                            }
                        }
                    };
                    #endregion

                    #region load components
                    if (weapon.Components != null)
                    {
                        if (weapon.Components.Count > 0)
                        {
                            foreach (var comp in weapon.Components)
                            {
                                //Log($"{weapon.Name} : {comp.Key}");
                                MenuItem compItem = new MenuItem(comp.Key, "Cliquez pour équiper ou supprimer ce composant.");
                                weaponComponents.Add(compItem, comp.Key);
                                weaponMenu.AddMenuItem(compItem);

                                #region Handle component button presses
                                weaponMenu.OnItemSelect += (sender, item, index) =>
                                {
                                    if (item == compItem)
                                    {
                                        var Weapon = weaponInfo[sender];
                                        var componentHash = Weapon.Components[weaponComponents[item]];
                                        if (HasPedGotWeapon(Game.PlayerPed.Handle, Weapon.Hash, false))
                                        {
                                            SetCurrentPedWeapon(Game.PlayerPed.Handle, Weapon.Hash, true);
                                            if (HasPedGotWeaponComponent(Game.PlayerPed.Handle, Weapon.Hash, componentHash))
                                            {
                                                RemoveWeaponComponentFromPed(Game.PlayerPed.Handle, Weapon.Hash, componentHash);

                                                Subtitle.Custom("Composant retiré.");
                                            }
                                            else
                                            {
                                                int ammo = GetAmmoInPedWeapon(Game.PlayerPed.Handle, Weapon.Hash);

                                                int clipAmmo = GetMaxAmmoInClip(Game.PlayerPed.Handle, Weapon.Hash, false);
                                                GetAmmoInClip(Game.PlayerPed.Handle, Weapon.Hash, ref clipAmmo);

                                                GiveWeaponComponentToPed(Game.PlayerPed.Handle, Weapon.Hash, componentHash);

                                                SetAmmoInClip(Game.PlayerPed.Handle, Weapon.Hash, clipAmmo);

                                                SetPedAmmo(Game.PlayerPed.Handle, Weapon.Hash, ammo);
                                                Subtitle.Custom("Composant équipé.");
                                            }
                                        }
                                        else
                                        {
                                            Notify.Error("Vous devez d'abord obtenir l'arme avant de pouvoir la modifier.");
                                        }
                                    }
                                };
                                #endregion
                            }
                        }
                    }
                    #endregion

                    // refresh and add to menu.
                    weaponMenu.RefreshIndex();

                    if (cat == 970310034) // 970310034 rifles
                    {
                        MenuController.AddSubmenu(rifles, weaponMenu);
                        MenuController.BindMenuItem(rifles, weaponMenu, weaponItem);
                        rifles.AddMenuItem(weaponItem);
                    }
                    else if (cat == 416676503 || cat == 690389602) // 416676503 hand guns // 690389602 stun gun
                    {
                        MenuController.AddSubmenu(handGuns, weaponMenu);
                        MenuController.BindMenuItem(handGuns, weaponMenu, weaponItem);
                        handGuns.AddMenuItem(weaponItem);
                    }
                    else if (cat == 860033945) // 860033945 shotguns
                    {
                        MenuController.AddSubmenu(shotguns, weaponMenu);
                        MenuController.BindMenuItem(shotguns, weaponMenu, weaponItem);
                        shotguns.AddMenuItem(weaponItem);
                    }
                    else if (cat == 3337201093 || cat == 1159398588) // 3337201093 sub machine guns // 1159398588 light machine guns
                    {
                        MenuController.AddSubmenu(smgs, weaponMenu);
                        MenuController.BindMenuItem(smgs, weaponMenu, weaponItem);
                        smgs.AddMenuItem(weaponItem);
                    }
                    else if (cat == 1548507267 || cat == 4257178988 || cat == 1595662460) // 1548507267 throwables // 4257178988 fire extinghuiser // jerry can
                    {
                        MenuController.AddSubmenu(throwables, weaponMenu);
                        MenuController.BindMenuItem(throwables, weaponMenu, weaponItem);
                        throwables.AddMenuItem(weaponItem);
                    }
                    else if (cat == 3566412244 || cat == 2685387236) // 3566412244 melee weapons // 2685387236 knuckle duster
                    {
                        MenuController.AddSubmenu(melee, weaponMenu);
                        MenuController.BindMenuItem(melee, weaponMenu, weaponItem);
                        melee.AddMenuItem(weaponItem);
                    }
                    else if (cat == 2725924767) // 2725924767 heavy weapons
                    {
                        MenuController.AddSubmenu(heavy, weaponMenu);
                        MenuController.BindMenuItem(heavy, weaponMenu, weaponItem);
                        heavy.AddMenuItem(weaponItem);
                    }
                    else if (cat == 3082541095) // 3082541095 sniper rifles
                    {
                        MenuController.AddSubmenu(snipers, weaponMenu);
                        MenuController.BindMenuItem(snipers, weaponMenu, weaponItem);
                        snipers.AddMenuItem(weaponItem);
                    }
                }
            }
            #endregion

            #region Disable submenus if no weapons in that category are allowed.
            if (handGuns.Size == 0)
            {
                handGunsBtn.LeftIcon = MenuItem.Icon.LOCK;
                handGunsBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                handGunsBtn.Enabled = false;
            }
            if (rifles.Size == 0)
            {
                riflesBtn.LeftIcon = MenuItem.Icon.LOCK;
                riflesBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                riflesBtn.Enabled = false;
            }
            if (shotguns.Size == 0)
            {
                shotgunsBtn.LeftIcon = MenuItem.Icon.LOCK;
                shotgunsBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                shotgunsBtn.Enabled = false;
            }
            if (smgs.Size == 0)
            {
                smgsBtn.LeftIcon = MenuItem.Icon.LOCK;
                smgsBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                smgsBtn.Enabled = false;
            }
            if (throwables.Size == 0)
            {
                throwablesBtn.LeftIcon = MenuItem.Icon.LOCK;
                throwablesBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                throwablesBtn.Enabled = false;
            }
            if (melee.Size == 0)
            {
                meleeBtn.LeftIcon = MenuItem.Icon.LOCK;
                meleeBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                meleeBtn.Enabled = false;
            }
            if (heavy.Size == 0)
            {
                heavyBtn.LeftIcon = MenuItem.Icon.LOCK;
                heavyBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                heavyBtn.Enabled = false;
            }
            if (snipers.Size == 0)
            {
                snipersBtn.LeftIcon = MenuItem.Icon.LOCK;
                snipersBtn.Description = "The server owner removed the permissions for all weapons in this category.";
                snipersBtn.Enabled = false;
            }
            #endregion

            #region Handle button presses
            menu.OnItemSelect += (sender, item, index) =>
            {
                Ped ped = new Ped(Game.PlayerPed.Handle);
                if (item == getAllWeapons)
                {
                    foreach (ValidWeapon vw in ValidWeapons.WeaponList)
                    {
                        if (IsAllowed(vw.Perm))
                        {
                            GiveWeaponToPed(Game.PlayerPed.Handle, vw.Hash, vw.GetMaxAmmo, false, true);

                            int ammoInClip = GetMaxAmmoInClip(Game.PlayerPed.Handle, vw.Hash, false);
                            SetAmmoInClip(Game.PlayerPed.Handle, vw.Hash, ammoInClip);
                            int ammo = 0;
                            GetMaxAmmo(Game.PlayerPed.Handle, vw.Hash, ref ammo);
                            SetPedAmmo(Game.PlayerPed.Handle, vw.Hash, ammo);
                        }
                    }

                    SetCurrentPedWeapon(Game.PlayerPed.Handle, (uint)GetHashKey("weapon_unarmed"), true);
                }
                else if (item == removeAllWeapons)
                {
                    ped.Weapons.RemoveAll();
                }
                else if (item == setAmmo)
                {
                    SetAllWeaponsAmmo();
                }
                else if (item == refillMaxAmmo)
                {
                    foreach (ValidWeapon vw in ValidWeapons.WeaponList)
                    {
                        if (HasPedGotWeapon(Game.PlayerPed.Handle, vw.Hash, false))
                        {
                            int ammoInClip = GetMaxAmmoInClip(Game.PlayerPed.Handle, vw.Hash, false);
                            SetAmmoInClip(Game.PlayerPed.Handle, vw.Hash, ammoInClip);
                            int ammo = 0;
                            GetMaxAmmo(Game.PlayerPed.Handle, vw.Hash, ref ammo);
                            SetPedAmmo(Game.PlayerPed.Handle, vw.Hash, ammo);
                        }
                    }
                }
                else if (item == spawnByName)
                {
                    SpawnCustomWeapon();
                }
            };
            #endregion

            #region Handle checkbox changes
            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == noReload)
                {
                    NoReload = _checked;
                    Subtitle.Custom($"No reload is now {(_checked ? "enabled" : "disabled")}.");
                }
                else if (item == unlimitedAmmo)
                {
                    UnlimitedAmmo = _checked;
                    Subtitle.Custom($"Unlimited ammo is now {(_checked ? "enabled" : "disabled")}.");
                }
            };
            #endregion

            void OnIndexChange(Menu m, MenuItem i)
            {
                if (i.ItemData is Game.WeaponHudStats stats)
                {
                    m.SetWeaponStats((float)stats.hudDamage / 100f, (float)stats.hudSpeed / 100f, (float)stats.hudAccuracy / 100f, (float)stats.hudRange / 100f);
                    m.ShowWeaponStatsPanel = true;
                }
                else
                {
                    m.ShowWeaponStatsPanel = false;
                }
            }

            handGuns.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            rifles.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            shotguns.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            smgs.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            throwables.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            melee.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            heavy.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };
            snipers.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => { OnIndexChange(sender, newItem); };

            handGuns.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            rifles.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            shotguns.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            smgs.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            throwables.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            melee.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            heavy.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
            snipers.OnMenuOpen += (sender) => { OnIndexChange(sender, sender.GetCurrentMenuItem()); };
        }


        #endregion

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
    }
}