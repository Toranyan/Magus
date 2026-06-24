using UnityEngine;

namespace magus.battle
{
    public interface IAbilityExecutor
    {
        void ExecuteAbility(AbilityExecutionContext context);

    }

}