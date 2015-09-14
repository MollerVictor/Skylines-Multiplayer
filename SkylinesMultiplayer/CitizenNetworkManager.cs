using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using JetBrains.Annotations;
using Lidgren.Network;
using UnityEngine;

namespace SkylinesMultiplayer
{
    public class CitizenNetworkManager : MonoBehaviour
    {
        private const int CITIZEN_POOL_SIZE = 150;
        private const int CITIZEN_POOL_NEAR_LIMIT = 130;    //How many cars should be in use before using the near search limit
        private const int SEARCH_SIZE = 9;                //How many grids at each direction it should look for cars
        private const int SEARCH_SIZE_NEAR_LIMIT = 7;
        private const int SEND_RATE = 10;    //How many times/sec the server should send the citizens postion

        public Dictionary<int, List<int>> m_fakeCitizens = new Dictionary<int, List<int>>();
        public Dictionary<int, NetworkCarObject> m_fakeCitizensCurrentPool = new Dictionary<int, NetworkCarObject>();
        public List<NetworkCarObject> m_fakeCitizensPoolAvailable = new List<NetworkCarObject>();
        public List<NetworkCarObject> m_fakeCitizensPoolInUse = new List<NetworkCarObject>();

        void Start()
        {
            if (MultiplayerManager.instance.m_isServer)
            {
                InvokeRepeating("ServerSendCitizenPositions", 0, 1 / (float)SEND_RATE);
            }
            else
            {
                var citizenParent = new GameObject("Citizens Network parent");
                for (int i = 0; i < CITIZEN_POOL_SIZE; i++)
                {
                    var obj = new GameObject("CitizenPoolObject " + i);
                    obj.transform.parent = citizenParent.transform;
                    obj.AddComponent<CitizenNetworkView>();
                    obj.AddComponent<MeshFilter>();
                    obj.AddComponent<MeshRenderer>();
                    obj.SetActive(false);
                    NetworkCarObject nCarObject = new NetworkCarObject();
                    nCarObject.GameObject = obj;
                    m_fakeCitizensPoolAvailable.Add(nCarObject);
                }
            }
        }

        void ServerSendCitizenPositions()
        {
            if (MultiplayerManager.instance.m_isServer)
            {
                foreach (var player in MultiplayerManager.instance.m_players)
                {
                    if (player.OwnerConnection == null)
                        continue;

                    if (!m_fakeCitizens.ContainsKey(player.ID))
                        m_fakeCitizens.Add(player.ID, new List<int>());

                    int searchSize = m_fakeCitizens[player.ID].Count > CITIZEN_POOL_NEAR_LIMIT ? SEARCH_SIZE_NEAR_LIMIT : SEARCH_SIZE;

                    int gridX = Mathf.Clamp((int)((player.PlayerGameObject.transform.position.x / 8f) + 1080f), 0, 0x86f);
                    int gridZ = Mathf.Clamp((int)((player.PlayerGameObject.transform.position.z / 8f) + 1080f), 0, 0x86f);

                    var cInstance = Singleton<CitizenManager>.instance;
                    var mInstance = MultiplayerManager.instance;

                    bool breakLoop = false;
                    //Adds all cars in the nearby grids
                    for (int k = -searchSize; k < searchSize; k++)
                    {
                        for (int l = -searchSize; l < searchSize; l++)
                        {
                            int index = ((gridZ + k) * 2160) + gridX + l;
                            int citizenID = cInstance.m_citizenGrid[index];
                            while (citizenID != 0)
                            {
                                var citizenInstace = cInstance.m_instances.m_buffer[citizenID];
                                if (!m_fakeCitizens[player.ID].Contains(citizenID))
                                {
                                    if (m_fakeCitizens[player.ID].Count >= CITIZEN_POOL_SIZE)
                                    {
                                        breakLoop = true;
                                        break;
                                    }
                                    
                                    
                                    if ((citizenInstace.m_flags & CitizenInstance.Flags.Created) != CitizenInstance.Flags.None)
                                    {
                                        m_fakeCitizens[player.ID].Add(citizenID);
                                    }
                                }
                                citizenID = citizenInstace.m_nextGridInstance;
                            }
                        }
                        if (breakLoop) break;
                    }

                    var citizenToRemove = new List<int>();
                    NetOutgoingMessage om2 = mInstance.m_server.CreateMessage();
                    om2.Write((int)MessageFunction.UpdateCitizensPositions);
                    om2.Write(m_fakeCitizens[player.ID].Count);
                    foreach (var citizenID in m_fakeCitizens[player.ID])
                    {
                        var citizenInstace = cInstance.m_instances.m_buffer[citizenID];
                        var citizenInfo = citizenInstace.Info;
                        
                        Vector3 citizenPosition;
                        Quaternion citizenRotation;
                        citizenInstace.GetSmoothPosition((ushort)citizenID, out citizenPosition, out citizenRotation);

                        Vector3 distance = player.PlayerGameObject.transform.position;
                        

                        //We need to check position manully insted of using grids for when removing citizen,
                        //becuse the a citizen switch grids it disappears for one simulation frame
                        if (Vector3.SqrMagnitude(player.PlayerGameObject.transform.position - citizenPosition) > Math.Pow((SEARCH_SIZE + 1) * 32 + 10, 2) || (citizenInstace.m_flags & CitizenInstance.Flags.Created) == CitizenInstance.Flags.None)
                        {
                            citizenToRemove.Add(citizenID);
                        }
                        
                        om2.Write(citizenID);
                        om2.Write(citizenInfo.m_prefabDataIndex);

                        om2.Write(citizenPosition.x);
                        om2.Write(citizenPosition.y);
                        om2.Write(citizenPosition.z);

                        om2.Write(citizenRotation.x);
                        om2.Write(citizenRotation.y);
                        om2.Write(citizenRotation.z);
                        om2.Write(citizenRotation.w);
                    }
                    mInstance.m_server.SendMessage(om2, player.OwnerConnection, NetDeliveryMethod.Unreliable, 0);
                    
                    citizenToRemove.ForEach(x =>
                    {
                        m_fakeCitizens[player.ID].Remove(x);

                        /*NetOutgoingMessage om3 = MultiplayerManager.m_server.CreateMessage();
                        om3.Write((int)MessageFunction.RemoveVehicle);
                        om3.Write(x);
                        MultiplayerManager.m_server.SendMessage(om3, player.OwnerConnection, NetDeliveryMethod.ReliableUnordered, 0);*/
                    });
                }
            }
        }

