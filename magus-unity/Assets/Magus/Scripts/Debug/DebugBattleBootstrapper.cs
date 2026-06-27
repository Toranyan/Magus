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

			var spellMaster1 = MasterData.GetMasterData<SpellMasterData>("spell_fireball_01");
			var spellMaster2 = MasterData.GetMasterData<SpellMasterData>("spell_blackhole_01");

			var playerController = BattleController.Instance.PlayerController;

			var unitSpell1 = new UnitSpellInstance(new SpellInfo(spellMaster1), playerController);
			var unitSpell2 = new UnitSpellInstance(new SpellInfo(spellMaster2), playerController);
			playerController.SetSpell(0, unitSpell1);
			playerController.SetSpell(1, unitSpell2);
		}
	}
}