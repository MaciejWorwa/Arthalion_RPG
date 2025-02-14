using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Security.Cryptography;

public enum MeleeCategory
{
    Basic,
    Cavalry,
    Fencing,
    Brawling,
    Flail,
    Parry,
    Polearm,
    TwoHanded
}
public enum RangedCategory
{
    Blackpowder,
    Bow,
    Crossbow,
    Engineering,
    Entangling,
    Explosives,
    Sling,
    Throwing
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

    [Header("Czas przeładowania")] // Tutaj są uwzględnione cechy Repeater (wielostrzał) i Reload (przeładowanie)
    public int ReloadTime;
    public int ReloadLeft;
    public int AmmoMax; // Maksymalna ilość amunicji w magazynku broni ---------------------- (MECHANIKA DO WPROWADZENIA)
    public int AmmoLeft; // Aktualna ilość amunicji w magazynku broni ---------------------- (MECHANIKA DO WPROWADZENIA)

    [Header("Cechy ogólne")]
    public int Durable; // Wytrzymały (str. 292) ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Practical; // Praktyczny (redukuje poziom porażki o 1)
    public bool Shoddy; //Tandetny  ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Unrielable; // Zawodny (zwiększa poziom porażki o 1)

    [Header("Cechy broni")]
    public bool Accurate; // Celny (+10 do trafienia)
    public bool Blackpowder; // Prochowa ---------------------- (MECHANIKA DO WPROWADZENIA)
    public int Blast; // Wybuchowa ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Damaging; // Przebijająca
    public bool Dangerous; // Niebezpieczna ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Defensive; // Parujący
    public bool Distract; // Dekoncentrujący (Powoduje cofanie się) ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Entangle; // Unieruchamiający
    public bool Fast; // Szybka ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Hack; // Rąbiąca ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Impact; // Druzgoczący
    public bool Impale; // Nadziewający (str. 298)
    public bool Imprecise; // Nieprecyzyjna (zmiejsza poziom testu ataku o 1)
    public bool Penetrating; // Przekłuwająca ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Pistol; // Pistolet
    public bool Precise; // Precyzyjna (zwiększa poziom udanego testu ataku o 1)
    public bool Pummel; // Ogłuszający ---------------------- (MECHANIKA DO WPROWADZENIA)
    public int Slash; // Sieczna ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Slow; // Powolny ---------------------- (MECHANIKA DO WPROWADZENIA)
    public int Shield; // Tarcza
    public int Spread; // Rozrzucająca  ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Tiring; // Ciężka
    public bool TrapBlade; // Łamacz mieczy ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Trip; // Przewracająca ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Unbreakable; // Niełamliwa ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Undamaging; // Tępy
    public bool Unbalanced; // Niewyważona
    public bool Wrap; // Plącząca (utrudnia parowanie o 1 PS)

    [Header("Cechy pancerza")]
    public int Armor;
    public bool Bulky; // Nieporęczny (zwiększa obciążenie o 1) ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Flexible; // Giętki  ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Impenetrable; // Nieprzebijalny ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Lightweight; // Poręczny (redukuje obciążenie o 1) ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Partial; // Częściowy  ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool WeakPoints; // Wrażliwe punkty  ---------------------- (MECHANIKA DO WPROWADZENIA)

    public Dictionary<int, int> WeaponsWithReloadLeft = new Dictionary<int, int>(); // słownik zawierający wszystkie posiadane przez postać bronie wraz z ich ReloadLeft

    public void ResetWeapon()
    {
        Id = 0;
        Name = "Pięści";
        Type = new string[] { "melee" };
        NaturalWeapon = true;
        TwoHanded = false;
        Quality = "Zwykła";
        S = 0;
        AttackRange = 1.5f;
        ReloadTime = 0;
        ReloadLeft = 0;
        AmmoLeft = 0;
        AmmoMax = 0;
        Category = "brawling";
        Encumbrance = 0;
        Damage = 0;

        Accurate = false;
        Blackpowder = false;
        Blast = 0;
        Damaging = false;
        Dangerous = false;
        Defensive = false;
        Distract = false;
        Durable = 0;
        Entangle = false;
        Fast = false;
        Hack = false;
        Impact = false;
        Impale = false;
        Imprecise = false;
        Penetrating = false;
        Pistol = false;
        Practical = false;
        Precise = false;
        Pummel = false;
        Slash = 0;
        Slow = false;
        Shield = 0;
        Shoddy = false;
        Spread = 0;
        Tiring = false;
        TrapBlade = false;
        Trip = false;
        Unbreakable = false;
        Unbalanced = false;
        Undamaging = true; // <------------- Tępy
        Unrielable = false;
        Wrap = false;
    }
}
