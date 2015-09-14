using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.Steamworks;
using UnityEngine;

namespace SkylinesMultiplayer
{
    class Scoreboard : MonoBehaviour
    {
        void OnGUI()
        {
            if (Input.GetKey(KeyCode.Tab))
            {
                int boxHeight = MultiplayerManager.instance.m_players.Count * 28 + 10;

                GUI.Box(new Rect(45, 45, 105, boxHeight), "");

                int y = 0;
                foreach (NetworkPlayer networkPlayer in MultiplayerManager.instance.m_players)
                {
                    GUI.Label(new Rect(50, 50 + y, 100, 25),  "Player: " + networkPlayer.Name);
                    y += 28;
                }
            }
        }
    }
}
