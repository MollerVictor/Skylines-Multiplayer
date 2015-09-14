using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace SkylinesMultiplayer
{
    class MultiplayerMenuManager : MonoBehaviour
    {
        public Mod UserMod { get; set; }

        private bool m_modIsEnabled;

        private NeedToRestartGamePanel m_needToRestartGamePanel;

        void Start()
        {
            m_modIsEnabled = GetModEnabledState();

            UIView v = UIView.GetAView();
            m_needToRestartGamePanel = v.AddUIComponent(typeof(NeedToRestartGamePanel)) as NeedToRestartGamePanel;

            InvokeRepeating("CheckIfModGotActivated", 0, 2);
        }


        void CheckIfModGotActivated()
        {
            bool newModIsEnabled = GetModEnabledState();
            if (m_modIsEnabled != newModIsEnabled)
            {
                if (newModIsEnabled)
                    UserMod.LoadMod();
                else
                    ShowNeedToRestartGamePanel();
            }

            m_modIsEnabled = newModIsEnabled;
        }

        bool GetModEnabledState()
        {
            var pluginManager = PluginManager.instance;
            var plugins = ReflectionHelper.GetPrivate<Dictionary<string, PluginManager.PluginInfo>>(pluginManager, "m_Plugins");

            foreach (var item in plugins)
            {
                if (item.Value.name == Settings.WorkshopID)
                {
                    return item.Value.isEnabled;
                }
            }
            return false;
        }

        void ShowNeedToRestartGamePanel()
        {
            m_needToRestartGamePanel.BringToFront();
            m_needToRestartGamePanel.Focus();
            m_needToRestartGamePanel.Show();
            UIView.PushModal(m_needToRestartGamePanel);
        }
    }
}
