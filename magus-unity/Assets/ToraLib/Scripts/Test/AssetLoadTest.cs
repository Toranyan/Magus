using UnityEngine;
using System.Collections;

using tora.assetbundle;

public class AssetLoadTest : MonoBehaviour {

	[SerializeField]
	protected string m_assetBundleToLoad;

	[SerializeField]
	protected string m_prefabToLoad;

	// Use this for initialization
	void Start () {

		//CAssetBundleManager.
		StartCoroutine(AsyncInit());
	}


	public IEnumerator AsyncInit() {

		AssetBundleManager abm = AssetBundleManager.Instance;

		bool loadDone = false;
		AssetBundle assetBundle = null;
		abm.LoadAssetBundleName(m_assetBundleToLoad, (a) => {
			loadDone = true;
			assetBundle = a;
		});

		while(!loadDone) {
			yield return false;
		}

		if(assetBundle != null) {
			//instantiate
			GameObject pfb = assetBundle.LoadAsset<GameObject>(m_prefabToLoad);

			if(pfb != null) {
				GameObject.Instantiate(pfb);
			} else {
				Debug.LogWarning("Asset not found : ");
			}
		}




		yield return true;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
