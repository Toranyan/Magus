using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A unit locked to a plane
/// </summary>
public class PlaneUnit : MonoBehaviour
{

    [SerializeField]
    private float movSpeed = 10;

    [SerializeField]
    private Weapon mainWeapon;

    
    public void Move(Vector3 mov) {
        //var newPos = transform.localPosition + mov;

        mov.Normalize();


        transform.localPosition += mov * movSpeed * Time.deltaTime;
    }




}
