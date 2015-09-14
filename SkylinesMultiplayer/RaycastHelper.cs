using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace SkylinesMultiplayer
{
    [Flags]
    public enum RayFlags
    {
        None = 0,
        Physics = 1,
        Building = 2,
        Road = 4,
        Prop = 8,
        Terrain = 16,
        Tree = 32,
        Citizen = 64,
        Vehicle = 128,
        VehicleParked = 256,
        Player = 512
    }

    public struct RayHitInfo
    {
        public bool rayHit;
        public RayFlags hitType;
        public Vector3 point;
        public RaycastHit raycastHit;
        public int index;
    }

    class RaycastHelper
    {
        public static RayHitInfo Raycast(Vector3 startPos, Vector3 endPos)
        {
            var rayHitInfo = new RayHitInfo();
            Segment3 segment = new Segment3(startPos, endPos);
            Vector3 direction = (endPos - startPos).normalized;
            float length = Vector3.Distance(startPos, endPos);
            float closestRayHitDistance = float.MaxValue;
            Vector3 tempHitPos;


            ushort a;
            ushort b;

            RaycastHit hit;
            Ray ray = new Ray(startPos, endPos - startPos);
            if (Physics.Raycast(ray, out hit))
            {
                closestRayHitDistance = Vector3.SqrMagnitude(startPos - hit.point);

                rayHitInfo.rayHit = true;
                
                rayHitInfo.point = hit.point;
                rayHitInfo.raycastHit = hit;

                if (hit.collider.tag == "Player")
                {
                    rayHitInfo.hitType = RayFlags.Player;
                }
                else
                {
                    rayHitInfo.hitType = RayFlags.Physics;
                }
            }

            if (Singleton<NetManager>.instance.RayCast(segment, 0f, ItemClass.Service.None, ItemClass.Service.None, ItemClass.SubService.None,
                ItemClass.SubService.None, ItemClass.Layer.Default, ItemClass.Layer.None, NetNode.Flags.OnGround, NetSegment.Flags.None,
                out tempHitPos, out a, out b))
            {
                float distance = Vector3.SqrMagnitude(startPos - tempHitPos);
                if (distance < closestRayHitDistance)
                {
                    closestRayHitDistance = distance;

                    rayHitInfo.rayHit = true;
                    rayHitInfo.hitType = RayFlags.Road;
                    rayHitInfo.point = tempHitPos;
                }
            }

            if (Singleton<BuildingManager>.instance.RayCast(segment, ItemClass.Service.None,
                ItemClass.SubService.None, ItemClass.Layer.Default, Building.Flags.None, out tempHitPos, out a))
            {
                float distance = Vector3.SqrMagnitude(startPos - tempHitPos);
                if (distance < closestRayHitDistance)
                {
                    closestRayHitDistance = distance;

                    rayHitInfo.rayHit = true;
                    rayHitInfo.hitType = RayFlags.Building;
                    rayHitInfo.point = tempHitPos;
                }
            }

            if (Singleton<PropManager>.instance.RayCast(segment, ItemClass.Service.None,
                ItemClass.SubService.None, ItemClass.Layer.Default, PropInstance.Flags.None, out tempHitPos, out a))
            {
                float distance = Vector3.SqrMagnitude(startPos - tempHitPos);
                if (distance < closestRayHitDistance)
                {
                    closestRayHitDistance = distance;

                    rayHitInfo.rayHit = true;
                    rayHitInfo.hitType = RayFlags.Prop;
                    rayHitInfo.point = tempHitPos;
                }
            }

            if (Singleton<TerrainManager>.instance.RayCast(segment, out tempHitPos))
            {
                float distance = Vector3.SqrMagnitude(startPos - tempHitPos);
                if (distance < closestRayHitDistance)
                {
                    closestRayHitDistance = distance;

                    rayHitInfo.rayHit = true;
                    rayHitInfo.hitType = RayFlags.Terrain;
                    rayHitInfo.point = tempHitPos;
                }
            }
            uint c;
            if (Singleton<TreeManager>.instance.RayCast(segment, ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default, TreeInstance.Flags.None, out tempHitPos,out c))
            {
                float distance = Vector3.SqrMagnitude(startPos - tempHitPos);
                if (distance < closestRayHitDistance)
                {
                    closestRayHitDistance = distance;

                    rayHitInfo.rayHit = true;
                    rayHitInfo.hitType = RayFlags.Tree;
                    rayHitInfo.point = tempHitPos;
                }
            }

            if (Singleton<CitizenManager>.instance.RayCast(segment, CitizenInstance.Flags.None, out tempHitPos, out a))
            {
                float distance = Vector3.SqrMagnitude(startPos - tempHitPos);
                if (distance < closestRayHitDistance)
                {
                    closestRayHitDistance = distance;

                    rayHitInfo.rayHit = true;
                    rayHitInfo.hitType = RayFlags.Citizen;
                    rayHitInfo.point = tempHitPos;
                    rayHitInfo.index = a;
                }
            }

            if (Singleton<VehicleManager>.instance.RayCast(segment, Vehicle.Flags.None, VehicleParked.Flags.All, out tempHitPos, out a, out b))
            {
                float distance = Vector3.SqrMagnitude(startPos - tempHitPos);
                if (distance < closestRayHitDistance)
                {
                    closestRayHitDistance = distance;

                    rayHitInfo.rayHit = true;
                    rayHitInfo.hitType = RayFlags.Vehicle;
                    rayHitInfo.point = tempHitPos;
                    rayHitInfo.index = a != 0 ? a : b;
                }
            }

            if (Singleton<VehicleManager>.instance.RayCast(segment, Vehicle.Flags.All, VehicleParked.Flags.None, out tempHitPos, out a, out b))
            {
                float distance = Vector3.SqrMagnitude(startPos - tempHitPos);
                if (distance < closestRayHitDistance)
                {
                    closestRayHitDistance = distance;

                    rayHitInfo.rayHit = true;
                    rayHitInfo.hitType = RayFlags.VehicleParked;
                    rayHitInfo.point = tempHitPos;
                    rayHitInfo.index = a != 0 ? a : b;
                }
            }

            return rayHitInfo;
        }
    }
}
