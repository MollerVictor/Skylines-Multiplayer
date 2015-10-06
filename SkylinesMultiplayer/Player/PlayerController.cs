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
        //These values normaly never change, but don't have them as readonly/const incase if a mod want to change these values
        public int   WALK_SPEED             = 200;
        public int   RUN_SPEED              = 340;
        public float GROUND_SLOW            = 0.30f;    //How much you of your velocity gets removed/sec when on the ground
        public float AIR_Y_SLOW             = 0.85f;    //How much of your air y speed you keep
        public float JUMP_FORCE             = 28;       //The force that is applied when jumping
        public float JUMP_EXTRA_HEIGHT_TIME = 0.25f;    //How long you can hold the jump button to jump higher
        public float JUMP_INPUT_EXTRA_TIME  = 0.2f;     //How long after you press jump it still jumps if it can
        public float GRAVITY                = 35;
        public float STEP_SIZE              = 0.35f;
        public float TERRAIN_HEIGHT_OFFSET  = 0.75f;

        public float HOOK_COOLDOWN = 1.75f;
        public float HOOK_FORCE    = 50;
        public float HOOK_RELEASE_MAX_UP_MOVEMENTUM   = 22;
        public float HOOK_RELEASE_MAX_DOWN_MOVEMENTUM = -13;

        private Rigidbody m_rigidBody;

        private Vector3 m_velocity;
        private bool m_isGrounded;

        private float m_jumpExtraTimer;
        private float m_lastJumpTimer;

        private float m_hookCooldown;
        private LineRenderer m_hookLineRender;
        private Vector3 m_hookHitPosition;
        private bool m_hookHit;
        private RayHitInfo m_rayCastInfo;

        public int m_ignoreFlags;

        private GameObject m_sweepTestCollider;
        private Rigidbody m_sweepTestRigidbody;
        private Vector3 m_stepNewPosition;

        void Start()
        {
            m_rigidBody = gameObject.GetComponent<Rigidbody>();

            m_sweepTestCollider = new GameObject("SweepTestCollider");
            m_sweepTestCollider.layer = 2;   //Ignore raycast
            var collider = m_sweepTestCollider.AddComponent<BoxCollider>();
            collider.size = new Vector3(1, 2, 1);
            collider.isTrigger = true;
            m_sweepTestRigidbody = m_sweepTestCollider.AddComponent<Rigidbody>();
            m_sweepTestRigidbody.isKinematic = true;

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
            m_isGrounded = false;

            float terrainHeight = ModTerrainUtil.GetHeight(transform.position.x, transform.position.z);
            
            NetManager netManager = Singleton<NetManager>.instance;
            //Segment3 ray = new Segment3(gameObject.transform.position + new Vector3(0f, 1.0f, 0f), gameObject.transform.position - new Vector3(0, 0.1f, 0));

            Vector3 hitPos;
            Vector3 hitPos2;
            ushort nodeIndex;
            ushort segmentIndex;

            //Need to check fron below up becuse how the way road raycast work
            Segment3 ray2 = new Segment3(gameObject.transform.position, gameObject.transform.position - new Vector3(0f, 1.0f, 0f));

            Segment3 ray = new Segment3(gameObject.transform.position + new Vector3(0f, 1.5f, 0f), gameObject.transform.position + new Vector3(0f, -100f, 0f));


            if (  NetManager.instance.RayCast(ray, 0f, ItemClass.Service.Road, ItemClass.Service.PublicTransport, ItemClass.SubService.None, ItemClass.SubService.None, ItemClass.Layer.Default, ItemClass.Layer.None, NetNode.Flags.None, NetSegment.Flags.None, out hitPos, out nodeIndex, out segmentIndex)
                | NetManager.instance.RayCast(ray, 0f, ItemClass.Service.Beautification, ItemClass.Service.Water, ItemClass.SubService.None, ItemClass.SubService.None, ItemClass.Layer.Default, ItemClass.Layer.None, NetNode.Flags.None, NetSegment.Flags.None, out hitPos2, out nodeIndex, out segmentIndex))
            {
                terrainHeight = Mathf.Max(terrainHeight, Mathf.Max(hitPos.y, hitPos2.y));
            }

            if (transform.position.y < terrainHeight + 1 && m_velocity.y <= 0)
            {
                transform.position = new Vector3(transform.position.x, terrainHeight + 1 - TERRAIN_HEIGHT_OFFSET, transform.position.z);
                m_velocity.y = Math.Max(0, m_velocity.y);
                m_isGrounded = true;
            }
            
            RaycastHit rayHit3;

            //Move the collider up a bit so it dosen't touch the thing it gonna sweep test aganst
            m_sweepTestCollider.transform.position = transform.position + Vector3.up * 0.05f;
            if (m_sweepTestRigidbody.SweepTest(Vector3.down, out rayHit3, 0.1f))
            {
                m_isGrounded = true;
                m_velocity.y = Math.Max(0, m_velocity.y);
            }
            m_sweepTestCollider.transform.position = new Vector3(0, 10000, 0);

            if (!m_isGrounded && Physics.CheckSphere(transform.position - new Vector3(0, 1, 0), 0.45f))
            {
                m_isGrounded = true;
                m_velocity.y = Math.Max(0, m_velocity.y);
            }

            RaycastHit rayHit2;
            if (m_rigidBody.SweepTest(Vector3.up, out rayHit2, 0.05f))  //Stops your Y speed when you hit the roof so you fall own directly
            {
                m_velocity.y = Math.Min(0, m_velocity.y);
            }

            if (PlayerInput.JumpKey.GetKeyDown() && (m_isGrounded || m_hookHit))
            {
                if (m_hookHit)
                {
                    m_hookHit = false;
                    m_hookLineRender.enabled = false;
                    SendPlayerHookReleased();
                }

                Jump();
            }

            float movementSpeed = PlayerInput.WalkKey.GetKey() ? WALK_SPEED : RUN_SPEED;
            
            if (!m_hookHit)
            {
                int verticalInput = PlayerInput.MoveForwardsKey.GetKey() ? 1 : PlayerInput.MoveBackwardsKey.GetKey() ? -1 : 0;
                int horInput      = PlayerInput.MoveRightKey.GetKey()    ? 1 : PlayerInput.MoveLeftKey.GetKey() ? -1 : 0;
                Vector3 inputDir = new Vector3(horInput, 0, verticalInput);
                inputDir.Normalize();

                m_velocity += transform.rotation * inputDir * movementSpeed * Time.fixedDeltaTime;

                m_velocity.x = (m_velocity.x*(1 - GROUND_SLOW));
                m_velocity.z = (m_velocity.z*(1 - GROUND_SLOW));

                if (!m_isGrounded)
                    m_velocity.y -= GRAVITY * Time.fixedDeltaTime;   //Applys extra gravity becuse the normal one isen't enouch, and don't want to change the global gravity

                m_jumpExtraTimer -= Time.fixedDeltaTime;

                if (PlayerInput.JumpKey.GetKey())
                {
                    m_jumpExtraTimer = JUMP_INPUT_EXTRA_TIME;
                }

                if (m_jumpExtraTimer > 0 && m_isGrounded)
                {
                    Jump();
                }

                if (PlayerInput.JumpKey.GetKey() && m_hookHit)
                {
                    m_hookHit = false;
                    m_hookLineRender.enabled = false;
                    SendPlayerHookReleased();

                    Jump();
                }


                RaycastHit rayHit;
                Vector3 newVelocity = m_velocity;
                for (int i = 0; i < 5; i++)
                {
                    Vector3 newDelta = newVelocity * Time.fixedDeltaTime;
                    m_sweepTestCollider.transform.position = transform.position + -newDelta.normalized * 0.1f;
                    if (m_sweepTestRigidbody.SweepTest(newDelta.normalized, out rayHit, newDelta.magnitude + 0.1f))
                    {
                        m_sweepTestCollider.transform.position = transform.position + new Vector3(0, STEP_SIZE, 0) + -newDelta.normalized * 0.1f;
                        if (m_isGrounded && !m_sweepTestRigidbody.SweepTest(newDelta.normalized, out rayHit, newDelta.magnitude + 0.1f))
                        {
                            float newY = transform.position.y + STEP_SIZE;
                            m_sweepTestCollider.transform.position = new Vector3(transform.position.x + newDelta.x, transform.position.y + STEP_SIZE, transform.position.z + newDelta.z);
                            if (m_sweepTestRigidbody.SweepTest(Vector3.down, out rayHit, STEP_SIZE))
                            {
                                newY -= rayHit.distance;
                            }
                            m_stepNewPosition = new Vector3(transform.position.x + newDelta.x, newY, transform.position.z + newDelta.z);
                            break;
                        }
                        var distanceToPlane = Vector3.Dot(newDelta, rayHit.normal);

                        Vector3 desiredMotion = (distanceToPlane * rayHit.normal) / Time.fixedDeltaTime;
                        
                        newVelocity -= desiredMotion;
                        newVelocity += rayHit.normal*0.001f;
                    }
                    m_sweepTestCollider.transform.position = new Vector3(0, -10000, 0);
                }

                if (PlayerInput.JumpKey.GetKey() && m_lastJumpTimer <= JUMP_EXTRA_HEIGHT_TIME)
                {
                    m_velocity.y = JUMP_FORCE;
                }

                if (!m_isGrounded)
                {
                    newVelocity.y = newVelocity.y - newVelocity.y * AIR_Y_SLOW * Time.deltaTime;
                }

                m_velocity = newVelocity;
                m_rigidBody.velocity = m_velocity;
            }
        }

        void LateUpdate()
        {
            //When a stepsize happen then we need to move the player manully to ensure that he gets to the right position and not get stuck on anything
            if (m_stepNewPosition != Vector3.zero)
            {
                transform.position = m_stepNewPosition;
                m_stepNewPosition = Vector3.zero;
            }
        }


        void Jump()
        {
            m_isGrounded = false;
            m_jumpExtraTimer = 0;
            m_velocity.y = JUMP_FORCE;
            m_lastJumpTimer = 0;
        }
       
        void Update()
        {
            m_lastJumpTimer += Time.deltaTime;

            if (PlayerInput.FireKey.GetKeyDown())
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

            if (PlayerInput.SecFireKey.GetKeyDown() && m_hookCooldown <= 0)
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

            if (PlayerInput.SecFireKey.GetKey() && m_hookHit)
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

                m_velocity = dir * hookForce;
                m_rigidBody.velocity = m_velocity;
            }

            if (PlayerInput.SecFireKey.GetKeyUp() && m_hookHit)
            {
                OnHookReleased();
            }
        }

        private void OnHookReleased()
        {
            m_hookHit = false;
            m_hookLineRender.enabled = false;
            m_velocity.y = m_velocity.y > 0 ? Math.Min(m_velocity.y, HOOK_RELEASE_MAX_UP_MOVEMENTUM) : Math.Max(m_velocity.y, HOOK_RELEASE_MAX_DOWN_MOVEMENTUM);
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
