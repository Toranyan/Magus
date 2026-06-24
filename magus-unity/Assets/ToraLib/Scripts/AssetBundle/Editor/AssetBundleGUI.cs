using UnityEngine;
using System.Collections;

using UnityEditor;


namespace tora.assetbundle {

	public class AssetBundleGUI : EditorWindow {

		[MenuItem("Tora/Asset Bundles/Asset Bundle Build GUI")]
		public static void Init() {
			AssetBundleGUI window = (AssetBundleGUI)EditorWindow.GetWindow (typeof (AssetBundleGUI));
			window.Show();
		}

		public void OnGUI() {
			if(GUILayout.Button("Build Asset Bundles")) {
				AssetBundleBuilder.BuildAllAssetBundles();
			}
		}

	}

}