using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using ColossalFramework;
using UnityEngine;
using System.Reflection;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using ICities;
using Lidgren.Network;
using SkylinesMultiplayer.Prefabs;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace SkylinesMultiplayer 
{
    public class MultiplayerManager : MonoBehaviour
    {
        public static MultiplayerManager instance;

        public NetServer m_server;
        public NetClient m_client;

        public VehicleNetworkManager m_vNetworkManager;
        public CitizenNetworkManager m_cNetworkManager;

        public bool m_isServer;
        private bool m_failedToConnect;

        public List<NetworkPlayer> m_players = new List<NetworkPlayer>();

        private float m_simulationTimer;

        private UInt64 m_saveWorkshopID;

        void Awake()
        {
            instance = this;
            gameObject.AddComponent<Scoreboard>();
        }

        private int m_lastId = 1;
        void Start()
        {
            if (!MultiplayerLoading.UseVehiclesAndCitizens || !ServerSettings.IsHosting)
            {
                //TODO Rewrite this so it actully remove all vehicles not just the first 0x4000
                var vInstace = Singleton<VehicleManager>.instance;
                var cInstace = Singleton<CitizenManager>.instance;
                for (int i = 0; i < 0x4000; i++)
                {
                    vInstace.ReleaseVehicle((ushort)i);
                }

                InvokeRepeating("RemoveCitizens", 0, 1);
            }

            if (ServerSettings.IsHosting)
            {
                //Need to reset this so you not act like a host if you dc then join a server, should probely move this to the menu
                ServerSettings.IsHosting = false;

                var savegameMetaData = MultiplayerLoading.GetMetaDataForDateTime(SimulationManager.instance.m_metaData.m_currentDateTime);
                m_saveWorkshopID = savegameMetaData.assetRef.package.GetPublishedFileID().AsUInt64;
                InvokeRepeating("SendHeartBeat", 0, 160);

                StartServer();
            }
            else
            {
                ConnectToServer();
            }
        }

        void RemoveCitizens()
        {
            CitizenManager manager = Singleton<CitizenManager>.instance;
            int length = manager.m_instances.m_buffer.Length;

            if(m_lastId > length)
                return;

            try
            {
                for (ushort i = 0; i < 0x2710; i = (ushort)(i + 1))
                {
                    m_lastId = (ushort)((m_lastId + 1) % length);
                    CitizenInstance aInstace = manager.m_instances.m_buffer[m_lastId];
                    if ((aInstace.m_flags & CitizenInstance.Flags.Created) != CitizenInstance.Flags.None)
                    {
                        aInstace.m_sourceBuilding = 0;
                        aInstace.m_targetBuilding = 0;
                        manager.m_instances.m_buffer[m_lastId] = aInstace;
                        manager.ReleaseCitizenInstance((ushort)m_lastId);
                    }
                }
            }
            catch (Exception)
            {
            }
            
        }

        void SendHeartBeat()
        {
            if (m_server != null && m_saveWorkshopID != UInt64.MaxValue && !ServerSettings.Private)
            {
                using (var client = new WebClient())
                {
                    var values = new NameValueCollection
                    {
                        {"name", ServerSettings.Name.IsNullOrWhiteSpace() ? "No name" : ServerSettings.Name},
                        {"port", ServerSettings.Port.ToString()},
                        {"mapId", m_saveWorkshopID.ToString()},
                        {"modVersion", Settings.ModVersion.ToString()}
                    };
                    var result = client.UploadValues(Settings.ServerUrl + "/create", "POST", values);
                    string responsebody = Encoding.UTF8.GetString(result);
                    Debug.Log("Sending to masterserver: " + responsebody);
                }
            }
        }

        void OnDestroy()
        {
            if(m_server != null)
                m_server.Shutdown("Shutting down server");
            if(m_client != null)
                m_client.Disconnect("Client Disconnecting");
        }

        void LateUpdate()
        {
            m_simulationTimer += Time.deltaTime;
            if (m_simulationTimer >= 60f)
            {
                m_simulationTimer -= 60f;
            }

            Vector4 vector;
            vector.x = m_simulationTimer;
            vector.y = m_simulationTimer * 0.01666667f;
            vector.z = m_simulationTimer * 6.283185f;
            vector.w = m_simulationTimer * 0.1047198f;
            Shader.SetGlobalVector("_SimulationTime", vector);
        }
       
        void Update()
        {
            NetIncomingMessage msg;
            while (m_server != null && (msg = m_server.ReadMessage()) != null)
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
                    case NetIncomingMessageType.StatusChanged:
                        NetConnectionStatus status = (NetConnectionStatus)msg.ReadByte();

                        string reason = msg.ReadString();
                        Debug.Log(NetUtility.ToHexString(msg.SenderConnection.RemoteUniqueIdentifier) + " " + status + ": " + reason);

                        if (status == NetConnectionStatus.Connected)
                        {
                            Debug.Log("Remote connected: " + msg.SenderConnection.RemoteHailMessage.ReadString());

                            NetOutgoingMessage om = m_server.CreateMessage();
                            
                            om.Write((int)MessageFunction.SpawnAllPlayer);
                            om.Write(m_players.Count);
                            
                            foreach (var player in m_players)
                            {
                                om.Write(player.ID);
                                om.Write(player.Name);
                            }
                            m_server.SendMessage(om, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
                        }
                        else if (status == NetConnectionStatus.Disconnected)
                        {
                            MultiplayerManager mInstance = MultiplayerManager.instance;

                            var player = m_players.FirstOrDefault(x => x.OwnerConnection == msg.SenderConnection);
                            if (player != null)
                            {
                                MessageRemovePlayer message = new MessageRemovePlayer();
                                message.playerId = player.ID;
                                mInstance.SendNetworkMessage(message, SendTo.All);
                            }
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        HandleSeverMessage(msg);
                        break;
                    case NetIncomingMessageType.UnconnectedData:
                        var msgType = (UnConnectedMessageFunction)msg.ReadInt32();
                        switch (msgType)
                        {
                            case UnConnectedMessageFunction.PingServerForPlayerCount:
                                NetOutgoingMessage returnMessage = m_server.CreateMessage();
                                returnMessage.Write((int)UnConnectedMessageFunction.PingServerForPlayerCount);
                                returnMessage.Write(m_players.Count);
                                m_server.SendUnconnectedMessage(returnMessage, msg.SenderEndPoint);
                            break;
                            case UnConnectedMessageFunction.PingServerForMapId:
                                NetOutgoingMessage returnMessage2 = m_server.CreateMessage();
                                returnMessage2.Write((int)UnConnectedMessageFunction.PingServerForMapId);
                                returnMessage2.Write(m_players.Count);
                                returnMessage2.Write(m_saveWorkshopID);
                                m_server.SendUnconnectedMessage(returnMessage2, msg.SenderEndPoint);
                            break;
                        }
                        
                        break;
                    default:
                        Debug.LogWarning("Unhandled type: " + msg.MessageType);
                        break;
                }
                m_server.Recycle(msg);
            }

            while (m_client != null && (msg = m_client.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.VerboseDebugMessage:
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        Debug.Log(msg.ReadString());
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        if (msg.ReadString() == "=Failed".Trim())
                        {
                            Debug.Log("It failed");
                            m_failedToConnect = true;
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        HandleClientMessage(msg);
                        break;
                    default:
                        Debug.LogWarning("Unhandled type: " + msg.MessageType);
                        break;
                }
                m_client.Recycle(msg);
            }
        }

        void HandleSeverMessage(NetIncomingMessage msg)
        {
            MessageFunction type = (MessageFunction)msg.ReadInt32();
            switch (type)
            {
                case MessageFunction.ClassMessage:
                    SendTo sendTo = (SendTo)msg.ReadInt32();
                    string assemblyName = msg.ReadString();
                    string classType = msg.ReadString();
                    Message report = (Message)Activator.CreateInstance(assemblyName, classType).Unwrap();
                    msg.ReadAllFields(report);
                    SendNetworkMessage(report, sendTo, msg);
                    break;
                case MessageFunction.SpawnPlayer:
                    // broadcast this to all connections, except sender
                    List<NetConnection> all2 = m_server.Connections; // get copy
                    all2.Remove(msg.SenderConnection);

                    int id2 = msg.ReadInt32();
                    string playerName = msg.ReadString();
                    if (all2.Count > 0)
                    {
                        NetOutgoingMessage om = m_server.CreateMessage();
                        om.Write((int)MessageFunction.SpawnPlayer);
                        om.Write(id2);
                        om.Write(playerName);

                        m_server.SendMessage(om, all2, NetDeliveryMethod.ReliableUnordered, 0);
                    }
                    SpawnPlayer(id2, playerName, msg.SenderConnection);
                    break;
                default:
                    Debug.LogWarning("Unhandled msg type: " + type.ToString());
                    break;
            }
        }

        void HandleClientMessage(NetIncomingMessage msg)
        {
            MessageFunction type = (MessageFunction)msg.ReadInt32();
            switch (type)
            {
                case MessageFunction.ClassMessage:
                    string assemblyName = msg.ReadString();
                    string classType = msg.ReadString();
                    Message report = (Message)Activator.CreateInstance(assemblyName, classType).Unwrap();
                    msg.ReadAllFields(report);
                    report.OnCalled(report, msg);
                    break;
                case MessageFunction.SpawnAllPlayer:
                    int count = msg.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        int id = msg.ReadInt32();
                        string playerName = msg.ReadString();

                        SpawnPlayer(id, playerName);
                    }

                    int newId = SpawnLocalPlayer();
                    MessageSpawnPlayer spawnMessage = new MessageSpawnPlayer();
                    spawnMessage.playerId = newId;
                    spawnMessage.playerName = Steam.personaName;
                    SendNetworkMessage(spawnMessage, SendTo.Others);
                    break;
                case MessageFunction.UpdateVehiclesPositions:
                    m_vNetworkManager.UpdateCarsPositions(msg);
                    break;
                case MessageFunction.RemoveVehicle:
                    int carID2 = msg.ReadInt32();
                    m_vNetworkManager.RemoveCar(carID2);
                    break;
                case MessageFunction.UpdateCitizensPositions:
                    m_cNetworkManager.UpdateCitizensPositions(msg);
                    break;
                default:
                    Debug.LogWarning("Unhandled msg type: " + type.ToString());
                    break;
            }
        }

        public void SpawnPlayer(int id, string playerName, NetConnection ownerConnection = null)
        {
            GameObject clone = PlayerRemotePrefab.Create(id);

            NetworkPlayer playerInfo = new NetworkPlayer();
            playerInfo.ID = id;
            playerInfo.Name = playerName;
            playerInfo.PlayerGameObject = clone;
            playerInfo.OwnerConnection = ownerConnection;
            m_players.Add(playerInfo);

            Debug.Log("Player " + playerName + " has connected.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Playerid</returns>
        int SpawnLocalPlayer()
        {
            int id = Random.Range(1, 1000000);    //TODO Change this to something that is certain to be uniqe
            
            GameObject clone = PlayerLocalPrefab.Create(id);
            clone.transform.position = new Vector3(1800, 150, -1100);
            
            NetworkPlayer playerInfo = new NetworkPlayer();
            playerInfo.ID = id;
            playerInfo.Name = Steam.personaName;
            playerInfo.PlayerGameObject = clone;
            m_players.Add(playerInfo);

            GameObject.FindObjectOfType<CameraController>().GetComponent<HideUI>().MouseLocked = true;

            return id;
        }
        
        public void SendNetworkMessage(Message message, SendTo sendTo, NetIncomingMessage netMessage = null, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.ReliableOrdered)
        {
            if (m_isServer)
            {
                Type messageType = message.GetType();
                NetOutgoingMessage om = m_server.CreateMessage();
                om.Write((int)MessageFunction.ClassMessage);;
                om.Write(messageType.Assembly.ToString());
                om.Write(messageType.ToString());
                om.WriteAllFields(message);
               
                NetConnection sender = null;
                if (netMessage != null)
                {
                    sender = netMessage.SenderConnection;
                }

                switch (sendTo)
                {
                    case SendTo.All:
                        message.OnCalled(message, netMessage);
                        m_server.SendToAll(om, sender, deliveryMethod, 0);
                        break;
                    case SendTo.Others:
                        m_server.SendToAll(om, sender, deliveryMethod, 0);
                        if (sender != null)  //If server diden't send the message
                            message.OnCalled(message, netMessage);
                        break;
                    case SendTo.Server:
                        message.OnCalled(message, netMessage);
                        break;
                }
            }
            else
            {
                Type messageType = message.GetType();
                NetOutgoingMessage om = m_client.CreateMessage();
                om.Write((int)MessageFunction.ClassMessage);
                om.Write((int) sendTo);
                om.Write(messageType.Assembly.ToString());
                om.Write(messageType.ToString());
                om.WriteAllFields(message);
                m_client.SendMessage(om, deliveryMethod);
                
                if (sendTo == SendTo.All) //Trigger for sender if you sent it to everybody
                {
                    message.OnCalled(message, netMessage);
                }
            }
        }

        void StartServer()
        {
            NetPeerConfiguration config = new NetPeerConfiguration("FPSMod");

            Debug.Log("Starting Server");

            m_isServer = true;

            config.Port = ServerSettings.Port;
            config.SetMessageTypeEnabled(NetIncomingMessageType.UnconnectedData, true);

            m_server = new NetServer(config);
            m_server.Start();

            SpawnLocalPlayer();
            Debug.Log("wID: " + m_saveWorkshopID);
            Debug.Log("p: " + ServerSettings.Private);

            if (MultiplayerLoading.UseVehiclesAndCitizens)
            {
                m_vNetworkManager = gameObject.AddComponent<VehicleNetworkManager>();
                m_cNetworkManager = gameObject.AddComponent<CitizenNetworkManager>();
            }

            if (m_saveWorkshopID != UInt64.MaxValue && !ServerSettings.Private)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        var values = new NameValueCollection
                            {
                                {"name", ServerSettings.Name.IsNullOrWhiteSpace() ? "No name" : ServerSettings.Name},
                                {"port", ServerSettings.Port.ToString()},
                                {"mapId", m_saveWorkshopID.ToString()},
                                {"modVersion", Settings.ModVersion.ToString()}
                            };
                        var result = client.UploadValues(Settings.ServerUrl + "/create", "POST", values);
                        string responsebody = Encoding.UTF8.GetString(result);
                        Debug.Log("Sending to masterserver: " + responsebody);
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

        }

        void ConnectToServer()
        {
            NetPeerConfiguration config = new NetPeerConfiguration("FPSMod");

            Debug.Log("Joining Server " + ConnectSettings.Ip + ": " + ConnectSettings.Port);

            m_client = new NetClient(config);
            m_client.Start();
            NetOutgoingMessage hail = m_client.CreateMessage("This is the hail message");
            m_client.Connect(ConnectSettings.Ip, ConnectSettings.Port, hail);

            if (MultiplayerLoading.UseVehiclesAndCitizens)
            {
                m_vNetworkManager = gameObject.AddComponent<VehicleNetworkManager>();
                m_cNetworkManager = gameObject.AddComponent<CitizenNetworkManager>();
            }
        }

        void OnGUI()
        {
            if (m_failedToConnect)
            {
                GUI.Box(new Rect(Screen.width/2 - 100, Screen.height/2 - 35, 200, 25), "Failed to connect to server.");
                if(GUI.Button(new Rect(Screen.width/2 - 100, Screen.height/2, 200, 20), "Disconnect"))
                {
                    Singleton<LoadingManager>.instance.UnloadLevel();
                }
            }
            else if (m_client != null && m_client.ConnectionStatus != NetConnectionStatus.Connected)
            {
                GUI.Box(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 35, 200, 25), "Connecting...");
                if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2, 200, 20), "Disconnect"))
                {
                    Singleton<LoadingManager>.instance.UnloadLevel();
                }
            }

            /*if (m_client != null)
            {
                 string statistics = "\nVehicles Pool\nAvailable " + m_vNetworkManager.m_fakeCarsPoolAvailable.Count +
                                     "\nInuse: " + m_vNetworkManager.m_fakeCarsPoolInUse.Count +
                                     "\n\n Citizen Pool\nAvailable: " + m_cNetworkManager.m_fakeCitizensPoolAvailable.Count +
                                     "\nInuse: " + m_cNetworkManager.m_fakeCitizensPoolInUse.Count; ;

                 GUI.Box(new Rect(10, 300, 120, 300), statistics);
            }
            else if (m_server != null)
            {
                int carSync = 0;
                m_vNetworkManager.m_fakeCars.ForEach(x => carSync += x.Value.Count);
                int cimSync = 0;
                m_cNetworkManager.m_fakeCitizens.ForEach(x => cimSync += x.Value.Count);
                string statistics = "Vehicls send: " + carSync + "\n" +
                                    "Citzien send: " + cimSync + "\n" +
                                    "Send rate: " + m_lastSecondBytesSend + "\n" +
                                    "Recive rate: " + m_lastSecondBytesRecived;
                GUI.Box(new Rect(10, 300, 120, 300), statistics);
            }*/
        }
    }

}
