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
using static vMenuClient.MpPedDataManager;
using static vMenuShared.PermissionsManager;

namespace vMenuClient
{
    public class MpPedCustomization
    {
        // Variables
        private Menu menu;
        public Menu createCharacterMenu = new Menu("Créer un personnage", "Créer un nouveau personnage");
        public Menu savedCharactersMenu = new Menu("vMenu", "Gérer les personnages sauvegardés");
        public Menu inheritanceMenu = new Menu("vMenu", "Options d'héritage des personnages");
        public Menu appearanceMenu = new Menu("vMenu", "Options d'apparence des personnages");
        public Menu faceShapeMenu = new Menu("vMenu", "Options de forme du visage du personnage");
        public Menu tattoosMenu = new Menu("vMenu", "Options de tatouage de personnages");
        public Menu clothesMenu = new Menu("vMenu", "Options de vêtements des personnages");
        public Menu propsMenu = new Menu("vMenu", "Options d'accessoires de personnages");
        private Menu manageSavedCharacterMenu = new Menu("vMenu", "Gérer le personnage MP");

        // Need to be able to disable/enable these buttons from another class.
        internal MenuItem createMaleBtn = new MenuItem("Créer un personnage masculin", "Créez un nouveau personnage masculin.") { Label = "→→→" };
        internal MenuItem createFemaleBtn = new MenuItem("Créer un personnage féminin", "Créez un nouveau personnage féminin.") { Label = "→→→" };
        internal MenuItem editPedBtn = new MenuItem("Modifier un personnage sauvegardé", "Cela vous permet de modifier tout ce qui concerne votre personnage sauvegardé. Les modifications seront enregistrées dans le fichier d'enregistrement de ce personnage dès que vous aurez cliqué sur le bouton d'enregistrement.");

        public static bool DontCloseMenus { get { return MenuController.PreventExitingMenu; } set { MenuController.PreventExitingMenu = value; } }
        public static bool DisableBackButton { get { return MenuController.DisableBackButton; } set { MenuController.DisableBackButton = value; } }
        string selectedSavedCharacterManageName = "";
        private bool isEdidtingPed = false;
        private readonly List<string> facial_expressions = new List<string>() { "mood_Normal_1", "mood_Happy_1", "mood_Angry_1", "mood_Aiming_1", "mood_Injured_1", "mood_stressed_1", "mood_smug_1", "mood_sulk_1", };

        private MultiplayerPedData currentCharacter = new MultiplayerPedData();



        /// <summary>
        /// Makes or updates the character creator menu. Also has an option to load data from the <see cref="currentCharacter"/> data, to allow for editing an existing ped.
        /// </summary>
        /// <param name="male"></param>
        /// <param name="editPed"></param>
        private void MakeCreateCharacterMenu(bool male, bool editPed = false)
        {
            isEdidtingPed = editPed;
            if (!editPed)
            {
                currentCharacter = new MultiplayerPedData();
                currentCharacter.DrawableVariations.clothes = new Dictionary<int, KeyValuePair<int, int>>();
                currentCharacter.PropVariations.props = new Dictionary<int, KeyValuePair<int, int>>();
                currentCharacter.PedHeadBlendData = Game.PlayerPed.GetHeadBlendData();
                currentCharacter.Version = 1;
                currentCharacter.ModelHash = male ? (uint)GetHashKey("mp_m_freemode_01") : (uint)GetHashKey("mp_f_freemode_01");
                currentCharacter.IsMale = male;

                SetPedComponentVariation(Game.PlayerPed.Handle, 3, 15, 0, 0);
                SetPedComponentVariation(Game.PlayerPed.Handle, 8, 15, 0, 0);
                SetPedComponentVariation(Game.PlayerPed.Handle, 11, 15, 0, 0);
            }
            if (currentCharacter.DrawableVariations.clothes == null)
            {
                currentCharacter.DrawableVariations.clothes = new Dictionary<int, KeyValuePair<int, int>>();
            }
            if (currentCharacter.PropVariations.props == null)
            {
                currentCharacter.PropVariations.props = new Dictionary<int, KeyValuePair<int, int>>();
            }

            // Set the facial expression to default in case it doesn't exist yet, or keep the current one if it does.
            currentCharacter.FacialExpression = currentCharacter.FacialExpression ?? facial_expressions[0];

            // Set the facial expression on the ped itself.
            SetFacialIdleAnimOverride(Game.PlayerPed.Handle, currentCharacter.FacialExpression ?? facial_expressions[0], null);

            // Set the facial expression item list to the correct saved index.
            if (createCharacterMenu.GetMenuItems().ElementAt(6) is MenuListItem li)
            {
                int index = facial_expressions.IndexOf(currentCharacter.FacialExpression ?? facial_expressions[0]);
                if (index < 0)
                {
                    index = 0;
                }
                li.ListIndex = index;
            }

            appearanceMenu.ClearMenuItems();
            tattoosMenu.ClearMenuItems();
            clothesMenu.ClearMenuItems();
            propsMenu.ClearMenuItems();

            #region appearance menu.
            List<string> opacity = new List<string>() { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };

            List<string> overlayColorsList = new List<string>();
            for (int i = 0; i < GetNumHairColors(); i++)
            {
                overlayColorsList.Add($"Color #{i + 1}");
            }

            int maxHairStyles = GetNumberOfPedDrawableVariations(Game.PlayerPed.Handle, 2);
            //if (currentCharacter.ModelHash == (uint)PedHash.FreemodeFemale01)
            //{
            //    maxHairStyles /= 2;
            //}
            List<string> hairStylesList = new List<string>();
            for (int i = 0; i < maxHairStyles; i++)
            {
                hairStylesList.Add($"Style #{i + 1}");
            }
            hairStylesList.Add($"Style #{maxHairStyles + 1}");

            List<string> blemishesStyleList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(0); i++)
            {
                blemishesStyleList.Add($"Style #{i + 1}");
            }

            List<string> beardStylesList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(1); i++)
            {
                beardStylesList.Add($"Style #{i + 1}");
            }

