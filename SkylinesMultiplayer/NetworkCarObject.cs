using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SkylinesMultiplayer
{
    public class NetworkCarObject
    {
        public int ID { get; set; }
        public int CarID { get; set; }
        public GameObject GameObject { get; set; }
    }
}
