using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace magus.master
{
    public class MasterData
    {
        private static Dictionary<string, SpellMasterData> _spellMasterData = new Dictionary<string, SpellMasterData>();
		private static Dictionary<string, AbilityMasterData> _abilityMasterData = new Dictionary<string, AbilityMasterData>();

		private static bool _isInitialized = false;

		private static Dictionary<Type, Dictionary<string, BaseMasterData>> _masterDataDict = new Dictionary<Type, Dictionary<string, BaseMasterData>>() 
        {
            [typeof(SpellMasterData)] = new(),
			[typeof(AbilityMasterData)] = new(),
		};

		public static async UniTask InitializeAsync()
		{
			if (_isInitialized)
			{
				return;
			}

			await LoadDataAsync();

			_isInitialized = true;
		}

		public static async UniTask LoadDataAsync()
		{
            await UniTask.WhenAll(
				LoadMasterDataAsync<SpellMasterDataUnity, SpellMasterData>("MasterData/SpellMasterData", _masterDataDict[typeof(SpellMasterData)]),
				LoadMasterDataAsync<AbilityMasterDataUnity, AbilityMasterData>("MasterData/AbilityMasterData", _masterDataDict[typeof(AbilityMasterData)])
			);
        }

        private static async UniTask LoadMasterDataAsync<T, U>(string addressableId, Dictionary<string, BaseMasterData> dict) where T : MasterDataUnity<U> where U : BaseMasterData
		{
			var handle = Addressables.LoadAssetsAsync<T>(addressableId);

			await handle.Task;

			if (handle.Result == null)
			{
				Debug.LogError($"Failed to load master data from addressable: {addressableId}");
				return;
			}

			foreach (var so in handle.Result)
			{
				if (so == null)
				{
					continue;
				}
				dict[so.Data.Id] = so.Data;
			}

		}

		public static List<T> GetAllMasterData<T>() where T : BaseMasterData
		{
			if (_masterDataDict.TryGetValue(typeof(T), out var dict))
			{
				var list = new List<T>();
				foreach (var val in dict.Values)
				{
					list.Add((T)val);
				}
				return list;
			}
			return new List<T>();
		}

		public static T GetMasterData<T>(string id) where T : BaseMasterData
		{
			if (_masterDataDict.TryGetValue(typeof(T), out var dict))
			{
				if (dict.TryGetValue(id, out var val))
				{
					return (T)val;
				}
			}
			Debug.LogWarning($"Master data of type {typeof(T).Name} with id {id} not found.");
			return null;
		}

    }

}
