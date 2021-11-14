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
    public class Recording
    {
        // Variables
        private Menu menu;

        private void CreateMenu()
        {
            // Create the menu.
            menu = new Menu("Enregistrement", "Options d'enregistrement");

            MenuItem startRec = new MenuItem("Démarrer l'enregistrement", "Commencez un nouvel enregistrement de jeu en utilisant l'enregistrement intégré de GTA V.");
            MenuItem stopRec = new MenuItem("Arrêter l'enregistrement", "Arrêtez et sauvegardez votre enregistrement en cours.");
            MenuItem openEditor = new MenuItem("Rockstar Editor", "Ouvrez l'éditeur rockstar, notez que vous pourriez vouloir quitter la session avant de faire cela pour éviter certains problèmes.");
            menu.AddMenuItem(startRec);
            menu.AddMenuItem(stopRec);
            menu.AddMenuItem(openEditor);

            menu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == startRec)
                {
                    if (IsRecording())
                    {
                        Notify.Alert("Vous êtes déjà en train d'enregistrer un clip, vous devez d'abord arrêter l'enregistrement avant de pouvoir le recommencer !");
                    }
                    else
                    {
                        StartRecording(1);
                    }
                }
                else if (item == stopRec)
                {
                    if (!IsRecording())
                    {
                        Notify.Alert("Vous n'êtes PAS en train d'enregistrer un clip, vous devez d'abord commencer à enregistrer avant de pouvoir arrêter et enregistrer un clip.");
                    }
                    else
                    {
                        StopRecordingAndSaveClip();
                    }
                }
                else if (item == openEditor)
                {
                    if (GetSettingsBool(Setting.vmenu_quit_session_in_rockstar_editor))
                    {
                        QuitSession();
                    }
                    ActivateRockstarEditor();
                    // wait for the editor to be closed again.
                    while (IsPauseMenuActive())
                    {
                        await BaseScript.Delay(0);
                    }
                    // then fade in the screen.
                    DoScreenFadeIn(1);
                    Notify.Alert("Vous avez quitté votre session précédente avant d'entrer dans l'éditeur Rockstar. Redémarrez le jeu pour pouvoir rejoindre la session principale du serveur.", true, true);
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
