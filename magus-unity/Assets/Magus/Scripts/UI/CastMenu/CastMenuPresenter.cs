using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace magus.ui
{
    public class CastMenuPresenter
    {
        private UICastMenuView _view;

        public void Init()
		{
            //create view in manager
            //_view = UIManager.Instance.CreateView<UICastMenuView>();

            _view.Init();
		}

        public void Open()
		{

		}

        public void Close()
		{

		}

    }
}
