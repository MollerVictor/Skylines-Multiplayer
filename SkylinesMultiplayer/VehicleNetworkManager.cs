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
    public class VehicleNetworkManager : MonoBehaviour
    {
        private const int CAR_POOL_SIZE = 150;
        private const int CAR_POOL_NEAR_LIMIT = 130;    //How many cars should be in use before using the near search limit
        private const int SEARCH_SIZE = 7;                //How many grids at each direction it should look for cars
        private const int SEARCH_SIZE_NEAR_LIMIT = 5;
        private const int SEND_RATE = 10;    //How many times/sec the server should send the vehicles postion

        public Dictionary<int, List<int>> m_fakeCars = new Dictionary<int, List<int>>();
        public Dictionary<int, NetworkCarObject> m_fakeCarsCurrentPool = new Dictionary<int, NetworkCarObject>();
        public List<NetworkCarObject> m_fakeCarsPoolAvailable = new List<NetworkCarObject>();
        public List<NetworkCarObject> m_fakeCarsPoolInUse = new List<NetworkCarObject>();

        void Start()
        {
            if (MultiplayerManager.instance.m_isServer)
            {
                InvokeRepeating("ServerSendCarPositions", 0, 1 / (float)SEND_RATE);
            }
            else
            {
                var vehicleParent = new GameObject("Vehicles Network parent");
                for (int i = 0; i < CAR_POOL_SIZE; i++)
                {
                    var obj = new GameObject("CarPoolObject " + i);
                    obj.transform.parent = obj.transform;
                    obj.AddComponent<VehicleNetworkView>();
                    obj.AddComponent<MeshFilter>();
                    obj.AddComponent<MeshRenderer>();
                    obj.SetActive(false);
                    NetworkCarObject nCarObject = new NetworkCarObject();
                    nCarObject.GameObject = obj;
                    m_fakeCarsPoolAvailable.Add(nCarObject);
                }
            }
        }

        void ServerSendCarPositions()
        {
            if (MultiplayerManager.instance.m_isServer)
            {
                foreach (var player in MultiplayerManager.instance.m_players)
                {
                    if(player.OwnerConnection == null)
                        continue;
                    
                    if(!m_fakeCars.ContainsKey(player.ID))
                        m_fakeCars.Add(player.ID, new List<int>());

                    int searchSize = m_fakeCars[player.ID].Count > CAR_POOL_NEAR_LIMIT ? SEARCH_SIZE_NEAR_LIMIT : SEARCH_SIZE;
                    
                    int gridX = Mathf.Clamp((int)((player.PlayerGameObject.transform.position.x / 32f) + 270f), 0, 0x21b);
                    int gridZ = Mathf.Clamp((int)((player.PlayerGameObject.transform.position.z / 32f) + 270f), 0, 0x21b);

                    var vInstance = Singleton<VehicleManager>.instance;
                    var mInstance = MultiplayerManager.instance;

                    bool breakLoop = false;
                    //Adds all cars in the nearby grids
                    for (int k = -searchSize; k < searchSize; k++)
                    {
                        for (int l = -searchSize; l < searchSize; l++)
                        {
                            int index = ((gridZ + k) * 540) + gridX + l;
                            int carID = vInstance.m_vehicleGrid[index];
                            while (carID != 0)
                            {
                                var vehicle = vInstance.m_vehicles.m_buffer[carID];
                                if (!m_fakeCars[player.ID].Contains(carID))
                                {

                                    if ((vehicle.m_flags & Vehicle.Flags.Spawned) != Vehicle.Flags.None || vehicle.Info.m_instanceID.ParkedVehicle == 0)
                                    {
                                        if (m_fakeCars[player.ID].Count >= CAR_POOL_SIZE)
                                        {
                                            breakLoop = true;
                                            break;
                                        }
                                        
                                        m_fakeCars[player.ID].Add(carID);
                                    }
                                }
                                carID = vehicle.m_nextGridVehicle;
                            }
                        }
                        if (breakLoop) break;
                    }

                    var carsToRemove = new List<int>();

                    foreach (var carID in m_fakeCars[player.ID])
                    {
                        var vehicle = vInstance.m_vehicles.m_buffer[carID];
                        Vector3 carPosition;
                        Quaternion carRotation;
                        vehicle.GetSmoothPosition((ushort)carID, out carPosition, out carRotation);

                        if ((vehicle.m_flags & Vehicle.Flags.Spawned) == Vehicle.Flags.None || vehicle.Info.m_instanceID.ParkedVehicle != 0 || Vector3.SqrMagnitude(player.PlayerGameObject.transform.position - carPosition) > Math.Pow((SEARCH_SIZE + 1) * 32 + 10, 2))
                        {
                            carsToRemove.Add(carID);
                        }
                    }

                    carsToRemove.ForEach(x =>
                    {
                        m_fakeCars[player.ID].Remove(x);

                        NetOutgoingMessage om3 = mInstance.m_server.CreateMessage();
                        om3.Write((int)MessageFunction.RemoveVehicle);
                        om3.Write(x);
                        mInstance.m_server.SendMessage(om3, player.OwnerConnection, NetDeliveryMethod.ReliableUnordered, 0);
                    });

                    NetOutgoingMessage om2 = mInstance.m_server.CreateMessage();
                    om2.Write((int)MessageFunction.UpdateVehiclesPositions);
                    om2.Write(m_fakeCars[player.ID].Count);
                    foreach (var carID in m_fakeCars[player.ID])
                    {
                        var vehicle = vInstance.m_vehicles.m_buffer[carID];
                        Vector3 carPosition;
                        Quaternion carRotation;
                        vehicle.GetSmoothPosition((ushort)carID, out carPosition, out carRotation);

                        //We need to check position manully insted of using grids for when removing vehicles,
                        //becuse the a vehicle switch grids it disappears for one simulation frame
                        
                        om2.Write(carID);
                        om2.Write(vehicle.Info.m_prefabDataIndex);
                        
                        om2.Write(carPosition.x);
                        om2.Write(carPosition.y);
                        om2.Write(carPosition.z);

                        om2.Write(carRotation.x);
                        om2.Write(carRotation.y);
                        om2.Write(carRotation.z);
                        om2.Write(carRotation.w);
                    }
                    mInstance.m_server.SendMessage(om2, player.OwnerConnection, NetDeliveryMethod.Unreliable, 0);
                    

                }
            }
        }

        public void UpdateCarsPositions(NetIncomingMessage msg)
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


                if (!m_fakeCarsCurrentPool.ContainsKey(carID) && m_fakeCarsPoolAvailable.Count > 0)
                {
                    var obj = m_fakeCarsPoolAvailable[0];
                    m_fakeCarsPoolAvailable.RemoveAt(0);
                    m_fakeCarsPoolInUse.Add(obj);
                    m_fakeCarsCurrentPool.Add(carID, obj);

                    obj.GameObject.GetComponent<VehicleNetworkView>().ResertState();
                    obj.GameObject.GetComponent<VehicleNetworkView>().m_id = carID;
                    var prefab = PrefabCollection<VehicleInfo>.GetPrefab((uint)prefabIndex);
                    obj.GameObject.GetComponent<MeshFilter>().sharedMesh = prefab.m_mesh;
                    obj.GameObject.GetComponent<MeshRenderer>().sharedMaterial = prefab.m_material;
                    obj.GameObject.SetActive(true);
                }
                if (m_fakeCarsCurrentPool.ContainsKey(carID))
                    m_fakeCarsCurrentPool[carID].GameObject.GetComponent<VehicleNetworkView>().OnNetworkData(msg.ReceiveTime, pos, rot2);
            }
        }

        public void RemoveCar(int carID)
        {
            if (m_fakeCarsCurrentPool.ContainsKey(carID))
            {
                var obj2 = m_fakeCarsCurrentPool[carID];
                m_fakeCarsPoolInUse.Remove(obj2);
                m_fakeCarsCurrentPool.Remove(carID);
                m_fakeCarsPoolAvailable.Add(obj2);
                obj2.GameObject.transform.position = new Vector3(0, -1000, 0);
                obj2.GameObject.SetActive(false);
            }
        }
    }
}
