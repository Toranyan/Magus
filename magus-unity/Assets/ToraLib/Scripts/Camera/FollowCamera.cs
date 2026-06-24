using UnityEngine;
using System.Collections;

namespace tora.camera {

	[ExecuteInEditMode]
	public class FollowCamera : MonoBehaviour {
		
		/****************************************
		 * Inspector Fields
		 * **************************************/
		[SerializeField]
		protected bool m_enabled = true;

		[SerializeField]
		protected bool m_enableFollow = true;

		[SerializeField]
		protected bool m_enableLook = true;

		[SerializeField]
		protected eRunMode m_runMode = eRunMode.LateUpdate;
		
		[SerializeField]
		protected GameObject m_followTarget;
		[SerializeField]
		protected GameObject m_lookTarget;
		[SerializeField]
		protected GameObject m_autorotateTarget;
		
		[SerializeField]
		protected Vector3 m_vecFollowOffset; //follow point offset from target
		[SerializeField]
		protected Vector3 m_vecPosOffset = new Vector3(0, 10, -10); //camera offset from follow point
		[SerializeField]
		protected Vector3 m_vecPosRot; //rotation around follow point
		[SerializeField]
		protected Vector3 m_vecLookOffset; //look point offset from follow point
		
		[SerializeField]
		protected bool m_bRelativeOffset; //offset is relative to target rotation
		[SerializeField]
		protected bool m_bScaleOffset; //offsets will scale with target scale
		
		//rotation
		//public float m_rotRate;
		[SerializeField]
		protected Vector3 m_vecRotRate;
		[SerializeField]
		protected Vector3 m_vecRotMin; //limit rotation; 0 = no limit
		[SerializeField]
		protected Vector3 m_vecRotMax;
		
		[SerializeField]
		protected bool m_bFollow;
		
		[SerializeField]
		protected bool m_bBufferMap; //obstacle buffering
		[SerializeField]
		protected LayerMask m_mapLayer;
		[SerializeField]
		protected float m_bufferDistance; //disatnce to buffer
		
		[SerializeField]
		protected bool m_bDampFollow;
		[SerializeField]
		protected bool m_bDampLook;
		[SerializeField]
		protected bool m_bDampRotate;
		
		//public float m_damp;
		[SerializeField]
		protected float m_dampFollow = 1;
		[SerializeField]
		protected float m_dampLook;
		[SerializeField]
		protected float m_dampRotate;
		[SerializeField]
		protected float m_maxVelo = 1000;
		
		[SerializeField]
		protected bool m_bAutoRotate;
		
		
		/****************************************
		 * Protected Fields
		 * **************************************/
		

		protected Vector3 m_vecFollowTarget;
		protected Vector3 m_vecLookTarget;


		//damping
		protected Vector3 m_vecDampTarget; //current damped target position
		protected Vector3 m_vecDampLook;
		
		protected Vector3 m_vecDampRotate;
		
		protected Vector3 m_vecDampVelo; //used by damping function
		protected Vector3 m_vecDampLookVelo; 
		protected Vector3 m_vecDampRotateVelo; 



		
		
		/****************************************
		 * Properties
		 * **************************************/

		public bool Enabled {
			get { return m_enabled; }
			set { m_enabled = value; }
		}

		public GameObject FollowTarget {
			get { return m_followTarget; }
			set { m_followTarget = value; }
		}

		public GameObject LookTarget {
			get { return m_lookTarget; }
			set { m_lookTarget = value; }
		}

		public Vector3 VecFollowOffset {
			get { return m_vecFollowOffset; }
			set { m_vecFollowOffset = value; }
		}

		public Vector3 VecPosOffset {
			get { return m_vecPosOffset; }
			set { m_vecPosOffset = value; }
		}

		public Vector3 VecLookOffset {
			get { return m_vecLookOffset; }
			set { m_vecLookOffset = value; }
		}


		//damping
		public bool DampFollow {
			get { return m_bDampFollow; }
			set { m_bDampFollow = value; }
		}

		public bool DampLook {
			get { return m_bDampLook; }
			set { m_bDampLook = value; }
		}

		
		/****************************************
		 * Public Methods
		 * **************************************/

		// Use this for initialization
		public void Start () {
			
			m_vecDampVelo = Vector3.zero;
			m_vecDampLookVelo = Vector3.zero;
			
			m_vecDampTarget = transform.position;
			//m_vecDampLook = 

		}
		
		[ExecuteInEditMode]
		public void Update() {
			if(m_runMode == eRunMode.Update) {
				Run ();
			}
		}

		[ExecuteInEditMode]
		public void FixedUpdate() {
			if(m_runMode == eRunMode.FixedUpdate) {
				Run ();
			}
		}

		[ExecuteInEditMode]
		public void LateUpdate() {
			if(m_runMode == eRunMode.LateUpdate) {
				Run ();
			}
		}

