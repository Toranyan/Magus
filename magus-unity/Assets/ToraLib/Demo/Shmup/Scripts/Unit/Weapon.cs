using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField]
    private Projectile projectilePrefab;

    


    public void Fire() {

    }


    protected Projectile SpawnProjectile() {
        return GameObject.Instantiate<Projectile>(projectilePrefab);
    }

}
