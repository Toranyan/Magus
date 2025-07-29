using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace magus.chara
{
    public class GameCharaController : MonoBehaviour
    {
        [SerializeField]
        private ModelController _modelController;

        [SerializeField]
        private int _teamId;
        

        public int TeamId => _teamId;

        public void SetMoveVector(Vector3 moveVec)
		{
            _modelController.SetMoveVector(moveVec);
		}

        
    }
}