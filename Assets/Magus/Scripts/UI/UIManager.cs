using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tora.singleton;

namespace magus.ui
{

    public class UIManager : SingletonComponent<UIManager>
    {

        [SerializeField]
        private Canvas _mainCanvas;

        [SerializeField]
        private Transform _root;

        
        
    }

}