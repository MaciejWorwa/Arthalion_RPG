using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static UnityEngine.GraphicsBuffer;
using System.Linq;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

public enum SizeCategory
{
    Tiny = 0,      // drobny
    Little = 1,    // niewielki
    Small = 2,         // mały
    Average = 3,       // średni
    Large = 4,         // duży
    Enormous = 5,       // wielki
    Monstrous = 6   // monstrualny
}

public class Stats : MonoBehaviour
{
    public int Id;
    public bool IsBig;
    public int Overall; // Łączna wartość bojowa jednostki
    public int Exp; // Punkty doświadczenia

    [Header("Imię")]
    public string Name;

    [Header("Rasa")]
    public string Race;

    [Header("Rozmiar")]
    public SizeCategory Size; // Rozmiar

    [Header("Id początkowej broni")]
    public List<int> PrimaryWeaponIds = new List<int>();

    [Header("Atrybuty")]
    public int WW;
    public int US;
    public int S;
    public int Wt;
    public int I;
    public int Zw;
    public int Zr;
    public int Int;
    public int SW;
    public int Ogd;

    [Header("Cechy drugorzędowe")]
    public int Sz;
    public int TempSz;
    public int MaxHealth;
    public int TempHealth;
    public int CriticalWounds; // Ilość Ran Krytycznych
    public int CorruptionPoints; // Punkty Zepsucia
    public int SinPoints; // Punkty Grzechu (istotne dla kapłanów)
    public int PS;
    public int PP;
    public int Resolve; // Punkty Determinacji
    public int Resilience; // Punkty Bohatera
    public int ExtraPoints; // Dodatkowe punkty do rozdania między PP a Resilience
    public int Initiative; // Inicjatywa w walce
    public int CurrentEncumbrance; // Aktualne obciążenie ekwipunkiem
    public int MaxEncumbrance; // Maksymalny udźwig

    [Header("Punkty zbroi")]
    public int Armor_head;
    public int Armor_arms;
    public int Armor_torso;
    public int Armor_legs;

    [Header("Umiejętności")]
    public int Athletics;
    public int Channeling; // Splatanie magii
    public int Cool; // Opanowanie
    public int Dodge; // Unik
    public int Endurance; // Odporność
    public Dictionary<MeleeCategory, int> Melee; // Słownik przechowujący umiejętność Broń Biała dla każdej kategorii broni
    public Dictionary<RangedCategory, int> Ranged; // Słownik przechowujący umiejętność Broń Zasięgowa dla każdej kategorii broni

    [Header("Talenty")]
    public int AccurateShot; // Celny strzał
    public int Ambidextrous; // Oburęczność
    public bool Champion; // Czempion
    public int CombatMaster; // Mistrz walki
    public int CombatReflexes; // Bitewny refleks
    public int DirtyFighting; // Cios poniżej pasa
    public int Disarm; // Rozbrojenie
    public int Feint; // Finta
    public int Frightening; // Straszny
    public int FuriousAssault; // Wściekły atak
    public int Gunner; // Artylerzysta
    public int Hardy; // Twardziel
    public int Implacable; // Nieubłagany
    public int IronJaw; // Żelazna szczęka
    public int RapidReload; // Szybkie przeładowanie
    public int Resolute; // Nieugięty
    public int Robust; // Krzepki
    public bool Sharpshooter; // Strzał w dziesiątkę
    public int Shieldsman; // Tarczownik
    public int Sniper; // Snajper
    public int Sprinter; // Szybkobiegacz
    public int StoutHearted; // Waleczne serce
    public int StrikeMightyBlow; // Silny cios
    public bool StrikeToInjure; // Morderczy Atak
    public int StrikeToStun; // Ogłuszenie
    public int StrongBack; // Mocne plecy
    public int Sturdy; // Tragarz
    public int SureShot; // Strzał przebijający

