using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.IO;
using UnityEngine;
using KSP;

namespace PlanetRandomizer
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class PlanetSystem : MonoBehaviour
    {
        public static CelestialBody Sun { get { return Planetarium.fetch.Sun; } }
        public static CelestialBody Kerbin { get { return Planetarium.fetch.Home; } }

        private bool showGUI = false;
        private bool newGame = false;

        public static PlanetSystem Instance;

        private static bool systemLoaded = false;

        /*
         * if (File.Exists(KSPUtil.ApplicationRootPath + "/GameData/RealSolarSystem/RealSolarSystem.cfg")) // checks for RSS
            {
                smaSolarMin = 10 * smaSolarMin;
                smaSolarMax = 30 * smaSolarMax;
            }
            if (File.Exists(KSPUtil.ApplicationRootPath + "/GameData/PlanetFactory/PlanetFactory.dll")) // checks for PlanetFactory
            {
                smaSolarMax = 5 * smaSolarMax;
            }
         * */

        public void Start()
        {
            if (Instance)
            {
                DestroyImmediate(gameObject);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
                Instance = this;
                GameEvents.onGameStateCreated.Add(new EventData<Game>.OnEvent(OnGameCreated));
                GameEvents.onGameStateSave.Add(new EventData<ConfigNode>.OnEvent(OnGameSaved));
                GameEvents.onGameStateLoad.Add(new EventData<ConfigNode>.OnEvent(OnGameLoaded));
                GameEvents.onGameSceneSwitchRequested.Add(new EventData<GameEvents.FromToAction<GameScenes, GameScenes>>.OnEvent(OnMainMenu));
                Settings.MemorizeDefaultLayout();
                ScienceModifier.MemorizeDefaultScience();
                //GameEvents.onGameSceneLoadRequested.Add(new EventData<GameScenes>.OnEvent(OnGameSceneLoadRequested));
                //PlanetSettings.Load(KSPUtil.ApplicationRootPath + "/GameData/PlanetRandomizer/Resources/PlanetRandomizer.cfg");
                //PlanetDefault.Load(KSPUtil.ApplicationRootPath + "/GameData/PlanetRandomizer/Resources/PlanetRandomizer.cfg");
            }
        }

        void OnMainMenu(GameEvents.FromToAction<GameScenes, GameScenes> scenes)
        {
            if (scenes.to == GameScenes.MAINMENU && systemLoaded)
            {
                systemLoaded = false; //Prepare to load another system if a savegame is loaded.
                PlanetAligner.reloadDefaultPlanets();
                ScienceModifier.RestoreScience();
            }
        }

        void OnGameCreated(Game game)
        {
            if (game.UniversalTime <= 0.0)
            {
                newGame = true;
            }
        }

        void OnGameLoaded(ConfigNode gameNode)
        {
            if (!systemLoaded)
            {
                if (gameNode.HasNode("PLANET_DATA") && !newGame)
                {
                    Settings.Load(gameNode.GetNode("PLANET_DATA"));
                    UnityEngine.Debug.Log("Planets Loaded.");
                    PlanetAligner.AlignPlanetsToOrbits();
                }
                else
                {
                    UnityEngine.Debug.LogWarning("No default planet settings to load!");
                    showGUI = true;
                    newGame = false;
                }
            }
            systemLoaded = true;
        }

        void OnGameSaved(ConfigNode gameNode)
        {
            ConfigNode planetData = new ConfigNode();
            if (Settings.Save(planetData))
            {
                gameNode.AddNode("PLANET_DATA", planetData);
                UnityEngine.Debug.Log("Planet Data Saved!");
            }
        }

        Rect windowRect = new Rect(100, 100, 150, 120);

        void OnGUI()
        {
            if (showGUI)
            {
                windowRect = GUI.Window(121, windowRect, WindowFunction, "Planet Randomizer");
            }
        }

        void WindowFunction(int windowID)
        {
            Settings.Instance.seed = int.Parse(GUI.TextField(new Rect(10, 30, 60, 20), Settings.Instance.seed.ToString()));
            if (GUI.Button(new Rect(80, 30, 60, 20), "Random"))
            {
                Settings.Instance.seed = UnityEngine.Random.Range(1,999999);
            }
            if (GUI.Button(new Rect(10, 60, 130, 20), "Select"))
            {
                print("Select pressed");
                showGUI = true;
                //DefaultSystem();
                PlanetAligner.BuildNewOrbits();
            }
            if (GUI.Button(new Rect(10, 90, 130, 20), "Done"))
            {
                print("Done pressed");
                showGUI = false;
            }
            GUI.DragWindow();
        }

        private static int CompareByMass(PlanetData a, PlanetData b)
        {
            if (a == null)
            {
                if (b == null)
                    return 0;
                else
                    return -1;
            }
            else
            {
                if (b == null)
                    return 1;
                else
                    return b.Mass.CompareTo(a.Mass);
            }
        }

    }
}
