using UnityEngine;
using System.Collections;

using System;
using System.Collections.Generic;

namespace tora.input {

	/// <summary>
	/// Transforms clicks to raycasts
	/// </summary>
	public class RaycastController : MonoBehaviour {


		[SerializeField]
		protected Camera m_raycastCamera;

		[SerializeField]
		protected LayerMask m_layerMask;

		[SerializeField]
		protected float m_maxDistance = 100000;

		[SerializeField]
		protected bool m_logEnabled = true;

		public event Action<RaycastHit> OnClickEvent;
		public event Action<RaycastHit> OnRightClickEvent;


		public void Update() {
			if (Input.GetMouseButtonDown (0)) {
				OnClick (0, Input.mousePosition);
			}
			if (Input.GetMouseButtonDown (1)) {
				OnClick (1, Input.mousePosition);
			}
		}

		public void OnClick(int button, Vector3 screenPoint) {

			//Raycast
			Ray ray = m_raycastCamera.ScreenPointToRay(screenPoint);
			RaycastHit hitInfo;
			if (Physics.Raycast (ray, out hitInfo, m_maxDistance, (int)m_layerMask)) {
				Debug.Log ("Hit");
				if (button == 0) { //left click
					if (OnClickEvent != null) {
						OnClickEvent (hitInfo);
					}
				} else if (button == 1) { //right click
					if (OnRightClickEvent != null) {
						OnRightClickEvent (hitInfo);
					}
				}
			} else {
				Debug.Log ("Nohit");

			}

		}



	}

}