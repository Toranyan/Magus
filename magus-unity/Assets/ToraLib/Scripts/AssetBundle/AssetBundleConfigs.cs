using UnityEngine;
using System.Collections;

namespace tora.assetbundle {

	public class AssetBundleConfigs {

		public const string ASSET_BUNDLE_EXPORT_PATH = "/AssetBundles/"; //from datapath


		public static string GetAssetBundleBaseURL() {
			return "file://" + Application.dataPath + ASSET_BUNDLE_EXPORT_PATH;
		}
		
	}

}