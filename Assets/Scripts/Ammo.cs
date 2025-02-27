using System;
using System.Collections.Generic;

public class Ammo
{
    public string Name; // Nazwa amunicji

    // Lista efektów, które ta amunicja zmienia
    public int? S = null;        // Si³a (S) - null oznacza, ¿e nie zmienia tej cechy
    public float? AttackRange = null;
    public int? ReloadTime = null; // Czas prze³adowania
    public bool? Accurate = null; // Celny
    public bool? Penetrating = null; // Przek³uwaj¹cy
    public bool? Impale = null; // Nadziewaj¹cy
    public int? Slash = null; // Sieczny --------------------- do wprowadzenia (do inpput fielda w editWeaponPanel nalezy dodaæ wywo³anie funkcji, takie jak np. w input fieldzie Accurate)
    public bool? Undamaging = null; // Têpy
    public bool? Imprecise = null; // Nieprecyzyjna
    public bool? Dangerous = null; // Niebezpieczna   --------------------- do wprowadzenia (do inpput fielda w editWeaponPanel nalezy dodaæ wywo³anie funkcji, takie jak np. w input fieldzie Accurate)
    public bool? Pummel = null; // Og³uszaj¹cy
    public bool? Impact = null; // Druzgocz¹cy
    public int? Spread = null; // Rozrzucaj¹ca --------------------- do wprowadzenia (do inpput fielda w editWeaponPanel nalezy dodaæ wywo³anie funkcji, takie jak np. w input fieldzie Accurate)
    public bool? Precise = null; // Precyzyjna
    public int? Blast = null; // Wybuchowa

    // Konstruktor pozwalaj¹cy na ustawienie efektów
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

    // TO POSIADA PRZYK£ADOWO WPISANE WARTOŒCI. NALE¯Y TO UZUPE£NIÆ RZECZYWISTYMI WARTOŒCIAMI
    public static readonly Dictionary<string, Ammo> Ammos = new Dictionary<string, Ammo>
    {
        { "Brak", new Ammo("Brak") },
        { "Be³t", new Ammo("Be³t", strength: 5) },
        { "Bomba", new Ammo("Bomba", strength: 10, attackRange: 3.0f) },
        { "Du¿y pocisk i proch", new Ammo("Du¿y pocisk i proch", strength: 7, attackRange: 4.0f, accurate: true) },
        { "Improwizowany œrut i proch", new Ammo("Improwizowany œrut i proch", strength: 4, attackRange: 2.5f) },
        { "Kamyk", new Ammo("Kamyk", strength: 2, attackRange: 1.0f) },
        { "Pocisk kamienny", new Ammo("Pocisk kamienny", strength: 4, attackRange: 1.8f) },
        { "Pocisk o³owiany", new Ammo("Pocisk o³owiany", strength: 6, attackRange: 2.0f, accurate: true) },
        { "Strza³a", new Ammo("Strza³a", strength: 5, attackRange: 3.5f, accurate: true) },
        { "Strza³a elfia", new Ammo("Strza³a elfia", strength: 6, attackRange: 4.0f, accurate: true) },
        { "Strza³a przebijaj¹ca", new Ammo("Strza³a przebijaj¹ca", strength: 5, attackRange: 3.5f) },
        { "Strza³a z¹bkowana", new Ammo("Strza³a z¹bkowana", strength: 6, attackRange: 3.5f, accurate: true) }
    };

    internal static bool TryGetValue(string ammoType, out Ammo effect)
    {
        throw new NotImplementedException();
    }
}
