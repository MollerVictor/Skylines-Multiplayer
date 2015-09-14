using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using UnityEngine;

namespace SkylinesMultiplayer
{
    public class NetworkPlayer
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public NetConnection OwnerConnection { get; set; }
        public GameObject PlayerGameObject { get; set; }
    }
}
