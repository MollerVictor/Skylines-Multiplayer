using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using Lidgren.Network;
using UnityEditor;
using UnityEngine;

namespace SkylinesMultiplayer
{
    class JoinByIpPanel : UIPanel
    {
        private const string CONNECTING_STRING          = "Connecting...";
        private const string INVALID_IP_STRING          = "IP invalid format";
        private const string INVALID_PORT_STRING        = "Port invalid format"; 
        private const string COULD_NOT_CONNECT_STRING   = "Could not connect to server.";
        private const string DOWNLOAD_MAP_STRING        = "Please subscribe to the map,\nto join the game.";
        private const string DOWNLOADING_STRING         = "Downloading... {0}%";
        private const string FINSISH_DOWNLOAD_STRING    = "Map finished downloading, starting game.";
        

        private const int TIMEOUT_TIME = 6; //How long before stop accepting messages from the server when connecting
        

        public bool Visible { get; set; }

        private UITextField m_ipTextField;
        private UITextField m_portTextField;
        private UILabel m_connectionInfoLabel;

        private NetClient m_netClient;
        private IPEndPoint m_pingIp;
        private bool m_connecting;
        private float m_connectingTimeOutTimer;
        private ulong m_mapID;
        private bool m_downloadingMap;

        public override void Start()
        {
            this.backgroundSprite = "MenuPanel";
            this.color = new Color32(50, 50, 50, 255);
            this.width = 350;
            this.height = 185;
            this.isVisible = false;
            this.position += new Vector3(-this.width/2, this.height/2 + 50);
 

            UILabel ipLabel = this.AddUIComponent<UILabel>();
            ipLabel.text = "IP:";
            ipLabel.pivot = UIPivotPoint.BottomRight;
            ipLabel.relativePosition = new Vector2(10, 10);

            m_ipTextField = CreateTextField();
            m_ipTextField.width = 140;
            m_ipTextField.text = "127.0.0.1";
            m_ipTextField.pivot = UIPivotPoint.BottomRight;
            m_ipTextField.relativePosition = new Vector2(55, 10);
            

            UILabel portLabel = this.AddUIComponent<UILabel>();
            portLabel.text = "Port:";
            portLabel.pivot = UIPivotPoint.BottomRight;
            portLabel.relativePosition = new Vector2(10, 45);

            m_portTextField = CreateTextField();
            m_portTextField.numericalOnly = true;
            m_portTextField.text = "27015";
            m_portTextField.pivot = UIPivotPoint.BottomRight;
            m_portTextField.relativePosition = new Vector2(55, 45);

            UIButton connectButton = CreateButton();
            connectButton.text = "CONNECT";
            connectButton.width = 100;
            connectButton.height = 60;
            connectButton.pivot = UIPivotPoint.BottomRight;
            connectButton.relativePosition = new Vector2(this.width - connectButton.width - 110, this.height - connectButton.height - 5);
            connectButton.eventClick += OnClickConnect;

            UIButton closeButton = CreateButton();
            closeButton.text = "CANCEL";
            closeButton.width = 100;
            closeButton.height = 60;
            closeButton.pivot = UIPivotPoint.BottomRight;
            closeButton.relativePosition = new Vector2(this.width - closeButton.width - 5, this.height - closeButton.height - 5);
            closeButton.eventClick += OnClickCancel;

            m_connectionInfoLabel = this.AddUIComponent<UILabel>();
            m_connectionInfoLabel.text = "";
            m_connectionInfoLabel.pivot = UIPivotPoint.TopLeft;
            m_connectionInfoLabel.relativePosition = new Vector2(10, 80);
            m_connectionInfoLabel.textAlignment = UIHorizontalAlignment.Left;


            NetPeerConfiguration config = new NetPeerConfiguration("FPSMod");
            config.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, true);

            m_netClient = new NetClient(config);
            m_netClient.Start();
        }

        
        private void Update()
        {
            if (m_mapID != 0)
            {
                float subscribedItemProgress = Steam.workshop.GetSubscribedItemProgress(new PublishedFileId(m_mapID));

                if (subscribedItemProgress > 0)
                {
                    m_downloadingMap = true;
                    this.m_connectionInfoLabel.text = string.Format(DOWNLOADING_STRING, subscribedItemProgress * 100);
                }
                else if (m_downloadingMap)
                {
                    Package.AssetType[] assetTypes = new Package.AssetType[] { UserAssetType.SaveGameMetaData };
                    var package = PackageManager.FilterAssets(assetTypes).FirstOrDefault(x => x.package.GetPublishedFileID().AsUInt64 == m_mapID);

                    if (package != null)
                    {
                        m_downloadingMap = false;
                        StartGame(package);
                    }

                    this.m_connectionInfoLabel.text = FINSISH_DOWNLOAD_STRING;
                }
            }

            HandleIncomingNetworkMessages();
            
            if(m_connectingTimeOutTimer < 0 && m_connecting)
            {
                m_connectionInfoLabel.text = COULD_NOT_CONNECT_STRING;
                m_connecting = false;
            }
            else
            {
                m_connectingTimeOutTimer -= Time.deltaTime;
            }
        }

