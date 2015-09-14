using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using SkylinesMultiplayer.Prefabs;
using UnityEngine;

namespace SkylinesMultiplayer
{
    enum MessageFunction
    {
        None,
        SpawnAllPlayer,
        SpawnPlayer,
        UpdateVehiclesPositions,
        RemoveVehicle,
        UpdateCitizensPositions,
        PlayerShotBullet,
        ClassMessage,
        Ping
    }

    enum UnConnectedMessageFunction
    {
        None,
        PingServerForPlayerCount,
        PingServerForMapId
    }

    public enum SendTo
    {
        None,
        Others,
        All,
        Server
    }

    public class Message
    {
        public virtual void OnCalled(Message msg, NetIncomingMessage netMsg = null)
        {

        }
    }

    class MessageSpawnPlayer : Message
    {
        public int playerId;
        public string playerName;

        public override void OnCalled(Message msg, NetIncomingMessage netMsg = null)
        {
            MessageSpawnPlayer message = msg as MessageSpawnPlayer;
            MultiplayerManager.instance.SpawnPlayer(message.playerId, message.playerName, netMsg.SenderConnection);
        }
    }

    class MessageRemovePlayer : Message
    {
        public int playerId;

        public override void OnCalled(Message msg, NetIncomingMessage netMsg = null)
        {
            MessageRemovePlayer message = msg as MessageRemovePlayer;
            var player = MultiplayerManager.instance.m_players.FirstOrDefault(x => x.ID == message.playerId);
            if (player != null)
            {
                GameObject.Destroy(player.PlayerGameObject);
                MultiplayerManager.instance.m_players.Remove(player);
            }
        }
    }

    class MessageUpdatePlayerPosition : Message
    {
        public int playerId;
        public Vector3 position;
        public Vector3 rotation;

        public override void OnCalled(Message msg, NetIncomingMessage netMsg = null)
        {
            MessageUpdatePlayerPosition message = msg as MessageUpdatePlayerPosition;
            var instance = MultiplayerManager.instance;

            var player = instance.m_players.FirstOrDefault(x => x.ID == message.playerId);
            if (player != null)
            {
                player.PlayerGameObject.GetComponent<PlayerNetworkView>().OnNetworkData(netMsg.ReceiveTime, message.position, Quaternion.Euler(message.rotation));
            }
        }
    }

    class MessagePlayerHookUsed : Message
    {
        public int playerId;
        public Vector3 hookPosition;

        public override void OnCalled(Message msg, NetIncomingMessage netMsg = null)
        {
            MessagePlayerHookUsed message = msg as MessagePlayerHookUsed;

            var player = MultiplayerManager.instance.m_players.FirstOrDefault(x => x.ID == message.playerId);
            player.PlayerGameObject.GetComponent<PlayerRemote>().OnPlayerHookUsed(message.hookPosition);
        }
    }


    class MessagePlayerHookReleased : Message
    {
        public int playerId;

        public override void OnCalled(Message msg, NetIncomingMessage netMsg = null)
        {
            MessagePlayerHookReleased message = msg as MessagePlayerHookReleased;

            var player = MultiplayerManager.instance.m_players.FirstOrDefault(x => x.ID == message.playerId);
            player.PlayerGameObject.GetComponent<PlayerRemote>().OnPlayerHookReleased();
        }
    }


    class MessagePlayerShotBullet : Message
    {
        public int playerId;
        public int hitPlayerId;
        public int damage;
        public Vector3 hitPosition;
        public Vector3 hitDirection;

        public override void OnCalled(Message msg, NetIncomingMessage netMsg = null)
        {
            MessagePlayerShotBullet message = msg as MessagePlayerShotBullet;
            BulletHitParticlePrefab.Create(message.hitPosition, message.hitDirection);

            if (message.hitPlayerId != 0)
            {
                var playerHit = MultiplayerManager.instance.m_players.FirstOrDefault(x => x.ID == message.hitPlayerId);
                if (playerHit != null)
                {
                    playerHit.PlayerGameObject.GetComponent<Player>().OnDamageTaken(playerId, damage);
                }
            }
        }
    }
}
