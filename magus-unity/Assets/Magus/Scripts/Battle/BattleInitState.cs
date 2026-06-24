using tora.fsm;
using UnityEngine;


namespace magus.battle
{
	public class BattleInitState : IState
	{

		public class InitOptions
		{

		}

		public void Setup(InitOptions options)
		{

		}

		public void Init(StateMachine fsm)
		{
			//Spawn player
			//Init map
			//init enemies
			//init conditions


		}

		public void OnEnter(IState prevState)
		{
			throw new System.NotImplementedException();
		}

		public void OnExit(IState nextState)
		{
			throw new System.NotImplementedException();
		}

		public void OnUpdate()
		{
			throw new System.NotImplementedException();
		}
	}
}