using System;
using System.Collections.Generic;
using UnityEngine;
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

        public Canvas MainCanvas => _mainCanvas;

        private readonly Dictionary<Type, UIViewBase> _views = new();

        /// <summary>Returns the cached instance for this view type, instantiating it under the UI root on first use.</summary>
        public T GetOrCreateView<T>(T prefab) where T : UIViewBase
        {
            if (_views.TryGetValue(typeof(T), out var existing))
                return (T)existing;

            var view = Instantiate(prefab, _root);
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
        /// Pushes a view onto the navigation stack: opens it now, remembers what was
        /// active before it so PopView() can return to it. Use for back-navigable
        /// screens (Title -> Options -> back). If you don't already have a view
        /// instance, get/instantiate one first via GetOrCreateView.
        /// </summary>
        public void PushView(IUIView view)
        {
            _stateManager.Push(new ViewState(view, PopView));
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