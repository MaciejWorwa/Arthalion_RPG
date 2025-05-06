using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

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
    [SerializeField] private UnityEngine.UI.Button _standardAttackButton;
    [SerializeField] private UnityEngine.UI.Button _chargeButton;
    [SerializeField] private UnityEngine.UI.Button _frenzyButton;
    [SerializeField] private UnityEngine.UI.Button _mountAttackButton;
    [SerializeField] private UnityEngine.UI.Button _grapplingButton;
    [SerializeField] private UnityEngine.UI.Button _feintButton;
    [SerializeField] private UnityEngine.UI.Button _stompButton;
    [SerializeField] private UnityEngine.UI.Button _disarmButton;
    public Dictionary<string, bool> AttackTypes = new Dictionary<string, bool>();

    [SerializeField] private UnityEngine.UI.Button _aimButton;
    [SerializeField] private UnityEngine.UI.Button _defensiveStanceButton;
    [SerializeField] private UnityEngine.UI.Button _reloadButton;

    [Header("Panel do manualnego zarządzania sposobem obrony")]
    [SerializeField] private GameObject _parryAndDodgePanel;
    [SerializeField] private UnityEngine.UI.Button _dodgeButton;
    [SerializeField] private UnityEngine.UI.Button _parryButton;
    private string _parryOrDodge;
    public int[] DefenceResults = new int[2]; // Wynik rzutu obronnego

    private string _grapplingActionChoice = "";    // Zmienna do przechowywania wyboru akcji przy grapplingu
    [SerializeField] private GameObject _grapplingActionPanel;
    [SerializeField] private UnityEngine.UI.Button _grapplingAttackButton;
    [SerializeField] private UnityEngine.UI.Button _improveGrappleButton;
    [SerializeField] private UnityEngine.UI.Button _escapeGrappleButton;

    public string HitLocation = null;    // Zmienna do przechowywania wyboru lokacji
    [SerializeField] private GameObject _selectHitLocationPanel;
    [SerializeField] private GameObject _riderOrMountPanel;
    private string _riderOrMount;
    [SerializeField] private UnityEngine.UI.Button _riderButton;
    [SerializeField] private UnityEngine.UI.Button _mountButton;

    [SerializeField] private GameObject _criticalDeflectionPanel;
    private string _criticalDeflection;
    [SerializeField] private UnityEngine.UI.Button _armorDamageButton;

    [SerializeField] private GameObject _distractingWeaponPanel;
    private string _distractChoice;
    [SerializeField] private UnityEngine.UI.Button _distractButton;

    private bool _isTrainedWeaponCategory; // Określa, czy atakujący jest wyszkolony w używaniu broni, którą atakuje

    public bool IsManualPlayerAttack;

    private Unit[] _groupOfTargets;
    private bool _groupOfTargetsPenalty;
    private int _groupTargetModifier;
    [SerializeField] private GameObject _groupOfTargetsPanel;
    private Unit _newTargetUnit; //Jeżeli strzał trafi w inną jednostkę z grupy, to zmieniany jest cel ataku.

    // Metoda inicjalizująca słownik ataków
    void Start()
    {
        InitializeAttackTypes();
        UpdateAttackTypeButtonsColor();

        _dodgeButton.onClick.AddListener(() => ParryOrDodgeButtonClick("dodge"));
        _parryButton.onClick.AddListener(() => ParryOrDodgeButtonClick("parry"));

        _armorDamageButton.onClick.AddListener(() => CriticalDeflectionButtonClick("damage_armor"));

        _distractButton.onClick.AddListener(() => DistractButtonClick("distract"));

        _riderButton.onClick.AddListener(() => RiderOrMountButtonClick("rider"));
        _mountButton.onClick.AddListener(() => RiderOrMountButtonClick("mount"));

        _grapplingAttackButton.onClick.AddListener(() => GrapplingActionButtonClick("attack"));
        _improveGrappleButton.onClick.AddListener(() => GrapplingActionButtonClick("improve"));
        _escapeGrappleButton.onClick.AddListener(() => GrapplingActionButtonClick("escape"));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && _parryAndDodgePanel.activeSelf)
        {
            ParryOrDodgeButtonClick("");
        }

        if (Input.GetKeyDown(KeyCode.Escape) && _riderOrMountPanel.activeSelf)
        {
            RiderOrMountButtonClick("");
        }

        if (Input.GetKeyDown(KeyCode.Escape) && _grapplingActionPanel.activeSelf)
        {
            GrapplingActionButtonClick("");
        }

        if (Input.GetKeyDown(KeyCode.Escape) && _criticalDeflectionPanel.activeSelf)
        {
            CriticalDeflectionButtonClick("");
        }
    }

    #region Attack types
    private void InitializeAttackTypes()
    {
        // Dodajemy typy ataków do słownika
        AttackTypes.Add("StandardAttack", true);
        AttackTypes.Add("Charge", false);
        //AttackTypes.Add("Frenzy", false);  // Szał bojowy
        AttackTypes.Add("MountAttack", false);  // Atak wierzchowca
        AttackTypes.Add("Grappling", false);  // Zapasy
        AttackTypes.Add("Feint", false);  // Finta
        AttackTypes.Add("Stomp", false);  // Tupnięcie
        AttackTypes.Add("Disarm", false);  // Rozbrajanie
    }

    // Metoda ustawiająca dany typ ataku
    public void ChangeAttackType(string attackTypeName = null)
    {
        if (Unit.SelectedUnit == null) return;

        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();
        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();

        if (attackTypeName == null && unit.Entangled == 0 && unit.EntangledUnitId == 0)
        {
            attackTypeName = "StandardAttack";
        }
        else if (attackTypeName == null)
        {
            attackTypeName = "Grappling";
        }

        //Resetuje szarżę lub bieg, jeśli były aktywne
        if (attackTypeName != "Charge" && unit.IsCharging)
        {
            StartCoroutine(MovementManager.Instance.UpdateMovementRange(1));
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
            //        MovementManager.Instance.StartCoroutine(UpdateMovementRange(1);
            //    }
            //}

            //Tupnięcie jest dostępne tylko dla dużych jednostek
            if (AttackTypes["Stomp"] == true && stats.Size < SizeCategory.Large)
            {
                AttackTypes[attackTypeName] = false;
                AttackTypes["StandardAttack"] = true;
                Debug.Log("Tupnięcie mogą wykonywać tylko odpowiednio duże jednostki.");
            }

            //Rozbrajanie jest dostępne tylko dla jednostek ze zdolnością rozbrajania
            if (AttackTypes["Disarm"] == true && stats.Disarm == 0)
            {
                AttackTypes[attackTypeName] = false;
                AttackTypes["StandardAttack"] = true;
                Debug.Log("Rozbrajanie mogą wykonywać tylko jednostki posiadające ten talent.");
            }

            //Finta jest dostępna tylko dla jednostek ze zdolnością finty
            if (AttackTypes["Feint"] == true && stats.Feint == 0)
            {
                AttackTypes[attackTypeName] = false;
                AttackTypes["StandardAttack"] = true;
                Debug.Log("Fintę mogą wykonywać tylko jednostki posiadające ten talent.");
            }

            //Ograniczenie finty, ogłuszania i rozbrajania do ataków w zwarciu
            if ((AttackTypes["Feint"] || AttackTypes["Disarm"] || AttackTypes["Charge"]) == true && unit.GetComponent<Inventory>().EquippedWeapons[0] != null && unit.GetComponent<Inventory>().EquippedWeapons[0].Type.Contains("ranged"))
            {
                AttackTypes[attackTypeName] = false;
                AttackTypes["StandardAttack"] = true;
                Debug.Log("Jednostka walcząca bronią dystansową nie może wykonać tej akcji.");
            }

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

                if (!unit.CanDoAction || !unit.CanMove)
                {
                    Debug.Log("Ta jednostka nie może wykonać szarży w obecnej rundzie.");
                    return;
                }

                StartCoroutine(MovementManager.Instance.UpdateMovementRange(2, null, true));
                MovementManager.Instance.Retreat(false); // Zresetowanie bezpiecznego odwrotu
            }
        }

        UpdateAttackTypeButtonsColor();
    }

    public void UpdateAttackTypeButtonsColor()
    {
        _standardAttackButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["StandardAttack"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _chargeButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["Charge"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        //_frenzyButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["Frenzy"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _mountAttackButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["MountAttack"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _grapplingButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["Grappling"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _feintButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["Feint"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _stompButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["Stomp"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        _disarmButton.GetComponent<UnityEngine.UI.Image>().color = AttackTypes["Disarm"] ? new UnityEngine.Color(0.15f, 1f, 0.45f) : UnityEngine.Color.white;
        
        SetActionsButtonsInteractable();
    }

    public void SetActionsButtonsInteractable()
    {
        if (Unit.SelectedUnit == null) return;
        _disarmButton.interactable = Unit.SelectedUnit.GetComponent<Stats>().Disarm > 0;
        _feintButton.interactable = Unit.SelectedUnit.GetComponent<Stats>().Feint > 0;
        _frenzyButton.interactable = Unit.SelectedUnit.GetComponent<Stats>().Frenzy;
        _reloadButton.interactable = Unit.SelectedUnit.GetComponent<Inventory>().EquippedWeapons.Any(weapon => weapon != null && weapon.ReloadLeft > 0);
        _grapplingButton.gameObject.SetActive(!Unit.SelectedUnit.GetComponent<Unit>().IsMounted);
        _mountAttackButton.gameObject.SetActive(Unit.SelectedUnit.GetComponent<Unit>().IsMounted);
        _stompButton.interactable = Unit.SelectedUnit.GetComponent<Stats>().Size > SizeCategory.Average;
    }
    #endregion

    #region Attack functions
    public void Attack(Unit attacker, Unit target, bool opportunityAttack = false)
    {
        // Sprawdź, czy gra jest wstrzymana
        if (GameManager.IsGamePaused)
        {
            Debug.Log("Gra została wstrzymana. Aby ją wznowić musisz wyłączyć okno znajdujące się na polu gry.");
            return;
        }

        if (AttackTypes["MountAttack"]) // Atak wierzchowca
        {
            attacker = attacker.Mount;
        }

        bool furiousAssault = (attacker.GetComponent<Stats>().FuriousAssault > 0 && target.LastAttackerStats == attacker.GetComponent<Stats>() && attacker.CanMove && !attacker.CanDoAction);
        bool frenzyAttack = attacker.GetComponent<Stats>().FrenzyAttacksLeft > 0;

        // Sprawdź, czy jednostka może wykonać atak
        if (!attacker.CanDoAction && !opportunityAttack && !furiousAssault && !frenzyAttack && !AttackTypes["Stomp"])
        {
            Debug.Log("Wybrana jednostka nie może wykonać ataku w tej rundzie.");
            return;
        }

        if(AttackTypes["Stomp"])
        {
            if (attacker.GetComponent<Stats>().Size <= target.GetComponent<Stats>().Size)
            {
                Debug.Log("Tupnięcie można wykonać tylko na przeciwniku mniejszym od siebie.");
                return;
            }

            int advantage = attacker.tag == "PlayerUnit" ? InitiativeQueueManager.Instance.PlayersAdvantage : InitiativeQueueManager.Instance.EnemiesAdvantage;
            if (advantage == 0)
            {
                Debug.Log("Wybrana jednostka nie może wykonać tupnięcia w tej rundzie. Za mało punktów przewagi.");
                return;
            }
            else
            {
                //Zaktualizowanie przewagi
                InitiativeQueueManager.Instance.CalculateAdvantage(attacker.tag, -1);
            }
        }

        if (opportunityAttack) ChangeAttackType();

        StartCoroutine(AttackCoroutine(attacker, target, opportunityAttack, furiousAssault));
    }
    private IEnumerator AttackCoroutine(Unit attacker, Unit target, bool opportunityAttack = false, bool furiousAssault = false, bool reactionStrike = false, int secondAttackRollResult = 0)
    {
        // Czekaj aż użytkownik wybierze lokację trafienia (jeśli panel wyboru lokacji jest otwarty)
        while (_selectHitLocationPanel.activeSelf)
        {
            yield return null; // Czekaj na następną ramkę
        }

        // 1) Oblicz dystans między walczącymi
        float attackDistance = CalculateDistance(attacker.gameObject, target.gameObject);

        // 2) Pobierz statystyki i broń
        Stats attackerStats = attacker.Stats;
        Stats targetStats = target.Stats;

        Weapon attackerWeapon = null;
        Weapon targetWeapon = null;
        if (AttackTypes["Grappling"] || AttackTypes["Stomp"]) // Zapasy lub tupnięcie
        {
            attackerStats.GetComponent<Weapon>().ResetWeapon();
            attackerWeapon = attackerStats.GetComponent<Weapon>();

            if(AttackTypes["Grappling"])
            {
                targetStats.GetComponent<Weapon>().ResetWeapon();
                targetWeapon = targetStats.GetComponent<Weapon>();

                if (attacker.Entangled > 0 && target.EntangledUnitId != attacker.UnitId)
                {
                    Debug.Log("Celem ataku musi być jednostka, z którą toczą się zapasy.");
                    yield break;
                }
            }
            else targetWeapon = InventoryManager.Instance.ChooseWeaponToAttack(target.gameObject);
        }
        else // Zwykły atak bronią
        {
            attackerWeapon = InventoryManager.Instance.ChooseWeaponToAttack(attacker.gameObject);
            targetWeapon = InventoryManager.Instance.ChooseWeaponToAttack(target.gameObject);

            // Uwzględniamy typ amunicji
            if (attackerWeapon.Type.Contains("ranged") && attackerWeapon.Category != "entangling" && attackerWeapon.Category != "throwing")
            {
                if (string.IsNullOrEmpty(attackerWeapon.AmmoType) || attackerWeapon.AmmoType == "Brak")
                {
                    Debug.Log("Do ataku bronią dystansową niezbędne jest wybranie typu amunicji. Możesz to zrobić w panelu ekwipunku.");
                    yield break;
                }
                else
                {
                    InventoryManager.Instance.ApplyAmmoModifiers(attackerWeapon); // Aktualizujemy broń o dodatkowe cechy amunicji
                }
            }
        }

        bool isMeleeAttack = attackerWeapon.Type.Contains("melee") || (attackerWeapon.Pistol && attackDistance <= 1.5f);
        bool isRangedAttack = !isMeleeAttack && attackerWeapon.Type.Contains("ranged");
        bool hasTwoWeapons = attacker.GetComponent<Inventory>().EquippedWeapons.All(weapon => weapon != null && weapon.Shield == 0 && !weapon.TwoHanded);

        //Wściekły Atak dotyczy tylko ataków w zwarciu
        if (furiousAssault && isRangedAttack) furiousAssault = false;

        // Ustalamy umiejętności, które będą testowane w zależności od kategorii broni
        MeleeCategory meleeSkill = EnumConverter.ParseEnum<MeleeCategory>(attackerWeapon.Category) ?? MeleeCategory.Basic;
        RangedCategory rangedSkill = EnumConverter.ParseEnum<RangedCategory>(attackerWeapon.Category) ?? RangedCategory.Bow;
        int skillModifier = isRangedAttack ? attackerStats.GetSkillModifier(attackerStats.Ranged, rangedSkill) : attackerStats.GetSkillModifier(attackerStats.Melee, meleeSkill);
        _isTrainedWeaponCategory = meleeSkill == MeleeCategory.Basic || skillModifier > 0;

        if (isRangedAttack && !_isTrainedWeaponCategory && attackerWeapon.Category != "crossbow" && attackerWeapon.Category != "throwing")
        {
            Debug.Log("Wybrana jednostka nie może walczyć przy użyciu broni z tej kategorii.");
            yield break;
        }

        if(isRangedAttack && attacker.IsFrenzy)
        {
            Debug.Log("W trakcie szału bojowego można walczyć jedynie w zwarciu.");
            yield break;
        }

        // Finta
        if (AttackTypes["Feint"])
        {
            if (attackerWeapon.Category != "fencing")
            {
                Debug.Log("Fintę można wykonywać tylko przy użyciu broni szermierczej.");
                yield break;
            }

            StartCoroutine(Feint(attackerStats, targetStats, targetWeapon));
            yield break;
        }

        // Uwzględnienie cechy Eteryczny
        if (targetStats.Ethereal && attackerWeapon.Quality != "Magiczna" && attackerStats.Daemonic == 0)
        {
            Debug.Log($"{targetStats.Name} jest eteryczny/a i może zostać zraniony/a tylko magiczną bronią lub zaklęciem.");
            yield break;
        }

        // 3) Sprawdź zasięg i ewentualnie wykonaj szarżę
        float effectiveAttackRange = attackerWeapon.AttackRange;

        if (attackerWeapon.Type.Contains("throwing")) // Oblicz właściwy zasięg ataku, uwzględniając broń miotaną
        {
            effectiveAttackRange *= attackerStats.S / 10;
        }

        bool isOutOfRange = attackDistance > effectiveAttackRange;
        bool isRangedAndTooFar = isRangedAttack && (attackDistance > effectiveAttackRange * 3 || (attackDistance > effectiveAttackRange && attackerWeapon.Category == "entangling"));

        if (isOutOfRange && (!isRangedAttack || isRangedAndTooFar))
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

        // 4) Sprawdzenie dodatkowych warunków dla ataku dystansowego (np. przeszkody, czy broń jest naładowana, itp.)
        if (isRangedAttack)
        {
            bool validRanged = ValidateRangedAttack(attacker, target, attackerWeapon, attackDistance);
            if (!validRanged) yield break;

            // ==================================================================
            //DODAĆ MODYFIKATORY ZA PRZESZKODY NA LINII STRZAŁU
            // ==================================================================
        }

        // 5) Określamy, czy atak jest manualny czy automatyczny
        IsManualPlayerAttack = attacker.CompareTag("PlayerUnit") && GameManager.IsAutoDiceRollingMode == false;

        // 6) Jeśli to nie atak okazyjny ani wściekły atak – zużywamy akcję
        if (furiousAssault) // Wykorzystanie talentu Wściekły Atak (działa tylko jeśli jednostka, którą atakujemy została wcześniej przez nas zraniona, czyli poprzedni atak się udał)
        {
            attacker.CanMove = false;
            MovementManager.Instance.SetCanMoveToggle(false);
        }
        else if (!opportunityAttack && !reactionStrike && secondAttackRollResult == 0 && !AttackTypes["Stomp"])
        {
            if (attacker.CanDoAction)
            {
                RoundsManager.Instance.DoAction(attacker);
            }
            else if(!attacker.IsFrenzy || attackerStats.FrenzyAttacksLeft == 0)
            {
                Debug.Log("Wybrana jednostka nie może wykonać kolejnego ataku w tej rundzie.");
                yield break;
            }
        }

        // Rozbrojenie
        if (AttackTypes["Disarm"])
        {
            StartCoroutine(Disarm(attackerStats, targetStats, attackerWeapon, targetWeapon));
            yield break;
        }

        // ==================================================================
        // 7) *** RZUT ATAKU *** (manualny lub automatyczny)
        // ==================================================================
        
        // Sprawdzenie, czy strzelamy do pojedynczego wroga, czy do grupy
        if (isRangedAttack)
        {
            _groupOfTargets = GetAdjacentUnits(targetStats.transform.position);

            if (_groupOfTargets.Length > 1)
            {
                _groupOfTargetsPanel.SetActive(true);

                // Najpierw czekamy, aż gracz kliknie którykolwiek przycisk
                yield return new WaitUntil(() => !_groupOfTargetsPanel.activeSelf);
            }
        }

        int attackModifier = CalculateAttackModifier(attacker, attackerWeapon, target, attackDistance, furiousAssault);

        // Jeżeli w wyniku ataku dystansowego w grupę została trafiona przypadkowo inna jednostka, aktualizujemy cel
        if(_newTargetUnit != null)
        {
            target = _newTargetUnit;
            targetStats = target.Stats;
            targetWeapon = InventoryManager.Instance.ChooseWeaponToAttack(target.gameObject);
            _newTargetUnit = null;
        }

        // Modyfikator do trafienia, za wybór konkretnej lokacji
        if (HitLocation != null && HitLocation.Length > 0 && !((attackerWeapon.Pummel || attackerWeapon.Id == 4) && attackerStats.StrikeToStun > 0))
        {
            attackModifier -= 20;
            Debug.Log("Modyfikator -20 do trafienia za wybór konkretnej lokalizacji");
        }

        //Zwiększenie modyfikatora do ataku za atak okazyjny
        if (opportunityAttack) attackModifier += 20;
        if (attackModifier > 60) attackModifier = 60; // Górny limit modyfikatora
        if (attackModifier < -30) attackModifier = -30; // Dolny limit modyfikatora

        //Zresetowanie celowania, jeżeli było aktywne
        if (attacker.AimingBonus != 0)
        {
            attacker.AimingBonus = 0;
            UpdateAimButtonColor();
        }

        // Zresetowanie finty
        attacker.FeintedUnitId = 0;
        attacker.FeintModifier = 0;

        int rollOnAttack = secondAttackRollResult != 0 ? secondAttackRollResult : 0;
        if (rollOnAttack == 0)
        {
            if (IsManualPlayerAttack)
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(attackerStats, "trafienie", result => rollOnAttack = result));
                if (rollOnAttack == 0) yield break;
            }
            else
            {
                // Automatyczny rzut
                rollOnAttack = UnityEngine.Random.Range(1, 101);
            }
        }

        // 8) Liczymy poziomy sukcesu atakującego
        int skillValue;

        if (AttackTypes["Grappling"])
        {
            skillValue = attackerStats.S;
        }
        else if (isRangedAttack)
        {
            skillValue = attackerStats.US + attackerStats.GetSkillModifier(attackerStats.Ranged, rangedSkill);
        }
        else
        {
            skillValue = attackerStats.WW + attackerStats.GetSkillModifier(attackerStats.Melee, meleeSkill);
        }

        int[] results = CalculateSuccessLevel(attackerWeapon, rollOnAttack, skillValue, true, attackModifier);

        // Talent "Cios poniżej pasa"
        if (results[0] >= 0 && attackerWeapon.Category == "brawling" && attackerStats.DirtyFighting > 0)
        {
            results[1] += attackerStats.DirtyFighting;
            attackModifier += attackerStats.DirtyFighting * 10;
            Debug.Log("Uwzględniono modyfikator za talent Cios poniżej pasa. Łączny modyfikator: " + attackModifier);
        }

        // Talent "Dwie bronie"
        if (results[0] >= 0 && attackerStats.DualWielder > 0 && attacker.GetComponent<Inventory>().EquippedWeapons.All(weapon => weapon != null && weapon.Shield == 0 && !weapon.TwoHanded))
        {
            results[1] += attackerStats.DualWielder;
            attackModifier += attackerStats.DualWielder * 10;
            Debug.Log("Uwzględniono modyfikator za talent Dwie bronie. Łączny modyfikator: " + attackModifier);
        }

        int attackerSuccessValue = results[0];
        int attackerSuccessLevel = results[1];

        string successLevelColor = attackerSuccessValue >= 0 ? "green" : "red";
        string modifierString = attackModifier != 0 ? $" Modyfikator: {attackModifier}," : "";

        if (AttackTypes["Grappling"] == true && attacker.EntangledUnitId != target.UnitId)
        {
            Debug.Log($"{attackerStats.Name} próbuje pochwycić przeciwnika. Wynik rzutu: {rollOnAttack}, Wartość umiejętności: {skillValue},{modifierString} PS: <color={successLevelColor}>{attackerSuccessLevel}</color>");
        }
        else
        {
            Debug.Log($"{attackerStats.Name} atakuje przy użyciu {attackerWeapon.Name}. Wynik rzutu: {rollOnAttack}, Wartość umiejętności: {skillValue},{modifierString} PS: <color={successLevelColor}>{attackerSuccessLevel}</color>");
        }

        //Ustalamy miejsce trafienia
        string hitLocation = !String.IsNullOrEmpty(HitLocation) ? HitLocation : (DiceRollManager.Instance.IsDoubleDigit(rollOnAttack) ? DetermineHitLocation() : DetermineHitLocation(rollOnAttack));

        if (!String.IsNullOrEmpty(HitLocation))
        {
            Debug.Log($"Atak jest skierowany w {TranslateHitLocation(hitLocation)}.");
            HitLocation = null;
        }

        // Obsługa fuksa / pecha
        bool isFortunateOrUnfortunateEvent = false; // Zmienna używana do tego, aby nie powielać dwa razy szczęścia lub pecha w przypadku specyficznych broni

        if (DiceRollManager.Instance.IsDoubleDigit(rollOnAttack) || (attackerWeapon.Impale && rollOnAttack % 10 == 0 && _isTrainedWeaponCategory) || (attackerWeapon.Dangerous && attackerSuccessValue < 0 && (rollOnAttack % 10 == 9 || rollOnAttack / 10 == 9)))
        {
            isFortunateOrUnfortunateEvent = true;

            if (attackerSuccessValue >= 0)
            {
                Debug.Log($"{attackerStats.Name} wyrzucił/a <color=green>FUKSA</color> na trafienie!");

                StartCoroutine(CriticalWoundRoll(attackerStats, targetStats, hitLocation, attackerWeapon, rollOnAttack));
                attackerStats.FortunateEvents++;
            }
            else if (DiceRollManager.Instance.IsDoubleDigit(rollOnAttack) || (attackerWeapon.Dangerous && (rollOnAttack % 10 == 9 || rollOnAttack / 10 == 9)))
            {
                Debug.Log($"{attackerStats.Name} wyrzucił/a <color=red>PECHA</color> na trafienie!");
                attackerStats.UnfortunateEvents++;

                //Uwzględnienie cechy broni "Tandetny"
                if (attackerWeapon.Shoddy)
                {
                    attackerWeapon.Damage = attackerWeapon.S;
                    Debug.Log($"<color=red>{attackerWeapon.Name} ulega zniszczeniu ze względu na cechę \"Tandetny\".</color>");
                }
            }
        }

        // Obsługa FUKSA i PECHA
        bool isDoubleRoll = DiceRollManager.Instance.IsDoubleDigit(rollOnAttack);
        bool isImpaleRoll = attackerWeapon.Impale && rollOnAttack % 10 == 0 && _isTrainedWeaponCategory;
        bool isDangerousRoll = attackerWeapon.Dangerous && (rollOnAttack % 10 == 9 || rollOnAttack / 10 == 9) && attackerSuccessValue < 0;
        bool isSpecialRoll = isDoubleRoll || isImpaleRoll || isDangerousRoll; // Sprawdzamy, czy mamy do czynienia z którymś z „wyjątkowych” rzutów

        if (isSpecialRoll && !isFortunateOrUnfortunateEvent)
        {
            if (attackerSuccessValue >= 0)
            {
                Debug.Log($"{attackerStats.Name} wyrzucił/a <color=green>FUKSA</color> na trafienie!");
                StartCoroutine(CriticalWoundRoll(attackerStats, targetStats, hitLocation, attackerWeapon, rollOnAttack));
                attackerStats.FortunateEvents++;
            }
            else if (isDoubleRoll || isDangerousRoll) // Tu sprawdzamy tylko double albo „niebezpieczny” rzut na 9, bo impale przy nieudanym rzucie nie wywołuje żadnego efektu.
            {
                Debug.Log($"{attackerStats.Name} wyrzucił/a <color=red>PECHA</color> na trafienie!");
                attackerStats.UnfortunateEvents++;

                //Uwzględnienie cechy broni "Tandetny"
                if (attackerWeapon.Shoddy)
                {
                    attackerWeapon.Damage = attackerWeapon.S;
                    Debug.Log($"<color=red>{attackerWeapon.Name} ulega zniszczeniu ze względu na cechę \"Tandetny\".</color>");
                }
            }
        }

        // Jeśli to była broń dystansowa – resetujemy ładowanie
        if (isRangedAttack)
        {
            ResetWeaponLoad(attackerWeapon, attackerStats);
        }

        if(attacker.IsFrenzy)
        {
            attackerStats.FrenzyAttacksLeft--;
            if (attackerStats.FrenzyAttacksLeft < 0) attackerStats.FrenzyAttacksLeft = 0;
        }

        // ==================================================================
        // 9) *** OBRONA *** (tylko jeśli to atak w zwarciu lub mamy tarcze i możemy bronić się przed strzałem)
        // ==================================================================
        DefenceResults = new int[2];
        int defenceSuccessValue = 0;
        int defenceSuccessLevel = 0;
        int defenceRollResult = 0;
        int parryValue = 0;
        int dodgeValue = 0;
        bool canParry = false;
        bool canDodge = false;

        if (AttackTypes["Grappling"]) // Zapasy
        {
            // Jeżeli jesteśmy w trybie manualnych rzutów kośćmi
            if (!GameManager.IsAutoDiceRollingMode && target.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, "siłę", result => defenceRollResult = result));
                if (defenceRollResult == 0) yield break;
            }
            else
            {
                defenceRollResult = UnityEngine.Random.Range(1, 101);
            }

            int modifier = target.Entangled > 0 ? 10 : 0;
            int[] defenceTest = DiceRollManager.Instance.TestSkill("S", targetStats, null, modifier, defenceRollResult);
            defenceSuccessValue = defenceTest[0];
            defenceSuccessLevel = defenceTest[1];
        }
        else if(!opportunityAttack) // Zwykły atak bronią (w przypadku ataków okazyjnych nie można się bronić)
        {
            // Sprawdzenie, czy jednostka może próbować parować lub unikać ataku
            Inventory inventory = target.GetComponent<Inventory>();
            bool hasMeleeWeapon = inventory.EquippedWeapons.Any(weapon => weapon != null && weapon.Type.Contains("melee"));
            bool hasShield = inventory.EquippedWeapons.Any(weapon => weapon != null && weapon.Shield >= 2);
            bool bothUnarmed = attackerWeapon.Id == 0 && targetWeapon.Id == 0;

            canParry = !targetStats.Bestial && ((isMeleeAttack && (hasMeleeWeapon || bothUnarmed)) ||  (!isMeleeAttack && hasShield));
            canDodge = isMeleeAttack;

            if ((canParry || canDodge) && !target.Surprised)
            {
                Weapon weaponUsedForParry = GetBestParryWeapon(targetStats, targetWeapon);
                int parryModifier = CalculateParryModifier(target, targetStats, attackerStats, weaponUsedForParry, attackerWeapon);
                int dodgeModifier = CalculateDodgeModifier(target, targetStats, attackerWeapon);

                //Modyfikator za strach
                if (target.FearedUnits.Contains(attacker))
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
                yield return StartCoroutine(Defense(target, targetStats, attackerStats, attackerWeapon, weaponUsedForParry, targetMeleeSkill, parryValue, dodgeValue, parryModifier, dodgeModifier, canParry, canDodge));

                defenceSuccessValue = DefenceResults[0];
                defenceSuccessLevel = DefenceResults[1];
            }
        }

        //W przypadku manualnego ataku sprawdzamy, czy postać powinna zakończyć turę
        if (IsManualPlayerAttack && !attacker.CanMove && !attacker.CanDoAction && (!attacker.IsFrenzy || attackerStats.FrenzyAttacksLeft == 0))
        {
            RoundsManager.Instance.FinishTurn();
        }

        // 10) Teraz dopiero wiemy, ile wynoszą poziomy sukcesu atakującego i obrońcy
        // Następuje finalne rozstrzygnięcie
        int combinedSuccessLevel = attackerSuccessLevel - defenceSuccessLevel;

        // Sprawdzenie warunku trafienia
        bool attackSucceeded = (isRangedAttack && ((!canParry && attackerSuccessValue > 0) || combinedSuccessLevel > 0 || (canParry && combinedSuccessLevel == 0 && skillValue > parryValue))) || (isMeleeAttack && (combinedSuccessLevel > 0 || (combinedSuccessLevel == 0 && skillValue > Math.Max(parryValue, dodgeValue))));

        // Jeśli normalnie test trafienia zakończyłby się niepowodzeniem, ale dzięki premii za strzelanie do grupy udało się trafić, to przyjmujemy, że poziom sukcesu wynosi 0 (czyli trafienie minimalne).
        if (isRangedAttack && _groupTargetModifier != 0)
        {
            if ((!canParry && attackerSuccessValue - _groupTargetModifier < 0) || (canParry && combinedSuccessLevel - _groupTargetModifier / 10 < 0))
            {
                combinedSuccessLevel = 0;
                Debug.Log($"Trafienie powiodło się dzięki modyfikatorowi za celowanie w grupę — poziom sukcesu zostaje ustawiony na 0.");
            }    
        }
        _groupTargetModifier = 0;

        //Resetujemy czas przeładowania broni celu ataku, bo ładowanie zostało zakłócone przez atak, przed którym musi się bronić. W przypadku chybienia z broni dystansowej, nie przeszkadza to w ładowaniu
        if (targetWeapon.ReloadLeft != 0 && (attackSucceeded || isMeleeAttack))
        {
            ResetWeaponLoad(targetWeapon, targetStats);
        }

        //Resetujemy stan zaskoczenia, jeśli był aktywny
        target.Surprised = false;

        // Uwzględnienie cechy broni prochowej (wywołanie paniki). Uwzględnia również talent Niewzruszony
        if (attackerWeapon.Blackpowder && !(targetStats.Unshakable > 0 && !attackSucceeded))
        {
            int rollResult = 0;
            // Jeżeli jesteśmy w trybie manualnych rzutów kośćmi
            if (!GameManager.IsAutoDiceRollingMode && target.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, "opanowanie", result => rollResult = result));
                if (rollResult == 0) yield break;
            }
            else
            {
                rollResult = UnityEngine.Random.Range(1, 101);
            }

            int[] rollResults = DiceRollManager.Instance.TestSkill("SW", targetStats, "Cool", 20, rollResult);
            if (rollResults[0] < 0)
            {
                target.Broken++;
                Debug.Log($"<color=#FF7F50>Poziom paniki {targetStats.Name} wzrasta o 1.</color>");
            }
            else rollResults[1] += targetStats.Unshakable;
        }

        // Atak chybił, przerywamy funkcję lub gdy atakowanym jest Czempion, stosujemy kontratak
        if (attackSucceeded == false)
        {
            Debug.Log($"Atak {attackerStats.Name} chybił.");
            StartCoroutine(AnimationManager.Instance.PlayAnimation("miss", null, target.gameObject));
            ChangeAttackType(); // Resetuje typ ataku

            //Uwzględnienie talentu Czempion i Riposta
            if ((targetStats.Champion || targetStats.RiposteAttacksLeft > 0) && isMeleeAttack && (combinedSuccessLevel < 0 || (combinedSuccessLevel == 0 && skillValue < Math.Max(parryValue, dodgeValue))))
            {
                if (targetStats.RiposteAttacksLeft > 0) targetStats.RiposteAttacksLeft--;

                //Uwzględnienie cechy broni "Dekoncentrująca"
                if (attackerWeapon.Distract && defenceSuccessLevel - attackerSuccessLevel > 0 && _isTrainedWeaponCategory)
                {
                    _distractingWeaponPanel.SetActive(true);
                    yield return new WaitUntil(() => !_distractingWeaponPanel.activeSelf);

                    if (_distractChoice == "distract")
                    {
                        Debug.Log($"Możesz przesunąć {attackerStats.Name} o {defenceSuccessLevel - attackerSuccessLevel} metry/ów od {targetStats.Name}. Jedno pole na siatce odpowiada dwóm metrom.");
                        yield break;
                    }
                }

                int riposteDamage = CalculateDamage(defenceRollResult, defenceSuccessLevel - attackerSuccessLevel, targetStats, attackerStats, targetWeapon);
                string talentName = targetStats.Champion ? "Czempion" : "Riposta";
                Debug.Log($"{targetStats.Name} korzystając z talentu {talentName} kontratakuje i zadaje {riposteDamage} obrażeń.");

                //Ustalamy miejsce trafienia
                hitLocation = DiceRollManager.Instance.IsDoubleDigit(defenceRollResult) ? DetermineHitLocation() : DetermineHitLocation(defenceRollResult);
                int attackerArmor = CalculateArmor(targetStats, attackerStats, hitLocation, defenceRollResult, targetWeapon);

                ApplyDamageToTarget(riposteDamage, attackerArmor, targetStats, attackerStats, attacker, targetWeapon);

                // Animacja ataku
                StartCoroutine(AnimationManager.Instance.PlayAnimation("attack", target.gameObject, attacker.gameObject));

                if (attackerStats.TempHealth < 0)
                {
                    if (GameManager.IsAutoKillMode)
                    {
                        HandleDeath(attackerStats, attacker.gameObject, targetStats);
                    }
                    else
                    {
                        if (attackerStats.Daemonic > 0)
                        {
                            Debug.Log($"<color=red>{attackerStats.Name} zostaje odesłany do domeny Chaosu.</color>");
                        }
                        else
                        {
                            StartCoroutine(CriticalWoundRoll(targetStats, attackerStats, hitLocation, attackerWeapon, defenceRollResult));
                        }
                    }      
                }
            }

            yield break;
        }

        // Zaktualizowanie przewagi, skoro atak się powiódł
        InitiativeQueueManager.Instance.CalculateAdvantage(attacker.tag, 1);

        // Sprawdzenie, czy atak ogłuszy przeciwnika
        if (hitLocation == "head" && (attackerWeapon.Pummel || (attackerWeapon.Id == 4 && attackerStats.StrikeToStun > 0)))
        {
            StartCoroutine(Stun(attackerStats, targetStats));
        }

        // W przypadku ataku okazyjnego, cel jest zmuszony do testu opanowania
        if (opportunityAttack)
        {
            opportunityAttack = false;

            int rollResult = 0;
            if (!GameManager.IsAutoDiceRollingMode && targetStats.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, "opanowanie", result => rollResult = result));
                if (rollResult == 0) yield break;
            }

            int[] rollResults = DiceRollManager.Instance.TestSkill("SW", targetStats, "Cool", 0, rollResult);
            if (rollResults[0] < 0)
            {
                target.Broken += Math.Abs(rollResults[1]) + 1;
                Debug.Log($"<color=#FF7F50>Poziom paniki {targetStats.Name} wzrasta o {Math.Abs(rollResults[1]) + 1}</color>");
            }
        }

        // Udana próba pochwycenia przeciwnika
        if ((AttackTypes["Grappling"] == true || attackerWeapon.Entangle) && attacker.EntangledUnitId != target.UnitId)
        {
            attacker.EntangledUnitId = target.UnitId;
            target.Entangled++;
            target.CanMove = false;
            attacker.CanMove = false;
            MovementManager.Instance.SetCanMoveToggle(false);
            RoundsManager.Instance.FinishTurn();

            if(AttackTypes["Grappling"] == true)
            {
                Debug.Log($"{attackerStats.Name} pochwycił/a {targetStats.Name}.");
                yield break;
            }
            else if (attackerWeapon.Entangle)
            {
                Debug.Log($"{attackerStats.Name} pochwycił/a {targetStats.Name} przy użyciu {attackerWeapon.Name}.");
            }
        }

        //Uwzględnienie cechy broni "Dekoncentrująca"
        if (attackerWeapon.Distract && combinedSuccessLevel > 0 && _isTrainedWeaponCategory)
        {
            _distractingWeaponPanel.SetActive(true);
            yield return new WaitUntil(() => !_distractingWeaponPanel.activeSelf);

            if (_distractChoice == "distract")
            {
                Debug.Log($"Możesz przesunąć {targetStats.Name} o <color=green>{combinedSuccessLevel} m</color> od {attackerStats.Name}. Jedno pole na siatce odpowiada dwóm metrom.");
                yield break;
            }
        }

        if (attackerWeapon.Type.Contains("no-damage")) yield break; //Jeśli broń nie powoduje obrażeń, np. arkan, to pomijamy dalszą część kodu

        // 11) Jeśli atakujący wygrywa, zadaj obrażenia
        // Oblicz pancerz i finalne obrażenia
        int armor = CalculateArmor(attackerStats, targetStats, hitLocation, rollOnAttack, attackerWeapon);
        int damage = CalculateDamage(rollOnAttack, combinedSuccessLevel, attackerStats, targetStats, attackerWeapon);
        string furiousAssaultString = attackerStats.FuriousAssault > 0 && !furiousAssault ? $" Jednostka może skorzystać z talentu Wściekły Atak i zaatakować {targetStats.Name} ponownie." : "";
        Debug.Log($"{attackerStats.Name} zadaje {damage} obrażeń.{furiousAssaultString}");

        // 12) Zadaj obrażenia
        // Lista jednostek, które otrzymają obrażenia
        HashSet<Unit> affectedUnits = new HashSet<Unit> { target }; // Dodajemy target od razu

        // Jeśli broń ma cechę Spread (Rozrzucająca), znajdujemy wszystkie jednostki w obszarze rozrzutu
        if (attackerWeapon.Spread > 0 && attackDistance > 0)
        {
            float spreadRadius = attackerWeapon.Spread / 2f;

            if (attackDistance <= attackerWeapon.AttackRange / 10) // Bezpośredni dystans
            {
                damage += attackerWeapon.Spread;
                Debug.Log($"Uwzględnienie cechy broni \"Rozrzucająca\" w bezpośrednim dystansie. Dodatkowe obrażenia: {attackerWeapon.Spread}");
            }
            else
            {
                // Znajduje wszystkie jednostki w obszarze rozrzutu
                List<Collider2D> allColliders = Physics2D.OverlapCircleAll(target.transform.position, spreadRadius).ToList();

                foreach (Collider2D collider in allColliders)
                {
                    if (affectedUnits.Count >= attackerWeapon.Spread)
                        break; // Ogranicza liczbę jednostek do wartości Spread

                    Unit unit = collider.GetComponent<Unit>();
                    if (unit != null)
                    {
                        affectedUnits.Add(unit);
                        Debug.Log($"Do celów ataku dodano jednostkę: {unit.name}");
                    }
                }

                if (attackDistance <= attackerWeapon.AttackRange * 2) // Bliski, średni i daleki dystans
                {
                    Debug.Log($"Uwzględnienie cechy broni \"Rozrzucająca\". Ilość trafionych jednostek: {affectedUnits.Count}");
                }
                else if (attackDistance <= attackerWeapon.AttackRange * 3) // Bardzo daleki dystans
                {
                    damage -= attackerWeapon.Spread;
                    Debug.Log($"Uwzględnienie cechy broni \"Rozrzucająca\" w bardzo dalekim dystansie. Ilość trafionych jednostek: {affectedUnits.Count}. Obrażenia pomniejszone o: {attackerWeapon.Spread}");
                }
            }
        }

        // Jeśli broń ma cechę Blast (Wybuchowa), znajdujemy wszystkie jednostki w obszarze wybuchu
        if (attackerWeapon.Blast > 0)
        {
            float spreadRadius = attackerWeapon.Blast / 2f;

            // Znajduje wszystkie jednostki w obszarze rozrzutu
            List<Collider2D> allColliders = Physics2D.OverlapCircleAll(target.transform.position, spreadRadius).ToList();

            foreach (Collider2D collider in allColliders)
            {
                Unit unit = collider.GetComponent<Unit>();
                if (unit != null)
                {
                    affectedUnits.Add(unit);
                    Debug.Log($"Do celów ataku dodano jednostkę: {unit.name}");
                }
            }
        }

        // Zastosowanie obrażeń dla każdej jednostki w affectedUnits
        if (affectedUnits.Count > 1)
        {
            foreach (Unit affectedUnit in affectedUnits)
            {
                Stats affectedStats = affectedUnit.GetComponent<Stats>();
                int affectedUnitArmor = CalculateArmor(attackerStats, affectedStats, hitLocation, rollOnAttack, attackerWeapon);
                int affectedUnitDamage = CalculateDamage(rollOnAttack, combinedSuccessLevel, attackerStats, affectedStats, attackerWeapon);

                ApplyDamageToTarget(affectedUnitDamage, affectedUnitArmor, attackerStats, affectedStats, affectedUnit, attackerWeapon);
            }
        }
        else
        {
            ApplyDamageToTarget(damage, armor, attackerStats, targetStats, target, attackerWeapon);
        }

        // 13) Animacja ataku i ewentualnie sprawdzenie śmierci
        StartCoroutine(AnimationManager.Instance.PlayAnimation("attack", attacker.gameObject, target.gameObject));

        if (targetStats.TempHealth < 0)
        {
            if (GameManager.IsAutoKillMode)
            {
                HandleDeath(targetStats, target.gameObject, attackerStats);
            }
            else
            {
                if (targetStats.Daemonic > 0)
                {
                    Debug.Log($"<color=red>{targetStats.Name} zostaje odesłany do domeny Chaosu.</color>");
                }
                else
                {
                    StartCoroutine(CriticalWoundRoll(attackerStats, targetStats, hitLocation, attackerWeapon));
                }
            }
        }

        ChangeAttackType(); // Resetuje typ ataku

        // Uwzględnienie talentu "Dwie bronie" i wykonanie drugiego ataku z odwróconym wynikiem kości
        if (attackerStats.DualWielder > 0 && hasTwoWeapons && secondAttackRollResult == 0)
        {
            InventoryManager.Instance.SelectHand(false); // Zmiana na lewą rękę

            int reversedRoll = rollOnAttack == 100 ? 1 : (rollOnAttack % 10) * 10 + (rollOnAttack / 10);
            StartCoroutine(AttackCoroutine(attacker, target, false, false, false, reversedRoll));

            InventoryManager.Instance.SelectHand(true); // Ponowna zmiana na prawą rękę
        }
    }

    public void ApplyDamageToTarget(int damage, int armor, Stats attackerStats, Stats targetStats, Unit target, Weapon attackerWeapon = null, bool ignoring_Wt = false, bool isCorrosiveBloodReaction = false)
    {
        int targetWt = ignoring_Wt ? 0 : targetStats.Wt / 10;
        int reducedDamage = armor + targetWt + targetStats.Robust;
        int finalDamage = 0;

        // W zapasach zbroja nie jest uzwględniania
        if (AttackTypes["Grappling"])
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
            if ((attackerWeapon != null && !attackerWeapon.Undamaging) || isCorrosiveBloodReaction)
            {
                finalDamage = 1;
                reducedDamage = damage - 1;
            }
        }

        // Uwzględnienie cechy Demoniczny lub Ochrona
        if (targetStats.Daemonic > 0 || targetStats.Ward > 0)
        {
            int rollResult = UnityEngine.Random.Range(1, 11);

            // Wybiera aktywną cechę i jej nazwę
            int activeDefense = targetStats.Daemonic > targetStats.Ward ? targetStats.Daemonic : targetStats.Ward;
            string defenseName = targetStats.Daemonic > targetStats.Ward ? "Demoniczny" : "Ochrona";

            bool ignoredDamage = rollResult >= activeDefense;
            string rollMessage = ignoredDamage
                ? $"{targetStats.Name} ignoruje wszystkie obrażenia."
                : $"{targetStats.Name} nie udało się uniknąć obrażeń.";

            Debug.Log($"{targetStats.Name} wykonuje rzut obronny w związku z cechą \"{defenseName}\". Wynik rzutu: {rollResult}. {rollMessage}");

            if (ignoredDamage)
            {
                StartCoroutine(AnimationManager.Instance.PlayAnimation("miss", null, target.gameObject));
                return;
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
                    target.Prone = true; // Powalenie
                }
                else
                {
                    Debug.Log($"Punkty żywotności {targetStats.Name}: <color=#4dd2ff>{targetStats.TempHealth}/{targetStats.MaxHealth}</color>");
                }
            }
            else if (targetStats.TempHealth >= 0)
            {
                Debug.Log($"{targetStats.Name} został/a zraniony/a.");
            }

            target.DisplayUnitHealthPoints();

            if ((targetStats.TempHealth < 0 && GameManager.IsHealthPointsHidingMode) || (targetStats.TempHealth < 0 && targetStats.gameObject.CompareTag("EnemyUnit") && GameManager.IsStatsHidingMode))
            {
                Debug.Log($"Żywotność {targetStats.Name} spadła poniżej zera i wynosi <color=red>{targetStats.TempHealth}</color>.");
                target.Prone = true; // Powalenie
            }

            target.LastAttackerStats = attackerStats;

            // Aktualizuje żywotność w panelu jednostki, jeśli dostała obrażenia w wyniku ataku okazyjnego
            if (Unit.SelectedUnit == target.gameObject)
            {
                UnitsManager.Instance.UpdateUnitPanel(target.gameObject);
            }

            // Resetuje splatanie magii
            if (target.ChannelingModifier != 0)
            {
                Debug.Log($"Splatanie magii {targetStats.Name} zostało przerwane.");

                // Manifestacja Chaosu z modyfikatorem równym uzyskanym poziomom splecenia magii
                MagicManager.Instance.CheckForChaosManifestation(targetStats, 0, 0, -target.ChannelingModifier, "Channeling", 0, true);
            }

            StartCoroutine(AnimationManager.Instance.PlayAnimation("damage", null, target.gameObject, finalDamage));

            // Uwzględnienie cechy "Jad"
            if(attackerStats.Venom)
            {
                target.Poison++;
                target.PoisonTestModifier = attackerStats.VenomModifier;

                Debug.Log($"<color=#FF7F50>{targetStats.Name} zostaje zatruty/a. Modyfikator do testów przeciwko zatruciu: {target.PoisonTestModifier}</color>");
            }

            // Uwzględnienie cechy "Wampiryczny"
            if (attackerStats.Vampiric)
            {
                int healedAmount = Mathf.Min(finalDamage, attackerStats.MaxHealth - attackerStats.TempHealth);
                attackerStats.TempHealth += healedAmount;
                attackerStats.GetComponent<Unit>().DisplayUnitHealthPoints();

                if(healedAmount > 0)
                {
                    Debug.Log($"{attackerStats.Name} zregenerował/a {healedAmount} obrażeń.");
                }
            }

            //Uwzględnienie cechy broni "Przewracająca"
            if(attackerWeapon != null && attackerWeapon.Trip && _isTrainedWeaponCategory)
            {
                Debug.Log($"<color=#FF7F50>Broń {attackerStats.Name} posiada cechę \"Przewracająca\". Możesz zużyć 2 przewagi i wykonać przeciwstawny test Siły i Atletyki. Udany test powali {targetStats.Name}.</color>");
            }

            // Uwzględnienie cechy "Kwasowa krew"
            if (targetStats.CorrosiveBlood && !isCorrosiveBloodReaction)
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

                //Ustalamy miejsce trafienia
                string hitLocation = DetermineHitLocation();

                foreach (var pos in adjacentPositions)
                {
                    Collider2D collider = Physics2D.OverlapPoint(pos);
                    if (collider != null)
                    {
                        Unit adjacentUnit = collider.GetComponent<Unit>();
                        if (adjacentUnit != null && adjacentUnit != targetStats.GetComponent<Unit>())
                        {
                            int adjacentUnitArmor = CalculateArmor(targetStats, adjacentUnit.Stats, hitLocation, UnityEngine.Random.Range(1, 101));
                            int acidDamage = UnityEngine.Random.Range(1, 11);
                            Debug.Log($"{adjacentUnit.Stats.Name} otrzymuje {acidDamage} obrażeń spowodowanych kwasową krwią {targetStats.Name}.");

                            ApplyDamageToTarget(acidDamage, adjacentUnitArmor, targetStats, adjacentUnit.Stats, adjacentUnit, null, false, true);

                            if (adjacentUnit.Stats.TempHealth < 0 && GameManager.IsAutoKillMode)
                            {
                                HandleDeath(adjacentUnit.Stats, adjacentUnit.gameObject, targetStats);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            Debug.Log($"Atak nie przebił pancerza {targetStats.Name} i nie zadał obrażeń.");
            StartCoroutine(AnimationManager.Instance.PlayAnimation("parry", null, target.gameObject));
        }

        // Zaktualizowanie osiągnięć
        attackerStats.TotalDamageDealt += finalDamage;
        if (attackerStats.HighestDamageDealt < finalDamage) attackerStats.HighestDamageDealt = finalDamage;
        targetStats.TotalDamageTaken += finalDamage;
        if (targetStats.HighestDamageTaken < finalDamage) targetStats.HighestDamageTaken = finalDamage;
    }

    public void HandleDeath(Stats targetStats, GameObject target, Stats attackerStats = null)
    {
        // Zapobiega usunięciu postaci graczy, gdy statystyki przeciwników są ukryte
        if (GameManager.IsStatsHidingMode && targetStats.gameObject.CompareTag("PlayerUnit"))
        {
            return;
        }

        // Usuwanie jednostki
        UnitsManager.Instance.DestroyUnit(target);

        if (attackerStats == null && Unit.SelectedUnit != null)
        {
            attackerStats = Unit.SelectedUnit.GetComponent<Stats>();
        }
        else if(attackerStats == null) return;

        StartCoroutine(AnimationManager.Instance.PlayAnimation("kill", attackerStats.gameObject, target));

        // Aktualizacja podświetlenia pól w zasięgu ruchu atakującego
        GridManager.Instance.HighlightTilesInMovementRange(attackerStats);
    }

    public void DistractButtonClick(string value)
    {
        _distractChoice = value;
    }
    #endregion

    #region Defense functions
    public IEnumerator Defense(Unit target, Stats targetStats, Stats attackerStats, Weapon attackerWeapon, Weapon targetWeapon, MeleeCategory targetMeleeSkill, int parryValue, int dodgeValue, int parryModifier, int dodgeModifier, bool canParry, bool canDodge)
    {
        if (!GameManager.IsAutoDefenseMode)
        {
            yield return StartCoroutine(ManualDefense(target, targetStats, attackerStats, targetWeapon, parryValue, dodgeValue, parryModifier, dodgeModifier, targetMeleeSkill, canParry, canDodge));
        }
        else
        {
            yield return StartCoroutine(AutoDefense(target, targetStats, attackerStats, targetWeapon, parryValue, dodgeValue, parryModifier, dodgeModifier, targetMeleeSkill, canParry, canDodge));
        }

        yield return null;
    }
    public Weapon GetBestParryWeapon(Stats targetStats, Weapon defaultWeapon)
    {
        return targetStats.GetComponent<Inventory>().EquippedWeapons
            .Where(w => w != null)
            .OrderByDescending(w =>
            {
                int modifier = w.Defensive ? 10 : 0;
                modifier += w.Unbalanced ? -10 : 0;
                if (w == targetStats.GetComponent<Inventory>().EquippedWeapons[1] && w.Shield == 0)
                {
                    int ambidextrousBonus = targetStats.Ambidextrous > 0 ? Math.Min(20, targetStats.Ambidextrous * 10) : 0;
                    modifier -= 20 - ambidextrousBonus;
                }
                return modifier;
            }).FirstOrDefault() ?? defaultWeapon;
    }

    public int CalculateParryModifier(Unit target, Stats targetStats, Stats attackerStats, Weapon weaponUsedForParry, Weapon attackerWeapon)
    {
        int modifier = target.DefensiveBonus;
        if (target.Fatiqued > 0) modifier -= target.Fatiqued * 10;// Modyfikator za wyczerpanie
        else if (target.Stunned > 0 || target.Poison > 0) modifier -= 10; // Modyfikator za oszołomienie lub zatrucie

        if (weaponUsedForParry.Defensive) modifier += 10;
        if (weaponUsedForParry.Unbalanced) modifier -= 10;
        if (attackerWeapon.Wrap && _isTrainedWeaponCategory) modifier -= 10;
        if (attackerWeapon.Slow) modifier += 10;
        if (attackerWeapon.Fast && _isTrainedWeaponCategory) modifier -= 10;
        if (attackerStats.Size > targetStats.Size) modifier -= (attackerStats.Size - targetStats.Size) * 20; // Kara do parowania za rozmiar
        if (attackerStats.Size > targetStats.Size) Debug.Log($"Modyfikator do parowania za rozmiar atakującego: -{(attackerStats.Size - targetStats.Size) * 20}");

        // Modyfikator za dekoncentrującego przeciwnika w pobliżu
        foreach (var entry in InitiativeQueueManager.Instance.InitiativeQueue)
        {
            Unit unit = entry.Key;
            Stats distractingStats = unit.GetComponent<Stats>();
            if (distractingStats.Distracting && !ReferenceEquals(distractingStats, targetStats) && !unit.CompareTag(targetStats.tag))
            {
                float radius = (distractingStats.Wt / 10) / 2f;
                float distance = Vector2.Distance(unit.transform.position, targetStats.transform.position);

                if (distance <= radius)
                {
                    modifier -= 20;
                    break; // tylko raz -20, nawet jeśli więcej jednostek dekoncentruje
                }
            }
        }

        return modifier;
    }

    public int CalculateDodgeModifier(Unit target, Stats targetStats, Weapon attackerWeapon = null)
    {
        int modifier = target.DefensiveBonus;
        if (target.Fatiqued > 0) modifier -= target.Fatiqued * 10;// Modyfikator za wyczerpanie
        else if (target.Stunned > 0 || target.Poison > 0) modifier -= 10; // Modyfikator za oszołomienie lub zatrucie

        if (target.Prone) modifier -= 20;
        if (attackerWeapon != null && attackerWeapon.Slow) modifier += 10;

        if (target.IsMounted && targetStats.Vaulting == 0) modifier -= 20; // Modyfikator za dosiadanie wierzchowca bez talentu woltyżerka

        // Modyfikator za dekoncentrującego przeciwnika w pobliżu
        foreach (var entry in InitiativeQueueManager.Instance.InitiativeQueue)
        {
            Unit unit = entry.Key;
            Stats distractingStats = unit.GetComponent<Stats>();
            if (distractingStats.Distracting && !ReferenceEquals(distractingStats, targetStats) && !unit.CompareTag(targetStats.tag))
            {
                float radius = (distractingStats.Wt / 10) / 2f;
                float distance = Vector2.Distance(unit.transform.position, targetStats.transform.position);

                if (distance <= radius)
                {
                    modifier -= 20;
                    break; // tylko raz -20, nawet jeśli więcej jednostek dekoncentruje
                }
            }
        }

        //Modyfikator za przeciążenie
        int encumbrancePenalty = 0;
        if (targetStats.MaxEncumbrance - targetStats.CurrentEncumbrance < 0 && targetStats.CurrentEncumbrance < targetStats.MaxEncumbrance * 2)
        {
            encumbrancePenalty = 10;
        }
        else if (targetStats.MaxEncumbrance - targetStats.CurrentEncumbrance < 0 && targetStats.CurrentEncumbrance < targetStats.MaxEncumbrance * 3)
        {
            encumbrancePenalty = 20;
        }

        // Sprawdzamy, czy Zw nie spadnie poniżej 10
        if (targetStats.Zw - encumbrancePenalty < 10)
        {
            encumbrancePenalty = targetStats.Zw - 10;
        }

        modifier -= encumbrancePenalty;

        return modifier;
    }

    private IEnumerator ManualDefense(Unit target, Stats targetStats, Stats attackerStats, Weapon targetWeapon, int parryValue, int dodgeValue, int parryModifier, int dodgeModifier, MeleeCategory targetMeleeSkill, bool canParry, bool canDodge)
    {
        _parryAndDodgePanel.SetActive(true);
        _parryAndDodgePanel.GetComponentInChildren<TMP_Text>().text = "Wybierz reakcję atakowanej postaci.";
        _parryOrDodge = ""; // Resetujemy wybór reakcji obronnej
        _parryButton.gameObject.SetActive(canParry);
        _dodgeButton.gameObject.SetActive(canDodge);

        // Najpierw czekamy, aż gracz kliknie którykolwiek przycisk
        yield return new WaitUntil(() => !_parryAndDodgePanel.activeSelf);

        if (_parryOrDodge == "parry")
        {
            yield return StartCoroutine(Parry(target, targetStats, attackerStats, targetWeapon, parryValue, parryModifier, targetMeleeSkill));
        }
        else if (_parryOrDodge == "dodge")
        {
            yield return StartCoroutine(Dodge(target, targetStats, dodgeValue, dodgeModifier));
        }
    }

    private IEnumerator AutoDefense(Unit target, Stats targetStats, Stats attackerStats, Weapon targetWeapon, int parryValue, int dodgeValue, int parryModifier, int dodgeModifier, MeleeCategory targetMeleeSkill, bool canParry, bool canDodge)
    {
        if (parryValue >= dodgeValue && canParry)
        {
            yield return StartCoroutine(Parry(target, targetStats, attackerStats, targetWeapon, parryValue, parryModifier, targetMeleeSkill));
        }
        else if (canDodge)
        {
            yield return StartCoroutine(Dodge(target, targetStats, dodgeValue, dodgeModifier));
        }
    }

    private IEnumerator Parry(Unit target, Stats targetStats, Stats attackerStats, Weapon targetWeapon, int parryValue, int parryModifier, MeleeCategory targetMeleeSkill)
    {
        int defenceRollResult = 0;

        // Jeżeli jest ustawiony modyfikator w panelu jednostki
        if (DiceRollManager.Instance.RollModifier != 0)
        {
            parryValue += DiceRollManager.Instance.RollModifier;
            parryModifier += DiceRollManager.Instance.RollModifier;
            DiceRollManager.Instance.ResetRollModifier();
        }

        // Jeżeli jesteśmy w trybie manualnych rzutów kośćmi i wybrana jednostka to sojusznik to czekamy na wynik rzutu
        if (!GameManager.IsAutoDiceRollingMode && target.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, "parowanie", result => defenceRollResult = result));
            if (defenceRollResult == 0) yield break;
        }
        else
        {
            defenceRollResult = UnityEngine.Random.Range(1, 101);
        }

        DefenceResults = CalculateSuccessLevel(targetWeapon, defenceRollResult, parryValue, false);

        // Modyfikator za talent Tarczownik
        if (DefenceResults[0] >= 0 && targetStats.Shieldsman > 0 && target.GetComponent<Inventory>().EquippedWeapons.Any(w => w != null && w.Shield > 0))
        {
            DefenceResults[1] += targetStats.Shieldsman;
            parryModifier += targetStats.Shieldsman * 10;
        }
        // Modyfikator za talent Riposta
        if (DefenceResults[0] >= 0 && targetStats.Riposte > 0) 
        {
            DefenceResults[1] += targetStats.Riposte;
            parryModifier += targetStats.Riposte * 10;
        }

        int defenceSuccessValue = DefenceResults[0];
        int defenceSuccessLevel = DefenceResults[1];
        string coloredText = defenceSuccessValue >= 0 ? "green" : "red";
        string parryModifierString = parryModifier != 0 ? $" Modyfikator: {parryModifier}," : "";
        Debug.Log($"{targetStats.Name} próbuje parować przy użyciu {targetWeapon.Name}. Wynik rzutu: {defenceRollResult}. Wartość umiejętności: {targetStats.WW + targetStats.GetSkillModifier(targetStats.Melee, targetMeleeSkill)}.{parryModifierString} PS: <color={coloredText}>{defenceSuccessLevel}</color>.");

        // Obsługa fuksa / pecha
        if (DiceRollManager.Instance.IsDoubleDigit(defenceRollResult))
        {
            if (defenceSuccessValue >= 0)
            {
                Debug.Log($"{targetStats.Name} wyrzucił <color=green>FUKSA</color>!");

                //Trafienie krytyczne
                StartCoroutine(CriticalWoundRoll(targetStats, attackerStats, DetermineHitLocation(), targetWeapon, defenceRollResult));

                targetStats.FortunateEvents++;
            }
            else
            {
                Debug.Log($"{targetStats.Name} wyrzucił <color=red>PECHA</color>!");
                targetStats.UnfortunateEvents++;

                //Uwzględnienie cechy broni "Tandetny"
                if (targetWeapon.Shoddy)
                {
                    targetWeapon.Damage = targetWeapon.S;
                    Debug.Log($"<color=red>{targetWeapon.Name} ulega zniszczeniu ze względu na cechę \"Tandetny\".</color>");
                }
            }
        }
    }

    public IEnumerator Dodge(Unit target, Stats targetStats, int dodgeValue, int dodgeModifier)
    {
        int defenceRollResult = 0;

        // Jeżeli jest ustawiony modyfikator w panelu jednostki
        if (DiceRollManager.Instance.RollModifier != 0)
        {
            dodgeValue += DiceRollManager.Instance.RollModifier;
            dodgeModifier += DiceRollManager.Instance.RollModifier;
            DiceRollManager.Instance.ResetRollModifier();
        }

        // Jeżeli jesteśmy w trybie manualnych rzutów kośćmi i wybrana jednostka to sojusznik to czekamy na wynik rzutu
        if (!GameManager.IsAutoDiceRollingMode && target.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, "unik", result => defenceRollResult = result));
            if (defenceRollResult == 0) yield break;
        }
        else
        {
            defenceRollResult = UnityEngine.Random.Range(1, 101);
        }

        DefenceResults = CalculateSuccessLevel(null, defenceRollResult, dodgeValue, false);

        // Modyfikator za talent Woltyżerka
        if (DefenceResults[0] >= 0 && target.IsMounted && targetStats.Vaulting > 0)
        {
            DefenceResults[1] += targetStats.Vaulting;
            dodgeModifier += targetStats.Vaulting * 10;
        }

        int defenceSuccessValue = DefenceResults[0];
        int defenceSuccessLevel = DefenceResults[1];
        string coloredText = defenceSuccessValue >= 0 ? "green" : "red";
        string dodgeModifierString = dodgeModifier != 0 ? $" Modyfikator: {dodgeModifier}." : "";
        Debug.Log($"{targetStats.Name} próbuje unikać. Wynik rzutu: {defenceRollResult}. Wartość umiejętności: {targetStats.Dodge + targetStats.Zw}.{dodgeModifierString} PS: <color={coloredText}>{defenceSuccessLevel}</color>.");

        // Obsługa fuksa / pecha
        if (DiceRollManager.Instance.IsDoubleDigit(defenceRollResult))
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
    public void ParryOrDodgeButtonClick(string parryOrDodge)
    {
        _parryOrDodge = parryOrDodge;
    }
    #endregion

    #region Calculate success level
    public int[] CalculateSuccessLevel(Weapon weapon, int rollResult, int skillValue, bool isAttack, int modifier = 0)
    {
        int successValue = skillValue + modifier - rollResult;
        int successLevel = (skillValue + modifier) / 10 - rollResult / 10;

        //Uwzględnia dodatkowe cechy broni
        if (weapon != null)
        {
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
            else if (isAttack) // Udany test
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
        }

        return new int[] { successValue, successLevel };
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
            Debug.LogError("Nie udało się ustalić odległości pomiędzy jednostkami.");
            return 0;
        }
    }

    public bool ValidateRangedAttack(Unit attacker, Unit target, Weapon attackerWeapon, float attackDistance)
    {
        // Sprawdza, czy broń jest naładowana
        if (attackerWeapon.ReloadLeft != 0)
        {
            Debug.Log($"Broń {attacker.GetComponent<Stats>().Name} wymaga przeładowania.");
            return false;
        }

        // Sprawdza, czy cel nie znajduje się zbyt blisko
        if (attackDistance <= 1.5f && !attackerWeapon.Pistol)
        {
            Debug.Log($"{attacker.GetComponent<Stats>().Name} stoi zbyt blisko celu, aby wykonać atak dystansowy.");

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
                    Debug.Log($"Na linii strzału {attacker.GetComponent<Stats>().Name} znajduje się przeszkoda, przez którą strzał jest niemożliwy.");
                    return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region Calculate attack modifier
    //Oblicza modyfikator do trafienia
    public int CalculateAttackModifier(Unit attackerUnit, Weapon attackerWeapon, Unit targetUnit, float attackDistance = 0, bool furiousAssault = false)
    {
        int attackModifier = DiceRollManager.Instance.RollModifier;
        DiceRollManager.Instance.ResetRollModifier();
        Stats attackerStats = attackerUnit.GetComponent<Stats>();
        Stats targetStats = targetUnit.GetComponent<Stats>();

        // Modyfikator za celowanie
        attackModifier += attackerUnit.AimingBonus;
        if (attackerUnit.AimingBonus != 0) Debug.Log($"Uwzględniono modyfikator +20 za celowanie. Łączny modyfikator: " + attackModifier);

        // Modyfikator za różnicę rozmiarów
        if (attackerStats.Size < targetStats.Size)
        {
            attackModifier += 10;
            Debug.Log($"Uwzględniono modyfikator +10 za różnicę rozmiarów. Łączny modyfikator: " + attackModifier);
        }

        // Modyfikator za szarżę
        if (attackerUnit.IsCharging)
        {
            attackModifier += 10;
            Debug.Log($"Uwzględniono modyfikator za szarżę. Łączny modyfikator: " + attackModifier);
        }

        // Modyfikator za broń z cechą "Celny"
        if (attackerWeapon.Accurate && _isTrainedWeaponCategory)
        {
            attackModifier += 10;
            Debug.Log($"Uwzględniono modyfikator za cechę celny. Łączny modyfikator: " + attackModifier);
        }

        // Utrudnienie za atak słabszą ręką
        if (attackerUnit.GetComponent<Inventory>().EquippedWeapons[0] == null || attackerWeapon.Name != attackerUnit.GetComponent<Inventory>().EquippedWeapons[0].Name)
        {
            if (attackerWeapon.Id != 0)
            {
                attackModifier -= attackerStats.Ambidextrous > 0 ? 20 - Math.Min(20, attackerStats.Ambidextrous * 10) : 20; // Uwzględnia talent oburęczność
                Debug.Log($"Uwzględniono modyfikator za atak słabszą ręką. Łączny modyfikator: " + attackModifier);
            }
        }

        //// Modyfikatory za jakość broni
        //if (attackerWeapon.Quality == "Kiepska") attackModifier -= 5;
        //else if (attackerWeapon.Quality == "Najlepsza" || attackerWeapon.Quality == "Magiczna") attackModifier += 5;

        //Debug.Log($"Uwzględniono modyfikator za jakość broni. Łączny modyfikator: " + attackModifier);

        // Modyfikator za stany celu
        if (targetUnit.Deafened > 0 || targetUnit.Stunned > 0 || targetUnit.Blinded > 0) attackModifier += 10;
        if (targetUnit.Surprised)
        {
            attackModifier += 20;
        }
        if ((targetUnit.Entangled > 0 || targetUnit.EntangledUnitId != 0) && attackerUnit.Entangled == 0 && attackerUnit.EntangledUnitId == 0)
        {
            // DODAĆ TUTAJ W PRZYSZŁOŚCI, ŻEBY UWZGLĘDNIAŁO, KTO Z WALCZĄCYCH W ZAPASACH MA ILE POZIOMÓW POCHWYCENIA. NA TEGO, KTO MA MNIEJ JEST MODYFIKATOR +10, A TEGO KTO WIĘCEJ +20

            attackModifier += targetUnit.Entangled == 0 ? 10 : 20;
        }

        //Modyfikator za strach
        if(attackerUnit.FearedUnits.Contains(targetUnit))
        {
            attackModifier -= 10;
            Debug.Log($"Uwzględniono modyfikatory za strach przed celem ataku. Łączny modyfikator: " + attackModifier);
        }

        Debug.Log($"Uwzględniono modyfikatory za stany celu. Łączny modyfikator: " + attackModifier);

        // Modyfikator za dekoncentrującego przeciwnika w pobliżu
        foreach (var entry in InitiativeQueueManager.Instance.InitiativeQueue)
        {
            Unit unit = entry.Key;
            Stats distractingStats = unit.GetComponent<Stats>();
            if (distractingStats.Distracting && !ReferenceEquals(distractingStats, attackerStats) && !unit.CompareTag(attackerStats.tag))
            {
                float radius = (distractingStats.Wt / 10) / 2f;
                float distance = Vector2.Distance(unit.transform.position, attackerStats.transform.position);

                if (distance <= radius)
                {
                    attackModifier -= 20;
                    Debug.Log($"{attackerStats.Name} jest zdekoncentrowany przez {distractingStats.Name}. Uwzględniono modyfikator -20.");
                    break; // tylko raz -20, nawet jeśli więcej jednostek dekoncentruje
                }
            }
        }

        // Modyfikator za wyczerpanie
        if (attackerUnit.Fatiqued > 0) attackModifier -= attackerUnit.Fatiqued * 10;
        else if (attackerUnit.Poison > 0) attackModifier -= 10;

        Debug.Log($"Uwzględniono modyfikatory za stany atakującego. Łączny modyfikator: " + attackModifier);


        // Przewaga liczebna
        int adjacentEnemies;
        int outNumber = CountOutnumber(attackerUnit, targetUnit, out adjacentEnemies);

        // Tylko dla broni w Walce Wręcz
        if (attackerWeapon.Type.Contains("melee"))
        {
            // Przewaga liczebna
            attackModifier += outNumber;

            if(outNumber != 0)
            {
                Debug.Log("Uwzględniono modyfikator za przewagę liczebną. Łączny modyfikator: " + attackModifier);
            }

            if (attackerUnit.FeintedUnitId == targetUnit.UnitId)
            {
                attackModifier += attackerUnit.FeintModifier;
                Debug.Log("Uwzględniono modyfikator za fintę. Łączny modyfikator: " + attackModifier);
            }

            //Uwzględnienie talentu Wściekły Atak
            if (furiousAssault)
            {
                attackModifier += attackerStats.FuriousAssault * 10;
                Debug.Log("Uwzględniono modyfikator za Wściekły Atak. Łączny modyfikator: " + attackModifier);
            }

            // Modyfikator za różnicę rozmiarów dzięki wierzchowcowi atakującego
            if (attackerUnit.IsMounted && attackerUnit.Mount.GetComponent<Stats>().Size > targetStats.Size)
            {
                attackModifier += 20;
                Debug.Log($"Uwzględniono modyfikator +20 za przewagę rozmiaru wierzchowca dosiadanego przez atakującego. Łączny modyfikator: " + attackModifier);
            }

            // Modyfikator za różnicę rozmiarów dzięki wierzchowcowi celu
            if (targetUnit.IsMounted && targetUnit.Mount != null && targetUnit.Mount.GetComponent<Stats>().Size > attackerStats.Size && attackerWeapon.AttackRange == 1.5f)
            {
                attackModifier -= 10;
                Debug.Log($"Uwzględniono modyfikator -10 za przewagę rozmiaru wierzchowca dosiadanego przez cel. Łączny modyfikator: " + attackModifier);
            }
        }

        if (attackerWeapon.Type.Contains("ranged"))
        {
            // Sprawdza zasięg
            float effectiveAttackRange = attackerWeapon.AttackRange;

            if (attackerWeapon.Type.Contains("throwing")) // Oblicz właściwy zasięg ataku, uwzględniając broń miotaną
            {
                effectiveAttackRange *= attackerStats.S / 10;
            }

            // Modyfikator za dystans
            attackModifier += attackDistance switch
            {
                _ when attackDistance <= effectiveAttackRange / 10 => 40, // Bezpośredni dystans
                _ when attackDistance <= effectiveAttackRange / 2 => 20,  // Bliski dystans
                _ when attackDistance <= effectiveAttackRange => 0,      // Średni dystans
                _ when attackDistance <= effectiveAttackRange * 2 => attackerStats.Sniper > 0 ? (-10 + attackerStats.Sniper * 10) : -10, // Daleki dystans
                _ when attackDistance <= effectiveAttackRange * 3 => attackerStats.Sniper > 0 ? (-30 + attackerStats.Sniper * 10) : -30, // Bardzo daleki dystans
                _ => 0 // Domyślny przypadek, jeśli żaden warunek nie zostanie spełniony
            };

            Debug.Log($"Dystans: {attackDistance}. Zasięg broni: {effectiveAttackRange}");
            Debug.Log("Uwzględniono modyfikator za dystans. Łączny modyfikator: " + attackModifier);

            //Modyfikator za rozmiar celu
            if (targetStats.Size == SizeCategory.Monstrous) attackModifier += 60;
            else if (targetStats.Size == SizeCategory.Enormous) attackModifier += 40;
            else if (targetStats.Size == SizeCategory.Large) attackModifier += 20;
            else if (targetStats.Size == SizeCategory.Small && !attackerStats.Sharpshooter) attackModifier -= 10;
            else if (targetStats.Size == SizeCategory.Little && !attackerStats.Sharpshooter) attackModifier -= 20;
            else if (targetStats.Size == SizeCategory.Tiny && !attackerStats.Sharpshooter) attackModifier -= 30;

            if(targetStats.Size != SizeCategory.Average)
            {
                Debug.Log("Uwzględniono modyfikator za rozmiar celu. Łączny modyfikator: " + attackModifier);
            }

            //Modyfikator za oślepienie
            if (attackerUnit.Blinded > 0 && attackerUnit.Fatiqued == 0 && attackerUnit.Poison == 0)
            {
                attackModifier -= 10;
                Debug.Log("Uwzględniono modyfikator za oślepienie atakującego. Łączny modyfikator: " + attackModifier);
            }

           

            // Sprawdza, czy na linii strzału znajduje się przeszkoda
            RaycastHit2D[] raycastHits = Physics2D.RaycastAll(attackerUnit.transform.position, targetUnit.transform.position - attackerUnit.transform.position, attackDistance);

            foreach (var raycastHit in raycastHits)
            {
                if (raycastHit.collider == null) continue;

                var mapElement = raycastHit.collider.GetComponent<MapElement>();
                var unit = raycastHit.collider.GetComponent<Unit>();

                if (mapElement != null && mapElement.IsLowObstacle)
                {
                    attackModifier -= 20;
                    Debug.Log($"Strzał jest wykonywany w jednostkę znajdującą się za przeszkodą. Zastosowano modyfikator -20 do trafienia. Łączny modyfikator: " + attackModifier);
                    break; // Żeby modyfikator nie kumulował się za każdą przeszkodę
                }

                if (unit != null && unit != targetUnit && unit != attackerUnit && !_groupOfTargets.Contains(unit))
                {
                    attackModifier -= 20;
                    Debug.Log("Na linii strzału znajduje się inna jednostka. Zastosowano modyfikator -20 do trafienia. Łączny modyfikator: " + attackModifier);
                    break; // Żeby modyfikator nie kumulował się za każdą postać
                }
            }

            // Jeżeli strzelamy w grupę jednostek (i chcemy trafić którąkolwiek)
            int groupSize = _groupOfTargets.Length;
            if (groupSize > 1 && !_groupOfTargetsPenalty)
            {
                _groupTargetModifier = 0;

                if (groupSize >= 7)
                    _groupTargetModifier = 40;
                else if (groupSize >= 3)
                    _groupTargetModifier = 20;

                attackModifier += _groupTargetModifier;

                if(_groupTargetModifier != 0)
                {
                    Debug.Log("Cel ataku znajduje się w grupie innych jednostek. Zastosowano modyfikator do trafienia w całą grupę. Łączny modyfikator: " + attackModifier);
                }

                _newTargetUnit = _groupOfTargets[UnityEngine.Random.Range(0, _groupOfTargets.Length)];
                Debug.Log($"Pocisk kieruje się w stronę {_newTargetUnit.Stats.Name}.");
            }
            else if (adjacentEnemies > 0 && _groupOfTargetsPenalty) //Zastosowanie kary za strzelanie w grupę jednostek (jeśli nie chcemy trafić którejkolwiek)
            {
                attackModifier -= 20;
                _groupOfTargetsPenalty = false;
                Debug.Log("Uwzględniono modyfikator za to, że cel jest zaangażowany w walkę w zwarciu. Łączny modyfikator: " + attackModifier);
            }
        }

        return attackModifier;
    }

    //Modyfikator za przewagę liczebną
    private int CountOutnumber(Unit attacker, Unit target, out int adjacentAllies)
    {
        if (attacker.CompareTag(target.tag))
        {
            adjacentAllies = 0;
            return 0; // Jeśli atakujemy sojusznika to pomijamy przewagę liczebną
        }

        int adjacentOpponents = 0; // Przeciwnicy atakującego stojący obok celu ataku
        adjacentAllies = 0;    // Sojusznicy atakującego stojący obok celu ataku
        int adjacentOpponentsNearAttacker = 0; // Przeciwnicy atakującego stojący obok atakującego
        int modifier = 0;

        // Zbiór do przechowywania już policzonych przeciwników
        HashSet<Collider2D> countedOpponents = new HashSet<Collider2D>();

        List<Unit> alliesUnits = new List<Unit>();
        List<Unit> opponentsUnits = new List<Unit>();

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

                if (collider.CompareTag(allyTag) && (InventoryManager.Instance.ChooseWeaponToAttack(collider.gameObject).Type.Contains("melee") || pos == center))
                {
                    allies++;
                    alliesUnits.Add(collider.GetComponent<Unit>());
                }
                else if (collider.CompareTag(opponentTag) && !opponentsUnits.Contains(collider.GetComponent<Unit>()) && (InventoryManager.Instance.ChooseWeaponToAttack(collider.gameObject).Type.Contains("melee") || pos == center))
                {
                    opponents++;
                    opponentsUnits.Add(collider.GetComponent<Unit>());
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

        // Uwzględnienie talentu Mistrz Walki
        if (adjacentOpponents != adjacentAllies)
        {
            foreach (Unit allyUnit in alliesUnits)
            {
                if (allyUnit.Stats.CombatMaster > 0)
                {
                    int combatMasterBonus = allyUnit.Stats.CombatMaster; // 1 → liczone jako 2 jednostki, 2 → jako 3 itd.
                    int potentialAllies = adjacentAllies + combatMasterBonus;

                    // CombatMaster może jedynie niwelować przewagę przeciwników, ale nie dawać przewagi
                    adjacentAllies = Mathf.Min(potentialAllies, adjacentOpponents);
                }
            }
        }
        else if (adjacentAllies > adjacentOpponents)
        {
            foreach (Unit opponentUnit in opponentsUnits)
            {
                if (opponentUnit.Stats.CombatMaster > 0)
                {
                    int combatMasterBonus = opponentUnit.Stats.CombatMaster;
                    int potentialOpponents = adjacentOpponents + combatMasterBonus;

                    // CombatMaster może jedynie niwelować przewagę sojuszników, ale nie dawać przewagi
                    adjacentOpponents = Mathf.Min(potentialOpponents, adjacentAllies);
                }
            }
        }

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
    #endregion

    #region Calculating damage
    int CalculateDamage(int attackRoll, int successLevel, Stats attackerStats, Stats targetStats, Weapon attackerWeapon)
    {
        Unit attackerUnit = attackerStats.GetComponent<Unit>();
        int damage;

        // Uwzględnienie cechy broni "Przebijająca"
        if (!attackerWeapon.Undamaging && (attackerWeapon.Damaging || attackerStats.Size > targetStats.Size || (attackerUnit.IsCharging && attackerUnit.IsMounted && attackerUnit.Mount.GetComponent<Stats>().Size > targetStats.Size)) && successLevel < attackRoll % 10 && (!attackerWeapon.Tiring || attackerUnit.IsCharging) && _isTrainedWeaponCategory)
        {
            Debug.Log($"Używamy cechy Przebijająca i zamieniamy PS z {successLevel} na {attackRoll % 10}");
            successLevel = attackRoll % 10;
        }

        if (attackerWeapon.Type.Contains("melee") || attackerWeapon.Type.Contains("strength-based")) //Oblicza łączne obrażenia dla ataku w zwarciu
        {
            int strengthModifier = attackerStats.S / 10;

            if (attackerUnit.IsCharging && attackerUnit.IsMounted)
            {
                // Wykorzystanie siły wierzchowca w szarży
                if(attackerUnit.Mount.GetComponent<Stats>().S > attackerStats.S)
                {
                    strengthModifier = attackerUnit.Mount.GetComponent<Stats>().S / 10;
                }
            }

            Debug.Log($"Łączny poziom sukcesu {attackerStats.Name}: {successLevel}. Bonus z siły: {strengthModifier}. Siła broni: {Math.Max(0, attackerWeapon.S - attackerWeapon.Damage)}");
            damage = successLevel + strengthModifier + Math.Max(0, attackerWeapon.S - attackerWeapon.Damage);
        }
        else //Oblicza łączne obrażenia dla ataku dystansowego
        {
            string accurateShotString = attackerStats.AccurateShot > 0 ? $". Bonus za talent Celny Strzał: {attackerStats.AccurateShot}" : "";
            Debug.Log($"Poziom sukcesu {attackerStats.Name}: {successLevel}. Siła broni: {Math.Max(0, attackerWeapon.S - attackerWeapon.Damage)}{accurateShotString}");
            damage = successLevel + Math.Max(0, attackerWeapon.S - attackerWeapon.Damage) + attackerStats.AccurateShot;
        }

        // Talent "Cios poniżej pasa"
        if (attackerWeapon.Category == "brawling" && attackerStats.DirtyFighting > 0)
        {
            damage += attackerStats.DirtyFighting;
            Debug.Log($"Dodatkowe obrażenia za talent Cios Poniżej Pasa: {attackerStats.DirtyFighting}");
        }

        // Uwzględnia cechę Druzgoczący
        if (attackerWeapon.Impact && !attackerWeapon.Undamaging && (!attackerWeapon.Tiring || attackerUnit.IsCharging) || attackerStats.Size - targetStats.Size >= 2 || (attackerUnit.IsCharging && attackerUnit.IsMounted && attackerUnit.Mount.GetComponent<Stats>().Size - targetStats.Size >= 2) && _isTrainedWeaponCategory)
        {
            damage += attackRoll % 10; // Dodaje liczbę jedności z rzutu na atak
            Debug.Log($"Dodatkowe obrażenia za cechę Druzgoczący: {attackRoll % 10}");
        }

        // Uwzględnia przewagę rozmiaru
        if (attackerStats.Size > targetStats.Size && (attackerStats.Size - targetStats.Size) > 1)
        {
            damage *= attackerStats.Size - targetStats.Size;
            Debug.Log($"Modyfikator obrażeń za rozmiar. Rozmiar atakującego: {attackerStats.Size}. Rozmiar celu: {targetStats.Size}. Obrażenia zostały pomnożone x{attackerStats.Size - targetStats.Size}.");
        }

        // Modyfikator za rozmiar wierzchowca podczas szarży
        if (attackerUnit.IsCharging && attackerUnit.IsMounted && attackerUnit.Mount.GetComponent<Stats>().Size - targetStats.Size >= 2)
        {
            damage *= attackerUnit.Mount.GetComponent<Stats>().Size - targetStats.Size;
            Debug.Log($"Modyfikator obrażeń w trakcie szarży za rozmiar wierzchowca. Rozmiar wierzchowca: {attackerUnit.Mount.GetComponent<Stats>().Size}. Rozmiar celu: {targetStats.Size}. Obrażenia zostały pomnożone x{attackerUnit.Mount.GetComponent<Stats>().Size - targetStats.Size}.");
        }

        // Uwzględnienie talentu Nieugięty
        if (AttackTypes["Charge"] && attackerStats.Resolute > 0)
        {
            damage += attackerStats.Resolute;
            Debug.Log($"Dodatkowe obrażenia za talent Nieugięty: {attackerStats.Resolute}");
        }

        // Uwzględnienie talentu Silny Cios
        if (attackerWeapon.Type.Contains("melee") && !attackerWeapon.Type.Contains("no-damage") && attackerStats.StrikeMightyBlow > 0)
        {
            damage += attackerStats.StrikeMightyBlow;
            Debug.Log($"Dodatkowe obrażenia za talent Silny Cios: {attackerStats.StrikeMightyBlow}");
        }

        if (attackerUnit.IsFrenzy)
        {
            damage++;
            Debug.Log($"Dodatkowe obrażenia za Szał Bojowy: 1");
        }

        if (damage < 0) damage = 0;

        return damage;
    }
    #endregion

    #region Critical wounds
    public IEnumerator CriticalWoundRoll(Stats attackerStats, Stats targetStats, string hitLocation, Weapon attackerWeapon = null, int rollOnAttack = 0)
    {
        //TA METODA JEST DO ROZBUDOWANIA. Można dodać konkretne dodatkowe efekty np. ogłuszenie, krwawienie itp. w zależności również od lokacji

        // Uwzględnienie cechy Demoniczny
        if (targetStats.Daemonic > 0)
        {
            int daemonicRollResult = UnityEngine.Random.Range(1, 11);
            bool ignoredDamage = daemonicRollResult >= targetStats.Daemonic;
            string daemonicRollMessage = ignoredDamage
                ? $"{targetStats.Name} ignoruje wszystkie obrażenia."
                : $"{targetStats.Name} nie udało się uniknąć obrażeń.";

            Debug.Log($"{targetStats.Name} wykonuje rzut obronny w związku z cechą \"Demoniczny\". Wynik rzutu: {daemonicRollResult}. {daemonicRollMessage}");

            if (ignoredDamage) yield break;
        }

        // Pobranie pancerza dla trafionej lokalizacji
        string normalizedHitLocation = NormalizeHitLocation(hitLocation);
        List<Weapon> armorByLocation = targetStats.GetComponent<Inventory>().ArmorByLocation.ContainsKey(normalizedHitLocation) ? targetStats.GetComponent<Inventory>().ArmorByLocation[normalizedHitLocation] : new List<Weapon>();

        // Element pancerza, który został trafiony
        Weapon selectedArmor = armorByLocation
             .FirstOrDefault(weapon => (weapon.Category == "plate") && weapon.Armor - weapon.Damage > 0) // Najpierw szuka zbroi płytowej lub tarczy, ale tylko jeśli Armor > 0 (inaczej oznacza, że pancerz jest już całkowicie zniszczony)
             ?? armorByLocation.FirstOrDefault(weapon => weapon.Category == "chain" && weapon.Armor - weapon.Damage > 0) // Potem zbroja kolcza, ale tylko jeśli Armor > 0
             ?? armorByLocation.FirstOrDefault(weapon => weapon.Category == "leather" && weapon.Armor - weapon.Damage > 0); // Na końcu zbroja skórzana, ale tylko jeśli Armor > 0

        //Uwzględnienie cechy Nieprzebijalny
        if (rollOnAttack != 0 && selectedArmor != null && selectedArmor.Impenetrable && rollOnAttack % 2 != 0)
        {
            Debug.Log($"{selectedArmor.Name} posiada cechę \"Nieprzebijalny\". Trafienie krytyczne jest ignorowane.");
            yield break;
        }

        if (selectedArmor != null && targetStats.TempHealth >= 0)
        {
            _criticalDeflectionPanel.SetActive(true);

            // Najpierw czekamy, aż gracz kliknie którykolwiek przycisk
            yield return new WaitUntil(() => !_criticalDeflectionPanel.activeSelf);

            if (_criticalDeflection == "damage_armor")
            {
                if (selectedArmor.Durable == 0) // Uwzględnienie cechy Wytrzymały
                {
                    selectedArmor.Damage++;
                }
                else
                {
                    selectedArmor.Durable--;
                }

                string message = $"Trafienie krytyczne jest ignorowane, ale uszkodzono {selectedArmor.Name}.";

                if (selectedArmor.Damage >= selectedArmor.Armor && selectedArmor.Shield == 0 || selectedArmor.Damage >= selectedArmor.Shield && selectedArmor.Shield > 0)
                {
                    message += " <color=red>Ten element pancerza jest już uszkodzony tak bardzo, że stał się bezużyteczny.</color>";
                }
                Debug.Log(message);

                InventoryManager.Instance.CheckForEquippedWeapons(targetStats.GetComponent<Unit>());
                yield break;
            }
        }

        int rollResult = 0;
        if (!GameManager.IsAutoDiceRollingMode && attackerStats.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(attackerStats, "trafienie krytyczne", result => rollResult = result));
            if (rollResult == 0) yield break;
        }
        else
        {
            rollResult = UnityEngine.Random.Range(1, 101);
        }

        // Uwzględnienie talentu Morderczy Atak
        if (attackerStats.StrikeToInjure)
        {
            int extraRoll = UnityEngine.Random.Range(1, 101);
            if (extraRoll > rollResult) rollResult = extraRoll;
        }

        // Uwzględnienie cechy broni "Sieczna"
        if (attackerWeapon != null && attackerWeapon.Slash > 0 && _isTrainedWeaponCategory)
        {
            targetStats.GetComponent<Unit>().Bleeding++;
            Debug.Log($"<color=#FF7F50>Atak bronią sieczną powoduje krwawienie u {targetStats.Name}. Możesz wydać {attackerWeapon.Slash} punkt/y przewagi, aby zwiększyć o 1 poziom tego krwawienia.</color>");
        }

        int modifier = targetStats.TempHealth < 0 ? Math.Abs(targetStats.TempHealth * 10) : 0;
        string modifierString = modifier != 0 ? $" Modyfikator: {modifier}." : "";
        int extraWounds = 0;
        Debug.Log($"Wynik rzutu na trafienie krytyczne: {rollResult}.{modifierString} {targetStats.Name} otrzymuje trafienie krytyczne w {TranslateHitLocation(hitLocation)} o wartości <color=red>{rollResult + modifier}</color>");

        switch (hitLocation)
        {
            case "head":
                switch (rollResult + modifier)
                {
                    case int n when (n >= 1 && n <= 3):
                        extraWounds = 0; break;
                    case int n when (n >= 4 && n <= 6):
                        extraWounds = 1; break;
                    case int n when (n >= 7 && n <= 9):
                        extraWounds = 1; break;
                    case int n when (n >= 10 && n <= 15):
                        extraWounds = 1; break;
                    case int n when (n >= 16 && n <= 20):
                        extraWounds = 1; break;
                    case int n when (n >= 21 && n <= 25):
                        extraWounds = 1; break;
                    case int n when (n >= 26 && n <= 30):
                        extraWounds = 1; break;
                    case int n when (n >= 31 && n <= 35):
                        extraWounds = 2; break;
                    case int n when (n >= 36 && n <= 40):
                        extraWounds = 2; break;
                    case int n when (n >= 41 && n <= 45):
                        extraWounds = 2; break;
                    case int n when (n >= 46 && n <= 50):
                        extraWounds = 3; break;
                    case int n when (n >= 51 && n <= 55):
                        extraWounds = 3; break;
                    case int n when (n >= 56 && n <= 60):
                        extraWounds = 3; break;
                    case int n when (n >= 61 && n <= 65):
                        extraWounds = 4; break;
                    case int n when (n >= 66 && n <= 75):
                        extraWounds = 4; break;
                    case int n when (n >= 76 && n <= 80):
                        extraWounds = 4; break;
                    case int n when (n >= 81 && n <= 85):
                        extraWounds = 5; break;
                    case int n when (n >= 86 && n <= 94):
                        extraWounds = 5; break;
                    case int n when (n >= 95 && n <= 99):
                        extraWounds = 5; break;
                    case int n when (n >= 100):
                        extraWounds = -1; break; // Śmierć
                }
                break;

            case "leftArm" or "rightArm":
                switch (rollResult + modifier)
                {
                    case int n when (n >= 1 && n <= 10):
                        extraWounds = 0; break;
                    case int n when (n >= 11 && n <= 20):
                        extraWounds = 0; break;
                    case int n when (n >= 21 && n <= 25):
                        extraWounds = 1; break;
                    case int n when (n >= 26 && n <= 40):
                        extraWounds = 1; break;
                    case int n when (n >= 41 && n <= 45):
                        extraWounds = 1; break;
                    case int n when (n >= 46 && n <= 50):
                        extraWounds = 1; break;
                    case int n when (n >= 51 && n <= 55):
                        extraWounds = 2; break;
                    case int n when (n >= 56 && n <= 60):
                        extraWounds = 2; break;
                    case int n when (n >= 61 && n <= 75):
                        extraWounds = 2; break;
                    case int n when (n >= 76 && n <= 80):
                        extraWounds = 2; break;
                    case int n when (n >= 81 && n <= 85):
                        extraWounds = 3; break;
                    case int n when (n >= 86 && n <= 90):
                        extraWounds = 3; break;
                    case int n when (n >= 91 && n <= 95):
                        extraWounds = 3; break;
                    case int n when (n >= 96 && n <= 109):
                        extraWounds = 4; break;
                    case int n when (n >= 110 && n <= 115):
                        extraWounds = 4; break;
                    case int n when (n >= 116 && n <= 120):
                        extraWounds = 4; break;
                    case int n when (n >= 121 && n <= 125):
                        extraWounds = 5; break;
                    case int n when (n >= 126 && n <= 130):
                        extraWounds = 5; break;
                    case int n when (n >= 131 && n <= 135):
                        extraWounds = 5; break;
                    case int n when (n >= 136):
                        extraWounds = -1; break; // Śmierć
                }
                break;

            case "torso":
                switch (rollResult + modifier)
                {
                    case int n when (n >= 1 && n <= 10):
                        extraWounds = 0; break;
                    case int n when (n >= 11 && n <= 20):
                        extraWounds = 1; break;
                    case int n when (n >= 21 && n <= 25):
                        extraWounds = 1; break;
                    case int n when (n >= 26 && n <= 30):
                        extraWounds = 1; break;
                    case int n when (n >= 31 && n <= 35):
                        extraWounds = 2; break;
                    case int n when (n >= 36 && n <= 40):
                        extraWounds = 2; break;
                    case int n when (n >= 41 && n <= 45):
                        extraWounds = 2; break;
                    case int n when (n >= 46 && n <= 50):
                        extraWounds = 2; break;
                    case int n when (n >= 51 && n <= 55):
                        extraWounds = 3; break;
                    case int n when (n >= 56 && n <= 60):
                        extraWounds = 3; break;
                    case int n when (n >= 61 && n <= 65):
                        extraWounds = 3; break;
                    case int n when (n >= 66 && n <= 70):
                        extraWounds = 3; break;
                    case int n when (n >= 71 && n <= 75):
                        extraWounds = 4; break;
                    case int n when (n >= 76 && n <= 80):
                        extraWounds = 4; break;
                    case int n when (n >= 81 && n <= 85):
                        extraWounds = 4; break;
                    case int n when (n >= 86 && n <= 90):
                        extraWounds = 4; break;
                    case int n when (n >= 91 && n <= 95):
                        extraWounds = 5; break;
                    case int n when (n >= 96 && n <= 110):
                        extraWounds = 5; break;
                    case int n when (n >= 111 && n <= 115):
                        extraWounds = 5; break;
                    case int n when (n >= 116):
                        extraWounds = -1; break; // Śmierć
                }
                break;

            case "leftLeg" or "rightLeg":
                switch (rollResult + modifier)
                {
                    case int n when (n >= 1 && n <= 10):
                        extraWounds = 0; break;
                    case int n when (n >= 11 && n <= 20):
                        extraWounds = 0; break;
                    case int n when (n >= 21 && n <= 25):
                        extraWounds = 1; break;
                    case int n when (n >= 26 && n <= 40):
                        extraWounds = 1; break;
                    case int n when (n >= 41 && n <= 45):
                        extraWounds = 1; break;
                    case int n when (n >= 46 && n <= 50):
                        extraWounds = 1; break;
                    case int n when (n >= 51 && n <= 55):
                        extraWounds = 2; break;
                    case int n when (n >= 56 && n <= 60):
                        extraWounds = 2; break;
                    case int n when (n >= 61 && n <= 65):
                        extraWounds = 2; break;
                    case int n when (n >= 66 && n <= 70):
                        extraWounds = 2; break;
                    case int n when (n >= 71 && n <= 75):
                        extraWounds = 3; break;
                    case int n when (n >= 76 && n <= 80):
                        extraWounds = 3; break;
                    case int n when (n >= 81 && n <= 85):
                        extraWounds = 3; break;
                    case int n when (n >= 86 && n <= 90):
                        extraWounds = 4; break;
                    case int n when (n >= 91 && n <= 95):
                        extraWounds = 4; break;
                    case int n when (n >= 96 && n <= 105):
                        extraWounds = 4; break;
                    case int n when (n >= 106 && n <= 115):
                        extraWounds = 5; break;
                    case int n when (n >= 116 && n <= 120):
                        extraWounds = 5; break;
                    case int n when (n >= 121 && n <= 125):
                        extraWounds = 5; break;
                    case int n when (n >= 126):
                        extraWounds = -1; break; // Śmierć
                }
                break;
        }

        if (extraWounds > 0)
        {
            // Zadanie dodatkowych obrażeń
            targetStats.TempHealth -= extraWounds;

            Debug.Log($"{targetStats.Name} otrzymuje {extraWounds} obrażeń w wyniku trafienia krytycznego.");

            // Zwiększenie ilości ran krytycznych
            targetStats.CriticalWounds++;
            UnitsManager.Instance.UpdateUnitPanel(Unit.SelectedUnit);
        }

        // Obsługa śmierci
        if (targetStats.CriticalWounds > targetStats.Wt / 10 || extraWounds == -1)
        {
            string deathMessage = extraWounds == -1 ? $"Trafienie krytyczne powoduje natychmiastową śmierć {targetStats.Name}." : $"Ilość ran krytycznych {targetStats.Name} przekroczyła bonus z wytrzymałości.";
            Debug.Log($"<color=red>{deathMessage} Jednostka umiera.</color>");

            if (GameManager.IsAutoKillMode)
            {
                HandleDeath(targetStats, targetStats.gameObject, attackerStats);
            }
        }
        else if (targetStats != null && targetStats.gameObject != null && targetStats.TempHealth < 0)
        {
            // Jeśli jednostka nie umarła, ale jej żywotność spadła poniżej 0 – ustaw na 0.
            targetStats.TempHealth = 0;
        }

        if(targetStats != null)
        {
            targetStats.GetComponent<Unit>().DisplayUnitHealthPoints();
        }
    }

    public void CriticalDeflectionButtonClick(string value)
    {
        _criticalDeflection = value;
    }
    #endregion

    #region Check for attack localization and return armor value
    public IEnumerator OpenHitLocationPanel()
    {
        float holdTime = 0.3f;
        float elapsedTime = 0f;

        while (Input.GetMouseButton(1))
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= holdTime)
            {
                _selectHitLocationPanel.SetActive(true);
                break;
            }
            yield return null; // Poczekaj do następnej klatki
        }
    }

    public void SelectHitLocation(string hitLocation)
    {
        HitLocation = hitLocation;
    }

    // Metoda określająca miejsce trafienia
    public string DetermineHitLocation(int rollResult = 0)
    {
        int attackLocalization;
        if (rollResult == 0) // Sytuacja, która ma miejsce w przypadku trafień krytycznych. Wtedy rzut na lokalizację jest ustalany losowo.
        {
            attackLocalization = UnityEngine.Random.Range(1, 101);
        }
        else // Wynikiem rzutu na lokalizacje jest odwrócony wynik na trafienie
        {
            // Konwersja liczby na string i odwrócenie
            string reversedString = new string(rollResult.ToString().Reverse().ToArray());

            // Jeśli liczba była jednocyfrowa, dodajemy "0" na końcu
            if (rollResult < 10)
            {
                reversedString += "0";
            }

            attackLocalization = int.Parse(reversedString);
        }

        string hitLocation = attackLocalization switch
        {
            >= 1 and <= 9 => "head",
            >= 10 and <= 24 => "leftArm",
            >= 25 and <= 44 => "rightArm",
            >= 45 and <= 79 => "torso",
            >= 80 and <= 89 => "leftLeg",
            >= 90 and <= 100 => "rightLeg",
            _ => ""
        };

        // Wywołujemy logowanie
        Debug.Log($"Atak jest skierowany w {TranslateHitLocation(hitLocation)}.");

        return hitLocation;
    }

    public string NormalizeHitLocation(string hitLocation)
    {
        // Normalizujemy lokalizację trafienia
        string normalizedHitLocation = hitLocation switch
        {
            "rightArm" or "leftArm" => "arms",
            "rightLeg" or "leftLeg" => "legs",
            _ => hitLocation
        };

        return normalizedHitLocation;
    }

    // Metoda do logowania trafienia po polsku
    private string TranslateHitLocation(string hitLocation)
    {
        string message = hitLocation switch
        {
            "head" => "głowę",
            "leftArm" => "lewą rękę",
            "rightArm" => "prawą rękę",
            "torso" => "korpus",
            "leftLeg" => "lewą nogę",
            "rightLeg" => "prawą nogę",
            _ => "nieznaną lokalizację trafienia"
        };

        return message;
    }

    public int CalculateArmor(Stats attackerStats, Stats targetStats, string hitLocation, int attackRollResult, Weapon attackerWeapon = null)
    {
        string normalizedHitLocation = NormalizeHitLocation(hitLocation);

        int armor = normalizedHitLocation switch
        {
            "head" => targetStats.Armor_head,
            "arms" => targetStats.Armor_arms,
            "torso" => targetStats.Armor_torso,
            "legs" => targetStats.Armor_legs,
            _ => 0
        };

        Inventory inventory = targetStats.GetComponent<Inventory>();

        //Podwaja wartość zbroi w przypadku walki przy użyciu broni Tępej
        if (attackerWeapon != null && attackerWeapon.Undamaging) armor *= 2;

        //Zwiększenie pancerza, jeśli cel ataku posiada tarcze i użył parowania podczas obrony w tym ataku
        if (_parryOrDodge == "parry")
        {
            int shieldValue = inventory.EquippedWeapons
                .Where(weapon => weapon != null && weapon.Shield > 0)
                .Select(weapon => Mathf.Max(weapon.Shield - weapon.Damage, 0)) // Nie obniża cechy Shield poniżej 0
                .FirstOrDefault();

            armor += shieldValue; // Dodaje wartość tarczy do sumarycznej zbroi
        }

        // Pobranie pancerza dla trafionej lokalizacji
        List<Weapon> armorByLocation = inventory.ArmorByLocation.ContainsKey(normalizedHitLocation) ? inventory.ArmorByLocation[normalizedHitLocation] : new List<Weapon>();

        // Sprawdza, czy trafienie jest trafieniem krytycznym
        bool isCriticalHit = DiceRollManager.Instance.IsDoubleDigit(attackRollResult);

        // Uwzględnienie cechy pancerza "Częściowy"
        if (isCriticalHit || attackRollResult % 2 == 0)
        {
            foreach (Weapon armorItem in armorByLocation)
            {
                if (armorItem.Partial)
                {
                    armor -= armorItem.Armor;
                }
            }
        }

        // Uwzględnienie cechy pancerza "Wrażliwe punkty"
        if (isCriticalHit && attackerWeapon != null && attackerWeapon.Impale)
        {
            foreach (Weapon armorItem in armorByLocation)
            {
                if (armorItem.WeakPoints)
                {
                    armor -= armorItem.Armor;
                }
            }
        }

        // Sprawdzenie, czy żadna część pancerza nie jest metalowa
        bool noMetalArmor = armorByLocation.All(weapon => weapon.Category == "leather");

        if (attackerWeapon != null && attackerWeapon.Penetrating && noMetalArmor && _isTrainedWeaponCategory)
        {
            armor = 0;
            Debug.Log("Broń przekłuwająca całkowicie ignoruje niemetalowy pancerz.");
        }
        else if (attackerWeapon != null && attackerWeapon.Penetrating && armor > 0 && _isTrainedWeaponCategory)
        {
            armor--;
        }

        Weapon shield = null;
        //Sprawdza, czy jednostka broniąca się posiada tarcze i użyła parowania. Jeśli tak dodaje ją do listy broni dla danej lokacji
        if (_parryOrDodge == "parry" && inventory.EquippedWeapons.Any(weapon => weapon != null && weapon.Shield > 0))
        {
            shield = inventory.EquippedWeapons.FirstOrDefault(weapon => weapon != null && weapon.Shield > 0 && weapon.Shield - weapon.Damage > 0);
        }

        // Element pancerza, który został trafiony
        Weapon primaryArmor = armorByLocation
             .FirstOrDefault(weapon => (weapon.Category == "plate") && weapon.Armor - weapon.Damage > 0) // Najpierw szuka zbroi płytowej lub tarczy, ale tylko jeśli Armor > 0 (inaczej oznacza, że pancerz jest już całkowicie zniszczony)
             ?? armorByLocation.FirstOrDefault(weapon => weapon.Category == "chain" && weapon.Armor - weapon.Damage > 0) // Potem zbroja kolcza, ale tylko jeśli Armor > 0
             ?? armorByLocation.FirstOrDefault(weapon => weapon.Category == "leather" && weapon.Armor - weapon.Damage > 0); // Na końcu zbroja skórzana, ale tylko jeśli Armor > 0

        // Tworzy tablicę potencjalnie trafionych elementów zbroi (jeden element + ewentualnie tarcza)
        Weapon[] armorAndShield = new Weapon[] { primaryArmor, shield }.Where(w => w != null).ToArray();

        // Losuje trafiony element, jeśli są dwa różne do wyboru
        Weapon selectedArmor = (armorAndShield.Length == 2) ? armorAndShield[UnityEngine.Random.Range(0, 2)] : armorAndShield.FirstOrDefault();

        //Uwzględnienie broni Rąbiącej
        if (attackerWeapon != null && attackerWeapon.Hack && _isTrainedWeaponCategory)
        {
            if (selectedArmor != null)
            {
                if (selectedArmor.Durable == 0) // Uwzględnienie cechy Wytrzymały
                {
                    selectedArmor.Damage++;
                }
                else
                {
                    selectedArmor.Durable--;
                }

                string message = $"Broń rąbiąca uszkadza {selectedArmor.Name}.";

                if (selectedArmor.Damage >= selectedArmor.Armor && selectedArmor.Shield == 0 || selectedArmor.Damage >= selectedArmor.Shield && selectedArmor.Shield > 0)
                {
                    message += " <color=red>Ten element pancerza jest już uszkodzony tak bardzo, że stał się bezużyteczny.</color>";
                }

                Debug.Log(message);
            }
        }

        //Uwzgędnienie talentu Strzał Przebijający
        if (attackerWeapon != null && attackerWeapon.Type.Contains("ranged") && attackerStats.SureShot > 0)
        {
            armor = Math.Max(0, armor - attackerStats.SureShot);
        }

        //Uwzgędnienie cechy pancerza "Tandetny"
        if (selectedArmor != null && selectedArmor.Shoddy && isCriticalHit)
        {
            selectedArmor.Damage = selectedArmor.Armor;
            Debug.Log($"<color=red>{selectedArmor.Name} ulega zniszczeniu ze względu na cechę \"Tandetny\".</color>");
        }

        return armor;
    }

    public IEnumerator SelectRiderOrMount(Unit unit)
    {
        _riderOrMountPanel.SetActive(true);

        // Najpierw czekamy, aż gracz kliknie którykolwiek przycisk
        yield return new WaitUntil(() => !_riderOrMountPanel.activeSelf);

        if (Unit.SelectedUnit == null) yield break;

        if (_riderOrMount == "rider")
        {
            Attack(Unit.SelectedUnit.GetComponent<Unit>(), unit, false);
        }
        else if (_riderOrMount == "mount")
        {
            Attack(Unit.SelectedUnit.GetComponent<Unit>(), unit.Mount, false);
        }
    }

    public void RiderOrMountButtonClick(string riderOrmount)
    {
        _riderOrMount = riderOrmount;
    }
    #endregion

    #region Charge
    public void Charge(GameObject attacker, GameObject target)
    {
        //Sprawdza pole, w którym atakujący zatrzyma się po wykonaniu szarży
        GameObject targetTile = GetTileAdjacentToTarget(attacker, target);

        Stats attackerStats = attacker.GetComponent<Stats>();
        Stats targetStats = target.GetComponent<Stats>();

        Vector2 targetTilePosition;

        if (targetTile != null)
        {
            targetTilePosition = new Vector2(targetTile.transform.position.x, targetTile.transform.position.y);
        }
        else
        {
            Debug.Log($"Cel ataku stoi poza zasięgiem szarży.");
            return;
        }

        if (attacker.GetComponent<Unit>().Prone)
        {
            Debug.Log("Jednostka w stanie powalenia nie może wykonywać szarży.");
            return;
        }

        //Ścieżka ruchu szarżującego
        List<Vector2> path = MovementManager.Instance.FindPath(attacker.transform.position, targetTilePosition);

        //Sprawdza, czy postać jest wystarczająco daleko do wykonania szarży
        if (path.Count >= attackerStats.Sz / 2f && path.Count <= attackerStats.TempSz)
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

                if (attacker == null || target == null) yield break;

                //Uwzględnienie talentu Atak wyprzedzający
                if (targetStats.ReactionStrikesLeft > 0)
                {
                    Debug.Log($"Pozostałe Ataki Wyprzedzające: {targetStats.ReactionStrikesLeft}");

                    int initiativeRoll = 0;
                    if (!GameManager.IsAutoDiceRollingMode && target.CompareTag("PlayerUnit"))
                    {
                        yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(target.GetComponent<Stats>(), "inicjatywę", result => initiativeRoll = result));
                        if (initiativeRoll == 0) yield break;
                    }
                    else
                    {
                        initiativeRoll = UnityEngine.Random.Range(1, 101);
                    }

                    int[] initiativeTest = DiceRollManager.Instance.TestSkill("I", targetStats, null, 0, initiativeRoll);

                    if (initiativeTest[0] > 0)
                    {
                        targetStats.ReactionStrikesLeft--;
                        ChangeAttackType();

                        // Czekanie na zakończenie ataku reakcyjnego przed kontynuowaniem szarży
                        yield return StartCoroutine(AttackCoroutine(target.GetComponent<Unit>(), attacker.GetComponent<Unit>(), false, false, true));

                        if (attacker == null) yield break;
                        attacker.GetComponent<Unit>().IsCharging = true;
                    }
                }

                yield return StartCoroutine(AttackCoroutine(attacker.GetComponent<Unit>(), target.GetComponent<Unit>()));
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
        if (target == null) return null;

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

            if (path.Count == 0) continue;

            // Aktualizuje najkrótszą drogę
            if (path.Count < shortestPathLength)
            {
                shortestPathLength = path.Count;
                targetTile = tile;
            }
        }

        if (shortestPathLength > attacker.GetComponent<Stats>().TempSz && !GameManager.IsAutoCombatMode)
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
        // Sprawdzamy, czy atakujący może wykonać akcję
        if (!attacker.CanDoAction)
        {
            Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
            return;
        }

        // Sprawdzamy, czy mamy sytuację grapplingu (czy jedna z jednostek już jest w kontakcie)
        if (attacker.EntangledUnitId == target.UnitId || attacker.UnitId == target.EntangledUnitId)
        {
            StartCoroutine(GrapplingActionCoroutine(attacker, target));
        }
        else
        {
            // Jeśli nie ma grapplingu, wykonujemy zwykły atak
            Attack(attacker, target);
        }
    }

    private IEnumerator GrapplingActionCoroutine(Unit attacker, Unit target)
    {
        // Ustalamy role:
        // Jeśli attacker.EntangledUnitId == target.UnitId => attacker jest chwytającym (grappler)
        // Jeśli attacker.UnitId == target.EntangledUnitId => attacker jest pochwyconym (grappled)
        bool isGrappler = (attacker.EntangledUnitId == target.UnitId);
        bool isGrappled = (attacker.UnitId == target.EntangledUnitId);

        //// Jeśli obie relacje są prawdziwe, traktujemy jednostkę jako pochwyconą.
        //if (isGrappler && isGrappled)
        //{
        //    isGrappler = false;
        //}

        // Jeśli żadna z relacji nie występuje – wychodzimy i wykonujemy atak
        if (!isGrappler && !isGrappled)
        {
            Attack(attacker, target);
            yield break;
        }

        // Pobieramy statystyki obu jednostek
        Stats attackerStats = attacker.Stats;
        Stats targetStats = target.Stats;

        if (targetStats.Size - attackerStats.Size > 1)
        {
            Debug.Log($"<color=#FF7F50>{attackerStats.Name} nie jest w stanie pochwycić {targetStats.Name}. Cel jest zbyt duży.</color>");
            yield break;
        }

        // Ustawiamy widoczność przycisków w panelu wyboru akcji:
        // Załóżmy, że masz osobne przyciski: _attackGrappleButton, _improveGrappleButton, _escapeGrappleButton
        if (isGrappler)
        {
            // Jako chwytający, attacker może:
            // - Atakować (próba przejęcia kontroli nad celem, jeśli jeszcze go nie trzyma)
            // - Poprawić chwyt
            // - Uciec – opcja dostępna tylko, gdy attacker sam ma Entangled > 0
            _improveGrappleButton.interactable = true;
            _escapeGrappleButton.interactable = attacker.Entangled > 0;
        }
        else if (isGrappled)
        {
            // Jako pochwycony, attacker może:
            // - Atakować (próbując odwrócić sytuację, czyli „pochwycić” osobę chwytającą)
            // - Uciekać (opcję escape pokazujemy zawsze)
            _improveGrappleButton.interactable = false;
            _escapeGrappleButton.interactable = true;
        }

        // Wyświetlenie panelu akcji i oczekiwanie na wybór gracza
        _grapplingActionChoice = "";
        _grapplingActionPanel.SetActive(true);
        yield return new WaitUntil(() => !_grapplingActionPanel.activeSelf);

        switch (_grapplingActionChoice)
        {
            case "attack":
                Debug.Log($"{attackerStats.Name} decyduje się na atak (w ramach zapasów).");
                Attack(attacker, target);
                break;

            case "improve":
                Debug.Log($"{attackerStats.Name} próbuje poprawić chwyt na {targetStats.Name}.");
                RoundsManager.Instance.DoAction(attacker);

                // Każda jednostka wykonuje swój oddzielny rzut
                int attackerRoll = 0;
                if (!GameManager.IsAutoDiceRollingMode && attacker.CompareTag("PlayerUnit"))
                {
                    yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(attackerStats, "siłę", result => attackerRoll = result));
                    if (attackerRoll == 0) yield break;
                }
                else
                {
                    attackerRoll = UnityEngine.Random.Range(1, 101);
                }

                int targetRoll = 0;
                if (!GameManager.IsAutoDiceRollingMode && target.CompareTag("PlayerUnit"))
                {
                    yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, "siłę", result => targetRoll = result));
                    if (targetRoll == 0) yield break;
                }
                else
                {
                    targetRoll = UnityEngine.Random.Range(1, 101);
                }

                // Przeciwstawny test siły – osobno dla obu jednostek
                int[] attackerTest = DiceRollManager.Instance.TestSkill("S", attackerStats, null, 0, attackerRoll);
                int[] targetTest = DiceRollManager.Instance.TestSkill("S", targetStats, null, 0, targetRoll);
                int attackerSuccessValue = attackerTest[0];
                int targetSuccessValue = targetTest[0];
                int attackerSuccessLevel = attackerTest[1];
                int targetSuccessLevel = targetTest[1];


                if (targetStats.Size - attackerStats.Size == 1 && !DiceRollManager.Instance.IsDoubleDigit(attackerRoll))
                {
                    Debug.Log($"<color=#FF7F50>{attackerStats.Name} nie jest w stanie poprawić chwytu na {targetStats.Name}. Aby wygrać przeciwstawny test siły z większym przeciwnikiem, należy wyrzucić fuksa.</color>");
                    yield break;
                }

                if (attackerSuccessLevel > targetSuccessLevel)
                {
                    // Udało się – poprawiamy chwyt: zwiększamy poziom pochwycenia celu
                    StatesManager.Instance.Entangled(target, 1);
                    InitiativeQueueManager.Instance.CalculateAdvantage(attacker.tag, 1);
                    Debug.Log($"<color=#FF7F50>{attackerStats.Name} poprawia chwyt na {targetStats.Name}. Poziom pochwycenia wzrasta o 1 i wynosi {target.Entangled}.</color>");
                }
                else
                {
                    Debug.Log($"<color=#FF7F50>{attackerStats.Name} nie udaje się poprawić chwytu na {targetStats.Name}. Pochwycenie pozostaje na poziomie {target.Entangled}.</color>");
                }
                break;

            case "escape":
                Debug.Log($"{attackerStats.Name} próbuje uciec z pochwycenia.");
                RoundsManager.Instance.DoAction(attacker);

                // W przypadku ucieczki priorytetowo traktujemy jednostkę jako pochwyconą, jeśli taka relacja występuje
                if (isGrappled)
                {
                    // attacker jest pochwycony – więc przekazujemy: entanglingUnit = target, entangledUnit = attacker
                    yield return StartCoroutine(EscapeFromEntanglement(targetStats, attackerStats));
                }
                else if (isGrappler)
                {
                    // attacker jest chwytający – przekazujemy: entanglingUnit = attacker, entangledUnit = target
                    yield return StartCoroutine(EscapeFromEntanglement(attackerStats, targetStats));
                }
                break;

            default:
                yield break;
        }
    }

    private void GrapplingActionButtonClick(string action)
    {
        _grapplingActionChoice = action;
    }
    public IEnumerator EscapeFromEntanglement(Stats entanglingUnitStats, Stats entangledUnitStats)
    {
        Unit entangledUnit = entangledUnitStats.GetComponent<Unit>();
        Unit entanglingUnit = entanglingUnitStats.GetComponent<Unit>();

        if (entangledUnit.Entangled > 0)
        {
            int targetRoll = 0;
            int attackerRoll = 0;

            // Dla pochwyconego: jeśli to gracz, pobieramy ręczny wynik, w przeciwnym razie losujemy
            if (!GameManager.IsAutoDiceRollingMode && entangledUnit.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(entangledUnitStats, "siłę", result => targetRoll = result));
                if (targetRoll == 0) yield break;
            }
            else
            {
                targetRoll = UnityEngine.Random.Range(1, 101);
            }

            // Dla pochwytującego: sprawdzamy, czy ma tag "PlayerUnit"
            if (!GameManager.IsAutoDiceRollingMode && entanglingUnit.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(entanglingUnitStats, "siłę", result => attackerRoll = result));
                if (attackerRoll == 0) yield break;
            }
            else
            {
                attackerRoll = UnityEngine.Random.Range(1, 101);
            }

            // Wywołanie testów siły – przekazujemy wynik rzutu tylko dla jednostki, która była manualna
            int[] targetTest = DiceRollManager.Instance.TestSkill("S", entangledUnitStats, null, 0, targetRoll);
            int[] attackerTest = DiceRollManager.Instance.TestSkill("S", entanglingUnitStats, null, 0, attackerRoll);

            int targetSuccessValue = targetTest[0];
            int attackerSuccessValue = attackerTest[0];

            int targetSuccessLevel = targetTest[1];
            int attackerSuccessLevel = attackerTest[1];

            if (entanglingUnitStats.Size - entangledUnitStats.Size > 0 && !DiceRollManager.Instance.IsDoubleDigit(targetRoll))
            {
                Debug.Log($"<color=#FF7F50>{entangledUnitStats.Name} nie jest w stanie uwolnić się z pochwycenia przez {entanglingUnitStats.Name}. Aby wygrać przeciwstawny test siły z większym przeciwnikiem, należy wyrzucić fuksa.</color>");
                yield break;
            }

            if (targetSuccessValue > attackerSuccessValue)
            {
                // Zmniejszamy poziom pochwycenia o różnicę poziomów + 1
                entangledUnit.Entangled = Mathf.Max(0, entangledUnit.Entangled - (targetSuccessLevel - attackerSuccessLevel + 1));

                if (entangledUnit.Entangled == 0)
                {
                    entanglingUnit.EntangledUnitId = 0;
                    Debug.Log($"<color=#FF7F50>{entangledUnitStats.Name} uwolnił się z pochwycenia przez {entanglingUnitStats.Name}.</color>");
                }
                else
                {
                    Debug.Log($"<color=#FF7F50>{entangledUnitStats.Name} próbuje się uwolnić z pochwycenia przez {entanglingUnitStats.Name}. Poziom pochwycenia maleje o {targetSuccessLevel - attackerSuccessLevel + 1} i wynosi {entangledUnit.Entangled}.</color>");
                }
            }
            else
            {
                Debug.Log($"<color=#FF7F50>{entangledUnitStats.Name} bezskutecznie próbuje się uwolnić z pochwycenia przez {entanglingUnitStats.Name}. Poziom pochwycenia: {entangledUnit.Entangled}.</color>");
            }
        }
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
        if (Unit.SelectedUnit.GetComponent<Unit>().DefensiveBonus > 0)
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

    #region Reloading
    public void Reload()
    {
        StartCoroutine(ReloadCoroutine());
    }
    private IEnumerator ReloadCoroutine()
    {
        if (Unit.SelectedUnit == null) yield break;

        Weapon weapon = Unit.SelectedUnit.GetComponent<Inventory>().EquippedWeapons[0];
        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();

        if (weapon == null || weapon.ReloadLeft == 0)
        {
            Debug.Log($"Wybrana broń nie wymaga ładowania.");
            yield break;
        }

        if (weapon.ReloadLeft > 0)
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
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "broń zasięgową", result => reloadRollResult = result));
                if (reloadRollResult == 0) yield break;
            }
            else
            {
                reloadRollResult = UnityEngine.Random.Range(1, 101);
            }

            //Test Broni Zasięgowej danej kategorii
            int successLevel = DiceRollManager.Instance.TestSkill("US", stats, rangedSkill.ToString(), 0, reloadRollResult)[1];
            if (successLevel > 0)
            {
                weapon.ReloadLeft = Mathf.Max(0, weapon.ReloadLeft - successLevel);

            }

            StartCoroutine(AnimationManager.Instance.PlayAnimation("reload", Unit.SelectedUnit));
        }

        if (weapon.ReloadLeft == 0)
        {
            Debug.Log($"Broń {stats.Name} załadowana.");

            SetActionsButtonsInteractable();

            if(stats.RapidReload > 0 || (stats.Gunner > 0 && weapon.Category == "blackpowder"))
            {
                // Zaktualizowanie przewagi (ładowanie jest uznawane jako akcja Oceny Sytuacji)
                InitiativeQueueManager.Instance.CalculateAdvantage(stats.gameObject.tag, 3);
            }
        }
        else
        {
            Debug.Log($"{stats.Name} ładuje broń. Pozostał/y {weapon.ReloadLeft} PS do pełnego załadowania.");
        }

        InventoryManager.Instance.DisplayReloadTime();
    }

    private void ResetWeaponLoad(Weapon attackerWeapon, Stats attackerStats)
    {
        if (attackerWeapon.ReloadTime == 0) return;

        //Sprawia, że po ataku należy przeładować broń
        attackerWeapon.ReloadLeft = attackerWeapon.ReloadTime;
        attackerWeapon.WeaponsWithReloadLeft[attackerWeapon.Id] = attackerWeapon.ReloadLeft;

        //Uwzględnia zdolność Błyskawicznego Przeładowania
        if (attackerStats.RapidReload > 0)
        {
            attackerWeapon.ReloadLeft--;
        }

        //Uwzględnia zdolność Artylerzysta
        if (attackerStats.Gunner > 0 && attackerWeapon.Blackpowder)
        {
            attackerWeapon.ReloadLeft -= attackerStats.Gunner;
        }

        //Zapobiega ujemnej wartości czasu przeładowania
        if (attackerWeapon.ReloadLeft <= 0)
        {
            attackerWeapon.ReloadLeft = 0;

            if (attackerStats.RapidReload > 0 || (attackerStats.Gunner > 0 && attackerWeapon.Category == "blackpowder"))
            {
                // Zaktualizowanie przewagi (ładowanie jest uznawane jako akcja Oceny Sytuacji)
                InitiativeQueueManager.Instance.CalculateAdvantage(attackerStats.gameObject.tag, 3);
            }
        }

        InventoryManager.Instance.DisplayReloadTime();
    }
    #endregion

    #region Aiming
    public void SetAim()
    {
        if (Unit.SelectedUnit == null) return;

        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();

        //Sprawdza, czy postać już celuje i chce przestać, czy chce dopiero przycelować
        if (unit.AimingBonus != 0)
        {
            unit.AimingBonus = 0;
        }
        else
        {
            // Sprawdzamy, czy atakujący może wykonać akcję
            if (!unit.CanDoAction)
            {
                Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
                return;
            }

            //Wykonuje akcję
            RoundsManager.Instance.DoAction(Unit.SelectedUnit.GetComponent<Unit>());

            Weapon attackerWeapon = InventoryManager.Instance.ChooseWeaponToAttack(unit.gameObject);
            if (attackerWeapon == null)
            {
                attackerWeapon = unit.GetComponent<Weapon>();
            }

            //Dodaje modyfikator do trafienia uzwględniając strzał mierzony w przypadku ataków dystansowych
            unit.AimingBonus += 20;

            Debug.Log($"{unit.GetComponent<Stats>().Name} przycelowuje.");

            StartCoroutine(AnimationManager.Instance.PlayAnimation("aim", Unit.SelectedUnit));
        }

        UpdateAimButtonColor();
    }
    public void UpdateAimButtonColor()
    {
        if (Unit.SelectedUnit != null && Unit.SelectedUnit.GetComponent<Unit>().AimingBonus != 0)
        {
            _aimButton.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Color.green;
        }
        else
        {
            _aimButton.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Color.white;
        }
    }
    #endregion

    #region Opportunity attack
    // Sprawdza czy ruch powoduje atak okazyjny
    public void CheckForOpportunityAttack(GameObject movingUnit, Vector2 selectedTilePosition)
    {
        //Przy bezpiecznym odwrocie nie występuje atak okazyjny.
        if (Unit.SelectedUnit != null && Unit.SelectedUnit.GetComponent<Unit>().IsRetreating) return;

        List<Unit> adjacentOpponents = AdjacentOpponents(movingUnit.transform.position, movingUnit.tag);

        if (adjacentOpponents.Count == 0) return;

        // Atak okazyjny wywolywany dla kazdego wroga bedacego w zwarciu z bohaterem gracza
        foreach (Unit unit in adjacentOpponents)
        {
            Weapon weapon = InventoryManager.Instance.ChooseWeaponToAttack(unit.gameObject);

            //Jeżeli jest to jednostka unieruchomiona, nieprzytomna, w panice, mniejsza rozmiarem lub jednostka z bronią dystansową to ją pomijamy
            if (weapon.Type.Contains("ranged") || unit.Unconscious || unit.EntangledUnitId != 0 || unit.Entangled > 0 || unit.Broken > 0 || unit.GetComponent<Stats>().Size < movingUnit.GetComponent<Stats>().Size) continue;

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

    #region Feint
    public IEnumerator Feint(Stats attackerStats, Stats targetStats, Weapon targetWeapon)
    {
        Unit attackerUnit = attackerStats.GetComponent<Unit>();
        Unit targetUnit = targetStats.GetComponent<Unit>();

        //Wykonuje akcję
        RoundsManager.Instance.DoAction(attackerUnit);

        int targetRoll = 0;
        int attackerRoll = 0;

        // Dla atakowanego
        if (!GameManager.IsAutoDiceRollingMode && targetUnit.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, "walkę bronią białą", result => targetRoll = result));
            if (targetRoll == 0) yield break;
        }
        else
        {
            targetRoll = UnityEngine.Random.Range(1, 101);
        }

        // Dla atakującego
        if (!GameManager.IsAutoDiceRollingMode && attackerUnit.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(attackerStats, "walkę bronią szermierczą", result => attackerRoll = result));
            if (attackerRoll == 0) yield break;
        }
        else
        {
            attackerRoll = UnityEngine.Random.Range(1, 101);
        }

        MeleeCategory targetMeleeSkill = EnumConverter.ParseEnum<MeleeCategory>(targetWeapon.Category) ?? MeleeCategory.Basic;

        // Wywołanie testów – przekazujemy wynik rzutu tylko dla jednostki, która była manualna
        int[] targetTest = DiceRollManager.Instance.TestSkill("WW", targetStats, targetMeleeSkill.ToString(), 0, targetRoll);
        int[] attackerTest = DiceRollManager.Instance.TestSkill("WW", attackerStats, "Fencing", 0, attackerRoll);
        
        // Talent Finta
        if (attackerTest[0] >= 0 && attackerStats.Feint > 0)
        {
            attackerTest[1] += attackerStats.Feint;
            Debug.Log($"Poziom sukcesu {attackerStats.Name} wzrasta do <color=green>{attackerTest[1]}</color> za talent \"Finta.\"");
        }

        int targetSuccessLevel = targetTest[1];
        int attackerSuccessLevel = attackerTest[1];

        if (attackerSuccessLevel > targetSuccessLevel)
        {
            attackerUnit.FeintModifier = Math.Min(60, (attackerSuccessLevel - targetSuccessLevel) * 10);
            attackerUnit.FeintedUnitId = targetUnit.UnitId;

            Debug.Log($"Finta powiodła się. Następny atak {attackerStats.Name} przeciwko {targetStats.Name} będzie wykonywany z modyfikatorem <color=green>{attackerUnit.FeintModifier}</color>");
        }
        else
        {
            Debug.Log($"Finta nie powiodła się.");
        }

        // Resetuje typ ataku
        ChangeAttackType();
    }
    #endregion

    #region Stun
    private IEnumerator Stun(Stats attackerStats, Stats targetStats)
    {
        Unit attackerUnit = attackerStats.GetComponent<Unit>();
        Unit targetUnit = targetStats.GetComponent<Unit>();

        int targetRoll = 0;
        int attackerRoll = 0;

        // Dla atakowanego
        if (!GameManager.IsAutoDiceRollingMode && targetUnit.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, "odporność", result => targetRoll = result));
            if (targetRoll == 0) yield break;
        }
        else
        {
            targetRoll = UnityEngine.Random.Range(1, 101);
        }

        // Dla atakującego
        if (!GameManager.IsAutoDiceRollingMode && attackerUnit.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(attackerStats, "siłę", result => attackerRoll = result));
            if (attackerRoll == 0) yield break;
        }
        else
        {
            attackerRoll = UnityEngine.Random.Range(1, 101);
        }

        // Wywołanie testów – przekazujemy wynik rzutu tylko dla jednostki, która była manualna
        int[] attackerTest = DiceRollManager.Instance.TestSkill("S", attackerStats, null, 0, attackerRoll);
        int[] targetTest = DiceRollManager.Instance.TestSkill("Wt", targetStats, "Endurance", 0, targetRoll);

        // Talent Ogłuszanie
        if (attackerTest[0] >= 0 && attackerStats.StrikeToStun > 0)
        {
            attackerTest[1] += attackerStats.StrikeToStun;
            Debug.Log($"Poziom sukcesu {attackerStats.Name} wzrasta do <color=green>{attackerTest[1]}</color> za talent \"Ogłuszenie.\"");
        }

        // Talent Żelazna Szczęka
        if (targetTest[0] >= 0 && targetStats.IronJaw > 0)
        {
            targetTest[1] += targetStats.IronJaw;
            Debug.Log($"Poziom sukcesu {targetStats.Name} wzrasta do <color=green>{targetTest[1]}</color> za talent \"Źelazna Szczęka.\"");
        }

        int attackerSuccessLevel = attackerTest[1];
        int targetSuccessLevel = targetTest[1];

        // Jeśli ogłuszany posiada talent Żelazna Szczęka, wykonuje dodatkowy rzut obronny przed oszołomieniem
        if (attackerSuccessLevel > targetSuccessLevel && targetStats.IronJaw > 0)
        {
            if (!GameManager.IsAutoDiceRollingMode && targetUnit.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, "odporność", result => targetRoll = result));
                if (targetRoll == 0) yield break;
            }
            else
            {
                targetRoll = UnityEngine.Random.Range(1, 101);
            }

            int[] targetIronJawTest = DiceRollManager.Instance.TestSkill("Wt", targetStats, "Endurance", 0, targetRoll);
            if (targetIronJawTest[1] > 0)
            {
                targetSuccessLevel += targetIronJawTest[1];
                Debug.Log($"<color=#FF7F50>{targetStats.Name} korzysta z talentu Żelazna Szczęka i zmniejsza poziom oszołomienia o {targetIronJawTest[1]}.</color>");
            }
        }

        if (attackerSuccessLevel > targetSuccessLevel)
        {
            Debug.Log($"<color=#FF7F50>Atak {attackerStats.Name} oszołomił {targetStats.Name}.</color>");
            targetUnit.Stunned++;

            //Zresetowanie szału bojowego
            if (targetUnit.IsFrenzy)
            {
                StartCoroutine(FrenzyCoroutine(false, targetUnit));
            }
        }
        else
        {
            Debug.Log($"<color=#FF7F50>Atak {attackerStats.Name} nie dał rady oszołomić {targetStats.Name}.</color>");
        }
    }
    #endregion

    #region Disarm
    private IEnumerator Disarm(Stats attackerStats, Stats targetStats, Weapon attackerWeapon, Weapon targetWeapon)
    {
        if (targetStats.Size > attackerStats.Size || targetWeapon.Type.Contains("natural-weapon"))
        {
            Debug.Log("Nie można rozbrajać jednostek walczących bronią naturalną lub większych od siebie.");
            yield break;
        }

        Unit attackerUnit = attackerStats.GetComponent<Unit>();
        Unit targetUnit = targetStats.GetComponent<Unit>();

        //Wykonuje akcję
        RoundsManager.Instance.DoAction(attackerUnit);

        int targetRoll = 0;
        int attackerRoll = 0;

        // Dla atakowanego
        if (!GameManager.IsAutoDiceRollingMode && targetUnit.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(targetStats, "walkę bronią białą", result => targetRoll = result));
            if (targetRoll == 0) yield break;
        }
        else
        {
            targetRoll = UnityEngine.Random.Range(1, 101);
        }

        // Dla atakującego
        if (!GameManager.IsAutoDiceRollingMode && attackerUnit.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(attackerStats, "walkę bronią białą", result => attackerRoll = result));
            if (attackerRoll == 0) yield break;
        }
        else
        {
            attackerRoll = UnityEngine.Random.Range(1, 101);
        }

        MeleeCategory targetMeleeSkill = EnumConverter.ParseEnum<MeleeCategory>(targetWeapon.Category) ?? MeleeCategory.Basic;
        MeleeCategory attackerMeleeSkill = EnumConverter.ParseEnum<MeleeCategory>(attackerWeapon.Category) ?? MeleeCategory.Basic;

        // Wywołanie testów – przekazujemy wynik rzutu tylko dla jednostki, która była manualna
        int[] attackerTest = DiceRollManager.Instance.TestSkill("WW", attackerStats, attackerMeleeSkill.ToString(), 0, attackerRoll);
        int[] targetTest = DiceRollManager.Instance.TestSkill("WW", targetStats, targetMeleeSkill.ToString(), 0, targetRoll);

        // Talent Rozbrojenie
        if (attackerTest[0] >= 0 && attackerStats.Disarm > 0)
        {
            attackerTest[1] += attackerStats.Disarm;
            Debug.Log($"Poziom sukcesu {attackerStats.Name} wzrasta do <color=green>{attackerTest[1]}</color> za talent \"Rozbrojenie.\"");
        }

        int attackerSuccessLevel = attackerTest[1];
        int targetSuccessLevel = targetTest[1];

        if (attackerSuccessLevel > targetSuccessLevel)
        {
            Debug.Log($"{attackerStats.Name} rozbroił {targetStats.Name}.");
            targetStats.GetComponent<Weapon>().ResetWeapon();

            //Aktualizujemy tablicę dobytych broni
            Weapon[] equippedWeapons = targetStats.GetComponent<Inventory>().EquippedWeapons;
            for (int i = 0; i < equippedWeapons.Length; i++)
            {
                equippedWeapons[i] = null;
            }
        }
        else
        {
            Debug.Log($"{attackerStats.Name} nie dał rady rozbroić {targetStats.Name}.");
        }

        // Zresetowanie ładowania broni dystansowej celu rozbrajania, bo musi się bronić
        if (targetWeapon.ReloadLeft != 0)
        {
            ResetWeaponLoad(targetWeapon, targetStats);
        }

        // Zresetowanie typu ataku
        ChangeAttackType();
    }
    #endregion

    #region Frenzy
    public void Frenzy(bool value)
    {
        StartCoroutine(FrenzyCoroutine(value));
    }
    public IEnumerator FrenzyCoroutine(bool value, Unit unit = null)
    {
        if (Unit.SelectedUnit == null && unit == null) yield break;

        if(unit == null)
        {
            unit = Unit.SelectedUnit.GetComponent<Unit>();
        }

        Stats stats = unit.GetComponent<Stats>();

        if (!unit.CanDoAction)
        {
            Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
            yield break;
        }

        if (unit.IsFrenzy || value == false)
        {
            unit.IsFrenzy = false;
            unit.Fatiqued++;
            stats.FrenzyAttacksLeft = 0;
            Debug.Log($"<color=#FF7F50>Szał bojowy u {stats.Name} zostaje zakończony. Poziom wyczerpania wzrasta o 1.</color>");
            UpdateFrenzyButtonColor();
            yield break;
        }

        //Wykonuje akcję
        RoundsManager.Instance.DoAction(Unit.SelectedUnit.GetComponent<Unit>());

        int rollResult = 0;
        // Jeżeli jesteśmy w trybie manualnych rzutów kośćmi
        if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "siłę woli", result => rollResult = result));
            if (rollResult == 0) yield break;
        }
        else
        {
            rollResult = UnityEngine.Random.Range(1, 101);
        }

        //Test SW
        int successValue = DiceRollManager.Instance.TestSkill("SW", stats, null, 0, rollResult)[0];
        if (successValue >= 0)
        {
            Debug.Log($"<color=#FF7F50>{stats.Name} wprowadza się w szał bojowy. Staje się niewrażliwy/a na strach i grozę.</color>");
            unit.IsFrenzy = true;

            // Jednostka w szale bojowym nie odczuwa strachu
            unit.IsFearTestPassed = true;
            unit.FearedUnits.Clear();
            unit.Broken = 0;
        }
        else
        {
            Debug.Log($"{stats.Name} nie udało się wprowadzić w szał bojowy.");
        }

        UpdateFrenzyButtonColor();
    }

    public void UpdateFrenzyButtonColor()
    {
        if (Unit.SelectedUnit.GetComponent<Unit>().IsFrenzy)
        {
            _frenzyButton.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Color.green;
        }
        else
        {
            _frenzyButton.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Color.white;
        }
    }
    #endregion

    #region Find adjacent units
    public Unit[] GetAdjacentUnits(Vector2 centerPosition, Unit exclude = null)
    {
        List<Unit> adjacentUnits = new List<Unit>();
        Vector2[] directions = {
        Vector2.zero,
        Vector2.right,
        Vector2.left,
        Vector2.up,
        Vector2.down,
        new Vector2(1, 1),
        new Vector2(-1, -1),
        new Vector2(-1, 1),
        new Vector2(1, -1)
    };

        foreach (var dir in directions)
        {
            Vector2 pos = centerPosition + dir;
            Collider2D collider = Physics2D.OverlapPoint(pos);
            if (collider != null)
            {
                Unit unit = collider.GetComponent<Unit>();
                if (unit != null && unit != exclude)
                {
                    adjacentUnits.Add(unit);
                }
            }
        }

        return adjacentUnits.ToArray();
    }

    public void UnitOrGroupButtonClick(string unitOrGroup)
    {
        _groupOfTargetsPenalty = unitOrGroup == "unit";
    }
    #endregion
}
