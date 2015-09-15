using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkylinesMultiplayer.Prefabs;
using ColossalFramework;
using ColossalFramework.Math;
using Lidgren.Network;
using UnityEngine;

namespace SkylinesMultiplayer
{
    public class PlayerController : MonoBehaviour
    {
        private const float HEIGHT_OVER_GROUND = 0.5f;
        private const int WALK_SPEED    = 175;
        private const int RUN_SPEED     = 315;
        private const float GROUND_SLOW = 0.30f;    //How much you of your velocity gets removed/sec when on the ground
        private const float JUMP_FORCE = 10;

        private const float HOOK_COOLDOWN = 2;
        private const float HOOK_FORCE = 50;
        private const float HOOK_RELEASE_MAX_UP_MOVEMENTUM = 15;

        private Rigidbody m_rigidBody;

        private Vector3 m_velocity;
        private bool m_canJump;

        private float m_hookCooldown;
        private LineRenderer m_hookLineRender;
        private Vector3 m_hookHitPosition;
        private bool m_hookHit;
        private RayHitInfo m_rayCastInfo;

        public int m_ignoreFlags;

        void Start()
        {
            m_rigidBody = gameObject.GetComponent<Rigidbody>();
            
            m_hookLineRender = gameObject.AddComponent<LineRenderer>();
            m_hookLineRender.material = new Material(Shader.Find("Sprites/Default"));
            m_hookLineRender.SetWidth(1, 0.4f);
            Color startColor = new Color(0.55f, 0, 0);
            Color endColor = new Color(0.15f, 0, 0);
            m_hookLineRender.SetColors(startColor, endColor);
            m_hookLineRender.enabled = false;
        }

        void FixedUpdate()
        {
            m_canJump = false;

            float terrainHeight = ModTerrainUtil.GetHeight(transform.position.x, transform.position.z);
            
            NetManager netManager = Singleton<NetManager>.instance;
            Segment3 ray = new Segment3(gameObject.transform.position + new Vector3(0f, 1.0f, 0f), gameObject.transform.position - new Vector3(0, 0.1f, 0));

            Vector3 hitPos;
            ushort nodeIndex;
            ushort segmentIndex;

            //Need to check fron below up becuse how the way road raycast work
            Segment3 ray2 = new Segment3(gameObject.transform.position - new Vector3(0, 2.1f, 0), gameObject.transform.position + new Vector3(0f, 1.0f, 0f));

            if (netManager.RayCast(ray2, 0f, ItemClass.Service.None, ItemClass.Service.None, ItemClass.SubService.None,
                ItemClass.SubService.None, ItemClass.Layer.Default, ItemClass.Layer.None, NetNode.Flags.OnGround, NetSegment.Flags.None,
                out hitPos, out nodeIndex, out segmentIndex))
            {
                m_canJump = true;
                terrainHeight = Mathf.Max(terrainHeight, hitPos.y);
            }

            if (transform.position.y < terrainHeight)
            {
                m_rigidBody.velocity = Vector3.Scale(m_rigidBody.velocity, new Vector3(1, 0, 1));
                m_canJump = true;
            }
            
            if (Physics.Raycast(transform.position, Vector3.down, 0.1f) ||
                Physics.Raycast(transform.position + new Vector3(0.5f, 0, 0.5f), Vector3.down, 2.1f) ||
                Physics.Raycast(transform.position + new Vector3(-0.5f, 0, 0.5f), Vector3.down, 2.1f) ||
                Physics.Raycast(transform.position + new Vector3(0.5f, 0, -0.5f), Vector3.down, 2.1f) ||
                Physics.Raycast(transform.position + new Vector3(-0.5f, 0, -0.5f), Vector3.down, 2.1f))
            {
                m_canJump = true;
            }

            transform.position = new Vector3(transform.position.x, Math.Max(terrainHeight, transform.position.y), transform.position.z);

            if (Input.GetKeyDown(KeyCode.Space) && (m_canJump || m_hookHit))
            {
                m_rigidBody.velocity = new Vector3(m_rigidBody.velocity.x, JUMP_FORCE, m_rigidBody.velocity.y);

                if (m_hookHit)
                {
                    m_hookHit = false;
                    m_hookLineRender.enabled = false;
                    SendPlayerHookReleased();

                    m_rigidBody.velocity = new Vector3(m_rigidBody.velocity.x, JUMP_FORCE, m_rigidBody.velocity.y);
                }
            }


            float movementSpeed = Input.GetKey(KeyCode.LeftShift) ? RUN_SPEED : WALK_SPEED;

            if (!m_hookHit)
            {
                int verticalInput = Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0;
                int horInput = Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0;
                Vector3 inputDir = new Vector3(horInput, 0, verticalInput);
                inputDir.Normalize();

                m_velocity += transform.rotation * inputDir * movementSpeed * Time.deltaTime;

                m_velocity.x = (m_velocity.x*(1 - GROUND_SLOW));
                m_velocity.z = (m_velocity.z*(1 - GROUND_SLOW));

                m_rigidBody.velocity = new Vector3(m_velocity.x, m_rigidBody.velocity.y, m_velocity.z);

            }
        }
        
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                //SoundManager.PlayClipAtPoint("WeaponShot", transform.position, 0.5f);

