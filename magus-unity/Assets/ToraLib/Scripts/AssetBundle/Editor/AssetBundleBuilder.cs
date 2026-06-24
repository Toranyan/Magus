using UnityEngine;
using UnityEditor;

using System.IO;
using System.Collections;
using System.Collections.Generic;


namespace tora.assetbundle {

	public class AssetBundleBuilder {


		[MenuItem("Tora/Asset Bundles/Build Asset Bundles")]
		public static void BuildAllAssetBundles() {

			string dir = Directory.GetCurrentDirectory() + AssetBundleConfigs.ASSET_BUNDLE_EXPORT_PATH ;

			if(!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}

			BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
		}

		[MenuItem("Tora/Asset Bundles/Build Asset Bundles - Variants")]
		public static void BuildAssetBundlesVariants() {

			string dir = Directory.GetCurrentDirectory() + AssetBundleConfigs.ASSET_BUNDLE_EXPORT_PATH ;

			if(!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}

			List<AssetBundleBuild> listBuild = new List<AssetBundleBuild>();

			//temp
			AssetBundleBuild build = new AssetBundleBuild();
			build.assetBundleName = "globalassets";
			List<string> listPaths = new List<string>();
			listPaths.Add("Assets/RobotGame/AssetBundles/globalassets/testprefab.prefab/");
			build.assetNames = listPaths.ToArray();


			//localized shit


			AssetBundleBuild[] buildArr = listBuild.ToArray();

			BuildPipeline.BuildAssetBundles(dir, buildArr, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
		}

	}

}