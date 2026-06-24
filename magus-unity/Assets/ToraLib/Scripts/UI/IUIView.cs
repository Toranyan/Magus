using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tora.ui {

    public interface IUIView {

        bool IsOpen {
            get;
        }

        bool IsShown {
            get;
		}
        void Open();
        void Close();

        void Show();
        void Hide();
    }

}