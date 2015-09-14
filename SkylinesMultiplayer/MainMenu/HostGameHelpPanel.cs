using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.Plugins;
using ColossalFramework.Steamworks;
using ColossalFramework.UI;
using Lidgren.Network;
using UnityEditor;
using UnityEngine;

namespace SkylinesMultiplayer
{
    class HostGameHelpPanel : UIPanel
    {
        private const string HELP_STRING = "To host a server, you either need to download a save from the workshop,\nor upload your own save to workshop and then download it.\n\nTo share your own save go to content manager then \"SaveGames\"\nthen press \"Share\" on the save you want to host, then subscribe to your own save.";


        public override void Start()
        {
            this.backgroundSprite = "MenuPanel";
            this.color = new Color32(50, 50, 50, 255);
            this.width = 600;
            this.height = 200;
            this.isVisible = false;
            this.position += new Vector3(-this.width / 2, this.height / 2 + 50);


            UILabel ipLabel = this.AddUIComponent<UILabel>();
            ipLabel.text = HELP_STRING;
            ipLabel.pivot = UIPivotPoint.BottomRight;
            ipLabel.relativePosition = new Vector2(10, 20);

            UIButton closeButton = CreateButton();
            closeButton.text = "OKEY";
            closeButton.width = 100;
            closeButton.height = 60;
            closeButton.pivot = UIPivotPoint.BottomRight;
            closeButton.relativePosition = new Vector2(this.width - closeButton.width - 5, this.height - closeButton.height - 5);
            closeButton.eventClick += OnClickCancel;
        }


        private void OnClickCancel(UIComponent component, UIMouseEventParameter eventparam)
        {
            this.Hide();
            UIView.PopModal();
        }

        private UIButton CreateButton()
        {
            UIButton button = this.AddUIComponent<UIButton>();

            button.textPadding = new RectOffset(5, 5, 5, 5);
            button.normalBgSprite = "ButtonMenu";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.textColor = new Color32(255, 255, 255, 255);
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            button.focusedTextColor = new Color32(255, 255, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);

            return button;
        }
    }
}