    //STARE
    public bool ArmouredCasting; // Pancerz Wiary
    public bool DaemonicAura; // Demoniczna aura (Wt +2 na niemagiczną broń, odporność na truciznę, ataki demona to broń magiczna)
    public bool Ethereal; // Eteryczny
    public bool FastHands; //Dotyk mocy
    public bool Fearless; // Nieustraszony
    public bool MagicSense; //Zmysł magii
    public bool MightyMissile; // Morderczy pocisk
    public bool Terryfying; // Przerażający (test Terror)
    public bool WillOfIron; // Żelazna wola

    [Header("Statystyki")]
    public int HighestDamageDealt; // Największe zadane obrażenia
    public int TotalDamageDealt; // Suma zadanych obrażeń
    public int HighestDamageTaken; // Największe otrzymane obrażenia
    public int TotalDamageTaken; // Suma otrzymanych obrażeń
    public int OpponentsKilled; // Zabici przeciwnicy
    public string StrongestDefeatedOpponent; // Najsilniejszy pokonany przeciwnik
    public int StrongestDefeatedOpponentOverall; // Overall najsilniejszego pokonanego przeciwnika
    public int RoundsPlayed; // Suma rozegranych rund
    public int FortunateEvents; // Ilość "Szczęść"
    public int UnfortunateEvents; // Ilość "Pechów"

    private void Awake()
    {
        // Inicjalizacja domyślnych wartości TYLKO DO TESTÓW !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Melee = new Dictionary<MeleeCategory, int>
        {
            { MeleeCategory.Basic, 10 },
            { MeleeCategory.Cavalry, 5 },
            { MeleeCategory.Fencing, 15 },
            { MeleeCategory.Brawling, 8 },
            { MeleeCategory.Flail, 12 }
        };

        // Inicjalizacja domyślnych wartości TYLKO DO TESTÓW !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Ranged = new Dictionary<RangedCategory, int>
        {
            { RangedCategory.Bow, 13 },
            { RangedCategory.Crossbow, 21 }
        };

        Overall = CalculateOverall();
    }

    public void RollForBaseStats()
    {
        WW = RollStat(WW);
        US = RollStat(US);
        S = RollStat(S);
        Wt = RollStat(Wt);
        I = RollStat(I);
        Zw = RollStat(Zw);
        Zr = RollStat(Zr);
        Int = RollStat(Int);
        SW = RollStat(SW);
        Ogd = RollStat(Ogd);

        CalculateMaxHealth();

        // Rozdzielanie punktów ExtraPoints losowo pomiędzy PP i Resilience
        for (int i = 0; i < ExtraPoints; i++)
        {
            if (UnityEngine.Random.value < 0.5f)
                PP++;
            else
                Resilience++;
        }
        ExtraPoints = 0;

        PS = PP;
        Resolve = Resilience; // Punkty Determinacji są Równe Punktom Bohatera

        // Aktualizuje udźwig
        MaxEncumbrance = (S + Wt) / 10 + StrongBack + (Sturdy * 2);
    }
    private int RollStat(int statsValue)
    {
        if (statsValue == 0) return 0; // Jeśli wartość początkowa to 0, pozostaje 0
        if (statsValue < 10) return UnityEngine.Random.Range(1, 11); // Jeśli statystyka < 10, ustalamy wartość na 1-10

        int rollResult = UnityEngine.Random.Range(2, 21); // Losowanie 2-20
        if (Id > 4) statsValue -= 10; // Jeśli Id > 4 (czyli nie jest to człowiek, kranoslud, elf ani niziołek), odejmujemy 10

        return statsValue + rollResult;
    }

    public void CalculateMaxHealth()
    {
        int previousMaxHealth = MaxHealth;

        if (Size == SizeCategory.Tiny)
            MaxHealth = 1;
        else if (Size == SizeCategory.Little)
            MaxHealth = Wt / 10;
        else if (Size == SizeCategory.Small)
            MaxHealth = 2 * (Wt / 10) + SW / 10;
        else if (Size == SizeCategory.Average)
            MaxHealth = S / 10 + 2 * (Wt / 10) + SW / 10;
        else if (Size == SizeCategory.Large)
            MaxHealth = (S / 10 + 2 * (Wt / 10) + SW / 10) * 2;
        else if (Size == SizeCategory.Enormous)
            MaxHealth = (S / 10 + 2 * (Wt / 10) + SW / 10) * 4;
        else if (Size == SizeCategory.Monstrous)
            MaxHealth = (S / 10 + 2 * (Wt / 10) + SW / 10) * 8;

        //Uwzględnienie talentu Twardziel
        MaxHealth += Hardy * (Wt / 10);

        TempHealth += MaxHealth - previousMaxHealth;
    }

