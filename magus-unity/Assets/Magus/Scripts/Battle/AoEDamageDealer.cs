using UnityEngine;

namespace magus.battle
{
    /// <summary>
    /// One-shot area damage: spawn at the impact point and call Detonate() once.
    /// Unlike DOTAreaBase (which tracks a trigger volume over time and ticks
    /// repeatedly), this does a single Physics.OverlapSphere pass and is done -
    /// suited to an explosion/impact rather than a lingering hazard.
    /// </summary>
    [RequireComponent(typeof(DamageDealer))]
    public class AoEDamageDealer : MonoBehaviour
    {
        [SerializeField]
        private DamageDealer _damageDealer;

        public void Detonate(IBattleEntity owner, float radius, float damage)
        {
            _damageDealer.Damage = damage;
            _damageDealer.SetOwner(owner);
            _damageDealer.BeginAttack();

            var mask = LayerMask.GetMask("Character");
            var hits = Physics.OverlapSphere(transform.position, radius, mask);

            foreach (var hit in hits)
            {
                var receiver = hit.GetComponent<DamageReceiver>();
                if (receiver != null)
                {
                    // Closest point on the receiver's own collider to the blast center -
                    // where the AoE and the receiver actually intersect, rather than the
                    // dealer's (usually centered-on-impact) transform position.
                    var hitPos = hit.ClosestPoint(transform.position);
                    _damageDealer.TryDamage(receiver, hitPos);
                }
            }

            _damageDealer.EndAttack();
        }
    }
}
