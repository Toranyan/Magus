using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace magus
{
    public class AutoFireWeapon : Weapon
    {

		[SerializeField]
		private float fireRate;


		private double lastFireTimeElapsed;


		private void Update()
		{
			//try shoot
			if (lastFireTimeElapsed > 1.0f / fireRate)
			{
				//actually shoot
				var proj = SpawnProjectile();
				//send it to the target

			}

		}

	}

}
