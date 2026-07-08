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

			var unitSpell1 = new UnitSpellInstance(new SpellInfo(spellMaster1), playerController.Unit);
			var unitSpell2 = new UnitSpellInstance(new SpellInfo(spellMaster2), playerController.Unit);
			playerController.SetSpell(0, unitSpell1);
			playerController.SetSpell(1, unitSpell2);

			BattleController.Instance.InitRequired();

			BattleController.Instance.ProjectileManager.Preload(new[] {
				"Prefabs/Projectiles/BlackHole",
				"Prefabs/Projectiles/Fireball",
			}).Forget();
			BattleController.Instance.EffectManager.Preload(new[] {
				"Prefabs/Effects/BallExplosion",
				"Prefabs/Effects/Explosion_01",
			}).Forget();

		}
	}
}