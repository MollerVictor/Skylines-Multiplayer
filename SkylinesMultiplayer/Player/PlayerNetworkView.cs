using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using UnityEngine;

namespace SkylinesMultiplayer
{
    class PlayerNetworkView : MonoBehaviour
    {
        internal struct State
        {
            internal double timestamp;
            internal Vector3 pos;
            internal Quaternion rot;
            internal Vector3 angularVelocity;
        }

        public double m_InterpolationBackTime = 0.1;
        public double m_ExtrapolationLimit = 0.5;

        private Animator m_animator;
        public int state = 0;
        private Vector3 m_lastPos;

        // We store twenty states with "playback" information
        public State[] m_BufferedState = new State[20];
        // Keep track of what slots are used
        int m_TimestampCount;


        void Start()
        {
            m_animator = GetComponent<Animator>();
        }


        // We have a window of interpolationBackTime where we basically play 
        // By having interpolationBackTime the average ping, you will usually use interpolation.
        // And only if no more data arrives we will use extra polation
        void Update()
        {
            Vector3 speed = (transform.position - m_lastPos) / Time.deltaTime;
            speed.y = 0;
            float totalSpeed = Mathf.Abs(speed.magnitude);
            m_animator.SetFloat("Speed", totalSpeed / (32 * 0.35f));

            m_lastPos = transform.position;

            // This is the target playback time of the rigid body
            double interpolationTime = NetTime.Now - m_InterpolationBackTime;
            
            // Use interpolation if the target playback time is present in the buffer
            if (m_BufferedState[0].timestamp > interpolationTime)
            {
                // Go through buffer and find correct state to play back
                for (int i = 0; i < m_TimestampCount; i++)
                {
                    if (m_BufferedState[i].timestamp <= interpolationTime || i == m_TimestampCount - 1)
                    {
                        // The state one slot newer (<100ms) than the best playback state
                        State rhs = m_BufferedState[Mathf.Max(i - 1, 0)];
                        // The best playback state (closest to 100 ms old (default time))
                        State lhs = m_BufferedState[i];

                        // Use the time between the two slots to determine if interpolation is necessary
                        double length = rhs.timestamp - lhs.timestamp;
                        float t = 0.0F;
                        // As the time difference gets closer to 100 ms t gets closer to 1 in 
                        // which case rhs is only used
                        // Example:
                        // Time is 10.000, so sampleTime is 9.900 
                        // lhs.time is 9.910 rhs.time is 9.980 length is 0.070
                        // t is 9.900 - 9.910 / 0.070 = 0.14. So it uses 14% of rhs, 86% of lhs
                        if (length > 0.0001)
                        {
                            t = (float)((interpolationTime - lhs.timestamp) / length);
                        }
                        //    Debug.Log(t);
                        // if t=0 => lhs is used directly
                        transform.localPosition = Vector3.Lerp(lhs.pos, rhs.pos, t);
                        transform.localRotation = Quaternion.Slerp(lhs.rot, rhs.rot, t);
                        return;
                    }
                }
            }
            // Use extrapolation
            else
            {
                State latest = m_BufferedState[0];

                float extrapolationLength = (float)(interpolationTime - latest.timestamp);
                // Don't extrapolation for more than 500 ms, you would need to do that carefully
                if (extrapolationLength < m_ExtrapolationLimit)
                {
                    float axisLength = extrapolationLength * latest.angularVelocity.magnitude * Mathf.Rad2Deg;
                    Quaternion angularRotation = Quaternion.AngleAxis(axisLength, latest.angularVelocity);

                    transform.position = latest.pos;
                    transform.rotation = angularRotation * latest.rot;
                }
            }
        }

        public void OnNetworkData(double time, Vector3 position, Quaternion rotation)
        {
            // Shift the buffer sideways, deleting state 20
            for (int i = m_BufferedState.Length - 1; i >= 1; i--)
            {
                m_BufferedState[i] = m_BufferedState[i - 1];
            }

            // Record current state in slot 0
            State state = new State();
            state.timestamp = time;
            state.pos = position;
            state.rot = rotation;

            m_BufferedState[0] = state;

            // Update used slot count, however never exceed the buffer size
            // Slots aren't actually freed so this just makes sure the buffer is
            // filled up and that uninitalized slots aren't used.
            m_TimestampCount = Mathf.Min(m_TimestampCount + 1, m_BufferedState.Length);

            // Check if states are in order, if it is inconsistent you could reshuffel or 
            // drop the out-of-order state. Nothing is done here
            for (int i = 0; i < m_TimestampCount - 1; i++)
            {
                if (m_BufferedState[i].timestamp < m_BufferedState[i + 1].timestamp)
                    Debug.Log("State inconsistent");
            }
        }
    }
}
