using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SkylinesMultiplayer
{
    public class PlayerServer : MonoBehaviour
    {
        private int m_hp = 100;

        public int Hp { get { return m_hp; } set { m_hp = value; } }

        void OnTakeDamage(int shooterID, int damage)
        {
            Hp -= damage;
        }
    }
}
