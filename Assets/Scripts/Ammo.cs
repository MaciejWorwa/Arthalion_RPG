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
    public bool? Dangerous = null; // Niebezpieczna
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
        { "Be�t", new Ammo("Be�t", impale: true) },
        { "Bomba", new Ammo("Bomba", strength: 15, impact: true, dangerous: true, blast: 5) },
        { "Du�y pocisk i proch", new Ammo("Du�y pocisk i proch", strength: 2, impact: true, impale: true, penetrating: true) },
        { "Improwizowany �rut i proch", new Ammo("Improwizowany �rut i proch", attackRange: -12f)}, // ------------------- POWINNO ZMNIEJSZA� ZASI�G O PO�OW�
        { "Kamyk", new Ammo("Kamyk", strength: -2, attackRange: -5f, imprecise: true, undamaging: true) },
        { "Nas�czony Aqshy proch", new Ammo("Nas�czony Aqshy proch", strength: 2, attackRange: 5f, impale: true, penetrating: true) },
        { "Patron", new Ammo("Patron", strength: 1, impale: true, penetrating: true) },
        { "Pocisk i proch", new Ammo("Pocisk i proch", strength: 1, impale: true, penetrating: true) },
        { "Pocisk kamienny", new Ammo("Pocisk kamienny", pummel: true) },
        { "Pocisk o�owiany", new Ammo("Pocisk o�owiany", strength: 1, attackRange: -5f, pummel: true) },
        { "Precyzyjny pocisk i proch", new Ammo("Precyzyjny pocisk i proch", strength: 1, impale: true, penetrating: true, precise: true) },
        { "Strza�a", new Ammo("Strza�a", impale: true) },
        { "Strza�a elfia", new Ammo("Strza�a elfia", strength: 1, attackRange: 25f, accurate: true, impale: true, penetrating: true) },
        { "Strza�a przebijaj�ca", new Ammo("Strza�a przebijaj�ca", impale: true, penetrating: true) },
        { "Strza�a z�bkowana", new Ammo("Strza�a z�bkowana", impale: true, slash: 1) },
        { "�rut i proch", new Ammo("�rut i proch", spread: 3) },
        { "Zaostrzony patyk", new Ammo("Zaostrzony patyk", strength: -2, attackRange: -12f, imprecise: true, undamaging: true, dangerous: true) }, // ------------------- POWINNO ZMNIEJSZA� ZASI�G O PO�OW�
    };

    internal static bool TryGetValue(string ammoType, out Ammo effect)
    {
        throw new NotImplementedException();
    }
}
