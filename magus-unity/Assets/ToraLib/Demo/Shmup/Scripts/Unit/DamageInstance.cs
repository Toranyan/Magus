using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class DamageInstance
{
    public float Value;
    public DamageType Type;
    public IUnit Source;

    public DamageInstance (IUnit unit, DamageType type, float value) {
        Source = unit;
        Type = type;
        Value = value;
	}
}

public enum DamageType 
{
    None,
    Energy,
    Physical,
}