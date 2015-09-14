using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SkylinesMultiplayer
{
    public class FPSCameraController : MonoBehaviour
    {
        public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
        public RotationAxes axes = RotationAxes.MouseXAndY;
        public float sensitivityX = 7;
        public float sensitivityY = 7;

        public float minimumX = -360F;
        public float maximumX = 360F;

        public float minimumY = -90F;
        public float maximumY = 90F;

        public float normalFOV = 90;

        float rotationY = 0F;

        private bool m_invertY = false;

        void Update()
        {
            if (axes == RotationAxes.MouseXAndY)
            {
                float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

                if (m_invertY)
                    rotationY += Input.GetAxis("Mouse Y") * sensitivityY * -1;
                else
                    rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
            }
            else if (axes == RotationAxes.MouseX)
            {
                transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
            }
            else
            {
                if (m_invertY)
                    rotationY += Input.GetAxis("Mouse Y") * sensitivityY * -1;
                else
                    rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

                transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
            }
        }

        public void ChangeInvertY(bool invertY)
        {
            m_invertY = invertY;
        }

        public void ChangeSenestive(float sens)
        {
            sensitivityX = sens;
            sensitivityY = sens;
        }
    }
}
