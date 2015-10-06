using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.UI;
using UnityEngine;

namespace SkylinesMultiplayer
{
    public class Chat : UIPanel
    {
        private const int MaxChatMessageLimit = 5;
        private const int MaxChatMessageLength = 255;

        private Queue<string> m_messageLog = new Queue<string>(MaxChatMessageLimit);

        private bool m_usingChat;
        
        private UILabel m_chatLabel;
        private UITextField m_chatInputTextField;

        public override void Start()
        {
            UIView v = UIView.GetAView();
            
            m_chatLabel = this.AddUIComponent<UILabel>();
            m_chatLabel.text = "";
            m_chatLabel.anchor = UIAnchorStyle.Bottom | UIAnchorStyle.Left;
            m_chatLabel.pivot = UIPivotPoint.TopLeft;
            m_chatLabel.autoHeight = false;
            m_chatLabel.autoSize = false;
            m_chatLabel.textAlignment = UIHorizontalAlignment.Left;
            m_chatLabel.verticalAlignment = UIVerticalAlignment.Bottom;
            m_chatLabel.wordWrap = true;
            m_chatLabel.height = 250;
            m_chatLabel.width = 400;
            m_chatLabel.absolutePosition = new Vector3(50, v.fixedHeight - 320);

            m_chatInputTextField = CreateTextField();
            m_chatInputTextField.pivot = UIPivotPoint.TopLeft;
            m_chatInputTextField.absolutePosition = new Vector3(50, v.fixedHeight - 50);
            m_chatInputTextField.width = 250;
            m_chatInputTextField.maxLength = MaxChatMessageLength;
        }


        public override void Update()
        {
            if (PlayerInput.ChatKey.GetKeyDown())
            {
                m_usingChat = !m_usingChat;
                if (m_usingChat)
                {
                    m_chatInputTextField.Focus();
                    PlayerInput.DisableInput = true;
                }
                else
                {
                    SendChatMessage(m_chatInputTextField.text);
                    m_chatInputTextField.Cancel();  //Clears the textfield and remove focus from it
                    PlayerInput.DisableInput = false;
                }
            }

            if (m_usingChat)
            {
                m_chatInputTextField.Focus();
            }
        }

        void SendChatMessage(string msg)
        {
            //No point of sending if you have nothing to send
            if (msg.Length == 0)
                return;
            if (msg.Length > MaxChatMessageLength)
                msg.Remove(MaxChatMessageLength);
            string textToSend = MultiplayerManager.instance.MyNetworkPlayer.Name + ": " + msg;
            MessageSendChatMessage messageSendChat = new MessageSendChatMessage();
            messageSendChat.chatMessage = textToSend;
            MultiplayerManager.instance.SendNetworkMessage(messageSendChat, SendTo.All);
        }

        public void OnReceiveChatMessage(string msg)
        {
            if (m_messageLog.Count >= MaxChatMessageLimit)
                m_messageLog.Dequeue();
            m_messageLog.Enqueue(msg);
            RefreshChatText();
        }

        void RefreshChatText()
        {
            StringBuilder sb = new StringBuilder();
            foreach (string msg in m_messageLog)
            {
                sb.Append(msg + "\n");
            }
            m_chatLabel.text = sb.ToString();
        }

        private UITextField CreateTextField()
        {
            UITextField textField = this.AddUIComponent<UITextField>();

            textField.maxLength = 100;
            textField.enabled = true;
            textField.builtinKeyNavigation = true;
            textField.isInteractive = true;
            textField.readOnly = false;
            textField.submitOnFocusLost = true;

            textField.horizontalAlignment = UIHorizontalAlignment.Left;
            textField.selectionSprite = "EmptySprite";
            textField.selectionBackgroundColor = new Color32(0, 171, 234, 255);
            textField.normalBgSprite = "TextFieldPanel";
            textField.textColor = new Color32(174, 197, 211, 255);
            textField.disabledTextColor = new Color32(254, 254, 254, 255);
            textField.textScale = 1.3f;
            textField.opacity = 1;
            textField.color = new Color32(58, 88, 104, 255);
            textField.disabledColor = new Color32(254, 254, 254, 255);

            return textField;
        }
    }
}
