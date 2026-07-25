using magus.battle;
using UnityEngine;
using magus.master;
using magus.ui;
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

			var spellMaster0 = MasterData.GetMasterData<SpellMasterData>("spell_fireball_01");
			var spellMaster1 = MasterData.GetMasterData<SpellMasterData>("spell_meteor_01");
			var spellMaster2 = MasterData.GetMasterData<SpellMasterData>("spell_blackhole_01");

			var playerController = BattleController.Instance.PlayerController;

			var hasteData = MasterData.GetMasterData<StatusEffectMasterData>("status_haste");
			var statusEffectController = playerController.Unit.GetComponent<StatusEffectController>();
			statusEffectController.Apply(hasteData);

			var unitSpell0 = new UnitSpellInstance(new SpellInfo(spellMaster0), playerController.Unit);
			var unitSpell1 = new UnitSpellInstance(new SpellInfo(spellMaster1), playerController.Unit);
			var unitSpell2 = new UnitSpellInstance(new SpellInfo(spellMaster2), playerController.Unit);
			//unitSpell0.Autocast = true;
			playerController.SetSpell(0, unitSpell0);
			playerController.SetSpell(1, unitSpell1);
			playerController.SetSpell(2, unitSpell2);

			var battleUIView = UIManager.Instance.GetView<UIBattleView>();
			if (battleUIView != null)
			{
				for (int i = 0; i < 5; i++)
					battleUIView.SetSpell(i, playerController.GetSpell(i));

				battleUIView.SpellCastRequested += slot => playerController.CastSpellSlot(slot);
				battleUIView.Open();
			}

			BattleController.Instance.InitRequired();

			//BattleController.Instance.ProjectileManager.Preload(new[] {
			//	"Prefabs/Projectiles/BlackHole",
			//	"Prefabs/Projectiles/Fireball",
			//}).Forget();
			//BattleController.Instance.EffectManager.Preload(new[] {
			//	"Prefabs/Effects/BallExplosion",
			//	"Prefabs/Effects/Explosion_01",
			//}).Forget();

		}
	}
}