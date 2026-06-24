using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField]
    private DamageType _damageType;

    [SerializeField]
    private float _defaultDamageValue;

    private IUnit _source;
    private float _damageValue;

    public void Setup(IUnit source, float damage) {
        _source = source;
        _damageValue = damage;
    }

    private void OnTriggerEnter(Collider collider) {
        var dest = collider.GetComponent<IDestructible>();
        if (dest != null) 
        {
            //
            var damage = new DamageInstance(_source, _damageType, _damageValue);
            dest.TakeDamage(damage);
        }
    }


    private void Destroy() {

    }

}
