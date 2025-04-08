using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
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

    public GameObject Target;
    private List<Stats> _targetsStats; // Lista jednostek, które są wybierane jako cele zaklęcia, które pozwala wybrać więcej niż jeden cel
    public List<Stats> UnitsStatsAffectedBySpell; // Lista jednostek, na które w danym momencie wpływa jakieś zaklęcie z czasem trwania, np. Pancerz Eteru

    [Header("Panele do manualnego zarządzania")]
    [SerializeField] private GameObject _dispellPanel;
    private bool _wantsToDispell;

    // DO WPROWADZENIA
    [Header("Panel do manualnego zarządzania krytycznym splecieniem zaklęcia")]
    [SerializeField] private GameObject _criticalCastingPanel;
    [SerializeField] private UnityEngine.UI.Button _criticalWoundButton; // Zadaje dodatkowo ranę krytyczną, jeśli to zaklęcie zadające obrażenia
    [SerializeField] private UnityEngine.UI.Button _forceCastButton; // Zaklęcie jest rzucone mimo niewystarczającego poziomu sukcesu
    [SerializeField] private UnityEngine.UI.Button _antiDispelButton; // Zaklęcie nie może być rozproszone
    private string _criticalCastingString;

    // DO WPROWADZENIA
    [Header("Panel do manualnego zarządzania overcastingiem")]
    [SerializeField] private GameObject _overcastingPanel;
    [SerializeField] private UnityEngine.UI.Button _extraTargetButton;
    [SerializeField] private UnityEngine.UI.Button _extraDamageButton;
    [SerializeField] private UnityEngine.UI.Button _extraRangeButton;
    [SerializeField] private UnityEngine.UI.Button _extraAreaSizeButton;
    [SerializeField] private UnityEngine.UI.Button _extraDurationButton;
    private string _overcastingString;

    void Start()
    {
        //Wczytuje listę wszystkich zaklęć
        DataManager.Instance.LoadAndUpdateSpells();

        _targetsStats = new List<Stats>();
        UnitsStatsAffectedBySpell = new List<Stats>();
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
            int modifier = CalculateArmorModifier(stats) + stats.AethyricAttunement * 10;

            int[] test = DiceRollManager.Instance.TestSkill("SW", stats, "Channeling", modifier, rollResult);

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

        if (!Unit.SelectedUnit.GetComponent<Unit>().CanDoAction)
        {
            Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
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

        RoundsManager.Instance.DoAction(Unit.SelectedUnit.GetComponent<Unit>());

        Target = null;
        string selectedSpellName = _spellbookDropdown.SelectedButton.GetComponentInChildren<TextMeshProUGUI>().text;
        DataManager.Instance.LoadAndUpdateSpells(selectedSpellName);

        StartCoroutine(CastSpell());
    }

    public IEnumerator CastSpell()
    {
        if (Unit.SelectedUnit == null) yield break;

        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();
        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();
        Spell spell = Unit.SelectedUnit.GetComponent<Spell>();       
      
        unit.CanCastSpell = false;

        int rollResult = 0;
        if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "język magiczny", result => rollResult = result));
        }
        else
        {
            rollResult = UnityEngine.Random.Range(1, 101);
        }

        // Modyfikator za zbroję i talent Precyzyjne Inkantowanie
        int modifier = CalculateArmorModifier(stats) + stats.InstinctiveDiction * 10;

        // Modyfikator za poziomy podpalenia w pobliżu (tylko dla Tradycji Ognia)
        if (spell.Arcane == "Tradycja Ognia")
        {
            int totalAblaze = 0; // Zmienna do sumowania wartości poziomów podpaleń
            foreach (var entry in InitiativeQueueManager.Instance.InitiativeQueue)
            {
                Unit u = entry.Key;
                Stats unitStats = u.GetComponent<Stats>();

                // Sprawdzamy, czy jednostka ma Ablaze większe niż 0 i nie jest tą samą jednostką
                if (unit.Ablaze > 0 && !ReferenceEquals(unitStats, stats))
                {
                    totalAblaze += u.Ablaze;
                    Debug.Log($"{unitStats.Name} ma {u.Ablaze} poziomów podpalenia.");
                }
            }

            modifier += totalAblaze;
        }

        // Test języka magicznego na rzucanie zaklęcia
        int[] castingTest = DiceRollManager.Instance.TestSkill("Int", stats, "MagicLanguage", modifier, rollResult);

        int successLevels = castingTest[1] + unit.ChannelingModifier;
        string color = successLevels >= spell.CastingNumber ? "green" : "red"; // Zielony, jeśli >= CastingNumber, inaczej czerwony
        Debug.Log($"{stats.Name} splata zaklęcie. Uzyskane poziomy sukcesu: <color={color}>{successLevels}/{spell.CastingNumber}</color>.");

        bool spellFailed = spell.CastingNumber - successLevels > 0;
        Debug.Log(spellFailed ? $"Rzucanie zaklęcia {spell.Name} nie powiodło się." : $"Zaklęcie {spell.Name} zostało splecione pomyślnie.");

        if (unit.ChannelingModifier > 0 && spellFailed) //Jeśli czarodziej splatał wcześniej magię i zaklęcie nie powiodło się, występuję manifestacja Chaosu (od nadmiaru zebranej magii)
        {
            CheckForChaosManifestation(stats, rollResult, castingTest[0], castingTest[1], "MagicLanguage", spell.CastingNumber - successLevels, true);
        }
        else // Standardowe sprawdzenie warunków manifestacji
        {
            CheckForChaosManifestation(stats, rollResult, castingTest[0], castingTest[1], "MagicLanguage", spell.CastingNumber - successLevels);
        }

        // Zresetowanie zaklęcia
        ResetSpellCasting();
        unit.ChannelingModifier = 0;

        // Zaklęcie nie zostało w pełni splecione - przerywamy funkcję
        if (spellFailed) yield break;

        GridManager.Instance.ResetColorOfTilesInMovementRange();

        IsTargetSelecting = true;
        _targetsStats.Clear();

        //Zmienia kolor przycisku na aktywny
        _castSpellButton.GetComponent<Image>().color = Color.green;

        Debug.Log("Kliknij prawym przyciskiem myszy na jednostkę, która ma być celem zaklęcia.");

        while (Target == null)
        {
            yield return null;
        }

        Stats targetStats = Target.GetComponent<Stats>();
        Unit targetUnit = Target.GetComponent<Unit>();

        //Sprawdza dystans
        _spellDistance = CombatManager.Instance.CalculateDistance(Unit.SelectedUnit, Target);
        float spellRange = spell.Range != 1.5f ? spell.Range * stats.SW / 2f : spell.Range; // Zazwyczaj zasięg zaklęcia jest zależny od Siły Woli czarodzieja. Czary dotykowe mają zasięg równy 1.5f
        Debug.Log($"Dystans: {_spellDistance}. Zasięg zaklęcia: {spellRange}");

        if (_spellDistance > spellRange)
        {
            Debug.Log("Cel znajduje się poza zasięgiem zaklęcia.");
            ResetSpellCasting();
            yield break;
        }

        // Ustala obszar działania zaklęcia. Zwykle jest to mnożnik bonusu z Siły Woli
        float areaSize = spell.AreaSize * (stats.SW / 10) / 2f;

        // Pobiera wszystkie collidery w obszarze działania zaklęcia
        List<Collider2D> allTargets = Physics2D.OverlapCircleAll(Target.transform.position, areaSize).ToList();

        // Filtruje wśród colliderów jednostki, na których można użyć tego zaklęcia
        allTargets.RemoveAll(collider =>
            collider.GetComponent<Unit>() == null ||
            (collider.gameObject == Unit.SelectedUnit && spell.Type.Contains("offensive")) ||
            (collider.gameObject != Unit.SelectedUnit && spell.Type.Contains("self-only"))
        );

        if (allTargets.Count == 0)
        {
            Debug.Log("W obszarze działania zaklęcia musi znaleźć się odpowiedni cel.");
            ResetSpellCasting();
            yield break;
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
            canParry = Target.GetComponent<Inventory>().EquippedWeapons.Any(weapon => weapon != null && (weapon.Type.Contains("melee") || weapon.Id == 0));
            canDodge = true;
            Weapon targetWeapon = InventoryManager.Instance.ChooseWeaponToAttack(Target);

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
                ResetSpellCasting();
                yield break;
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

        int finalSuccessLevel = 0;
        bool dispellResolved = false;

        // Próba rozproszenia zaklęcia
        if (dispeller != null)
        {
            StartCoroutine(Dispell(dispeller, spell, castingTest[1], result =>
            {
                finalSuccessLevel = result;
                dispellResolved = true;
            }));
        }
        else
        {
            finalSuccessLevel = castingTest[1];
            dispellResolved = true;
        }

        while (!dispellResolved)
        {
            yield return null;
        }

        if (finalSuccessLevel < 0 || (finalSuccessLevel == 0 && dispeller.MagicLanguage + dispeller.Int > stats.MagicLanguage + stats.Int))
        {
            Debug.Log($"{dispeller.Name} rozproszył zaklęcie rzucane przez {stats.Name}.");
            ResetSpellCasting();
            yield break;
        }

        // Wywołanie efektu zaklęcia
        foreach (Collider2D collider in allTargets)
        {
            StartCoroutine(HandleSpellEffect(stats, collider.GetComponent<Stats>(), spell, rollResult, finalSuccessLevel));
        }

        ResetSpellCasting();
    }

    public void ResetSpellCasting()
    {
        IsTargetSelecting = false;
        //Zmienia kolor przycisku na nieaktywny
        _castSpellButton.GetComponent<Image>().color = Color.white;
        _wantsToDispell = false;

        if (Unit.SelectedUnit != null)
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
                // Przywraca pierwotne wartości dla cech int i bool. 
                FieldInfo[] fields = typeof(Stats).GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    // Celowo pomija obecne punkty żywotności, bo mogły ulec zmianie w trakcie działania zaklęcia.
                    if (field.Name == "TempHealth") continue;

                    object currentValue = field.GetValue(unit.GetComponent<Stats>());
                    object originalValue = field.GetValue(UnitsStatsAffectedBySpell[i]);

                    if (!Equals(currentValue, originalValue))
                    {
                        field.SetValue(unit.GetComponent<Stats>(), originalValue);
                    }
                }

                UnitsStatsAffectedBySpell.RemoveAt(i);
                break; // zakończ pętlę po znalezieniu i usunięciu pasującego wpisu
            }
        }

        UnitsManager.Instance.UpdateUnitPanel(Unit.SelectedUnit);
    }
    #endregion

    #region Handle spell effect
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

            // Czas trwania zaklęcia zazwyczaj opiera się na mnożniku bonusu z Siły Woli
            targetUnit.SpellDuration = spell.Duration * (spellcasterStats.SW / 10);

            UnitsStatsAffectedBySpell.Add(targetStats.Clone());
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

            // Test obronny przy użyciu TestSkill
            int[] skillTestResults = DiceRollManager.Instance.TestSkill(attributeName, targetStats, null, 0, saveRollResult);

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
            Debug.Log($"Debug atrybutów dla zaklęcia {spell.Name}:");

            foreach (var attribute in spell.Attributes)
            {
                Debug.Log($"- {attribute.Key}: {attribute.Value}");
            }

            // Konwersja listy Attributes do listy kluczy
            var keys = spell.Attributes.Select(a => a.Key).ToList(); // Używamy LINQ, aby uzyskać listę kluczy
            for (int i = 0; i < keys.Count; i++)
            {
                string attributeName = keys[i];
                int baseValue = spell.Attributes.First(a => a.Key == attributeName).Value; // Używamy First() zamiast bezpośredniego dostępu do słownika

                Stats affectedStats = spell.Type.Contains("self-attribute") ? spellcasterStats : targetStats;
                FieldInfo field = affectedStats.GetType().GetField(attributeName);
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
                    field.SetValue(affectedStats, boolValue);
                    Debug.Log($"{affectedStats.Name} otrzymał/a cechę {attributeName}: {(boolValue ? "aktywna" : "nieaktywna")}.");
                }
                else if (field.FieldType == typeof(int))
                {
                    if (attributeName == "TempHealth")
                    {
                        int newValue = (int)field.GetValue(affectedStats) + value;
                        if (newValue <= affectedStats.MaxHealth)
                        {
                            field.SetValue(affectedStats, newValue);
                            affectedStats.GetComponent<Unit>().DisplayUnitHealthPoints();
                            UnitsManager.Instance.UpdateUnitPanel(Unit.SelectedUnit);
                            Debug.Log($"{affectedStats.Name} odzyskał/a {value} punktów tymczasowej Żywotności.");
                        }
                    }
                    else
                    {
                        field.SetValue(affectedStats, (int)field.GetValue(affectedStats) + value);
                        Debug.Log($"{affectedStats.Name} zmienił/a cechę {attributeName} o {value}.");
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
        if (spell.Arcane == "Tradycja Śmierci" && targetStats != spellcasterStats)
        {
            targetUnit.Fatiqued++;
            Debug.Log($"<color=#FF7F50>Poziom wyczerpania {targetStats.Name} wzrasta o 1, gdyż jest celem zaklęcia z Tradycji Śmierci.</color>");
        }

        // Uwzględnienie zasad specjalnych Tradycji Światła
        if (spell.Arcane == "Tradycja Światła" && targetStats != spellcasterStats)
        {
            targetUnit.Blinded++;
            Debug.Log($"<color=#FF7F50>Poziom oślepienia {targetStats.Name} wzrasta o 1, gdyż jest celem zaklęcia z Tradycji Światła.</color>");
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

        // Normalizujemy lokalizację trafienia
        string normalizedHitLocation = hitLocation switch
        {
            "rightArm" or "leftArm" => "arms",
            "rightLeg" or "leftLeg" => "legs",
            _ => hitLocation
        };

        // Uwzględnienie zasad specjalnych Tradycji Światła (DODAĆ TU JESZCZE OŻYWIEŃCÓW, JEŚLI JUŻ WPROWADZĘ CECHĘ OŻYWIENIEC)
        if (spell.Arcane == "Tradycja Światła" && targetStats.Daemonic != 0 && targetStats != spellcasterStats)
        {
            damage += spellcasterStats.Int / 10;
            spell.WtIgnoring = true;
            spell.ArmourIgnoring = true;
            string unitType = targetStats.Daemonic != 0 ? "Demoniczny" : "Ożywieniec";
            Debug.Log($"{targetStats.Name} otrzymuje {spellcasterStats.Int / 10} dodatkowe obrażenia za cechę {unitType}, gdyż jest celem zaklęcia z Tradycji Światła.");
        }

        // Sprawdzamy zbroję
        int armor = CombatManager.Instance.CalculateArmor(spellcasterStats, targetStats, hitLocation, rollResult);

        // Pobranie pancerza dla trafionej lokalizacji
        List<Weapon> armorByLocation = targetStats.GetComponent<Inventory>().ArmorByLocation.ContainsKey(normalizedHitLocation) ? targetStats.GetComponent<Inventory>().ArmorByLocation[normalizedHitLocation] : new List<Weapon>();

        // Sprawdzenie, czy żadna część pancerza nie jest metalowa
        bool hasMetalArmor = armorByLocation.Any(weapon => weapon.Category == "chain" || weapon.Category == "plate");
        int metalArmorValue = armorByLocation.Where(armorItem => (armorItem.Category == "chain" || armorItem.Category == "plate") && armorItem.Armor - armorItem.Damage > 0).Sum(armorItem => armorItem.Armor - armorItem.Damage);

        if (spell.ArmourIgnoring) armor = 0;
        if (spell.MetalArmourIgnoring && hasMetalArmor)
        {
            armor -= metalArmorValue;

            // Zwiększenie obrażeń o wartość metalowej zbroi
            if(spell.Arcane == "Tradycja Metalu")
            {
                damage += metalArmorValue;
            }
        }

        CombatManager.Instance.ApplyDamageToTarget(damage, armor, spellcasterStats, targetStats, targetStats.GetComponent<Unit>(), null, spell.WtIgnoring);
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

        if ((isSuccessful && hasDouble && !((stats.AethyricAttunement > 0 && skillName == "Channeling") || (stats.InstinctiveDiction > 0 && skillName == "MagicLanguage"))) || (!isSuccessful && (hasDouble || hasZeroOnes)) || value == true)
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
}