using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using tora.singleton;
using magus.system;


namespace magus.singleton {

	public class SceneController : SingletonComponent<SceneController> {


		public void ChangeScene(Scene scene) {

			switch(scene)
			{
				case Scene.Battle:
					ChangeSceneInternal("BattleScene");
					break;
			}
		}

		private void ChangeSceneInternal(string sceneName)
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
		}


	}

}