using magus.battle;
using UnityEngine;
using UnityEngine.Pool;

namespace magus.ui
{

    public class DamageIndicatorManager : MonoBehaviour
    {
        [SerializeField]
        private DamageIndicator3d _damageIndicatorPrefab;

        [SerializeField]
        private Transform _indicatorContainer;

        private ObjectPool<DamageIndicator3d> _indicatorPool;

        public void Awake()
        {
            Initialize();
		}

		private void OnEnable()
		{
			DamageReceiver.GlobalDamageReceived += OnGlobalDamageReceived;
		}

        private void OnDisable()
        {
            DamageReceiver.GlobalDamageReceived -= OnGlobalDamageReceived;
		}


		public void Initialize()
        {
            _indicatorPool = new ObjectPool<DamageIndicator3d>(
                createFunc: CreateDamageIndicator,
                actionOnGet: indicator => indicator.gameObject.SetActive(true),
                actionOnRelease: indicator => indicator.gameObject.SetActive(false),
                actionOnDestroy: indicator => Destroy(indicator.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 100
            );
        }
        public DamageIndicator3d GetIndicator()
        {
            return _indicatorPool.Get();
        }
        public void ReleaseIndicator(DamageIndicator3d indicator)
        {
            _indicatorPool.Release(indicator);
        }


        public DamageIndicator3d CreateDamageIndicator()
        {
            var indicator = Instantiate(_damageIndicatorPrefab, _indicatorContainer);
            indicator.Finished += () => {
                ReleaseIndicator(indicator);
            };
			return indicator;
        }

        public void ShowIndicator(Vector3 worldPosition, float damageAmount, Color color, float size)
        {
            var indicator = GetIndicator();
            indicator.transform.position = worldPosition;

            indicator.Setup(damageAmount, color, size);
            indicator.StartAnimation();
		}

        public void HideIndicator(DamageIndicator3d indicator)
        {
            _indicatorPool.Release(indicator);
        }


        private void OnGlobalDamageReceived(DamageInfo damageInfo)
        {
			//TODO resolve color based on damage type

			//TODO resolve size based on damage amount

			ShowIndicator(damageInfo.HitPosition, damageInfo.Amount, Color.red, 1.5f);
		}


	}
}
