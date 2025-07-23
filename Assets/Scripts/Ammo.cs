using System;
using System.Collections.Generic;

public class Ammo
{
    public string Name; // Nazwa amunicji

    // Lista efektów, które ta amunicja zmienia
    public int? S = null;        // Siła (S) - null oznacza, że nie zmienia tej cechy
    public float? AttackRange = null;
    public int? ReloadTime = null; // Czas przeładowania
    public bool? Accurate = null; // Celny
    public bool? Penetrating = null; // Przekłuwający
    public bool? Impale = null; // Nadziewający
    public int? Slash = null; // Sieczny --------------------- do wprowadzenia (do inpput fielda w editWeaponPanel nalezy dodać wywołanie funkcji, takie jak np. w input fieldzie Accurate)
    public bool? Undamaging = null; // Tępy
    public bool? Imprecise = null; // Nieprecyzyjna
    public bool? Dangerous = null; // Niebezpieczna
    public bool? Pummel = null; // Ogłuszający
    public bool? Impact = null; // Druzgoczący
    public int? Spread = null; // Rozrzucająca --------------------- do wprowadzenia (do inpput fielda w editWeaponPanel nalezy dodać wywołanie funkcji, takie jak np. w input fieldzie Accurate)
    public bool? Precise = null; // Precyzyjna
    public int? Blast = null; // Wybuchowa

    // Konstruktor pozwalający na ustawienie efektów
    public Ammo(string name, int? strength = null, float? attackRange = null, int? reloadTime = null,
                      bool? accurate = null, bool? penetrating = null,
                      bool? impale = null, int? slash = null,
                      bool? undamaging = null, bool? imprecise = null,
                      bool? dangerous = null, bool? pummel = null,
                      bool? impact = null, int? spread = null, bool? precise = null,
                      int? blast = null)
    {
        Name = name;
        S = strength;
        AttackRange = attackRange;
        ReloadTime = reloadTime;
        Accurate = accurate;
        Penetrating = penetrating;
        Impale = impale;
        Slash = slash;
        Undamaging = undamaging;
        Imprecise = imprecise;
        Dangerous = dangerous;
        Pummel = pummel;
        Impact = impact;
        Spread = spread;
        Precise = precise;
        Blast = blast;
    }

    // TO POSIADA PRZYKŁADOWO WPISANE WARTOŚCI. NALEŻY TO UZUPEŁNIĆ RZECZYWISTYMI WARTOŚCIAMI
    public static readonly Dictionary<string, Ammo> Ammos = new Dictionary<string, Ammo>
    {
        { "Brak", new Ammo("Brak") },
        { "Bełt", new Ammo("Bełt", impale: true) },
        { "Bomba", new Ammo("Bomba", strength: 15, impact: true, dangerous: true, blast: 5) },
        { "Duży pocisk i proch", new Ammo("Duży pocisk i proch", strength: 2, impact: true, impale: true, penetrating: true) },
        { "Improwizowany śrut i proch", new Ammo("Improwizowany śrut i proch", attackRange: -12f)}, // ------------------- POWINNO ZMNIEJSZAĆ ZASIĘG O POŁOWĘ
        { "Kamyk", new Ammo("Kamyk", strength: -2, attackRange: -5f, imprecise: true, undamaging: true) },
        { "Nasączony Aqshy proch", new Ammo("Nasączony Aqshy proch", strength: 2, attackRange: 5f, impale: true, penetrating: true) },
        { "Patron", new Ammo("Patron", strength: 1, impale: true, penetrating: true) },
        { "Pocisk i proch", new Ammo("Pocisk i proch", strength: 1, impale: true, penetrating: true) },
        { "Pocisk kamienny", new Ammo("Pocisk kamienny", pummel: true) },
        { "Pocisk ołowiany", new Ammo("Pocisk ołowiany", strength: 1, attackRange: -5f, pummel: true) },
        { "Precyzyjny pocisk i proch", new Ammo("Precyzyjny pocisk i proch", strength: 1, impale: true, penetrating: true, precise: true) },
        { "Strzała", new Ammo("Strzała", impale: true) },
        { "Strzała elfia", new Ammo("Strzała elfia", strength: 1, attackRange: 25f, accurate: true, impale: true, penetrating: true) },
        { "Strzała przebijająca", new Ammo("Strzała przebijająca", impale: true, penetrating: true) },
        { "Strzała ząbkowana", new Ammo("Strzała ząbkowana", impale: true, slash: 1) },
        { "Śrut i proch", new Ammo("Śrut i proch", spread: 3) },
        { "Zaostrzony patyk", new Ammo("Zaostrzony patyk", strength: -2, attackRange: -12f, imprecise: true, undamaging: true, dangerous: true) }, // ------------------- POWINNO ZMNIEJSZAĆ ZASIĘG O POŁOWĘ
    };

    internal static bool TryGetValue(string ammoType, out Ammo effect)
    {
        return Ammos.TryGetValue(ammoType, out effect);
    }
}
