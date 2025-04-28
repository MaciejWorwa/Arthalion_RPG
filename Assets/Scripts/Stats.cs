using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using UnityEngine;

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
    public int Overall; // Łączna wartość bojowa jednostki
    public int Exp; // Punkty doświadczenia

    [Header("Imię")]
    public string Name;

    [Header("Rasa")]
    public string Race;

    [Header("Rozmiar")]
    public SizeCategory Size; // Rozmiar

    [Header("Nazwy początkowych broni")]
    public List<string> PrimaryWeaponNames = new List<string>();

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
    public int MagicLanguage; // Język magiczny
    public int Pray; // Modlitwa
    public Dictionary<MeleeCategory, int> Melee; // Słownik przechowujący umiejętność Broń Biała dla każdej kategorii broni
    public Dictionary<RangedCategory, int> Ranged; // Słownik przechowujący umiejętność Broń Zasięgowa dla każdej kategorii broni

    [Header("Talenty")]
    public int AethyricAttunement; // Zmysł Magii
    public int AccurateShot; // Celny strzał
    public int Ambidextrous; // Oburęczność
    public int CombatMaster; // Mistrz walki
    public int CombatReflexes; // Bitewny refleks
    public int DirtyFighting; // Cios poniżej pasa
    public int Disarm; // Rozbrojenie
    public int DualWielder; // Dwie bronie
    public int FastHands; // Ruchliwe dłonie
    public int Feint; // Finta
    public bool Frenzy; // Szał bojowy
    public int FrenzyAttacksLeft; // Pozostałe ataki w szale bojowym w obecnej rundzie
    public int FuriousAssault; // Wściekły atak
    public int Gunner; // Artylerzysta
    public int Hardy; // Twardziel
    public int HolyHatred; // Święta nienawiść
    public int Implacable; // Nieubłagany
    public int InstinctiveDiction; // Precyzyjne inkantowanie
    public int IronJaw; // Żelazna szczęka
    public int MagicResistance; // Odporność na magię
    public int RapidReload; // Szybkie przeładowanie
    public int ReactionStrike; // Atak wyprzedzający
    public int ReactionStrikesLeft; // Pozostałe ataki wyprzedzające w obecnej rundzie
    public bool Relentless; // Nieustępliwy
    public int Resolute; // Nieugięty
    public int Riposte; // Riposta
    public int RiposteAttacksLeft; // Pozostałe riposty w obecnej rundzie
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
    public int Unshakable; // Niewzruszony
    public int Vaulting; // Woltyżerka
    public bool WarWizard; // Mag bitewny

    [Header("Cechy stworzeń")]
    public bool Bestial; // Zwierzęcy
    public bool Bounce; // Skoczny
    public bool Champion; // Czempion
    public bool CorrosiveBlood; // Kwasowa krew
    public int Daemonic; // Demoniczny
    public bool Distracting; // Dekoncentrujący
    public bool Ethereal; // Eteryczny
    public int Fear; // Strach
    public bool Hungry; // Żarłoczny
    public bool ImmunityToPsychology; // Niewrażliwość na psychologię
    public int NaturalArmor;
    public bool Regeneration; // Regeneracja
    public bool Stride; // Długi krok
    public int Terror; // Groza
    public bool Undead; // Ożywieniec
    public bool Unstable; // Niestabilny
    public bool Vampiric; // Wampiryczny
    public bool Venom; // Jad
    public int VenomModifier; // Siła jadu
    public int Ward; // Ochrona

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

    public string Notebook; // Notatka

    public List<SpellEffect> ActiveSpellEffects = new List<SpellEffect>();

    private void Awake()
    {
        if (Melee == null)
        {
            Melee = new Dictionary<MeleeCategory, int>();
        }

        if (Ranged == null)
        {
            Ranged = new Dictionary<RangedCategory, int>();
        }

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

    public void CalculateMaxHealth(bool isSizeChange = false)
    {
        int previousMaxHealth = MaxHealth;

        // Sprawdzamy, czy SW wynosi 0, w takim przypadku używamy S zamiast SW
        int effectiveSW = (SW == 0) ? S : SW;

        if (Size == SizeCategory.Tiny)
            MaxHealth = 1;
        else if (Size == SizeCategory.Little)
            MaxHealth = Wt / 10;
        else if (Size == SizeCategory.Small)
            MaxHealth = 2 * (Wt / 10) + effectiveSW / 10;
        else if (Size == SizeCategory.Average)
            MaxHealth = S / 10 + 2 * (Wt / 10) + effectiveSW / 10;
        else if (Size == SizeCategory.Large)
            MaxHealth = (S / 10 + 2 * (Wt / 10) + effectiveSW / 10) * 2;
        else if (Size == SizeCategory.Enormous)
            MaxHealth = (S / 10 + 2 * (Wt / 10) + effectiveSW / 10) * 4;
        else if (Size == SizeCategory.Monstrous)
            MaxHealth = (S / 10 + 2 * (Wt / 10) + effectiveSW / 10) * 8;

        // Uwzględnienie talentu Twardziel
        MaxHealth += Hardy * (Wt / 10);

        if (isSizeChange)
        {
            TempHealth = MaxHealth;
        }
        else
        {
            TempHealth += MaxHealth - previousMaxHealth;
        }

        if (GetComponent<Unit>().Stats != null)
        {
            GetComponent<Unit>().DisplayUnitHealthPoints();
        }
    }


    public void ChangeUnitSize(int newSize)
    {
        if (!Enum.IsDefined(typeof(SizeCategory), newSize)) return; // Sprawdzenie poprawności wartości

        SizeCategory previousSize = Size;
        SizeCategory newSizeCategory = (SizeCategory)newSize; // Konwersja int -> SizeCategory

        if (newSizeCategory == previousSize) return;

        int sizeDifference = newSize - (int)previousSize;

        // Aktualizacja statystyk zgodnie z różnicą w rozmiarze
        S = Mathf.Max(0, S + sizeDifference * 10);
        Wt = Mathf.Max(0, Wt + sizeDifference * 10);
        Zw = Mathf.Max(0, Zw - sizeDifference * 5); // Większy rozmiar = mniejsza zręczność

        // Aktualizacja rozmiaru
        Size = newSizeCategory;

        ChangeTokenSize((int)Size);

        // Przeliczenie zdrowia
        CalculateMaxHealth(true);
    }

    public void ChangeTokenSize(int size)
    {
        if (size > 3)
        {
            float tokenSizeModifier = 1f + (size - 3) * 0.25f;
            transform.localScale = new Vector3(tokenSizeModifier, tokenSizeModifier, 1f);
        }
        else if (size < 2)
        {
            transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        }
        else
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
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
        // Zdolność regeneracji
        if (Regeneration)
        {
            int rollResult = UnityEngine.Random.Range(1, 11);
            int currentWounds;

            if (TempHealth < MaxHealth)
            {
                currentWounds = MaxHealth - TempHealth;
            }
            else return;

            int woundsToHeal = 0;

            if (TempHealth == 0)
            {
                if (rollResult >= 8)
                {
                    woundsToHeal = 1;
                }
                else return;
            }
            else
            {
                woundsToHeal = rollResult < currentWounds ? rollResult : currentWounds;
            }

            TempHealth += woundsToHeal;
                
            if (rollResult == 10 && CriticalWounds > 0)
            {
                CriticalWounds--;
                Debug.Log($"{Name} zregenerował/a 1 ranę krytyczną.");
            }

            this.GetComponent<Unit>().DisplayUnitHealthPoints();
            Debug.Log($"{Name} zregenerował/a {woundsToHeal} żywotności.");
        }
    }

    public int CalculateOverall()
    {
        // Sumowanie wszystkich cech głównych
        int primaryStatsSum = WW + US + S + Wt + I + Zw + Zr + Int + SW + Ogd;
        //Debug.Log("primaryStatsSum " + primaryStatsSum);

        // Uwzględnienie rozmiaru (większy rozmiar = większy mnożnik)
        float sizeMultiplier = Mathf.Pow(2f, 1f + (int)Size) / 10; // Każdy poziom rozmiaru zwiększa overall
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
            //Debug.Log("weaponPower " + weaponPower);
        }

        // Zliczanie aktywnych talentów
        int activeTalentsCount = GetType()
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(field => field.FieldType == typeof(bool) && (bool)field.GetValue(this))
            .Count();

        //Debug.Log("activeTalentsCount " + activeTalentsCount);

        // Obliczanie Overall z uwzględnieniem mnożnika rozmiaru
        int overall = Mathf.RoundToInt(((primaryStatsSum / 3) + weaponPower + MaxHealth + Sz + totalArmor * 2 + activeTalentsCount + skillSum / 3) * sizeMultiplier);

        //Debug.Log($"Overall {Name} to {overall}");

        return overall;
    }
    public int GetEffectiveStat(string statName)
    {
        int baseValue = 0;
        // Pobieramy bazową wartość danej statystyki – można odnieść się do właściwego pola
        FieldInfo field = this.GetType().GetField(statName, BindingFlags.Public | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(int))
        {
            baseValue = (int)field.GetValue(this);
        }

        // Sumujemy wszystkie modyfikatory, które dotyczą danej statystyki
        int modifierSum = ActiveSpellEffects
                            .Where(effect => effect.StatModifiers.ContainsKey(statName))
                            .Sum(effect => effect.StatModifiers[statName]);

        return baseValue + modifierSum;
    }

    public void UpdateSpellEffects()
    {
        for (int i = ActiveSpellEffects.Count - 1; i >= 0; i--)
        {
            SpellEffect effect = ActiveSpellEffects[i];
            effect.RemainingRounds--;

            if (effect.RemainingRounds <= 0)
            {
                // Odwrócenie działania efektu – dla każdej modyfikowanej statystyki odejmujemy wartość buffa.
                foreach (var mod in effect.StatModifiers)
                {
                    FieldInfo field = this.GetType().GetField(mod.Key, BindingFlags.Public | BindingFlags.Instance);
                    if (field != null && field.FieldType == typeof(int))
                    {
                        int currentValue = (int)field.GetValue(this);
                        field.SetValue(this, currentValue - mod.Value);
                    }
                    if (field != null && field.FieldType == typeof(bool))
                    {
                        // Załóżmy, że w słowniku mod.Value == 1 oznacza, że buff włączył daną cechę
                        // Aby odwrócić, ustawiamy ją na false – oczywiście, jeśli oryginalna wartość była false.
                        // Jeśli mogło być też true – trzeba to odpowiednio przechowywać (np. jako dodatkowe pole w SpellEffect).
                        field.SetValue(this, false);
                    }

                    if (field != null && field.Name == "NaturalArmor")
                    {
                        InventoryManager.Instance.CheckForEquippedWeapons();
                    }

                }
                Debug.Log($"Efekt zaklęcia {effect.SpellName} oddziałujący na {Name} zakończył się.");
                ActiveSpellEffects.RemoveAt(i);
            }
        }

        if(Unit.SelectedUnit != null)
        {
            UnitsManager.Instance.UpdateUnitPanel(Unit.SelectedUnit);
        }
    }

    //Zwraca kopię tej klasy
    public Stats Clone()
    {
        return (Stats)this.MemberwiseClone();
    }
}
