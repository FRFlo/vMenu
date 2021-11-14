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
    public class WeaponLoadouts
    {
        // Variables
        private Menu menu = null;
        private Menu SavedLoadoutsMenu = new Menu("Configurations Sauvegardées", "Liste des configurations d'armes sauvegardés");
        private Menu ManageLoadoutMenu = new Menu("Gérer les configurations", "Gérer les configurations d'armes sauvegardées");
        public bool WeaponLoadoutsSetLoadoutOnRespawn { get; private set; } = UserDefaults.WeaponLoadoutsSetLoadoutOnRespawn;

        private Dictionary<string, List<ValidWeapon>> SavedWeapons = new Dictionary<string, List<ValidWeapon>>();

        public static Dictionary<string, List<ValidWeapon>> GetSavedWeapons()
        {
            int handle = StartFindKvp("vmenu_string_saved_weapon_loadout_");
            Dictionary<string, List<ValidWeapon>> saves = new Dictionary<string, List<ValidWeapon>>();
            while (true)
            {
                string kvp = FindKvp(handle);
                if (string.IsNullOrEmpty(kvp))
                {
                    break;
                }
                saves.Add(kvp, JsonConvert.DeserializeObject<List<ValidWeapon>>(GetResourceKvpString(kvp)));
            }
            EndFindKvp(handle);
            return saves;
        }

        private string SelectedSavedLoadoutName { get; set; } = "";
        // vmenu_temp_weapons_loadout_before_respawn
        // vmenu_string_saved_weapon_loadout_

        /// <summary>
        /// Returns the saved weapons list, as well as sets the <see cref="SavedWeapons"/> variable.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, List<ValidWeapon>> RefreshSavedWeaponsList()
        {
            if (SavedWeapons.Count > 0) SavedWeapons.Clear();

            int handle = StartFindKvp("vmenu_string_saved_weapon_loadout_");
            List<string> saves = new List<string>();
            while (true)
            {
                string kvp = FindKvp(handle);
                if (string.IsNullOrEmpty(kvp))
                {
                    break;
                }
                saves.Add(kvp);
            }
            EndFindKvp(handle);

            foreach (var save in saves)
            {
                SavedWeapons.Add(save, JsonConvert.DeserializeObject<List<ValidWeapon>>(GetResourceKvpString(save)));
            }

            return SavedWeapons;
        }

        /// <summary>
        /// Creates the menu if it doesn't exist yet and sets the event handlers.
        /// </summary>
        public void CreateMenu()
        {
            menu = new Menu(Game.Player.Name, "weapon loadouts management");

            MenuController.AddSubmenu(menu, SavedLoadoutsMenu);
            MenuController.AddSubmenu(SavedLoadoutsMenu, ManageLoadoutMenu);

            MenuItem saveLoadout = new MenuItem("Sauvegarder les armes", "Enregistrez vos armes actuelles dans un nouvel emplacement de configuration.");
            MenuItem savedLoadoutsMenuBtn = new MenuItem("Gérer les configurations", "Gérer les configurations d'armes sauvegardées.") { Label = "→→→" };
            MenuCheckboxItem enableDefaultLoadouts = new MenuCheckboxItem("Restaurer la configuration par défaut lors de la réapparition", "Si vous avez défini une charge comme charge par défaut, cette charge sera équipée automatiquement à chaque fois que vous réapparaissez.", WeaponLoadoutsSetLoadoutOnRespawn);

            menu.AddMenuItem(saveLoadout);
            menu.AddMenuItem(savedLoadoutsMenuBtn);
            MenuController.BindMenuItem(menu, SavedLoadoutsMenu, savedLoadoutsMenuBtn);
            if (IsAllowed(Permission.WLEquipOnRespawn))
            {
                menu.AddMenuItem(enableDefaultLoadouts);

                menu.OnCheckboxChange += (sender, checkbox, index, _checked) =>
                {
                    WeaponLoadoutsSetLoadoutOnRespawn = _checked;
                };
            }


            void RefreshSavedWeaponsMenu()
            {
                int oldCount = SavedLoadoutsMenu.Size;
                SavedLoadoutsMenu.ClearMenuItems(true);

                RefreshSavedWeaponsList();

                foreach (var sw in SavedWeapons)
                {
                    MenuItem btn = new MenuItem(sw.Key.Replace("vmenu_string_saved_weapon_loadout_", ""), "Cliquez pour gérer cette configuration.") { Label = "→→→" };
                    SavedLoadoutsMenu.AddMenuItem(btn);
                    MenuController.BindMenuItem(SavedLoadoutsMenu, ManageLoadoutMenu, btn);
                }

                if (oldCount > SavedWeapons.Count)
                {
                    SavedLoadoutsMenu.RefreshIndex();
                }
            }


            MenuItem spawnLoadout = new MenuItem("Equiper l'équipement", "Equiper la configuration d'armes sauvegardée. Cela supprimera toutes vos armes actuelles et les remplacera par cet emplacement sauvegardé.");
            MenuItem renameLoadout = new MenuItem("Renommer l'équipement", "Renommez ce configuration sauvegardé.");
            MenuItem cloneLoadout = new MenuItem("Cloner l'équipement", "Clone cette configuration sauvegardée dans un nouvel emplacement..");
            MenuItem setDefaultLoadout = new MenuItem("Définir comme équipement par défaut", "Définissez ce chargement pour qu'il devienne votre chargement par défaut à chaque fois que vous réapparaissez. Cela remplacera l'option 'Restaurer les armes' dans le menu 'Paramètres divers'. Vous pouvez activer cette option dans le menu principal des chargements d'armes.");
            MenuItem replaceLoadout = new MenuItem("~r~Remplacer l'équipement", "~r~This replaces this saved slot with the weapons that you currently have in your inventory. This action can not be undone!");
            MenuItem deleteLoadout = new MenuItem("~r~Supprimer l'équipement", "~r~Cela supprimera cette configuration sauvegardée.  Cette action ne peut être annulée !");

            if (IsAllowed(Permission.WLEquip))
                ManageLoadoutMenu.AddMenuItem(spawnLoadout);
            ManageLoadoutMenu.AddMenuItem(renameLoadout);
            ManageLoadoutMenu.AddMenuItem(cloneLoadout);
            ManageLoadoutMenu.AddMenuItem(setDefaultLoadout);
            ManageLoadoutMenu.AddMenuItem(replaceLoadout);
            ManageLoadoutMenu.AddMenuItem(deleteLoadout);

            // Save the weapons loadout.
            menu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == saveLoadout)
                {
                    string name = await GetUserInput("Entrez un nom de sauvegarde", 30);
                    if (string.IsNullOrEmpty(name))
                    {
                        Notify.Error(CommonErrors.InvalidInput);
                    }
                    else
                    {
                        if (SavedWeapons.ContainsKey("vmenu_string_saved_weapon_loadout_" + name))
                        {
                            Notify.Error(CommonErrors.SaveNameAlreadyExists);
                        }
                        else
                        {
                            if (SaveWeaponLoadout("vmenu_string_saved_weapon_loadout_" + name))
                            {
                                Log("saveweapons called from menu select (save loadout button)");
                                Notify.Success($"Vos armes ont été sauvegardées en tant que ~g~<C>{name}</C>~s~.");
                            }
                            else
                            {
                                Notify.Error(CommonErrors.UnknownError);
                            }
                        }
                    }
                }
            };

            // manage spawning, renaming, deleting etc.
            ManageLoadoutMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (SavedWeapons.ContainsKey(SelectedSavedLoadoutName))
                {
                    List<ValidWeapon> weapons = SavedWeapons[SelectedSavedLoadoutName];

                    if (item == spawnLoadout) // spawn
                    {
                        await SpawnWeaponLoadoutAsync(SelectedSavedLoadoutName, false, true, false);
                    }
                    else if (item == renameLoadout || item == cloneLoadout) // rename or clone
                    {
                        string newName = await GetUserInput("Entrez un nom de sauvegarde", SelectedSavedLoadoutName.Replace("vmenu_string_saved_weapon_loadout_", ""), 30);
                        if (string.IsNullOrEmpty(newName))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                        }
                        else
                        {
                            if (SavedWeapons.ContainsKey("vmenu_string_saved_weapon_loadout_" + newName))
                            {
                                Notify.Error(CommonErrors.SaveNameAlreadyExists);
                            }
                            else
                            {
                                SetResourceKvp("vmenu_string_saved_weapon_loadout_" + newName, JsonConvert.SerializeObject(weapons));
                                Notify.Success($"Votre équipement a été {(item == renameLoadout ? "renommée" : "clonée")} to ~g~<C>{newName}</C>~s~.");

                                if (item == renameLoadout)
                                    DeleteResourceKvp(SelectedSavedLoadoutName);

                                ManageLoadoutMenu.GoBack();
                            }
                        }
                    }
                    else if (item == setDefaultLoadout) // set as default
                    {
                        SetResourceKvp("vmenu_string_default_loadout", SelectedSavedLoadoutName);
                        Notify.Success("C'est maintenant votre équipement par défaut.");
                        item.LeftIcon = MenuItem.Icon.TICK;
                    }
                    else if (item == replaceLoadout) // replace
                    {
                        if (replaceLoadout.Label == "Etes-vous sûr ?")
                        {
                            replaceLoadout.Label = "";
                            SaveWeaponLoadout(SelectedSavedLoadoutName);
                            Log("save weapons called from replace loadout");
                            Notify.Success("Votre équipement sauvegardé a été remplacé par vos armes actuelles.");
                        }
                        else
                        {
                            replaceLoadout.Label = "Etes-vous sûr ?";
                        }
                    }
                    else if (item == deleteLoadout) // delete
                    {
                        if (deleteLoadout.Label == "Etes-vous sûr ?")
                        {
                            deleteLoadout.Label = "";
                            DeleteResourceKvp(SelectedSavedLoadoutName);
                            ManageLoadoutMenu.GoBack();
                            Notify.Success("Votre équipement sauvegardé a été supprimé.");
                        }
                        else
                        {
                            deleteLoadout.Label = "Etes-vous sûr ?";
                        }
                    }
                }
            };

            // Reset the 'are you sure' states.
            ManageLoadoutMenu.OnMenuClose += (sender) =>
            {
                deleteLoadout.Label = "";
                renameLoadout.Label = "";
            };
            // Reset the 'are you sure' states.
            ManageLoadoutMenu.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) =>
            {
                deleteLoadout.Label = "";
                renameLoadout.Label = "";
            };

            // Refresh the spawned weapons menu whenever this menu is opened.
            SavedLoadoutsMenu.OnMenuOpen += (sender) =>
            {
                RefreshSavedWeaponsMenu();
            };

            // Set the current saved loadout whenever a loadout is selected.
            SavedLoadoutsMenu.OnItemSelect += (sender, item, index) =>
            {
                if (SavedWeapons.ContainsKey("vmenu_string_saved_weapon_loadout_" + item.Text))
                {
                    SelectedSavedLoadoutName = "vmenu_string_saved_weapon_loadout_" + item.Text;
                }
                else // shouldn't ever happen, but just in case
                {
                    ManageLoadoutMenu.GoBack();
                }
            };

            // Reset the index whenever the ManageLoadout menu is opened. Just to prevent auto selecting the delete option for example.
            ManageLoadoutMenu.OnMenuOpen += (sender) =>
            {
                ManageLoadoutMenu.RefreshIndex();
                string kvp = GetResourceKvpString("vmenu_string_default_loadout");
                if (string.IsNullOrEmpty(kvp) || kvp != SelectedSavedLoadoutName)
                {
                    setDefaultLoadout.LeftIcon = MenuItem.Icon.NONE;
                }
                else
                {
                    setDefaultLoadout.LeftIcon = MenuItem.Icon.TICK;
                }

            };

            // Refresh the saved weapons menu.
            RefreshSavedWeaponsMenu();
        }

        /// <summary>
        /// Gets the menu.
        /// </summary>
        /// <returns></returns>
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