		public void OnDrawGizmos() {

			Gizmos.color = Color.green;
			Gizmos.DrawLine(transform.position, m_vecFollowTarget);
			Gizmos.DrawSphere(m_vecFollowTarget, 0.1f);

			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(transform.position, m_vecLookTarget);
			Gizmos.DrawSphere(m_vecLookTarget, 0.1f);

		}


		public void Init() {

		}
		
		public void Run() {
			//if(m_bFollow && m_followTarget) {
			if(!m_enabled) {
				return;	
			}
				
				//determine position
				
				Vector3 vecFinalPos;// = m_followTarget.transform.position;
				//Vector3 vecFinalLook;// = m_lookTarget.transform.position;
				//Vector3 vecFinalRot;
				//Quaternion qFinalRot;
				
				//Vector3 m_vecFollowTarget;// = m_followTarget.transform.position;
				//Vector3 m_vecLookTarget;// = m_lookTarget.transform.position;
				Vector3 vecRotTarget;
				
				Vector3 vecPosOffset = m_vecPosOffset;
				Vector3 vecLookOffset = m_vecLookOffset;
				//Vector3 vecRotOffset = m_vecPosRot;

			GameObject followTarget = m_followTarget;
			GameObject lookTarget = m_lookTarget;

			//default targets
			if(lookTarget == null) {
				lookTarget = followTarget;
			}


			//the delta time
			float deltaTime = 0;
			if(m_runMode == eRunMode.FixedUpdate) {
				deltaTime = Time.fixedDeltaTime;
			} else if(m_runMode == eRunMode.Update ||
			          m_runMode == eRunMode.LateUpdate) {
				deltaTime = Time.deltaTime;
			}

				
			//Follow Target
			if (m_enableFollow) {
				if (m_followTarget != null) {
					m_vecFollowTarget = m_followTarget.transform.position + m_vecFollowOffset;
				} else {
					m_vecFollowTarget = Vector3.zero + m_vecFollowOffset;
				}
				//damping
				if (m_bDampFollow) { //&& Application.platform != RuntimePlatform.WindowsEditor) {
					m_vecDampTarget = Vector3.SmoothDamp(m_vecDampTarget, m_vecFollowTarget, ref m_vecDampVelo, m_dampFollow, m_maxVelo, deltaTime);

					//change follow vector
					m_vecFollowTarget = m_vecDampTarget;
				} else {
					//update the damp target in case 
					m_vecDampTarget = m_vecFollowTarget;
				}
			}
			
			if (m_enableLook) {
				//Look Target
				if (lookTarget != null) {
					m_vecLookTarget = lookTarget.transform.position;
				} else { // use same as follow target
					m_vecLookTarget = Vector3.zero;
				}
			}
			
				
			//damping
			if(m_bDampLook) {
				m_vecDampLook = Vector3.SmoothDamp(m_vecDampLook, m_vecLookTarget, ref m_vecDampLookVelo, m_dampLook, m_maxVelo, deltaTime);
				
				//change look vector
				m_vecLookTarget = m_vecDampLook;
			} else {
				//just update the damp target
				m_vecDampLook = m_vecLookTarget;
			}
			
			//OFFSETS
			vecPosOffset = m_vecPosOffset;
			vecLookOffset = m_vecLookOffset;
		
			//Scale
			if(m_bScaleOffset) {
				if(m_followTarget != null) {
					vecPosOffset.x *= m_followTarget.transform.localScale.x;
					vecPosOffset.y *= m_followTarget.transform.localScale.y;
					vecPosOffset.z *= m_followTarget.transform.localScale.z;
				}
				if(lookTarget != null) {
					vecLookOffset.x *= lookTarget.transform.localScale.x;
					vecLookOffset.y *= lookTarget.transform.localScale.y;
					vecLookOffset.z *= lookTarget.transform.localScale.z;
				}
			}
			
			//ROTATION
			
			//vecRotTarget = m_vecPosRot;
			
			//autorotation
			if(m_bAutoRotate && m_autorotateTarget != null) {
				
				Vector3 vecAutoDelta = m_autorotateTarget.transform.position - m_vecFollowTarget;
				vecAutoDelta.Normalize();
				
				//vecRotTarget = Quaternion.LookRotation(vecAutoDelta).eulerAngles;
				//vecRotTarget = Quaternion.FromToRotation(Vector3.forward, vecAutoDelta).eulerAngles;
				vecRotTarget = vecAutoDelta;
				
				//update pos rot for when camera leaves autorotate
				m_vecPosRot = Quaternion.LookRotation(vecRotTarget).eulerAngles;
				
				//limit
				if(m_vecRotMin.x != 0) {
					//reduce
					while (m_vecPosRot.x > 180) {
						m_vecPosRot.x -= 360;	
					}
					m_vecPosRot.x = Mathf.Clamp(m_vecPosRot.x, m_vecRotMin.x, m_vecRotMax.x);
				}
				if(m_vecRotMin.y != 0) {
					m_vecPosRot.y = Mathf.Clamp(m_vecPosRot.y, m_vecRotMin.y, m_vecRotMax.y);
				}
				
			} 
			
			
			vecRotTarget = Quaternion.Euler(m_vecPosRot) * Vector3.forward;
			
			
			
			//damping
			if(m_bDampRotate) {
				//m_vecDampLook = Vector3.SmoothDamp(m_vecDampLook, vecLookTarget, ref m_vecDampLookVelo, m_damp, m_maxVelo, deltaTime);
				m_vecDampRotate = Vector3.SmoothDamp(m_vecDampRotate, vecRotTarget, ref m_vecDampRotateVelo, m_dampRotate, m_maxVelo, deltaTime);
				
				//change look vector
				vecRotTarget = m_vecDampRotate;
				
			} else {
				//just update the damp target
				m_vecDampRotate = vecRotTarget;
			}
			
			//rotate the pos offset
			vecPosOffset = Quaternion.LookRotation(vecRotTarget) * vecPosOffset;
			//vecPosOffset = Quaternion.FromToRotation(Vector3.forward, vecRotTarget) * vecPosOffset;
			
			//relative rotation
			if(m_bRelativeOffset && m_followTarget != null) {
				vecPosOffset = m_followTarget.transform.rotation * vecPosOffset;
				//vecPosOffset *= m_followTarget.transform.localScale.
			}
			
			//rotate look offset
			//vecLookOffset = Quaternion.Euler(m_vecLookRot) *  vecLookOffset;
			if(m_bRelativeOffset && lookTarget != null) {
				vecLookOffset = lookTarget.transform.rotation * vecLookOffset;
			}
			
			//final look
			//vecFinalLook = m_vecLookTarget + vecLookOffset;
			m_vecLookTarget = m_vecLookTarget + vecLookOffset;
			
			//calc final pos
			vecFinalPos = m_vecFollowTarget + vecPosOffset;
			
			
			//Obstacle buffering
			if(m_bBufferMap) {
				RaycastHit hit;
				int mask = m_mapLayer;
				Vector3 rayStart = m_vecLookTarget;
				Vector3 vecLookDelta = vecFinalPos - m_vecLookTarget;
				float dist = Vector3.Distance(vecFinalPos, m_vecLookTarget) + m_bufferDistance;
				
				if(Physics.Raycast(rayStart, vecLookDelta.normalized,out hit, dist, mask)) {
					//Debug.Log("Cam Buffer");
					//get the delta
					//Vector3 vecBufferDelta = vecFinalLook - hit.point;
					
					//calc buffered position
					Vector3 vecBufferedPos = hit.point - vecLookDelta.normalized * m_bufferDistance;
					
					//change final position
					vecFinalPos = vecBufferedPos;
					
					//position
					//transform.position = vecFinalPos;
				}
			}


			//position
			if (m_enableFollow) {
				transform.position = vecFinalPos;
			}
			
			//look at
			Vector3 vecUp = Vector3.up;
			if(m_bRelativeOffset && m_followTarget) {
				vecUp = m_followTarget.transform.rotation * vecUp;
			}
			transform.LookAt(m_vecLookTarget, vecUp);
			
			
			//}
		}

