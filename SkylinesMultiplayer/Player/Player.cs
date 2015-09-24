using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Lidgren.Network;
using UnityEngine;

namespace SkylinesMultiplayer
{
    public class Player : MonoBehaviour
    {
        private const int SEND_RATE = 30;   //How often the player send his position

        public bool m_isMine;

        public int m_playerId;

        private int m_hp = 100;

        public int Hp { get { return m_hp; } set { m_hp = value; } }

        public void OnDamageTaken(int shooterID, int damage)
        {
            Hp -= damage;
        }

        void Start()
        {
            if (m_isMine)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                InvokeRepeating("SendPosition", 0, 1 / (float)SEND_RATE);
            }
        }

        void OnGUI()
        {
            if (m_isMine)
            {
                GUI.Box(new Rect(Screen.width/2 - 5, Screen.height/2 - 5, 10, 10), "");
            }
        }

        private void SendPosition()
        {
            var mInstance = MultiplayerManager.instance;

            var message = new MessageUpdatePlayerPosition();
            message.playerId = m_playerId;
            message.position = transform.position;
            message.rotation = transform.rotation.eulerAngles;

            mInstance.SendNetworkMessage(message, SendTo.Others, null, NetDeliveryMethod.Unreliable);
        }
    }
}
