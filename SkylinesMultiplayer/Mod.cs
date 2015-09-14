using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using CitiesSkylinesDetour;
using ColossalFramework;
using ColossalFramework.DataBinding;
using ColossalFramework.IO;
using ColossalFramework.Math;
using ColossalFramework.Packaging;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using ICities;
using Newtonsoft.Json;
using UnityEngine;
using SimpleJSON;

namespace SkylinesMultiplayer
{
    class ServerEntry : JSONClass
    {
        public int id { get; set; }

        public string name { get; set; }

        public string ip { get; set; }

        public int? port { get; set; }

        public int? mapId { get; set; }
    }

    public class Mod : IUserMod
    {
        public string Name
        {
            get
            {
                LoadMod();
                var multiplayerMenuManager = new GameObject("MultiplayerMenuManager");
                var mmmComponent = multiplayerMenuManager.AddComponent<MultiplayerMenuManager>();
                mmmComponent.UserMod = this;
                return "First Person Multiplayer";
            }
        }

        public string Description
        {
            get { return "Run around in the city with other people."; }
        }

        private static UIComponent m_joinByIpPanel;

        public void LoadMod()
        {
            if (IsModActive() && GameObject.Find("Multiplayer Menu") == null)
            {
                var openJoinByIpMethod = typeof(Mod).GetMethod("OpenJoinByIp");
                var srcMethod7 = typeof(MainMenu).GetMethod("Continue", BindingFlags.Instance | BindingFlags.NonPublic);
                RedirectionHelper.RedirectCalls(srcMethod7, openJoinByIpMethod);

                new GameObject("Multiplayer Menu");

                GameObject.Find("MenuContainer")
                    .transform.Find("Group")
                    .Find("MenuArea")
                    .Find("Menu")
                    .Find("LoadGame")
                    .GetComponent<UIButton>()
                    .text = "HOST GAME";

                GameObject.Find("MenuContainer")
                    .transform.Find("Group")
                    .Find("MenuArea")
                    .Find("Menu")
                    .Find("NewGame")
                    .GetComponent<UIButton>().text = "SERVER LIST";;

                var joinByIpButton = GameObject.Find("MenuContainer")
                    .transform.Find("Group")
                    .Find("MenuArea")
                    .Find("Menu")
                    .Find("Continue")
                    .GetComponent<UIButton>();

                joinByIpButton.text = "JOIN BY IP";


                var loadPanel = GameObject.Find("(Library) LoadPanel");

                UIView v = UIView.GetAView();
                m_joinByIpPanel = v.AddUIComponent(typeof(JoinByIpPanel));


                if (loadPanel.GetComponent<LoadPanel>() != null)
                    GameObject.Destroy(loadPanel.GetComponent<LoadPanel>());
                if (loadPanel.GetComponent<HostPanel>() == null)
                    loadPanel.AddComponent<HostPanel>();
                loadPanel.transform.Find("Caption").Find("Label").GetComponent<UILabel>().text = "Host Game";
                loadPanel.transform.Find("Load").GetComponent<UIButton>().text = "Host";
                loadPanel.transform.Find("Load").GetComponent<BindEvent>().Unbind();
                BindingReference re = new BindingReference();
                re.component = loadPanel.GetComponent<HostPanel>();
                re.memberName = "OnLoad";
                loadPanel.transform.Find("Load").GetComponent<BindEvent>().dataTarget = re;
                loadPanel.transform.Find("Load").GetComponent<BindEvent>().Bind();

                var newGamePanel = GameObject.Find("(Library) NewGamePanel");

                if (newGamePanel.GetComponent<NewGamePanel>() != null)
                    GameObject.Destroy(newGamePanel.GetComponent<NewGamePanel>());
                if (newGamePanel.GetComponent<JoinPanel>() == null)
                    newGamePanel.AddComponent<JoinPanel>();
                newGamePanel.transform.Find("Caption").Find("Label").GetComponent<UILabel>().text = "Join Game";
                newGamePanel.transform.Find("Start").GetComponent<UIButton>().text = "Join";
                newGamePanel.transform.Find("Start").GetComponent<BindEvent>().Unbind();
                BindingReference re2 = new BindingReference();
                re2.component = newGamePanel.GetComponent<JoinPanel>();
                re2.memberName = "OnStartNewGame";
                newGamePanel.transform.Find("Start").GetComponent<BindEvent>().dataTarget = re2;
                newGamePanel.transform.Find("Start").GetComponent<BindEvent>().Bind();
            }
        }

        public static bool IsModActive()
        {
            var pluginManager = PluginManager.instance;
            var plugins = ReflectionHelper.GetPrivate<Dictionary<string, PluginManager.PluginInfo>>(pluginManager, "m_Plugins");
            
            foreach (var item in plugins)
            {
                if (item.Value.name != Settings.WorkshopID)
                {
                    continue;
                }

                return item.Value.isEnabled;
            }

            return false;
        }

        public static void OpenJoinByIp()
        {
            m_joinByIpPanel.isVisible = true;
            m_joinByIpPanel.BringToFront();
        }

