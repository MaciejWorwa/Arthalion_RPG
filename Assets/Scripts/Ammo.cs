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
    public bool? Dangerous = null; // Niebezpieczna
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
        { "Be³t", new Ammo("Be³t", impale: true) },
        { "Bomba", new Ammo("Bomba", strength: 15, impact: true, dangerous: true, blast: 5) },
        { "Du¿y pocisk i proch", new Ammo("Du¿y pocisk i proch", strength: 2, impact: true, impale: true, penetrating: true) },
        { "Improwizowany œrut i proch", new Ammo("Improwizowany œrut i proch", attackRange: -12f)}, // ------------------- POWINNO ZMNIEJSZAÆ ZASIÊG O PO£OWÊ
        { "Kamyk", new Ammo("Kamyk", strength: -2, attackRange: -5f, imprecise: true, undamaging: true) },
        { "Nas¹czony Aqshy proch", new Ammo("Nas¹czony Aqshy proch", strength: 2, attackRange: 5f, impale: true, penetrating: true) },
        { "Patron", new Ammo("Patron", strength: 1, impale: true, penetrating: true) },
        { "Pocisk i proch", new Ammo("Pocisk i proch", strength: 1, impale: true, penetrating: true) },
        { "Pocisk kamienny", new Ammo("Pocisk kamienny", pummel: true) },
        { "Pocisk o³owiany", new Ammo("Pocisk o³owiany", strength: 1, attackRange: -5f, pummel: true) },
        { "Precyzyjny pocisk i proch", new Ammo("Precyzyjny pocisk i proch", strength: 1, impale: true, penetrating: true, precise: true) },
        { "Strza³a", new Ammo("Strza³a", impale: true) },
        { "Strza³a elfia", new Ammo("Strza³a elfia", strength: 1, attackRange: 25f, accurate: true, impale: true, penetrating: true) },
        { "Strza³a przebijaj¹ca", new Ammo("Strza³a przebijaj¹ca", impale: true, penetrating: true) },
        { "Strza³a z¹bkowana", new Ammo("Strza³a z¹bkowana", impale: true, slash: 1) },
        { "Œrut i proch", new Ammo("Œrut i proch", spread: 3) },
        { "Zaostrzony patyk", new Ammo("Zaostrzony patyk", strength: -2, attackRange: -12f, imprecise: true, undamaging: true, dangerous: true) }, // ------------------- POWINNO ZMNIEJSZAÆ ZASIÊG O PO£OWÊ
    };

    internal static bool TryGetValue(string ammoType, out Ammo effect)
    {
        throw new NotImplementedException();
    }
}
