using System.Collections.Generic;
using Quintessence.Combat;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Game
{
    /// Poca d'agua que o orbe do Afogado deixa no chao: encharca o heroi (fica lento) e marca com o elemento
    /// os inimigos dentro dela, como a poca de corrupcao. Some sozinha.
    public class WaterPuddle : MonoBehaviour
    {
        [SerializeField] float radius = 2f;
        [SerializeField] float interval = 0.5f;
        float life, tick;
        ElementType element = ElementType.Water;
        static readonly Collider[] buffer = new Collider[24];
        readonly HashSet<Health> seen = new();
        public float Radius => radius;
        public ElementType Element => element;
        public int Pulses { get; private set; }

        /// Cria a poca a partir do prefab montado no editor (o material vem dele: nada de shader procurado em tempo de jogo).
        public static WaterPuddle Spawn(GameObject prefab, Vector3 point, float radius, ElementType element, float seconds)
        {
            if (prefab == null) return null;
            var go = Instantiate(prefab, new Vector3(point.x, 0.02f, point.z), Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            var p = go.GetComponent<WaterPuddle>();
            p.radius = radius;
            p.element = element;
            p.life = seconds;
            go.transform.localScale = new Vector3(radius / 2f, 1f, radius / 2f); // o prefab tem raio 2
            return p;
        }

        void Update()
        {
            // o ultimo segundo encolhe: avisa que vai sumir
            if ((life -= Time.deltaTime) <= 0f) { Destroy(gameObject); return; }
            if (life < 1f) transform.localScale = new Vector3(radius / 2f * life, 1f, radius / 2f * life);
            if ((tick -= Time.deltaTime) > 0f) return;
            tick = interval;
            Pulses++;
            // no ultimo segundo a poca encolhe na tela e no efeito
            float r = radius * Mathf.Min(1f, life);
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.5f, r, buffer, ~0, QueryTriggerInteraction.Ignore);
            seen.Clear();
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h.IsDead || !seen.Add(h)) continue;
                var flat = h.transform.position - transform.position; flat.y = 0f;
                if (flat.magnitude > r) continue;
                if (h.Team == Team.Player) { StatusHolder.GetOrAdd(h).Apply(StatusType.Soaked, 1f, Team.Enemy, gameObject); continue; }
                // so criatura: barril e boneco nao ficam marcados
                if (h.GetComponent<UnityEngine.AI.NavMeshAgent>() == null) continue;
                var mark = h.GetComponent<ElementMark>();
                if (mark == null) mark = h.gameObject.AddComponent<ElementMark>();
                if (mark.State.HasMark && mark.State.Element != element) continue;
                mark.State.Hit(element, out _);
                GameEvents.RaiseMarkApplied(element, h);
            }
        }
    }
}
