using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class MagicManager : MonoBehaviour
{
    // Prywatne statyczne pole przechowujące instancję
    private static MagicManager instance;

    // Publiczny dostęp do instancji
    public static MagicManager Instance
    {
        get { return instance; }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            // Jeśli instancja już istnieje, a próbujemy utworzyć kolejną, niszczymy nadmiarową
            Destroy(gameObject);
        }
    }

    [SerializeField] private CustomDropdown _spellbookDropdown;
    [SerializeField] private Button _castSpellButton;
    public List<Spell> SpellBook = new List<Spell>();
    public static bool IsTargetSelecting;
    private float _spellDistance;

    private List<Stats> _targetsStats; // Lista jednostek, które są wybierane jako cele zaklęcia, które pozwala wybrać więcej niż jeden cel
    public List<Stats> UnitsStatsAffectedBySpell; // Lista jednostek, na które w danym momencie wpływa jakieś zaklęcie z czasem trwania, np. Pancerz Eteru

    // Zmienne do przechowywania wyniku
    private int _manualRollResult;
    private bool _isWaitingForRoll;

    void Start()
    {
        //Wczytuje listę wszystkich zaklęć
        DataManager.Instance.LoadAndUpdateSpells();

        _targetsStats = new List<Stats>();
        UnitsStatsAffectedBySpell = new List<Stats>();
    }

    public void ChannelingMagic()
    {
        if (Unit.SelectedUnit == null) return;

        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();
        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();

        //Sprawdzenie, czy wybrana postać może splatać magię
        if (stats.Channeling == 0)
        {
            Debug.Log($"Wybrana jednostka nie potrafi splatać magii.");
            return;
        }

        if (!unit.CanDoAction)
        {
            Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
            return;
        }

        //Wykonuje akcję
        RoundsManager.Instance.DoAction(unit);

        StartCoroutine(ChannelingMagicCoroutine());
        IEnumerator ChannelingMagicCoroutine()
        {
            int rollResult = 0;

            if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "splatanie magii", result => rollResult = result));
                if (rollResult == 0) yield break;
            }
            else
            {
                rollResult = UnityEngine.Random.Range(1, 101);
            }

            // Modyfikator za zbroję
            int modifier = CalculateArmorModifier(stats);

            int[] test = DiceRollManager.Instance.TestSkill("SW", stats, "Channeling", modifier, rollResult);

            unit.ChannelingModifier = Math.Max(0, unit.ChannelingModifier + test[1]);

            Debug.Log($"Poziomy sukcesu zebrane w wyniku splatania magii: <color=#4dd2ff>{unit.ChannelingModifier}</color>");

            CheckForChaosManifestation(stats, rollResult, test[0], test[1]);
        }
    }

    public void CastingSpellMode()
    {
        if (Unit.SelectedUnit == null) return;

        if (Unit.SelectedUnit.GetComponent<Stats>().MagicLanguage == 0)
        {
            Debug.Log("Wybrana jednostka nie może rzucać zaklęć.");
            return;
        }

        if (!Unit.SelectedUnit.GetComponent<Unit>().CanCastSpell)
        {
            Debug.Log("Wybrana jednostka nie może w tej rundzie rzucić więcej zaklęć.");
            return;
        }

        if (_spellbookDropdown.SelectedButton == null)
        {
            Debug.Log("Musisz najpierw wybrać zaklęcie z listy.");
            return;
        }

        GridManager.Instance.ResetColorOfTilesInMovementRange();

        IsTargetSelecting = true;

        string selectedSpellName = _spellbookDropdown.SelectedButton.GetComponentInChildren<TextMeshProUGUI>().text;
        DataManager.Instance.LoadAndUpdateSpells(selectedSpellName);

        _targetsStats.Clear();

        //Zmienia kolor przycisku na aktywny
        _castSpellButton.GetComponent<Image>().color = Color.green;

        Debug.Log("Kliknij prawym przyciskiem myszy na jednostkę, która ma być celem zaklęcia.");
    }

    public IEnumerator CastSpell(GameObject target)
    {
        if (Unit.SelectedUnit == null) yield break;

        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();
        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();
        Spell spell = Unit.SelectedUnit.GetComponent<Spell>();
        Stats targetStats = target.GetComponent<Stats>();
        Unit targetUnit = target.GetComponent<Unit>();

        //Sprawdza dystans
        _spellDistance = CombatManager.Instance.CalculateDistance(Unit.SelectedUnit, target.gameObject);
        float spellRange = spell.Range != 1.5f ? spell.Range * stats.SW : spell.Range; // Zazwyczaj zasięg zaklęcia jest zależny od Siły Woli czarodzieja. Czary dotykowe mają zasięg równy 1.5f
        if (_spellDistance > spellRange)
        {
            Debug.Log("Cel znajduje się poza zasięgiem zaklęcia.");
            yield break;
        }

        Debug.Log($"Dystans: {_spellDistance}. Zasięg zaklęcia: {spellRange}");

        // Pobiera wszystkie collidery w obszarze działania zaklęcia
        List<Collider2D> allTargets = Physics2D.OverlapCircleAll(target.transform.position, spell.AreaSize / 2).ToList();

        // Filtruje wśród colliderów jednostki, na których można użyć tego zaklęcia
        allTargets.RemoveAll(collider =>
            collider.GetComponent<Unit>() == null ||
            (collider.gameObject == Unit.SelectedUnit && spell.Type.Contains("offensive")) ||
            (collider.gameObject != Unit.SelectedUnit && spell.Type.Contains("self-only"))
        );

        if (allTargets.Count == 0)
        {
            Debug.Log("W obszarze działania zaklęcia musi znaleźć się odpowiedni cel.");
            yield break;
        }

        //// W przypadku zaklęć, które atakują wiele celów naraz pozwala na wybranie kilku celów zanim zacznie rzucać zaklęcie
        //if (spell.Type.Contains("multiple-targets") && spell.Type.Contains("magic-level-related") && _targetsStats.Count < spellcasterStats.Mag)
        //{
        //    _targetsStats.Add(allTargets[0].GetComponent<Stats>());

        //    if (_targetsStats.Count < spellcasterStats.Mag)
        //    {
        //        Debug.Log("Wskaż prawym przyciskiem myszy kolejny cel. Możesz wskakać kilkukrotnie tę samą jednostkę.");
        //        return;
        //    }
        //}

        // W przypadku zaklęć, które atakują wiele celów naraz pozwala na wybranie kilku celów zanim zacznie rzucać zaklęcie
        // W RAMACH TESU USTALIWŁEM LIMIT NA SZTYWNO JAKO 3 CELE
        if (spell.Type.Contains("multiple-targets") && _targetsStats.Count < 3)
        {
            _targetsStats.Add(allTargets[0].GetComponent<Stats>());

            // W RAMACH TESU USTALIWŁEM LIMIT NA SZTYWNO JAKO 3 CELE
            if (_targetsStats.Count < 3)
            {
                Debug.Log("Wskaż prawym przyciskiem myszy kolejny cel. Możesz wskakać kilkukrotnie tę samą jednostkę.");
                yield break;
            }
        }

        if (!unit.CanDoAction)
        {
            Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
            yield break;
        }

        RoundsManager.Instance.DoAction(unit);

        //Czary dotykowe (ofensywne)
        if (spell.Range <= 1.5f && spell.Type.Contains("offensive"))
        {
            //Zresetowanie broni, aby zaklęcie dotykowe było wykonywane przy pomocy rąk
            stats.GetComponent<Weapon>().ResetWeapon();
            Weapon attackerWeapon = stats.GetComponent<Weapon>();

            int touchRollResult = 0;

            if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "dotknięcie przeciwnika", result => touchRollResult = result));
                if (touchRollResult == 0) yield break;
            }
            else
            {
                touchRollResult = UnityEngine.Random.Range(1, 101);
            }

            //Uwzględnienie zdolności Dotyk Mocy
            int touchModifier = stats.FastHands * 10;

            int[] attackerTest = DiceRollManager.Instance.TestSkill("WW", stats, MeleeCategory.Brawling.ToString(), touchModifier, touchRollResult);
            int attackerSuccessLevel = attackerTest[1];

            CombatManager.Instance.DefenceResults = new int[2];
            int defenceSuccessValue = 0;
            int defenceSuccessLevel = 0;
            int parryValue = 0;
            int dodgeValue = 0;
            bool canParry = false;
            bool canDodge = false;

            // Sprawdzenie, czy jednostka może próbować parować lub unikać ataku
            canParry = target.GetComponent<Inventory>().EquippedWeapons.Any(weapon => weapon != null && (weapon.Type.Contains("melee") || weapon.Id == 0));
            canDodge = true;
            Weapon targetWeapon = InventoryManager.Instance.ChooseWeaponToAttack(target.gameObject);

            if ((canParry || canDodge) && !targetUnit.Surprised)
            {
                Weapon weaponUsedForParry = CombatManager.Instance.GetBestParryWeapon(targetStats, targetWeapon);
                int parryModifier = CombatManager.Instance.CalculateParryModifier(targetUnit, targetStats, stats, weaponUsedForParry, attackerWeapon);
                int dodgeModifier = CombatManager.Instance.CalculateDodgeModifier(targetUnit, targetStats, attackerWeapon);

                //Modyfikator za strach
                if (targetUnit.FearedUnits.Contains(unit))
                {
                    parryModifier -= 10;
                    dodgeModifier -= 10;
                    Debug.Log($"Uwzględniono modyfikatory za strach przed atakującym.");
                }

                // Ograniczenie modyfikatorów do zakresu od -30 do +60
                parryModifier = Mathf.Clamp(parryModifier, -30, 60);
                dodgeModifier = Mathf.Clamp(dodgeModifier, -30, 60);

                string parryModifierString = parryModifier != 0 ? $" Modyfikator: {parryModifier}," : "";
                string dodgeModifierString = dodgeModifier != 0 ? $" Modyfikator: {dodgeModifier}," : "";

                // Obliczamy sumaryczną wartość parowania i uniku
                MeleeCategory targetMeleeSkill = EnumConverter.ParseEnum<MeleeCategory>(targetWeapon.Category) ?? MeleeCategory.Basic;
                parryValue = targetStats.WW + targetStats.GetSkillModifier(targetStats.Melee, targetMeleeSkill) + parryModifier;
                dodgeValue = targetStats.Dodge + targetStats.Zw + dodgeModifier;

                // Funkcja obrony
                yield return StartCoroutine(CombatManager.Instance.Defense(targetUnit, targetStats, stats, attackerWeapon, weaponUsedForParry, targetMeleeSkill, parryValue, dodgeValue, parryModifier, dodgeModifier, canParry, canDodge));

                defenceSuccessValue = CombatManager.Instance.DefenceResults[0];
                defenceSuccessLevel = CombatManager.Instance.DefenceResults[1];
            }

            // Następuje finalne rozstrzygnięcie
            int combinedSuccessLevel = attackerSuccessLevel - defenceSuccessLevel;

            // Sprawdzenie warunku trafienia
            bool attackSucceeded = combinedSuccessLevel > 0 || (combinedSuccessLevel == 0 && stats.WW + stats.GetSkillModifier(stats.Melee, MeleeCategory.Brawling) > Math.Max(parryValue, dodgeValue));

            if (!attackSucceeded)
            {
                Debug.Log("Atak chybił.");
                yield break;
            }
        }

        int rollResult = 0;
        if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "język magiczny", result => rollResult = result));
        }
        else
        {
            rollResult = UnityEngine.Random.Range(1, 101);
        }

        // Modyfikator za zbroję
        int modifier = CalculateArmorModifier(stats);

        // Test języka magicznego na rzucanie zaklęcia
        int[] castingTest = DiceRollManager.Instance.TestSkill("Int", stats, "MagicLanguage", modifier, rollResult);

        int successLevels = Math.Max(0, castingTest[1] + unit.ChannelingModifier);
        string color = successLevels >= spell.CastingNumber ? "green" : "red"; // Zielony, jeśli >= CastingNumber, inaczej czerwony
        Debug.Log($"{stats.Name} splata zaklęcie. Uzyskane poziomy sukcesu: <color={color}>{successLevels}/{spell.CastingNumber}</color>.");

        bool spellFailed = spell.CastingNumber - successLevels > 0;
        Debug.Log(spellFailed ? "Rzucanie zaklęcia nie powiodło się." : "Zaklęcie zostało splecione pomyślnie.");

        // Zresetowanie zaklęcia
        ResetSpellCasting();
        //_targetsStats.Clear();
        unit.ChannelingModifier = 0;

        // Zaklęcie nie zostało w pełni splecione - przerywamy funkcję
        if (spellFailed) yield break;

        // Wywołanie efektu zaklęcia
        if (spell.Type.Contains("multiple-targets"))
        {
            foreach (Stats tStats in _targetsStats)
            {
                StartCoroutine(HandleSpellEffect(stats, tStats, spell, rollResult, castingTest[1]));
            }
            //_targetsStats.Clear();
        }
        else
        {
            foreach (Collider2D collider in allTargets)
            {
                StartCoroutine(HandleSpellEffect(stats, collider.GetComponent<Stats>(), spell, rollResult, castingTest[1]));
            }
        }
    }

    public void ResetSpellCasting()
    {
        IsTargetSelecting = false;
        //Zmienia kolor przycisku na nieaktywny
        _castSpellButton.GetComponent<Image>().color = Color.white;

        if(Unit.SelectedUnit != null)
        {
            GridManager.Instance.HighlightTilesInMovementRange(Unit.SelectedUnit.GetComponent<Stats>());
        }
    }

    public void ResetSpellEffect(Unit unit)
    {
        for (int i = 0; i < UnitsStatsAffectedBySpell.Count; i++)
        {
            if (unit.UnitId == UnitsStatsAffectedBySpell[i].GetComponent<Unit>().UnitId)
            {
                // Przywraca pierwotne wartości (sprzed działania zaklęcia) dla wszystkich cech. Celowo pomija obecne punkty żywotności, bo mogły ulec zmianie w trakcie działania zaklęcia.
                FieldInfo[] fields = typeof(Stats).GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    if (field.FieldType == typeof(int) && field.Name != "TempHealth")
                    {
                        int currentValue = (int)field.GetValue(unit.GetComponent<Stats>());
                        int otherValue = (int)field.GetValue(UnitsStatsAffectedBySpell[i]);

                        if (currentValue != otherValue)
                        {
                            field.SetValue(unit.GetComponent<Stats>(), otherValue);
                        }
                    }
                }

                UnitsStatsAffectedBySpell.RemoveAt(i);
            }
        }

        UnitsManager.Instance.UpdateUnitPanel(Unit.SelectedUnit);
    }

    private void CheckForChaosManifestation(Stats stats, int rollResult, int successValue, int successLevel)
    {
        bool isSuccessful = successValue >= 0;
        bool hasDouble = DiceRollManager.Instance.IsDoubleDigit(rollResult);
        bool hasZeroOnes = rollResult % 10 == 0;

        if ((isSuccessful && hasDouble && stats.AethyricAttunement == 0) || (!isSuccessful && (hasDouble || hasZeroOnes)))
        {
            int roll = UnityEngine.Random.Range(1, 101);
            int modifier = Math.Max(0, (-successLevel) * 10);
            int finalRoll = roll + modifier;

            if (modifier != 0)
            {
                Debug.Log($"<color=red>Występuje manifestacja Chaosu!</color> Wynik rzutu: {roll}. Modyfikator za poziomy porażki: {modifier}. Poziom manifestacji: <color=red>{finalRoll}</color>.");
            }
            else
            {
                Debug.Log($"<color=red>Występuje manifestacja Chaosu!</color> Wynik rzutu na manifestację: <color=red>{finalRoll}</color>.");
            }
        }
    }

    private int CalculateArmorModifier(Stats stats)
    {
        int modifier = 0;
        bool etherArmor = false;

        if (UnitsStatsAffectedBySpell != null && UnitsStatsAffectedBySpell.Count > 0)
        {
            //Przeszukanie statystyk jednostek, na które działają zaklęcia czasowe
            for (int i = 0; i < UnitsStatsAffectedBySpell.Count; i++)
            {
                //Jeżeli wcześniejsza wartość zbroi (w tym przypadku na głowie, ale to może być dowolna lokalizacja) jest inna niż obecna, świadczy to o użyciu Pancerzu Eteru
                if (UnitsStatsAffectedBySpell[i].Name == stats.Name && UnitsStatsAffectedBySpell[i].Armor_head != stats.Armor_head)
                {
                    etherArmor = true;
                }
            }
        }

        //Uwzględnienie ujemnego modyfikatora za zbroję (z wyjątkiem Pancerza Eteru)
        if (etherArmor == false)
        {
            int[] armors = { stats.Armor_head, stats.Armor_arms, stats.Armor_torso, stats.Armor_legs };
            int armouredCastingModifier = stats.ArmouredCasting == true ? 3 : 0;
            modifier -= Math.Max(0, armors.Max() - armouredCastingModifier); //Odejmuje największa wartość zbroi i uwzględnia Pancerz Wiary
        }

        return modifier * 10;
    }

    private IEnumerator HandleSpellEffect(Stats spellcasterStats, Stats targetStats, Spell spell, int rollResult, int successLevel)
    {
        Unit targetUnit = targetStats.GetComponent<Unit>();

        //Uwzględnienie czasu trwania zaklęcia, które wpływa na statystyki postaci
        if (spell.Duration != 0 && spell.Type.Contains("buff"))
        {
            //Zakończenie wpływu poprzedniego zaklęcia, jeżeli na wybraną jednostkę już jakieś działało. JEST TO ZROBIONE TYMCZASOWO. TEN LIMIT ZOSTAŁ WPROWADZONY DLA UPROSZCZENIA KODU.
            if (UnitsStatsAffectedBySpell != null && UnitsStatsAffectedBySpell.Any(stat => stat.GetComponent<Unit>().UnitId == targetUnit.UnitId))
            {
                ResetSpellEffect(targetUnit);
                Debug.Log($"Poprzednie zaklęcie wpływające na {targetStats.Name} zostało zresetowane. W obecnej wersji symulatora nie ma możliwości kumulowania efektów wielu zaklęć.");
            }

            targetUnit.SpellDuration = spell.Duration;

            UnitsStatsAffectedBySpell.Add(targetStats.Clone());
        }

        //Uwzględnienie testu obronnego
        if (spell.SaveTestRequiring == true && spell.Attribute.Length > 0)
        {
            //Szuka odpowiedniej cechy w statystykach celu
            FieldInfo field = targetStats.GetType().GetField(spell.Attribute[0]);

            if (field == null || field.FieldType != typeof(int)) yield break;

            int value = (int)field.GetValue(targetStats);

            int saveRollResult = UnityEngine.Random.Range(1, 101);

            if (saveRollResult > value)
            {
                Debug.Log($"{targetStats.Name} wykonał test na {spell.Attribute[0]} i wyrzucił {saveRollResult}. Wartość cechy: {value}. Nie udało mu się przeciwstawić zaklęciu.");
            }
            else
            {
                Debug.Log($"{targetStats.Name} wykonał test na {spell.Attribute[0]} i wyrzucił {saveRollResult}. Wartość cechy: {value}. Udało mu się przeciwstawić zaklęciu.");
                yield break;
            }
        }

        // Uwzględnienie testu obronnego
        if (spell.SaveTestRequiring == true && spell.Attribute.Length > 0)
        {
            // Szuka odpowiedniej cechy w statystykach celu
            string attributeName = spell.Attribute[0];

            // Rzut na test obronny
            int saveRollResult = 0;
            if (!GameManager.IsAutoDiceRollingMode && targetStats.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, $"rzut obronny ({attributeName})", result => saveRollResult = result));
                if (saveRollResult == 0) yield break;
            }
            else
            {
                saveRollResult = UnityEngine.Random.Range(1, 101);
            }

            // Test obronny przy użyciu TestSkill
            int[] skillTestResults = DiceRollManager.Instance.TestSkill(attributeName, targetStats, null, 0, saveRollResult);

            if (skillTestResults[0] < 0)
            {
                Debug.Log($"{targetStats.Name} nie udało mu się przeciwstawić zaklęciu.");
            }
            else
            {
                Debug.Log($"{targetStats.Name} udało mu się przeciwstawić zaklęciu.");
                yield break;
            }
        }
        else if (spell.Attribute != null && spell.Attribute.Length > 0) // Zaklęcia wpływające na cechy, np. Uzdrowienie i Pancerz Eteru
        {
            for (int i = 0; i < spell.Attribute.Length; i++)
            {
                //Szuka odpowiedniej cechy w statystykach celu
                FieldInfo field = targetStats.GetType().GetField(spell.Attribute[i]);

                if (field == null || field.FieldType != typeof(int)) yield break;

                int value = spell.Strength;
                
                // TO PRAWDOPODOBNIE BĘDZIE ZALEŻNE OD POZIOMÓW SUKCESU LUB BONUSU Z JAKIEJŚ CECHY
                // if (spell.Type.Contains("magic-level-related"))
                // {
                //     value += spellcasterStats.Mag;
                // }

                // Zaklęcia leczące
                if (spell.Attribute[0] == "TempHealth")
                {
                    // Zapobiega leczeniu ponad maksymalną wartość żywotności
                    if (value + targetStats.TempHealth > targetStats.MaxHealth)
                    {
                        value = targetStats.MaxHealth - targetStats.TempHealth;
                    }

                    field.SetValue(targetStats, (int)field.GetValue(targetStats) + value);

                    //Zaktualizowanie punktów żywotności
                    targetStats.GetComponent<Unit>().DisplayUnitHealthPoints();
                    UnitsManager.Instance.UpdateUnitPanel(Unit.SelectedUnit);

                    Debug.Log($"{targetStats.Name} odzyskał {value} punktów Żywotności.");
                    yield break;
                }

                field.SetValue(targetStats, (int)field.GetValue(targetStats) + value);
            }

            UnitsManager.Instance.UpdateUnitPanel(Unit.SelectedUnit);
        }

        //Zaklęcia zadające obrażenia
        if (!spell.Type.Contains("no-damage") && spell.Type.Contains("offensive"))
        {
            DealMagicDamage(spellcasterStats, targetStats, spell, rollResult, successLevel);
        }
    }

    private void DealMagicDamage(Stats spellcasterStats, Stats targetStats, Spell spell, int rollResult, int successLevel)
    {
        int damage = successLevel + spell.Strength;
        Debug.Log($"Poziom sukcesu {spellcasterStats.Name}: {successLevel}. Siła zaklęcia: {spell.Strength}");

        //Ustalamy miejsce trafienia
        string hitLocation = !String.IsNullOrEmpty(CombatManager.Instance.HitLocation) ? CombatManager.Instance.HitLocation : (DiceRollManager.Instance.IsDoubleDigit(rollResult) ? CombatManager.Instance.DetermineHitLocation() : CombatManager.Instance.DetermineHitLocation(rollResult));

        // Sprawdzamy zbroję
        int armor = CombatManager.Instance.CalculateArmor(spellcasterStats, targetStats, hitLocation, rollResult);

        if (spell.ArmourIgnoring) armor = 0;

        CombatManager.Instance.ApplyDamageToTarget(damage, armor, spellcasterStats, targetStats, targetStats.GetComponent<Unit>());
    }
}