using UnityEngine;
using System.Collections;

using System.Collections.Generic;

namespace tora.unit {

	[RequireComponent(typeof(Rigidbody))]
	public class UnitController : MonoBehaviour {

		//Serialized Fields

		//unit params

		[SerializeField]
		protected float m_speed = 10;

		[SerializeField]
		public float m_moveThreshold = 1;

		//Protected fields

		//Rigidbody cache
		protected Rigidbody m_rigidbody;
		//
		[SerializeField]
		public Vector3 m_moveTarget;

		[SerializeField]
		public Vector3 m_vecMove;


		//Properties

		public Rigidbody Rigidbody {
			get {
				if (m_rigidbody == null) {
					m_rigidbody = GetComponent<Rigidbody> ();
				}
				return m_rigidbody;
			}
		}


		//Public Functions


		/// <summary>
		/// Send a move command to the unit to move to target location
		/// </summary>
		/// <param name="vecTarget">Vec target.</param>
		public void CommandMove(Vector3 vecTarget) {
			m_moveTarget = vecTarget;
		}

		//Protected Functions

		protected void Update() {
			//check commands
			ProcessCommands();

			//update AI

			//update model

		}

		protected void ProcessCommands() {

			if (Vector3.Distance (m_moveTarget, transform.position) > m_moveThreshold) {
				m_vecMove = m_moveTarget - transform.position;
			}



		}


		protected void UpdateAI() {

		}

		protected void UpdateModel() {

		}

		protected void FixedUpdate() {
			//calc final velocity
			Vector3 vecFinal = m_vecMove * m_speed;
			this.Rigidbody.linearVelocity = vecFinal;
		}

	}


}