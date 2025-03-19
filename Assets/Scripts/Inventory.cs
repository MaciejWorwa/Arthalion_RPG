using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<Weapon> AllWeapons = new List<Weapon>(); //Wszystkie posiadane przez postać bronie
    //public List<Armor> AllArmors = new List<Armor>(); //Wszystkie posiadane przez postać elementy zbroi
    public Weapon[] EquippedWeapons = new Weapon[2]; //Przedmioty trzymane w rękach (liczba 2 odpowiada za dwie ręce. Gdy mamy broń dwuręczną to w obu rękach będzie kopia tej samej broni)
    public List<Weapon> EquippedArmors = new List<Weapon>(); //Wszystkie ubrane przez postać elementy zbroi
    public int CopperCoins;
    public int SilverCoins;
    public int GoldCoins;

    public Dictionary<string, List<string>> ArmorCategories = new Dictionary<string, List<string>>
    {
        { "head", new List<string>() },
        { "arms", new List<string>() },
        { "torso", new List<string>() },
        { "legs", new List<string>() }
    };

}
