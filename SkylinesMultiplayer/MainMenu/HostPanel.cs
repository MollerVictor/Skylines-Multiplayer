using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using UnityEngine;

namespace SkylinesMultiplayer
{

    public class HostPanel : LoadSavePanelBase<SaveGameMetaData>
    {
        private UISprite m_AchNope;
        private UILabel m_CityName;
        public string m_forceEnvironment;
        private static string m_LastSaveName;
        private UIButton m_LoadButton;
        private UIListBox m_SaveList;
        private UITextureSprite m_SnapShotSprite;
        private UITextField m_serverNameTextbox;
        private UITextField m_serverPortTextbox;
        private UICheckBox m_serverPrivateCheckbox;

        private HostGameHelpPanel m_hostGameHelpPanel;

        protected void Awake()
        {
            this.m_SaveList = transform.Find("SaveList").GetComponent<UIListBox>();

            var asd = new ColossalFramework.UI.PropertyChangedEventHandler<int>(this.OnListingSelectionChanged);

            m_SaveList.ClearEventInvocations("eventSelectedIndexChanged");
            //m_LoadButton.ClearEventInvocations("eventButtonStateChanged");

            this.m_SaveList.eventSelectedIndexChanged += new ColossalFramework.UI.PropertyChangedEventHandler<int>(this.OnListingSelectionChanged);
            this.m_LoadButton = transform.Find("Load").GetComponent<UIButton>();
            this.m_CityName = transform.Find("CityInfo").Find("CityName").GetComponent<UILabel>();
            this.m_SnapShotSprite = transform.Find("SnapShot").GetComponent<UITextureSprite>();
            //this.m_AchNope = transform.Find("Ach").Find("AchNope").GetComponent<UISprite>();
    
            var hostInfoPanel = transform.Find("CityInfo").GetComponent<UIPanel>();

            m_serverNameTextbox = hostInfoPanel.AddUIComponent<UITextField>();
            var serverNameLabel = hostInfoPanel.AddUIComponent<UILabel>();
            SetLabel(serverNameLabel, "Server Name: ", 0, 115);
            SetTextBox(m_serverNameTextbox, "Unnamed Server", 85, 115, 300, 20);

            m_serverPortTextbox = hostInfoPanel.AddUIComponent<UITextField>();
            var serverPortLabel = hostInfoPanel.AddUIComponent<UILabel>();
            SetLabel(serverPortLabel, "Port: ", 0, 140);
            SetTextBox(m_serverPortTextbox, "27015", 85, 140, 100, 20);

            m_serverPrivateCheckbox = hostInfoPanel.AddUIComponent<UICheckBox>();
            var serverPrivateLabel = hostInfoPanel.AddUIComponent<UILabel>();
            SetLabel(serverPrivateLabel, "Private Server: ", 0, 165);
            SetCheckBox(m_serverPrivateCheckbox, 35, 166);


            transform.Find("CityInfo").Find("MapThemeLabel").gameObject.SetActive(false);
            transform.Find("CityInfo").Find("MapTheme").gameObject.SetActive(false);
            transform.Find("CityInfo").Find("PopulationLabel").gameObject.SetActive(false);
            transform.Find("CityInfo").Find("Population").gameObject.SetActive(false);
            transform.Find("CityInfo").Find("MoneyLabel").gameObject.SetActive(false);
            transform.Find("CityInfo").Find("Money").gameObject.SetActive(false);
            transform.Find("AchGroup").gameObject.SetActive(false);

            this.ClearInfo();

            UIView v = UIView.GetAView();
            m_hostGameHelpPanel = v.AddUIComponent(typeof(HostGameHelpPanel)) as HostGameHelpPanel;
        }

        void Update()
        {
            //Make sure so you only have 0-9 in the port input
            Regex rgx = new Regex("[^0-9]");
            m_serverPortTextbox.text = rgx.Replace(m_serverPortTextbox.text, "");
        }

