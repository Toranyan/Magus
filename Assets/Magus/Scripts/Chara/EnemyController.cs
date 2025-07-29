using magus.battle;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace magus.chara
{
    public class EnemyController : MonoBehaviour
    {

		[SerializeField]
		private GameCharaController _charaController;

		private void Update()
		{
			var vecDelta = BattleController.Instance.PlayerController.transform.position - transform.position;

			_charaController.SetMoveVector(vecDelta);

		}

	}

}