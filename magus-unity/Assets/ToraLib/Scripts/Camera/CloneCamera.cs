using UnityEngine;
using System.Collections;

namespace tora.camera {

	/// <summary>
	/// Clone camera.
	/// Copies another camera
	/// </summary>
	public class CloneCamera : MonoBehaviour {

		public Camera targetCamera;

		public float scale;
		
		// Update is called once per frame
		void Update () {

			transform.localRotation = targetCamera.transform.localRotation;
			transform.localPosition = targetCamera.transform.localPosition * scale;
		
		}
	}


}