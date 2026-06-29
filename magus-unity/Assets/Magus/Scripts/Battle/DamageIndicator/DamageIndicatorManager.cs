using magus.battle;
using UnityEngine;
using UnityEngine.Pool;

namespace magus.ui
{

    public class DamageIndicatorManager : MonoBehaviour
    {
        [SerializeField]
        private DamageIndicator _damageIndicatorPrefab;

        [SerializeField]
        private Transform _indicatorContainer;

        [SerializeField]
        private Canvas _canvas;

        private ObjectPool<DamageIndicator> _indicatorPool;

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
            _indicatorPool = new ObjectPool<DamageIndicator>(
                createFunc: CreateDamageIndicator,
                actionOnGet: indicator => indicator.gameObject.SetActive(true),
                actionOnRelease: indicator => indicator.gameObject.SetActive(false),
                actionOnDestroy: indicator => Destroy(indicator.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 100
            );
        }
        public DamageIndicator GetIndicator()
        {
            return _indicatorPool.Get();
        }
        public void ReleaseIndicator(DamageIndicator indicator)
        {
            _indicatorPool.Release(indicator);
        }


        public DamageIndicator CreateDamageIndicator()
        {
            var indicator = Instantiate(_damageIndicatorPrefab, transform);
            indicator.transform.SetParent(_indicatorContainer);
            indicator.Finished += () => {
                ReleaseIndicator(indicator);
            };
			return indicator;
        }

        public void ShowIndicator(Vector3 position, float damageAmount, Color color, float size)
        {
            var indicator = GetIndicator();
            indicator.transform.localPosition = position;

            indicator.Setup(damageAmount, color, size);
            indicator.StartAnimation();
		}

        public void HideIndicator(DamageIndicator indicator)
        {
            _indicatorPool.Release(indicator);
        }


        private void OnGlobalDamageReceived(DamageInfo damageInfo)
        {
			//Transform world position to screen position
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(damageInfo.HitPosition);

			//transform screen position to UI position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _indicatorContainer as RectTransform,
                screenPosition,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : _canvas.worldCamera,
                out Vector2 uiPosition
            );

			//TODO resolve color based on damage type

			//TODO resolve size based on damage amount

			ShowIndicator(uiPosition, damageInfo.Amount, Color.red, 24f);
		}


	}
}
