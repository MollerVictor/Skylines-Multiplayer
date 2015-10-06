using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace SkylinesMultiplayer
{
    class HideUI : MonoBehaviour
    {

        public bool MouseLocked { get { return m_mouseLocked; } set { m_mouseLocked = value; } }

        private bool uiHidden = false;

        private bool m_lastForcedPausedStatus;

        private bool m_mouseLocked = false;

        private List<string> m_dontRemoveWhiteList = new List<string>(new string[] { "(Library) ExitConfirmPanel", "(Library) OptionsPanel", "(Library) PauseMenu", "Chat" }); 
        

        void Start()
        {
            Hide();

            var cameras = GameObject.FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                if (cam.name == "UIView")
                {
                    List<MonoBehaviour> allChildren = new List<MonoBehaviour>();
                    allChildren.AddRange(cam.GetComponentsInChildren<UIPanel>());
                    allChildren.AddRange(cam.GetComponentsInChildren<UISlicedSprite>());
                    allChildren.AddRange(cam.GetComponentsInChildren<UISprite>());
                    allChildren.AddRange(cam.GetComponentsInChildren<UIButton>());

                    foreach (var child in allChildren)
                    {
                        Transform nextHighestParent = child.transform;

                        while (nextHighestParent.parent != null && nextHighestParent.parent.parent != null)
                        {
                            nextHighestParent = nextHighestParent.parent;
                        }

                        if (m_dontRemoveWhiteList.Contains(nextHighestParent.name))
                            continue;

                        child.enabled = false;
                    }

                    Transform menu = cam.transform.Find("(Library) PauseMenu").Find("Menu");
                    menu.Find("SaveGame").GetComponent<UIButton>().isEnabled = false;
                    menu.Find("LoadGame").GetComponent<UIButton>().isEnabled = false;
                    menu.Find("Statistics").GetComponent<UIButton>().isEnabled = false;
                    break;
                }
            }
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

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Show()
        {
            var cameraController = GameObject.FindObjectOfType<CameraController>();
            var camera = cameraController.gameObject.GetComponent<Camera>();
            Destroy(cameraController.GetComponent<CameraHook>());

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