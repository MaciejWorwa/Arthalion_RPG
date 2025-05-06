using System.Collections.Generic;
using UnityEngine;

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
    public WeaponBaseStats BaseWeaponStats; // Przechowywanie bazowych statystyk broni

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

    [Header("Typ amunicji")]
    public string AmmoType = "Brak"; // Rodzaj amunicji

    [Header("Czas przeładowania")] // Tutaj są uwzględnione cechy Repeater (wielostrzał) i Reload (przeładowanie)
    public int ReloadTime;
    public int ReloadLeft;
    public int AmmoMax; // Maksymalna ilość amunicji w magazynku broni ---------------------- (MECHANIKA DO WPROWADZENIA)
    public int AmmoLeft; // Aktualna ilość amunicji w magazynku broni ---------------------- (MECHANIKA DO WPROWADZENIA)

    [Header("Cechy ogólne")]
    public int Durable; // Wytrzymały
    public bool Practical; // Praktyczny (redukuje poziom porażki o 1)
    public bool Shoddy; //Tandetny
    public bool Unrielable; // Zawodny (zwiększa poziom porażki o 1)
    public bool Bulky; // Nieporęczny (zwiększa obciążenie o 1) ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Lightweight; // Poręczny (redukuje obciążenie o 1) ---------------------- (MECHANIKA DO WPROWADZENIA)

    [Header("Cechy broni")]
    public bool Accurate; // Celny (+10 do trafienia)
    public bool Blackpowder; // Prochowa
    public int Blast; // Wybuchowa
    public bool Damaging; // Przebijająca
    public bool Dangerous; // Niebezpieczna
    public bool Defensive; // Parujący
    public bool Distract; // Dekoncentrujący (Powoduje cofanie się)
    public bool Entangle; // Unieruchamiający ---------------------- DZIAŁA JAKO TAKO, MOŻLIWE ŻE TRZEBA BĘDZIE POPRAWIĆ
    public bool Fast; // Szybka
    public bool Hack; // Rąbiąca
    public bool Impact; // Druzgoczący
    public bool Impale; // Nadziewający (str. 298)
    public bool Imprecise; // Nieprecyzyjna (zmiejsza poziom testu ataku o 1)
    public bool Penetrating; // Przekłuwająca
    public bool Pistol; // Pistolet
    public bool Precise; // Precyzyjna (zwiększa poziom udanego testu ataku o 1)
    public bool Pummel; // Ogłuszający
    public int Slash; // Sieczna
    public bool Slow; // Powolny
    public int Shield; // Tarcza
    public int Spread; // Rozrzucająca
    public bool Tiring; // Ciężka
    public bool TrapBlade; // Łamacz mieczy ---------------------- (MECHANIKA DO WPROWADZENIA, powiązać z cechą Wytrzymały (Durable) str. 292)
    public bool Trip; // Przewracająca
    public bool Unbreakable; // Niełamliwa ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Undamaging; // Tępy
    public bool Unbalanced; // Niewyważona
    public bool Wrap; // Plącząca (utrudnia parowanie o 1 PS)

    [Header("Cechy pancerza")]
    public int Armor;
    public bool Flexible; // Giętki
    public bool Impenetrable; // Nieprzebijalny
    public bool Partial; // Częściowy
    public bool WeakPoints; // Wrażliwe punkty

    public Dictionary<int, int> WeaponsWithReloadLeft = new Dictionary<int, int>(); // słownik zawierający wszystkie posiadane przez postać bronie wraz z ich ReloadLeft

    // Funkcja pomocnicza do zapisywania bazowych cech broni dystansowych, przed uwzględnieniem typu amunicji
    public void SetBaseWeaponStats()
    {
        // Zapisujemy bazowe statystyki przy uruchomieniu
        BaseWeaponStats = new WeaponBaseStats
        {
            S = this.S,
            AttackRange = this.AttackRange,
            ReloadTime = this.ReloadTime,
            Accurate = this.Accurate,
            Penetrating = this.Penetrating,
            Impale = this.Impale,
            Slash = this.Slash,
            Undamaging = this.Undamaging,
            Imprecise = this.Imprecise,
            Dangerous = this.Dangerous,
            Pummel = this.Pummel,
            Impact = this.Impact,
            Spread = this.Spread,
            Precise = this.Precise,
            Blast = this.Blast
        };
    }

    public void ResetWeapon()
    {
        Id = 0;
        Name = "Pięści i kopniaki";
        Type = new string[] { "melee", "natural-weapon" };
        NaturalWeapon = true;
        TwoHanded = false;
        Quality = "Zwykła";
        S = 0;
        AttackRange = 1.5f;
        AmmoType = "Brak";
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

[System.Serializable]
public class WeaponBaseStats
{
    public int S;
    public float AttackRange;
    public int ReloadTime;
    public bool Accurate;
    public bool Penetrating;
    public bool Impale;
    public int Slash;
    public bool Undamaging;
    public bool Imprecise;
    public bool Dangerous;
    public bool Pummel;
    public bool Impact;
    public int Spread;
    public bool Precise;
    public int Blast;
}

