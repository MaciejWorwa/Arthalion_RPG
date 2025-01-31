using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System.Linq;
using static SimpleFileBrowser.FileBrowser;
using UnityEditor;
using System.Drawing;
using TMPro;
using System;
using static UnityEngine.GraphicsBuffer;
using UnityEditor.Experimental.GraphView;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

public class CombatManager : MonoBehaviour
{
    // Prywatne statyczne pole przechowujące instancję
    private static CombatManager instance;

    // Publiczny dostęp do instancji
    public static CombatManager Instance
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

    [Header("Przyciski wszystkich typów ataku")]
    [SerializeField] private UnityEngine.UI.Button _aimButton;
    [SerializeField] private UnityEngine.UI.Button _defensiveStanceButton;
    [SerializeField] private UnityEngine.UI.Button _standardAttackButton;
    [SerializeField] private UnityEngine.UI.Button _chargeButton;
    [SerializeField] private UnityEngine.UI.Button _allOutAttackButton;
    [SerializeField] private UnityEngine.UI.Button _guardedAttackButton;
    [SerializeField] private UnityEngine.UI.Button _grapplingButton;
    [SerializeField] private UnityEngine.UI.Button _feintButton;
    [SerializeField] private UnityEngine.UI.Button _stunButton;
    [SerializeField] private UnityEngine.UI.Button _disarmButton;
    public Dictionary<string, bool> AttackTypes = new Dictionary<string, bool>();

    [Header("Panele do manualnego zarządzania")]
    [SerializeField] private GameObject _parryAndDodgePanel;
    [SerializeField] private UnityEngine.UI.Button _dodgeButton;
    [SerializeField] private UnityEngine.UI.Button _parryButton;
    [SerializeField] private UnityEngine.UI.Button _getDamageButton;
    [SerializeField] private UnityEngine.UI.Button _cancelButton;
    private string _parryOrDodge;
    [SerializeField] private GameObject _applyDefenceRollResultPanel;
    [SerializeField] private GameObject _applyRollResultPanel;
    [SerializeField] private TMP_InputField _rollInputField;
    [SerializeField] private TMP_InputField _defenceRollInputField;

    private bool _isTrainedWeaponCategory; // Określa, czy atakujący jest wyszkolony w używaniu broni, którą atakuje

    // Zmienne do przechowywania wyniku
    private int _manualRollResult;
    private bool _isWaitingForRoll;
    public bool IsManualPlayerAttack;

    private List<string> _greenskinsList = new List<string> { "Goblin", "Hobgoblin", "Ork zwyczajny", "Czarny ork", "Dziki ork" }; //Lista string wszystkich zielonoskórych

