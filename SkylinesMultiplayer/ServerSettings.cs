using System;

namespace SkylinesMultiplayer
{
    public class ServerSettings
    {
        public static string Name {get; set;}
        public static int Port {get; set;}
        public static bool Private { get; set; }
        public static bool IsHosting { get; set; }
    }

    public class ConnectSettings
    {
        public static string Ip { get; set; }
        public static int Port { get; set; }
    }
}