    // Pobieranie modyfikatora za umiejętność dla danej kategorii broni
    public int GetSkillModifier<T>(Dictionary<T, int> modifiers, T category) where T : Enum
    {
        if (modifiers.TryGetValue(category, out int modifier))
        {
            return modifier;
        }

        // Domyślny modyfikator (jeśli kategoria nie istnieje w słowniku)
        return 0;
    }

    public void CheckForSpecialRaceAbilities()
    {
        //Zdolność regeneracji
        if (Race == "Troll" || Race == "Troll Chaosu")
        {
            int regeneration = UnityEngine.Random.Range(0, 11);
            int currentWounds;

            if (TempHealth < MaxHealth)
            {
                currentWounds = MaxHealth - TempHealth;
            }
            else return;

            int woundsToHeal = regeneration < currentWounds ? regeneration : currentWounds;
            TempHealth += woundsToHeal;
            this.GetComponent<Unit>().DisplayUnitHealthPoints();

            Debug.Log($"{Name} zregenerował {woundsToHeal} żywotności.");
        }
    }

    public int CalculateOverall()
    {
        // Sumowanie wszystkich cech głównych
        int primaryStatsSum = WW + US + S + Wt + I + Zw + Zr + Int + SW + Ogd;
        //Debug.Log("primaryStatsSum " + primaryStatsSum);

        // Uwzględnienie rozmiaru (większy rozmiar = większy mnożnik)
        float sizeMultiplier = (1f + ((int)Size)) / 10; // Każdy poziom rozmiaru zwiększa overall
        //Debug.Log("sizeMultiplier " + sizeMultiplier);

        // Sumowanie zbroi i wytrzymałości
        int totalArmor = Armor_head + Armor_arms + Armor_torso + Armor_legs + (Wt / 10 * 4);
        //Debug.Log("totalArmor " + totalArmor);

        // Uwzględnienie umiejętności
        int skillSum = Athletics + Channeling + Dodge;
        //Debug.Log("skillSum " + skillSum);

        // Sumowanie umiejętności broni białej
        if (Melee != null)
        {
            skillSum += Melee.Values.Sum();
            //Debug.Log("Melee.Values.Sum() " + Melee.Values.Sum());
        }

        // Sumowanie umiejętności broni dystansowej
        if (Ranged != null)
        {
            skillSum += Ranged.Values.Sum();
            //Debug.Log("Ranged.Values.Sum() " + Ranged.Values.Sum());
        }

        // Uwzględnienie mocy broni
        int weaponPower = 0;
        Weapon weapon = InventoryManager.Instance.ChooseWeaponToAttack(this.gameObject);
        if (weapon != null)
        {
            if (weapon.Type.Contains("ranged"))
            {
                weaponPower += weapon.S * 8;
            }
            else if (weapon.Type.Contains("melee"))
            {
                weaponPower += weapon.S + (S / 10 * 8);
            }
           // Debug.Log("weaponPower " + weaponPower);
        }

        // Zliczanie aktywnych talentów
        int activeTalentsCount = GetType()
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(field => field.FieldType == typeof(bool) && (bool)field.GetValue(this))
            .Count();

       // Debug.Log("activeTalentsCount " + activeTalentsCount);

        // Obliczanie Overall z uwzględnieniem mnożnika rozmiaru
        int overall = Mathf.RoundToInt(((primaryStatsSum / 3) + weaponPower + MaxHealth + Sz + totalArmor * 2 + activeTalentsCount + skillSum / 3) * sizeMultiplier);

        //Debug.Log($"Overall {Name} to {overall}");

        return overall;
    }


    //Zwraca kopię tej klasy
    public Stats Clone()
    {
        return (Stats)this.MemberwiseClone();
    }
}
