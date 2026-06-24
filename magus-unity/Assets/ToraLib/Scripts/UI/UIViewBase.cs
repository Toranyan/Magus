using Cysharp.Threading.Tasks;
using UnityEngine;

namespace tora.ui {

    public class UIViewBase : MonoBehaviour, IUIView {

        [SerializeField]
        private GameObject _root;

        public bool IsOpen {
            get;
            protected set;
        }

        public bool IsShown
		{
            get;
            protected set;
		}

        public virtual void Show() {
            _root.SetActive(true);
            IsShown = true;
        }

        public virtual void Hide() {
            _root.SetActive(false);
            IsShown = false;
        }

		public void Open()
		{
            Show();
            IsOpen = true;
        }

		public void Close()
		{
            Hide();
            IsOpen = false;
        }
	}

}