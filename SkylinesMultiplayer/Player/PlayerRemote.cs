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
            m_hookLineRender.material = new Material(Shader.Find(" Diffuse"));
            m_hookLineRender.SetWidth(0.5f, 0.5f);
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