                RayHitInfo hitInfo = RaycastHelper.Raycast(Camera.main.transform.position, Camera.main.transform.position + Camera.main.transform.forward * 300);

                if (hitInfo.rayHit)
                {
                    int hitPlayerId = 0;
                    if (hitInfo.hitType == RayFlags.Player)
                    {
                        hitPlayerId = hitInfo.raycastHit.collider.GetComponent<Player>().m_playerId;
                    }

                    MessagePlayerShotBullet message = new MessagePlayerShotBullet();
                    message.playerId = gameObject.GetComponent<Player>().m_playerId;
                    message.hitPlayerId = hitPlayerId;
                    message.damage = 10;
                    message.hitPosition = hitInfo.point;
                    message.hitDirection = (transform.position - hitInfo.point);
                    MultiplayerManager.instance.SendNetworkMessage(message, SendTo.All);
                }
            }

            m_hookCooldown -= Time.deltaTime;
            m_hookCooldown = Math.Max(0, m_hookCooldown);

            if (Input.GetMouseButtonDown(1) && m_hookCooldown <= 0)
            {
                Vector3 startPointShot = Camera.main.transform.position;
                Vector3 endPointShot = Camera.main.transform.position + Camera.main.transform.forward * 100;

                m_rayCastInfo = RaycastHelper.Raycast(startPointShot, endPointShot);

                m_hookHitPosition = m_rayCastInfo.point;

                if (m_rayCastInfo.rayHit)
                {
                    m_hookCooldown = HOOK_COOLDOWN;
                    m_hookHit = true;

                    m_hookLineRender.SetPosition(0, transform.position);
                    m_hookLineRender.SetPosition(1, m_hookHitPosition);
                    m_hookLineRender.enabled = true;

                    SendPlayerHookUsed(m_hookHitPosition);
                }
            }

