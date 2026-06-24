using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitGyro : MonoBehaviour
{

    [SerializeField]
    private Vector3 m_forwardVector;

    public Vector3 ForwardVector {
        get { return m_forwardVector; }
        set { m_forwardVector = value; }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateRotations();
    }

    private void UpdateRotations() 
    {
        transform.rotation = Quaternion.LookRotation(m_forwardVector);

    }
}