            List<string> eyebrowsStyleList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(2); i++)
            {
                eyebrowsStyleList.Add($"Style #{i + 1}");
            }

            List<string> ageingStyleList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(3); i++)
            {
                ageingStyleList.Add($"Style #{i + 1}");
            }

            List<string> makeupStyleList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(4); i++)
            {
                makeupStyleList.Add($"Style #{i + 1}");
            }

            List<string> blushStyleList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(5); i++)
            {
                blushStyleList.Add($"Style #{i + 1}");
            }

            List<string> complexionStyleList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(6); i++)
            {
                complexionStyleList.Add($"Style #{i + 1}");
            }

            List<string> sunDamageStyleList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(7); i++)
            {
                sunDamageStyleList.Add($"Style #{i + 1}");
            }

            List<string> lipstickStyleList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(8); i++)
            {
                lipstickStyleList.Add($"Style #{i + 1}");
            }

            List<string> molesFrecklesStyleList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(9); i++)
            {
                molesFrecklesStyleList.Add($"Style #{i + 1}");
            }

            List<string> chestHairStyleList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(10); i++)
            {
                chestHairStyleList.Add($"Style #{i + 1}");
            }

            List<string> bodyBlemishesList = new List<string>();
            for (int i = 0; i < GetNumHeadOverlayValues(11); i++)
            {
                bodyBlemishesList.Add($"Style #{i + 1}");
            }

            List<string> eyeColorList = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                eyeColorList.Add($"Eye Color #{i + 1}");
            }

            /*

            0               Blemishes             0 - 23,   255  
            1               Facial Hair           0 - 28,   255  
            2               Eyebrows              0 - 33,   255  
            3               Ageing                0 - 14,   255  
            4               Makeup                0 - 74,   255  
            5               Blush                 0 - 6,    255  
            6               Complexion            0 - 11,   255  
            7               Sun Damage            0 - 10,   255  
            8               Lipstick              0 - 9,    255  
            9               Moles/Freckles        0 - 17,   255  
            10              Chest Hair            0 - 16,   255  
            11              Body Blemishes        0 - 11,   255  
            12              Add Body Blemishes    0 - 1,    255  
            
            */


            // hair
            int currentHairStyle = editPed ? currentCharacter.PedAppearance.hairStyle : GetPedDrawableVariation(Game.PlayerPed.Handle, 2);
            int currentHairColor = editPed ? currentCharacter.PedAppearance.hairColor : 0;
            int currentHairHighlightColor = editPed ? currentCharacter.PedAppearance.hairHighlightColor : 0;

            // 0 blemishes
            int currentBlemishesStyle = editPed ? currentCharacter.PedAppearance.blemishesStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 0) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 0) : 0;
            float currentBlemishesOpacity = editPed ? currentCharacter.PedAppearance.blemishesOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 0, currentBlemishesStyle, currentBlemishesOpacity);

            // 1 beard
            int currentBeardStyle = editPed ? currentCharacter.PedAppearance.beardStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 1) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 1) : 0;
            float currentBeardOpacity = editPed ? currentCharacter.PedAppearance.beardOpacity : 0f;
            int currentBeardColor = editPed ? currentCharacter.PedAppearance.beardColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 1, currentBeardStyle, currentBeardOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 1, 1, currentBeardColor, currentBeardColor);

            // 2 eyebrows
            int currentEyebrowStyle = editPed ? currentCharacter.PedAppearance.eyebrowsStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 2) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 2) : 0;
            float currentEyebrowOpacity = editPed ? currentCharacter.PedAppearance.eyebrowsOpacity : 0f;
            int currentEyebrowColor = editPed ? currentCharacter.PedAppearance.eyebrowsColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 2, currentEyebrowStyle, currentEyebrowOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 2, 1, currentEyebrowColor, currentEyebrowColor);

            // 3 ageing
            int currentAgeingStyle = editPed ? currentCharacter.PedAppearance.ageingStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 3) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 3) : 0;
            float currentAgeingOpacity = editPed ? currentCharacter.PedAppearance.ageingOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 3, currentAgeingStyle, currentAgeingOpacity);

            // 4 makeup
            int currentMakeupStyle = editPed ? currentCharacter.PedAppearance.makeupStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 4) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 4) : 0;
            float currentMakeupOpacity = editPed ? currentCharacter.PedAppearance.makeupOpacity : 0f;
            int currentMakeupColor = editPed ? currentCharacter.PedAppearance.makeupColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 4, currentMakeupStyle, currentMakeupOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 4, 2, currentMakeupColor, currentMakeupColor);

            // 5 blush
            int currentBlushStyle = editPed ? currentCharacter.PedAppearance.blushStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 5) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 5) : 0;
            float currentBlushOpacity = editPed ? currentCharacter.PedAppearance.blushOpacity : 0f;
            int currentBlushColor = editPed ? currentCharacter.PedAppearance.blushColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 5, currentBlushStyle, currentBlushOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 5, 2, currentBlushColor, currentBlushColor);

            // 6 complexion
            int currentComplexionStyle = editPed ? currentCharacter.PedAppearance.complexionStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 6) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 6) : 0;
            float currentComplexionOpacity = editPed ? currentCharacter.PedAppearance.complexionOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 6, currentComplexionStyle, currentComplexionOpacity);

            // 7 sun damage
            int currentSunDamageStyle = editPed ? currentCharacter.PedAppearance.sunDamageStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 7) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 7) : 0;
            float currentSunDamageOpacity = editPed ? currentCharacter.PedAppearance.sunDamageOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 7, currentSunDamageStyle, currentSunDamageOpacity);

            // 8 lipstick
            int currentLipstickStyle = editPed ? currentCharacter.PedAppearance.lipstickStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 8) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 8) : 0;
            float currentLipstickOpacity = editPed ? currentCharacter.PedAppearance.lipstickOpacity : 0f;
            int currentLipstickColor = editPed ? currentCharacter.PedAppearance.lipstickColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 8, currentLipstickStyle, currentLipstickOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 8, 2, currentLipstickColor, currentLipstickColor);

            // 9 moles/freckles
            int currentMolesFrecklesStyle = editPed ? currentCharacter.PedAppearance.molesFrecklesStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 9) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 9) : 0;
            float currentMolesFrecklesOpacity = editPed ? currentCharacter.PedAppearance.molesFrecklesOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 9, currentMolesFrecklesStyle, currentMolesFrecklesOpacity);

            // 10 chest hair
            int currentChesthairStyle = editPed ? currentCharacter.PedAppearance.chestHairStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 10) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 10) : 0;
            float currentChesthairOpacity = editPed ? currentCharacter.PedAppearance.chestHairOpacity : 0f;
            int currentChesthairColor = editPed ? currentCharacter.PedAppearance.chestHairColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 10, currentChesthairStyle, currentChesthairOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 10, 1, currentChesthairColor, currentChesthairColor);

            // 11 body blemishes
            int currentBodyBlemishesStyle = editPed ? currentCharacter.PedAppearance.bodyBlemishesStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 11) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 11) : 0;
            float currentBodyBlemishesOpacity = editPed ? currentCharacter.PedAppearance.bodyBlemishesOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 11, currentBodyBlemishesStyle, currentBodyBlemishesOpacity);

            int currentEyeColor = editPed ? currentCharacter.PedAppearance.eyeColor : 0;
            SetPedEyeColor(Game.PlayerPed.Handle, currentEyeColor);

            MenuListItem hairStyles = new MenuListItem("Style de cheveux", hairStylesList, currentHairStyle, "Sélectionnez une coiffure.");
            //MenuListItem hairColors = new MenuListItem("Hair Color", overlayColorsList, currentHairColor, "Select a hair color.");
            MenuListItem hairColors = new MenuListItem("Couleur des cheveux", overlayColorsList, currentHairColor, "Choisissez une couleur de cheveux.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Hair };
            //MenuListItem hairHighlightColors = new MenuListItem("Hair Highlight Color", overlayColorsList, currentHairHighlightColor, "Select a hair highlight color.");
            MenuListItem hairHighlightColors = new MenuListItem("Couleur des mèches de cheveux", overlayColorsList, currentHairHighlightColor, "Sélectionnez une couleur de reflet pour les cheveux.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Hair };

            MenuListItem blemishesStyle = new MenuListItem("Style des imperfections", blemishesStyleList, currentBlemishesStyle, "Sélectionnez un style d'imperfections.");
            //MenuSliderItem blemishesOpacity = new MenuSliderItem("Blemishes Opacity", "Select a blemishes opacity.", 0, 10, (int)(currentBlemishesOpacity * 10f), false);
            MenuListItem blemishesOpacity = new MenuListItem("Opacité des imperfections", opacity, (int)(currentBlemishesOpacity * 10f), "Sélectionnez l'opacité des imperfections") { ShowOpacityPanel = true };

            MenuListItem beardStyles = new MenuListItem("Style de barbe", beardStylesList, currentBeardStyle, "Choisissez un style de barbe/de pilosité faciale.");
            MenuListItem beardOpacity = new MenuListItem("Opacité de la barbe", opacity, (int)(currentBeardOpacity * 10f), "Sélectionnez l'opacité de votre barbe/poils du visage.") { ShowOpacityPanel = true };
            MenuListItem beardColor = new MenuListItem("Couleur de la barbe", overlayColorsList, currentBeardColor, "Sélectionnez une couleur de barbe.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Hair };
            //MenuSliderItem beardOpacity = new MenuSliderItem("Beard Opacity", "Select the opacity for your beard/facial hair.", 0, 10, (int)(currentBeardOpacity * 10f), false);
            //MenuListItem beardColor = new MenuListItem("Beard Color", overlayColorsList, currentBeardColor, "Select a beard color");

            MenuListItem eyebrowStyle = new MenuListItem("Style des sourcils", eyebrowsStyleList, currentEyebrowStyle, "Sélectionnez un style de sourcils.");
            MenuListItem eyebrowOpacity = new MenuListItem("Opacité des sourcils", opacity, (int)(currentEyebrowOpacity * 10f), "Sélectionnez l'opacité pour vos sourcils.") { ShowOpacityPanel = true };
            MenuListItem eyebrowColor = new MenuListItem("Couleur des sourcils", overlayColorsList, currentEyebrowColor, "Sélectionnez une couleur de sourcils.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Hair };
            //MenuSliderItem eyebrowOpacity = new MenuSliderItem("Eyebrows Opacity", "Select the opacity for your eyebrows.", 0, 10, (int)(currentEyebrowOpacity * 10f), false);

            MenuListItem ageingStyle = new MenuListItem("Style de vieillissement", ageingStyleList, currentAgeingStyle, "Sélectionnez un style de vieillissement.");
            MenuListItem ageingOpacity = new MenuListItem("Opacité du vieillissement", opacity, (int)(currentAgeingOpacity * 10f), "Sélectionnez une opacité de vieillissement.") { ShowOpacityPanel = true };
            //MenuSliderItem ageingOpacity = new MenuSliderItem("Ageing Opacity", "Select an ageing opacity.", 0, 10, (int)(currentAgeingOpacity * 10f), false);

            MenuListItem makeupStyle = new MenuListItem("Style de maquillage", makeupStyleList, currentMakeupStyle, "Sélectionnez un style de maquillage.");
            MenuListItem makeupOpacity = new MenuListItem("Opacité du maquillage", opacity, (int)(currentMakeupOpacity * 10f), "Sélectionnez une opacité de maquillage") { ShowOpacityPanel = true };
            //MenuSliderItem makeupOpacity = new MenuSliderItem("Makeup Opacity", 0, 10, (int)(currentMakeupOpacity * 10f), "Select a makeup opacity.");
            MenuListItem makeupColor = new MenuListItem("Couleur du maquillage", overlayColorsList, currentMakeupColor, "Sélectionnez une couleur de maquillage.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Makeup };

            MenuListItem blushStyle = new MenuListItem("Style blush", blushStyleList, currentBlushStyle, "Sélectionnez un style de blush.");
            MenuListItem blushOpacity = new MenuListItem("Opacité du blush", opacity, (int)(currentBlushOpacity * 10f), "Sélectionnez une opacité du blush.") { ShowOpacityPanel = true };
            //MenuSliderItem blushOpacity = new MenuSliderItem("Blush Opacity", 0, 10, (int)(currentBlushOpacity * 10f), "Select a blush opacity.");
            MenuListItem blushColor = new MenuListItem("Couleur blush", overlayColorsList, currentBlushColor, "Sélectionnez une couleur de blush.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Makeup };

            MenuListItem complexionStyle = new MenuListItem("Style de teint", complexionStyleList, currentComplexionStyle, "Sélectionnez un style de teint.");
            //MenuSliderItem complexionOpacity = new MenuSliderItem("Complexion Opacity", 0, 10, (int)(currentComplexionOpacity * 10f), "Select a complexion opacity.");
            MenuListItem complexionOpacity = new MenuListItem("Opacité du teint", opacity, (int)(currentComplexionOpacity * 10f), "Sélectionnez une opacité du teint.") { ShowOpacityPanel = true };

            MenuListItem sunDamageStyle = new MenuListItem("Style des dommages causés par le soleil", sunDamageStyleList, currentSunDamageStyle, "Sélectionnez un style de dommages causés par le soleil.");
            //MenuSliderItem sunDamageOpacity = new MenuSliderItem("Sun Damage Opacity", 0, 10, (int)(currentSunDamageOpacity * 10f), "Select a sun damage opacity.");
            MenuListItem sunDamageOpacity = new MenuListItem("Opacité des dommages causés par le soleil", opacity, (int)(currentSunDamageOpacity * 10f), "Sélectionnez une opacité pour les dommages causés par le soleil.") { ShowOpacityPanel = true };

            MenuListItem lipstickStyle = new MenuListItem("Style rouge à lèvres", lipstickStyleList, currentLipstickStyle, "Sélectionnez un style de rouge à lèvres.");
            //MenuSliderItem lipstickOpacity = new MenuSliderItem("Lipstick Opacity", 0, 10, (int)(currentLipstickOpacity * 10f), "Select a lipstick opacity.");
            MenuListItem lipstickOpacity = new MenuListItem("Opacité du rouge à lèvres", opacity, (int)(currentLipstickOpacity * 10f), "Sélectionnez une opacité de rouge à lèvres.") { ShowOpacityPanel = true };
            MenuListItem lipstickColor = new MenuListItem("Couleur du rouge à lèvres", overlayColorsList, currentLipstickColor, "Choisissez une couleur de rouge à lèvres.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Makeup };

            MenuListItem molesFrecklesStyle = new MenuListItem("Style des taches de rousseur et grains de beauté", molesFrecklesStyleList, currentMolesFrecklesStyle, "Sélectionnez un style de taches de rousseur et de grains de beauté.");
            //MenuSliderItem molesFrecklesOpacity = new MenuSliderItem("Moles and Freckles Opacity", 0, 10, (int)(currentMolesFrecklesOpacity * 10f), "Select a moles and freckles opacity.");
            MenuListItem molesFrecklesOpacity = new MenuListItem("Opacité des taches de rousseur et grains de beauté", opacity, (int)(currentMolesFrecklesOpacity * 10f), "Sélectionnez une opacité des grains de beauté et des taches de rousseur.") { ShowOpacityPanel = true };

            MenuListItem chestHairStyle = new MenuListItem("Style de cheveux de la poitrine", chestHairStyleList, currentChesthairStyle, "Choisissez une coiffure pour la poitrine.");
            //MenuSliderItem chestHairOpacity = new MenuSliderItem("Chest Hair Opacity", 0, 10, (int)(currentChesthairOpacity * 10f), "Select a chest hair opacity.");
            MenuListItem chestHairOpacity = new MenuListItem("Opacité des poils du torse", opacity, (int)(currentChesthairOpacity * 10f), "Sélectionnez une opacité de poils de poitrine.") { ShowOpacityPanel = true };
            MenuListItem chestHairColor = new MenuListItem("Couleur des poils du torse", overlayColorsList, currentChesthairColor, "Choisissez la couleur des poils du torse.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Hair };

            // Body blemishes
            MenuListItem bodyBlemishesStyle = new MenuListItem("Style des imperfections du corps", bodyBlemishesList, currentBodyBlemishesStyle, "Sélectionnez le style des taches sur le corps.");
            MenuListItem bodyBlemishesOpacity = new MenuListItem("Opacité des imperfections du corps", opacity, (int)(currentBodyBlemishesOpacity * 10f), "Sélectionnez l'opacité des taches sur le corps.") { ShowOpacityPanel = true };

            MenuListItem eyeColor = new MenuListItem("Couleurs des yeux", eyeColorList, currentEyeColor, "Sélectionnez une couleur d'yeux/de lentilles de contact.");

            appearanceMenu.AddMenuItem(hairStyles);
            appearanceMenu.AddMenuItem(hairColors);
            appearanceMenu.AddMenuItem(hairHighlightColors);

            appearanceMenu.AddMenuItem(blemishesStyle);
            appearanceMenu.AddMenuItem(blemishesOpacity);

            appearanceMenu.AddMenuItem(beardStyles);
            appearanceMenu.AddMenuItem(beardOpacity);
            appearanceMenu.AddMenuItem(beardColor);

            appearanceMenu.AddMenuItem(eyebrowStyle);
            appearanceMenu.AddMenuItem(eyebrowOpacity);
            appearanceMenu.AddMenuItem(eyebrowColor);

            appearanceMenu.AddMenuItem(ageingStyle);
            appearanceMenu.AddMenuItem(ageingOpacity);

            appearanceMenu.AddMenuItem(makeupStyle);
            appearanceMenu.AddMenuItem(makeupOpacity);
            appearanceMenu.AddMenuItem(makeupColor);

            appearanceMenu.AddMenuItem(blushStyle);
            appearanceMenu.AddMenuItem(blushOpacity);
            appearanceMenu.AddMenuItem(blushColor);

            appearanceMenu.AddMenuItem(complexionStyle);
            appearanceMenu.AddMenuItem(complexionOpacity);

            appearanceMenu.AddMenuItem(sunDamageStyle);
            appearanceMenu.AddMenuItem(sunDamageOpacity);

            appearanceMenu.AddMenuItem(lipstickStyle);
            appearanceMenu.AddMenuItem(lipstickOpacity);
            appearanceMenu.AddMenuItem(lipstickColor);

            appearanceMenu.AddMenuItem(molesFrecklesStyle);
            appearanceMenu.AddMenuItem(molesFrecklesOpacity);

            appearanceMenu.AddMenuItem(chestHairStyle);
            appearanceMenu.AddMenuItem(chestHairOpacity);
            appearanceMenu.AddMenuItem(chestHairColor);

            appearanceMenu.AddMenuItem(bodyBlemishesStyle);
            appearanceMenu.AddMenuItem(bodyBlemishesOpacity);

            appearanceMenu.AddMenuItem(eyeColor);

            if (male)
            {
                // There are weird people out there that wanted makeup for male characters
                // so yeah.... here you go I suppose... strange...


                makeupStyle.Enabled = false;
                makeupStyle.LeftIcon = MenuItem.Icon.LOCK;
                makeupStyle.Description = "Ceci n'est pas disponible pour les personnages masculins.";

                makeupOpacity.Enabled = false;
                makeupOpacity.LeftIcon = MenuItem.Icon.LOCK;
                makeupOpacity.Description = "Ceci n'est pas disponible pour les personnages masculins.";

                makeupColor.Enabled = false;
                makeupColor.LeftIcon = MenuItem.Icon.LOCK;
                makeupColor.Description = "Ceci n'est pas disponible pour les personnages masculins.";


                blushStyle.Enabled = false;
                blushStyle.LeftIcon = MenuItem.Icon.LOCK;
                blushStyle.Description = "Ceci n'est pas disponible pour les personnages masculins.";

                blushOpacity.Enabled = false;
                blushOpacity.LeftIcon = MenuItem.Icon.LOCK;
                blushOpacity.Description = "Ceci n'est pas disponible pour les personnages masculins.";

                blushColor.Enabled = false;
                blushColor.LeftIcon = MenuItem.Icon.LOCK;
                blushColor.Description = "Ceci n'est pas disponible pour les personnages masculins.";


                lipstickStyle.Enabled = false;
                lipstickStyle.LeftIcon = MenuItem.Icon.LOCK;
                lipstickStyle.Description = "Ceci n'est pas disponible pour les personnages masculins.";

                lipstickOpacity.Enabled = false;
                lipstickOpacity.LeftIcon = MenuItem.Icon.LOCK;
                lipstickOpacity.Description = "Ceci n'est pas disponible pour les personnages masculins.";

                lipstickColor.Enabled = false;
                lipstickColor.LeftIcon = MenuItem.Icon.LOCK;
                lipstickColor.Description = "Ceci n'est pas disponible pour les personnages masculins.";

            }
            else
            {
                beardStyles.Enabled = false;
                beardStyles.LeftIcon = MenuItem.Icon.LOCK;
                beardStyles.Description = "Ceci n'est pas disponible pour les personnages féminins.";

                beardOpacity.Enabled = false;
                beardOpacity.LeftIcon = MenuItem.Icon.LOCK;
                beardOpacity.Description = "Ceci n'est pas disponible pour les personnages féminins.";

                beardColor.Enabled = false;
                beardColor.LeftIcon = MenuItem.Icon.LOCK;
                beardColor.Description = "Ceci n'est pas disponible pour les personnages féminins.";


                chestHairStyle.Enabled = false;
                chestHairStyle.LeftIcon = MenuItem.Icon.LOCK;
                chestHairStyle.Description = "Ceci n'est pas disponible pour les personnages féminins.";

                chestHairOpacity.Enabled = false;
                chestHairOpacity.LeftIcon = MenuItem.Icon.LOCK;
                chestHairOpacity.Description = "Ceci n'est pas disponible pour les personnages féminins.";

                chestHairColor.Enabled = false;
                chestHairColor.LeftIcon = MenuItem.Icon.LOCK;
                chestHairColor.Description = "Ceci n'est pas disponible pour les personnages féminins.";
            }

            #endregion

            #region clothing options menu
            string[] clothingCategoryNames = new string[12] { "Non utilisé (tête)", "Masques", "Non utilisé (cheveux)", "Haut du corps", "Bas du corps", "Sacs et parachutes", "Chaussures", "Écharpes et chaînes", "T-shirt et accessoires", "Gilets pare-balles et accessoires 2", "Badges et logos", "Chemises superposées et vestes" };
            for (int i = 0; i < 12; i++)
            {
                if (i != 0 && i != 2)
                {
                    int currentVariationIndex = editPed && currentCharacter.DrawableVariations.clothes.ContainsKey(i) ? currentCharacter.DrawableVariations.clothes[i].Key : GetPedDrawableVariation(Game.PlayerPed.Handle, i);
                    int currentVariationTextureIndex = editPed && currentCharacter.DrawableVariations.clothes.ContainsKey(i) ? currentCharacter.DrawableVariations.clothes[i].Value : GetPedTextureVariation(Game.PlayerPed.Handle, i);

                    int maxDrawables = GetNumberOfPedDrawableVariations(Game.PlayerPed.Handle, i);

                    List<string> items = new List<string>();
                    for (int x = 0; x < maxDrawables; x++)
                    {
                        items.Add($"Drawable #{x} (sur {maxDrawables})");
                    }

                    int maxTextures = GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, i, currentVariationIndex);

                    MenuListItem listItem = new MenuListItem(clothingCategoryNames[i], items, currentVariationIndex, $"Sélectionnez un objet à dessiner en utilisant les touches fléchées et appuyez sur ~o~entrée~s~ pour faire défiler toutes les textures disponibles. Texture actuellement sélectionnée : #{currentVariationTextureIndex + 1} (sur {maxTextures}).");
                    clothesMenu.AddMenuItem(listItem);
                }
            }
            #endregion

            #region props options menu
            string[] propNames = new string[5] { "Chapeaux et casques", "Lunettes", "Accessoires divers", "Montres", "Bracelets" };
            for (int x = 0; x < 5; x++)
            {
                int propId = x;
                if (x > 2)
                {
                    propId += 3;
                }

                int currentProp = editPed && currentCharacter.PropVariations.props.ContainsKey(propId) ? currentCharacter.PropVariations.props[propId].Key : GetPedPropIndex(Game.PlayerPed.Handle, propId);
                int currentPropTexture = editPed && currentCharacter.PropVariations.props.ContainsKey(propId) ? currentCharacter.PropVariations.props[propId].Value : GetPedPropTextureIndex(Game.PlayerPed.Handle, propId);

                List<string> propsList = new List<string>();
                for (int i = 0; i < GetNumberOfPedPropDrawableVariations(Game.PlayerPed.Handle, propId); i++)
                {
                    propsList.Add($"Prop #{i} (sur {GetNumberOfPedPropDrawableVariations(Game.PlayerPed.Handle, propId)})");
                }
                propsList.Add("Pas de Prop");


                if (GetPedPropIndex(Game.PlayerPed.Handle, propId) != -1)
                {
                    int maxPropTextures = GetNumberOfPedPropTextureVariations(Game.PlayerPed.Handle, propId, currentProp);
                    MenuListItem propListItem = new MenuListItem($"{propNames[x]}", propsList, currentProp, $"Sélectionnez un accessoire en utilisant les touches fléchées et appuyez sur ~o~entrée~s~ pour faire défiler toutes les textures disponibles. Texture actuellement sélectionnée : #{currentPropTexture + 1} (sur {maxPropTextures}).");
                    propsMenu.AddMenuItem(propListItem);
                }
                else
                {
                    MenuListItem propListItem = new MenuListItem($"{propNames[x]}", propsList, currentProp, "Sélectionnez un accessoire en utilisant les touches fléchées et appuyez sur ~o~enter~s~ pour faire défiler toutes les textures disponibles.");
                    propsMenu.AddMenuItem(propListItem);
                }


            }
            #endregion

            #region face features menu
            foreach (MenuSliderItem item in faceShapeMenu.GetMenuItems())
            {
                if (editPed)
                {
                    if (currentCharacter.FaceShapeFeatures.features == null)
                    {
                        currentCharacter.FaceShapeFeatures.features = new Dictionary<int, float>();
                    }
                    else
                    {
                        if (currentCharacter.FaceShapeFeatures.features.ContainsKey(item.Index))
                        {
                            item.Position = (int)(currentCharacter.FaceShapeFeatures.features[item.Index] * 10f) + 10;
                            SetPedFaceFeature(Game.PlayerPed.Handle, item.Index, currentCharacter.FaceShapeFeatures.features[item.Index]);
                        }
                        else
                        {
                            item.Position = 10;
                            SetPedFaceFeature(Game.PlayerPed.Handle, item.Index, 0f);
                        }
                    }
                }
                else
                {
                    item.Position = 10;
                    SetPedFaceFeature(Game.PlayerPed.Handle, item.Index, 0f);
                }
            }
            #endregion

            #region Tattoos menu
            List<string> headTattoosList = new List<string>();
            List<string> torsoTattoosList = new List<string>();
            List<string> leftArmTattoosList = new List<string>();
            List<string> rightArmTattoosList = new List<string>();
            List<string> leftLegTattoosList = new List<string>();
            List<string> rightLegTattoosList = new List<string>();
            List<string> badgeTattoosList = new List<string>();

            TattoosData.GenerateTattoosData();
            if (male)
            {
                int counter = 1;
                foreach (var tattoo in MaleTattoosCollection.HEAD)
                {
                    headTattoosList.Add($"Tattoo #{counter} (sur {MaleTattoosCollection.HEAD.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.TORSO)
                {
                    torsoTattoosList.Add($"Tattoo #{counter} (sur {MaleTattoosCollection.TORSO.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.LEFT_ARM)
                {
                    leftArmTattoosList.Add($"Tattoo #{counter} (sur {MaleTattoosCollection.LEFT_ARM.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.RIGHT_ARM)
                {
                    rightArmTattoosList.Add($"Tattoo #{counter} (sur {MaleTattoosCollection.RIGHT_ARM.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.LEFT_LEG)
                {
                    leftLegTattoosList.Add($"Tattoo #{counter} (sur {MaleTattoosCollection.LEFT_LEG.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.RIGHT_LEG)
                {
                    rightLegTattoosList.Add($"Tattoo #{counter} (sur {MaleTattoosCollection.RIGHT_LEG.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.BADGES)
                {
                    badgeTattoosList.Add($"Badge #{counter} (sur {MaleTattoosCollection.BADGES.Count})");
                    counter++;
                }
            }
            else
            {
                int counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.HEAD)
                {
                    headTattoosList.Add($"Tattoo #{counter} (sur {FemaleTattoosCollection.HEAD.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.TORSO)
                {
                    torsoTattoosList.Add($"Tattoo #{counter} (sur {FemaleTattoosCollection.TORSO.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.LEFT_ARM)
                {
                    leftArmTattoosList.Add($"Tattoo #{counter} (sur {FemaleTattoosCollection.LEFT_ARM.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.RIGHT_ARM)
                {
                    rightArmTattoosList.Add($"Tattoo #{counter} (sur {FemaleTattoosCollection.RIGHT_ARM.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.LEFT_LEG)
                {
                    leftLegTattoosList.Add($"Tattoo #{counter} (sur {FemaleTattoosCollection.LEFT_LEG.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.RIGHT_LEG)
                {
                    rightLegTattoosList.Add($"Tattoo #{counter} (sur {FemaleTattoosCollection.RIGHT_LEG.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.BADGES)
                {
                    badgeTattoosList.Add($"Badge #{counter} (sur {FemaleTattoosCollection.BADGES.Count})");
                    counter++;
                }
            }

            const string tatDesc = "Cycle through the list to preview tattoos. If you like one, press enter to select it, selecting it will add the tattoo if you don't already have it. If you already have that tattoo then the tattoo will be removed.";
            MenuListItem headTatts = new MenuListItem("Head Tattoos", headTattoosList, 0, tatDesc);
            MenuListItem torsoTatts = new MenuListItem("Torso Tattoos", torsoTattoosList, 0, tatDesc);
            MenuListItem leftArmTatts = new MenuListItem("Left Arm Tattoos", leftArmTattoosList, 0, tatDesc);
            MenuListItem rightArmTatts = new MenuListItem("Right Arm Tattoos", rightArmTattoosList, 0, tatDesc);
            MenuListItem leftLegTatts = new MenuListItem("Left Leg Tattoos", leftLegTattoosList, 0, tatDesc);
            MenuListItem rightLegTatts = new MenuListItem("Right Leg Tattoos", rightLegTattoosList, 0, tatDesc);
            MenuListItem badgeTatts = new MenuListItem("Badge Overlays", badgeTattoosList, 0, tatDesc);

            tattoosMenu.AddMenuItem(headTatts);
            tattoosMenu.AddMenuItem(torsoTatts);
            tattoosMenu.AddMenuItem(leftArmTatts);
            tattoosMenu.AddMenuItem(rightArmTatts);
            tattoosMenu.AddMenuItem(leftLegTatts);
            tattoosMenu.AddMenuItem(rightLegTatts);
            tattoosMenu.AddMenuItem(badgeTatts);
            tattoosMenu.AddMenuItem(new MenuItem("Remove All Tattoos", "Click this if you want to remove all tattoos and start over."));
            #endregion

            createCharacterMenu.RefreshIndex();
            appearanceMenu.RefreshIndex();
            inheritanceMenu.RefreshIndex();
            tattoosMenu.RefreshIndex();
        }

        /// <summary>
        /// Saves the mp character and quits the editor if successful.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SavePed()
        {
            currentCharacter.PedHeadBlendData = Game.PlayerPed.GetHeadBlendData();
            if (isEdidtingPed)
            {
                string json = JsonConvert.SerializeObject(currentCharacter);
                if (StorageManager.SaveJsonData(currentCharacter.SaveName, json, true))
                {
                    Notify.Success("Votre personnage a été sauvegardé avec succès.");
                    return true;
                }
                else
                {
                    Notify.Error("Votre personnage n'a pas pu être sauvegardé. Raison inconnue : (");
                    return false;
                }
            }
            else
            {
                string name = await GetUserInput(windowTitle: "Entrez un nom de sauvegarde.", maxInputLength: 30);
                if (string.IsNullOrEmpty(name))
                {
                    Notify.Error(CommonErrors.InvalidInput);
                    return false;
                }
                else
                {
                    currentCharacter.SaveName = "mp_ped_" + name;
                    string json = JsonConvert.SerializeObject(currentCharacter);

                    if (StorageManager.SaveJsonData("mp_ped_" + name, json, false))
                    {
                        Notify.Success($"Votre personnage (~g~<C>{name}</C>~s~) a été sauvegardé.");
                        Log($"Personnage sauvegardé {name}. Data: {json}");
                        return true;
                    }
                    else
                    {
                        Notify.Error($"L'enregistrement a échoué, très probablement parce que ce nom (~y~<C>{name}</C>~s~) est déjà utilisé.");
                        return false;
                    }
                }
            }

        }

        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            // Create the menu.
            menu = new Menu(Game.Player.Name, "Personnalisation du ped");

            MenuItem savedCharacters = new MenuItem("Personnages sauvegardés", "Créez, modifiez ou supprimez vos personnages multijoueurs sauvegardés.")
            {
                Label = "→→→"
            };

            MenuController.AddMenu(createCharacterMenu);
            MenuController.AddMenu(savedCharactersMenu);
            MenuController.AddMenu(inheritanceMenu);
            MenuController.AddMenu(appearanceMenu);
            MenuController.AddMenu(faceShapeMenu);
            MenuController.AddMenu(tattoosMenu);
            MenuController.AddMenu(clothesMenu);
            MenuController.AddMenu(propsMenu);

            CreateSavedPedsMenu();

            menu.AddMenuItem(createMaleBtn);
            MenuController.BindMenuItem(menu, createCharacterMenu, createMaleBtn);
            menu.AddMenuItem(createFemaleBtn);
            MenuController.BindMenuItem(menu, createCharacterMenu, createFemaleBtn);
            menu.AddMenuItem(savedCharacters);
            MenuController.BindMenuItem(menu, savedCharactersMenu, savedCharacters);

            menu.RefreshIndex();

            createCharacterMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            inheritanceMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            appearanceMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            faceShapeMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            tattoosMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            clothesMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            propsMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");

            createCharacterMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            inheritanceMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            appearanceMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            faceShapeMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            tattoosMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            clothesMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            propsMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");

            createCharacterMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            inheritanceMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            appearanceMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            faceShapeMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            tattoosMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            clothesMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            propsMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");

            createCharacterMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            inheritanceMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            appearanceMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            faceShapeMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            tattoosMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            clothesMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            propsMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");


            MenuItem inheritanceButton = new MenuItem("Héritage du personnage", "Options d'héritage du personnage.");
            MenuItem appearanceButton = new MenuItem("Apparence du personnage", "Options d'apparence du personnage.");
            MenuItem faceButton = new MenuItem("Options de forme du visage du personnage", "Options de forme du visage du personnage.");
            MenuItem tattoosButton = new MenuItem("Options de tatouage de personnages", "Options de tatouage du personnage.");
            MenuItem clothesButton = new MenuItem("Vêtements du personnage", "Vêtements pour personnages.");
            MenuItem propsButton = new MenuItem("Accessoires du personnage", "Accessoires du personnage.");
            MenuItem saveButton = new MenuItem("Sauvegarder le personnage", "Sauvegardez votre personnage.");
            MenuItem exitNoSave = new MenuItem("Quitter sans sauvegarder", "En êtes-vous sûr ? Tout travail non sauvé sera perdu.");
            MenuListItem faceExpressionList = new MenuListItem("Expression faciale", new List<string> { "Normal", "Joyeux", "En colère", "Aiming", "Blessé", "Stressé", "Smug", "Sulk" }, 0, "Définissez une expression faciale qui sera utilisée chaque fois que votre ped est immobile.");

            inheritanceButton.Label = "→→→";
            appearanceButton.Label = "→→→";
            faceButton.Label = "→→→";
            tattoosButton.Label = "→→→";
            clothesButton.Label = "→→→";
            propsButton.Label = "→→→";

            createCharacterMenu.AddMenuItem(inheritanceButton);
            createCharacterMenu.AddMenuItem(appearanceButton);
            createCharacterMenu.AddMenuItem(faceButton);
            createCharacterMenu.AddMenuItem(tattoosButton);
            createCharacterMenu.AddMenuItem(clothesButton);
            createCharacterMenu.AddMenuItem(propsButton);
            createCharacterMenu.AddMenuItem(faceExpressionList);
            createCharacterMenu.AddMenuItem(saveButton);
            createCharacterMenu.AddMenuItem(exitNoSave);

            MenuController.BindMenuItem(createCharacterMenu, inheritanceMenu, inheritanceButton);
            MenuController.BindMenuItem(createCharacterMenu, appearanceMenu, appearanceButton);
            MenuController.BindMenuItem(createCharacterMenu, faceShapeMenu, faceButton);
            MenuController.BindMenuItem(createCharacterMenu, tattoosMenu, tattoosButton);
            MenuController.BindMenuItem(createCharacterMenu, clothesMenu, clothesButton);
            MenuController.BindMenuItem(createCharacterMenu, propsMenu, propsButton);

            #region inheritance
            List<string> parents = new List<string>();
            for (int i = 0; i < 46; i++)
            {
                parents.Add($"#{i}");
            }

            var inheritanceDads = new MenuListItem("Père", parents, 0, "Sélectionnez un père.");
            var inheritanceMoms = new MenuListItem("Mère", parents, 0, "Sélectionnez une mère.");
            List<float> mixValues = new List<float>() { 0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f };
            var inheritanceShapeMix = new MenuSliderItem("Mélange de formes de tête", "Sélectionnez la part de la forme de votre tête qui doit être héritée de votre père ou de votre mère. Tout à gauche, c'est votre père, tout à droite, c'est votre mère.", 0, 10, 5, true) { SliderLeftIcon = MenuItem.Icon.MALE, SliderRightIcon = MenuItem.Icon.FEMALE };
            var inheritanceSkinMix = new MenuSliderItem("Mélange de peau pour le corps", "Choisissez la proportion de votre teint qui doit être héritée de votre père ou de votre mère. Tout à gauche, c'est votre père, tout à droite, c'est votre mère.", 0, 10, 5, true) { SliderLeftIcon = MenuItem.Icon.MALE, SliderRightIcon = MenuItem.Icon.FEMALE };

            inheritanceMenu.AddMenuItem(inheritanceDads);
            inheritanceMenu.AddMenuItem(inheritanceMoms);
            inheritanceMenu.AddMenuItem(inheritanceShapeMix);
            inheritanceMenu.AddMenuItem(inheritanceSkinMix);

            void SetHeadBlend()
            {
                SetPedHeadBlendData(Game.PlayerPed.Handle, inheritanceDads.ListIndex, inheritanceMoms.ListIndex, 0, inheritanceDads.ListIndex, inheritanceMoms.ListIndex, 0, mixValues[inheritanceShapeMix.Position], mixValues[inheritanceSkinMix.Position], 0f, false);
            }

            inheritanceMenu.OnListIndexChange += (_menu, listItem, oldSelectionIndex, newSelectionIndex, itemIndex) =>
            {
                SetHeadBlend();
            };

            inheritanceMenu.OnSliderPositionChange += (sender, item, oldPosition, newPosition, itemIndex) =>
            {
                SetHeadBlend();
            };
            #endregion

            #region appearance
            Dictionary<int, KeyValuePair<string, string>> hairOverlays = new Dictionary<int, KeyValuePair<string, string>>()
            {
                { 0, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_a") },
                { 1, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 2, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 3, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_003_a") },
                { 4, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 5, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 6, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 7, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 8, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_008_a") },
                { 9, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 10, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 11, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 12, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 13, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 14, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_long_a") },
                { 15, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_long_a") },
                { 16, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
                { 17, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_a") },
                { 18, new KeyValuePair<string, string>("mpbusiness_overlays", "FM_Bus_M_Hair_000_a") },
                { 19, new KeyValuePair<string, string>("mpbusiness_overlays", "FM_Bus_M_Hair_001_a") },
                { 20, new KeyValuePair<string, string>("mphipster_overlays", "FM_Hip_M_Hair_000_a") },
                { 21, new KeyValuePair<string, string>("mphipster_overlays", "FM_Hip_M_Hair_001_a") },
                { 22, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_a") },
            };

            // manage the list changes for appearance items.
            appearanceMenu.OnListIndexChange += (_menu, listItem, oldSelectionIndex, newSelectionIndex, itemIndex) =>
            {
                if (itemIndex == 0) // hair style
                {
                    ClearPedFacialDecorations(Game.PlayerPed.Handle);
                    currentCharacter.PedAppearance.HairOverlay = new KeyValuePair<string, string>("", "");

                    if (newSelectionIndex >= GetNumberOfPedDrawableVariations(Game.PlayerPed.Handle, 2))
                    {
                        SetPedComponentVariation(Game.PlayerPed.Handle, 2, 0, 0, 0);
                        currentCharacter.PedAppearance.hairStyle = 0;
                    }
                    else
                    {
                        SetPedComponentVariation(Game.PlayerPed.Handle, 2, newSelectionIndex, 0, 0);
                        currentCharacter.PedAppearance.hairStyle = newSelectionIndex;
                        if (hairOverlays.ContainsKey(newSelectionIndex))
                        {
                            SetPedFacialDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(hairOverlays[newSelectionIndex].Key), (uint)GetHashKey(hairOverlays[newSelectionIndex].Value));
                            currentCharacter.PedAppearance.HairOverlay = new KeyValuePair<string, string>(hairOverlays[newSelectionIndex].Key, hairOverlays[newSelectionIndex].Value);
                        }
                    }
                }
                else if (itemIndex == 1 || itemIndex == 2) // hair colors
                {
                    var tmp = (MenuListItem)_menu.GetMenuItems()[1];
                    int hairColor = tmp.ListIndex;
                    tmp = (MenuListItem)_menu.GetMenuItems()[2];
                    int hairHighlightColor = tmp.ListIndex;

                    SetPedHairColor(Game.PlayerPed.Handle, hairColor, hairHighlightColor);

                    currentCharacter.PedAppearance.hairColor = hairColor;
                    currentCharacter.PedAppearance.hairHighlightColor = hairHighlightColor;
                }
                else if (itemIndex == 33) // eye color
                {
                    int selection = ((MenuListItem)_menu.GetMenuItems()[itemIndex]).ListIndex;
                    SetPedEyeColor(Game.PlayerPed.Handle, selection);
                    currentCharacter.PedAppearance.eyeColor = selection;
                }
                else
                {
                    int selection = ((MenuListItem)_menu.GetMenuItems()[itemIndex]).ListIndex;
                    float opacity = 0f;
                    if (_menu.GetMenuItems()[itemIndex + 1] is MenuListItem)
                        opacity = (((float)((MenuListItem)_menu.GetMenuItems()[itemIndex + 1]).ListIndex + 1) / 10f) - 0.1f;
                    else if (_menu.GetMenuItems()[itemIndex - 1] is MenuListItem)
                        opacity = (((float)((MenuListItem)_menu.GetMenuItems()[itemIndex - 1]).ListIndex + 1) / 10f) - 0.1f;
                    else if (_menu.GetMenuItems()[itemIndex] is MenuListItem)
                        opacity = (((float)((MenuListItem)_menu.GetMenuItems()[itemIndex]).ListIndex + 1) / 10f) - 0.1f;
                    else
                        opacity = 1f;
                    switch (itemIndex)
                    {
                        case 3: // blemishes
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 0, selection, opacity);
                            currentCharacter.PedAppearance.blemishesStyle = selection;
                            currentCharacter.PedAppearance.blemishesOpacity = opacity;
                            break;
                        case 5: // beards
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 1, selection, opacity);
                            currentCharacter.PedAppearance.beardStyle = selection;
                            currentCharacter.PedAppearance.beardOpacity = opacity;
                            break;
                        case 7: // beards color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 1, 1, selection, selection);
                            currentCharacter.PedAppearance.beardColor = selection;
                            break;
                        case 8: // eyebrows
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 2, selection, opacity);
                            currentCharacter.PedAppearance.eyebrowsStyle = selection;
                            currentCharacter.PedAppearance.eyebrowsOpacity = opacity;
                            break;
                        case 10: // eyebrows color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 2, 1, selection, selection);
                            currentCharacter.PedAppearance.eyebrowsColor = selection;
                            break;
                        case 11: // ageing
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 3, selection, opacity);
                            currentCharacter.PedAppearance.ageingStyle = selection;
                            currentCharacter.PedAppearance.ageingOpacity = opacity;
                            break;
                        case 13: // makeup
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 4, selection, opacity);
                            currentCharacter.PedAppearance.makeupStyle = selection;
                            currentCharacter.PedAppearance.makeupOpacity = opacity;
                            break;
                        case 15: // makeup color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 4, 2, selection, selection);
                            currentCharacter.PedAppearance.makeupColor = selection;
                            break;
                        case 16: // blush style
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 5, selection, opacity);
                            currentCharacter.PedAppearance.blushStyle = selection;
                            currentCharacter.PedAppearance.blushOpacity = opacity;
                            break;
                        case 18: // blush color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 5, 2, selection, selection);
                            currentCharacter.PedAppearance.blushColor = selection;
                            break;
                        case 19: // complexion
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 6, selection, opacity);
                            currentCharacter.PedAppearance.complexionStyle = selection;
                            currentCharacter.PedAppearance.complexionOpacity = opacity;
                            break;
                        case 21: // sun damage
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 7, selection, opacity);
                            currentCharacter.PedAppearance.sunDamageStyle = selection;
                            currentCharacter.PedAppearance.sunDamageOpacity = opacity;
                            break;
                        case 23: // lipstick
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 8, selection, opacity);
                            currentCharacter.PedAppearance.lipstickStyle = selection;
                            currentCharacter.PedAppearance.lipstickOpacity = opacity;
                            break;
                        case 25: // lipstick color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 8, 2, selection, selection);
                            currentCharacter.PedAppearance.lipstickColor = selection;
                            break;
                        case 26: // moles and freckles
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 9, selection, opacity);
                            currentCharacter.PedAppearance.molesFrecklesStyle = selection;
                            currentCharacter.PedAppearance.molesFrecklesOpacity = opacity;
                            break;
                        case 28: // chest hair
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 10, selection, opacity);
                            currentCharacter.PedAppearance.chestHairStyle = selection;
                            currentCharacter.PedAppearance.chestHairOpacity = opacity;
                            break;
                        case 30: // chest hair color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 10, 1, selection, selection);
                            currentCharacter.PedAppearance.chestHairColor = selection;
                            break;
                        case 31: // body blemishes
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 11, selection, opacity);
                            currentCharacter.PedAppearance.bodyBlemishesStyle = selection;
                            currentCharacter.PedAppearance.bodyBlemishesOpacity = opacity;
                            break;
                    }
                }
            };

            // manage the slider changes for opacity on the appearance items.
            appearanceMenu.OnListIndexChange += (_menu, listItem, oldSelectionIndex, newSelectionIndex, itemIndex) =>
            {
                if (itemIndex > 2 && itemIndex < 33)
                {

                    int selection = ((MenuListItem)_menu.GetMenuItems()[itemIndex - 1]).ListIndex;
                    float opacity = 0f;
                    if (_menu.GetMenuItems()[itemIndex] is MenuListItem)
                        opacity = (((float)((MenuListItem)_menu.GetMenuItems()[itemIndex]).ListIndex + 1) / 10f) - 0.1f;
                    else if (_menu.GetMenuItems()[itemIndex + 1] is MenuListItem)
                        opacity = (((float)((MenuListItem)_menu.GetMenuItems()[itemIndex + 1]).ListIndex + 1) / 10f) - 0.1f;
                    else if (_menu.GetMenuItems()[itemIndex - 1] is MenuListItem)
                        opacity = (((float)((MenuListItem)_menu.GetMenuItems()[itemIndex - 1]).ListIndex + 1) / 10f) - 0.1f;
                    else
                        opacity = 1f;
                    switch (itemIndex)
                    {
                        case 4: // blemishes
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 0, selection, opacity);
                            currentCharacter.PedAppearance.blemishesStyle = selection;
                            currentCharacter.PedAppearance.blemishesOpacity = opacity;
                            break;
                        case 6: // beards
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 1, selection, opacity);
                            currentCharacter.PedAppearance.beardStyle = selection;
                            currentCharacter.PedAppearance.beardOpacity = opacity;
                            break;
                        case 9: // eyebrows
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 2, selection, opacity);
                            currentCharacter.PedAppearance.eyebrowsStyle = selection;
                            currentCharacter.PedAppearance.eyebrowsOpacity = opacity;
                            break;
                        case 12: // ageing
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 3, selection, opacity);
                            currentCharacter.PedAppearance.ageingStyle = selection;
                            currentCharacter.PedAppearance.ageingOpacity = opacity;
                            break;
                        case 14: // makeup
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 4, selection, opacity);
                            currentCharacter.PedAppearance.makeupStyle = selection;
                            currentCharacter.PedAppearance.makeupOpacity = opacity;
                            break;
                        case 17: // blush style
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 5, selection, opacity);
                            currentCharacter.PedAppearance.blushStyle = selection;
                            currentCharacter.PedAppearance.blushOpacity = opacity;
                            break;
                        case 20: // complexion
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 6, selection, opacity);
                            currentCharacter.PedAppearance.complexionStyle = selection;
                            currentCharacter.PedAppearance.complexionOpacity = opacity;
                            break;
                        case 22: // sun damage
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 7, selection, opacity);
                            currentCharacter.PedAppearance.sunDamageStyle = selection;
                            currentCharacter.PedAppearance.sunDamageOpacity = opacity;
                            break;
                        case 24: // lipstick
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 8, selection, opacity);
                            currentCharacter.PedAppearance.lipstickStyle = selection;
                            currentCharacter.PedAppearance.lipstickOpacity = opacity;
                            break;
                        case 27: // moles and freckles
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 9, selection, opacity);
                            currentCharacter.PedAppearance.molesFrecklesStyle = selection;
                            currentCharacter.PedAppearance.molesFrecklesOpacity = opacity;
                            break;
                        case 29: // chest hair
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 10, selection, opacity);
                            currentCharacter.PedAppearance.chestHairStyle = selection;
                            currentCharacter.PedAppearance.chestHairOpacity = opacity;
                            break;
                        case 32: // body blemishes
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 11, selection, opacity);
                            currentCharacter.PedAppearance.bodyBlemishesStyle = selection;
                            currentCharacter.PedAppearance.bodyBlemishesOpacity = opacity;
                            break;
                    }
                }
            };
            #endregion

            #region clothes
            clothesMenu.OnListIndexChange += (_menu, listItem, oldSelectionIndex, newSelectionIndex, realIndex) =>
            {
                int componentIndex = realIndex + 1;
                if (realIndex > 0)
                {
                    componentIndex += 1;
                }

                int textureIndex = GetPedTextureVariation(Game.PlayerPed.Handle, componentIndex);
                int newTextureIndex = 0;
                SetPedComponentVariation(Game.PlayerPed.Handle, componentIndex, newSelectionIndex, newTextureIndex, 0);
                if (currentCharacter.DrawableVariations.clothes == null)
                {
                    currentCharacter.DrawableVariations.clothes = new Dictionary<int, KeyValuePair<int, int>>();
                }

                int maxTextures = GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, componentIndex, newSelectionIndex);

                currentCharacter.DrawableVariations.clothes[componentIndex] = new KeyValuePair<int, int>(newSelectionIndex, newTextureIndex);
                listItem.Description = $"Sélectionnez un objet à dessiner en utilisant les touches fléchées et appuyez sur ~o~entrée~s~ pour faire défiler toutes les textures disponibles. Texture actuellement sélectionnée : #{newTextureIndex + 1} (sur {maxTextures}).";
            };

            clothesMenu.OnListItemSelect += (sender, listItem, listIndex, realIndex) =>
            {
                int componentIndex = realIndex + 1; // skip face options as that fucks up with inheritance faces
                if (realIndex > 0) // skip hair features as that is done in the appeareance menu
                {
                    componentIndex += 1;
                }

                int textureIndex = GetPedTextureVariation(Game.PlayerPed.Handle, componentIndex);
                int newTextureIndex = (GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, componentIndex, listIndex) - 1) < (textureIndex + 1) ? 0 : textureIndex + 1;
                SetPedComponentVariation(Game.PlayerPed.Handle, componentIndex, listIndex, newTextureIndex, 0);
                if (currentCharacter.DrawableVariations.clothes == null)
                {
                    currentCharacter.DrawableVariations.clothes = new Dictionary<int, KeyValuePair<int, int>>();
                }

                int maxTextures = GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, componentIndex, listIndex);

                currentCharacter.DrawableVariations.clothes[componentIndex] = new KeyValuePair<int, int>(listIndex, newTextureIndex);
                listItem.Description = $"Sélectionnez un objet à dessiner en utilisant les touches fléchées et appuyez sur ~o~enter~s~ pour faire défiler toutes les textures disponibles. Texture actuellement sélectionnée : #{newTextureIndex + 1} (sur {maxTextures}).";
            };
            #endregion

            #region props
            propsMenu.OnListIndexChange += (_menu, listItem, oldSelectionIndex, newSelectionIndex, realIndex) =>
            {
                int propIndex = realIndex;
                if (realIndex == 3)
                {
                    propIndex = 6;
                }
                if (realIndex == 4)
                {
                    propIndex = 7;
                }

                int textureIndex = 0;
                if (newSelectionIndex >= GetNumberOfPedPropDrawableVariations(Game.PlayerPed.Handle, propIndex))
                {
                    SetPedPropIndex(Game.PlayerPed.Handle, propIndex, -1, -1, false);
                    ClearPedProp(Game.PlayerPed.Handle, propIndex);
                    if (currentCharacter.PropVariations.props == null)
                    {
                        currentCharacter.PropVariations.props = new Dictionary<int, KeyValuePair<int, int>>();
                    }
                    currentCharacter.PropVariations.props[propIndex] = new KeyValuePair<int, int>(-1, -1);
                    listItem.Description = $"Sélectionnez un accessoire en utilisant les touches fléchées et appuyez sur ~o~entrée~s~ pour faire défiler toutes les textures disponibles.";
                }
                else
                {
                    SetPedPropIndex(Game.PlayerPed.Handle, propIndex, newSelectionIndex, textureIndex, true);
                    if (currentCharacter.PropVariations.props == null)
                    {
                        currentCharacter.PropVariations.props = new Dictionary<int, KeyValuePair<int, int>>();
                    }
                    currentCharacter.PropVariations.props[propIndex] = new KeyValuePair<int, int>(newSelectionIndex, textureIndex);
                    if (GetPedPropIndex(Game.PlayerPed.Handle, propIndex) == -1)
                    {
                        listItem.Description = $"Sélectionnez un accessoire en utilisant les touches fléchées et appuyez sur ~o~enter~s~ pour faire défiler toutes les textures disponibles.";
                    }
                    else
                    {
                        int maxPropTextures = GetNumberOfPedPropTextureVariations(Game.PlayerPed.Handle, propIndex, newSelectionIndex);
                        listItem.Description = $"Sélectionnez un accessoire en utilisant les touches fléchées et appuyez sur ~o~entrée~s~ pour faire défiler toutes les textures disponibles. Texture actuellement sélectionnée : #{textureIndex + 1} (sur {maxPropTextures}).";
                    }
                }
            };

            propsMenu.OnListItemSelect += (sender, listItem, listIndex, realIndex) =>
            {
                int propIndex = realIndex;
                if (realIndex == 3)
                {
                    propIndex = 6;
                }
                if (realIndex == 4)
                {
                    propIndex = 7;
                }

                int textureIndex = GetPedPropTextureIndex(Game.PlayerPed.Handle, propIndex);
                int newTextureIndex = (GetNumberOfPedPropTextureVariations(Game.PlayerPed.Handle, propIndex, listIndex) - 1) < (textureIndex + 1) ? 0 : textureIndex + 1;
                if (textureIndex >= GetNumberOfPedPropDrawableVariations(Game.PlayerPed.Handle, propIndex))
                {
                    SetPedPropIndex(Game.PlayerPed.Handle, propIndex, -1, -1, false);
                    ClearPedProp(Game.PlayerPed.Handle, propIndex);
                    if (currentCharacter.PropVariations.props == null)
                    {
                        currentCharacter.PropVariations.props = new Dictionary<int, KeyValuePair<int, int>>();
                    }
                    currentCharacter.PropVariations.props[propIndex] = new KeyValuePair<int, int>(-1, -1);
                    listItem.Description = $"Sélectionnez un accessoire en utilisant les touches fléchées et appuyez sur ~o~entrée~s~ pour faire défiler toutes les textures disponibles.";
                }
                else
                {
                    SetPedPropIndex(Game.PlayerPed.Handle, propIndex, listIndex, newTextureIndex, true);
                    if (currentCharacter.PropVariations.props == null)
                    {
                        currentCharacter.PropVariations.props = new Dictionary<int, KeyValuePair<int, int>>();
                    }
                    currentCharacter.PropVariations.props[propIndex] = new KeyValuePair<int, int>(listIndex, newTextureIndex);
                    if (GetPedPropIndex(Game.PlayerPed.Handle, propIndex) == -1)
                    {
                        listItem.Description = $"Sélectionnez un accessoire en utilisant les touches fléchées et appuyez sur ~o~entrée~s~ pour faire défiler toutes les textures disponibles.";
                    }
                    else
                    {
                        int maxPropTextures = GetNumberOfPedPropTextureVariations(Game.PlayerPed.Handle, propIndex, listIndex);
                        listItem.Description = $"Sélectionnez un accessoire en utilisant les touches fléchées et appuyez sur ~o~entrée~s~ pour faire défiler toutes les textures disponibles. Texture actuellement sélectionnée : #{newTextureIndex + 1} (sur {maxPropTextures}).";
                    }
                }
                //propsMenu.UpdateScaleform();
            };
            #endregion

            #region face shape data
            /*
            Nose_Width  
            Nose_Peak_Hight  
            Nose_Peak_Lenght  
            Nose_Bone_High  
            Nose_Peak_Lowering  
            Nose_Bone_Twist  
            EyeBrown_High  
            EyeBrown_Forward  
            Cheeks_Bone_High  
            Cheeks_Bone_Width  
            Cheeks_Width  
            Eyes_Openning  
            Lips_Thickness  
            Jaw_Bone_Width 'Bone size to sides  
            Jaw_Bone_Back_Lenght 'Bone size to back  
            Chimp_Bone_Lowering 'Go Down  
            Chimp_Bone_Lenght 'Go forward  
            Chimp_Bone_Width  
            Chimp_Hole  
            Neck_Thikness  
            */

            List<float> faceFeaturesValuesList = new List<float>()
            {
               -1.0f,    // 0
               -0.9f,    // 1
               -0.8f,    // 2
               -0.7f,    // 3
               -0.6f,    // 4
               -0.5f,    // 5
               -0.4f,    // 6
               -0.3f,    // 7
               -0.2f,    // 8
               -0.1f,    // 9
                0.0f,    // 10
                0.1f,    // 11
                0.2f,    // 12
                0.3f,    // 13
                0.4f,    // 14
                0.5f,    // 15
                0.6f,    // 16
                0.7f,    // 17
                0.8f,    // 18
                0.9f,    // 19
                1.0f     // 20
            };

            var faceFeaturesNamesList = new string[20]
            {
                "Largeur du nez",               // 0
                "Hauteau du nez",         // 1
                "Longueur du pic du nez",         // 2
                "Hauteur de l'os du nez",         // 3
                "Abaissement de la pointe du nez",       // 4
                "Torsion de l'os du nez",          // 5
                "Hauteur des sourcils",          // 6
                "Épaisseur des sourcils",           // 7
                "Hauteur des pommettes",        // 8
                "Largeur des pommettes",         // 9
                "Largeur des joues",             // 10
                "Ouverture des yeux",             // 11
                "Épaisseur des lèvres",           // 12
                "Largeur de l'os de la mâchoire",           // 13
                "Profondeur/longueur des os de la mâchoire",    // 14
                "Hauteur du menton",              // 15
                "Profondeur/longueur du menton",        // 16
                "Largeur du menton",               // 17
                "Taille du trou du menton",           // 18
                "Épaisseur du col"            // 19
            };

            for (int i = 0; i < 20; i++)
            {
                MenuSliderItem faceFeature = new MenuSliderItem(faceFeaturesNamesList[i], $"Définissez le valeur nominale de {faceFeaturesNamesList[i]}.", 0, 20, 10, true);
                faceShapeMenu.AddMenuItem(faceFeature);
            }

            faceShapeMenu.OnSliderPositionChange += (sender, sliderItem, oldPosition, newPosition, itemIndex) =>
            {
                if (currentCharacter.FaceShapeFeatures.features == null)
                {
                    currentCharacter.FaceShapeFeatures.features = new Dictionary<int, float>();
                }
                float value = faceFeaturesValuesList[newPosition];
                currentCharacter.FaceShapeFeatures.features[itemIndex] = value;
                SetPedFaceFeature(Game.PlayerPed.Handle, itemIndex, value);
            };

            #endregion

            #region tattoos
            void CreateListsIfNull()
            {
                if (currentCharacter.PedTatttoos.HeadTattoos == null)
                {
                    currentCharacter.PedTatttoos.HeadTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.TorsoTattoos == null)
                {
                    currentCharacter.PedTatttoos.TorsoTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.LeftArmTattoos == null)
                {
                    currentCharacter.PedTatttoos.LeftArmTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.RightArmTattoos == null)
                {
                    currentCharacter.PedTatttoos.RightArmTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.LeftLegTattoos == null)
                {
                    currentCharacter.PedTatttoos.LeftLegTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.RightLegTattoos == null)
                {
                    currentCharacter.PedTatttoos.RightLegTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.BadgeTattoos == null)
                {
                    currentCharacter.PedTatttoos.BadgeTattoos = new List<KeyValuePair<string, string>>();
                }
            }

            void ApplySavedTattoos()
            {
                // remove all decorations, and then manually re-add them all. what a retarded way of doing this R*....
                ClearPedDecorations(Game.PlayerPed.Handle);

                foreach (var tattoo in currentCharacter.PedTatttoos.HeadTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.TorsoTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.LeftArmTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.RightArmTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.LeftLegTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.RightLegTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.BadgeTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }

                if (!string.IsNullOrEmpty(currentCharacter.PedAppearance.HairOverlay.Key) && !string.IsNullOrEmpty(currentCharacter.PedAppearance.HairOverlay.Value))
                {
                    // reset hair value
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(currentCharacter.PedAppearance.HairOverlay.Key), (uint)GetHashKey(currentCharacter.PedAppearance.HairOverlay.Value));
                }
            }

            tattoosMenu.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) =>
            {
                CreateListsIfNull();
                ApplySavedTattoos();
            };

            #region tattoos menu list select events
            tattoosMenu.OnListIndexChange += (sender, item, oldIndex, tattooIndex, menuIndex) =>
            {
                CreateListsIfNull();
                ApplySavedTattoos();
                if (menuIndex == 0) // head
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.HEAD.ElementAt(tattooIndex) : FemaleTattoosCollection.HEAD.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.HeadTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 1) // torso
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.TORSO.ElementAt(tattooIndex) : FemaleTattoosCollection.TORSO.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.TorsoTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 2) // left arm
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.LEFT_ARM.ElementAt(tattooIndex) : FemaleTattoosCollection.LEFT_ARM.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.LeftArmTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 3) // right arm
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.RIGHT_ARM.ElementAt(tattooIndex) : FemaleTattoosCollection.RIGHT_ARM.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.RightArmTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 4) // left leg
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.LEFT_LEG.ElementAt(tattooIndex) : FemaleTattoosCollection.LEFT_LEG.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.LeftLegTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 5) // right leg
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.RIGHT_LEG.ElementAt(tattooIndex) : FemaleTattoosCollection.RIGHT_LEG.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.RightLegTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 6) // badges
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.BADGES.ElementAt(tattooIndex) : FemaleTattoosCollection.BADGES.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.BadgeTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
            };

            tattoosMenu.OnListItemSelect += (sender, item, tattooIndex, menuIndex) =>
            {
                CreateListsIfNull();

                if (menuIndex == 0) // head
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.HEAD.ElementAt(tattooIndex) : FemaleTattoosCollection.HEAD.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.HeadTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~r~removed~s~.");
                        currentCharacter.PedTatttoos.HeadTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~g~added~s~.");
                        currentCharacter.PedTatttoos.HeadTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 1) // torso
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.TORSO.ElementAt(tattooIndex) : FemaleTattoosCollection.TORSO.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.TorsoTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~r~removed~s~.");
                        currentCharacter.PedTatttoos.TorsoTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~g~added~s~.");
                        currentCharacter.PedTatttoos.TorsoTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 2) // left arm
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.LEFT_ARM.ElementAt(tattooIndex) : FemaleTattoosCollection.LEFT_ARM.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.LeftArmTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~r~removed~s~.");
                        currentCharacter.PedTatttoos.LeftArmTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~g~added~s~.");
                        currentCharacter.PedTatttoos.LeftArmTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 3) // right arm
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.RIGHT_ARM.ElementAt(tattooIndex) : FemaleTattoosCollection.RIGHT_ARM.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.RightArmTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~r~removed~s~.");
                        currentCharacter.PedTatttoos.RightArmTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~g~added~s~.");
                        currentCharacter.PedTatttoos.RightArmTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 4) // left leg
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.LEFT_LEG.ElementAt(tattooIndex) : FemaleTattoosCollection.LEFT_LEG.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.LeftLegTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~r~removed~s~.");
                        currentCharacter.PedTatttoos.LeftLegTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~g~added~s~.");
                        currentCharacter.PedTatttoos.LeftLegTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 5) // right leg
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.RIGHT_LEG.ElementAt(tattooIndex) : FemaleTattoosCollection.RIGHT_LEG.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.RightLegTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~r~removed~s~.");
                        currentCharacter.PedTatttoos.RightLegTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} has been ~g~added~s~.");
                        currentCharacter.PedTatttoos.RightLegTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 6) // badges
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.BADGES.ElementAt(tattooIndex) : FemaleTattoosCollection.BADGES.ElementAt(tattooIndex);
                    KeyValuePair<string, string> tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.BadgeTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Badge #{tattooIndex + 1} has been ~r~removed~s~.");
                        currentCharacter.PedTatttoos.BadgeTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Badge #{tattooIndex + 1} has been ~g~added~s~.");
                        currentCharacter.PedTatttoos.BadgeTattoos.Add(tat);
                    }
                }

                ApplySavedTattoos();

            };

            // eventhandler for when a tattoo is selected.
            tattoosMenu.OnItemSelect += (sender, item, index) =>
            {
                Notify.Success("All tattoos have been removed.");
                currentCharacter.PedTatttoos.HeadTattoos.Clear();
                currentCharacter.PedTatttoos.TorsoTattoos.Clear();
                currentCharacter.PedTatttoos.LeftArmTattoos.Clear();
                currentCharacter.PedTatttoos.RightArmTattoos.Clear();
                currentCharacter.PedTatttoos.LeftLegTattoos.Clear();
                currentCharacter.PedTatttoos.RightLegTattoos.Clear();
                currentCharacter.PedTatttoos.BadgeTattoos.Clear();
                ClearPedDecorations(Game.PlayerPed.Handle);
            };

            #endregion
            #endregion


            // handle list changes in the character creator menu.
            createCharacterMenu.OnListIndexChange += (sender, item, oldListIndex, newListIndex, itemIndex) =>
            {
                if (item == faceExpressionList)
                {
                    currentCharacter.FacialExpression = facial_expressions[newListIndex];
                    SetFacialIdleAnimOverride(Game.PlayerPed.Handle, currentCharacter.FacialExpression ?? facial_expressions[0], null);
                }
            };

            // handle button presses for the createCharacter menu.
            createCharacterMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == saveButton) // save ped
                {
                    if (await SavePed())
                    {
                        while (!MenuController.IsAnyMenuOpen())
                        {
                            await BaseScript.Delay(0);
                        }

                        while (IsControlPressed(2, 201) || IsControlPressed(2, 217) || IsDisabledControlPressed(2, 201) || IsDisabledControlPressed(2, 217))
                            await BaseScript.Delay(0);
                        await BaseScript.Delay(100);

                        createCharacterMenu.GoBack();
                    }
                }
                else if (item == exitNoSave) // exit without saving
                {
                    bool confirm = false;
                    AddTextEntry("vmenu_warning_message_first_line", "Etes-vous sûr de vouloir quitter le créateur de personnage ?");
                    AddTextEntry("vmenu_warning_message_second_line", "Vous perdrez toutes les personnalisations (non sauvegardées) !");
                    createCharacterMenu.CloseMenu();

                    // wait for confirmation or cancel input.
                    while (true)
                    {
                        await BaseScript.Delay(0);
                        int unk = 1;
                        int unk2 = 1;
                        SetWarningMessage("vmenu_warning_message_first_line", 20, "vmenu_warning_message_second_line", true, 0, ref unk, ref unk2, true, 0);
                        if (IsControlJustPressed(2, 201) || IsControlJustPressed(2, 217)) // continue/accept
                        {
                            confirm = true;
                            break;
                        }
                        else if (IsControlJustPressed(2, 202)) // cancel
                        {
                            break;
                        }
                    }

                    // if confirmed to discard changes quit the editor.
                    if (confirm)
                    {
                        while (IsControlPressed(2, 201) || IsControlPressed(2, 217) || IsDisabledControlPressed(2, 201) || IsDisabledControlPressed(2, 217))
                            await BaseScript.Delay(0);
                        await BaseScript.Delay(100);
                        menu.OpenMenu();
                    }
                    else // otherwise cancel and go back to the editor.
                    {
                        createCharacterMenu.OpenMenu();
                    }
                }
                else if (item == inheritanceButton) // update the inheritance menu anytime it's opened to prevent some weird glitch where old data is used.
                {
                    var data = Game.PlayerPed.GetHeadBlendData();
                    inheritanceDads.ListIndex = data.FirstFaceShape;
                    inheritanceMoms.ListIndex = data.SecondFaceShape;
                    inheritanceShapeMix.Position = (int)(data.ParentFaceShapePercent * 10f);
                    inheritanceSkinMix.Position = (int)(data.ParentSkinTonePercent * 10f);
                    inheritanceMenu.RefreshIndex();
                }
            };

            // eventhandler for whenever a menu item is selected in the main mp characters menu.
            menu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == createMaleBtn)
                {
                    uint model = (uint)GetHashKey("mp_m_freemode_01");

                    if (!HasModelLoaded(model))
                    {
                        RequestModel(model);
                        while (!HasModelLoaded(model))
                        {
                            await BaseScript.Delay(0);
                        }
                    }

                    int maxHealth = Game.PlayerPed.MaxHealth;
                    int maxArmour = Game.Player.MaxArmor;
                    int health = Game.PlayerPed.Health;
                    int armour = Game.PlayerPed.Armor;

                    SaveWeaponLoadout("vmenu_temp_weapons_loadout_before_respawn");
                    SetPlayerModel(Game.Player.Handle, model);
                    await SpawnWeaponLoadoutAsync("vmenu_temp_weapons_loadout_before_respawn", false, true, true);

                    Game.Player.MaxArmor = maxArmour;
                    Game.PlayerPed.MaxHealth = maxHealth;
                    Game.PlayerPed.Health = health;
                    Game.PlayerPed.Armor = armour;

                    ClearPedDecorations(Game.PlayerPed.Handle);
                    ClearPedFacialDecorations(Game.PlayerPed.Handle);
                    SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
                    SetPedHairColor(Game.PlayerPed.Handle, 0, 0);
                    SetPedEyeColor(Game.PlayerPed.Handle, 0);
                    ClearAllPedProps(Game.PlayerPed.Handle);

                    MakeCreateCharacterMenu(male: true);
                }
                else if (item == createFemaleBtn)
                {
                    uint model = (uint)GetHashKey("mp_f_freemode_01");

                    if (!HasModelLoaded(model))
                    {
                        RequestModel(model);
                        while (!HasModelLoaded(model))
                        {
                            await BaseScript.Delay(0);
                        }
                    }

                    int maxHealth = Game.PlayerPed.MaxHealth;
                    int maxArmour = Game.Player.MaxArmor;
                    int health = Game.PlayerPed.Health;
                    int armour = Game.PlayerPed.Armor;

                    SaveWeaponLoadout("vmenu_temp_weapons_loadout_before_respawn");
                    SetPlayerModel(Game.Player.Handle, model);
                    await SpawnWeaponLoadoutAsync("vmenu_temp_weapons_loadout_before_respawn", false, true, true);

                    Game.Player.MaxArmor = maxArmour;
                    Game.PlayerPed.MaxHealth = maxHealth;
                    Game.PlayerPed.Health = health;
                    Game.PlayerPed.Armor = armour;

                    ClearPedDecorations(Game.PlayerPed.Handle);
                    ClearPedFacialDecorations(Game.PlayerPed.Handle);
                    SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
                    SetPedHairColor(Game.PlayerPed.Handle, 0, 0);
                    SetPedEyeColor(Game.PlayerPed.Handle, 0);
                    ClearAllPedProps(Game.PlayerPed.Handle);

                    MakeCreateCharacterMenu(male: false);
                }
                else if (item == savedCharacters)
                {
                    UpdateSavedPedsMenu();
                }
            };
        }

        /// <summary>
        /// Spawns this saved ped.
        /// </summary>
        /// <param name="name"></param>
        internal async Task SpawnThisCharacter(string name, bool restoreWeapons)
        {
            currentCharacter = StorageManager.GetSavedMpCharacterData(name);
            await SpawnSavedPed(restoreWeapons);
        }

        /// <summary>
        /// Spawns the ped from the data inside <see cref="currentCharacter"/>.
        /// Character data MUST be set BEFORE calling this function.
        /// </summary>
        /// <returns></returns>
        private async Task SpawnSavedPed(bool restoreWeapons)
        {
            if (currentCharacter.Version < 1)
            {
                return;
            }
            if (IsModelInCdimage(currentCharacter.ModelHash))
            {
                if (!HasModelLoaded(currentCharacter.ModelHash))
                {
                    RequestModel(currentCharacter.ModelHash);
                    while (!HasModelLoaded(currentCharacter.ModelHash))
                    {
                        await BaseScript.Delay(0);
                    }
                }
                int maxHealth = Game.PlayerPed.MaxHealth;
                int maxArmour = Game.Player.MaxArmor;
                int health = Game.PlayerPed.Health;
                int armour = Game.PlayerPed.Armor;

                SaveWeaponLoadout("vmenu_temp_weapons_loadout_before_respawn");
                SetPlayerModel(Game.Player.Handle, currentCharacter.ModelHash);
                await SpawnWeaponLoadoutAsync("vmenu_temp_weapons_loadout_before_respawn", false, true, true);

                Game.Player.MaxArmor = maxArmour;
                Game.PlayerPed.MaxHealth = maxHealth;
                Game.PlayerPed.Health = health;
                Game.PlayerPed.Armor = armour;

                ClearPedDecorations(Game.PlayerPed.Handle);
                ClearPedFacialDecorations(Game.PlayerPed.Handle);
                SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
                SetPedHairColor(Game.PlayerPed.Handle, 0, 0);
                SetPedEyeColor(Game.PlayerPed.Handle, 0);
                ClearAllPedProps(Game.PlayerPed.Handle);

                #region headblend
                var data = currentCharacter.PedHeadBlendData;
                SetPedHeadBlendData(Game.PlayerPed.Handle, data.FirstFaceShape, data.SecondFaceShape, data.ThirdFaceShape, data.FirstSkinTone, data.SecondSkinTone, data.ThirdSkinTone, data.ParentFaceShapePercent, data.ParentSkinTonePercent, 0f, data.IsParentInheritance);

                while (!HasPedHeadBlendFinished(Game.PlayerPed.Handle))
                {
                    await BaseScript.Delay(0);
                }
                #endregion

                #region appearance
                var appData = currentCharacter.PedAppearance;
                // hair
                SetPedComponentVariation(Game.PlayerPed.Handle, 2, appData.hairStyle, 0, 0);
                SetPedHairColor(Game.PlayerPed.Handle, appData.hairColor, appData.hairHighlightColor);
                if (!string.IsNullOrEmpty(appData.HairOverlay.Key) && !string.IsNullOrEmpty(appData.HairOverlay.Value))
                {
                    SetPedFacialDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(appData.HairOverlay.Key), (uint)GetHashKey(appData.HairOverlay.Value));
                }
                // blemishes
                SetPedHeadOverlay(Game.PlayerPed.Handle, 0, appData.blemishesStyle, appData.blemishesOpacity);
                // bread
                SetPedHeadOverlay(Game.PlayerPed.Handle, 1, appData.beardStyle, appData.beardOpacity);
                SetPedHeadOverlayColor(Game.PlayerPed.Handle, 1, 1, appData.beardColor, appData.beardColor);
                // eyebrows
                SetPedHeadOverlay(Game.PlayerPed.Handle, 2, appData.eyebrowsStyle, appData.eyebrowsOpacity);
                SetPedHeadOverlayColor(Game.PlayerPed.Handle, 2, 1, appData.eyebrowsColor, appData.eyebrowsColor);
                // ageing
                SetPedHeadOverlay(Game.PlayerPed.Handle, 3, appData.ageingStyle, appData.ageingOpacity);
                // makeup
                SetPedHeadOverlay(Game.PlayerPed.Handle, 4, appData.makeupStyle, appData.makeupOpacity);
                SetPedHeadOverlayColor(Game.PlayerPed.Handle, 4, 2, appData.makeupColor, appData.makeupColor);
                // blush
                SetPedHeadOverlay(Game.PlayerPed.Handle, 5, appData.blushStyle, appData.blushOpacity);
                SetPedHeadOverlayColor(Game.PlayerPed.Handle, 5, 2, appData.blushColor, appData.blushColor);
                // complexion
                SetPedHeadOverlay(Game.PlayerPed.Handle, 6, appData.complexionStyle, appData.complexionOpacity);
                // sundamage
                SetPedHeadOverlay(Game.PlayerPed.Handle, 7, appData.sunDamageStyle, appData.sunDamageOpacity);
                // lipstick
                SetPedHeadOverlay(Game.PlayerPed.Handle, 8, appData.lipstickStyle, appData.lipstickOpacity);
                SetPedHeadOverlayColor(Game.PlayerPed.Handle, 8, 2, appData.lipstickColor, appData.lipstickColor);
                // moles and freckles
                SetPedHeadOverlay(Game.PlayerPed.Handle, 9, appData.molesFrecklesStyle, appData.molesFrecklesOpacity);
                // chest hair 
                SetPedHeadOverlay(Game.PlayerPed.Handle, 10, appData.chestHairStyle, appData.chestHairOpacity);
                SetPedHeadOverlayColor(Game.PlayerPed.Handle, 10, 1, appData.chestHairColor, appData.chestHairColor);
                // body blemishes 
                SetPedHeadOverlay(Game.PlayerPed.Handle, 11, appData.bodyBlemishesStyle, appData.bodyBlemishesOpacity);
                // eyecolor
                SetPedEyeColor(Game.PlayerPed.Handle, appData.eyeColor);
                #endregion

                #region Face Shape Data
                for (var i = 0; i < 19; i++)
                {
                    SetPedFaceFeature(Game.PlayerPed.Handle, i, 0f);
                }

                if (currentCharacter.FaceShapeFeatures.features != null)
                {
                    foreach (var t in currentCharacter.FaceShapeFeatures.features)
                    {
                        SetPedFaceFeature(Game.PlayerPed.Handle, t.Key, t.Value);
                    }
                }
                else
                {
                    currentCharacter.FaceShapeFeatures.features = new Dictionary<int, float>();
                }

                #endregion

                #region Clothing Data
                if (currentCharacter.DrawableVariations.clothes != null && currentCharacter.DrawableVariations.clothes.Count > 0)
                {
                    foreach (var cd in currentCharacter.DrawableVariations.clothes)
                    {
                        SetPedComponentVariation(Game.PlayerPed.Handle, cd.Key, cd.Value.Key, cd.Value.Value, 0);
                    }
                }
                #endregion

                #region Props Data
                if (currentCharacter.PropVariations.props != null && currentCharacter.PropVariations.props.Count > 0)
                {
                    foreach (var cd in currentCharacter.PropVariations.props)
                    {
                        if (cd.Value.Key > -1)
                        {
                            SetPedPropIndex(Game.PlayerPed.Handle, cd.Key, cd.Value.Key, cd.Value.Value > -1 ? cd.Value.Value : 0, true);
                        }
                    }
                }
                #endregion

                #region Tattoos

                if (currentCharacter.PedTatttoos.HeadTattoos == null)
                {
                    currentCharacter.PedTatttoos.HeadTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.TorsoTattoos == null)
                {
                    currentCharacter.PedTatttoos.TorsoTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.LeftArmTattoos == null)
                {
                    currentCharacter.PedTatttoos.LeftArmTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.RightArmTattoos == null)
                {
                    currentCharacter.PedTatttoos.RightArmTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.LeftLegTattoos == null)
                {
                    currentCharacter.PedTatttoos.LeftLegTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.RightLegTattoos == null)
                {
                    currentCharacter.PedTatttoos.RightLegTattoos = new List<KeyValuePair<string, string>>();
                }
                if (currentCharacter.PedTatttoos.BadgeTattoos == null)
                {
                    currentCharacter.PedTatttoos.BadgeTattoos = new List<KeyValuePair<string, string>>();
                }

                foreach (var tattoo in currentCharacter.PedTatttoos.HeadTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.TorsoTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.LeftArmTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.RightArmTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.LeftLegTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.RightLegTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.BadgeTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                #endregion
            }

            // Set the facial expression, or set it to 'normal' if it wasn't saved/set before.
            SetFacialIdleAnimOverride(Game.PlayerPed.Handle, currentCharacter.FacialExpression ?? facial_expressions[0], null);
        }

        /// <summary>
        /// Creates the saved mp characters menu.
        /// </summary>
        private void CreateSavedPedsMenu()
        {
            UpdateSavedPedsMenu();

            MenuController.AddMenu(manageSavedCharacterMenu);

            MenuItem spawnPed = new MenuItem("Personnage sauvegardés", "Crée le personnage sauvegardé sélectionné.");
            editPedBtn = new MenuItem("Modifier un caractère sauvegardé", "Cela vous permet de modifier tout ce qui concerne votre personnage sauvegardé. Les modifications seront enregistrées dans le fichier d'enregistrement de ce personnage dès que vous aurez cliqué sur le bouton d'enregistrement.");
            MenuItem clonePed = new MenuItem("Cloner personnage enregistré", "Cela va créer un clone de votre personnage sauvegardé. Il vous sera demandé de fournir un nom pour ce personnage. Si ce nom est déjà pris, l'action sera annulée.");
            MenuItem setAsDefaultPed = new MenuItem("Définir comme caractère par défaut", "Si vous définissez ce personnage comme votre personnage par défaut et que vous activez l'option 'Respawn comme personnage par défaut' dans le menu Paramètres divers, vous serez dans la peau de ce personnage à chaque fois que vous (re)sortirez du jeu.");
            MenuItem renameCharacter = new MenuItem("Renommer un caractère sauvegardé", "Vous pouvez renommer ce personnage sauvegardé. Si le nom est déjà pris, l'action sera annulée.");
            MenuItem delPed = new MenuItem("Supprimer un caractère sauvegardé", "Supprime le caractère sauvegardé sélectionné. Cette opération ne peut pas être annulée !")
            {
                LeftIcon = MenuItem.Icon.WARNING
            };
            manageSavedCharacterMenu.AddMenuItem(spawnPed);
            manageSavedCharacterMenu.AddMenuItem(editPedBtn);
            manageSavedCharacterMenu.AddMenuItem(clonePed);
            manageSavedCharacterMenu.AddMenuItem(setAsDefaultPed);
            manageSavedCharacterMenu.AddMenuItem(renameCharacter);
            manageSavedCharacterMenu.AddMenuItem(delPed);

            MenuController.BindMenuItem(manageSavedCharacterMenu, createCharacterMenu, editPedBtn);

            manageSavedCharacterMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == editPedBtn)
                {
                    currentCharacter = StorageManager.GetSavedMpCharacterData(selectedSavedCharacterManageName);

                    await SpawnSavedPed(true);

                    MakeCreateCharacterMenu(male: currentCharacter.IsMale, editPed: true);
                }
                else if (item == spawnPed)
                {
                    currentCharacter = StorageManager.GetSavedMpCharacterData(selectedSavedCharacterManageName);

                    await SpawnSavedPed(true);
                }
                else if (item == clonePed)
                {
                    var tmpCharacter = StorageManager.GetSavedMpCharacterData("mp_ped_" + selectedSavedCharacterManageName);
                    string name = await GetUserInput(windowTitle: "Entrez un nom pour le personnage cloné", defaultText: tmpCharacter.SaveName.Substring(7), maxInputLength: 30);
                    if (string.IsNullOrEmpty(name))
                    {
                        Notify.Error(CommonErrors.InvalidSaveName);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(GetResourceKvpString("mp_ped_" + name)))
                        {
                            Notify.Error(CommonErrors.SaveNameAlreadyExists);
                        }
                        else
                        {
                            tmpCharacter.SaveName = "mp_ped_" + name;
                            if (StorageManager.SaveJsonData("mp_ped_" + name, JsonConvert.SerializeObject(tmpCharacter), false))
                            {
                                Notify.Success($"Votre personnage a été cloné. Le nom du personnage cloné est : ~g~<C>{name}</C>~s~.");
                                UpdateSavedPedsMenu();
                            }
                            else
                            {
                                Notify.Error("Le clone n'a pas pu être créé, raison inconnue. Un personnage existe-t-il déjà avec ce nom ? :(");
                            }
                        }
                    }
                }
                else if (item == renameCharacter)
                {
                    var tmpCharacter = StorageManager.GetSavedMpCharacterData("mp_ped_" + selectedSavedCharacterManageName);
                    string name = await GetUserInput(windowTitle: "Entrez un nouveau nom de caractère", defaultText: tmpCharacter.SaveName.Substring(7), maxInputLength: 30);
                    if (string.IsNullOrEmpty(name))
                    {
                        Notify.Error(CommonErrors.InvalidInput);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(GetResourceKvpString("mp_ped_" + name)))
                        {
                            Notify.Error(CommonErrors.SaveNameAlreadyExists);
                        }
                        else
                        {
                            tmpCharacter.SaveName = "mp_ped_" + name;
                            if (StorageManager.SaveJsonData("mp_ped_" + name, JsonConvert.SerializeObject(tmpCharacter), false))
                            {
                                StorageManager.DeleteSavedStorageItem("mp_ped_" + selectedSavedCharacterManageName);
                                Notify.Success($"Votre personnage a été renommé en ~g~<C>{name}</C>~s~.");
                                UpdateSavedPedsMenu();
                                while (!MenuController.IsAnyMenuOpen())
                                {
                                    await BaseScript.Delay(0);
                                }
                                manageSavedCharacterMenu.GoBack();
                            }
                            else
                            {
                                Notify.Error("Un problème est survenu lors du renommage de votre personnage. Votre ancien personnage ne sera PAS supprimé pour cette raison.");
                            }
                        }
                    }
                }
                else if (item == delPed)
                {
                    if (delPed.Label == "Etes-vous sûr ?")
                    {
                        delPed.Label = "";
                        DeleteResourceKvp("mp_ped_" + selectedSavedCharacterManageName);
                        Notify.Success("Votre personnage enregistré a été supprimé.");
                        manageSavedCharacterMenu.GoBack();
                        UpdateSavedPedsMenu();
                        manageSavedCharacterMenu.RefreshIndex();
                    }
                    else
                    {
                        delPed.Label = "Etes-vous sûr ?";
                    }
                }
                else if (item == setAsDefaultPed)
                {
                    Notify.Success($"Votre personnage <C>{selectedSavedCharacterManageName}</C> sera désormais utilisé comme personnage par défaut lors de votre (re)apparaissez.");
                    SetResourceKvp("vmenu_default_character", "mp_ped_" + selectedSavedCharacterManageName);
                }

                if (item != delPed)
                {
                    if (delPed.Label == "Etes-vous sûr ?")
                    {
                        delPed.Label = "";
                    }
                }
            };

            // reset the "are you sure" state.
            manageSavedCharacterMenu.OnMenuClose += (sender) =>
            {
                manageSavedCharacterMenu.GetMenuItems()[2].Label = "";
            };

            savedCharactersMenu.OnItemSelect += (sender, item, index) =>
            {
                selectedSavedCharacterManageName = item.Text;
                manageSavedCharacterMenu.MenuSubtitle = item.Text;
                manageSavedCharacterMenu.CounterPreText = $"{(item.Label.Substring(0, 3) == "(M)" ? "(Homme) " : "(Femme) ")}";
                manageSavedCharacterMenu.RefreshIndex();
            };
        }

        /// <summary>
        /// Updates the saved peds menu.
        /// </summary>
        private void UpdateSavedPedsMenu()
        {
            string defaultChar = GetResourceKvpString("vmenu_default_character") ?? "";

            List<string> names = new List<string>();
            var handle = StartFindKvp("mp_ped_");
            while (true)
            {
                string foundName = FindKvp(handle);
                if (string.IsNullOrEmpty(foundName))
                {
                    break;
                }
                else
                {
                    names.Add(foundName.Substring(7));
                }
            }
            EndFindKvp(handle);
            savedCharactersMenu.ClearMenuItems();
            if (names.Count > 0)
            {
                names.Sort((a, b) => { return a.ToLower().CompareTo(b.ToLower()); });
                foreach (string item in names)
                {
                    var tmpData = StorageManager.GetSavedMpCharacterData("mp_ped_" + item);
                    MenuItem btn = new MenuItem(item, "Cliquez pour créer, modifier, cloner, renommer ou supprimer ce personnage enregistré.")
                    {
                        Label = $"({(tmpData.IsMale ? "M" : "F")}) →→→"
                    };
                    if (defaultChar == "mp_ped_" + item)
                    {
                        btn.LeftIcon = MenuItem.Icon.TICK;
                        btn.Description += " ~g~Ce personnage est actuellement défini comme votre personnage par défaut et sera utilisé à chaque fois que vous (re)apparaissez.";
                    }
                    savedCharactersMenu.AddMenuItem(btn);
                    MenuController.BindMenuItem(savedCharactersMenu, manageSavedCharacterMenu, btn);
                }
            }
            savedCharactersMenu.RefreshIndex();
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
