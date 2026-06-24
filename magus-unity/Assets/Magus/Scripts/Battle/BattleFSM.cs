using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tora.fsm;

namespace App.Battle
{
    public class BattleFSM : StateMachine
    {
        public enum BattleState
		{
            Init,
            Battle,
            Result,
		}

        public void ChangeState(BattleState state)
		{


		}
    }
}