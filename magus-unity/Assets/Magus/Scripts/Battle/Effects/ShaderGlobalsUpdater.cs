using UnityEngine;

namespace magus.battle
{

    [ExecuteInEditMode]
    public class ShaderGlobalsUpdater : MonoBehaviour
    {
        [SerializeField] private Transform _player;


        static readonly int _playerPosID = Shader.PropertyToID("_PlayerPosition");

        public void SetPlayer(Transform player)
        {
            _player = player;
		}

		[ExecuteInEditMode]
		private void LateUpdate()
        {
            if (_player != null)
            {
				Debug.Log(_player.position);
				Shader.SetGlobalVector(_playerPosID, _player.position);
			}
		}
    }

}