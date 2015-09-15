using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SkylinesMultiplayer
{
    class PlayerRemote : MonoBehaviour
    {
        private LineRenderer m_hookLineRender;

        void Start()
        {
            var obj = new GameObject("Hook Linerender");
            m_hookLineRender = obj.AddComponent<LineRenderer>();
            m_hookLineRender.material = new Material(Shader.Find("Sprites/Default"));
            m_hookLineRender.SetWidth(1, 0.4f);
            Color startColor = new Color(0.55f, 0, 0);
            Color endColor = new Color(0.15f, 0, 0);
            m_hookLineRender.SetColors(startColor, endColor);
            m_hookLineRender.enabled = false;
        }

        void Update()
        {
            m_hookLineRender.SetPosition(0, transform.position + new Vector3(0, 1, 0));
        }

        public void OnPlayerHookUsed(Vector3 hookPosition)
        {
            m_hookLineRender.SetPosition(0, transform.position);
            m_hookLineRender.SetPosition(1, hookPosition);
            m_hookLineRender.enabled = true;
        }

        public void OnPlayerHookReleased()
        {
            m_hookLineRender.enabled = false;
        }
    }
}
