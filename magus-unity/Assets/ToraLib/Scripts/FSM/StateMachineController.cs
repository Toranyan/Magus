using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tora.fsm {

    public class StateMachineController : MonoBehaviour {

        private StateMachine _stateMachine;

		public void Setup(StateMachine stateMachine) {
			_stateMachine = stateMachine;
		}

		private void Update() {
			if (_stateMachine != null) {
				_stateMachine.Update();
			}
		}

	}

}