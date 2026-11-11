using System.Collections;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Boneco de treino: apanha, e empurrado, some ao morrer e volta depois. So para a cena de teste.
    [RequireComponent(typeof(Health))]
    public class DummyTarget : MonoBehaviour
    {
        [SerializeField] float respawnDelay = 2.5f;
        Health health;
        Collider col;
        Vector3 home, velocity;
        float punch;

        void Awake()
        {
            health = GetComponent<Health>();
            col = GetComponent<Collider>();
            home = transform.position;
        }

        void OnEnable() { health.Damaged += OnDamaged; health.Died += OnDied; }
        void OnDisable() { health.Damaged -= OnDamaged; health.Died -= OnDied; }

        void OnDamaged(DamageInfo info, float applied)
        {
            velocity += info.Direction * info.Knockback;
            punch = 1f;
        }

        void OnDied(DamageInfo info) => StartCoroutine(Respawn());

        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(0.25f);
            Show(false);
            yield return new WaitForSeconds(respawnDelay);
            transform.position = home;
            velocity = Vector3.zero;
            health.Pool.Revive();
            Show(true);
        }

        void Show(bool on)
        {
            if (col != null) col.enabled = on;
            foreach (Transform child in transform) child.gameObject.SetActive(on);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            transform.position += velocity * dt;
            velocity = Vector3.Lerp(velocity, Vector3.zero, 10f * dt);
            punch = Mathf.MoveTowards(punch, 0f, dt * 6f);
            transform.localScale = new Vector3(1f + 0.2f * punch, 1f - 0.2f * punch, 1f + 0.2f * punch);
        }
    }
}
