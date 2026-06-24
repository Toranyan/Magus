using UnityEngine;

using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;

using tora.singleton;

namespace tora.assetbundle {

	public class AssetBundleManager : SingletonComponent<AssetBundleManager> {

		protected Dictionary<string, AssetBundle> m_dictAssetBundle = new Dictionary<string, AssetBundle>();

		public static int Version {
			get {
				return 0;
			}
		}

		public void LoadAssetBundleName(string name, Action<AssetBundle> callback) {
			string url = AssetBundleConfigs.GetAssetBundleBaseURL() + name;
			StartCoroutine(LoadAssetBundleURLInternal(url, callback));
		}

		public IEnumerator LoadAssetBundleNameAsync(string name, Action<AssetBundle> callback) {
			string url = AssetBundleConfigs.GetAssetBundleBaseURL() + name;
			yield return LoadAssetBundleURLInternal(url, callback);
		}

		protected IEnumerator LoadAssetBundleURLInternal(string url, Action<AssetBundle> callback) {

			url = "file://D:/Manuel/Projects/Unity/RobotGame/AssetBundles/globalassets.unity3d";

			Debug.Log("Loading Asset Bundle from : " + url);

			if(File.Exists(url)) {
				//Debug.Log(url + " EXISTS");
			} else {
				Debug.Log("File not found : " + url);
			}

			WWW www = WWW.LoadFromCacheOrDownload(url, Version);
			yield return www;

			AssetBundle assetBundle = null;

			//add to list
			if(www.error == null) {
				assetBundle = www.assetBundle;
				//UnityEngine.Object[] objects = assetBundle.LoadAllAssets();

				if(assetBundle != null) {
					m_dictAssetBundle.Add(assetBundle.name, assetBundle);

				} else {
					Debug.LogWarning("Asset Bundle not found in : " + www.url);

				}
			} else {
				Debug.LogWarning("WWW error : " + www.error);
			}

			if(callback != null) {
				callback(assetBundle);
			}

		}



		public void LoadAsset<T>(string assetBundleName, string objectName, Action<T> callback) {

		}

		public IEnumerator LoadAssetInternal<T>(string assetBundleName, string objectName, Action<T> callback) {
			yield return true;
		}


	}

}