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
using vMenuShared;

namespace vMenuClient
{
    public class WeatherOptions
    {
        // Variables
        private Menu menu;
        public MenuCheckboxItem dynamicWeatherEnabled;
        public MenuCheckboxItem blackout;
        public MenuCheckboxItem snowEnabled;
        public static readonly List<string> weatherTypes = new List<string>()
        {
            "EXTRASUNNY",
            "CLEAR",
            "NEUTRAL",
            "SMOG",
            "FOGGY",
            "CLOUDS",
            "OVERCAST",
            "CLEARING",
            "RAIN",
            "THUNDER",
            "BLIZZARD",
            "SNOW",
            "SNOWLIGHT",
            "XMAS",
            "HALLOWEEN"
        };

        private void CreateMenu()
        {
            // Create the menu.
            menu = new Menu(Game.Player.Name, "Options de la météo");

            dynamicWeatherEnabled = new MenuCheckboxItem("Activer/Désactiver la météo dynamique", "Activez ou désactivez les changements dynamiques de météo.", EventManager.DynamicWeatherEnabled);
            blackout = new MenuCheckboxItem("Activer/Désactiver Blackout", "Cela désactive ou active toutes les lumières de la carte.", EventManager.IsBlackoutEnabled);
            snowEnabled = new MenuCheckboxItem("Activer les effets de neige", "Cela forcera la neige à apparaître sur le sol et activera les effets de particules de neige pour les voitures et les véhicules. Combinez avec la météo X-MAS ou Light Snow pour de meilleurs résultats.", ConfigManager.GetSettingsBool(ConfigManager.Setting.vmenu_enable_snow));
            MenuItem extrasunny = new MenuItem("Très ensoleillé", "Réglez la météo sur ~y~Très ensoleillé~s~!") { ItemData = "EXTRASUNNY" };
            MenuItem clear = new MenuItem("Clair", "Réglez la météo sur ~y~Clair~s~!") { ItemData = "CLEAR" };
            MenuItem neutral = new MenuItem("Neutre", "Réglez la météo sur ~y~Neutre~s~!") { ItemData = "NEUTRAL" };
            MenuItem smog = new MenuItem("Smog", "Réglez la météo sur ~y~Smog~s~!") { ItemData = "SMOG" };
            MenuItem foggy = new MenuItem("Brumeux", "Réglez la météo sur ~y~Brumeux~s~!") { ItemData = "FOGGY" };
            MenuItem clouds = new MenuItem("Nuageux", "Réglez la météo sur ~y~Nuageux~s~!") { ItemData = "CLOUDS" };
            MenuItem overcast = new MenuItem("Temps couvert", "Réglez la météo sur ~y~Temps couvert~s~!") { ItemData = "OVERCAST" };
            MenuItem clearing = new MenuItem("Nettoyage", "Réglez la météo sur ~y~Nettoyage~s~!") { ItemData = "CLEARING" };
            MenuItem rain = new MenuItem("Pluie", "Réglez la météo sur ~y~Pluie~s~!") { ItemData = "RAIN" };
            MenuItem thunder = new MenuItem("Tonnerre", "Réglez la météo sur ~y~Tonnerre~s~!") { ItemData = "THUNDER" };
            MenuItem blizzard = new MenuItem("Blizzard", "Réglez la météo sur ~y~Blizzard~s~!") { ItemData = "BLIZZARD" };
            MenuItem snow = new MenuItem("Neige", "Réglez la météo sur ~y~Neige~s~!") { ItemData = "SNOW" };
            MenuItem snowlight = new MenuItem("Neige légère", "Réglez la météo sur ~y~Neige légère~s~!") { ItemData = "SNOWLIGHT" };
            MenuItem xmas = new MenuItem("Neige de Noël", "Réglez la météo sur ~y~Neige de Noël~s~!") { ItemData = "XMAS" };
            MenuItem halloween = new MenuItem("Halloween", "Réglez la météo sur ~y~Halloween~s~!") { ItemData = "HALLOWEEN" };
            MenuItem removeclouds = new MenuItem("Supprimer tous les nuages", "Supprimez tous les nuages du ciel !");
            MenuItem randomizeclouds = new MenuItem("Randomiser les nuages", "Ajoutez des nuages aléatoires au ciel !");

            if (IsAllowed(Permission.WODynamic))
            {
                menu.AddMenuItem(dynamicWeatherEnabled);
            }
            if (IsAllowed(Permission.WOBlackout))
            {
                menu.AddMenuItem(blackout);
            }
            if (IsAllowed(Permission.WOSetWeather))
            {
                menu.AddMenuItem(snowEnabled);
                menu.AddMenuItem(extrasunny);
                menu.AddMenuItem(clear);
                menu.AddMenuItem(neutral);
                menu.AddMenuItem(smog);
                menu.AddMenuItem(foggy);
                menu.AddMenuItem(clouds);
                menu.AddMenuItem(overcast);
                menu.AddMenuItem(clearing);
                menu.AddMenuItem(rain);
                menu.AddMenuItem(thunder);
                menu.AddMenuItem(blizzard);
                menu.AddMenuItem(snow);
                menu.AddMenuItem(snowlight);
                menu.AddMenuItem(xmas);
                menu.AddMenuItem(halloween);
            }
            if (IsAllowed(Permission.WORandomizeClouds))
            {
                menu.AddMenuItem(randomizeclouds);
            }

            if (IsAllowed(Permission.WORemoveClouds))
            {
                menu.AddMenuItem(removeclouds);
            }

            menu.OnItemSelect += (sender, item, index2) =>
            {
                if (item == removeclouds)
                {
                    ModifyClouds(true);
                }
                else if (item == randomizeclouds)
                {
                    ModifyClouds(false);
                }
                else if (item.ItemData is string weatherType)
                {
                    Notify.Custom($"Le temps sera changé en ~y~{item.Text}~s~. Cela prendra {EventManager.WeatherChangeTime} secondes.");
                    UpdateServerWeather(weatherType, EventManager.IsBlackoutEnabled, EventManager.DynamicWeatherEnabled, EventManager.IsSnowEnabled);
                }
            };

            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == dynamicWeatherEnabled)
                {
                    Notify.Custom($"Les changements météorologiques dynamiques sont maintenant {(_checked ? "~g~activé" : "~r~désactivé")}~s~.");
                    UpdateServerWeather(EventManager.GetServerWeather, EventManager.IsBlackoutEnabled, _checked, EventManager.IsSnowEnabled);
                }
                else if (item == blackout)
                {
                    Notify.Custom($"Le mode Blackout est maintenant {(_checked ? "~g~activé" : "~r~désactivé")}~s~.");
                    UpdateServerWeather(EventManager.GetServerWeather, _checked, EventManager.DynamicWeatherEnabled, EventManager.IsSnowEnabled);
                }
                else if (item == snowEnabled)
                {
                    Notify.Custom($"Les effets de neige seront désormais forcés {(_checked ? "~g~activé" : "~r~désactivé")}~s~.");
                    UpdateServerWeather(EventManager.GetServerWeather, EventManager.IsBlackoutEnabled, EventManager.DynamicWeatherEnabled, _checked);
                }
            };
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