        public static void NullOverride()
        {
        }

        private void ServerListButtonOnPress(UIComponent comp, UIButton.ButtonState sel)
        {
            var serverList = GameObject.Find("(Library) ServerList");
            serverList.GetComponent<UIPanel>().isVisible = true;
        }
    }

    public class Server
    {
        public int id { get; set; }

        public string name { get; set; }

        public string ip { get; set; }

        public int? port { get; set; }

        public int? mapId { get; set; }
    }

    public class MultiplayerLoading : LoadingExtensionBase
    {
        public static bool UseVehiclesAndCitizens = true;

        private HideUI m_hideUI;
        private MultiplayerManager m_mInstance;


        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            RedirectCalls();

            var multiplayerManager = new GameObject("MultiplayerManager");
            m_mInstance = multiplayerManager.AddComponent<MultiplayerManager>();

            var cameraController = GameObject.FindObjectOfType<CameraController>();
            m_hideUI = cameraController.gameObject.AddComponent<HideUI>();
        }

        void RedirectCalls()
        {
            var nullMethod = typeof(Mod).GetMethod("NullOverride");

            //Removes the ability to pause/unpause the game
            var srcMethod = typeof(SimulationManager).GetMethod("set_SimulationPaused");
            RedirectionHelper.RedirectCalls(srcMethod, nullMethod);

            if (!UseVehiclesAndCitizens)
            {
                var srcMethod2 = typeof(CitizenManager).GetMethod("CreateCitizenInstance");
                RedirectionHelper.RedirectCalls(srcMethod2, nullMethod);

                var myMethod = typeof(CitizenManager).GetMethods().Where(m => m.Name == "CreateCitizen").ToList();
                RedirectionHelper.RedirectCalls(myMethod[0], nullMethod);
                RedirectionHelper.RedirectCalls(myMethod[1], nullMethod);

                var srcMethod4 = typeof(CitizenManager).GetMethod("CreateUnits");
                RedirectionHelper.RedirectCalls(srcMethod4, nullMethod);


                var srcMethod6 = typeof(CitizenManager).GetMethod("InitializeInstance", BindingFlags.Instance | BindingFlags.NonPublic);
                RedirectionHelper.RedirectCalls(srcMethod6, nullMethod);

                var srcMethod5 = typeof(VehicleManager).GetMethod("CreateVehicle");
                RedirectionHelper.RedirectCalls(srcMethod5, nullMethod);
            }
        }

        public override void OnLevelUnloading()
        {
            if(m_hideUI != null)
                GameObject.Destroy(m_hideUI);
            if (m_mInstance != null && m_mInstance.gameObject != null)
                GameObject.Destroy(m_mInstance.gameObject);
            MultiplayerManager.instance = null;
        }

        public static SaveGameMetaData GetMetaDataForDateTime(DateTime needle)
        {
            SaveGameMetaData result = null;
            foreach (Package.Asset current in PackageManager.FilterAssets(new Package.AssetType[]
                                                                          {
                UserAssetType.SaveGameMetaData
            }))
            {
                if (current != null && current.isEnabled)
                {
                    SaveGameMetaData saveGameMetaData = current.Instantiate<SaveGameMetaData>();
                    if (saveGameMetaData != null)
                    {
                        try
                        {
                            Stream s = saveGameMetaData.assetRef.GetStream();
                            SimulationMetaData mysimmeta = DataSerializer.Deserialize<SimulationMetaData>(s, DataSerializer.Mode.File);
                            if (mysimmeta.m_currentDateTime.Equals(needle))
                            {
                                return saveGameMetaData;
                            }
                        }
                        catch (Exception ex)
                        {
                            ex.ToString();
                        }
                    }
                }
            }
            return result;
        }
    }

    public class ModTerrainUtil : TerrainExtensionBase
    {
        private static ITerrain terrain = null;

        /// <summary>
        /// Gets the height of the terrain on position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static float GetHeight(float x, float z)
        {
            if (terrain == null)
            {
                return 0.0f;
            }

            return terrain.SampleTerrainHeight(x, z);
        }

        public override void OnCreated(ITerrain _terrain)
        {
            terrain = _terrain;
        }
    }

    //Setting unlimited money to avoid bankruptcy popups
    public class UnlimitedMoneyEconomy : EconomyExtensionBase
    {
        public override long OnUpdateMoneyAmount(long internalMoneyAmount)
        {
            return long.MaxValue;
        }

        public override bool OverrideDefaultPeekResource
        {
            get { return true; }
        }

        public override int OnPeekResource(EconomyResource resource, int amount)
        {
            return amount;
        }
    }

    //Make sure you never reach milestones to avoid popups
    public class NoMilestonesEconomy : MilestonesExtensionBase
    {
        public override int OnGetPopulationTarget(int originalTarget, int scaledTarget)
        {
            return base.OnGetPopulationTarget(int.MaxValue - 1, int.MaxValue - 1);
        }
    }
}
