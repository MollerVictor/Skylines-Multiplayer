using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SkylinesMultiplayer
{
    struct InputKey
    {
        public KeyCode keyCode;
        public bool ignoreDisableInput;

        public bool GetKey()
        {
            if (PlayerInput.DisableInput && ignoreDisableInput)
            {
                return Input.GetKey(keyCode);
            }
            if (!PlayerInput.DisableInput)
            {
                return Input.GetKey(keyCode);
            }
            return false;
        }

        public bool GetKeyDown()
        {
            if (PlayerInput.DisableInput && ignoreDisableInput)
            {
                return Input.GetKeyDown(keyCode);
            }
            if (!PlayerInput.DisableInput)
            {
                return Input.GetKeyDown(keyCode);
            }
            return false;
        }

        public bool GetKeyUp()
        {
            if (PlayerInput.DisableInput && ignoreDisableInput)
            {
                return Input.GetKeyUp(keyCode);
            }
            if (!PlayerInput.DisableInput)
            {
                return Input.GetKeyUp(keyCode);
            }
            return false;
        }
    }

    struct InputMouse
    {
        public int mouseButton;
        public bool ignoreDisableInput;

        public bool GetKey()
        {
            if (PlayerInput.DisableInput && ignoreDisableInput)
            {
                return Input.GetMouseButton(mouseButton);
            }
            if (!PlayerInput.DisableInput)
            {
                return Input.GetMouseButton(mouseButton);
            }
            return false;
        }

        public bool GetKeyDown()
        {
            if (PlayerInput.DisableInput && ignoreDisableInput)
            {
                return Input.GetMouseButtonDown(mouseButton);
            }
            if (!PlayerInput.DisableInput)
            {
                return Input.GetMouseButtonDown(mouseButton);
            }
            return false;
        }

        public bool GetKeyUp()
        {
            if (PlayerInput.DisableInput && ignoreDisableInput)
            {
                return Input.GetMouseButtonUp(mouseButton);
            }
            if (!PlayerInput.DisableInput)
            {
                return Input.GetMouseButtonUp(mouseButton);
            }
            return false;
        }
    }

    class PlayerInput
    {
        public static bool DisableInput { get { return m_disableInput; } set { m_disableInput = value; } }

        private static bool m_disableInput;

        public static InputMouse FireKey = new InputMouse { mouseButton = 0};
        public static InputMouse SecFireKey = new InputMouse { mouseButton = 1 };

        public static InputKey MoveForwardsKey  = new InputKey { keyCode = KeyCode.W };
        public static InputKey MoveBackwardsKey = new InputKey { keyCode = KeyCode.S };
        public static InputKey MoveLeftKey      = new InputKey { keyCode = KeyCode.A };
        public static InputKey MoveRightKey     = new InputKey { keyCode = KeyCode.D };
        public static InputKey JumpKey          = new InputKey { keyCode = KeyCode.Space };
        public static InputKey WalkKey          = new InputKey { keyCode = KeyCode.LeftShift };
        public static InputKey ChatKey          = new InputKey { keyCode = KeyCode.Return, ignoreDisableInput = true };
    }
}
