using System;
using UnityEngine;


namespace magus.battle
{

    public enum BattleEntityType
    {
        Player,
        Enemy,
        Projectile,
        Environment,
        Neutral,
	}

	/// <summary>
	/// Represents an object that participates in the battle scene (player, enemy, projectile, etc.).
	/// Keep minimal to avoid forcing many implementations to provide extra members.
	/// </summary>
	public interface IBattleEntity
    {
        int TeamId { get; }

        GameObject GameObject { get; }

    }

    [Obsolete("IOwner has been replaced by IBattleEntity. Implement IBattleEntity instead.")]
    public interface IOwner : IBattleEntity
    {
    }

}