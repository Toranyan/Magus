using System;
using UnityEngine;

namespace magus.battle
{

	[RequireComponent(typeof(PoolableHandler))]
    public class ExplosionEffect : MonoBehaviour
    {

		private PoolableHandler _poolableHandler;

		private double _particleDuration;

		private double _currentLifeTime;


		public event Action<ExplosionEffect> Killed;

		private void Awake()
		{
			_poolableHandler = GetComponent<PoolableHandler>();

			var particle = GetComponentInChildren<ParticleSystem>();
			_particleDuration = particle.main.duration;
		}

		private void OnEnable()
		{
			_currentLifeTime = 0;
		}

		public void Reset()
		{
			Killed = null;
		}

		private void Update()
		{
			_currentLifeTime += Time.deltaTime;
			if (_currentLifeTime > _particleDuration)
			{
				Kill();
			}
		}

		public void Kill()
		{
			OnKilled();
		}


		private void OnKilled()
		{
			Killed?.Invoke(this);
			_poolableHandler.ReturnToPool();
		}

	}

}