        private void SetTextBox(UITextField textField, string p, int x, int y, int width, int height)
        {
            textField.relativePosition = new Vector3(x, y - 4);  //Adjust y to fit with labels
            textField.text = p;
            textField.textScale = 0.8f;
            textField.cursorBlinkTime = 0.45f;
            textField.cursorWidth = 1;
            textField.selectionBackgroundColor = new Color(233, 201, 148, 255);
            textField.selectionSprite = "EmptySprite";
            textField.padding = new RectOffset(5, 0, 5, 0);
            textField.foregroundSpriteMode = UIForegroundSpriteMode.Fill;
            textField.normalBgSprite = "TextFieldPanel";
            textField.hoveredBgSprite = "TextFieldPanelHovered";
            textField.focusedBgSprite = "TextFieldPanel";
            textField.size = new Vector3(width, height);
            textField.isInteractive = true;
            textField.enabled = true;
            textField.readOnly = false;
            textField.builtinKeyNavigation = true;
            textField.horizontalAlignment = UIHorizontalAlignment.Left;
            textField.verticalAlignment = UIVerticalAlignment.Middle;
            textField.color = new Color32(100, 100, 100, 255);
        }

        private void SetLabel(UILabel label, string p, int x, int y)
        {
            label.relativePosition = new Vector3(x, y);
            label.text = p;
            label.textScale = 0.8f;
            label.verticalAlignment = UIVerticalAlignment.Middle;
            label.size = new Vector3(120, 20);
        }

        private void SetCheckBox(UICheckBox checkBox, int x, int y)
        {
            checkBox.relativePosition = new Vector3(x, y + 4);
            checkBox.size = new Vector3(195, 21);

            var sprite = checkBox.AddUIComponent<UISprite>();
            sprite.spriteName = "check-checked";

            var sprite2 = checkBox.AddUIComponent<UISprite>();
            sprite2.spriteName = "check-unchecked";

            checkBox.checkedBoxObject = sprite;
        }

        private void ClearInfo()
        {
            if (this.m_SnapShotSprite.texture != null)
            {
                UnityEngine.Object.Destroy(this.m_SnapShotSprite.texture);
            }
            this.m_CityName.text = "-";
        }

        private void OnEnterFocus(UIComponent comp, UIFocusEventParameter p)
        {
            if (p.gotFocus == base.component)
            {
                this.m_SaveList.Focus();
            }
        }

        private void OnItemDoubleClick(UIComponent comp, int sel)
        {
            if ((comp == this.m_SaveList) && (sel != -1))
            {
                if (this.m_SaveList.selectedIndex != sel)
                {
                    this.m_SaveList.selectedIndex = sel;
                }
                this.OnLoad();
            }
        }

        public void OnKeyDown(UIComponent comp, UIKeyEventParameter p)
        {
            if (!p.used)
            {
                if (p.keycode == KeyCode.Escape)
                {
                    this.OnClosed();
                    p.Use();
                }
                else if (p.keycode == KeyCode.Return)
                {
                    p.Use();
                    this.OnLoad();
                }
            }
        }

        private void OnVisibilityChanged(UIComponent comp, bool visible)
        {
            if (visible)
            {
                Refresh();
                base.component.CenterToParent();
                base.component.Focus();
            }
            else
            {
                MainMenu.SetFocus();
            }
        }

        private void OnListingSelectionChanged(UIComponent comp, int sel)
        {
            if (sel > -1)
            {
                SaveGameMetaData listingMetaData = base.GetListingMetaData(sel);
               
                string cityName = listingMetaData.cityName;
                this.m_CityName.text = cityName ?? "Unknown";
                
                if (this.m_SnapShotSprite.texture != null)
                {
                    UnityEngine.Object.Destroy(this.m_SnapShotSprite.texture);
                }
                
                if (listingMetaData.imageRef != null)
                {
                    this.m_SnapShotSprite.texture = listingMetaData.imageRef.Instantiate<Texture>();
                    if (this.m_SnapShotSprite.texture != null)
                    {
                        this.m_SnapShotSprite.texture.wrapMode = TextureWrapMode.Clamp;
                    }
                }
                else
                {
                    this.m_SnapShotSprite.texture = null;
                }
                
                Package.Asset listingData = base.GetListingData(sel);
                SaveGameMetaData data2 = base.GetListingMetaData(sel);
                //this.m_AchNope.isVisible = ((Singleton<PluginManager>.instance.enabledModCount > 0) || ((listingData.package != null) && (listingData.package.GetPublishedFileID() != PublishedFileId.invalid))) || data2.achievementsDisabled;
            }
            else
            {
                this.ClearInfo();
            }
        }