        private void HandleIncomingNetworkMessages()
        {
            NetIncomingMessage msg;
            while (m_netClient != null && (msg = m_netClient.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.DebugMessage:
                        Debug.Log(msg.ReadString());
                        break;
                    case NetIncomingMessageType.WarningMessage:
                        Debug.LogWarning(msg.ReadString());
                        break;
                    case NetIncomingMessageType.ErrorMessage:
                        Debug.LogError(msg.ReadString());
                        break;
                    case NetIncomingMessageType.UnconnectedData:
                        int msgType = msg.ReadInt32();
                        if (msgType == (int)UnConnectedMessageFunction.PingServerForMapId)
                        {
                            int playerCount = msg.ReadInt32();
                            ulong mapID = msg.ReadUInt64();
                            Debug.Log("got response " + playerCount + " " + mapID);
                            ConnectToServer(mapID);
                            m_mapID = mapID;
                        }
                        break;
                    default:
                        Debug.LogWarning("Unhandled type: " + msg.MessageType);
                        break;
                }
                m_netClient.Recycle(msg);
            }
        }

        private void OnClickConnect(UIComponent component, UIMouseEventParameter eventparam)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(m_ipTextField.text, out ip))
            {
                Debug.Log("Ip error");
                m_connectionInfoLabel.text = INVALID_IP_STRING; 
                return;
            }

            int port;
            if (!int.TryParse(m_portTextField.text, out port) && port > 1 && port < 50000)
            {
                Debug.Log("port error");
                m_connectionInfoLabel.text = INVALID_PORT_STRING;
                return;
            }

            m_connecting = true;
            m_connectingTimeOutTimer = TIMEOUT_TIME;

            ConnectSettings.Ip = m_ipTextField.text;
            ConnectSettings.Port = port;

            NetOutgoingMessage msg = m_netClient.CreateMessage();
            msg.Write((int)UnConnectedMessageFunction.PingServerForMapId);

            int ipInt = BitConverter.ToInt32(ip.GetAddressBytes(), 0);
            IPAddress ipAddress = new IPAddress(ipInt);
            IPEndPoint receiver = new IPEndPoint(ipAddress, port);

            m_netClient.SendUnconnectedMessage(msg, receiver);

            m_connectionInfoLabel.text = CONNECTING_STRING;
        }

        private void OnClickCancel(UIComponent component, UIMouseEventParameter eventparam)
        {
            this.Hide();
        }

        private UITextField CreateTextField()
        {
            UITextField textField = this.AddUIComponent<UITextField>();

            textField.maxLength = 100;
            textField.enabled = true;
            textField.builtinKeyNavigation = true;
            textField.isInteractive = true;
            textField.readOnly = false;
            textField.submitOnFocusLost = true;

            textField.horizontalAlignment = UIHorizontalAlignment.Left;
            textField.selectionSprite = "EmptySprite";
            textField.selectionBackgroundColor = new Color32(0, 171, 234, 255);
            textField.normalBgSprite = "TextFieldPanel";
            textField.textColor = new Color32(174, 197, 211, 255);
            textField.disabledTextColor = new Color32(254, 254, 254, 255);
            textField.textScale = 1.3f;
            textField.opacity = 1;
            textField.color = new Color32(58, 88, 104, 255);
            textField.disabledColor = new Color32(254, 254, 254, 255);

            return textField;
        }

        private UIButton CreateButton()
        {
            UIButton button = this.AddUIComponent<UIButton>();

            button.textPadding = new RectOffset(5, 5, 5, 5);
            button.normalBgSprite = "ButtonMenu";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.textColor = new Color32(255, 255, 255, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            button.focusedTextColor = new Color32(255, 255, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);

            return button;
        }


        private void ConnectToServer(ulong mapId)
        {
            m_connecting = false;

            Package.AssetType[] assetTypes = new Package.AssetType[] { UserAssetType.SaveGameMetaData };
            var package = PackageManager.FilterAssets(assetTypes).FirstOrDefault(x => x.package.GetPublishedFileID().AsUInt64 == mapId);

            if (package != null)
            {
                StartGame(package);
            }
            else
            {
                //TODO Also show a popup here as backup if the steamoverlay dosen't work
                Steam.ActivateGameOverlayToWebPage("http://steamcommunity.com/sharedfiles/filedetails/?id=" + mapId + "/");
                Debug.Log("Please Download mapid " + mapId);
                m_connectionInfoLabel.text = DOWNLOAD_MAP_STRING;
            }

            UIView.library.Hide(base.GetType().Name, 1);
        }

        void StartGame(Package.Asset package)
        {
            m_connectionInfoLabel.text = "Game Starting";

            SaveGameMetaData mmd = package.Instantiate<SaveGameMetaData>();
            SavePanel.lastLoadedName = package.name;
            Debug.Log(mmd.cityName);
            SimulationMetaData ngs = new SimulationMetaData
            {
                m_CityName = mmd.cityName,
                m_updateMode = SimulationManager.UpdateMode.LoadGame,
                m_environment = "",
                m_disableAchievements = SimulationMetaData.MetaBool.True
            };

            Singleton<LoadingManager>.instance.LoadLevel(mmd.assetRef, "Game", "InGame", ngs);
        }
    }
}
