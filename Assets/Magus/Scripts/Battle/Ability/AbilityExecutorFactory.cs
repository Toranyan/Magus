using magus.battle;

namespace magus.battle
{

    public class AbilityExecutorFactory
    {
        public static IAbilityExecutor CreateAbilityExecutor(AbilityInfo info)
		{
            switch(info.Type)
			{
                case AbilityType.Projectile:
                    return new ProjectileAbiltyExecutor();
                default:
                    return null;
			}
		}
    }


}
