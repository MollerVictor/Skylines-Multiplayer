using ColossalFramework;
using UnityEngine;

namespace SkylinesMultiplayer
{
    class HideUI : MonoBehaviour
    {

        public bool MouseLocked { get { return m_mouseLocked; } set { m_mouseLocked = value; } }

        private bool uiHidden = false;

        private bool m_lastForcedPausedStatus;

        private bool m_mouseLocked = false;

        void Start()
        {
            Hide();
        }

        void OnDestroy()
        {
            if (uiHidden)
            {
                Show();
            }
        }

        void Hide()
        {
            var cameraController = GameObject.FindObjectOfType<CameraController>();
            var camera = cameraController.gameObject.GetComponent<Camera>();
            camera.gameObject.AddComponent<CameraHook>();
            camera.GetComponent<OverlayEffect>().enabled = false;

            var cameras = GameObject.FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                if (cam.name == "UIView")
                {
                    cam.enabled = false;
                    break;
                }
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Show()
        {
            var cameraController = GameObject.FindObjectOfType<CameraController>();
            var camera = cameraController.gameObject.GetComponent<Camera>();
            Destroy(cameraController.GetComponent<CameraHook>());
            camera.GetComponent<OverlayEffect>().enabled = true;

            var cameras = GameObject.FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                if (cam.name == "UIView")
                {
                    cam.enabled = true;
                    break;
                }
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            bool forcedPause = Singleton<SimulationManager>.instance.ForcedSimulationPaused;
           
            if (m_mouseLocked && !forcedPause)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (forcedPause != m_lastForcedPausedStatus)
            {
                if (forcedPause)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
            }

            m_lastForcedPausedStatus = forcedPause;
        }

    }
}