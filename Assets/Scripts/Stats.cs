using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public enum SizeCategory
{
    Little = 0,    // drobny
    Small = 1,         // mały
    Average = 2,       // średni
    Big = 3,         // duży
    Large = 4       // wielki
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

    [Header("Type")]
    public string Type;

    [Header("Rozmiar")]
    public SizeCategory Size; // Rozmiar

    [Header("Nazwy początkowych broni")]
    public List<string> PrimaryWeaponNames = new List<string>();
    public List<PairString> PrimaryWeaponAttributes = new List<PairString>();

    [Header("Cechy")]
    public int S;
    public int K;
    public int Zw;
    public int Zr;
    public int Int;
    public int P;
    public int Ch;
    public int SW;

    [Header("Cechy drugorzędowe")]
    public int Sz;
    public int TempSz;
    public int MaxHealth;
    public int TempHealth;
    public int CriticalWounds; // Ilość Ran Krytycznych
    public int SinPoints; // Punkty Grzechu (istotne dla kapłanów)
    public int TempPL; // Punkty losu aktualne
    public int MaxPL; // Punkty Losu Maksymalne
    public int PB; // Punkty Bohatera
    public int ExtraPoints; // Dodatkowe punkty do rozdania między PL a PB
    public int Initiative; // Inicjatywa
    public int CurrentEncumbrance; // Aktualne obciążenie ekwipunkiem
    public int MaxEncumbrance; // Maksymalny udźwig
    public int ExtraEncumbrance; // Dodatkowe obciążenie za przedmioty niebędące uzbrojeniem

    [Header("Zbroja")]
    public int Armor_head;
    public int Armor_arms;
    public int Armor_torso;
    public int Armor_legs;

    public int ArmorPenaltyZw; // bieżąca kara z pancerza zastosowana do Zw
    public int ArmorPenaltyP;  // bieżąca kara z pancerza zastosowana do P


    [Header("Umiejętności")]
    public int Athletics; // Atletyka
    public int Cool; // Opanowanie
    public int Dodge; // Unik
    public int Endurance; // Odporność
    public int MeleeCombat; // Walka Wręcz
    public int RangedCombat; // Walka Dystansowa
    public int Reflex; // Refleks
    public int Spellcasting; // Rzucanie zaklęć

    public int Pray; // Modlitwa
    public int Channeling; // Splatanie magii
    public int MagicLanguage; // Język magiczny


    [Header("Talenty")]
    public bool CombatMaster; // Wojownik
    public bool Fast; // Szybki
    public int Pitiless; // Bezlitosny
    public bool Sharpshooter; // Strzelec wyborowy
    public int SurvivalInstinct; // Instynkt Przetrwania

    public string[] Resistance = new string[4]; // np. ["Fizyczne", "Ogień"]
    public string[] Slayer = new string[3];
    public string[] Specialist = new string[3]; // null/"" = pusty slot

    public int AethyricAttunement; // Zmysł Magii
    public int AccurateShot; // Celny strzał
    public int Ambidextrous; // Oburęczność
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
    public bool Hardy; // Wytrzymały
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
    public int Scary; // Straszny


    public bool Belligerent; // Wojowniczy
    public bool Bestial; // Zwierzęcy
    public bool Bounce; // Skoczny
    public bool Champion; // Czempion
    public bool CorrosiveBlood; // Kwasowa krew
    public int Daemonic; // Demoniczny
    public bool Distracting; // Dekoncentrujący
    public bool Ethereal; // Eteryczny
    public int Fear; // Strach
    public int Flight; // Latanie
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
        Overall = CalculateOverall();
    }

    public void SetBaseStats()
    {
        CalculateMaxHealth();

        // Rozdzielanie punktów ExtraPoints losowo pomiędzy PP i Resilience
        for (int i = 0; i < ExtraPoints; i++)
        {
            if (UnityEngine.Random.value < 0.5f)
                MaxPL++;
            else
                PB++;
        }
        ExtraPoints = 0;

        TempPL = MaxPL;

        if (Fast) Sz += 2;

        // Aktualizuje udźwig
        MaxEncumbrance = Math.Max(1, 6 + S);
    }

    public void CalculateMaxHealth(bool isSizeChange = false)
    {
        int previousMaxHealth = MaxHealth;

        if (Size == SizeCategory.Little)
            MaxHealth = 1;
        else if (Size == SizeCategory.Small)
            MaxHealth = 10 + S + K;
        else if (Size == SizeCategory.Average)
            MaxHealth = 12 + S + K;
        else if (Size == SizeCategory.Big)
            MaxHealth = 18 + S + K;
        else if (Size == SizeCategory.Large)
            MaxHealth = 2 * (18 + S + K);

        // Uwzględnienie cechy specjalnej Wytrzymały
        if (Hardy == true) MaxHealth *= 2;

        if (MaxHealth < 1) MaxHealth = 1;

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

        // Aktualizacja rozmiaru
        Size = newSizeCategory;

        ChangeTokenSize((int)Size);

        // Przeliczenie zdrowia
        CalculateMaxHealth(true);
    }

    public void ChangeTokenSize(int size)
    {
        if (size > 2)
        {
            float tokenSizeModifier = 1f + (size - 2) * 0.25f;
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

    public int CalculateOverall()
    {
        // Sumowanie wszystkich cech głównych
        int primaryStatsSum = S + K + Zw + Zr + Int + P + Ch + SW;
        //Debug.Log("primaryStatsSum " + primaryStatsSum);

        //// Uwzględnienie rozmiaru (większy rozmiar = większy mnożnik)
        //float sizeMultiplier = Mathf.Pow(2f, 1f + (int)Size) / 10; // Każdy poziom rozmiaru zwiększa overall
        ////Debug.Log("sizeMultiplier " + sizeMultiplier);

        // Sumowanie zbroi
        int totalArmor = Armor_head + Armor_arms + Armor_torso + Armor_legs;
        //Debug.Log("totalArmor " + totalArmor);

        // Uwzględnienie umiejętności
        int skillSum = MeleeCombat + RangedCombat + Athletics + Spellcasting + Dodge;
        //Debug.Log("skillSum " + skillSum);


        // Uwzględnienie mocy broni
        int weaponPower = 0;
        Weapon weapon = InventoryManager.Instance.ChooseWeaponToAttack(this.gameObject);
        if (weapon != null)
        {
            weaponPower += weapon.S * 8;
            //Debug.Log("weaponPower " + weaponPower);
        }

        // Zliczanie aktywnych talentów
        int activeTalentsCount = GetType()
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(field => field.FieldType == typeof(bool) && (bool)field.GetValue(this))
            .Count();

        //Debug.Log("activeTalentsCount " + activeTalentsCount);

        // Obliczanie Overall z uwzględnieniem mnożnika rozmiar
        int overall = Mathf.RoundToInt(primaryStatsSum + weaponPower + MaxHealth + Sz + totalArmor + activeTalentsCount + skillSum); // * sizeMultiplier);

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
                    Unit affectedUnit = GetComponent<Unit>();

                    // Szukamy pola najpierw w Stats
                    FieldInfo field = this.GetType().GetField(mod.Key, BindingFlags.Public | BindingFlags.Instance);
                    object targetObject = this;

                    // Jeśli nie znaleziono w Stats, próbujemy znaleźć w Unit
                    if (field == null && affectedUnit != null)
                    {
                        field = affectedUnit.GetType().GetField(mod.Key, BindingFlags.Public | BindingFlags.Instance);
                        targetObject = affectedUnit;
                    }

                    if (field == null) continue;

                    if (field.FieldType == typeof(int))
                    {
                        int currentValue = (int)field.GetValue(targetObject);
                        field.SetValue(targetObject, currentValue - mod.Value);
                    }
                    if (field.FieldType == typeof(bool))
                    {
                        // Załóżmy, że w słowniku mod.Value == 1 oznacza, że buff włączył daną cechę
                        // Aby odwrócić, ustawiamy ją na false – oczywiście, jeśli oryginalna wartość była false.
                        // Jeśli mogło być też true – trzeba to odpowiednio przechowywać (np. jako dodatkowe pole w SpellEffect).
                        field.SetValue(targetObject, false);
                    }

                    if (field.Name == "NaturalArmor")
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

    //Sprawdza, czy postać specjalizuje się z danej rzeczy
    public bool HasSpecialist(string skill) => !string.IsNullOrEmpty(skill) && Specialist != null && Specialist.Any(s => s == skill);

    public bool HasSlayer(string skill) => !string.IsNullOrEmpty(skill) && Slayer != null && Slayer.Any(s => s == skill);

    //Zwraca kopię tej klasy
    public Stats Clone()
    {
        return (Stats)this.MemberwiseClone();
    }
}
