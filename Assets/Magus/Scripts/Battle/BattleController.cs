using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tora.singleton;

using tora.fsm;

namespace App.Battle
{
    public class BattleController : SingletonComponent<BattleController>
    {




		private void Start()
		{
			//create fsm
			StateMachine fsm = new StateMachine();
		}

	}

}