            if (Input.GetMouseButton(1) && m_hookHit)
            {
                if (m_rayCastInfo.hitType == RayFlags.Citizen && m_rayCastInfo.index != 0)
                {
                    var cInstance = Singleton<CitizenManager>.instance;

                    var citizenInstace = cInstance.m_instances.m_buffer[m_rayCastInfo.index];
                    var citizenInfo = citizenInstace.Info;
                        
                    Vector3 citizenPosition;
                    Quaternion citizenRotation;
                    citizenInstace.GetSmoothPosition((ushort)m_rayCastInfo.index, out citizenPosition, out citizenRotation);

                    m_hookHitPosition = citizenPosition;
                    m_hookLineRender.SetPosition(1, m_hookHitPosition);
                    SendPlayerHookUsed(m_hookHitPosition);
                }
                else
                {
                    
                    if (m_rayCastInfo.hitType == RayFlags.Vehicle && m_rayCastInfo.index != 0)
                    {
                        var vInstance = Singleton<VehicleManager>.instance;

                        var citizenInstace = vInstance.m_vehicles.m_buffer[m_rayCastInfo.index];
                        var citizenInfo = citizenInstace.Info;

                        Vector3 citizenPosition;
                        Quaternion citizenRotation;
                        citizenInstace.GetSmoothPosition((ushort)m_rayCastInfo.index, out citizenPosition, out citizenRotation);

                        m_hookHitPosition = citizenPosition;
                        m_hookLineRender.SetPosition(1, m_hookHitPosition);
                        SendPlayerHookUsed(m_hookHitPosition);
                    }
                }

                m_hookLineRender.SetPosition(0, transform.position);
                
                Vector3 dir = (m_hookHitPosition - transform.position).normalized;

                float hookForce = Math.Min(Vector3.Distance(transform.position, m_hookHitPosition) * 15, HOOK_FORCE);

                m_rigidBody.velocity = dir * hookForce;
                m_rigidBody.useGravity = false;
            }
            else
            {
                m_rigidBody.useGravity = true;
            }

            if (Input.GetMouseButtonUp(1) && m_hookHit)
            {
                OnHookReleased();
            }
        }

        private void OnHookReleased()
        {
            m_hookHit = false;
            m_hookLineRender.enabled = false;
            m_rigidBody.velocity = new Vector3(m_rigidBody.velocity.x, Math.Min(m_rigidBody.velocity.y, HOOK_RELEASE_MAX_UP_MOVEMENTUM) , m_rigidBody.velocity.z);  //Limit upward movementum so you don't fly really far up
            SendPlayerHookReleased();
        }

        private void SendPlayerShot(int playerId, Vector3 hitPoint, Vector3 hitDirection)
        {
            MultiplayerManager mInstance = MultiplayerManager.instance;

            int playerID = GetComponent<Player>().m_playerId;

            MessagePlayerShotBullet message = new MessagePlayerShotBullet();
            message.playerId = playerID;
            message.hitPosition = hitPoint;
            message.hitDirection = hitDirection;

            if (mInstance.m_isServer)
            {
                List<NetConnection> sendTo = mInstance.m_server.Connections;

                if (sendTo.Count > 0)
                {
                    NetOutgoingMessage om = mInstance.m_server.CreateMessage();
                    om.Write((int)MessageFunction.PlayerShotBullet);
                    om.WriteAllFields(message);

                    mInstance.m_server.SendToAll(om, NetDeliveryMethod.ReliableOrdered);
                }
            }
            else
            {
                NetOutgoingMessage om = mInstance.m_client.CreateMessage();
                om.Write((int)MessageFunction.PlayerShotBullet);
                om.WriteAllFields(message);

                mInstance.m_client.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
            }
        }

        void OnGUI()
        {
            GUI.Box(new Rect(Screen.width - 150, Screen.height - 100, 50, 25), m_hookCooldown.ToString("F2"));
        }

        private void SendPlayerHookUsed(Vector3 hookPosition)
        {
            MultiplayerManager mInstance = MultiplayerManager.instance;

            int playerID = GetComponent<Player>().m_playerId;

            MessagePlayerHookUsed message = new MessagePlayerHookUsed();
            message.playerId = playerID;
            message.hookPosition = hookPosition;
            mInstance.SendNetworkMessage(message, SendTo.Others);
        }

        private void SendPlayerHookReleased()
        {
            MultiplayerManager mInstance = MultiplayerManager.instance;

            int playerID = GetComponent<Player>().m_playerId;

            MessagePlayerHookReleased message = new MessagePlayerHookReleased();
            message.playerId = playerID;
            mInstance.SendNetworkMessage(message, SendTo.Others);
        }
    }
}
