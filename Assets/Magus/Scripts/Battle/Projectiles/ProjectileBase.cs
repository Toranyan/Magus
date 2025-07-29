using magus.chara;
using System;
using UnityEngine;

namespace magus.battle
{
    public class ProjectileBase : MonoBehaviour
    {

        [SerializeField]
        private float _damage;

		[SerializeField]
		private float _lifeTime = 10; 

		private Vector3 _vecVelocity;

		private bool _isAlive;
		private float _aliveTime;


		public event Action<ProjectileBase> Revived;
		public event Action<ProjectileBase> Killed;


		public void Revive()
		{
			_isAlive = true;
			gameObject.SetActive(true);
			_aliveTime = 0;
			Revived?.Invoke(this);
		}

		public void Kill()
		{
			gameObject.SetActive(false);
			Killed?.Invoke(this);
		}

		public void Setup(Vector3 initialVelocity)
		{
			_vecVelocity = initialVelocity;
		}

		private void Update()
		{

			if (_aliveTime >= _lifeTime)
			{
				Kill();
			}
			_aliveTime += Time.deltaTime;

			transform.position += Time.deltaTime * _vecVelocity;
		}

		private void OnCollisionEnter(Collision collision)
		{

			Debug.Log($"Collision : {name} x {collision.collider.name}");

			var chara = collision.gameObject.GetComponent<GameCharaController>();
			if (chara != null)
			{
				OnCollideWithChara(chara);
			}
		}

		private void OnCollideWithChara(GameCharaController chara)
		{
			//determine if same team
			//apply damage
			//delete

			if(!_isAlive)
			{
				return;
			}

		}


	}
}