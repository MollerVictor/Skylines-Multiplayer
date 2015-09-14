using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace SkylinesMultiplayer
{
    internal class PlayerCollision : MonoBehaviour
    {
        private const int POOL_SIZE = 15;
        private const int PROP_POOL_SIZE = 100;
        private const int SEARCH_SIZE = 3;

        public Dictionary<int, GameObject> m_fakeCarsPoolInUse = new Dictionary<int, GameObject>(POOL_SIZE);
        public List<GameObject> m_fakeCarsPoolAvailable = new List<GameObject>(POOL_SIZE);


        public Dictionary<int, GameObject> m_fakePropsPoolInUse = new Dictionary<int, GameObject>(POOL_SIZE);
        public List<GameObject> m_fakePropsPoolAvailable = new List<GameObject>(POOL_SIZE);

        private void Start()
        {
            var buildingParent = new GameObject("Bulding Colliders");
            for (int i = 0; i < POOL_SIZE; i++)
            {
                var obj = new GameObject("Building Collider " + i);
                obj.transform.parent = buildingParent.transform;
                obj.AddComponent<MeshFilter>();
                obj.AddComponent<MeshCollider>();
                m_fakeCarsPoolAvailable.Add(obj);
            }

            /*for (int i = 0; i < PROP_POOL_SIZE; i++)
            {
                var obj = new GameObject("Prop Collider " + i);
                obj.AddComponent<MeshFilter>();
                obj.AddComponent<MeshCollider>();
                m_fakePropsPoolAvailable.Add(obj);
            }*/
        }

        private void FixedUpdate()
        {
            UpdateBuildingsCollision();
           // UpdatePropCollision();
        }

        private void UpdateBuildingsCollision()
        {
            var bInstance = Singleton<BuildingManager>.instance;

            int gridX = Mathf.Clamp((int) ((transform.position.x/64f) + 135f), 0, 0x10d);
            int gridZ = Mathf.Clamp((int) ((transform.position.z/64f) + 135f), 0, 0x10d);

            for (int k = -SEARCH_SIZE; k <= SEARCH_SIZE; k++)
            {
                for (int l = -SEARCH_SIZE; l <= SEARCH_SIZE; l++)
                {
                    int index = ((gridZ + k)*270) + gridX + l;
                    int num10 = 0;

                    int buildingID = bInstance.m_buildingGrid[index];

                    while (buildingID != 0)
                    {
                        if (!m_fakeCarsPoolAvailable.Any())
                            return;

                        if (buildingID != 0)
                        {
                            if (!m_fakeCarsPoolInUse.ContainsKey(buildingID))
                            {
                                if (m_fakeCarsPoolAvailable.Any())
                                {
                                    var building = bInstance.m_buildings.m_buffer[buildingID];
                                    
                                    Vector3 position;
                                    Quaternion rotation;
                                    building.CalculateMeshPosition(out position, out rotation);

                                    Vector3 distance = transform.position - position;
                                    distance.y = 0;
                                    if (distance.sqrMagnitude < Math.Pow(32, 2))
                                    {
                                        m_fakeCarsPoolAvailable[0].transform.position = position;
                                        m_fakeCarsPoolAvailable[0].transform.rotation = rotation;
                                        m_fakeCarsPoolAvailable[0].GetComponent<MeshFilter>().sharedMesh =
                                            building.Info.m_mesh;
                                        m_fakeCarsPoolAvailable[0].GetComponent<MeshCollider>().sharedMesh =
                                            building.Info.m_mesh;

                                        m_fakeCarsPoolAvailable[0].SetActive(true);
                                        m_fakeCarsPoolInUse.Add(buildingID, m_fakeCarsPoolAvailable[0]);
                                        m_fakeCarsPoolAvailable.RemoveAt(0);


                                        /* foreach (var prop in building.Info.m_props)
                                         {
                                           
                                             if (!m_fakePropsPoolInUse.ContainsKey(prop.m_index) && m_fakePropsPoolAvailable.Any())
                                             {
                                                 if (prop.m_prop != null)
                                                 {
                                                    
                                                     m_fakePropsPoolAvailable[0].transform.position = position + prop.m_position;
                                                     m_fakePropsPoolAvailable[0].transform.rotation = new Quaternion(prop.m_angle, 0, 0, 0);
                                                   
                                                     m_fakePropsPoolAvailable[0].GetComponent<MeshFilter>().sharedMesh =
                                                         prop.m_prop.m_lodMesh;
                                                     m_fakePropsPoolAvailable[0].GetComponent<MeshCollider>().sharedMesh
                                                         =
                                                         prop.m_prop.m_lodMesh;
                                                    
                                                     m_fakePropsPoolAvailable[0].SetActive(true);
                                                     m_fakePropsPoolInUse.Add(prop.m_index, m_fakePropsPoolAvailable[0]);
                                                     m_fakePropsPoolAvailable.RemoveAt(0);

                                                     m_fakePropsPoolAvailable[0].name += " " + prop.m_fixedHeight;
                                                 }
                                             }
                                         }*/
                                    }
                                }
                            }
                        }

                        buildingID = bInstance.m_buildings.m_buffer[buildingID].m_nextGridBuilding;

                        if (++num10 >= 0x8000)
                        {
                            Debug.LogError("Invalid list detected!\n" + System.Environment.StackTrace);
                            break;
                        }
                    }
                }
            }

            var toRemove = new List<KeyValuePair<int, GameObject>>();
            foreach (var building in m_fakeCarsPoolInUse)
            {
                Vector3 distance = transform.position - building.Value.transform.position;
                distance.y = 0;
                if (distance.sqrMagnitude > Math.Pow(42, 2))
                {
                    toRemove.Add(building);
                }
            }

            foreach (var building in toRemove)
            {
                m_fakeCarsPoolInUse.Remove(building.Key);
                m_fakeCarsPoolAvailable.Add(building.Value);
                building.Value.SetActive(false);
            }
        }

        private void UpdatePropCollision()
        {
            var pInstance = Singleton<PropManager>.instance;

            /*int num = Mathf.Max((int)(((transform.position.x - 800f) / 64f) + 135f), 0);
            int num2 = Mathf.Max((int)(((transform.position.z - 800f) / 64f) + 135f), 0);
            int num3 = Mathf.Min((int)(((transform.position.x + 800f) / 64f) + 135f), 0x10d);
            int num4 = Mathf.Min((int)(((transform.position.z + 800f) / 64f) + 135f), 0x10d);*/

            int gridX = (int)Mathf.Clamp(((transform.position.x + 0x8000) * 270) / 0x10000, 0, 0x10d);
            int gridZ = (int)Mathf.Clamp(((transform.position.z + 0x8000) * 270) / 0x10000, 0, 0x10d);

            //int grid X = Mathf.Clamp((int) ((transform.position.x/64f)), 0, 0x10d);
            //int gridZ = Mathf.Clamp((int) ((transform.position.z/64f)), 0, 0x10d);

            //int gridX = Mathf.Clamp((int) ((transform.position.x/64f) + 135f), 0, 0x10d);
            //int gridZ = Mathf.Clamp((int) ((transform.position.z/64f) + 135f), 0, 0x10d);

            for (int k = -20; k <= 20; k++)
            {
                for (int l = -20; l <= 20; l++)
                {
                    int index = ((gridZ + k)*270) + gridX + l;

                    int num10 = 0;
                    if(index < 0)  continue;
                    
                    int propID = pInstance.m_propGrid[index];
                    
                    while (propID != 0)
                    {
                        Debug.Log("2");
                        if (!m_fakePropsPoolAvailable.Any())
                            return;

                        if (propID != 0)
                        {
                            Debug.Log("3");
                            if (!m_fakePropsPoolInUse.ContainsKey(propID))
                            {
                                if (m_fakePropsPoolAvailable.Any())
                                {
                                    var prop = pInstance.m_props.m_buffer[propID];

                                    Vector3 position = prop.Position;
                                    Quaternion rotation = new Quaternion(prop.Angle, 0, 0, 0);


                                    Vector3 distance = transform.position - position;
                                    distance.y = 0;
                                    if (distance.sqrMagnitude < Math.Pow(32, 2))
                                    {
                                        m_fakePropsPoolAvailable[0].transform.position = position;
                                        m_fakePropsPoolAvailable[0].transform.rotation = rotation;
                                        m_fakePropsPoolAvailable[0].GetComponent<MeshFilter>().sharedMesh =
                                            prop.Info.m_mesh;
                                        m_fakePropsPoolAvailable[0].GetComponent<MeshCollider>().sharedMesh =
                                            prop.Info.m_mesh;

                                        m_fakePropsPoolAvailable[0].SetActive(true);
                                        m_fakePropsPoolInUse.Add(propID, m_fakePropsPoolAvailable[0]);
                                        m_fakePropsPoolAvailable.RemoveAt(0);
                                    }
                                }
                            }
                        }

                        propID = pInstance.m_props.m_buffer[propID].m_nextGridProp;

                        if (++num10 >= 0x8000)
                        {
                            Debug.LogError("Invalid list detected!\n" + System.Environment.StackTrace);
                            break;
                        }
                    }
                }
            }

            /*var toRemove = new List<KeyValuePair<int, GameObject>>();
            foreach (var prop in m_fakePropsPoolInUse)
            {
                Vector3 distance = transform.position - prop.Value.transform.position;
                distance.y = 0;
                if (distance.sqrMagnitude > Math.Pow(42, 2))
                {
                    toRemove.Add(prop);
                }
            }

            foreach (var prop in toRemove)
            {
                m_fakePropsPoolInUse.Remove(prop.Key);
                m_fakePropsPoolAvailable.Add(prop.Value);
                prop.Value.SetActive(false);
            }*/
        }
    }
}
