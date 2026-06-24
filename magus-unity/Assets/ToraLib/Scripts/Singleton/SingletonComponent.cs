using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tora.singleton {

    public class SingletonComponent<T> : MonoBehaviour where T : Component {
		public static T Instance {
			get {
				if (_instance == null) {
					_instance = FindObjectOfType<T>();

					if (_instance == null) {
						_instance = new GameObject(typeof(T).Name).AddComponent<T>();
					}
				}
				return _instance;
			}
		}

		private static T _instance = null;


		private void Awake() {

			if (_instance == null) {
				_instance = this as T;
			} else if (_instance != this) {
				Debug.LogWarning($"Duplicate of singleton {this} created");
				Destroy(this.gameObject);
			}
		}
	}

}