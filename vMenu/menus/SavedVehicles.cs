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
    public class SavedVehicles
    {
        // Variables
        private Menu menu;
        private Menu selectedVehicleMenu = new Menu("Gérer le véhicule", "Gérer ce véhicule sauvegardé.");
        private Menu unavailableVehiclesMenu = new Menu("Véhicules manquants", "Véhicules sauvegardés non disponibles");
        private Dictionary<string, VehicleInfo> savedVehicles = new Dictionary<string, VehicleInfo>();
        private List<Menu> subMenus = new List<Menu>();
        private Dictionary<MenuItem, KeyValuePair<string, VehicleInfo>> svMenuItems = new Dictionary<MenuItem, KeyValuePair<string, VehicleInfo>>();
        private KeyValuePair<string, VehicleInfo> currentlySelectedVehicle = new KeyValuePair<string, VehicleInfo>();
        private int deleteButtonPressedCount = 0;


        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            string menuTitle = "Véhicules sauvegardés";
            #region Create menus and submenus
            // Create the menu.
            menu = new Menu(menuTitle, "Gérer les véhicules sauvegardés");

            MenuItem saveVehicle = new MenuItem("Sauvegarder le véhicule actuel", "Sauvegardez le véhicule dans lequel vous êtes actuellement assis.");
            menu.AddMenuItem(saveVehicle);
            saveVehicle.LeftIcon = MenuItem.Icon.CAR;

            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == saveVehicle)
                {
                    if (Game.PlayerPed.IsInVehicle())
                    {
                        SaveVehicle();
                    }
                    else
                    {
                        Notify.Error("Vous n'êtes actuellement dans aucun véhicule. Veuillez entrer dans un véhicule avant d'essayer de l'enregistrer.");
                    }
                }
            };

            for (int i = 0; i < 23; i++)
            {
                Menu categoryMenu = new Menu("Véhicules sauvegardés", GetLabelText($"VEH_CLASS_{i}"));

                MenuItem categoryButton = new MenuItem(GetLabelText($"VEH_CLASS_{i}"), $"Tous les véhicules sauvegardés de la catégorie {(GetLabelText($"VEH_CLASS_{i}"))}.");
                subMenus.Add(categoryMenu);
                MenuController.AddSubmenu(menu, categoryMenu);
                menu.AddMenuItem(categoryButton);
                categoryButton.Label = "→→→";
                MenuController.BindMenuItem(menu, categoryMenu, categoryButton);

                categoryMenu.OnMenuClose += (sender) =>
                {
                    UpdateMenuAvailableCategories();
                };

                categoryMenu.OnItemSelect += (sender, item, index) =>
                {
                    UpdateSelectedVehicleMenu(item, sender);
                };
            }

            MenuItem unavailableModels = new MenuItem("Véhicules sauvegardés non disponibles", "Ces véhicules sont actuellement indisponibles car les modèles ne sont pas présents dans le jeu. Il est fort probable que ces véhicules ne soient pas streamés depuis le serveur.")
            {
                Label = "→→→"
            };

            menu.AddMenuItem(unavailableModels);
            MenuController.BindMenuItem(menu, unavailableVehiclesMenu, unavailableModels);
            MenuController.AddSubmenu(menu, unavailableVehiclesMenu);


            MenuController.AddMenu(selectedVehicleMenu);
            MenuItem spawnVehicle = new MenuItem("Faire apparaitre le véhicule", "Générer le véhicule sauvegardé.");
            MenuItem renameVehicle = new MenuItem("Renommer le véhicule", "Renommez votre véhicule sauvegardé.");
            MenuItem replaceVehicle = new MenuItem("~r~Remplacer le véhicule", "Votre véhicule sauvegardé sera remplacé par le véhicule dans lequel vous êtes actuellement assis. ~r~Attention : ceci ne peut pas être annulé !");
            MenuItem deleteVehicle = new MenuItem("~r~Supprimer le véhicule", "~r~Ceci supprimera votre véhicule sauvegardé. Attention : cette opération ne peut pas être annulée !");
            selectedVehicleMenu.AddMenuItem(spawnVehicle);
            selectedVehicleMenu.AddMenuItem(renameVehicle);
            selectedVehicleMenu.AddMenuItem(replaceVehicle);
            selectedVehicleMenu.AddMenuItem(deleteVehicle);

            selectedVehicleMenu.OnMenuOpen += (sender) =>
            {
                spawnVehicle.Label = "(" + GetDisplayNameFromVehicleModel(currentlySelectedVehicle.Value.model).ToLower() + ")";
            };

            selectedVehicleMenu.OnMenuClose += (sender) =>
            {
                selectedVehicleMenu.RefreshIndex();
                deleteButtonPressedCount = 0;
                deleteVehicle.Label = "";
            };

            selectedVehicleMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == spawnVehicle)
                {
                    if (MainMenu.VehicleSpawnerMenu != null)
                    {
                        SpawnVehicle(currentlySelectedVehicle.Value.model, MainMenu.VehicleSpawnerMenu.SpawnInVehicle, MainMenu.VehicleSpawnerMenu.ReplaceVehicle, false, vehicleInfo: currentlySelectedVehicle.Value, saveName: currentlySelectedVehicle.Key.Substring(4));
                    }
                    else
                    {
                        SpawnVehicle(currentlySelectedVehicle.Value.model, true, true, false, vehicleInfo: currentlySelectedVehicle.Value, saveName: currentlySelectedVehicle.Key.Substring(4));
                    }
                }
                else if (item == renameVehicle)
                {
                    string newName = await GetUserInput(windowTitle: "Saisissez un nouveau nom pour ce véhicule.", maxInputLength: 30);
                    if (string.IsNullOrEmpty(newName))
                    {
                        Notify.Error(CommonErrors.InvalidInput);
                    }
                    else
                    {
                        if (StorageManager.SaveVehicleInfo("veh_" + newName, currentlySelectedVehicle.Value, false))
                        {
                            DeleteResourceKvp(currentlySelectedVehicle.Key);
                            while (!selectedVehicleMenu.Visible)
                            {
                                await BaseScript.Delay(0);
                            }
                            Notify.Success("Votre véhicule a été renommé avec succès.");
                            UpdateMenuAvailableCategories();
                            selectedVehicleMenu.GoBack();
                            currentlySelectedVehicle = new KeyValuePair<string, VehicleInfo>(); // clear the old info
                        }
                        else
                        {
                            Notify.Error("Ce nom est déjà utilisé ou quelque chose d'inconnu a échoué. Contactez le propriétaire du serveur si vous pensez que quelque chose ne va pas.");
                        }
                    }
                }
                else if (item == replaceVehicle)
                {
                    if (Game.PlayerPed.IsInVehicle())
                    {
                        SaveVehicle(currentlySelectedVehicle.Key.Substring(4));
                        selectedVehicleMenu.GoBack();
                        Notify.Success("Votre véhicule sauvegardé a été remplacé par votre véhicule actuel.");
                    }
                    else
                    {
                        Notify.Error("Vous devez être dans un véhicule avant de pouvoir replacer votre ancien véhicule.");
                    }
                }
                else if (item == deleteVehicle)
                {
                    if (deleteButtonPressedCount == 0)
                    {
                        deleteButtonPressedCount = 1;
                        item.Label = "Appuyez à nouveau pour confirmer.";
                        Notify.Alert("Êtes-vous sûr de vouloir supprimer ce véhicule ? Appuyez à nouveau sur le bouton pour confirmer.");
                    }
                    else
                    {
                        deleteButtonPressedCount = 0;
                        item.Label = "";
                        DeleteResourceKvp(currentlySelectedVehicle.Key);
                        UpdateMenuAvailableCategories();
                        selectedVehicleMenu.GoBack();
                        Notify.Success("Votre véhicule enregistré a été supprimé.");
                    }
                }
                if (item != deleteVehicle) // if any other button is pressed, restore the delete vehicle button pressed count.
                {
                    deleteButtonPressedCount = 0;
                    deleteVehicle.Label = "";
                }
            };
            unavailableVehiclesMenu.InstructionalButtons.Add(Control.FrontendDelete, "Supprimer le véhicule !");

            unavailableVehiclesMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler(Control.FrontendDelete, Menu.ControlPressCheckType.JUST_RELEASED, new Action<Menu, Control>((m, c) =>
            {
                if (m.Size > 0)
                {
                    int index = m.CurrentIndex;
                    if (index < m.Size)
                    {
                        MenuItem item = m.GetMenuItems().Find(i => i.Index == index);
                        if (item != null && (item.ItemData is KeyValuePair<string, VehicleInfo> sd))
                        {
                            if (item.Label == "~r~Etes-vous sûr ?")
                            {
                                Log("Unavailable saved vehicle deleted, data: " + JsonConvert.SerializeObject(sd));
                                DeleteResourceKvp(sd.Key);
                                unavailableVehiclesMenu.GoBack();
                                UpdateMenuAvailableCategories();
                            }
                            else
                            {
                                item.Label = "~r~Etes-vous sûr ?";
                            }
                        }
                        else
                        {
                            Notify.Error("D'une manière ou d'une autre, ce véhicule n'a pas pu être retrouvé.");
                        }
                    }
                    else
                    {
                        Notify.Error("Vous avez réussi à déclencher la suppression d'un élément de menu qui n'existe pas, comment... ?");
                    }
                }
                else
                {
                    Notify.Error("Il n'y a actuellement aucun véhicule indisponible à supprimer !");
                }
            }), true));

            void ResetAreYouSure()
            {
                foreach (var i in unavailableVehiclesMenu.GetMenuItems())
                {
                    if (i.ItemData is KeyValuePair<string, VehicleInfo> vd)
                    {
                        i.Label = $"({vd.Value.name})";
                    }
                }
            }
            unavailableVehiclesMenu.OnMenuClose += (sender) => ResetAreYouSure();
            unavailableVehiclesMenu.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) => ResetAreYouSure();

            #endregion
        }


        /// <summary>
        /// Updates the selected vehicle.
        /// </summary>
        /// <param name="selectedItem"></param>
        /// <returns>A bool, true if successfull, false if unsuccessfull</returns>
        private bool UpdateSelectedVehicleMenu(MenuItem selectedItem, Menu parentMenu = null)
        {
            if (!svMenuItems.ContainsKey(selectedItem))
            {
                Notify.Error("D'une manière très étrange, vous avez réussi à sélectionner un bouton qui n'existe pas dans cette liste. Votre véhicule n'a donc pas pu être chargé :( Peut-être vos fichiers de sauvegarde sont-ils endommagés ?");
                return false;
            }
            var vehInfo = svMenuItems[selectedItem];
            selectedVehicleMenu.MenuSubtitle = $"{vehInfo.Key.Substring(4)} ({vehInfo.Value.name})";
            currentlySelectedVehicle = vehInfo;
            MenuController.CloseAllMenus();
            selectedVehicleMenu.OpenMenu();
            if (parentMenu != null)
            {
                MenuController.AddSubmenu(parentMenu, selectedVehicleMenu);
            }
            return true;
        }


        /// <summary>
        /// Updates the available vehicle category list.
        /// </summary>
        public void UpdateMenuAvailableCategories()
        {
            savedVehicles = GetSavedVehicles();
            svMenuItems = new Dictionary<MenuItem, KeyValuePair<string, VehicleInfo>>();

            for (int i = 1; i < GetMenu().Size - 1; i++)
            {
                if (savedVehicles.Any(a => GetVehicleClassFromName(a.Value.model) == i - 1 && IsModelInCdimage(a.Value.model)))
                {
                    GetMenu().GetMenuItems()[i].RightIcon = MenuItem.Icon.NONE;
                    GetMenu().GetMenuItems()[i].Label = "→→→";
                    GetMenu().GetMenuItems()[i].Enabled = true;
                    GetMenu().GetMenuItems()[i].Description = $"Tous les véhicules sauvés de la catégorie {GetMenu().GetMenuItems()[i].Text}";
                }
                else
                {
                    GetMenu().GetMenuItems()[i].Label = "";
                    GetMenu().GetMenuItems()[i].RightIcon = MenuItem.Icon.LOCK;
                    GetMenu().GetMenuItems()[i].Enabled = false;
                    GetMenu().GetMenuItems()[i].Description = $"Vous n'avez pas de véhicules sauvegardés qui appartiennent à la catégorie {GetMenu().GetMenuItems()[i].Text}.";
                }
            }

            // Check if the items count will be changed. If there are less cars than there were before, one probably got deleted
            // so in that case we need to refresh the index of that menu just to be safe. If not, keep the index where it is for improved
            // usability of the menu.
            foreach (Menu m in subMenus)
            {
                int size = m.Size;
                int vclass = subMenus.IndexOf(m);

                int count = savedVehicles.Count(a => GetVehicleClassFromName(a.Value.model) == vclass);
                if (count < size)
                {
                    m.RefreshIndex();
                }
            }

            foreach (Menu m in subMenus)
            {
                // Clear items but don't reset the index because we can guarantee that the index won't be out of bounds.
                // this is the case because of the loop above where we reset the index if the items count changes.
                m.ClearMenuItems(true);
            }

            // Always clear this index because it's useless anyway and it's safer.
            unavailableVehiclesMenu.ClearMenuItems();

            foreach (var sv in savedVehicles)
            {
                if (IsModelInCdimage(sv.Value.model))
                {
                    int vclass = GetVehicleClassFromName(sv.Value.model);
                    Menu menu = subMenus[vclass];

                    MenuItem savedVehicleBtn = new MenuItem(sv.Key.Substring(4), $"Gérer ce véhicule sauvegardé.")
                    {
                        Label = $"({sv.Value.name}) →→→"
                    };
                    menu.AddMenuItem(savedVehicleBtn);

                    svMenuItems.Add(savedVehicleBtn, sv);
                }
                else
                {
                    MenuItem missingVehItem = new MenuItem(sv.Key.Substring(4), "Ce modèle n'a pas pu être trouvé dans les fichiers du jeu. Très probablement parce qu'il s'agit d'un véhicule ajouté et qu'il n'est actuellement pas streamé par le serveur.")
                    {
                        Label = "(" + sv.Value.name + ")",
                        Enabled = false,
                        LeftIcon = MenuItem.Icon.LOCK,
                        ItemData = sv
                    };
                    //SetResourceKvp(sv.Key + "_tmp_dupe", JsonConvert.SerializeObject(sv.Value));
                    unavailableVehiclesMenu.AddMenuItem(missingVehItem);
                }
            }
            foreach (Menu m in subMenus)
            {
                m.SortMenuItems((MenuItem A, MenuItem B) =>
                {
                    return A.Text.ToLower().CompareTo(B.Text.ToLower());
                });
            }
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
    }
}
