using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickDetector : MonoBehaviour
{
    [SerializeField]
    private Camera m_projectionCamera;

    [SerializeField]
    private string m_clickableLayer;

    [SerializeField]
    private Collider m_clickableCollider;

    [SerializeField]
    private GameObject m_clickIndicator;

    private Vector3 m_lastClickedPos;

    public event Action<GameObject, Vector3> ClickDetected;


    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)) {
            //check if collided
            m_lastClickedPos = Input.mousePosition;
            Ray ray = m_projectionCamera.ScreenPointToRay(Input.mousePosition);
            Collider collider = m_clickableCollider;
            Vector3 intersectPos;

            if(GetRayColliderIntersect(ray, collider, out intersectPos)) {
                OnClickDetected(collider.gameObject, intersectPos);
            }
        }

        if(Input.GetMouseButtonUp(0)) {

        }

        //Ray ray = m_projectionCamera.ScreenPointToRay(m_lastClickedPos);
        //Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
    }

    private void OnClickDetected(GameObject clickedGameObject, Vector3 clickPos) {

        ClickDetected.Invoke(clickedGameObject, clickPos);

        //GameObject.Instantiate(m_clickIndicatorPrefab);
        //m_clickIndicatorPrefab
    } 


    /// <summary>
    /// Gets Collision between a ray and collider
    /// </summary>
    /// <param name="ray"></param>
    /// <param name="collider"></param>
    /// <param name="intersectPos"></param>
    /// <param name="maxDist"></param>
    /// <returns></returns>
    public bool GetRayColliderIntersect(Ray ray, Collider collider, out Vector3 intersectPos, float maxDist = 10000) {

        intersectPos = Vector3.zero;

        RaycastHit hitInfo;
        collider.Raycast(ray, out hitInfo, maxDist);

        return false;
    }


}
