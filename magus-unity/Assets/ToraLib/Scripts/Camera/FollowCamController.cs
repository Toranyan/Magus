using UnityEngine;
using System.Collections;

namespace tora.camera {

	public class FollowCamController : MonoBehaviour {

		/****************************************
		 * Inspector Fields
		 * **************************************/

		[SerializeField]
		protected FollowCamera m_camera;
		[SerializeField]
		protected Vector3 m_rotateRate;
		[SerializeField]
		protected float m_zoomRate;
		[SerializeField]
		protected float m_zoomMin;
		[SerializeField]
		protected float m_zoomMax;


		[SerializeField]
		protected KeyCode m_rotHorizontalPositiveKey = KeyCode.RightArrow;
		[SerializeField]
		protected KeyCode m_rotHorizontalNegativeKey = KeyCode.LeftArrow;
		[SerializeField]
		protected KeyCode m_rotVerticalPositiveKey = KeyCode.UpArrow;
		[SerializeField]
		protected KeyCode m_rotVerticalNegativeKey = KeyCode.DownArrow;
		[SerializeField]
		protected KeyCode m_zoomInKey = KeyCode.KeypadPlus;
		[SerializeField]
		protected KeyCode m_zoomOutKey = KeyCode.KeypadMinus;


		[SerializeField]
		protected float m_mouseSensitivity = 0.1f;


		protected Vector3 m_mousePos;


		/****************************************
		 * Monobehavior Methods
		 * **************************************/

		// Use this for initialization
		public void Awake () {

			//self?
			if(m_camera == null) {
				m_camera = GetComponent<FollowCamera>();
			}

			if(m_camera == null) {
				m_camera = Camera.main.GetComponent<FollowCamera>();
			}
			if(m_camera == null) {
				Debug.LogWarning("Camera not found");
				return;
			}

			//init mouse pos
			m_mousePos = Input.mousePosition;


		}
		
		// Update is called once per frame
		public void Update () {
			UpdateInput();
		}

		/****************************************
		 * Protected Methods
		 * **************************************/

		protected void UpdateInput() {

			if(m_camera == null) {
				return;
			}

			Vector3 vecCamera = Vector3.zero;

			//vecCamera.y += Input.GetAxis("Horizontal2");
			//vecCamera.x += Input.GetAxis("Vertical2");
			vecCamera.y += Input.GetKey(m_rotHorizontalPositiveKey) ? 1 : 0;
			vecCamera.y -= Input.GetKey(m_rotHorizontalNegativeKey) ? 1 : 0;
			vecCamera.x += Input.GetKey(m_rotVerticalPositiveKey) ? 1 : 0;
			vecCamera.x -= Input.GetKey(m_rotVerticalNegativeKey) ? 1 : 0;

			//mouse
			//Input.mousePosition
			Vector3 vecDeltaMouse = Input.mousePosition - m_mousePos;
			m_mousePos = Input.mousePosition;
			vecDeltaMouse *= m_mouseSensitivity;
			//flip
			vecCamera.x -= vecDeltaMouse.y;
			vecCamera.y += vecDeltaMouse.x;


			//by rate
			vecCamera = Vector3.Scale(vecCamera, m_rotateRate) * Time.deltaTime;

			m_camera.RotateOffset(vecCamera);


			//zoom
			Vector3 posOffset = m_camera.VecPosOffset;
			float zoom = 0;
			zoom += Input.GetKey(m_zoomInKey) ? 1 : 0;
			zoom -= Input.GetKey(m_zoomOutKey) ? 1 : 0;
			zoom = zoom * m_zoomRate * Time.deltaTime;
			//add to offset
			posOffset.z += zoom;

			//limit
			if(m_zoomMin != 0) {
				posOffset.z = Mathf.Clamp(posOffset.z, m_zoomMin, m_zoomMax);
			}


			m_camera.VecPosOffset = posOffset;

		}

	}

}
