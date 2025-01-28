using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum MeleeCategory
{
    Basic,
    Cavalry,
    Fencing,
    Brawling,
    Flail
}
public enum RangedCategory
{
    Bow,
    Crossbow
}


public class Weapon : MonoBehaviour
{
    public int Id;

    [Header("Nazwa")]
    public string Name;

    [Header("Typ")]
    public string[] Type;
    public bool TwoHanded;
    public bool NaturalWeapon;

    [Header("Jakość")]
    public string Quality;

    [Header("Kategoria")]
    public string Category;

    [Header("Obciążenie")]
    public int Encumbrance; // Obciążenie

    [Header("Uszkodzenie")]
    public int Damage; // Uszkodzenie broni

    [Header("Siła")]
    public int S;

    [Header("Zasięg")]
    public float AttackRange;

    [Header("Czas przeładowania")]
    public int ReloadTime;
    public int ReloadLeft;

    [Header("Cechy")]
    public bool Accurate; // Celny (+10 do trafienia)
    public bool Blackpowder; // Prochowa
    public int Blast; // Wybuchowa
    public bool Bulky; // Nieporęczny (zwiększa obciążenie o 1)
    public bool Damaging; // Przebijająca
    public bool Dangerous; // Niebezpieczna
    public bool Defensive; // Parujący
    public bool Distract; // Dekoncentrujący (Powoduje cofanie się. Mechanika jeszcze nie wprowadzona)
    public int Durable; // Wytrzymały (str. 292)
    public bool Entangle; // Unieruchamiający
    public bool Fast; // Szybka
    public bool Hack; // Rąbiąca
    public bool Impact; // Druzgoczący
    public bool Impale; // Nadziewający (str. 298)
    public bool Imprecise; // Nieprecyzyjna (zmiejsza poziom testu ataku o 1)
    public bool Lightweight; // Poręczny (redukuje obciążenie o 1)
    public bool Penetrating; // Przekłuwająca
    public bool Practical; // Praktyczny (redukuje poziom porażki o 1)
    public bool Precise; // Precyzyjna (zwiększa poziom udanego testu ataku o 1)
    public bool Pummel; // Ogłuszający
    public bool Slow; // Powolny
    public int Shield; // Tarcza
    public bool Tiring; // Ciężka
    public bool Undamaging; // Tępy
    public bool Unbalanced; // Niewyważona
    public bool Unrielable; // Zawodny (zwiększa poziom porażki o 1)
    public bool Wrap; // Plącząca (utrudnia parowanie o 1 PS)

    public Dictionary<int, int> WeaponsWithReloadLeft = new Dictionary<int, int>(); // słownik zawierający wszystkie posiadane przez postać bronie wraz z ich ReloadLeft

    public void ResetWeapon()
    {
        Id = 0;
        Name = "Pięści";
        Type[0] = "melee";
        TwoHanded = false;
        Quality = "Zwykła";
        S = -4;
        AttackRange = 1.5f;
        ReloadTime = 0;
        ReloadLeft = 0;

        Defensive = false;
        Fast = false;
        Impact = false;
        Pummel = false;
        Slow = false;
        Tiring = false;
    }
}
