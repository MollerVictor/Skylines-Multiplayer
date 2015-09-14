using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SkylinesMultiplayer
{
    class CameraHook : MonoBehaviour
    {
        void OnPreCull()
        {
            var camera = gameObject.GetComponent<Camera>();
            camera.rect = new Rect(0, 0, 1, 1);
        }
    }
}