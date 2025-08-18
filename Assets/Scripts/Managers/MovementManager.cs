using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MovementManager : MonoBehaviour
{
    // Prywatne statyczne pole przechowujące instancję
    private static MovementManager instance;

    // Publiczny dostęp do instancji
    public static MovementManager Instance
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
    [HideInInspector] public bool IsMoving;
    [SerializeField] private Button _runButton;
    [SerializeField] private Button _flightButton;
    [SerializeField] private Button _retreatButton;
    [SerializeField] private Toggle _canMoveToggle;

    [Header("Panel do manualnego zarządzania sposobem odwrotu")]
    [SerializeField] private GameObject _retreatPanel;
    [SerializeField] private UnityEngine.UI.Button _advantageButton;
    [SerializeField] private UnityEngine.UI.Button _dodgeButton;
    private string _retreatWay;

    void Start()
    {
        _dodgeButton.onClick.AddListener(() => RetreatWayButtonClick("dodge"));
        _advantageButton.onClick.AddListener(() => RetreatWayButtonClick("advantage"));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && _retreatPanel.activeSelf)
        {
            Retreat(false);
        }
    }

    private void RetreatWayButtonClick(string retreatWay)
    {
        _retreatWay = retreatWay;
    }

    #region Move functions
    public void MoveSelectedUnit(GameObject selectedTile, GameObject unitGameObject)
    {
        // Nie pozwala wykonać akcji ruchu, dopóki poprzedni ruch nie zostanie zakończony. Sprawdza też, czy gra nie jest wstrzymana (np. poprzez otwarcie dodatkowych paneli)
        if (IsMoving == true || GameManager.IsGamePaused) return;

        Unit unit = unitGameObject.GetComponent<Unit>();

        if (!unit.CanMove && !unit.IsRunning)
        {
            Debug.Log("Ta jednostka nie może wykonać ruchu w tej rundzie.");
            return;
        }
        else if (!unit.CanDoAction && unit.IsRunning)
        {
            Debug.Log("Ta jednostka nie może wykonać biegu w tej rundzie.");
            StartCoroutine(UpdateMovementRange(1));
            return;
        }

        // Jeśli jednostka pochwytująca inną wykonuje ruch, to pochwycenie zostane przerwane. Chyba, że jest ono w zasięgu broni, którą pochwytuje
        if (unit.EntangledUnitId != 0)
        {
            Weapon attackerWeapon = InventoryManager.Instance.ChooseWeaponToAttack(unit.gameObject);
            foreach (var u in UnitsManager.Instance.AllUnits)
            {
                if (u.UnitId == unit.EntangledUnitId && u.Entangled > 0)
                { 
                    if (attackerWeapon != null && attackerWeapon.Category == "entangling")
                    {
                        float effectiveAttackRange = attackerWeapon.AttackRange;

                        if (attackerWeapon.Type.Contains("throwing"))
                        {
                            effectiveAttackRange *= unit.Stats.S / 10f;
                        }

                        float distance = Vector3.Distance(unit.transform.position, selectedTile.transform.position);

                        if (distance > effectiveAttackRange)
                        {
                            CombatManager.Instance.ReleaseEntangledUnit(unit, u, attackerWeapon);
                        }
                    }
                    else
                    {
                        // Jeśli broń nie jest typu "entangling", zawsze rozluźniamy chwyt
                        CombatManager.Instance.ReleaseEntangledUnit(unit, u);
                    }

                    break;
                }
            }
        }

        // Sprawdza zasięg ruchu postaci lub wierzchowca
        int movementRange = unit.GetComponent<Stats>().TempSz;

        // Pozycja postaci przed zaczęciem wykonywania ruchu
        Vector2 startCharPos = unit.transform.position;

        // Aktualizuje informację o zajęciu pola, które postać opuszcza
        GridManager.Instance.ResetTileOccupancy(startCharPos);

        // Pozycja pola wybranego jako cel ruchu
        Vector2 selectedTilePos = new Vector2(selectedTile.transform.position.x, selectedTile.transform.position.y);

        // Znajdź najkrótszą ścieżkę do celu
        List<Vector2> path = FindPath(startCharPos, selectedTilePos);

        // Sprawdza czy wybrane pole jest w zasięgu ruchu postaci. W przypadku automatycznej walki ten warunek nie jest wymagany.
        if (path.Count > 0 && (path.Count <= movementRange || GameManager.IsAutoCombatMode))
        {
            if (!unit.IsRunning && RoundsManager.RoundNumber != 0)
            {
                unit.CanMove = false;
                SetCanMoveToggle(false);

                if (!unit.IsRetreating) // Odwrót
                {
                    Debug.Log($"<color=green>{unit.GetComponent<Stats>().Name} wykonał/a ruch. </color>");
                }

                //Sprawdzamy, czy postać powinna zakończyć turę
                if (!unit.CanDoAction)
                {
                    RoundsManager.Instance.FinishTurn();
                }
            }
            else
            {
                RoundsManager.Instance.DoAction(unit);

                if (unit.IsRunning)
                {
                    StartCoroutine(UpdateMovementRange(1));
                }
            }

            //Resetuje pozycję obronną, jeśli była aktywna
            if (unit.DefensiveBonus != 0 && unit.CanDoAction)
            {
                CombatManager.Instance.DefensiveStance();
            }

            // Oznacza wybrane pole jako zajęte (gdyż trochę potrwa, zanim postać tam dojdzie i gdyby nie zaznaczyć, to można na nie ruszyć inną postacią)
            selectedTile.GetComponent<Tile>().IsOccupied = true;

            //Zapobiega zaznaczeniu jako zajęte pola docelowego, do którego jednostka w trybie automatycznej walki niekoniecznie da radę dojść
            if (GameManager.IsAutoCombatMode)
            {
                AutoCombatManager.Instance.TargetTile = selectedTile.GetComponent<Tile>();
            }

            // Resetuje kolor pól w zasięgu ruchu na czas jego wykonywania
            GridManager.Instance.ResetColorOfTilesInMovementRange();

            //Sprawdza, czy ruch powoduje ataki okazyjne
            CombatManager.Instance.CheckForOpportunityAttack(unitGameObject, selectedTilePos);

            // Wykonuje pojedynczy ruch tyle razy ile wynosi zasięg ruchu postaci
            StartCoroutine(MoveWithDelay(unitGameObject, path, movementRange));
        }
        else
        {
            Debug.Log("Wybrane pole jest poza zasięgiem ruchu lub jest zajęte.");
        }
    }

    private IEnumerator MoveWithDelay(GameObject unitGameObject, List<Vector2> path, int movementRange)
    {
        // Ogranicz iterację do mniejszej wartości: movementRange lub liczby elementów w liście path
        int iterations = Mathf.Min(movementRange, path.Count);

        for (int i = 0; i < iterations; i++)
        {
            Vector2 nextPos = path[i];

            float elapsedTime = 0f;
            float duration = 0.2f; // Czas trwania interpolacji

            while (elapsedTime < duration && unitGameObject != null && !ReinforcementLearningManager.Instance.IsLearning)
            {
                IsMoving = true;

                unitGameObject.transform.position = Vector2.Lerp(unitGameObject.transform.position, nextPos, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null; // Poczekaj na odświeżenie klatki animacji
            }

            //Na wypadek, gdyby w wyniku ataku okazyjnego podczas ruchu jednostka została zabita i usunięta
            if (unitGameObject == null)
            {
                IsMoving = false;
                yield break;
            }

            unitGameObject.transform.position = nextPos;
        }

        if ((Vector2)unitGameObject.transform.position == path[iterations - 1])
        {
            IsMoving = false;
            Retreat(false);

            if (Unit.SelectedUnit != null)
            {
                GridManager.Instance.HighlightTilesInMovementRange(Unit.SelectedUnit.GetComponent<Stats>());
            }
        }

        //Zaznacza jako zajęte faktyczne pole, na którym jednostka zakończy ruch, a nie pole do którego próbowała dojść
        if (GameManager.IsAutoCombatMode || ReinforcementLearningManager.Instance.IsLearning)
        {
            AutoCombatManager.Instance.CheckForTargetTileOccupancy(unitGameObject);
        }
    }

    public List<Vector2> FindPath(Vector2 start, Vector2 goal)
    {
        // Tworzy listę otwartych węzłów
        List<Node> openNodes = new List<Node>();

        // Dodaje węzeł początkowy do listy otwartych węzłów
        Node startNode = new Node
        {
            Position = start,
            G = 0,
            H = CalculateDistance(start, goal),
            F = 0 + CalculateDistance(start, goal),
            Parent = default
        };
        openNodes.Add(startNode);

        // Tworzy listę zamkniętych węzłów
        List<Vector2> closedNodes = new List<Vector2>();

        while (openNodes.Count > 0)
        {
            // Znajduje węzeł z najmniejszym kosztem F i usuwa go z listy otwartych węzłów
            Node current = openNodes.OrderBy(n => n.F).First();
            openNodes.Remove(current);

            // Dodaje bieżący węzeł do listy zamkniętych węzłów
            closedNodes.Add(current.Position);

            // Sprawdza, czy bieżący węzeł jest węzłem docelowym
            if (current.Position == goal)
            {
                // Tworzy listę punktów i dodaje do niej węzły od węzła docelowego do początkowego
                List<Vector2> path = new List<Vector2>();
                Node node = current;

                while (node.Position != start)
                {
                    path.Add(new Vector2(node.Position.x, node.Position.y));
                    node = node.Parent;
                }

                // Odwraca kolejność punktów w liście, aby uzyskać ścieżkę od początkowego do docelowego
                path.Reverse();

                return path;
            }

            // Pobiera sąsiadów bieżącego węzła
            List<Node> neighbors = new List<Node>();
            neighbors.Add(new Node { Position = current.Position + Vector2.up });
            neighbors.Add(new Node { Position = current.Position + Vector2.down });
            neighbors.Add(new Node { Position = current.Position + Vector2.left });
            neighbors.Add(new Node { Position = current.Position + Vector2.right });

            // Przetwarza każdego sąsiada
            foreach (Node neighbor in neighbors)
            {
                // Sprawdza, czy sąsiad jest w liście zamkniętych węzłów
                if (closedNodes.Contains(neighbor.Position))
                {
                    continue;
                }

                // Sprawdza, czy na miejscu sąsiada występuje inny collider niż tile
                Collider2D collider = Physics2D.OverlapPoint(neighbor.Position);

                if (collider != null)
                {
                    bool isTile = false;

                    if (collider.gameObject.CompareTag("Tile") && !collider.gameObject.GetComponent<Tile>().IsOccupied)
                    {
                        isTile = true;
                    }

                    if (isTile)
                    {
                        // Oblicza koszt G dla sąsiada
                        int gCost = current.G + 1;

                        // Sprawdza, czy sąsiad jest już na liście otwartych węzłów
                        Node existingNode = openNodes.Find(n => n.Position == neighbor.Position);

                        if (existingNode != null)
                        {
                            // Jeśli koszt G dla bieżącego węzła jest mniejszy niż dla istniejącego węzła, to aktualizuje go
                            if (gCost < existingNode.G)
                            {
                                existingNode.G = gCost;
                                existingNode.F = existingNode.G + existingNode.H;
                                existingNode.Parent = current;
                            }
                        }
                        else
                        {
                            // Jeśli sąsiad nie jest jeszcze na liście otwartych węzłów, to dodaje go
                            Node newNode = new Node
                            {
                                Position = neighbor.Position,
                                G = gCost,
                                H = CalculateDistance(neighbor.Position, goal),
                                F = gCost + CalculateDistance(neighbor.Position, goal),
                                Parent = current
                            };
                            openNodes.Add(newNode);
                        }
                    }
                }
            }
        }

        // Jeśli nie udało się znaleźć ścieżki, to zwraca pustą listę
        return new List<Vector2>();
    }
    #endregion

    // Funkcja obliczająca odległość pomiędzy dwoma punktami na płaszczyźnie XY
    private int CalculateDistance(Vector2 a, Vector2 b)
    {
        return (int)(Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y));
    }

    #region Charge, run and flight modes
    public void Run()
    {
        if (Unit.SelectedUnit != null && Unit.SelectedUnit.GetComponent<Unit>().Prone)
        {
            Debug.Log("Jednostka w stanie powalenia nie może wykonywać biegu.");
            return;
        }

        //Uwzględnia cechę Długi Krok
        int modifier = Unit.SelectedUnit != null && Unit.SelectedUnit.GetComponent<Stats>().Stride ? 3 : 2;

        StartCoroutine(UpdateMovementRange(modifier));
        Retreat(false); // Zresetowanie bezpiecznego odwrotu
    }

    public void Flight()
    {
        if (Unit.SelectedUnit == null) return;
        if (Unit.SelectedUnit.GetComponent<Unit>().Prone)
        {
            Debug.Log("Jednostka w stanie powalenia nie może wykonywać lotu.");
            return;
        }

        int modifier = Unit.SelectedUnit.GetComponent<Stats>().Flight;

        StartCoroutine(UpdateMovementRange(modifier, null, false, true));
        Retreat(false); // Zresetowanie bezpiecznego odwrotu
    }

    public IEnumerator UpdateMovementRange(int modifier, Unit unit = null, bool isCharging = false, bool isFlying = false)
    {
        if (Unit.SelectedUnit != null)
        {
            unit = Unit.SelectedUnit.GetComponent<Unit>();
        }

        if (unit == null) yield break;

        Stats stats = unit.GetComponent<Stats>();

        //Jeżeli postać już jest w trybie szarży, lotu lub biegu, resetuje je
        if ((isCharging && unit.IsCharging || unit.IsRunning && !isCharging || unit.IsFlying && isFlying) && modifier > 1)
        {
            modifier = 1;
        }

        //Modyfikator za przeciążenie
        if (stats.MaxEncumbrance - stats.CurrentEncumbrance < 0 && stats.CurrentEncumbrance < stats.MaxEncumbrance * 2)
        {
            stats.TempSz = Math.Max(3, stats.Sz - 1);
        }
        else if (stats.MaxEncumbrance - stats.CurrentEncumbrance < 0 && stats.CurrentEncumbrance < stats.MaxEncumbrance * 3)
        {
            stats.TempSz = Math.Max(2, stats.Sz - 2);
        }
        else if (stats.CurrentEncumbrance > stats.MaxEncumbrance * 3)
        {
            stats.TempSz = 0;
        }
        else
        {
            stats.TempSz = stats.Sz;
        }

        // Uwzględnienie szybkości wierzchowca
        if(unit.IsMounted && unit.Mount != null)
        {
            stats.TempSz = unit.Mount.GetComponent<Stats>().Sz;
            if (unit.Mount.GetComponent<Stats>().Flight != 0) unit.Stats.TempSz = unit.Mount.GetComponent<Stats>().Flight;
        }

        //Sprawdza, czy jednostka może wykonać bieg, lot lub szarże
        if (modifier > 1 && !unit.CanDoAction)
        {
            Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
            yield break;
        }
        else if (modifier > 1 && stats.Race == "Zombie")
        {
            Debug.Log("Ta jednostka nie może wykonywać akcji biegu.");
            yield break;
        }

        //Aktualizuje obecny tryb poruszania postaci
        unit.IsCharging = modifier > 1 && isCharging;
        unit.IsFlying = modifier > 1 && isFlying;
        unit.IsRunning = modifier > 1 && !isCharging && !isFlying? true : false;

        if (!unit.IsCharging)
        {
            CombatManager.Instance.ChangeAttackType("StandardAttack"); //Resetuje szarże jako obecny typ ataku i ustawia standardowy atak
        }

        if (unit.IsRunning)
        {
            int rollResult = 0;
            if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "atletykę", result => rollResult = result));
                if (rollResult == 0) yield break;
            }

            //Oblicza obecną szybkość
            stats.TempSz *= modifier;
            stats.TempSz += DiceRollManager.Instance.TestSkill("Zw", stats, "Athletics", 20, rollResult) / 2;

            //Uwzględnia talent Szybkobiegacz
            stats.TempSz += stats.Sprinter;
        }
        else if(unit.IsFlying)
        {
            //Oblicza obecną szybkość
            stats.TempSz = modifier;
        }
        else
        {
            // Jednostki latające mogą podczas szarżu użyc lotu
            if(unit.IsCharging && stats.Flight != 0)
            {
                stats.TempSz = stats.Flight;
            }
            else
            {
                //Oblicza obecną szybkość
                stats.TempSz *= modifier;
            }
        }

        // Uwzględnia cechę Skoczny
        if((unit.IsRunning || unit.IsCharging) && stats.Bounce)
        {
            stats.TempSz *= 2;
        }

        // Uwzględnia ogłuszenie i powalenie
        if (unit.Stunned > 0 || unit.Prone) stats.TempSz /= 2;

        ChangeButtonColor(modifier, unit.IsRunning, unit.IsFlying);

        // Aktualizuje podświetlenie pól w zasięgu ruchu
        GridManager.Instance.HighlightTilesInMovementRange(stats);
    }

    private void ChangeButtonColor(int modifier, bool isRunning, bool IsFlying)
    {
        //_chargeButton.GetComponent<Image>().color = modifier == 1 ? Color.white : modifier == 2 ? Color.green : Color.white;
        _runButton.GetComponent<Image>().color = modifier == 2 && isRunning ? Color.green : Color.white;
        _flightButton.GetComponent<Image>().color = modifier > 1 && IsFlying ? Color.green : Color.white;
    }

    public void ShowOrHideFlightButton(bool value)
    {
        _flightButton.gameObject.SetActive(value);
    }
    #endregion

    #region Retreat
    //Bezpieczny odwrót
    public void Retreat(bool value)
    {
        StartCoroutine(RetreatCoroutine(value));
    }
    public IEnumerator RetreatCoroutine(bool value)
    {
        if (Unit.SelectedUnit == null) yield break;
        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();
        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();

        int advantage = 0;
        int cost = stats.Relentless ? 1 : 2;
        if (unit.tag == "PlayerUnit")
        {
            advantage = InitiativeQueueManager.Instance.PlayersAdvantage;
        }
        else if (unit.tag == "EnemyUnit")
        {
            advantage = InitiativeQueueManager.Instance.EnemiesAdvantage;
        }

        if (value == true)
        {
            if (!unit.CanMove || (!unit.CanDoAction && advantage < cost)) //Sprawdza, czy jednostka może wykonać ruch
            {
                Debug.Log("Ta jednostka nie może w obecnej rundzie wykonać odwrotu.");
                yield break;
            }
            //Jeżeli do wyboru jest tylko Unik, bo nie ma wystarczającej ilości przewagi, albo tylko Przewaga, bo nie ma dostępnych akcji, to panel nie jest wyświetlany
            _retreatPanel.SetActive(advantage >= 2 && unit.CanDoAction);

            if(GameManager.IsAutoCombatMode)
            {
                _retreatPanel.SetActive(false);
                _retreatWay = "advantage";
            }

            if (_retreatPanel.activeSelf)
            {
                // Najpierw czekamy, aż gracz kliknie którykolwiek przycisk
                yield return new WaitUntil(() => !_retreatPanel.activeSelf);

                // Jeżeli wybraliśmy unik to czekamy na wynik rzutu
                int rollResult = 0;
                if (_retreatWay == "dodge" && !GameManager.IsAutoDiceRollingMode && unit.CompareTag("PlayerUnit"))
                {
                    yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(unit.GetComponent<Stats>(), "unik", result => rollResult = result));
                    if (rollResult == 0) yield break;
                }
                else if (_retreatWay == "dodge")
                {
                    rollResult = UnityEngine.Random.Range(1, 101);
                }
                else if (_retreatWay != "advantage") // Zamknięcie okna. Odwrót nie jest wykonywany
                {
                    value = false;
                }
            }
            else
            {
                // Jest ustalany jedyny możliwy sposób odwrotu
                _retreatWay = advantage >= 2 ? "advantage" : "dodge";
            }

            if (_retreatWay == "dodge")
            {
                // Test uniku
                int dodgeModifier = CombatManager.Instance.CalculateDodgeModifier(unit, stats);
                int dodgeValue = stats.Dodge + stats.Zw + dodgeModifier;

                RoundsManager.Instance.DoAction(unit);

                yield return StartCoroutine(CombatManager.Instance.Dodge(unit, stats, dodgeValue, dodgeModifier));

                // Test ataku przeciwników w zwarciu
                List<Unit> opponentsUnits = new List<Unit>();

                // Funkcja pomocnicza do zliczania jednostek w sąsiedztwie danej pozycji
                CountAdjacentUnits(unit.transform.position, unit.tag);

                void CountAdjacentUnits(Vector2 center, string allyTag)
                {
                    Vector2[] positions = {
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
                        if (collider == null || collider.GetComponent<Unit>() == null) continue;

                        if (!collider.CompareTag(allyTag) && !opponentsUnits.Contains(collider.GetComponent<Unit>()) && InventoryManager.Instance.ChooseWeaponToAttack(collider.gameObject).Type.Contains("melee"))
                        {
                            opponentsUnits.Add(collider.GetComponent<Unit>());
                            Debug.Log($"Dodajemy {collider.GetComponent<Stats>().Name} do listy jednostek, ktore będą wykonywały atak okazyjny");
                        }
                    }
                }

                int highestOpponentSuccessValue = 0;

                foreach (Unit attacker in opponentsUnits)
                {
                    Stats attackerStats = attacker.GetComponent<Stats>();
                    Weapon attackerWeapon = InventoryManager.Instance.ChooseWeaponToAttack(attacker.gameObject);

                    // Ustalamy umiejętności, które będą testowane w zależności od kategorii broni
                    MeleeCategory meleeSkill = EnumConverter.ParseEnum<MeleeCategory>(attackerWeapon.Category) ?? MeleeCategory.Basic;

                    int rollOnAttack = 0;
                    if (!GameManager.IsAutoDiceRollingMode && attacker.CompareTag("PlayerUnit"))
                    {
                        yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(attackerStats, "trafienie", result => rollOnAttack = result));
                        if (rollOnAttack == 0) yield break;
                    }
                    else
                    {
                        rollOnAttack = UnityEngine.Random.Range(1, 101);
                    }
                    Debug.Log("attacker " + attacker);
                    int skillValue = attackerStats.Zr;
                    int attackModifier = CombatManager.Instance.CalculateAttackModifier(attacker, attackerWeapon, unit, 0, false);

                    int[] results = CombatManager.Instance.CalculateSuccessLevel(attackerWeapon, rollOnAttack, skillValue, true, attackModifier);
                    int attackerSuccessValue = results[0];
                    int attackerSuccessLevel = results[1];

                    if (highestOpponentSuccessValue < attackerSuccessValue)
                    {
                        highestOpponentSuccessValue = attackerSuccessValue;
                        Debug.Log("highestOpponentSuccessValue  " + highestOpponentSuccessValue);
                    }

                    string successLevelColor = attackerSuccessValue >= 0 ? "green" : "red";
                    string modifierString = attackModifier != 0 ? $" Modyfikator: {attackModifier}," : "";

                    Debug.Log($"{attackerStats.Name} atakuje przy użyciu {attackerWeapon.Name}. Wynik rzutu: {rollOnAttack}, Wartość umiejętności: {skillValue},{modifierString} PS: <color={successLevelColor}>{attackerSuccessLevel}</color>");
                }

                Debug.Log("CombatManager.Instance.DefenceResults[0]  " + CombatManager.Instance.DefenceResults[0]);
                Debug.Log("highestOpponentSuccessValue  " + highestOpponentSuccessValue);

                if (highestOpponentSuccessValue > CombatManager.Instance.DefenceResults[0])
                {
                    value = false;
                    Debug.Log($"{stats.Name} nie udaje się wykonać bezpiecznego odwrotu.");
                }
                else
                {
                    Debug.Log($"{stats.Name} udało się wykonać bezpieczny odwrót. Może się teraz przemieścić bez wywołania ataku okazyjnego.");
                }

                highestOpponentSuccessValue = 0;
            }
            else if (_retreatWay == "advantage")
            {
                //Zaktualizowanie przewagi
                InitiativeQueueManager.Instance.CalculateAdvantage(unit.tag, -cost);
                string group = unit.tag == "PlayerUnit" ? "sojuszników" : "przeciwników";
                Debug.Log($"<color=green>{unit.GetComponent<Stats>().Name} wykonuje odwrót korzystając z punktów przewagi. Przewaga {group} została zmniejszona o {cost}.</color>");
            }
        }

        unit.IsRetreating = value;
        if (value == true)
        {
            StartCoroutine(UpdateMovementRange(1));
            CombatManager.Instance.ChangeAttackType("StandardAttack");
        }

        _retreatButton.GetComponent<Image>().color = unit.IsRetreating ? Color.green : Color.white;

        _retreatWay = "";
    }

    #endregion

    #region Highlight path
    public void HighlightPath(GameObject unit, GameObject tile)
    {
        var path = FindPath(unit.transform.position, new Vector2(tile.transform.position.x, tile.transform.position.y));

        if (path.Count <= unit.GetComponent<Stats>().TempSz)
        {
            foreach (Vector2 tilePosition in path)
            {
                Collider2D collider = Physics2D.OverlapPoint(tilePosition);
                collider.gameObject.GetComponent<Tile>().HighlightTile();
            }
        }
    }
    #endregion

    public void SetCanMoveToggle(bool canMove)
    {
        _canMoveToggle.isOn = canMove;
    }
    public void SetCanMoveByToggle()
    {
        if (Unit.SelectedUnit == null) return;
        Unit.SelectedUnit.GetComponent<Unit>().CanMove = _canMoveToggle.isOn;

        GridManager.Instance.HighlightTilesInMovementRange(Unit.SelectedUnit.GetComponent<Stats>());
    }
}

public class Node
{
    public Vector2 Position; // Pozycja węzła na siatce
    public int G; // Koszt dotarcia do węzła
    public int H; // Szacowany koszt dotarcia z węzła do celu
    public int F; // Całkowity koszt (G + H)
    public Node Parent; // Węzeł nadrzędny w ścieżce
}