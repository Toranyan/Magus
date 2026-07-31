using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using magus.battle;
using UnityEngine;

namespace magus.chara
{
    public class ModelController : MonoBehaviour
    {
        [SerializeField]
        private CharacterController _characterController;

		[SerializeField]
		private Animator _animator;

		[SerializeField]
		private float _moveSpeed = 1;

		[SerializeField]
		private bool _isAffectedByGravity = true;

		[SerializeField]
		private float _gravity = -9.8f;

		[SerializeField]
		private float _terminalVelocity = -50;

		[SerializeField]
		private bool _ragdollOnDeath = true;

		[Tooltip("Useful for rough ground")]
		public float _groundedOffset = -0.14f;

		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float _groundedRadius = 0.28f;

		[Tooltip("What layers the character uses as ground")]
		public LayerMask _groundLayers;

		[Tooltip("Effect id spawned on death (e.g. an explosion). Left empty if the unit has none.")]
		[SerializeField]
		private string _deathEffectId;

		private static readonly int DeathStateHash = Animator.StringToHash("Death");

		private Vector3 _moveVector;
		private Unit _unit;

		public event Action DeathAnimationFinished;

		/// <summary>Fired when the death sequence starts so external services (e.g. EffectManager)
		/// can spawn the effect. Gameplay code should not directly spawn visual effects.</summary>
		public event Action<string, Vector3> DeathEffectRequested;

		public bool IsGrounded { get; private set; }

		private float _verticalVelocity;

		// Resolved through the owning Unit's Modifiers (e.g. Haste) when one has been
		// assigned via SetOwner; falls back to the raw inspector value otherwise.
		public float MoveSpeed
		{
			get
			{
				return _unit != null
					? _unit.Modifiers.Resolve(ModifierType.MoveSpeed, _moveSpeed)
					: _moveSpeed;
			}
			private set { _moveSpeed = value; }
		}

		public void SetOwner(Unit unit)
		{
			_unit = unit;
		}

		public void SetBaseMoveSpeed(float moveSpeed)
		{
			_moveSpeed = moveSpeed;
		}

		private void Awake()
		{
			Init();
		}

		public void Init()
		{
			_moveVector = Vector3.zero;
			_animator.SetFloat("Speed", 2);
			_animator.SetBool("Alive", true);
		}

		public void Setup()
		{
			_animator.gameObject.SetActive(true);
			_animator.enabled = true;
			_characterController.enabled = true;
		}

		public void Update()
		{
			JumpAndGravity();
			GroundedCheck();
			UpdatePosition(Time.deltaTime);
			UpdateRotation(Time.deltaTime);
		}

		public void SetMoveVector(Vector3 vector)
		{
			_moveVector.x = vector.x;
			_moveVector.z = vector.z;
			_moveVector.y = 0;

			var speed = vector.magnitude;
			_animator.SetFloat("Speed", speed);
		}

		public void UpdatePosition(float deltaTime)
		{
			//scale with movespeed
			var realMoveVec = ((MoveSpeed * _moveVector) + new Vector3(0, _verticalVelocity, 0)) * deltaTime;

			//apply effects
			if (_characterController.enabled)
			{
				_characterController.Move(realMoveVec);
			}
				
		}

		public void UpdateRotation(float deltaTime)
		{
			if (_moveVector.magnitude > 0)
			{
				_characterController.transform.rotation = Quaternion.LookRotation(_moveVector, Vector3.up);
			}
		}

		private void JumpAndGravity()
		{
			if (IsGrounded)
			{
				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}
			} else
			{

			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity > _terminalVelocity)
			{
				_verticalVelocity += _gravity * Time.deltaTime;
			}
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _groundedOffset,
				transform.position.z);
			IsGrounded = Physics.CheckSphere(spherePosition, _groundedRadius, _groundLayers,
				QueryTriggerInteraction.Ignore);

		}

		public void StartDeathAnimation()
		{
			_characterController.enabled = false;

			if (_ragdollOnDeath)
			{
				Ragdoll().Forget();
			}
			else if (_animator.HasState(0, DeathStateHash))
			{
				DeathAnimation().Forget();
			}
			else
			{
				// No ragdoll and no death animation to play - just hide the model immediately.
				_animator.gameObject.SetActive(false);
				OnDeathAnimationFinished();
			}

			if (!string.IsNullOrEmpty(_deathEffectId))
			{
				DeathEffectRequested?.Invoke(_deathEffectId, transform.position);
			}
		}

		public async UniTask Ragdoll()
		{
			_animator.enabled = false;
			await UniTask.Delay(TimeSpan.FromSeconds(3));
			OnDeathAnimationFinished();
		}

		private async UniTask DeathAnimation()
		{
			//
			_animator.SetBool("Alive", false);
			AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

			var animationStartTime = Time.time;
			var maxDeathAnimTime = 3;
			
			do
			{
				// Check if we're in the "Death" state and if it's finished
				if (stateInfo.IsName("Death") && stateInfo.normalizedTime >= 1f)
				{
					Debug.Log("Death animation finished!");
					break;
				}
				await UniTask.WaitForEndOfFrame(this);
			} while (Time.time - animationStartTime < maxDeathAnimTime);

			OnDeathAnimationFinished();
		}

		private void OnDeathAnimationFinished()
		{
			DeathAnimationFinished?.Invoke();
		}

	}
}