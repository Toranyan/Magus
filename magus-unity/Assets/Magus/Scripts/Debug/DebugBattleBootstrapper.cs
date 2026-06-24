using magus.battle;
using UnityEngine;
using magus.master;
using Cysharp.Threading.Tasks;


namespace magus.debug
{
    public class DebugBattleBootstrapper : MonoBehaviour
    {

		private void Start()
		{
			Init().Forget();	
		}

		private async UniTask Init()
		{
			await MasterData.LoadDataAsync();

			var abilityMaster1 = MasterData.GetMasterData<AbilityMasterData>("ability_fireball_01");
			var abilityMaster2 = MasterData.GetMasterData<AbilityMasterData>("ability_blackhole_01");

			var playerController = BattleController.Instance.PlayerController;
			playerController.SetAbility(0, new AbilityInfo(abilityMaster1, playerController));
			playerController.SetAbility(1, new AbilityInfo(abilityMaster2, playerController));
		}
	}
}