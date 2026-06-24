using UnityEngine;
using System.Collections;

namespace tora.fsm {

	public abstract class State : IState {

		protected StateMachine _fsm;

		public virtual void Init(StateMachine fsm) {

		}

		public virtual void OnEnter(IState prevState) {

		}

		public virtual void OnExit(IState nextState) {

		}

		public virtual void OnUpdate() {

		}

	}

}