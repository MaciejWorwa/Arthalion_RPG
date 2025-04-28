using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

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
    [SerializeField] private UnityEngine.UI.Button _castSpellButton;
    public List<Spell> SpellBook = new List<Spell>();
    [SerializeField] private UnityEngine.UI.Toggle _grimuarToggle;
    public static bool IsTargetSelecting;
    private float _spellDistance;

    public HashSet<GameObject> Targets = new HashSet<GameObject>();
    private List<Stats> _targetsStats; // Lista jednostek, które są wybierane jako cele zaklęcia, które pozwala wybrać więcej niż jeden cel

    [Header("Panele do manualnego zarządzania")]
    [SerializeField] private GameObject _dispellPanel;
    private bool _wantsToDispell;
    private bool _dispellDone;

    // DO WPROWADZENIA
    [Header("Panel do manualnego zarządzania krytycznym splecieniem zaklęcia")]
    [SerializeField] private GameObject _criticalCastingPanel;
    [SerializeField] private UnityEngine.UI.Button _criticalWoundButton; // Zadaje dodatkowo ranę krytyczną, jeśli to zaklęcie zadające obrażenia
    [SerializeField] private UnityEngine.UI.Button _forceCastButton; // Zaklęcie jest rzucone mimo niewystarczającego poziomu sukcesu
    [SerializeField] private UnityEngine.UI.Button _antiDispellButton; // Zaklęcie nie może być rozproszone
    private string _criticalCastingString;

    // DO WPROWADZENIA
    [Header("Panel do manualnego zarządzania overcastingiem")]
    [SerializeField] private GameObject _overcastingPanel;
    [SerializeField] private UnityEngine.UI.Button _extraTargetButton;
    [SerializeField] private UnityEngine.UI.Button _extraDamageButton;
    [SerializeField] private UnityEngine.UI.Button _extraRangeButton;
    [SerializeField] private UnityEngine.UI.Button _extraAreaSizeButton;
    [SerializeField] private UnityEngine.UI.Button _extraDurationButton;
    private List<string> _overcastingStrings = new List<string>();
    private int _overcastingLevel;
    [SerializeField] private TMP_Text _overcastingLevelDisplay;

    [Header("Tradycje magii")]
    [SerializeField] private List<UnityEngine.UI.Toggle> _arcanesToggles;
    [SerializeField] private UnityEngine.UI.Toggle _aqshyToggle;
    [SerializeField] private UnityEngine.UI.Toggle _azyrToggle;
    [SerializeField] private UnityEngine.UI.Toggle _chamonToggle;
    [SerializeField] private UnityEngine.UI.Toggle _ghurToggle;
    [SerializeField] private UnityEngine.UI.Toggle _ghyranToggle;
    [SerializeField] private UnityEngine.UI.Toggle _hyshToggle;
    [SerializeField] private UnityEngine.UI.Toggle _shyishToggle;
    [SerializeField] private UnityEngine.UI.Toggle _ulguToggle;

    void Start()
    {
        //Wczytuje listę wszystkich zaklęć
        DataManager.Instance.LoadAndUpdateSpells();

        _targetsStats = new List<Stats>();

        _arcanesToggles = new List<UnityEngine.UI.Toggle> {
        _aqshyToggle, _azyrToggle, _chamonToggle,
        _ghurToggle, _ghyranToggle, _hyshToggle,
        _shyishToggle, _ulguToggle
        };

        foreach (var toggle in _arcanesToggles)
        {
            toggle.onValueChanged.AddListener((isOn) => OnArcaneToggleChanged(toggle, isOn));
        }

        _criticalWoundButton.onClick.AddListener(() => CriticalCastingButtonClick("critical_wound"));
        _forceCastButton.onClick.AddListener(() => CriticalCastingButtonClick("force_cast"));
        _antiDispellButton.onClick.AddListener(() => CriticalCastingButtonClick("anti_dispell"));

        _extraTargetButton.onClick.AddListener(() => OvercastingButtonClick(_extraTargetButton.gameObject, "target"));
        _extraDamageButton.onClick.AddListener(() => OvercastingButtonClick(_extraDamageButton.gameObject, "damage"));
        _extraRangeButton.onClick.AddListener(() => OvercastingButtonClick(_extraRangeButton.gameObject, "range"));
        _extraAreaSizeButton.onClick.AddListener(() => OvercastingButtonClick(_extraAreaSizeButton.gameObject, "area_size"));
        _extraDurationButton.onClick.AddListener(() => OvercastingButtonClick(_extraDurationButton.gameObject, "duration"));
    }

    #region Channeling magic
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

            // Modyfikator za zbroję i talent Zmysł Magii
            int modifier = CalculateArmorModifier(stats);

            int[] test = DiceRollManager.Instance.TestSkill("SW", stats, "Channeling", modifier, rollResult);

            // Talent Zmysł Magii
            if (test[0] >= 0 && stats.AethyricAttunement > 0)
            {
                test[1] += stats.AethyricAttunement;
                Debug.Log($"Poziom sukcesu {stats.Name} wzrasta do <color=green>{test[1]}</color> za talent \"Zmysł Magii.\"");
            }

            unit.ChannelingModifier = Math.Max(0, unit.ChannelingModifier + test[1]);

            Debug.Log($"Poziomy sukcesu zebrane w wyniku splatania magii: <color=#4dd2ff>{unit.ChannelingModifier}</color>");

            CheckForChaosManifestation(stats, rollResult, test[0], test[1], "Channeling");
        }
    }
    #endregion

    #region Casting
    public void CastingSpellMode()
    {
        if (Unit.SelectedUnit == null) return;

        if (Unit.SelectedUnit.GetComponent<Stats>().MagicLanguage == 0)
        {
            Debug.Log("Wybrana jednostka nie może rzucać zaklęć.");
            return;
        }

        if (!Unit.SelectedUnit.GetComponent<Unit>().CanCastSpell && RoundsManager.RoundNumber != 0)
        {
            Debug.Log("Wybrana jednostka nie może w tej rundzie rzucić więcej zaklęć.");
            return;
        }

        if (_spellbookDropdown.SelectedButton == null)
        {
            Debug.Log("Musisz najpierw wybrać zaklęcie z listy.");
            return;
        }

        Targets.Clear();
        string selectedSpellName = _spellbookDropdown.SelectedButton.GetComponentInChildren<TextMeshProUGUI>().text;
        DataManager.Instance.LoadAndUpdateSpells(selectedSpellName);

        if (!Unit.SelectedUnit.GetComponent<Unit>().CanDoAction && (!Unit.SelectedUnit.GetComponent<Stats>().WarWizard || Unit.SelectedUnit.GetComponent<Spell>().CastingNumber > 5))
        {
            Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
            return;
        }

        if (!Unit.SelectedUnit.GetComponent<Stats>().WarWizard || Unit.SelectedUnit.GetComponent<Spell>().CastingNumber > 5)
        {
            RoundsManager.Instance.DoAction(Unit.SelectedUnit.GetComponent<Unit>());
        }

        StartCoroutine(CastSpell());
    }

    public IEnumerator CastSpell()
    {
        if (Unit.SelectedUnit == null) yield break;

        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();
        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();
        Spell spell = Unit.SelectedUnit.GetComponent<Spell>();

        SetArcaneToggle(spell);

        unit.CanCastSpell = false;
        _criticalCastingString = "";
        _overcastingStrings.Clear();
        _dispellDone = false;

        int rollResult = 0;
        if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, spell.Arcane == "Cuda" || spell.Arcane == "Błogosławieństwa" ? "modlitwę" : "język magiczny", result => rollResult = result));
        }
        else
        {
            rollResult = UnityEngine.Random.Range(1, 101);
        }

        // Modyfikator za zbroję
        int modifier = CalculateArmorModifier(stats);

        // Modyfikator za poziomy podpalenia w pobliżu (tylko dla Tradycji Ognia)
        if (_aqshyToggle.isOn)
        {
            int totalAblaze = 0; // Zmienna do sumowania wartości poziomów podpaleń
            float maxDistance = (stats.SW / 10) / 2f; // Maksymalny zasięg uwzględniający modyfikator za podpalenia
            foreach (var entry in InitiativeQueueManager.Instance.InitiativeQueue)
            {
                Unit u = entry.Key;
                Stats unitStats = u.GetComponent<Stats>();

                // Sprawdzamy, czy jednostka ma Ablaze większe niż 0 i znajduje się w odpowiednim zasięgu od rzucającego zaklęcie
                if (u.Ablaze > 0 && !ReferenceEquals(unitStats, stats))
                {
                    float distance = Vector2.Distance(u.transform.position, stats.transform.position);
                    if (distance <= maxDistance)
                    {
                        totalAblaze += u.Ablaze;
                    }
                }
            }

            if (totalAblaze > 0)
            {
                modifier += totalAblaze * 10;
                Debug.Log($"Modyfikator z Tradycji Ognia za podpalenia w pobliżu: {totalAblaze * 10}.");
            }
        }

        // Test języka magicznego na rzucanie zaklęcia lub test modlitwy na rzucanie cudu lub błogosławieństwa
        int[] castingTest = spell.Arcane == "Cuda" || spell.Arcane == "Błogosławieństwa" ? DiceRollManager.Instance.TestSkill("Ogd", stats, "Pray", modifier, rollResult) : DiceRollManager.Instance.TestSkill("Int", stats, "MagicLanguage", modifier, rollResult);

        // Talent Precyzyjne Inkantowanie
        if (castingTest[0] >= 0 && stats.InstinctiveDiction > 0 && spell.Arcane != "Cuda" && spell.Arcane != "Błogosławieństwa")
        {
            castingTest[1] += stats.InstinctiveDiction;
            Debug.Log($"Poziom sukcesu {stats.Name} wzrasta do <color=green>{castingTest[1]}</color> za talent \"Precyzyjne Inkantowanie.\"");
        }

        int castingNumberRequired = _grimuarToggle.isOn ? spell.CastingNumber * 2 : spell.CastingNumber;

        int successLevels = castingTest[1] + unit.ChannelingModifier;
        string color = successLevels >= castingNumberRequired ? "green" : "red"; // Zielony, jeśli >= CastingNumber, inaczej czerwony
        Debug.Log($"{stats.Name} splata zaklęcie. Uzyskane poziomy sukcesu: <color={color}>{successLevels}/{castingNumberRequired}</color>.");

        bool spellFailed = castingNumberRequired - successLevels > 0;
        Debug.Log(spellFailed ? $"Rzucanie zaklęcia {spell.Name} nie powiodło się." : $"Zaklęcie {spell.Name} zostało splecione pomyślnie.");

        if (unit.ChannelingModifier > 0 && spellFailed) //Jeśli czarodziej splatał wcześniej magię i zaklęcie nie powiodło się, występuję manifestacja Chaosu (od nadmiaru zebranej magii)
        {
            CheckForChaosManifestation(stats, rollResult, castingTest[0], castingTest[1], spell.Arcane == "Cuda" || spell.Arcane == "Błogosławieństwa" ? "Pray" : "MagicLanguage", castingNumberRequired - successLevels, true);
        }
        else // Standardowe sprawdzenie warunków manifestacji
        {
            CheckForChaosManifestation(stats, rollResult, castingTest[0], castingTest[1], spell.Arcane == "Cuda" || spell.Arcane == "Błogosławieństwa" ? "Pray" : "MagicLanguage", castingNumberRequired - successLevels);
        }

        // Zresetowanie zaklęcia
        ResetSpellCasting();
        unit.ChannelingModifier = 0;

        // Krytyczne rzucenie zaklęcia
        if (DiceRollManager.Instance.IsDoubleDigit(rollResult) && rollResult <= stats.Int + stats.MagicLanguage)
        {
            StartCoroutine(CriticalCastingRoll(spell, spellFailed));
        }

        if(successLevels - castingNumberRequired > 0)
        {
            StartCoroutine(Overcasting(spell, successLevels - castingNumberRequired));
        }

        while (_criticalCastingPanel.activeSelf || _overcastingPanel.activeSelf)
        {
            yield return null;
        }

        // Zaklęcie nie zostało w pełni splecione - przerywamy funkcję
        if (spellFailed && _criticalCastingString != "force_cast") yield break;

        GridManager.Instance.ResetColorOfTilesInMovementRange();

        IsTargetSelecting = true;
        _targetsStats.Clear();

        //Zmienia kolor przycisku na aktywny
        _castSpellButton.GetComponent<UnityEngine.UI.Image>().color = Color.green;

        Debug.Log("Kliknij prawym przyciskiem myszy na jednostkę, która ma być celem zaklęcia.");

        if(spell.Type.Contains("targets-scaling-by-Int"))
        {
            spell.Targets = stats.Int / 10;
        }

        while (Targets.Count < spell.Targets)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) break;
            yield return null;
        }

        foreach (GameObject target in Targets)
        {
            Stats targetStats = target.GetComponent<Stats>();
            Unit targetUnit = target.GetComponent<Unit>();

            //Sprawdza dystans
            _spellDistance = CombatManager.Instance.CalculateDistance(Unit.SelectedUnit, target);
            float baseStat = spell.Arcane == "Cuda" ? stats.Ogd : stats.SW;
            float spellRange = spell.Range == 1.5f || spell.Arcane == "Błogosławieństwa" ? spell.Range : spell.Range * baseStat / 2f; // Zazwyczaj zasięg zaklęcia jest zależny od Siły Woli lub Ogłady czarodzieja. Czary dotykowe mają zasięg równy 1.5f
            Debug.Log($"Dystans: {_spellDistance}. Zasięg zaklęcia: {spellRange}");

            if (_spellDistance > spellRange)
            {
                Debug.Log($"{targetStats.Name} znajduje się poza zasięgiem zaklęcia.");
                continue;
                //ResetSpellCasting();
                //unit.CanCastSpell = true;
                //yield break;
            }

            // Ustala obszar działania zaklęcia. Zwykle jest to mnożnik bonusu z Siły Woli
            float areaSize = spell.Type.Contains("constant-area-size") ? spell.AreaSize : spell.AreaSize * (stats.SW / 10) / 2f;

            // Pobiera wszystkie collidery w obszarze działania zaklęcia
            List<Collider2D> allTargets = Physics2D.OverlapCircleAll(target.transform.position, areaSize).ToList();

            // Filtruje wśród colliderów jednostki, na których można użyć tego zaklęcia
            allTargets.RemoveAll(collider =>
                collider.GetComponent<Unit>() == null ||
                (collider.gameObject == Unit.SelectedUnit && spell.Type.Contains("offensive")) ||
                (collider.gameObject != Unit.SelectedUnit && spell.Type.Contains("self-only"))
            );

            if (allTargets.Count == 0)
            {
                Debug.Log("W obszarze działania zaklęcia musi znaleźć się odpowiedni cel.");
                continue;
                //ResetSpellCasting();
                //unit.CanCastSpell = true;
                //yield break;
            }

            //Czary dotykowe (ofensywne)
            if (spell.Range == 1.5f && spell.Type.Contains("offensive"))
            {
                //Zresetowanie broni, aby zaklęcie dotykowe było wykonywane przy pomocy rąk
                stats.GetComponent<Weapon>().ResetWeapon();
                Weapon attackerWeapon = stats.GetComponent<Weapon>();

                int touchRollResult = 0;

                if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
                {
                    yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "dotknięcie przeciwnika", result => touchRollResult = result));
                    if (touchRollResult == 0) continue;
                }
                else
                {
                    touchRollResult = UnityEngine.Random.Range(1, 101);
                }

                int[] attackerTest = DiceRollManager.Instance.TestSkill("WW", stats, MeleeCategory.Brawling.ToString(), 0, touchRollResult);

                // Talent Ruchliwe Dłonie
                if (attackerTest[0] >= 0 && stats.FastHands > 0)
                {
                    attackerTest[1] += stats.FastHands;
                    Debug.Log($"Poziom sukcesu {stats.Name} wzrasta do <color=green>{attackerTest[1]}</color> za talent \"Ruchliwe Dłonie.\"");
                }

                int attackerSuccessLevel = attackerTest[1];

                CombatManager.Instance.DefenceResults = new int[2];
                int defenceSuccessValue = 0;
                int defenceSuccessLevel = 0;
                int parryValue = 0;
                int dodgeValue = 0;
                bool canParry = false;
                bool canDodge = false;

                // Sprawdzenie, czy jednostka może próbować parować lub unikać ataku
                canParry = target.GetComponent<Inventory>().EquippedWeapons.Any(weapon => weapon != null && !targetStats.Bestial && (weapon.Type.Contains("melee") || weapon.Id == 0));
                canDodge = true;
                Weapon targetWeapon = InventoryManager.Instance.ChooseWeaponToAttack(target);

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
                    Debug.Log($"Atak skierowany w {targetStats.Name} chybił.");
                    //ResetSpellCasting();
                    continue;
                }
            }

            // Sprawdzenie, czy ktoś z przeciwnej drużyny potrafi rozpraszać magię i wybranie najlepszego z nich (w rozpraszaniu)
            Stats dispeller = null;
            foreach (Unit u in UnitsManager.Instance.AllUnits)
            {
                if (!u.CompareTag(unit.tag) && u.CanDispell && (dispeller == null || dispeller.MagicLanguage + dispeller.Int < u.GetComponent<Stats>().MagicLanguage + u.GetComponent<Stats>().Int))
                {
                    dispeller = u.GetComponent<Stats>();
                }
            }

            int successLevelAfterDispell = 0;

            // Próba rozproszenia zaklęcia
            if (dispeller != null && !_dispellDone && _criticalCastingString != "anti_dispell")
            {
                StartCoroutine(Dispell(dispeller, spell, castingTest[1], result =>
                {
                    successLevelAfterDispell = result;
                }));

                _dispellDone = true;
            }
            else
            {
                successLevelAfterDispell = castingTest[1];
            }

            while (_dispellPanel.activeSelf)
            {
                yield return null;
            }

            if (successLevelAfterDispell < 0 || (successLevelAfterDispell == 0 && dispeller.MagicLanguage + dispeller.Int > stats.MagicLanguage + stats.Int))
            {
                Debug.Log($"{dispeller.Name} rozproszył zaklęcie rzucane przez {stats.Name}.");
                ResetSpellCasting();
                yield break;
            }

            int finalSuccessLevel = successLevelAfterDispell;

            // Wywołanie efektu zaklęcia
            foreach (var collider in allTargets)
            {
                // Uwzględnienie talentu Odporność na magię
                if(collider.GetComponent<Stats>().MagicResistance != 0)
                {
                    finalSuccessLevel -= collider.GetComponent<Stats>().MagicResistance * 2;
                    Debug.Log($"{collider.GetComponent<Stats>().Name} posiada talent \"Odporność na magię\" o wartości {collider.GetComponent<Stats>().MagicResistance}. PS zaklęcia został pomniejszony o {collider.GetComponent<Stats>().MagicResistance * 2}.");
                    if (finalSuccessLevel < 0 && _criticalCastingString != "force_cast")
                    {
                        Debug.Log($"Zaklęcie nie wywołuje wpływu na {collider.GetComponent<Stats>().Name}.");
                        continue;
                    }

                    StartCoroutine(HandleSpellEffect(stats, collider.GetComponent<Stats>(), spell, rollResult, finalSuccessLevel));
                }
                else
                {
                    StartCoroutine(HandleSpellEffect(stats, collider.GetComponent<Stats>(), spell, rollResult, successLevelAfterDispell));
                }
            }
        }  

        ResetSpellCasting();
    }

    public void ResetSpellCasting()
    {
        IsTargetSelecting = false;
        _castSpellButton.GetComponent<UnityEngine.UI.Image>().color = Color.white;
        _wantsToDispell = false;

        if (Unit.SelectedUnit != null)
        {
            GridManager.Instance.HighlightTilesInMovementRange(Unit.SelectedUnit.GetComponent<Stats>());
        }
    }
    #endregion

    #region Handle spell effect
    private IEnumerator HandleSpellEffect(Stats spellcasterStats, Stats targetStats, Spell spell, int rollResult, int successLevel)
    {
        Unit targetUnit = targetStats.GetComponent<Unit>();

        //Uwzględnienie czasu trwania zaklęcia, które wpływa na statystyki postaci
        if (spell.Duration != 0 && spell.Type.Contains("buff"))
        {
            // Obliczenie czasu trwania efektu – przykładowo modyfikowany przez Siłę Woli (SW) lub Ogładę
            int baseStat = spell.Arcane == "Cuda" ? spellcasterStats.Ogd : spellcasterStats.SW;
            int effectDuration = spell.Type.Contains("constant-duration") || spell.Arcane == "Błogosławieństwa" ? spell.Duration : spell.Duration * (baseStat / 10);

            // Przygotowanie słownika modyfikacji – iterujemy po liście atrybutów, które zaklęcie ma zmieniać
            Dictionary<string, int> modifications = new Dictionary<string, int>();

            // Przykładowa logika: dla każdego atrybutu pobieramy klucz i wartość
            // Jeśli chcemy skalować pierwszy atrybut przez SW, można to zrobić warunkowo
            for (int i = 0; i < spell.Attributes.Count; i++)
            {
                string attributeName = spell.Attributes[i].Key;
                int baseModifier = spell.Attributes[i].Value;

                // Jeśli to pierwszy atrybut i zaklęcie ma skalowanie przez SW
                if (i == 0 && spell.Type.Contains("scaling-by-SW"))
                {
                    baseModifier *= spellcasterStats.SW / 10;
                }
                modifications[attributeName] = baseModifier;
            }

            // Jeśli efekt dotyczy jednostki, która już ma jakiś efekt tego samego zaklęcia, możesz zdecydować czy mają się kumulować,
            // czy nadpisywać – poniżej przykład, gdzie zawsze nadpisujemy poprzedni efekt z danego spellName.
            var existingEffect = targetStats.ActiveSpellEffects.FirstOrDefault(e => e.SpellName == spell.Name);
            if (existingEffect != null)
            {
                existingEffect.RemainingRounds = effectDuration;
                Debug.Log($"Nadpisujemy poprzedni efekt zaklęcia {spell.Name} u {targetStats.Name}.");
                yield break;
            }

            // Tworzymy nowy efekt
            SpellEffect newEffect = new SpellEffect(spell.Name, effectDuration, modifications);
            targetStats.ActiveSpellEffects.Add(newEffect);
        }

        // Uwzględnienie testu obronnego
        if (spell.SaveTestRequiring == true && spell.Attributes != null && spell.Attributes.Count > 0)
        {
            // Pobiera pierwszy atrybut jako ten, który służy do testu obronnego
            string attributeName = spell.Attributes.First().Key;  // Zmieniono dostęp do atrybutu, teraz używamy First()

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

            string skillName = null;
            if(attributeName == "Dodge")
            {
                attributeName = "Zw";
                skillName = "Dodge";
            }

            // Test obronny przy użyciu TestSkill
            int[] skillTestResults = DiceRollManager.Instance.TestSkill(attributeName, targetStats, skillName, 0, saveRollResult);

            if (skillTestResults[0] < 0)
            {
                Debug.Log($"{targetStats.Name} nie udało się przeciwstawić zaklęciu.");
            }
            else
            {
                Debug.Log($"{targetStats.Name} udało się przeciwstawić zaklęciu.");
                yield break;
            }
        }
        else if (spell.Attributes != null && spell.Attributes.Count > 0)
        {
            // Konwersja listy Attributes do listy kluczy
            var keys = spell.Attributes.Select(a => a.Key).ToList();

            for (int i = 0; i < keys.Count; i++)
            {
                string attributeName = keys[i];
                int baseValue = spell.Attributes.First(a => a.Key == attributeName).Value;

                Stats affectedStats = spell.Type.Contains("self-attribute") ? spellcasterStats : targetStats;
                Unit affectedUnit = affectedStats.GetComponent<Unit>();

                // Szukamy pola najpierw w Stats
                FieldInfo field = affectedStats.GetType().GetField(attributeName);

                object targetObject = affectedStats;

                // Jeśli nie znaleziono w Stats, próbujemy znaleźć w Unit
                if (field == null && affectedUnit != null)
                {
                    field = affectedUnit.GetType().GetField(attributeName);
                    targetObject = affectedUnit;
                }

                if (field == null) continue;

                int value = baseValue;

                // Tylko pierwszy atrybut jest skalowany przez SW
                if (i == 0 && spell.Type.Contains("attribute-scaling-by-SW"))
                {
                    value *= affectedStats.SW / 10;
                }

                if (field.FieldType == typeof(bool))
                {
                    bool boolValue = value != 0;
                    field.SetValue(targetObject, boolValue);
                    Debug.Log($"{affectedStats.Name} zyskuje cechę {attributeName}: {(boolValue ? "aktywna" : "nieaktywna")}.");
                }
                else if (field.FieldType == typeof(int))
                {
                    if (attributeName == "TempHealth" && targetObject is Stats)
                    {
                        int newValue = (int)field.GetValue(affectedStats) + value;
                        if (newValue <= affectedStats.MaxHealth)
                        {
                            field.SetValue(affectedStats, newValue);
                            affectedUnit.DisplayUnitHealthPoints();
                            UnitsManager.Instance.UpdateUnitPanel(Unit.SelectedUnit);
                            Debug.Log($"{affectedStats.Name} odzyskuje {value} punktów tymczasowej Żywotności.");
                        }
                    }
                    else
                    {
                        int current = (int)field.GetValue(targetObject);
                        field.SetValue(targetObject, current + value);
                        Debug.Log($"Zaklęcie {spell.Name} zmienia u {affectedStats.Name} cechę {attributeName} o {value}.");
                    }

                    if (attributeName == "NaturalArmor")
                    {
                        InventoryManager.Instance.CheckForEquippedWeapons();
                    }
                }
            }

            UnitsManager.Instance.UpdateUnitPanel(Unit.SelectedUnit);
        }

        // Uwzględnienie zasad specjalnych Tradycji Śmierci
        if (_shyishToggle.isOn && targetStats != spellcasterStats)
        {
            targetUnit.Fatiqued++;
            Debug.Log($"<color=#FF7F50>Poziom wyczerpania {targetStats.Name} wzrasta o 1, gdyż jest celem zaklęcia z Tradycji Śmierci.</color>");
        }

        // Uwzględnienie zasad specjalnych Tradycji Światła
        if (_hyshToggle.isOn && targetStats != spellcasterStats)
        {
            targetUnit.Blinded++;
            Debug.Log($"<color=#FF7F50>Poziom oślepienia {targetStats.Name} wzrasta o 1, gdyż jest celem zaklęcia z Tradycji Światła.</color>");
        }

        // Uwzględnienie zasad specjalnych Tradycji Życia
        if (_ghyranToggle.isOn && targetStats.Daemonic == 0 && !targetStats.Undead && (targetUnit.Fatiqued != 0 || targetUnit.Bleeding != 0))
        {
            targetUnit.Fatiqued = 0;
            targetUnit.Bleeding = 0;
            Debug.Log($"<color=#FF7F50>{targetStats.Name} traci wszystkie poziomy wyczerpania i krwawienia, gdyż jest celem zaklęcia z Tradycji Życia.</color>");
        }

        // Uwzględnienie zasad specjalnych Tradycji Zwierząt ---------------------- DO UZUPEŁNIENIA, ŻEBY TO ZAUTOMATYZOWAĆ
        if (_ghurToggle.isOn && spellcasterStats.Fear < 1)
        {
            //spellcasterStats.Fear++;
            int duration = UnityEngine.Random.Range(1, 11);
            Debug.Log($"{spellcasterStats.Name} zyskuje cechę Strach (1) na {duration} rund/y, ponieważ rzucił/a zzaklęcie z Tradycji Zwierząt. <color=red>Uwzględnij to manualnie.</color>");
        }

        //Zaklęcia zadające obrażenia
        if (!spell.Type.Contains("no-damage") && spell.Type.Contains("offensive"))
        {
            DealMagicDamage(spellcasterStats, targetStats, spell, rollResult, successLevel);
        }
    }

    private void DealMagicDamage(Stats spellcasterStats, Stats targetStats, Spell spell, int rollResult, int successLevel)
    {
        int damage = spell.Type.Contains("constant-strength") ? spell.Strength : successLevel + spell.Strength;
        Debug.Log($"Poziom sukcesu {spellcasterStats.Name}: {successLevel}. Siła zaklęcia: {spell.Strength}");

        //Ustalamy miejsce trafienia
        string unnormalizedHitLocation = !String.IsNullOrEmpty(CombatManager.Instance.HitLocation) ? CombatManager.Instance.HitLocation : (DiceRollManager.Instance.IsDoubleDigit(rollResult) ? CombatManager.Instance.DetermineHitLocation() : CombatManager.Instance.DetermineHitLocation(rollResult));
        string hitLocation = CombatManager.Instance.NormalizeHitLocation(unnormalizedHitLocation);

        if(spell.Arcane == "Cuda" && spellcasterStats.HolyHatred > 0)
        {
            damage += spellcasterStats.HolyHatred;
            Debug.Log($"Obrażenia zostają powiększone o {spellcasterStats.HolyHatred} za talent \"Święta Nienawiść\".");
        }

        // Uwzględnienie zasad specjalnych Tradycji Światła lub Życia
        if ((_hyshToggle.isOn || _ghyranToggle.isOn) && (targetStats.Daemonic != 0 || targetStats.Undead) && targetStats != spellcasterStats)
        {
            bool isGhyran = _ghyranToggle.isOn;
            int bonusDamage = isGhyran ? spellcasterStats.SW / 10 : spellcasterStats.Int / 10;
            damage += bonusDamage;
            spell.WtIgnoring = true;
            spell.ArmourIgnoring = true;

            string unitType = targetStats.Daemonic != 0 ? "Demoniczny" : "Ożywieniec";
            string traditionName = isGhyran ? "Tradycji Życia" : "Tradycji Światła";

            Debug.Log($"{targetStats.Name} otrzymuje {bonusDamage} dodatkowe obrażenia za cechę {unitType}, gdyż jest celem zaklęcia z {traditionName}.");
        }

        // Uwzględnienie zasad specjalnych Tradycji Życia
        if (_ghyranToggle.isOn && (targetStats.Daemonic != 0 || targetStats.Undead) && targetStats != spellcasterStats)
        {
            damage += spellcasterStats.SW / 10;
            spell.WtIgnoring = true;
            spell.ArmourIgnoring = true;
            string unitType = targetStats.Daemonic != 0 ? "Demoniczny" : "Ożywieniec";
            Debug.Log($"{targetStats.Name} otrzymuje {spellcasterStats.Int / 10} dodatkowe obrażenia za cechę {unitType}, gdyż jest celem zaklęcia z Tradycji Życia.");
        }

        // Uwzględnienie zasad specjalnych Tradycji Ognia
        if (_aqshyToggle.isOn)
        {
            targetStats.GetComponent<Unit>().Ablaze++;
            Debug.Log($"<color=#FF7F50>Poziom podpalenia {targetStats.Name} wzrasta o 1, gdyż jest celem zaklęcia z Tradycji Ognia.</color>");
        }

        // Sprawdzamy zbroję
        int armor = CombatManager.Instance.CalculateArmor(spellcasterStats, targetStats, hitLocation, rollResult);

        // Pobranie pancerza dla trafionej lokalizacji
        List<Weapon> armorByLocation = targetStats.GetComponent<Inventory>().ArmorByLocation.ContainsKey(hitLocation) ? targetStats.GetComponent<Inventory>().ArmorByLocation[hitLocation] : new List<Weapon>();

        // Sprawdzenie, czy żadna część pancerza nie jest metalowa
        bool hasMetalArmor = armorByLocation.Any(weapon => weapon.Category == "chain" || weapon.Category == "plate");
        int metalArmorValue = armorByLocation.Where(armorItem => (armorItem.Category == "chain" || armorItem.Category == "plate") && armorItem.Armor - armorItem.Damage > 0).Sum(armorItem => armorItem.Armor - armorItem.Damage);

        if (spell.ArmourIgnoring || _ulguToggle.isOn) armor = 0;
        if ((spell.MetalArmourIgnoring || _chamonToggle.isOn || _azyrToggle.isOn) && hasMetalArmor)
        {
            armor -= metalArmorValue;

            // Zwiększenie obrażeń o wartość metalowej zbroi
            if(_chamonToggle.isOn)
            {
                damage += metalArmorValue;
                Debug.Log($"{targetStats.Name} otrzymuje {metalArmorValue} dodatkowe obrażenia za metalowy pancerz, gdyż jest celem zaklęcia z Tradycji Metalu.");
            }
        }

        // Zadanie obrażeń 
        CombatManager.Instance.ApplyDamageToTarget(damage, armor, spellcasterStats, targetStats, targetStats.GetComponent<Unit>(), null, spell.WtIgnoring);

        if (targetStats.TempHealth < 0)
        {
            if (GameManager.IsAutoKillMode)
            {
                CombatManager.Instance.HandleDeath(targetStats, targetStats.gameObject, null);
            }
            else
            {
                if (targetStats.Daemonic > 0)
                {
                    Debug.Log($"<color=red>{targetStats.Name} zostaje odesłany do domeny Chaosu.</color>");
                }
                else
                {
                    StartCoroutine(CombatManager.Instance.CriticalWoundRoll(spellcasterStats, targetStats, hitLocation, null, rollResult));
                }
            }
        }

        // Zastosowanie efektu Tradycji Niebios (obrażenia przeskakują na sąsiednie jednostki)
        if (_azyrToggle.isOn)
        {
            Vector2 targetPos = targetStats.transform.position;
            Vector2[] adjacentPositions = {
                    targetPos + Vector2.right,
                    targetPos + Vector2.left,
                    targetPos + Vector2.up,
                    targetPos + Vector2.down,
                    targetPos + new Vector2(1, 1),
                    targetPos + new Vector2(-1, -1),
                    targetPos + new Vector2(-1, 1),
                    targetPos + new Vector2(1, -1)
                };

            foreach (var pos in adjacentPositions)
            {
                Collider2D collider = Physics2D.OverlapPoint(pos);
                if (collider != null)
                {
                    Unit adjacentUnit = collider.GetComponent<Unit>();
                    if (adjacentUnit != null && adjacentUnit != targetStats.GetComponent<Unit>())
                    {
                        int adjacentUnitArmor = CombatManager.Instance.CalculateArmor(spellcasterStats, adjacentUnit.GetComponent<Stats>(), hitLocation, rollResult);
                        int electricDamage = (spellcasterStats.SW / 10) + UnityEngine.Random.Range(1, 11);
                        Debug.Log($"{adjacentUnit.Stats.Name} otrzymuje {electricDamage} obrażeń spowodowanych ładunkiem elektrycznym zaklęcia z Tradycji Niebios.");

                        CombatManager.Instance.ApplyDamageToTarget(electricDamage, adjacentUnitArmor, spellcasterStats, adjacentUnit.GetComponent<Stats>(), adjacentUnit);
                    }
                }
            }
        }

        if(_criticalCastingString == "critical_wound")
        {
            StartCoroutine(CombatManager.Instance.CriticalWoundRoll(spellcasterStats, targetStats, hitLocation, null, rollResult));
        }
    }
    #endregion

    #region Dispell
    private IEnumerator Dispell(Stats targetStats, Spell spell, int casterSuccessLevel, Action<int> onResult)
    {
        if(targetStats.MagicLanguage == 0) yield break;

        _dispellPanel.SetActive(true);

        // Najpierw czekamy, aż gracz kliknie którykolwiek przycisk
        yield return new WaitUntil(() => !_dispellPanel.activeSelf);

        if (!_wantsToDispell)
        {
            onResult?.Invoke(casterSuccessLevel);
            yield break;
        }
                
        Unit targetUnit = targetStats.GetComponent<Unit>();

        int rollResult = 0;

        if (!GameManager.IsAutoDiceRollingMode && targetStats.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, "język magiczny", result => rollResult = result));
            if (rollResult == 0) yield break;
        }
        else
        {
            rollResult = UnityEngine.Random.Range(1, 101);
        }

        // Modyfikator za zbroję
        int modifier = CalculateArmorModifier(targetStats);

        int[] dispellTest = DiceRollManager.Instance.TestSkill("Int", targetStats, "MagicLanguage", modifier, rollResult);

        int combinedSuccessLevel = casterSuccessLevel - dispellTest[1];

        if (RoundsManager.RoundNumber != 0)
        {
            targetUnit.CanDispell = false;
        }

        onResult?.Invoke(combinedSuccessLevel);
    }

    public void DispellDecision(bool value)
    {
        _wantsToDispell = value;
    }

    #endregion

    #region Critical casting
    public IEnumerator CriticalCastingRoll(Spell spell, bool spellFailed)
    {
        // Wyświetlenie panelu akcji i oczekiwanie na wybór gracza
        _criticalCastingString = "";
        if (_criticalCastingPanel != null)
        {
            _criticalWoundButton.interactable = spell.Type.Contains("offensive") && !spellFailed;
            _antiDispellButton.interactable = !spellFailed;
            _forceCastButton.interactable = spellFailed;

            _criticalCastingPanel.SetActive(true);
            yield return new WaitUntil(() => !string.IsNullOrEmpty(_criticalCastingString));
            _criticalCastingPanel.SetActive(false);
        }
        else yield break;
    }

    private void CriticalCastingButtonClick(string action)
    {
        _criticalCastingString = action;
    }
    #endregion

    #region Overcasting

    // Wartości z tabeli Overcastingu
    int[] targetValues = { 1, 1, 1, 2, 2, 2, 3 };
    int[] damageValues = { 1, 2, 3, 4, 5, 6, 7 };
    int[] rangeMultipliers = { 2, 2, 2, 3, 3, 3, 4 };
    int[] areaMultipliers = { 1, 1, 2, 2, 2, 2, 3 };
    int[] durationMultipliers = { 1, 2, 2, 2, 3, 3, 3 };

    public IEnumerator Overcasting(Spell spell, int successLevels)
    {
        _overcastingLevel = successLevels;
        _overcastingLevelDisplay.text = "PS do rozdania: " + _overcastingLevel.ToString();
        _overcastingStrings.Clear();
        ClearAllOvercastCounterTexts();

        if (_overcastingPanel != null)
        {
            _extraTargetButton.interactable = !spell.Type.Contains("self-only") && spell.AreaSize == 0;
            _extraDamageButton.interactable = spell.Type.Contains("magic-missile");
            _extraRangeButton.interactable = spell.Range != 1.5f;
            _extraAreaSizeButton.interactable = spell.AreaSize != 0;
            _extraDurationButton.interactable = spell.Duration != 0;

            _overcastingPanel.SetActive(true);

            // Czekaj, aż gracz wybierze wszystkie efekty albo zamknie panel
            yield return new WaitUntil(() => _overcastingLevel == 0 || !_overcastingPanel.activeSelf);

            var effectCounts = new Dictionary<string, int>
            {
                { "target", 0 },
                { "damage", 0 },
                { "range", 0 },
                { "area_size", 0 },
                { "duration", 0 }
            };

            // Zliczamy wystąpienia każdego efektu
            foreach (string effect in _overcastingStrings)
            {
                if (effectCounts.ContainsKey(effect))
                    effectCounts[effect]++;
            }

            // Dla każdej akcji obliczamy łączny koszt (SL) wykorzystany przy kliknięciach
            int slTargetCost = GetCumulativeCost("target", effectCounts["target"]);
            if (slTargetCost > 0)
                spell.Targets += GetOvercastingEffectValue(slTargetCost, targetValues);

            int slDamageCost = GetCumulativeCost("damage", effectCounts["damage"]);
            if (slDamageCost > 0)
                spell.Strength += GetOvercastingEffectValue(slDamageCost, damageValues);

            int slRangeCost = GetCumulativeCost("range", effectCounts["range"]);
            if (slRangeCost > 0)
                spell.Range *= GetOvercastingEffectValue(slRangeCost, rangeMultipliers);

            int slAreaCost = GetCumulativeCost("area_size", effectCounts["area_size"]);
            if (slAreaCost > 0)
                spell.AreaSize *= GetOvercastingEffectValue(slAreaCost, areaMultipliers);

            int slDurationCost = GetCumulativeCost("duration", effectCounts["duration"]);
            if (slDurationCost > 0)
                spell.Duration *= GetOvercastingEffectValue(slDurationCost, durationMultipliers);

            //Debug.Log($"spell.Targets {spell.Targets}");
            //Debug.Log($"spell.Strength {spell.Strength}");
            //Debug.Log($"spell.Range {spell.Range}");
            //Debug.Log($"spell.AreaSize {spell.AreaSize}");
            //Debug.Log($"spell.Duration {spell.Duration}");

            _overcastingLevel = 0;
            _overcastingPanel.SetActive(false);
        }
        else yield break;
    }

    private void OvercastingButtonClick(GameObject button, string action)
    {
        // Ile razy dotychczas kliknięto przycisk dla danej akcji
        int count = _overcastingStrings.Count(a => a == action);

        // Pobierz tablicę kosztów dla danej akcji
        int[] costTable = GetCostTableForAction(action);

        // Sprawdź, czy koszt bieżącego (kolejnego) kliknięcia przekracza dostępne SL
        // Używamy costTable[count] – zakładając, że index count odpowiada kosztowi kolejnego kliknięcia
        if (count < costTable.Length && costTable[count] > _overcastingLevel)
        {
            Debug.Log("Niewystarczająca ilość Poziomów Sukcesu, aby zwiększyć ten efekt.");
            return;
        }

        // Dodajemy dany efekt (akcję) do listy kliknięć
        _overcastingStrings.Add(action);

        // Odejmujemy koszt kliknięcia – koszt bieżącego kliknięcia jest costTable[count]
        if (count < costTable.Length)
        {
            _overcastingLevel -= costTable[count];
        }

        // Sumuje łączny koszt tego efektu
        int totalCost = 0;
        for (int i = 0; i <= count && i < costTable.Length; i++)
        {
            totalCost += costTable[i];
        }

        // Obliczamy wartość efektu – zgodnie z Twoją tabelą, używając funkcji GetOvercastingEffectValue
        int cumulativeValue = GetOvercastingEffectValue(totalCost, GetValueTableForAction(action));

        // Formatowanie tekstu: dla "damage" oraz "target" pokażemy "+{cumulativeValue}",
        // a dla pozostałych (np. range, area_size, duration) "x{cumulativeValue}"
        string display = (action == "damage" || action == "target") ? $"+{cumulativeValue}" : $"x{cumulativeValue}";

        // Aktualizacja tekstu w przycisku (CounterText)
        TextMeshProUGUI text = button.transform.Find("CounterText")?.GetComponent<TextMeshProUGUI>();
        if (text != null)
            text.text = display;

        // Aktualizujemy wyświetlaną liczbę dostępnych Poziomów Sukcesu
        _overcastingLevelDisplay.text = $"PS do rozdania: {_overcastingLevel}";
    }

    private int[] GetCostTableForAction(string action)
    {
        // Koszty kliknięć zgodne z kolumną SL (rozwijane sumarycznie)
        // Przykład: koszt 1, potem +2 (czyli 3 łącznie), potem +2 (czyli 5 łącznie), potem +3 (czyli 8 łącznie) itd.
        switch (action)
        {
            case "target":
                return new int[] { 1, 4, 16 }; // SL: 1, 5, 21
            case "damage":
                return new int[] { 1, 1, 1, 2, 3, 5, 8 }; // SL: 1,2,3,5,8,13,21
            case "range":
                return new int[] { 1, 4, 16 }; // SL: 1, 5, 21
            case "area_size":
                return new int[] { 3, 18 }; // SL: 3, 21
            case "duration":
                return new int[] { 2, 6}; // SL: 2, 8
            default:
                return new int[] { 1 };
        }
    }

    private int[] GetValueTableForAction(string action)
    {
        switch (action)
        {
            case "target": return targetValues;
            case "damage": return damageValues;
            case "range": return rangeMultipliers;
            case "area_size": return areaMultipliers;
            case "duration": return durationMultipliers;
            default: return new int[] { 0 };
        }
    }

    // Funkcja pomocnicza do pobierania wartości z tabeli na podstawie Poziomu Sukcesu
    private int GetOvercastingEffectValue(int sl, int[] thresholds)
    {
        if (sl >= 21) return thresholds[6];
        if (sl >= 13) return thresholds[5];
        if (sl >= 8) return thresholds[4];
        if (sl >= 5) return thresholds[3];
        if (sl >= 3) return thresholds[2];
        if (sl >= 2) return thresholds[1];
        if (sl >= 1) return thresholds[0];
        return 0;
    }

    private int GetCumulativeCost(string action, int count)
    {
        int[] costs = GetCostTableForAction(action);
        int total = 0;
        // Sumujemy koszty od 0 do count-1 (czyli dla każdego kliknięcia)
        for (int i = 0; i < count && i < costs.Length; i++)
        {
            total += costs[i];
        }
        return total;
    }

    private void ClearAllOvercastCounterTexts()
    {
        if (_overcastingPanel == null)
            return;

        // Znajdź kontener z przyciskami (zakładamy, że nazywa się "Buttons")
        Transform buttonsContainer = _overcastingPanel.transform.Find("Buttons");
        if (buttonsContainer == null) return;

        // Iteruj po wszystkich przyciskach w kontenerze
        foreach (Transform button in buttonsContainer)
        {
            // Znajdź dziecko o nazwie "CounterText" w przycisku
            Transform counterTransform = button.Find("CounterText");
            if (counterTransform != null)
            {
                TextMeshProUGUI text = counterTransform.GetComponent<TextMeshProUGUI>();
                if (text != null)
                    text.text = ""; // Resetuj tekst
            }
        }
    }
    #endregion

    private int CalculateArmorModifier(Stats stats)
    {
        int modifier = 0;

        int[] armors = { stats.Armor_head, stats.Armor_arms, stats.Armor_torso, stats.Armor_legs };
        //int armouredCastingModifier = stats.ArmouredCasting == true ? 3 : 0;
        //modifier -= Math.Max(0, armors.Max() - armouredCastingModifier); //Odejmuje największa wartość zbroi i uwzględnia Pancerz Wiary

        modifier -= Math.Max(0, armors.Max());

        modifier += stats.NaturalArmor;

        return modifier * 10;
    }

    #region Chaos manifestation
    public void CheckForChaosManifestation(Stats stats, int rollResult, int successValue, int successLevel, string skillName, int castingNumberLeft = 0, bool value = false)
    {
        Unit unit = stats.GetComponent<Unit>();
        bool isSuccessful = successValue >= 0;
        bool hasDouble = DiceRollManager.Instance.IsDoubleDigit(rollResult);
        bool hasZeroOnes = rollResult % 10 == 0;

        if (!isSuccessful && hasDouble && skillName == "Pray") // Pech na modlitwę
        {
            int roll = UnityEngine.Random.Range(1, 101);
            int modifier = Math.Max(Math.Max(0, (-successLevel) * 10), Math.Max(0, castingNumberLeft * 10));
            int finalRoll = roll + modifier;

            if (modifier != 0)
            {
                Debug.Log($"<color=red>Występuje Gniew Boży!</color> Wynik rzutu: {roll}. Modyfikator: {modifier}. Poziom Gniewu Bożego: <color=red>{finalRoll}</color>.");
            }
            else
            {
                Debug.Log($"<color=red>Występuje Gniew Boży!</color> Wynik rzutu: <color=red>{finalRoll}</color>.");
            }
        }
        else if ((isSuccessful && hasDouble && !((stats.AethyricAttunement > 0 && skillName == "Channeling") || (stats.InstinctiveDiction > 0 && skillName == "MagicLanguage"))) || (!isSuccessful && (hasDouble || hasZeroOnes)) || value == true) // Pech na Splatanie lub Język magiczny
        {
            int roll = UnityEngine.Random.Range(1, 101);
            int modifier = Math.Max(Math.Max(0, (-successLevel) * 10), Math.Max(0, castingNumberLeft * 10));
            int finalRoll = roll + modifier;

            if (modifier != 0)
            {
                Debug.Log($"<color=red>Występuje manifestacja Chaosu!</color> Wynik rzutu: {roll}. Modyfikator: {modifier}. Poziom manifestacji: <color=red>{finalRoll}</color>.");
            }
            else
            {
                Debug.Log($"<color=red>Występuje manifestacja Chaosu!</color> Wynik rzutu na manifestację: <color=red>{finalRoll}</color>.");
            }

            if (skillName == "Channeling" && isSuccessful && value == false)
            {
                unit.ChannelingModifier += stats.SW / 10;
                Debug.Log($"Poziomy sukcesu zebrane w wyniku splatania magii zostają powiększone o bonus z Siły Woli i wynoszą: <color=#4dd2ff>{unit.ChannelingModifier}</color>");
            }
            else if (skillName == "Channeling" && (!isSuccessful || value == true) && unit.ChannelingModifier != 0)
            {
                unit.ChannelingModifier = 0;
                Debug.Log($"Wszystkie poziomy sukcesu zebrane w wyniku splatania magii przepadają.");
            }
        }
    }
    #endregion

    #region Arcanes special effects
    private void OnArcaneToggleChanged(UnityEngine.UI.Toggle changedToggle, bool isOn)
    {
        if (!isOn) return;

        foreach (var toggle in _arcanesToggles)
        {
            if (toggle != changedToggle)
            {
                toggle.isOn = false;
            }
        }
    }

    private void SetArcaneToggle(Spell spell)
    {
        switch (spell.Arcane)
        {
            case "Tradycja Ognia":
                _aqshyToggle.isOn = true;
                break;
            case "Tradycja Niebios":
                _azyrToggle.isOn = true;
                break;
            case "Tradycja Metalu":
                _chamonToggle.isOn = true;
                break;
            case "Tradycja Bestii":
                _ghurToggle.isOn = true;
                break;
            case "Tradycja Życia":
                _ghyranToggle.isOn = true;
                break;
            case "Tradycja Światła":
                _hyshToggle.isOn = true;
                break;
            case "Tradycja Śmierci":
                _shyishToggle.isOn = true;
                break;
            case "Tradycja Cienia":
                _ulguToggle.isOn = true;
                break;
        }
    }
    #endregion
}