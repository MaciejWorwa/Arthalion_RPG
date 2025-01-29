using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEditor.Experimental.GraphView;

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
    [SerializeField] private Button _retreatButton;
    [SerializeField] private Toggle _canMoveToggle;

    #region Move functions
    public void MoveSelectedUnit(GameObject selectedTile, GameObject unitGameObject)
    {
        // Nie pozwala wykonać akcji ruchu, dopóki poprzedni ruch nie zostanie zakończony. Sprawdza też, czy gra nie jest wstrzymana (np. poprzez otwarcie dodatkowych paneli)
        if( IsMoving == true || GameManager.IsGamePaused) return;

        Unit unit = unitGameObject.GetComponent<Unit>();

        if(!unit.CanMove && !unit.IsRunning)
        {
            Debug.Log("Ta jednostka nie może wykonać ruchu w tej rundzie.");
            return;
        }
        else if (!unit.CanDoAction && unit.IsRunning)
        {
            Debug.Log("Ta jednostka nie może wykonać biegu w tej rundzie.");
            UpdateMovementRange(1);
            return;
        }

        // Sprawdza zasięg ruchu postaci
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
            if(!unit.IsRunning && RoundsManager.RoundNumber != 0)
            {
                unit.CanMove = false;
                SetCanMoveToggle(false);
                Debug.Log($"<color=green>{unit.GetComponent<Stats>().Name} wykonał/a ruch. </color>");
            }
            else
            {
                RoundsManager.Instance.DoAction(unit);
                UpdateMovementRange(1);
            }

            //Resetuje pozycję obronną, jeśli była aktywna
            if (unit.DefensiveBonus != 0 && unit.CanDoAction)
            {
                CombatManager.Instance.DefensiveStance();
            }

            // Oznacza wybrane pole jako zajęte (gdyż trochę potrwa, zanim postać tam dojdzie i gdyby nie zaznaczyć, to można na nie ruszyć inną postacią)
            selectedTile.GetComponent<Tile>().IsOccupied = true;

            //Zapobiega zaznaczeniu jako zajęte pola docelowego, do którego jednostka w trybie automatycznej walki niekoniecznie da radę dojść
            if(GameManager.IsAutoCombatMode)
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

            while (elapsedTime < duration && unitGameObject != null)
            {
                IsMoving = true;

                unitGameObject.transform.position = Vector2.Lerp(unitGameObject.transform.position, nextPos, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null; // Poczekaj na odświeżenie klatki animacji
            }

            //Na wypadek, gdyby w wyniku ataku okazyjnego podczas ruchu jednostka została zabita i usunięta
            if(unitGameObject == null)
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
            
            if(Unit.SelectedUnit != null)
            {
                GridManager.Instance.HighlightTilesInMovementRange(Unit.SelectedUnit.GetComponent<Stats>());
            }
        }

        //Zaznacza jako zajęte faktyczne pole, na którym jednostka zakończy ruch, a nie pole do którego próbowała dojść
        if(GameManager.IsAutoCombatMode)
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

    #region Charge and Run modes
    public void Run()
    {
        UpdateMovementRange(2);
        Retreat(false); // Zresetowanie bezpiecznego odwrotu
    }

    public void UpdateMovementRange(int modifier, Unit unit = null, bool isCharging = false)
    {
        if (Unit.SelectedUnit != null)
        {
            unit = Unit.SelectedUnit.GetComponent<Unit>();
        }

        if(unit == null) return;

        Stats stats = unit.GetComponent<Stats>();

        //Jeżeli postać już jest w trybie szarży lub biegu, resetuje je
        if (isCharging && unit.IsCharging && modifier == 2 || unit.IsRunning && !isCharging && modifier == 2)
        {
            modifier = 1;
        }

        //Sprawdza, czy jednostka może wykonać bieg lub szarże
        if ((modifier == 2 || modifier == 3) && !unit.CanDoAction)
        {
            Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
            return;
        } 
        else if( modifier == 3 && stats.Race == "Zombie")
        {
            Debug.Log("Ta jednostka nie może wykonywać akcji biegu.");
            return;
        } 

        //Aktualizuje obecny tryb poruszania postaci
        unit.IsCharging = isCharging;
        unit.IsRunning = modifier == 2 && !isCharging ? true : false;

        if(unit.IsRunning)
        {
            CombatManager.Instance.ChangeAttackType("StandardAttack"); //Resetuje szarże jako obecny typ ataku i ustawia standardowy atak
        }

        //Sprawdza, czy zbroja nie jest wynikiem zaklęcia Pancerz Eteru
        bool etherArmor = false;
        if(MagicManager.Instance.UnitsStatsAffectedBySpell != null && MagicManager.Instance.UnitsStatsAffectedBySpell.Count > 0)
        {
            //Przeszukanie statystyk jednostek, na które działają zaklęcia czasowe
            for (int i = 0; i < MagicManager.Instance.UnitsStatsAffectedBySpell.Count; i++)
            {
                //Jeżeli wcześniejsza wartość zbroi (w tym przypadku na głowie, ale to może być dowolna lokalizacja) jest inna niż obecna, świadczy to o użyciu Pancerzu Eteru
                if (MagicManager.Instance.UnitsStatsAffectedBySpell[i].Name == stats.Name && MagicManager.Instance.UnitsStatsAffectedBySpell[i].Armor_head != stats.Armor_head)
                {
                    etherArmor = true;
                }
            }
        }
        //Uwzględnia karę do Szybkości za zbroję płytową
        bool has_plate_armor = stats.Armor_head >= 5 || stats.Armor_torso >= 5 || stats.Armor_arms >= 5 || stats.Armor_legs >= 5;
        bool is_sturdy = stats.Sturdy;
        int movement_armor_penalty = (has_plate_armor && !is_sturdy && !etherArmor) ? 1 : 0;

        //Oblicza obecną szybkość
        stats.TempSz = (stats.Sz - movement_armor_penalty) * modifier;

        if(unit.IsRunning)
        {
            // DODAĆ TUTAJ OPCJE DLA MANUALNEGO RZUCANIA KOŚĆMI
            
            stats.TempSz += UnitsManager.Instance.TestSkill("Athletics", "Zw", stats, 20) / 2;
        }

        ChangeButtonColor(modifier, unit.IsCharging);

        // Aktualizuje podświetlenie pól w zasięgu ruchu
        GridManager.Instance.HighlightTilesInMovementRange(stats);  
    }

    //Bezpieczny odwrót
    public void Retreat(bool value)
    {
        if (Unit.SelectedUnit == null) return;
        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();

        //DODAĆ NOWE ZASADY ODWROTU: a) gdy ma więcej przewag niż każdy przeciwnik od którego odchodzi to przewagi się zerują i ma do wykonania ruch oraz akcję, b) gdy ma tyle samo lub mniej przewag może użyć akcję na unik (przeciwstawny test unik/broń biała). Wygrana strona dostaje +1 przewagi, jak unikający wygra to może wykonać ruch bez okazyjki.
        // Czyli sama akcja odwrotu nie aktywuje się przy ruchu, tylko w momencie kliknięcia na nią. Potem gracz decyduje, czy się ruszyć, czy nie - ale zna już wynik akcji Odwrotu.

        if(value == true && !unit.CanDoAction) //Sprawdza, czy jednostka może wykonać akcję podwójną
        {
            Debug.Log("Ta jednostka nie może w tej rundzie wykonać akcji podwójnej.");
            return;
        }

        unit.IsRetreating = value;
        if(value == true)
        {
            UpdateMovementRange(1);
            CombatManager.Instance.ChangeAttackType("StandardAttack");
        }

        _retreatButton.GetComponent<Image>().color = unit.IsRetreating ? Color.green : Color.white;
    }

    private void ChangeButtonColor(int modifier, bool isCharging)
    {  
        //_chargeButton.GetComponent<Image>().color = modifier == 1 ? Color.white : modifier == 2 ? Color.green : Color.white;
        _runButton.GetComponent<Image>().color = modifier == 2 && !isCharging ? Color.green : Color.white;   
    }
    #endregion

   
    #region Highlight path
    public void HighlightPath(GameObject unit, GameObject tile)
    {
        var path = FindPath(unit.transform.position, new Vector2 (tile.transform.position.x, tile.transform.position.y));

        if(path.Count <= unit.GetComponent<Stats>().TempSz)
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