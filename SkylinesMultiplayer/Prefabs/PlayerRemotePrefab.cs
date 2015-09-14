using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SkylinesMultiplayer.Prefabs
{
    class PlayerRemotePrefab
    {
        private const string CITIZEN_PREFAB_NAME = "Bluecollar Male";

        public static GameObject Create(int id)
        {
            GameObject citizenPrefabModel = GetPlayerModelPrefab();

            GameObject gameObject = Object.Instantiate(citizenPrefabModel, new Vector3(500, 150, 0), Quaternion.identity) as GameObject;

            gameObject.tag = "Player";
            gameObject.name = "Player Remote (" + id + ")";
            gameObject.AddComponent<PlayerNetworkView>();
            gameObject.AddComponent<PlayerRemote>();
            var playerComp = gameObject.AddComponent<Player>();
            playerComp.m_playerId = id;
            var boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(1, 2, 1);

            Object.Destroy(gameObject.GetComponent<ResidentAI>());
            Object.Destroy(gameObject.GetComponent<CitizenInfo>());

            var anim = gameObject.GetComponent<Animator>();
            anim.SetInteger("State", 0);

            if (MultiplayerManager.instance.m_isServer)
            {
                gameObject.AddComponent<PlayerServer>();
            }

            return gameObject;
        }

        private static GameObject GetPlayerModelPrefab()
        {
            GameObject citizenPrefabModel = null;
            for (int i = 0; i < PrefabCollection<CitizenInfo>.LoadedCount(); i++)
            {
                if (PrefabCollection<CitizenInfo>.GetLoaded((uint)i).name.ToLower().Contains(CITIZEN_PREFAB_NAME.ToLower()))
                {
                    citizenPrefabModel = PrefabCollection<CitizenInfo>.GetLoaded((uint)i).gameObject;
                    break;
                }
            }

            if (citizenPrefabModel == null)
                Debug.LogError("Coulden't load player model");

            return citizenPrefabModel;
        }
    }
}
