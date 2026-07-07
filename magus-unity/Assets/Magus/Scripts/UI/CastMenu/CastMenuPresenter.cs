using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace magus.ui
{
    public class CastMenuPresenter
    {
        private readonly UICastMenuView _viewPrefab;
        private UICastMenuView _view;

        public CastMenuPresenter(UICastMenuView viewPrefab)
        {
            _viewPrefab = viewPrefab;
        }

        public void Init()
		{
            _view = UIManager.Instance.GetOrCreateView(_viewPrefab);
            _view.Init();
		}

        public void Open()
		{
            _view.Open();
		}

        public void Close()
		{
            _view.Close();
		}

    }
}