		public void RotateOffset(Vector3 vec) {
			//m_vecPosRot += vec * m_rotRate * Time.deltaTime;	
			m_vecPosRot += vec;
			
			//limit
			if(m_vecRotMin.x != 0 || m_vecRotMax.x != 0) {
				//reduce
				while (m_vecPosRot.x > 180) {
					m_vecPosRot.x -= 360;	
				}
				m_vecPosRot.x = Mathf.Clamp(m_vecPosRot.x, m_vecRotMin.x, m_vecRotMax.x);
			}
			if(m_vecRotMin.y != 0 || m_vecRotMax.y != 0) {
				m_vecPosRot.y = Mathf.Clamp(m_vecPosRot.y, m_vecRotMin.y, m_vecRotMax.y);
			}
		}





		/*
		public void RotateOffsetRate(Vector3 vec) {
			
			//m_vecPosRot += vec * m_rotRate * Time.deltaTime;	
			m_vecPosRot += Vector3.Scale(vec, m_vecRotRate) * Time.deltaTime;
			
			//limit
			if(m_vecRotLimit.x > 0) {
				//reduce
				while (m_vecPosRot.x > 180) {
					m_vecPosRot.x -= 360;	
				}
				m_vecPosRot.x = Mathf.Clamp(m_vecPosRot.x, -m_vecRotLimit.x, m_vecRotLimit.x);
			}
			if(m_vecRotLimit.y > 0) {
				m_vecPosRot.y = Mathf.Clamp(m_vecPosRot.y, -m_vecRotLimit.y, m_vecRotLimit.y);
			}
			
		}*/


		//public void 



		
		public enum eRunMode {
			Update,
			LateUpdate,
			FixedUpdate,
		}
		
	}

} //namespace
