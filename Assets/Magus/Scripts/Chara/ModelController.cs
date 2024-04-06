using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace App.Chara
{
    public class ModelController : MonoBehaviour
    {
        [SerializeField]
        private CharacterController _characterController;

		[SerializeField]
		private Animator _animator;

		[SerializeField]
		private float _moveSpeed = 1;

		private Vector3 _moveVector;

		public float MoveSpeed
		{
			get { return _moveSpeed; }
			private set { _moveSpeed = value; }
		}

		private void Awake()
		{
			Init();
		}

		public void Init()
		{
			_moveVector = Vector3.zero;
			_animator.SetFloat("Speed", 2);
		}

		public void Update()
		{
			UpdatePosition(Time.deltaTime);

			UpdateRotation(Time.deltaTime);
		}

		public void SetMoveVector(Vector3 vector)
		{
			_moveVector = vector;

			var speed = vector.magnitude;
			_animator.SetFloat("Speed", speed);
		}

		public void UpdatePosition(float deltaTime)
		{
			//scale with movespeed
			var realMoveVec = MoveSpeed * deltaTime * _moveVector;

			//apply effects
			_characterController.Move(realMoveVec);
		}

		public void UpdateRotation(float deltaTime)
		{
			if (_moveVector.magnitude > 0)
			{
				_characterController.transform.rotation = Quaternion.LookRotation(_moveVector, Vector3.up);
			}
		}

	}
}