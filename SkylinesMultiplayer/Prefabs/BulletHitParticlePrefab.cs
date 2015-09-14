using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SkylinesMultiplayer.Prefabs
{
    class BulletHitParticlePrefab
    {
        public static void Create(Vector3 position, Vector3 normal)
        {
            var effect = new GameObject("Particle Effect");
            effect.transform.position = position;
            effect.transform.rotation = Quaternion.LookRotation(normal);
            
            var particleSystem = effect.AddComponent<ParticleSystem>();
            particleSystem.startLifetime = 0.4f;
            particleSystem.startSpeed = 15;
            particleSystem.startSize = 0.13f;
            particleSystem.emissionRate = 100;
            particleSystem.loop = false;

            Material material = new Material(Shader.Find(" Diffuse"));
            material.color = Color.black;
            effect.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;

            GameObject.Destroy(effect, 0.2f);

            /*for (int i = 0; i < 30; i++)
            {
                ParticleSystem.Particle p = new ParticleSystem.Particle();
                p.velocity = normal*5;
                //p.position = position;
                p.lifetime = 5;
                particleSystem.Emit(p);
            }
            particleSystem.Play();*/
        }
    }
}
