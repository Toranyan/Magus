using UnityEngine;

namespace magus.master
{
	public class MasterDataUnity<T> : ScriptableObject where T : BaseMasterData
	{
		public T Data;
	}
}
