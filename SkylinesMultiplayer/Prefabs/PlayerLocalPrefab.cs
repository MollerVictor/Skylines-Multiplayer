using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace SkylinesMultiplayer.Prefabs
{
    class PlayerLocalPrefab
    {
        public static GameObject Create(int id)
        {
            GameObject gameObject = new GameObject("Player Local");
            gameObject.layer = 2;   //Ignore raycast
            var playerComp = gameObject.gameObject.AddComponent<Player>();
            playerComp.m_playerId = id;
            playerComp.m_isMine = true;
            gameObject.tag = "Player";

            Camera.main.GetComponent<CameraController>().enabled = false;
            Camera.main.GetComponent<DepthOfField>().enabled = false;
            Camera.main.transform.parent = gameObject.transform;
            Camera.main.transform.localPosition = new Vector3(0, 1.5f, 0);
            Camera.main.transform.rotation = Quaternion.identity;
            Camera.main.nearClipPlane = 0.75f;  //If I use lower it acts weird with the street lights at night
            Camera.main.fieldOfView = 90;

            Camera.main.gameObject.AddComponent<FPSCameraController>().axes = FPSCameraController.RotationAxes.MouseY;
            gameObject.AddComponent<FPSCameraController>().axes = FPSCameraController.RotationAxes.MouseX;
            gameObject.AddComponent<PlayerController>();
            gameObject.AddComponent<PlayerCollision>();

            var collObject = new GameObject("BoxCollider");
            collObject.layer = 2;   //Ignore raycast
            collObject.transform.parent = gameObject.transform;
            var collider = collObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.2f,2,1.2f);
            collObject.AddComponent<DontRotate>();
            
            var rigidBody = gameObject.AddComponent<Rigidbody>();
            rigidBody.freezeRotation = true;
            rigidBody.useGravity = false;

            return gameObject;
        }
    }
}
