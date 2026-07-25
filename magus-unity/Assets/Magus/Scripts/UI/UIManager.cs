using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using tora.singleton;
using tora.ui;

namespace magus.ui
{

    public class UIManager : SingletonComponent<UIManager>
    {

        [SerializeField]
        private Canvas _mainCanvas;

        [SerializeField]
        private Transform _root;

        [SerializeField]
        private UIStateManager _stateManager;

        /// <summary>
        /// Views pre-instantiated in the scene (e.g. a test bootstrap scene) rather than
        /// loaded from Addressables. Registered into the same dictionary GetOrCreateView/
        /// GetOrLoadViewAsync use, keyed by each instance's concrete type, so callers don't
        /// need to know whether a view came from here or was created through code.
        /// </summary>
        [SerializeField]
        private List<UIViewBase> _startupViews;

        public Canvas MainCanvas => _mainCanvas;

        /// <summary>
        /// Views are addressed by AddressableAutoSetting's convention: address == path
        /// relative to Assets/Magus/Addressables/, no extension. View prefabs live under
        /// Assets/Magus/Addressables/UI/Views/, so a view type's address is
        /// "UI/Views/{type name}" (e.g. UITitle.prefab at Addressables/UI/Views/UITitle.prefab
        /// -> "UI/Views/UITitle").
        /// </summary>
        private const string ViewAddressPrefix = "UI/Views/";

		private readonly Dictionary<Type, UIViewBase> _views = new();
        private readonly Dictionary<Type, UniTask<UIViewBase>> _pendingLoads = new();

        private void Awake()
        {
            if (_startupViews == null)
                return;

            foreach (var view in _startupViews)
            {
                if (view == null)
                    continue;

                _views[view.GetType()] = view;
            }
        }

        /// <summary>Returns the cached instance for this view type, instantiating it under the UI root on first use.</summary>
        public T GetOrCreateView<T>(T prefab) where T : UIViewBase
        {
            if (_views.TryGetValue(typeof(T), out var existing))
                return (T)existing;

            var view = Instantiate(prefab, _root);
            _views[typeof(T)] = view;
            return view;
        }

        /// <summary>
        /// Returns the cached instance for this view type, loading it from Addressables
        /// on first use (see ViewAddressPrefix for the addressing convention). Concurrent
        /// calls for the same type share a single load rather than instantiating twice.
        /// </summary>
        public async UniTask<T> GetOrLoadViewAsync<T>() where T : UIViewBase
        {
            if (_views.TryGetValue(typeof(T), out var existing))
                return (T)existing;

            if (!_pendingLoads.TryGetValue(typeof(T), out var pending))
            {
                pending = LoadViewAsync<T>().Preserve();
                _pendingLoads[typeof(T)] = pending;
            }

            return (T)await pending;
        }

        private async UniTask<UIViewBase> LoadViewAsync<T>() where T : UIViewBase
        {
            var prefab = await Addressables.LoadAssetAsync<GameObject>(ViewAddressPrefix + typeof(T).Name);
            var view = Instantiate(prefab, _root).GetComponent<T>();
            _views[typeof(T)] = view;
            return view;
        }

        /// <summary>Returns the cached instance for this view type, or null if it has never been created.</summary>
        public T GetView<T>() where T : UIViewBase
        {
            return _views.TryGetValue(typeof(T), out var view) ? (T)view : null;
        }

        /// <summary>Instantiates (if needed) and opens the view for this prefab. Does not touch back-navigation history.</summary>
        public T Open<T>(T prefab) where T : UIViewBase
        {
            var view = GetOrCreateView(prefab);
            view.Open();
            return view;
        }

        /// <summary>Closes the cached instance for this view type, if one has been created. Does not touch back-navigation history.</summary>
        public void Close<T>() where T : UIViewBase
        {
            GetView<T>()?.Close();
        }

        /// <summary>
        /// Opens a view by type alone: cached instance if one exists, otherwise loads
        /// it from Addressables (see ViewAddressPrefix) and caches it. Does not touch
        /// back-navigation history - use PushView&lt;T&gt;() for that.
        /// </summary>
        public void OpenView<T>() where T : UIViewBase
        {
            OpenViewAsync<T>().Forget();
        }

        private async UniTaskVoid OpenViewAsync<T>() where T : UIViewBase
        {
            var view = await GetOrLoadViewAsync<T>();
            view.Open();
        }

        /// <summary>
        /// Pushes a view onto the navigation stack: opens it now, remembers what was
        /// active before it so PopView() can return to it. Use for back-navigable
        /// screens (Title -> Options -> back). If you don't already have a view
        /// instance, get/instantiate one first via GetOrCreateView.
        /// </summary>
        public void PushView(IUIView view)
        {
            _stateManager.Push(new ViewState(view, PopView));
        }

        /// <summary>
        /// Same as PushView(IUIView), but resolves the view by type alone: cached
        /// instance if one exists, otherwise loads it from Addressables (see
        /// ViewAddressPrefix) and caches it.
        /// </summary>
        public void PushView<T>() where T : UIViewBase
        {
            PushViewAsync<T>().Forget();
        }

        private async UniTaskVoid PushViewAsync<T>() where T : UIViewBase
        {
            var view = await GetOrLoadViewAsync<T>();
            PushView(view);
        }

        /// <summary>Returns to the state that was active before the last PushView()/PushState().</summary>
        public void PopView()
        {
            _stateManager.Pop();
        }

        /// <summary>
        /// Escape hatch for states with logic beyond open/close (e.g. TitleUIState,
        /// the navigation stack's root state). Most screens should use PushView instead.
        /// </summary>
        public void PushState(IUIState state)
        {
            _stateManager.Push(state);
        }

    }

}