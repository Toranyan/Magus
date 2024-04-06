using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tora.singleton;

namespace magus.ui {

    public class UIController : SingletonComponent<UIController>
    {
        [SerializeField]
        private UITitle _uiTitle;

    }

}