        public void UpdateCitizensPositions(NetIncomingMessage msg)
        {
            int carCount = msg.ReadInt32();
            for (int i = 0; i < carCount; i++)
            {
                int carID = msg.ReadInt32();
                int prefabIndex = msg.ReadInt32();

                Vector3 pos = new Vector3();
                pos.x = msg.ReadFloat();
                pos.y = msg.ReadFloat();
                pos.z = msg.ReadFloat();

                Quaternion rot2 = new Quaternion();
                rot2.x = msg.ReadFloat();
                rot2.y = msg.ReadFloat();
                rot2.z = msg.ReadFloat();
                rot2.w = msg.ReadFloat();


                if (!m_fakeCitizensCurrentPool.ContainsKey(carID) && m_fakeCitizensPoolAvailable.Count > 0)
                {
                    var obj = m_fakeCitizensPoolAvailable[0];
                    m_fakeCitizensPoolAvailable.RemoveAt(0);
                    m_fakeCitizensPoolInUse.Add(obj);
                    m_fakeCitizensCurrentPool.Add(carID, obj);

                    obj.GameObject.GetComponent<CitizenNetworkView>().ResertState();
                    obj.GameObject.GetComponent<CitizenNetworkView>().m_id = carID;
                    var prefab = PrefabCollection<CitizenInfo>.GetPrefab((uint)prefabIndex);
                    obj.GameObject.GetComponent<MeshFilter>().sharedMesh = prefab.m_lodMesh;
                    obj.GameObject.GetComponent<MeshRenderer>().sharedMaterial = prefab.m_lodMaterial;
                    obj.GameObject.SetActive(true);
                }
                if (m_fakeCitizensCurrentPool.ContainsKey(carID))
                    m_fakeCitizensCurrentPool[carID].GameObject.GetComponent<CitizenNetworkView>().OnNetworkData(msg.ReceiveTime, pos, rot2);
            }
        }

        public void RemoveCitizen(int carID)
        {
            if (m_fakeCitizensCurrentPool.ContainsKey(carID))
            {
                var obj2 = m_fakeCitizensCurrentPool[carID];
                m_fakeCitizensPoolInUse.Remove(obj2);
                m_fakeCitizensCurrentPool.Remove(carID);
                m_fakeCitizensPoolAvailable.Add(obj2);
                obj2.GameObject.transform.position = new Vector3(0, -1000, 0);
                obj2.GameObject.SetActive(false);
            }
        }
    }
}