    // Metoda inicjalizująca słownik ataków
    void Start()
    {
        InitializeAttackTypes();
        UpdateAttackTypeButtonsColor();

        _dodgeButton.onClick.AddListener(() => ParryOrDodgeButtonClick("dodge"));
        _parryButton.onClick.AddListener(() => ParryOrDodgeButtonClick("parry"));
        _getDamageButton.onClick.AddListener(() => ParryOrDodgeButtonClick(""));
        _cancelButton.onClick.AddListener(() => ParryOrDodgeButtonClick("cancel"));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && _parryAndDodgePanel.activeSelf)
        {
            ParryOrDodgeButtonClick("cancel");
        }
    }

    #region Attack types
    private void InitializeAttackTypes()
    {
        // Dodajemy typy ataków do słownika
        AttackTypes.Add("StandardAttack", true);
        AttackTypes.Add("Charge", false);
        AttackTypes.Add("AllOutAttack", false);  // Szaleńczy atak
        AttackTypes.Add("GuardedAttack", false);  // Ostrożny atak
        AttackTypes.Add("Grappling", false);  // Atak wielokrotny
        AttackTypes.Add("Feint", false);  // Finta
        AttackTypes.Add("Stun", false);  // Ogłuszanie
        AttackTypes.Add("Disarm", false);  // Rozbrajanie
    }

    // Metoda ustawiająca dany typ ataku
    public void ChangeAttackType(string attackTypeName = null)
    {
        if (Unit.SelectedUnit == null) return;

        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();

        if (attackTypeName == null && unit.Entangled == 0 && unit.EntangledUnitId == 0)
        {
            attackTypeName = "StandardAttack";
        }
        else if(attackTypeName == null)
        {
            attackTypeName = "Grappling";
        }

        //Resetuje szarżę lub bieg, jeśli były aktywne
        if (attackTypeName != "Charge" && unit.IsCharging)
        {
            MovementManager.Instance.UpdateMovementRange(1);
        }

        if (attackTypeName == "Charge" && (!unit.CanDoAction || !unit.CanMove))
        {
            Debug.Log("Ta jednostka nie może wykonać szarży w obecnej rundzie.");
            return;
        }

        // Sprawdzamy, czy słownik zawiera podany typ ataku
        if (AttackTypes.ContainsKey(attackTypeName))
        {
            // Ustawiamy wszystkie typy ataków na false
            List<string> keysToReset = new List<string>();

            foreach (var key in AttackTypes.Keys)
            {
                if (key != attackTypeName)
                {
                    keysToReset.Add(key);
                }
            }

            foreach (var key in keysToReset)
            {
                AttackTypes[key] = false;
            }

            AttackTypes[attackTypeName] = true;

            //// Zmieniamy wartość bool dla danego typu ataku na true, a jeśli już był true to zmieniamy na standardowy atak.
            //if (!AttackTypes[attackTypeName])
            //{
            //    AttackTypes[attackTypeName] = true;
            //}
            //else
            //{
            //    AttackTypes[attackTypeName] = false;
            //    AttackTypes["StandardAttack"] = true;
            //    if (unit.GetComponent<Stats>().TempSz == unit.GetComponent<Stats>().Sz * 2)
            //    {
            //        MovementManager.Instance.UpdateMovementRange(1);
            //    }
            //}

            ////Ogłuszanie jest dostępne tylko dla jednostek ze zdolnością ogłuszania
            //if (AttackTypes["Stun"] == true && unit.GetComponent<Stats>().StrikeToStun == false)
            //{
            //    AttackTypes[attackTypeName] = false;
            //    AttackTypes["StandardAttack"] = true;
            //    Debug.Log("Ogłuszanie mogą wykonywać tylko jednostki posiadające tą zdolność.");
            //}

            ////Rozbrajanie jest dostępne tylko dla jednostek ze zdolnością rozbrajania
            //if (AttackTypes["Disarm"] == true && unit.GetComponent<Stats>().Disarm == false)
            //{
            //    AttackTypes[attackTypeName] = false;
            //    AttackTypes["StandardAttack"] = true;
            //    Debug.Log("Rozbrajanie mogą wykonywać tylko jednostki posiadające tą zdolność.");
            //}

            ////Ograniczenie finty, ogłuszania i rozbrajania do ataków w zwarciu
            //if ((AttackTypes["Feint"] || AttackTypes["Stun"] || AttackTypes["Disarm"] || AttackTypes["Charge"]) == true && unit.GetComponent<Inventory>().EquippedWeapons[0] != null && unit.GetComponent<Inventory>().EquippedWeapons[0].Type.Contains("ranged"))
            //{
            //    AttackTypes[attackTypeName] = false;
            //    AttackTypes["StandardAttack"] = true;
            //    Debug.Log("Jednostka walcząca bronią dystansową nie może wykonać tej akcji.");
            //}
            //// else if ((AttackTypes["AllOutAttack"] || AttackTypes["GuardedAttack"] || AttackTypes["Charge"]) == true)
            //// {
            ////     AttackTypes[attackTypeName] = false;
            ////     AttackTypes["StandardAttack"] = true;
            ////     Debug.Log("Ta jednostka nie może w tej rundzie wykonać akcji podwójnej.");
            //// }
            //else

            // Podczas pochwycenia lub pochwytywania kogoś możemy tylko wykonywac atak typu Zapasy
            if (attackTypeName != "Grappling" && (unit.Entangled > 0 || unit.EntangledUnitId != 0))
            {
                AttackTypes[attackTypeName] = false;
                AttackTypes["Grappling"] = true;

                Debug.Log("Ta jednostka w obecnej rundzie nie może wykonywać innej akcji ataku niż zapasy.");
            }

            if (AttackTypes["Charge"] == true && !unit.IsCharging)
            {
                bool isEngagedInCombat = AdjacentOpponents(unit.transform.position, unit.tag).Count > 0 ? true : false;

                if (isEngagedInCombat == true)
                {
                    Debug.Log("Ta jednostka nie może wykonać szarży, bo jest związana walką.");
                    return;
                }

                if(!unit.CanDoAction || !unit.CanMove)
                {
                    Debug.Log("Ta jednostka nie może wykonać szarży w obecnej rundzie.");
                    return;
                }

                MovementManager.Instance.UpdateMovementRange(2, null, true);
                MovementManager.Instance.Retreat(false); // Zresetowanie bezpiecznego odwrotu
            }
        }

        UpdateAttackTypeButtonsColor();
    }

    public void UpdateAttackTypeButtonsColor()
    {
        _standardAttackButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["StandardAttack"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _chargeButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["Charge"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _allOutAttackButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["AllOutAttack"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _guardedAttackButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["GuardedAttack"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _grapplingButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["Grappling"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _feintButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["Feint"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _stunButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["Stun"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _disarmButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["Disarm"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
    }

    #endregion

    #region Attack function
    public void Attack(Unit attacker, Unit target, bool opportunityAttack = false)
    {
        StartCoroutine(AttackCoroutine(attacker, target, opportunityAttack));
    }
    private IEnumerator AttackCoroutine(Unit attacker, Unit target, bool opportunityAttack)
    {
        // 1) Sprawdź, czy gra jest wstrzymana
        if (GameManager.IsGamePaused)
        {
            Debug.Log("Gra została wstrzymana. Aby ją wznowić musisz wyłączyć okno znajdujące się na polu gry.");
            yield break;
        }

        // 2) Sprawdź, czy jednostka może wykonać atak
        if (!attacker.CanDoAction && !opportunityAttack)
        {
            Debug.Log("Wybrana jednostka nie może wykonać ataku w tej rundzie.");
            yield break;
        }

        // 3) Pobierz statystyki i broń
        Stats attackerStats = attacker.Stats;
        Stats targetStats = target.Stats;

        Weapon attackerWeapon = null;
        Weapon targetWeapon = null;
        if (AttackTypes["Grappling"] == true) // Zapasy
        {
            attackerStats.GetComponent<Weapon>().ResetWeapon();
            attackerWeapon = attackerStats.GetComponent<Weapon>();

            targetStats.GetComponent<Weapon>().ResetWeapon();
            targetWeapon = targetStats.GetComponent<Weapon>();

            if(attacker.Entangled > 0 && target.EntangledUnitId != attacker.UnitId)
            {
                Debug.Log("Celem ataku musi być jednostka, z którą toczą się zapasy.");
                yield break;
            }
        }
        else // Zwykły atak bronią
        {
            attackerWeapon = InventoryManager.Instance.ChooseWeaponToAttack(attacker.gameObject);
            targetWeapon = InventoryManager.Instance.ChooseWeaponToAttack(target.gameObject);
        }

        // Ustalamy umiejętności, które będą testowane w zależności od kategorii broni
        MeleeCategory meleeSkill = EnumConverter.ParseEnum<MeleeCategory>(attackerWeapon.Category) ?? MeleeCategory.Basic;
        RangedCategory rangedSkill = EnumConverter.ParseEnum<RangedCategory>(attackerWeapon.Category) ?? RangedCategory.Bow;
        int skillModifier = attackerWeapon.Type.Contains("ranged") ? attackerStats.GetSkillModifier(attackerStats.Ranged, rangedSkill) : attackerStats.GetSkillModifier(attackerStats.Melee, meleeSkill);
        _isTrainedWeaponCategory = skillModifier > 0 ? true : false;

        Debug.Log("Czy jednostka jest wyszkolona w tej broni: " + _isTrainedWeaponCategory);

        if (attackerWeapon.Type.Contains("ranged") && !_isTrainedWeaponCategory && attackerWeapon.Category != "crossbow" && attackerWeapon.Category != "throwing")
        {
            Debug.Log("Wybrana jednostka nie może walczyć przy użyciu broni z tej kategorii.");
            yield break;
        }

        // 4) Oblicz dystans
        float attackDistance = CalculateDistance(attacker.gameObject, target.gameObject);

        // 5) Sprawdź zasięg i ewentualnie wykonaj szarżę
        float effectiveAttackRange = attackerWeapon.AttackRange;

        if (attackerWeapon.Type.Contains("throwing")) // Oblicz właściwy zasięg ataku, uwzględniając broń miotaną
        {
            effectiveAttackRange *= attackerStats.S / 10f;
        }

        bool isOutOfRange = attackDistance > effectiveAttackRange;
        bool isRangedAndTooFar = attackerWeapon.Type.Contains("ranged") && attackDistance > effectiveAttackRange * 3 && attackerWeapon.Category != "entangling";

        if (isOutOfRange && (!attackerWeapon.Type.Contains("ranged") || isRangedAndTooFar))
        {
            // Poza zasięgiem
            if (attacker.IsCharging)
            {
                // Jeżeli to miała być szarża, próbujemy ją wykonać
                Charge(attacker.gameObject, target.gameObject);
            }
            else
            {
                Debug.Log("Cel jest poza zasięgiem ataku.");
            }
            yield break;
        }

        // 6) Sprawdzenie dodatkowych warunków dla ataku dystansowego (np. przeszkody, czy broń jest naładowana, itp.)
        if (attackerWeapon.Type.Contains("ranged"))
        {
            bool validRanged = ValidateRangedAttack(attacker, target, attackerWeapon, attackDistance);
            if (!validRanged) yield break;


            // ==================================================================
            //DODAĆ MODYFIKATORY ZA PRZESZKODY NA LINII STRZAŁU
            // ==================================================================
        }

        // 7) Określamy, czy atak jest manualny czy automatyczny
        IsManualPlayerAttack = attacker.CompareTag("PlayerUnit") && GameManager.IsAutoDiceRollingMode == false;

        // 8) Jeśli to nie atak okazyjny – zużywamy akcję
        if (!opportunityAttack)
        {
            RoundsManager.Instance.DoAction(attacker);
        }

        // ==================================================================
        // 9) *** RZUT ATAKU *** (manualny lub automatyczny)
        // ==================================================================
        int attackModifier = CalculateAttackModifier(attacker, attackerWeapon, target, attackDistance);

        //Zwiększenie modyfikatora do ataku za atak okazyjny
        if(opportunityAttack) attackModifier += 20;
        if(attackModifier > 60) attackModifier = 60; // Górny limit modyfikatora
        if(attackModifier < -30) attackModifier = -30; // Dolny limit modyfikatora

        int rollOnAttack;
        if (IsManualPlayerAttack)
        {
            string message = $"Wykonaj rzut kośćmi na trafienie.";
            if (attackModifier > 0)
            {
                message += $" Modyfikator: <color=green>{attackModifier}</color>.";
            }
            else if (attackModifier < 0)
            {
                message += $" Modyfikator: <color=red>{attackModifier}</color>.";
            }
            message += $" Jeśli atak chybi, wyłącz okno znajdujące się na środku ekranu.";
            Debug.Log(message);

            // Wywołujemy panel do wpisania wyniku (korutyna czeka, żeby reszta kodu nie poszła od razu dalej)
            yield return StartCoroutine(WaitForRollValue());
            rollOnAttack = _manualRollResult;
        }
        else
        {
            // Automatyczny rzut
            rollOnAttack = UnityEngine.Random.Range(1, 101);
        }

        // 10) Liczymy poziomy sukcesu atakującego
        int skillValue;

        if (AttackTypes["Grappling"])
        {
            skillValue = attackerStats.S;
        }
        else if (attackerWeapon.Type.Contains("ranged"))
        {
            skillValue = attackerStats.US + attackerStats.GetSkillModifier(attackerStats.Ranged, rangedSkill);
        }
        else
        {
            skillValue = attackerStats.WW + attackerStats.GetSkillModifier(attackerStats.Melee, meleeSkill);
        }

        int[] results = CalculateSuccessLevel(attackerWeapon, rollOnAttack, skillValue, true, attackModifier);
        int attackerSuccessValue = results[0];
        int attackerSuccessLevel = results[1];

        string successLevelColor = results[0] >= 0 ? "green" : "red";
        string modifierString = attackModifier != 0 ? $" Modyfikator: {attackModifier}," : "";

        if (AttackTypes["Grappling"] == true)
        {
            Debug.Log($"{attackerStats.Name} próbuje pochwycić przeciwnika. Wynik rzutu: {rollOnAttack}, Wartość umiejętności: {skillValue},{modifierString} PS: <color={successLevelColor}>{attackerSuccessLevel}</color>");
        }
        else
        {
            Debug.Log($"{attackerStats.Name} atakuje przy użyciu {attackerWeapon.Name}. Wynik rzutu: {rollOnAttack}, Wartość umiejętności: {skillValue},{modifierString} PS: <color={successLevelColor}>{attackerSuccessLevel}</color>");
        }

        // Obsługa fuksa / pecha

        bool isFortunateOrUnfortunateEvent = false; // Zmienna używana do tego, aby nie powielać dwa razy szczęścia lub pecha w przypadku specyficznych broni

        if (IsDoubleDigit(rollOnAttack) || (attackerWeapon.Impale && rollOnAttack % 10 == 0) || (attackerWeapon.Dangerous && attackerSuccessValue < 0 && (rollOnAttack % 10 == 9 || rollOnAttack / 10 == 9)))
        {
            isFortunateOrUnfortunateEvent = true;

            if (attackerSuccessValue >= 0)
            {
                Debug.Log($"{attackerStats.Name} wyrzucił <color=green>FUKSA</color> na trafienie!");
                attackerStats.FortunateEvents++;
            }
            else if (IsDoubleDigit(rollOnAttack) || (attackerWeapon.Dangerous && (rollOnAttack % 10 == 9 || rollOnAttack / 10 == 9)))
            {
                Debug.Log($"{attackerStats.Name} wyrzucił <color=red>PECHA</color> na trafienie!");
                attackerStats.UnfortunateEvents++;
            }
        }

        // Obsługa FUKSA i PECHA
        bool isDoubleRoll = IsDoubleDigit(rollOnAttack);
        bool isImpaleRoll = attackerWeapon.Impale && rollOnAttack % 10 == 0;
        bool isDangerousRoll = attackerWeapon.Dangerous && (rollOnAttack % 10 == 9 || rollOnAttack / 10 == 9) && attackerSuccessValue < 0;
        bool isSpecialRoll = isDoubleRoll || isImpaleRoll || isDangerousRoll; // Sprawdzamy, czy mamy do czynienia z którymś z „wyjątkowych” rzutów

        if (isSpecialRoll && !isFortunateOrUnfortunateEvent)
        {
            if (attackerSuccessValue >= 0)
            {
                Debug.Log($"{attackerStats.Name} wyrzucił <color=green>FUKSA</color> na trafienie!");
                attackerStats.FortunateEvents++;
            }
            else
            {
                // Tu sprawdzamy tylko double albo „niebezpieczny” rzut na 9, bo impale przy nieudanym rzucie nie wywołuje żadnego efektu.
                if (isDoubleRoll || isDangerousRoll)
                {
                    Debug.Log($"{attackerStats.Name} wyrzucił <color=red>PECHA</color> na trafienie!");
                    attackerStats.UnfortunateEvents++;
                }
            }
        }

        // Jeśli to była broń dystansowa – resetujemy ładowanie
        if (attackerWeapon.Type.Contains("ranged"))
        {
            ResetWeaponLoad(attackerWeapon, attackerStats);
        }

        // ==================================================================
        // 11) *** OBRONA *** (tylko jeśli to atak w zwarciu i lub mamy tarcze i możemy bronić się przed strzałem)
        // ==================================================================
        int defenceSuccessValue = 0;
        int defenceSuccessLevel = 0;
        int defenceRollResult = 0;

        if (AttackTypes["Grappling"]) // Zapasy
        {
            // Jeżeli jesteśmy w trybie manualnych rzutów kośćmi
            if (!GameManager.IsAutoDiceRollingMode && target.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(WaitForDefenceRollValue());
                defenceRollResult = _manualRollResult;
            }
            else
            {
                defenceRollResult = UnityEngine.Random.Range(1, 101);
            }

            defenceSuccessValue = UnitsManager.Instance.TestSkill("S", targetStats, null, -10, defenceRollResult);
            defenceSuccessLevel = defenceSuccessValue / 10;
        }
        else // Zwykły atak bronią
        {
            // Modyfikatory do parowania i uników
            int parryModifier = 0;
            int dodgeModifier = 0;
            if (target.DefensiveBonus != 0) // Modyfikator za pozycję obronną
            {
                parryModifier += target.DefensiveBonus;
                dodgeModifier += target.DefensiveBonus;
            }

            if (target.Fatiqued > 0) // Modyfikator za wyczerpanie
            {
                parryModifier -= target.Fatiqued * 10;
                dodgeModifier -= target.Fatiqued * 10;
            }
            else if (target.Stunned > 0) // Modyfikator za oszołomienie
            {
                parryModifier -= 10;
                dodgeModifier -= 10;
            }
            else if (target.Poison > 0) // Modyfikator za zatrucie
            {
                parryModifier -= 10;
                dodgeModifier -= 10;
            }

            if (attackerWeapon.Wrap && _isTrainedWeaponCategory) parryModifier -= 10;
            if (attackerWeapon.Slow)
            {
                parryModifier += 10;
                dodgeModifier += 10;
            }
            if (targetWeapon.Defensive) parryModifier += 10;
            if (targetWeapon.Unbalanced) parryModifier -= 10;
            if (attackerStats.Size > targetStats.Size) parryModifier -= (attackerStats.Size - targetStats.Size) * 20; // Kara do parowania za rozmiar
            if (attackerStats.Size > targetStats.Size) Debug.Log($"modyfikator parowania za rozmiar -{(attackerStats.Size - targetStats.Size) * 20}");
            if (parryModifier < -30) parryModifier = -30; // Dolny limit modyfikatora

            string parryModifierString = parryModifier != 0 ? $" Modyfikator: {parryModifier}," : "";
            string dodgeModifierString = dodgeModifier != 0 ? $" Modyfikator: {dodgeModifier}," : "";

            // Sprawdzenie, czy jednostka może próbować parować lub unikać ataku
            bool canParry = attackerWeapon.Type.Contains("melee") || target.GetComponent<Inventory>().EquippedWeapons.Any(weapon => weapon != null && weapon.Shield >= 2);
            bool canDodge = attackerWeapon.Type.Contains("melee");

            // Obliczamy sumaryczną wartość parowania i uniku
            MeleeCategory targetMeleeSkill = EnumConverter.ParseEnum<MeleeCategory>(targetWeapon.Category) ?? MeleeCategory.Basic;
            int parryValue = targetStats.WW + targetStats.GetSkillModifier(targetStats.Melee, targetMeleeSkill) + parryModifier;
            int dodgeValue = targetStats.Dodge + targetStats.Zw + dodgeModifier;


            if (canParry || canDodge)
            {
                // Jeśli AutoDefense jest wyłączone => czekamy, aż gracz wybierze reakcję obronną
                if (!GameManager.IsAutoDefenseMode)
                {
                    Debug.Log("Wybierz reakcję atakowanej postaci.");

                    _parryAndDodgePanel.SetActive(true);

                    _dodgeButton.gameObject.SetActive(canDodge);
                    _parryButton.gameObject.SetActive(canParry);

                    // Najpierw czekamy, aż gracz kliknie którykolwiek przycisk
                    yield return new WaitUntil(() => _parryAndDodgePanel.activeSelf == false);

                    // Jeżeli jesteśmy w trybie manualnych rzutów kośćmi i wybrano parowanie lub unik, to czekamy na wynik rzutu
                    if ((_parryOrDodge == "parry" || _parryOrDodge == "dodge") && !GameManager.IsAutoDiceRollingMode && target.CompareTag("PlayerUnit"))
                    {
                        yield return StartCoroutine(WaitForDefenceRollValue());
                        defenceRollResult = _manualRollResult;
                    }
                    else if (_parryOrDodge == "parry" || _parryOrDodge == "dodge")
                    {
                        defenceRollResult = UnityEngine.Random.Range(1, 101);
                    }
                    else if (_parryOrDodge == "cancel")
                    {
                        // Kontynuujemy korytynę
                    }

                    // Policz poziom sukcesu obrońcy
                    if (_parryOrDodge == "parry")
                    {
                        // Parowanie
                        int[] defenceResults = CalculateSuccessLevel(targetWeapon, defenceRollResult, parryValue, false);
                        defenceSuccessValue = defenceResults[0];
                        defenceSuccessLevel = defenceResults[1];

                        string coloredText = defenceSuccessValue >= 0 ? "green" : "red";
                        Debug.Log($"{targetStats.Name} próbuje parować. Wynik rzutu: {defenceRollResult}, Wartość umiejętności: {targetStats.WW + targetStats.GetSkillModifier(targetStats.Melee, targetMeleeSkill)},{parryModifierString} PS: <color={coloredText}>{defenceSuccessLevel}</color>");
                    }
                    else if (_parryOrDodge == "dodge")
                    {
                        // Unik
                        int[] defenceResults = CalculateSuccessLevel(targetWeapon, defenceRollResult, dodgeValue, false);
                        defenceSuccessValue = defenceResults[0];
                        defenceSuccessLevel = defenceResults[1];

                        string coloredText = defenceSuccessValue >= 0 ? "green" : "red";
                        Debug.Log($"{targetStats.Name} próbuje unikać. Wynik rzutu: {defenceRollResult}, Wartość umiejętności: {targetStats.Dodge + targetStats.Zw},{dodgeModifierString} PS: <color={coloredText}>{defenceSuccessLevel}</color>");
                    }

                    // Resetujemy wybór reakcji obronnej
                    _parryOrDodge = "";
                }
                else
                {
                    defenceRollResult = UnityEngine.Random.Range(1, 101);

                    if (parryValue >= dodgeValue)
                    {
                        // Parowanie
                        int[] defenceResults = CalculateSuccessLevel(targetWeapon, defenceRollResult, parryValue, false);
                        defenceSuccessValue = defenceResults[0];
                        defenceSuccessLevel = defenceResults[1];

                        string coloredText = defenceSuccessValue >= 0 ? "green" : "red";
                        Debug.Log($"{targetStats.Name} próbuje parować. Wynik rzutu: {defenceRollResult}, Wartość umiejętności: {targetStats.WW + targetStats.GetSkillModifier(targetStats.Melee, targetMeleeSkill)},{parryModifierString} PS: <color={coloredText}>{defenceSuccessLevel}</color>");
                    }
                    else
                    {
                        // Unik
                        int[] defenceResults = CalculateSuccessLevel(targetWeapon, defenceRollResult, dodgeValue, false);
                        defenceSuccessValue = defenceResults[0];
                        defenceSuccessLevel = defenceResults[1];

                        string coloredText = defenceSuccessValue >= 0 ? "green" : "red";
                        Debug.Log($"{targetStats.Name} próbuje unikać. Wynik rzutu: {defenceRollResult}, Wartość umiejętności: {targetStats.Dodge + targetStats.Zw},{dodgeModifierString} PS: <color={coloredText}>{defenceSuccessLevel}</color>");
                    }
                }
            }

            // Obsługa fuksa / pecha
            if (IsDoubleDigit(defenceRollResult))
            {
                if (defenceSuccessValue >= 0)
                {
                    Debug.Log($"{targetStats.Name} wyrzucił <color=green>FUKSA</color>!");
                    targetStats.FortunateEvents++;
                }
                else
                {
                    Debug.Log($"{targetStats.Name} wyrzucił <color=red>PECHA</color>!");
                    targetStats.UnfortunateEvents++;
                }
            }
        }

        //Resetujemy czas przeładowania broni celu ataku, bo ładowanie zostało zakłócone przez atak, przed którym musiał się bronić.
        if (targetWeapon.ReloadLeft != 0)
        {
            ResetWeaponLoad(targetWeapon, targetStats);
        }

        // 12) Teraz dopiero wiemy, ile wynoszą poziomy sukcesu atakującego i obrońcy
        // Następuje finalne rozstrzygnięcie
        int combinedSuccessLevel = attackerSuccessLevel - defenceSuccessLevel;
        if (combinedSuccessLevel <= 0 && attackerWeapon.Type.Contains("melee") || combinedSuccessLevel < 0 && attackerWeapon.Type.Contains("ranged"))
        {
            // Atak chybił
            Debug.Log($"Atak {attackerStats.Name} chybił.");
            StartCoroutine(AnimationManager.Instance.PlayAnimation("miss", null, target.gameObject));
            yield break;
        }
        else
        {
            //Zaktualizowanie przewagi
            InitiativeQueueManager.Instance.CalculateAdvantage(attacker.tag, 1);
            string group = attacker.tag == "PlayerUnit" ? "sojuszników" : "przeciwników";
            Debug.Log($"Przewaga {group} została zwiększona o <color=#4dd2ff>1</color>.");
        }

        //W przypadku manualnego ataku sprawdzamy, czy postać powinna zakończyć turę
        if (IsManualPlayerAttack && !attacker.CanMove && !attacker.CanDoAction)
        {
            RoundsManager.Instance.FinishTurn();
        }

        if (attackerWeapon.Type.Contains("no-damage")) yield break; //Jeśli broń nie powoduje obrażeń, np. arkan, to pomijamy dalszą część kodu

        // Udana próba pochwycenia przeciwnika
        if(AttackTypes["Grappling"] == true && attacker.EntangledUnitId != target.UnitId)
        {
            attacker.EntangledUnitId = target.UnitId;
            target.Entangled++;
            target.CanMove = false;
            attacker.CanMove = false;
            MovementManager.Instance.SetCanMoveToggle(false);
            RoundsManager.Instance.FinishTurn();

            Debug.Log($"{attackerStats.Name} pochwycił {targetStats.Name}");

            yield break;
        }
        
        // 13) Jeśli atakujący wygrywa, zadaj obrażenia
        // Oblicz pancerz i finalne obrażenia
        int armor = CalculateArmor(targetStats, attackerWeapon);
        int damage = CalculateDamage(rollOnAttack, combinedSuccessLevel, attackerStats, targetStats, attackerWeapon);

        Debug.Log($"{attackerStats.Name} zadaje {damage} obrażeń.");

        // 14) Zadaj obrażenia
        ApplyDamageToTarget(damage, armor, attackerStats, attackerWeapon, targetStats, target);

        // 15) Animacja ataku i ewentualnie sprawdzenie śmierci
        StartCoroutine(AnimationManager.Instance.PlayAnimation("attack", attacker.gameObject, target.gameObject));

        if (targetStats.TempHealth < 0 && GameManager.IsAutoKillMode)
        {
            HandleDeath(targetStats, target.gameObject, attackerStats);
        }
    }

    private void ApplyDamageToTarget(int damage, int armor, Stats attackerStats, Weapon attackerWeapon, Stats targetStats, Unit target)
    {
        // targetStats.TotalDamageTaken += damage; itp.
        int targetWt = targetStats.Wt / 10;
        int reducedDamage = armor + targetWt;
        int finalDamage = 0;

        // W zapasach zbroja nie jest uzwględniania
        if(AttackTypes["Grappling"] == true)
        {
            reducedDamage -= armor;
        }

        if (damage > reducedDamage)
        {
            finalDamage = damage - reducedDamage;
        }
        else
        {
            // Jeśli atak nie przebił pancerza, ale broń NIE JEST tępa, to broń zadaje 1 obrażeń
            if (!attackerWeapon.Undamaging) 
            {
                finalDamage = 1;
                reducedDamage = damage - 1;
            }
        }

        if (finalDamage > 0)
        {
            targetStats.TempHealth -= finalDamage;
            Debug.Log($"{targetStats.Name} znegował {reducedDamage} obrażeń.");

            //Informacja o punktach żywotności po zadaniu obrażeń
            if (!GameManager.IsHealthPointsHidingMode || target.CompareTag("PlayerUnit"))
            {
                if (targetStats.TempHealth < 0)
                {
                    Debug.Log($"Punkty żywotności {targetStats.Name}: <color=red>{targetStats.TempHealth}</color><color=#4dd2ff>/{targetStats.MaxHealth}</color>");
                }
                else
                {
                    Debug.Log($"Punkty żywotności {targetStats.Name}: <color=red>{targetStats.TempHealth}</color><color=#4dd2ff>/{targetStats.MaxHealth}</color>");
                }
                target.DisplayUnitHealthPoints();
            }

            StartCoroutine(AnimationManager.Instance.PlayAnimation("damage", null, target.gameObject, finalDamage));
        }
        else
        {
            Debug.Log("Atak nie przebił pancerza i nie zadał obrażeń.");
            StartCoroutine(AnimationManager.Instance.PlayAnimation("parry", null, target.gameObject));
        }
    }

    private int[] CalculateSuccessLevel(Weapon weapon, int rollResult, int skillValue, bool isAttack, int modifier = 0)
    {
        int successValue = skillValue + modifier - rollResult;
        int successLevel = (skillValue + modifier) / 10 - rollResult / 10;

        if (successValue < 0) // Nieudany test
        {
            if (weapon.Practical && _isTrainedWeaponCategory) // Uwzględnia zaletę przedmiotu "Praktyczny"
            {
                Debug.Log($"Używamy cechy Praktyczny i podnosimy PS z {successLevel} na {successLevel + 1}");
                successLevel++;
            }
            else if (weapon.Unrielable) // Uwzględnia zaletę przedmiotu "Zawodny"
            {
                Debug.Log($"Używamy cechy Zawodny i obniżamy PS z {successLevel} na {successLevel - 1}");
                successLevel--;
            }
        }
        else if(isAttack) // Udany test
        {
            if (weapon.Precise && _isTrainedWeaponCategory) // Uwzględnia zaletę przedmiotu "Precyzyjny"
            {
                Debug.Log($"Używamy cechy Precyzyjny i podnosimy PS z {successLevel} na {successLevel + 1}");
                successLevel++;
            }
        }

        if (weapon.Imprecise && isAttack) // Uwzględnia zaletę przedmiotu "Nieprecyzyjny"
        {
            Debug.Log($"Używamy cechy Nieprecyzyjny i obniżamy PS z {successLevel} na {successLevel - 1}");
            successLevel--;
        }

        return new int[] { successValue, successLevel };
    }

    public void HandleDeath(Stats targetStats, GameObject target, Stats attackerStats)
    {
        // Zapobiega usunięciu postaci graczy, gdy statystyki przeciwników są ukryte
        if (GameManager.IsStatsHidingMode && targetStats.gameObject.CompareTag("PlayerUnit"))
        {
            return;
        }

        StartCoroutine(AnimationManager.Instance.PlayAnimation("kill", attackerStats.gameObject, target));

        //Aktualizuje osiągnięcia
        attackerStats.OpponentsKilled++;
        if (attackerStats.StrongestDefeatedOpponentOverall < targetStats.Overall)
        {
            attackerStats.StrongestDefeatedOpponentOverall = targetStats.Overall;
            attackerStats.StrongestDefeatedOpponent = targetStats.Name;
        }

        // Usuwanie jednostki
        UnitsManager.Instance.DestroyUnit(target);

        // Aktualizacja podświetlenia pól w zasięgu ruchu atakującego
        GridManager.Instance.HighlightTilesInMovementRange(attackerStats);
    }
    #endregion

    #region Calculating distance and validating distance attack
    public float CalculateDistance(GameObject attacker, GameObject target)
    {
        if (attacker != null && target != null)
        {
            return Vector2.Distance(attacker.transform.position, target.transform.position);
        }
        else
        {
            Debug.LogError("Nie udało się ustalić odległości pomiędzy walczącymi.");
            return 0;
        }
    }

    public bool ValidateRangedAttack(Unit attacker, Unit target, Weapon attackerWeapon, float attackDistance)
    {
        // Sprawdza, czy broń jest naładowana
        if (attackerWeapon.ReloadLeft != 0)
        {
            Debug.Log($"Broń wymaga przeładowania.");
            return false;
        }

        // Sprawdza, czy cel nie znajduje się zbyt blisko
        if (attackDistance <= 1.5f && !attackerWeapon.Pistol)
        {
            Debug.Log($"Jednostka stoi zbyt blisko celu, aby wykonać atak dystansowy.");

            return false;
        }

        // Sprawdza, czy na linii strzału znajduje się przeszkoda
        RaycastHit2D[] raycastHits = Physics2D.RaycastAll(attacker.transform.position, target.transform.position - attacker.transform.position, attackDistance);

        foreach (var raycastHit in raycastHits)
        {
            if (raycastHit.collider == null) continue;

            var mapElement = raycastHit.collider.GetComponent<MapElement>();

            if (mapElement != null)
            {
                if (mapElement.IsHighObstacle)
                {
                    Debug.Log("Na linii strzału znajduje się przeszkoda, przez którą strzał jest niemożliwy.");
                    return false;
                }
            }
        }

        return true;
    }

    #endregion


    // Funkcja sprawdzająca, czy liczba ma dwie identyczne cyfry
    private bool IsDoubleDigit(int number)
    {
        // Jeśli wynik to dokładnie 100, również spełnia warunek
        if (number == 100) return true;

        // Sprawdzenie dla liczb dwucyfrowych
        if (number >= 10 && number <= 99)
        {
            int tens = number / 10;  // Cyfra dziesiątek
            int ones = number % 10; // Cyfra jedności
            return tens == ones;    // Sprawdzenie, czy cyfry są takie same
        }

        return false;
    }

    //Oblicza modyfikator do trafienia
    private int CalculateAttackModifier(Unit attackerUnit, Weapon attackerWeapon, Unit targetUnit, float attackDistance = 0)
    {
        int attackModifier = 0;

        Stats attackerStats = attackerUnit.GetComponent<Stats>();
        Stats targetStats = targetUnit.GetComponent<Stats>();

        // Modyfikator za rozmiar
        if (attackerStats.Size < targetStats.Size) attackModifier += 10;

        if (attackerStats.Size < targetStats.Size) Debug.Log($"modyfikator za rozmiar +10");

        // Modyfikator za szarżę
        if (attackerUnit.IsCharging) attackModifier += 10;

        // Modyfikator za broń z cechą "Celny"
        if (attackerWeapon.Accurate && _isTrainedWeaponCategory) attackModifier += 10;

        // Utrudnienie za atak słabszą ręką
        if (attackerUnit.GetComponent<Inventory>().EquippedWeapons[0] == null || attackerWeapon.Name != attackerUnit.GetComponent<Inventory>().EquippedWeapons[0].Name)
        {
            if (!attackerStats.Ambidextrous && attackerWeapon.Id != 0)
            {
                attackModifier -= 20;
            }
        }

        // Modyfikatory za jakość broni
        if (attackerWeapon.Quality == "Kiepska") attackModifier -= 5;
        else if (attackerWeapon.Quality == "Najlepsza" || attackerWeapon.Quality == "Magiczna") attackModifier += 5;

        // Modyfikator za stany celu
        if (targetUnit.Deafened > 0 || targetUnit.Stunned > 0 || targetUnit.Blinded > 0) attackModifier += 10;
        if (targetUnit.Surprised)
        {
            attackModifier += 20;
            targetUnit.Surprised = false;
        }
        if ((targetUnit.Entangled > 0 || targetUnit.EntangledUnitId != 0) && attackerUnit.Entangled == 0 && attackerUnit.EntangledUnitId == 0)
        {
            // DODAĆ TUTAJ W PRZYSZŁOŚCI, ŻEBY UWZGLĘDNIAŁO, KTO Z WALCZĄCYCH W ZAPASACH MA ILE POZIOMÓW POCHWYCENIA. NA TEGO, KTO MA MNIEJ JEST MODYFIKATOR +10, A TEGO KTO WIĘCEJ +20
      
            attackModifier += targetUnit.Entangled == 0 ? 10 : 20;
        }

        // Modyfikator za wyczerpanie
        if (attackerUnit.Fatiqued > 0) attackModifier -= attackerUnit.Fatiqued * 10;
        else if(attackerUnit.Poison > 0) attackModifier -= 10;

        if (attackerWeapon.Type.Contains("ranged"))
        {
            // Modyfikator za dystans
            attackModifier += attackDistance switch
            {
                _ when attackDistance <= attackerWeapon.AttackRange / 10 => 40, // Bezpośredni dystans
                _ when attackDistance <= attackerWeapon.AttackRange / 2 => 20,  // Bliski dystans
                _ when attackDistance <= attackerWeapon.AttackRange * 2 => -10, // Daleki dystans
                _ when attackDistance <= attackerWeapon.AttackRange * 3 => -30, // Bardzo daleki dystans
                _ => 0 // Domyślny przypadek, jeśli żaden warunek nie zostanie spełniony
            };

            //Modyfikator za oślepienie
            if(attackerUnit.Blinded > 0 && attackerUnit.Fatiqued == 0 && attackerUnit.Poison == 0) attackModifier -= 10;
        }

        // Przewaga liczebna
        attackModifier += CountOutnumber(attackerUnit, targetUnit);

        // Bijatyka
        if (attackerWeapon.Type.Contains("melee") &&
            (attackerWeapon.Id == 0 || attackerWeapon.Id == 11) &&
            attackerStats.StreetFighting)
        {
            attackModifier += 10;
        }

        //Zapiekła nienawiść
        if (attackerStats.GrudgeBornFury == true && _greenskinsList.Contains(targetStats.Race))
        {
            attackModifier += 5;
        }

        return attackModifier;
    }

    //Modyfikator za przewagę liczebną
    private int CountOutnumber(Unit attacker, Unit target)
    {
        if (attacker.CompareTag(target.tag)) return 0; // Jeśli atakujemy sojusznika to pomijamy przewagę liczebną

        int adjacentOpponents = 0; // Przeciwnicy atakującego stojący obok celu ataku
        int adjacentAllies = 0;    // Sojusznicy atakującego stojący obok celu ataku
        int adjacentOpponentsNearAttacker = 0; // Przeciwnicy atakującego stojący obok atakującego
        int modifier = 0;

        // Zbiór do przechowywania już policzonych przeciwników
        HashSet<Collider2D> countedOpponents = new HashSet<Collider2D>();

        // Funkcja pomocnicza do zliczania jednostek w sąsiedztwie danej pozycji
        void CountAdjacentUnits(Vector2 center, string allyTag, string opponentTag, ref int allies, ref int opponents)
        {
            Vector2[] positions = {
                center,
                center + Vector2.right,
                center + Vector2.left,
                center + Vector2.up,
                center + Vector2.down,
                center + new Vector2(1, 1),
                center + new Vector2(-1, -1),
                center + new Vector2(-1, 1),
                center + new Vector2(1, -1)
            };

            foreach (var pos in positions)
            {
                Collider2D collider = Physics2D.OverlapPoint(pos);
                if (collider == null) continue;

                if (collider.CompareTag(allyTag) && InventoryManager.Instance.ChooseWeaponToAttack(collider.gameObject).Type.Contains("melee"))
                {
                    allies++;
                }
                else if (collider.CompareTag(opponentTag) && !countedOpponents.Contains(collider) && InventoryManager.Instance.ChooseWeaponToAttack(collider.gameObject).Type.Contains("melee"))
                {
                    opponents++;
                    countedOpponents.Add(collider); // Dodajemy do zestawu zliczonych przeciwników
                }
            }
        }

        // Zlicza sojuszników i przeciwników atakującego w sąsiedztwie celu ataku
        CountAdjacentUnits(target.transform.position, attacker.tag, target.tag, ref adjacentAllies, ref adjacentOpponents);

        // Zlicza przeciwników atakujacego w sąsiedztwie atakującego (bez liczenia jego sojuszników, bo oni nie mają wpływu na przewagę)
        int ignoredAllies = 0; // Tymczasowy licznik, ignorowany
        CountAdjacentUnits(attacker.transform.position, attacker.tag, target.tag, ref ignoredAllies /* ignorujemy sojuszników */, ref adjacentOpponentsNearAttacker);

        // Dodaje przeciwników w sąsiedztwie atakującego do całkowitej liczby jego przeciwników
        adjacentOpponents += adjacentOpponentsNearAttacker;

        // Wylicza modyfikator na podstawie stosunku przeciwników do sojuszników atakującego
        if (adjacentAllies >= adjacentOpponents * 3)
        {
            modifier = 40;
        }
        else if (adjacentAllies >= adjacentOpponents * 2)
        {
            modifier = 20;
        }

        return modifier;
    }

    #region Calculating damage
    int CalculateDamage(int attackRoll, int successLevel, Stats attackerStats, Stats targetStats, Weapon attackerWeapon)
    {
        int damage;

        // Uwzględnienie cechy broni "Przebijająca"
        if ((attackerWeapon.Damaging || attackerStats.Size - targetStats.Size >= 1) && successLevel < attackRoll % 10 && (!attackerWeapon.Tiring || attackerStats.GetComponent<Unit>().IsCharging) && _isTrainedWeaponCategory)
        {
            Debug.Log($"Używamy cechy Przebijająca i zamieniamy PS z {successLevel} na {attackRoll % 10}");
            successLevel = attackRoll % 10;
        }

        if (attackerWeapon.Type.Contains("melee") || attackerWeapon.Type.Contains("strength-based")) //Oblicza łączne obrażenia dla ataku w zwarciu
        {
            Debug.Log($"succesLevel {successLevel} siła atakującego: {attackerStats.S / 10} siła broni: {Math.Max(0, attackerWeapon.S - attackerWeapon.Damage)}");
            damage = successLevel + attackerStats.S / 10 + Math.Max(0, attackerWeapon.S - attackerWeapon.Damage);
        }
        else //Oblicza łączne obrażenia dla ataku dystansowego
        {
            damage = successLevel + attackerWeapon.S;
        }

        // Uwzględnia cechę Druzgoczący
        if (attackerWeapon.Impact && (!attackerWeapon.Tiring || attackerStats.GetComponent<Unit>().IsCharging) || attackerStats.Size - targetStats.Size >= 2 && _isTrainedWeaponCategory) 
        {
            damage += attackRoll % 10; // Dodaje liczbę jedności z rzutu na atak
        }

        // Uwzględnia przewagę rozmiaru
        if (attackerStats.Size > targetStats.Size)
        {
            damage *= attackerStats.Size - targetStats.Size;
            Debug.Log($"modyfikator obrazeń za rozmiar atakujący rozmiar {attackerStats.Size} cel rozmiar {targetStats.Size} mnożnik obrażeń {attackerStats.Size - targetStats.Size}");
        }

        if (damage < 0) damage = 0;

        return damage;
    }
    #endregion

    #region Check for attack localization and return armor value
    private int CalculateArmor(Stats targetStats, Weapon attackerWeapon)
    {
        int attackLocalization = UnityEngine.Random.Range(1, 101);
        int armor = 0;

        switch (attackLocalization)
        {
            case int n when (n >= 1 && n <= 9):
                Debug.Log("Trafienie w głowę.");
                armor = targetStats.Armor_head;
                break;
            case int n when (n >= 10 && n <= 24):
                Debug.Log("Trafienie w lewą rękę.");
                armor = targetStats.Armor_arms;
                break;
            case int n when (n >= 25 && n <= 44):
                Debug.Log("Trafienie w prawą rękę.");
                armor = targetStats.Armor_arms;
                break;
            case int n when (n >= 45 && n <= 79):
                Debug.Log("Trafienie w korpus.");
                armor = targetStats.Armor_torso;
                break;
            case int n when (n >= 80 && n <= 89):
                Debug.Log("Trafienie w lewą nogę.");
                armor = targetStats.Armor_legs;
                break;
            case int n when (n >= 90 && n <= 100):
                Debug.Log("Trafienie w prawą nogę.");
                armor = targetStats.Armor_legs;
                break;
        }

        //Podwaja wartość zbroi w przypadku walki przy użyciu broni Tępej
        if(attackerWeapon.Undamaging) armor *= 2;

        //Zwiększenie pancerza, jeśli cel ataku posiada tarcze
        armor += targetStats.GetComponent<Inventory>().EquippedWeapons.Where(weapon => weapon != null && weapon.Shield > 0).Select(weapon => weapon.Shield).FirstOrDefault();

        //Uwzględnienie broni przebijających zbroję
        if (attackerWeapon.Penetrating == true) armor --; // UWZGLĘDNIĆ TU W PRZYSZŁOŚCI, ŻE PANCERZE NIEMETALOWE POWINNY BYĆ CAŁKOWICIE IGNOROWANE

        return armor;
    }
    #endregion

    #region Charge
    public void Charge(GameObject attacker, GameObject target)
    {
        //Sprawdza pole, w którym atakujący zatrzyma się po wykonaniu szarży
        GameObject targetTile = GetTileAdjacentToTarget(attacker, target);

        Vector2 targetTilePosition;

        if(targetTile != null)
        {
            targetTilePosition = new Vector2(targetTile.transform.position.x, targetTile.transform.position.y);
        }
        else
        {
            Debug.Log($"Cel ataku stoi poza zasięgiem szarży.");
            return;
        }

        if(attacker.GetComponent<Unit>().Prone)
        {
            Debug.Log("Jednostka w stanie powalenia nie może wykonywać szarży.");
            return;
        }

        //Ścieżka ruchu szarżującego
        List<Vector2> path = MovementManager.Instance.FindPath(attacker.transform.position, targetTilePosition);

        //Sprawdza, czy postać jest wystarczająco daleko do wykonania szarży
        if (path.Count >= attacker.GetComponent<Stats>().Sz / 2f && path.Count <= attacker.GetComponent<Stats>().TempSz)
        {
            //Zapisuje grę przed wykonaniem ruchu, aby użycie punktu szczęścia wczytywało pozycję przed wykonaniem szarży i można było wykonać ją ponownie
            SaveAndLoadManager.Instance.SaveUnits(UnitsManager.Instance.AllUnits, "autosave");

            MovementManager.Instance.MoveSelectedUnit(targetTile, attacker);

            // Wywołanie funkcji z wyczekaniem na koniec animacji ruchu postaci
            float delay = 0.25f;
            StartCoroutine(DelayedAttack(attacker, target, path.Count * delay));

            IEnumerator DelayedAttack(GameObject attacker, GameObject target, float delay)
            {
                yield return new WaitForSeconds(delay);

                if(attacker == null || target == null) yield break;

                Attack(attacker.GetComponent<Unit>(), target.GetComponent<Unit>(), false);
                     
                ChangeAttackType(); // Resetuje szarżę
            }
        }
        else
        {
            ChangeAttackType(); // Resetuje szarżę

            Debug.Log("Zbyt mała odległość na wykonanie szarży.");
        }
    }

    // Szuka wolnej pozycji obok celu szarży, do której droga postaci jest najkrótsza
    public GameObject GetTileAdjacentToTarget(GameObject attacker, GameObject target)
    {
        if(target == null) return null;

        Vector2 targetPos = target.transform.position;

        //Wszystkie przylegające pozycje do atakowanego
        Vector2[] positions = { targetPos + Vector2.right,
            targetPos + Vector2.left,
            targetPos + Vector2.up,
            targetPos + Vector2.down,
            targetPos + new Vector2(1, 1),
            targetPos + new Vector2(-1, -1),
            targetPos + new Vector2(-1, 1),
            targetPos + new Vector2(1, -1)
        };

        GameObject targetTile = null;

        //Długość najkrótszej ścieżki do pola docelowego
        int shortestPathLength = int.MaxValue;

        //Lista przechowująca ścieżkę ruchu szarżującego
        List<Vector2> path = new List<Vector2>();

        foreach (Vector2 pos in positions)
        {
            GameObject tile = GameObject.Find($"Tile {pos.x - GridManager.Instance.transform.position.x} {pos.y - GridManager.Instance.transform.position.y}");

            //Jeżeli pole jest zajęte to szukamy innego
            if (tile == null || tile.GetComponent<Tile>().IsOccupied) continue;

            path = MovementManager.Instance.FindPath(attacker.transform.position, pos);

            if(path.Count == 0) continue;

            // Aktualizuje najkrótszą drogę
            if (path.Count < shortestPathLength)
            {
                shortestPathLength = path.Count;
                targetTile = tile;
            }  
        }

        if(shortestPathLength > attacker.GetComponent<Stats>().TempSz && !GameManager.IsAutoCombatMode)
        {
            return null;
        }
        else
        {
            return targetTile;
        }      
    }
    #endregion

    #region Grappling
    public void Grappling(Unit attacker, Unit target)
    {
        // JEŻELI ATAKUJĄCY JUŻ MA W CHWYCIE CEL TO DODAĆ TUTAJ OKNO WYBORU (JAK PRZY PAROWANIU I UNIKU) W KTÓRYM ATAKUJĄCY WYBIERA, CZY CHCE ZADAĆ OBRAZENIA W CHWYCIE, CZY POCHWYCIĆ PRZECIWNIKA MOCNIEJ.
        // DO WYBORU: ZAATAKUJ, POPRAW CHWYT, UWOLNIJ SIĘ

        //TYMCZASOWO PO PROSTU ATAK
        Attack(attacker, target);
    }
    #endregion

    #region Defensive stance
    public void DefensiveStance()
    {
        if (Unit.SelectedUnit == null) return;

        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();

        if (unit.DefensiveBonus == 0)
        {
            if (!unit.CanDoAction) return;

            //Wykonuje akcję
            RoundsManager.Instance.DoAction(unit);

            Debug.Log($"{unit.GetComponent<Stats>().Name} przyjmuje pozycja obronną.");

            unit.DefensiveBonus = 20;
        }
        else
        {
            unit.DefensiveBonus = 0;
        }

        UpdateDefensiveStanceButtonColor();
    }
    public void UpdateDefensiveStanceButtonColor()
    {
        if(Unit.SelectedUnit.GetComponent<Unit>().DefensiveBonus > 0)
        {
            _defensiveStanceButton.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Color.green;

            //Wyświetla ikonkę pozycji obronnej przy tokenie jednostki
            Unit.SelectedUnit.transform.Find("Canvas/Defensive_stance_image").gameObject.SetActive(true);
        }
        else
        {
            _defensiveStanceButton.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Color.white;

            //Ukrywa ikonkę pozycji obronnej przy tokenie jednostki
            Unit.SelectedUnit.transform.Find("Canvas/Defensive_stance_image").gameObject.SetActive(false);
        }
    }
    #endregion

    // Korutyna czekająca na wpisanie wyniku rzutu obronnego przez użytkownika
    private IEnumerator WaitForDefenceRollValue()
    {
        _isWaitingForRoll = true;
        _manualRollResult = 0;

        // Wyświetl panel do wpisania wyniku
        if (_applyDefenceRollResultPanel != null)
        {
            _applyDefenceRollResultPanel.SetActive(true);
        }

        // Wyzeruj pole tekstowe
        if (_defenceRollInputField != null)
        {
            _defenceRollInputField.text = "";
        }

        // Czekaj aż użytkownik wpisze wartość i kliknie Submit
        while (_isWaitingForRoll)
        {
            yield return null; // Czekaj na następną ramkę
        }

        // Ukryj panel po wpisaniu
        if (_applyDefenceRollResultPanel != null)
        {
            _applyDefenceRollResultPanel.SetActive(false);
        }
    }

    private IEnumerator WaitForRollValue()
    {
        _isWaitingForRoll = true;
        _manualRollResult = 0;

        // Wyświetl panel do wpisania wyniku
        if (_applyRollResultPanel != null)
        {
            _applyRollResultPanel.SetActive(true);
        }

        // Wyzeruj pole tekstowe
        if (_rollInputField != null)
        {
            _rollInputField.text = "";
        }

        // Czekaj aż użytkownik wpisze wartość i kliknie Submit
        while (_isWaitingForRoll)
        {
            yield return null; // Czekaj na następną ramkę
        }

        // Ukryj panel po wpisaniu
        if (_applyRollResultPanel != null)
        {
            _applyRollResultPanel.SetActive(false);
        }
    }

    public void OnSubmitRoll()
    {
        if (_rollInputField != null && int.TryParse(_rollInputField.text, out int result))
        {
            _manualRollResult = result;
            _isWaitingForRoll = false; // Przerywamy oczekiwanie
            _rollInputField.text = ""; // Czyścimy pole
        }

        if (_defenceRollInputField != null && int.TryParse(_defenceRollInputField.text, out int defenceResult))
        {
            _manualRollResult = defenceResult;
            _isWaitingForRoll = false; // Przerywamy oczekiwanie
            _defenceRollInputField.text = ""; // Czyścimy pole
        }
    }
   
    void ParryOrDodgeButtonClick(string parryOrDodge)
    {
        _parryOrDodge = parryOrDodge;
    }

    #region Reloading
    public void Reload()
    {
        StartCoroutine(ReloadCoroutine());
    }
    private IEnumerator ReloadCoroutine()
    {
        if(Unit.SelectedUnit == null) yield break;

        Weapon weapon = Unit.SelectedUnit.GetComponent<Inventory>().EquippedWeapons[0];
        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();

        if (weapon == null || !weapon.Type.Contains("ranged")) 
        {
            Debug.Log($"Wybrana broń nie wymaga ładowania.");
            yield break;
        }

        if(weapon.ReloadLeft > 0)
        {
            if (!Unit.SelectedUnit.GetComponent<Unit>().CanDoAction)
            {
                Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
                yield break;
            }

            //Wykonuje akcję
            RoundsManager.Instance.DoAction(Unit.SelectedUnit.GetComponent<Unit>());

            //Ustalenie testowanej umiejętności w zależności od kategorii broni
            RangedCategory rangedSkill = EnumConverter.ParseEnum<RangedCategory>(weapon.Category) ?? RangedCategory.Bow;

            int reloadRollResult = 0;
            // Jeżeli jesteśmy w trybie manualnych rzutów kośćmi
            if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(WaitForRollValue());
                reloadRollResult = _manualRollResult;
            }
            else
            {
                reloadRollResult = UnityEngine.Random.Range(1, 101);
            }

            //Test Broni Zasięgowej danej kategorii
            int successLevel = UnitsManager.Instance.TestSkill("US", stats, rangedSkill.ToString(), 0, reloadRollResult) / 10;
            if(successLevel > 0)
            {
                weapon.ReloadLeft = Mathf.Max(0, weapon.ReloadLeft - successLevel);

            }

            StartCoroutine(AnimationManager.Instance.PlayAnimation("reload", Unit.SelectedUnit));       
        }
        
        if(weapon.ReloadLeft == 0)
        {
            Debug.Log($"Broń {stats.Name} załadowana.");
        }
        else
        {
            Debug.Log($"{stats.Name} ładuje broń. Pozostał/y {weapon.ReloadLeft} PS do pełnego załadowania.");
        }  

        InventoryManager.Instance.DisplayReloadTime();    
    }

    private void ResetWeaponLoad(Weapon attackerWeapon, Stats attackerStats)
    {
        //Sprawia, że po ataku należy przeładować broń
        attackerWeapon.ReloadLeft = attackerWeapon.ReloadTime;
        attackerWeapon.WeaponsWithReloadLeft[attackerWeapon.Id] = attackerWeapon.ReloadLeft;

        //Uwzględnia zdolność Błyskawicznego Przeładowania
        if (attackerStats.RapidReload == true)
        {
            attackerWeapon.ReloadLeft--;   
        }

        //Uwzględnia zdolność Artylerzysta
        if (attackerStats.MasterGunner == true && attackerWeapon.Type.Contains("gunpowder"))
        {
            attackerWeapon.ReloadLeft--;
        }

        //Zapobiega ujemnej wartości czasu przeładowania
        if(attackerWeapon.ReloadLeft < 0)
        {
            attackerWeapon.ReloadLeft = 0;
        }

        InventoryManager.Instance.DisplayReloadTime();
    }
    #endregion

    #region Opportunity attack
    // Sprawdza czy ruch powoduje atak okazyjny
    public void CheckForOpportunityAttack(GameObject movingUnit, Vector2 selectedTilePosition)
    {
        //Przy bezpiecznym odwrocie nie występuje atak okazyjny
        if(Unit.SelectedUnit != null && Unit.SelectedUnit.GetComponent<Unit>().IsRetreating) return;

        List<Unit> adjacentOpponents = AdjacentOpponents(movingUnit.transform.position, movingUnit.tag);

        if(adjacentOpponents.Count == 0) return;

        // Atak okazyjny wywolywany dla kazdego wroga bedacego w zwarciu z bohaterem gracza
        foreach (Unit unit in adjacentOpponents)
        {
            Weapon weapon = InventoryManager.Instance.ChooseWeaponToAttack(unit.gameObject);

            //Jeżeli jest to jednostka unieruchomiona, nieprzytomna, w panice lub jednostka z bronią dystansową to ją pomijamy
            if (weapon.Type.Contains("ranged") || unit.Unconscious || unit.EntangledUnitId != 0 || unit.Entangled > 0 || unit.Broken > 0) continue;

            // Sprawdzenie czy ruch powoduje oddalenie się od przeciwników (czyli atak okazyjny)
            float distanceFromOpponentAfterMove = Vector2.Distance(selectedTilePosition, unit.transform.position);

            if (distanceFromOpponentAfterMove > 1.8f)
            {
                Debug.Log($"Ruch spowodował atak okazyjny od {unit.GetComponent<Stats>().Name}.");

                // Wywołanie ataku okazyjnego
                Attack(unit, movingUnit.GetComponent<Unit>(), true);             
            }
        }
    }

    // Funkcja pomocnicza do sprawdzania jednostek w sąsiedztwie danej pozycji
    public List<Unit> AdjacentOpponents(Vector2 center, string movingUnitTag)
    {
        Vector2[] positions = {
            center,
            center + Vector2.right,
            center + Vector2.left,
            center + Vector2.up,
            center + Vector2.down,
            center + new Vector2(1, 1),
            center + new Vector2(-1, -1),
            center + new Vector2(-1, 1),
            center + new Vector2(1, -1)
        };

        List<Unit> units = new List<Unit>();

        foreach (var pos in positions)
        {
            Collider2D collider = Physics2D.OverlapPoint(pos);
            if (collider == null || collider.GetComponent<Unit>() == null) continue;

            if (!collider.CompareTag(movingUnitTag))
            {
                units.Add(collider.GetComponent<Unit>());
            }
        }

        return units;
    }
    #endregion
}
