using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public class ActionContext
{
    public Unit Unit;
    public string RaceName;
    public int StateIndex;

    public bool WeaponIsLoaded;
    public bool HasRanged;
    public bool IsInMelee;
    public bool IsBeyondAttackRange;
    public bool IsInChargeRange;
    public bool TargetBehindObstacle;
    public bool IsHeavilyWounded;

    public bool OpponentExist;
    public bool FurthestUnitExist;
    public bool MostInjuredUnitExist;
    public bool LeastInjuredUnitExist;
    public bool WeakestUnitExist;
    public bool StrongestUnitExist;
    public bool TargetWithMostAlliesExist;

    public Weapon CurrentWeapon;
    public float DistanceToClosestOpponent;

    // Dodajemy referencję do TargetsInfo, by w ChooseValidActionEpsilonGreedy
    // móc sprawdzać info.Distances, itp.
    public TargetsInfo Info;
}
