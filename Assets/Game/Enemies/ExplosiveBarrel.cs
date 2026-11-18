using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Enemies
{
    /// Barril de polvora: qualquer golpe acende. Estoura depois de um instante e fere todos em volta,
    /// bem mais os inimigos que o heroi. Um barril acende o outro.
    /// Agua molha a polvora (como apaga velas e respiradouros): por WetSeconds o barril fica azulado, sem a faixa
    /// de alcance no chao, e o proximo golpe so o quebra, sem explosao. O golpe basico nao tem elemento, entao
    /// quem tem Agua ainda escolhe: skill de Agua desarma, golpe basico explode.
    [RequireComponent(typeof(Health))]
    public class ExplosiveBarrel : MonoBehaviour, IDamageIntercept
    {
        [SerializeField] float radius = 3.2f;
        [SerializeField] float damageToEnemies = 26f;
        [SerializeField] float damageToHero = 10f;
        [SerializeField] float fuse = 0.22f;

        public const float WetSeconds = 6f;

        Health health;
        bool lit;
        float wetLeft;
        Renderer[] renderers;
        Transform scorch;
        MaterialPropertyBlock block;
        static readonly Collider[] buffer = new Collider[32];
        public float Radius => radius;
        public bool Lit => lit;
        public bool Wet => wetLeft > 0f;
        /// Barris molhados e barris que quebraram molhados, sem explodir (para os testes).
        public static int Soaked { get; private set; }
        public static int Fizzled { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCounters() { Soaked = 0; Fizzled = 0; }

        void Awake()
        {
            health = GetComponent<Health>();
            health.Died += _ => Light();
            var model = transform.Find("Model");
            renderers = model != null ? model.GetComponentsInChildren<Renderer>() : new Renderer[0];
            scorch = transform.Find("Scorch");
        }

        public bool Intercept(in DamageInfo info)
        {
            if (lit) return false;
            if (Wet)
            {
                // molhado: o golpe so quebra o barril (lascas e vapor), sem explodir
                Fizzled++;
                GameEvents.RaisePowderDoused(transform.position, true);
                Destroy(gameObject);
                return true;
            }
            if (info.Element != ElementType.Water) return false;
            Soak();
            return true;
        }

        void Soak()
        {
            Soaked++;
            wetLeft = WetSeconds;
            SetWetLook(true);
            GameEvents.RaisePowderDoused(transform.position, false);
        }

        void Update()
        {
            if (wetLeft <= 0f) return;
            wetLeft -= Time.deltaTime;
            if (wetLeft <= 0f) SetWetLook(false);
        }

        void SetWetLook(bool wet)
        {
            // azul-acinzentado escuro, sem brilho: le como madeira encharcada; a faixa do alcance some (nao ha perigo)
            if (scorch != null) scorch.gameObject.SetActive(!wet);
            block ??= new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                if (wet) block.SetColor("_BaseColor", new Color(0.3f, 0.45f, 0.74f));
                else block.Clear();
                r.SetPropertyBlock(block);
            }
        }

        void Light()
        {
            if (lit) return;
            lit = true;
            GameEvents.RaiseTelegraph(new TelegraphEvent { Point = transform.position, Direction = Vector3.forward, Radius = radius, Duration = fuse, Source = gameObject });
            Invoke(nameof(Explode), fuse);
        }

        void Explode()
        {
            var center = transform.position;
            GameEvents.RaiseExploded(center, radius);
            int n = Physics.OverlapSphereNonAlloc(center + Vector3.up, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
            var seen = new HashSet<Health>();
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h == health || h.IsDead || !seen.Add(h)) continue;
                var dir = h.transform.position - center; dir.y = 0f;
                if (dir.magnitude > radius + 0.4f) continue;
                bool hero = h.Team == Team.Player;
                h.TakeDamage(new DamageInfo(hero ? damageToHero : damageToEnemies, ElementType.Fire, hero ? Team.Enemy : Team.Player, gameObject)
                {
                    Point = h.transform.position + Vector3.up * 1.2f, Direction = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.forward,
                    Knockback = 7f, IsReaction = true,
                });
            }
            Destroy(gameObject);
        }
    }
}
