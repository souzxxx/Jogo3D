using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Enemies
{
    /// Armadilha de espinhos: avisa um circulo curto e sobe. Fura quem estiver em cima, heroi ou inimigo.
    public class SpikeTrap : MonoBehaviour
    {
        [SerializeField] float interval = 3.6f;
        [SerializeField] float windup = 0.8f;
        [SerializeField] float radius = 1.45f;
        [SerializeField] float damage = 9f;
        [SerializeField] Transform spikes;
        [SerializeField] float downY = -0.5f;

        float timer, upLeft;
        bool warned;
        static readonly Collider[] buffer = new Collider[24];
        readonly HashSet<Health> seen = new();
        public int Strikes { get; private set; }
        public float Radius => radius;

        public void Bind(Transform moving, float hiddenY) { spikes = moving; downY = hiddenY; }

        void OnEnable()
        {
            timer = Random.Range(1.2f, interval);
            SetHeight(0f);
        }

        void OnDisable() => GameEvents.RaiseTelegraphCancelled(gameObject);

        void SetHeight(float k)
        {
            if (spikes == null) return;
            var p = spikes.localPosition;
            p.y = Mathf.Lerp(downY, 0f, k);
            spikes.localPosition = p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (upLeft > 0f)
            {
                upLeft -= dt;
                SetHeight(Mathf.Clamp01(upLeft / 0.25f));
            }
            timer -= dt;
            if (!warned && timer <= windup)
            {
                warned = true;
                GameEvents.RaiseTelegraph(new TelegraphEvent
                {
                    Point = transform.position, Direction = Vector3.forward, Radius = radius, Duration = Mathf.Max(0f, timer), Source = gameObject,
                });
            }
            if (timer > 0f) return;
            timer = interval;
            warned = false;
            Strike();
        }

        void Strike()
        {
            Strikes++;
            upLeft = 0.7f;
            SetHeight(1f);
            Sfx.Play("block");
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
            seen.Clear();
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h.IsDead || !seen.Add(h)) continue;
                // so criatura: cristal, barril e boneco nao sao feridos pelo chao
                if (h.Team == Team.Enemy && h.GetComponent<UnityEngine.AI.NavMeshAgent>() == null) continue;
                var flat = h.transform.position - transform.position;
                flat.y = 0f;
                if (flat.magnitude > radius + 0.2f) continue;
                var from = h.Team == Team.Player ? Team.Enemy : Team.Player;
                h.TakeDamage(new DamageInfo(damage, ElementType.None, from, gameObject)
                {
                    Point = h.transform.position + Vector3.up * 0.6f, Direction = Vector3.up, Knockback = 0f, IsReaction = true,
                });
            }
        }
    }
}
