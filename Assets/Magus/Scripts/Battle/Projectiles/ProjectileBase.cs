using Cysharp.Threading.Tasks;
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

		[SerializeField]
		private string _deathEffectId;

		[SerializeField]
		private bool _killOnCollision = true;

		private Vector3 _vecVelocity;

		private bool _isAlive;
		private float _aliveTime;

		public IBattleEntity Owner { get; private set; }

		// Expose the kill-on-collision flag
		public bool IsKillOnCollide
		{
			get => _killOnCollision;
			set => _killOnCollision = value;
		}

		public event Action<ProjectileBase> Killed;

		public void Setup(Vector3 initialVelocity)
		{
			_vecVelocity = initialVelocity;
			_isAlive = true;
			_aliveTime = 0;
		}

		public void ClearEvents()
		{
			Killed = null;
		}

		public void Kill()
		{
			_isAlive = false;
			gameObject.SetActive(false);

			CreateDeathEffect();

			Killed?.Invoke(this);
		}

		

		public void SetOwner(IBattleEntity owner)
		{
			Owner = owner;
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

		private void OnTriggerEnter(Collider other)
		{
			var obj = other.gameObject.GetComponent<DamageReceiver>();
			if (obj != null)
			{
				OnCollideDamageReceiver(obj);
			}
		}

		private void OnCollideDamageReceiver(DamageReceiver obj)
		{
			//determine if same team
			//apply damage
			//delete
			if(!_isAlive)
			{
				return;
			}
			if (obj.TeamId == Owner.TeamId)
			{
				return;
			}
			Debug.Log($"Collision : {name} x {obj.name}");

			obj.Damage(new DamageInfo(
				amount: _damage,
				source: Owner,
				receiver: obj.GetComponent<IBattleEntity>(),
				location: transform.position,
				type: DamageType.Physical
			));

			if (_killOnCollision)
			{
				Kill();
			}
		}

		private void CreateDeathEffect()
		{
			BattleController.Instance.EffectManager.CreateEffect(_deathEffectId, transform.position).Forget();
		}

	}
}