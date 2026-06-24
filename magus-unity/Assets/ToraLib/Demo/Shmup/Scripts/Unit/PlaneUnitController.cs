using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneUnitController : MonoBehaviour
{
    [SerializeField]
    private PlaneUnit _unit;

    [SerializeField]
    private GameObject _plane;

    [SerializeField]
    private Camera _camera;

    // Update is called once per frame
    void Update()
    {
        UpdateMovement();
    }

    private void UpdateMovement() {
        var vert = Input.GetAxis("Vertical");
        var hor = Input.GetAxis("Horizontal");

        Vector3 vecMovment = new Vector3(hor, vert, 0);
        var planeMovement = ProjectVectorOnPlane(vecMovment);

        _unit.Move(planeMovement);
    }

    private Vector3 ProjectVectorOnPlane(Vector3 vector) {
        var vecCam = _camera.transform.rotation * vector;
        var planeZ = _plane.transform.rotation * Vector3.forward;
        var planeX = _plane.transform.rotation * Vector3.right;
        var x = Vector3.Dot(vecCam, planeX);
        var z = Vector3.Dot(vecCam, planeZ);

        return new Vector3(x, 0, z);
    }

}
