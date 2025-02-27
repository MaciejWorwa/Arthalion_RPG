using System;
using System.Collections.Generic;

public class Ammo
{
    public string Name; // Nazwa amunicji

    // Lista efekt�w, kt�re ta amunicja zmienia
    public int? S = null;        // Si�a (S) - null oznacza, �e nie zmienia tej cechy
    public float? AttackRange = null;
    public int? ReloadTime = null; // Czas prze�adowania
    public bool? Accurate = null; // Celny
    public bool? Penetrating = null; // Przek�uwaj�cy
    public bool? Impale = null; // Nadziewaj�cy
    public int? Slash = null; // Sieczny --------------------- do wprowadzenia (do inpput fielda w editWeaponPanel nalezy doda� wywo�anie funkcji, takie jak np. w input fieldzie Accurate)
    public bool? Undamaging = null; // T�py
    public bool? Imprecise = null; // Nieprecyzyjna
    public bool? Dangerous = null; // Niebezpieczna   --------------------- do wprowadzenia (do inpput fielda w editWeaponPanel nalezy doda� wywo�anie funkcji, takie jak np. w input fieldzie Accurate)
    public bool? Pummel = null; // Og�uszaj�cy
    public bool? Impact = null; // Druzgocz�cy
    public int? Spread = null; // Rozrzucaj�ca --------------------- do wprowadzenia (do inpput fielda w editWeaponPanel nalezy doda� wywo�anie funkcji, takie jak np. w input fieldzie Accurate)
    public bool? Precise = null; // Precyzyjna
    public int? Blast = null; // Wybuchowa

    // Konstruktor pozwalaj�cy na ustawienie efekt�w
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

    // TO POSIADA PRZYK�ADOWO WPISANE WARTO�CI. NALE�Y TO UZUPE�NI� RZECZYWISTYMI WARTO�CIAMI
    public static readonly Dictionary<string, Ammo> Ammos = new Dictionary<string, Ammo>
    {
        { "Brak", new Ammo("Brak") },
        { "Be�t", new Ammo("Be�t", strength: 5) },
        { "Bomba", new Ammo("Bomba", strength: 10, attackRange: 3.0f) },
        { "Du�y pocisk i proch", new Ammo("Du�y pocisk i proch", strength: 7, attackRange: 4.0f, accurate: true) },
        { "Improwizowany �rut i proch", new Ammo("Improwizowany �rut i proch", strength: 4, attackRange: 2.5f) },
        { "Kamyk", new Ammo("Kamyk", strength: 2, attackRange: 1.0f) },
        { "Pocisk kamienny", new Ammo("Pocisk kamienny", strength: 4, attackRange: 1.8f) },
        { "Pocisk o�owiany", new Ammo("Pocisk o�owiany", strength: 6, attackRange: 2.0f, accurate: true) },
        { "Strza�a", new Ammo("Strza�a", strength: 5, attackRange: 3.5f, accurate: true) },
        { "Strza�a elfia", new Ammo("Strza�a elfia", strength: 6, attackRange: 4.0f, accurate: true) },
        { "Strza�a przebijaj�ca", new Ammo("Strza�a przebijaj�ca", strength: 5, attackRange: 3.5f) },
        { "Strza�a z�bkowana", new Ammo("Strza�a z�bkowana", strength: 6, attackRange: 3.5f, accurate: true) }
    };

    internal static bool TryGetValue(string ammoType, out Ammo effect)
    {
        throw new NotImplementedException();
    }
}