        public void OnLoad()
        {
            if ((!SavePanel.isSaving && Singleton<LoadingManager>.exists) && !Singleton<LoadingManager>.instance.m_currentlyLoading)
            {
                ServerSettings.Name = m_serverNameTextbox.text;
                ServerSettings.Port = int.Parse(m_serverPortTextbox.text);
                ServerSettings.Private = m_serverPrivateCheckbox.isChecked;
                ServerSettings.IsHosting = true;

                

                base.CloseEverything();
                m_LastSaveName = base.GetListingName(this.m_SaveList.selectedIndex);
                SavePanel.lastLoadedName = m_LastSaveName;
                SaveGameMetaData listingMetaData = base.GetListingMetaData(this.m_SaveList.selectedIndex);
                base.PrintModsInfo(listingMetaData);
                Package.Asset listingData = base.GetListingData(this.m_SaveList.selectedIndex);
                SavePanel.lastCloudSetting = (listingData.package != null) && PackageManager.IsSteamCloudPath(listingData.package.packagePath);
                SimulationMetaData ngs = new SimulationMetaData
                {
                    m_CityName = listingMetaData.cityName,
                    m_updateMode = SimulationManager.UpdateMode.LoadGame,
                    m_environment = this.m_forceEnvironment
                };
                if ((Singleton<PluginManager>.instance.enabledModCount > 0) || ((listingData.package != null) && (listingData.package.GetPublishedFileID() != PublishedFileId.invalid)))
                {
                    ngs.m_disableAchievements = SimulationMetaData.MetaBool.True;
                }
                Singleton<LoadingManager>.instance.LoadLevel(listingData, "Game", "InGame", ngs);
                UIView.library.Hide(base.GetType().Name, 1);

                Debug.Log("menuid " + listingData.package.GetPublishedFileID());
            }
        }
    

        protected void Refresh()
        {
            ClearListing();
            Package.AssetType[] assetTypes = new Package.AssetType[] { UserAssetType.SaveGameMetaData };
            IEnumerator<Package.Asset> enumerator = PackageManager.FilterAssets(assetTypes).GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    Package.Asset current = enumerator.Current;
                    if ((current != null) && current.isEnabled && current.package.GetPublishedFileID() != PublishedFileId.invalid)
                    {
                        SaveGameMetaData mmd = current.Instantiate<SaveGameMetaData>();
                        AddToListing(current.name + " - " + mmd.timeStamp, current, mmd);
                    }
                }
            }
            finally
            {
                if (enumerator == null)
                {
                }
                enumerator.Dispose();
            }
            foreach (Package.Asset asset2 in SaveHelper.GetSavesOnDisk())
            {
                SaveGameMetaData data2 = new SaveGameMetaData
                {
                    assetRef = asset2
                };
                AddToListing("<color red>" + asset2.name + "</color>", asset2, data2);
            }
            this.m_SaveList.items = base.GetListingItems();
            if (this.m_SaveList.items.Length > 0)
            {
                int num = FindIndexOf(m_LastSaveName);
                m_SaveList.selectedIndex = (num == -1) ? 0 : num;
                this.m_LoadButton.isEnabled = true;
            }
            else
            {
                this.m_LoadButton.isEnabled = false;
                Invoke("ShowNeedToRestartGamePanel", 0.1f);
            }
        }

        void ShowNeedToRestartGamePanel()
        {
            m_hostGameHelpPanel.Show();
            m_hostGameHelpPanel.BringToFront();
            m_hostGameHelpPanel.Focus();

            UIView.PushModal(m_hostGameHelpPanel);
        }
    }
}
