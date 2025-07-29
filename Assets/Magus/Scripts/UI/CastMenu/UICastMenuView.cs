using System.Collections;
using System.Collections.Generic;
using tora.ui;
using UnityEngine;
using UnityEngine.UI;

namespace magus.ui
{
    public class UICastMenuView : UIViewBase
    {
        [SerializeField]
        Button _castButton;

		public void Init()
		{
			_castButton.onClick.AddListener(OnCastButtonTapped);

		}


		private void OnCastButtonTapped()
		{
		}


	}

}