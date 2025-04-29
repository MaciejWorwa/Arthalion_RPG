using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

public class UnitsManager : MonoBehaviour
{
    // Prywatne statyczne pole przechowujące instancję
    private static UnitsManager instance;

    // Publiczny dostęp do instancji
    public static UnitsManager Instance
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

    [SerializeField] private GameObject _unitPanel;
    [SerializeField] private UnityEngine.UI.Button _spellbookButton;
    [SerializeField] private GameObject _spellListPanel;
    [SerializeField] private TMP_Text _raceDisplay;
    [SerializeField] private TMP_Text _healthDisplay;
    [SerializeField] private UnityEngine.UI.Slider _healthBar;
    [SerializeField] private UnityEngine.UI.Image _tokenDisplay;
    [SerializeField] private UnityEngine.UI.Image _tokenBorder;
    [SerializeField] private GameObject _unitPrefab;
    [SerializeField] private CustomDropdown _unitsDropdown;
    public Transform UnitsScrollViewContent;
    [SerializeField] private UnityEngine.UI.Toggle _unitTagToggle;
    [SerializeField] private UnityEngine.UI.Button _createUnitButton; // Przycisk do tworzenia jednostek na losowych pozycjach
    [SerializeField] private UnityEngine.UI.Button _removeUnitButton;
    public UnityEngine.UI.Toggle SortSavedUnitsByDateToggle;
    [SerializeField] private UnityEngine.UI.Button _selectUnitsButton; // Przycisk do zaznaczania wielu jednostek
    [SerializeField] private UnityEngine.UI.Button _removeSavedUnitFromListButton; // Przycisk do usuwania zapisanych jednostek z listy
    [SerializeField] private UnityEngine.UI.Button _updateUnitButton;
    [SerializeField] private UnityEngine.UI.Button _removeUnitConfirmButton;
    [SerializeField] private GameObject _removeUnitConfirmPanel;
    public static bool IsTileSelecting;
    public static bool IsMultipleUnitsSelecting = false;
    public static bool IsUnitRemoving = false;
    public static bool IsUnitEditing = false;
    public bool IsSavedUnitsManaging = false;
    public List<Unit> AllUnits = new List<Unit>();

    void Start()
    {
        //Wczytuje listę wszystkich jednostek
        DataManager.Instance.LoadAndUpdateStats();

        _removeUnitConfirmButton.onClick.AddListener(() =>
        {
            if (Unit.SelectedUnit != null)
            {
                DestroyUnit(Unit.SelectedUnit);
                _removeUnitConfirmPanel.SetActive(false);
            }
            else
            {
                Debug.Log("Aby usunąć jednostkę, musisz najpierw ją wybrać.");
            }
        });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete) && (Unit.SelectedUnit != null || (AreaSelector.Instance.SelectedUnits != null && AreaSelector.Instance.SelectedUnits.Count > 1)))
        {
            if (_removeUnitConfirmPanel.activeSelf == false)
            {
                _removeUnitConfirmPanel.SetActive(true);
            }
            else
            {
                //Jeśli jest zaznaczone więcej jednostek, to usuwa wszystkie
                if (AreaSelector.Instance.SelectedUnits != null && AreaSelector.Instance.SelectedUnits.Count > 1)
                {
                    for (int i = AreaSelector.Instance.SelectedUnits.Count - 1; i >= 0; i--)
                    {
                        Unit unit = AreaSelector.Instance.SelectedUnits[i];

                        //Usuwa też wierzchowce
                        if (unit.Mount != null && unit.IsMounted)
                        {
                            DestroyUnit(unit.Mount.gameObject);
                        }

                        DestroyUnit(unit.gameObject);
                    }
                    AreaSelector.Instance.SelectedUnits.Clear();
                }
                else
                {
                    DestroyUnit(Unit.SelectedUnit);
                }
                _removeUnitConfirmPanel.SetActive(false);
            }
        }
    }

    #region Creating units
    public void CreateUnitMode()
    {
        IsTileSelecting = true;

        Debug.Log("Wybierz pole, na którym chcesz stworzyć jednostkę.");
        return;
    }

    public void CreateUnitOnSelectedTile(Vector2 position)
    {
        CreateUnit(_unitsDropdown.GetSelectedIndex(), "", position);

        //Resetuje kolor przycisku z wybraną jednostką na liście jednostek
        CreateUnitButton.SelectedUnitButtonImage.color = new Color(0.55f, 0.66f, 0.66f, 0.05f);
    }

    public void CreateUnitOnRandomTile()
    {
        List<Vector2> availablePositions = GridManager.Instance.AvailablePositions();
        Vector2 position = Vector2.zero;

        if (!SaveAndLoadManager.Instance.IsLoading)
        {
            if (availablePositions.Count == 0)
            {
                Debug.Log("Nie można stworzyć nowej jednostki. Brak wolnych pól.");
                return;
            }

            // Wybranie losowej pozycji z dostępnych
            int randomIndex = UnityEngine.Random.Range(0, availablePositions.Count);
            position = availablePositions[randomIndex];
        }

        CreateUnit(_unitsDropdown.GetSelectedIndex(), "", position);
    }

    public GameObject CreateUnit(int unitId, string unitName, Vector2 position)
    {
        if (_unitsDropdown.SelectedButton == null && SaveAndLoadManager.Instance.IsLoading != true)
        {
            Debug.Log("Wybierz jednostkę z listy.");
            return null;
        }

        // Pole na którym chcemy stworzyć jednostkę
        GameObject selectedTile = GameObject.Find($"Tile {position.x - GridManager.Instance.transform.position.x} {position.y - GridManager.Instance.transform.position.y}");

        //Gdy próbujemy wczytać jednostkę na polu, które nie istnieje (bo np. siatka jest obecnie mniejsza niż siatka, na której były zapisywane jednostki) lub jest zajęte to wybiera im losową pozycję
        if ((selectedTile == null || selectedTile.GetComponent<Tile>().IsOccupied) && SaveAndLoadManager.Instance.IsLoading == true)
        {
            List<Vector2> availablePositions = GridManager.Instance.AvailablePositions();

            if (availablePositions.Count == 0)
            {
                Debug.Log("Nie można stworzyć nowej jednostki. Brak wolnych pól.");
                return null;
            }

            // Wybranie losowej pozycji z dostępnych
            int randomIndex = UnityEngine.Random.Range(0, availablePositions.Count);
            position = availablePositions[randomIndex];

            selectedTile = GameObject.Find($"Tile {position.x - GridManager.Instance.transform.position.x} {position.y - GridManager.Instance.transform.position.y}");
        }
        else if (selectedTile == null)
        {
            Debug.Log("Nie można stworzyć jednostki.");
            return null;
        }

        //Odnacza jednostkę, jeśli jakaś jest zaznaczona
        if (Unit.SelectedUnit != null && IsTileSelecting)
        {
            Unit.SelectedUnit.GetComponent<Unit>().SelectUnit();
        }

        //Tworzy nową postać na odpowiedniej pozycji
        GameObject newUnitObject = Instantiate(_unitPrefab, position, Quaternion.identity);
        Stats stats = newUnitObject.GetComponent<Stats>();
        Unit unit = newUnitObject.GetComponent<Unit>();

        //Umieszcza postać jako dziecko EmptyObject'u do którego są podpięte wszystkie jednostki
        newUnitObject.transform.SetParent(GameObject.Find("----------Units-------------------").transform);

        //Ustawia Id postaci, które będzie definiować jego rasę i statystyki
        stats.Id = unitId;

        //Zmienia status wybranego pola na zajęte
        selectedTile.GetComponent<Tile>().IsOccupied = true;

        // Aktualizuje liczbę wszystkich postaci
        AllUnits.Add(unit);

        // Ustala unikalne Id jednostki
        int newUnitId = 1;
        bool idExists;
        // Pętla sprawdzająca, czy inne jednostki mają takie samo Id
        do
        {
            idExists = false;

            foreach (var u in AllUnits)
            {
                if (u.UnitId == newUnitId)
                {
                    idExists = true;
                    newUnitId++; // Zwiększa id i sprawdza ponownie
                    break;
                }
            }
        }
        while (idExists);
        unit.UnitId = newUnitId;

        //Ustala nazwę jednostki (potrzebne, do wczytywania jednostek z listy zapisanych jednostek)
        if (_unitsDropdown.SelectedButton != null && IsSavedUnitsManaging)
        {
            stats.Name = _unitsDropdown.SelectedButton.GetComponentInChildren<TextMeshProUGUI>().text;
        }

        //Wczytuje statystyki dla danego typu jednostki
        DataManager.Instance.LoadAndUpdateStats(newUnitObject);

        //Ustala nazwę GameObjectu jednostki
        if (unitName.Length < 1)
        {
            newUnitObject.name = stats.Race + $" {newUnitId}";
        }
        else
        {
            newUnitObject.name = unitName;
        }

        // Wczytuje dane zapisanej jednostki
        if (IsSavedUnitsManaging && IsTileSelecting)
        {
            //Jeżeli gra już jest w trakcie wczytywania to nie powielamy tego. Jest to istotne, żeby nie wystąpiły błędy przy wczytywaniu gry, jeśli na mapie są zapisane jednostki
            bool wasLoadingInitially = SaveAndLoadManager.Instance.IsLoading;

            if (!wasLoadingInitially)
            {
                SaveAndLoadManager.Instance.IsLoading = true;
            }

            string savedUnitsFolder = Path.Combine(Application.persistentDataPath, "savedUnitsList");
            string baseFileName = stats.Name;

            //string unitFilePath = Path.Combine(savedUnitsFolder, baseFileName + "_unit.json");
            string weaponFilePath = Path.Combine(savedUnitsFolder, baseFileName + "_weapon.json");
            string inventoryFilePath = Path.Combine(savedUnitsFolder, baseFileName + "_inventory.json");
            string tokenJsonPath = Path.Combine(savedUnitsFolder, baseFileName + "_token.json");

            // SaveAndLoadManager.Instance.LoadComponentDataWithReflection<UnitData, Unit>(newUnit, unitFilePath);
            SaveAndLoadManager.Instance.LoadComponentDataWithReflection<WeaponData, Weapon>(newUnitObject, weaponFilePath);

            // Wczytaj ekwipunek
            InventoryData inventoryData = JsonUtility.FromJson<InventoryData>(File.ReadAllText(inventoryFilePath));
            Inventory inventory = newUnitObject.GetComponent<Inventory>();
            if (File.Exists(inventoryFilePath))
            {
                foreach (var weapon in inventoryData.AllWeapons)
                {
                    InventoryManager.Instance.AddWeaponToInventory(weapon, newUnitObject);
                }

                //Wczytanie aktualnie dobytych broni
                foreach (var weapon in inventory.AllWeapons)
                {
                    if (weapon.Id == inventoryData.EquippedWeaponsId[0])
                    {
                        inventory.EquippedWeapons[0] = weapon;
                    }
                    if (weapon.Id == inventoryData.EquippedWeaponsId[1])
                    {
                        inventory.EquippedWeapons[1] = weapon;
                    }
                    if (inventoryData.EquippedArmorsId.Contains(weapon.Id))
                    {
                        inventory.EquippedArmors.Add(weapon);
                    }
                }
                InventoryManager.Instance.CheckForEquippedWeapons();
            }

            //Wczytanie pieniędzy
            inventory.CopperCoins = inventoryData.CopperCoins;
            inventory.SilverCoins = inventoryData.SilverCoins;
            inventory.GoldCoins = inventoryData.GoldCoins;

            // Wczytaj token, jeśli istnieje
            if (File.Exists(tokenJsonPath))
            {
                string tokenJson = File.ReadAllText(tokenJsonPath);
                TokenData tokenData = JsonUtility.FromJson<TokenData>(tokenJson);

                if (tokenData.filePath.Length > 1)
                {
                    StartCoroutine(TokensManager.Instance.LoadTokenImage(tokenData.filePath, newUnitObject));
                }
            }

            if (!wasLoadingInitially)
            {
                SaveAndLoadManager.Instance.IsLoading = false;
            }
        }

        IsTileSelecting = false;

        if (SaveAndLoadManager.Instance.IsLoading != true)
        {
            //Ustawia tag postaci, który definiuje, czy jest to sojusznik, czy przeciwnik, a także jej domyślny kolor.
            if (_unitTagToggle.isOn)
            {

                newUnitObject.tag = "PlayerUnit";
                unit.DefaultColor = new Color(0f, 0.54f, 0.17f, 1.0f);
            }
            else
            {
                newUnitObject.tag = "EnemyUnit";
                unit.DefaultColor = new Color(0.59f, 0.1f, 0.19f, 1.0f);
            }
            unit.ChangeUnitColor(newUnitObject);

            stats.ChangeTokenSize((int)stats.Size);

            //Losuje początkowe statystyki dla każdej jednostki
            if (!IsSavedUnitsManaging)
            {
                stats.RollForBaseStats();
            }

            // Dodaje do ekwipunku początkową broń adekwatną dla danej jednostki i wyposaża w nią
            if (stats.PrimaryWeaponNames != null && stats.PrimaryWeaponNames.Count > 0 && !IsSavedUnitsManaging)
            {
                Unit.LastSelectedUnit = Unit.SelectedUnit != null ? Unit.SelectedUnit : null;
                Unit.SelectedUnit = newUnitObject;
                SaveAndLoadManager.Instance.IsLoading = true; // Tylko po to, żeby informacja o dobyciu broni i dodaniu do ekwipunku z metody GrabWeapon i LoadWeapon nie były wyświetlane w oknie wiadomości

                InventoryManager.Instance.GrabPrimaryWeapons();
            }

            //Ustala początkową inicjatywę i dodaje jednostkę do kolejki inicjatywy
            stats.Initiative = stats.I + (stats.CombatReflexes * 10) + UnityEngine.Random.Range(1, 11);

            InitiativeQueueManager.Instance.AddUnitToInitiativeQueue(unit);
        }

        return newUnitObject;
    }

    public void SetSavedUnitsManaging(bool value)
    {
        IsSavedUnitsManaging = value;
        IsTileSelecting = false;

        if (IsSavedUnitsManaging)
        {
            IsUnitEditing = false;

            _createUnitButton.gameObject.SetActive(false);
            _removeUnitButton.gameObject.SetActive(false);
            SortSavedUnitsByDateToggle.gameObject.SetActive(true);
            _selectUnitsButton.gameObject.SetActive(false);
            _updateUnitButton.gameObject.SetActive(false);
            _removeSavedUnitFromListButton.gameObject.SetActive(true);
        }
        else
        {
            _removeSavedUnitFromListButton.gameObject.SetActive(false);
            SortSavedUnitsByDateToggle.gameObject.SetActive(false);
            EditUnitModeOff();
        }
    }
    #endregion

    #region Removing units
    public void DestroyUnitMode()
    {
        if (GameManager.IsMapHidingMode)
        {
            Debug.Log("Aby usuwać jednostki, wyjdź z trybu ukrywania obszarów.");
            return;
        }

        IsUnitRemoving = !IsUnitRemoving;

        //Zmienia kolor przycisku usuwania jednostek na aktywny
        _removeUnitButton.GetComponent<UnityEngine.UI.Image>().color = IsUnitRemoving ? Color.green : Color.white;

        if (IsUnitRemoving)
        {
            //Jeżeli jest włączony tryb zaznaczania wielu jednostek to go resetuje
            if (IsMultipleUnitsSelecting)
            {
                SelectMultipleUnitsMode();
            }
            Debug.Log("Wybierz jednostkę, którą chcesz usunąć. Możesz również zaznaczyć obszar, wtedy zostaną usunięte wszystkie znajdujące się w nim jednostki.");
        }
    }
    public void DestroyUnit(GameObject unitObject = null)
    {
        if (unitObject == null)
        {
            unitObject = Unit.SelectedUnit;
        }
        else if (unitObject == Unit.SelectedUnit)
        {
            unitObject.GetComponent<Unit>().SelectUnit();
        }

        Unit unit = unitObject.GetComponent<Unit>();
        Stats stats = unit.Stats;

        //Usunięcie jednostki z kolejki inicjatywy
        InitiativeQueueManager.Instance.RemoveUnitFromInitiativeQueue(unit);

        //Uwolnienie jednostki uwięzionej przez jednostkę, która umiera
        if (unit.EntangledUnitId != 0)
        {
            foreach (var u in AllUnits)
            {
                if (u.UnitId == unit.GetComponent<Unit>().EntangledUnitId && u.Entangled > 0)
                {
                    u.Entangled = 0;
                }
            }
        }

        if(unit.IsMounted && unit.Mount != null && !SaveAndLoadManager.Instance.IsLoading && (AreaSelector.Instance.SelectedUnits == null || !AreaSelector.Instance.SelectedUnits.Contains(unit)))
        {
            unit.Mount.transform.SetParent(GameObject.Find("----------Units-------------------").transform);
            InitiativeQueueManager.Instance.AddUnitToInitiativeQueue(unit.Mount);
            unit.Mount.gameObject.SetActive(true);
            unit.Mount.HasRider = false;
        }

        //Aktualizuje kolejkę inicjatywy
        InitiativeQueueManager.Instance.UpdateInitiativeQueue();

        //Usuwa jednostkę z listy wszystkich jednostek
        AllUnits.Remove(unit);

        //Resetuje Tile, żeby nie było uznawane jako zajęte
        GridManager.Instance.ResetTileOccupancy(unit.transform.position);

        // Aktualizuje osiągnięcia
        if (unit.LastAttackerStats != null)
        {
            unit.LastAttackerStats.OpponentsKilled++;
            if (unit.LastAttackerStats.StrongestDefeatedOpponentOverall < stats.Overall)
            {
                unit.LastAttackerStats.StrongestDefeatedOpponentOverall = stats.Overall;
                unit.LastAttackerStats.StrongestDefeatedOpponent = stats.Name;
            }

            // Uwzględnia cechę Żarłoczny
            if (unit.LastAttackerStats.Hungry)
            {
                StartCoroutine(HungryTrait(unit.LastAttackerStats, stats));
            }
        }

        Destroy(unitObject);

        //Resetuje kolor przycisku usuwania jednostek
        _removeUnitButton.GetComponent<UnityEngine.UI.Image>().color = Color.white;
    }

    private IEnumerator HungryTrait(Stats stats, Stats deadBodyStats)
    {
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

        int[] rollResults = DiceRollManager.Instance.TestSkill("SW", stats, null, 20, rollResult);
        if (rollResults[0] < 0)
        {
            Debug.Log($"<color=red>{stats.Name} traci następną turę, ucztując na martwym ciele {deadBodyStats.Name}. Pamiętaj, aby to uwzględnić.</color>");
        }
    }

    public void RemoveUnitFromList(GameObject confirmPanel)
    {
        if (_unitsDropdown.SelectedButton == null)
        {
            Debug.Log("Wybierz jednostkę z listy.");
        }
        else
        {
            confirmPanel.SetActive(true);
        }
    }
    #endregion

    #region Unit selecting
    public void SelectMultipleUnitsMode(bool value = true)
    {
        // Jeśli `value` jest false, wyłącza tryb zaznaczania, w przeciwnym razie przełącza tryb
        IsMultipleUnitsSelecting = value ? !IsMultipleUnitsSelecting : false;

        // Ustawia kolor przycisku w zależności od stanu
        _selectUnitsButton.GetComponent<UnityEngine.UI.Image>().color = IsMultipleUnitsSelecting ? Color.green : Color.white;

        // Wyświetla komunikat, jeśli tryb zaznaczania jest aktywny
        if (IsMultipleUnitsSelecting)
        {
            //Jeżeli jest włączony tryb usuwania jednostek to go resetuje
            if (IsUnitRemoving)
            {
                DestroyUnitMode();
            }
            Debug.Log("Zaznacz jednostki na wybranym obszarze przy użyciu myszy. Klikając Ctrl+C możesz je skopiować, a następnie wkleić przy pomocy Ctrl+V.");
        }
    }
    #endregion

    #region Unit editing
    public void EditUnitModeOn(Animator panelAnimator)
    {
        IsUnitEditing = true;

        _createUnitButton.gameObject.SetActive(false);
        _removeUnitButton.gameObject.SetActive(false);
        _selectUnitsButton.gameObject.SetActive(false);
        _updateUnitButton.gameObject.SetActive(true);
        _removeSavedUnitFromListButton.gameObject.SetActive(false);

        if (!AnimationManager.Instance.PanelStates.ContainsKey(panelAnimator))
        {
            AnimationManager.Instance.PanelStates[panelAnimator] = false; // Domyślny stan panelu
        }

        //Jeśli panel edycji jednostek jest schowany to wysuwamy go
        if (AnimationManager.Instance.PanelStates[panelAnimator] == false)
        {
            AnimationManager.Instance.TogglePanel(panelAnimator);
        }

        // Jeżeli mamy wybraną jednostkę, pobieramy jej rasę
        string currentRace = Unit.SelectedUnit.GetComponent<Stats>().Race;

        int foundIndex = -1;
        for (int i = 0; i < _unitsDropdown.Buttons.Count; i++)
        {
            // Tutaj sprawdzamy text w komponencie TextMeshProUGUI
            var txt = _unitsDropdown.Buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null && txt.text == currentRace)
            {
                foundIndex = i;
                break;
            }
        }

        // Jeśli znaleźliśmy pasujący przycisk, wywołujemy `SetSelectedIndex(foundIndex+1)`
        if (foundIndex != -1)
        {
            // Indeksy w `Buttons` idą od 0, a `SelectOption` od 1
            _unitsDropdown.SetSelectedIndex(foundIndex + 1);
        }
    }

    public void EditUnitModeOff()
    {
        IsUnitEditing = false;

        _createUnitButton.gameObject.SetActive(true);
        _removeUnitButton.gameObject.SetActive(true);
        _selectUnitsButton.gameObject.SetActive(true);
        _updateUnitButton.gameObject.SetActive(false);

        if (IsSavedUnitsManaging)
        {
            _removeSavedUnitFromListButton.gameObject.SetActive(false);
        }
    }

    public void UpdateUnitNameOrRace()
    {
        if (Unit.SelectedUnit == null) return;

        if (_unitsDropdown.SelectedButton == null)
        {
            Debug.Log("Wybierz rasę z listy. Zmiana rasy wpłynie na statystyki.");
            return;
        }

        GameObject unit = Unit.SelectedUnit;
        Stats stats = unit.GetComponent<Stats>();

        //Ustawia tag postaci, który definiuje, czy jest to sojusznik, czy przeciwnik, a także jej domyślny kolor.
        if (_unitTagToggle.isOn)
        {
            unit.tag = "PlayerUnit";
            unit.GetComponent<Unit>().DefaultColor = new Color(0f, 0.54f, 0.17f, 1.0f);
        }
        else
        {
            unit.tag = "EnemyUnit";
            unit.GetComponent<Unit>().DefaultColor = new Color(0.59f, 0.1f, 0.19f, 1.0f); ;
        }
        unit.GetComponent<Unit>().ChangeUnitColor(unit);

        stats.ChangeTokenSize((int)stats.Size);

        //Sprawdza, czy rasa jest zmieniana
        if (stats.Id != _unitsDropdown.GetSelectedIndex())
        {
            bool changeName = false;

            if (stats.Name.Contains(stats.Race))
            {
                changeName = true;
            }

            // Sprawdza, czy ostatni jeden lub dwa znaki to liczba
            string currentName = stats.Name;
            string numberSuffix = "";
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(currentName, @"(\d{1,2})$");
            if (match.Success)
            {
                numberSuffix = match.Value; // Przechowuje numer znaleziony na końcu nazwy
            }

            // Ustala nową rasę na podstawie rasy wybranej z listy
            stats.Id = _unitsDropdown.GetSelectedIndex();

            string newRaceName = _unitsDropdown.SelectedButton.GetComponentInChildren<TextMeshProUGUI>().text;

            if (changeName)
            {
                // Jeśli zmieniamy nazwę, dodajemy zachowaną liczbę (jeśli istnieje)
                if (!string.IsNullOrEmpty(numberSuffix))
                {
                    stats.Name = $"{newRaceName} {numberSuffix}";
                }
                else
                {
                    stats.Name = newRaceName;
                }
            }

            //Aktualizuje statystyki
            DataManager.Instance.LoadAndUpdateStats(unit);

            //Losuje początkowe statystyki dla człowieka, elfa, krasnoluda i niziołka
            if (stats.Id <= 4 && !IsSavedUnitsManaging)
            {
                stats.RollForBaseStats();
                unit.GetComponent<Unit>().DisplayUnitHealthPoints();
            }

            //Aktualizuje aktualną żywotność
            stats.TempHealth = stats.MaxHealth;

            // Aktualizuje udźwig
            stats.MaxEncumbrance = (stats.S + stats.Wt) / 10 + stats.StrongBack + (stats.Sturdy * 2);

            //Ustala inicjatywę i aktualizuje kolejkę inicjatywy
            stats.Initiative = stats.I + UnityEngine.Random.Range(1, 11);
            InitiativeQueueManager.Instance.RemoveUnitFromInitiativeQueue(unit.GetComponent<Unit>());
            InitiativeQueueManager.Instance.AddUnitToInitiativeQueue(unit.GetComponent<Unit>());
            InitiativeQueueManager.Instance.UpdateInitiativeQueue();

            //Dodaje do ekwipunku początkową broń adekwatną dla danej jednostki i wyposaża w nią
            if (unit.GetComponent<Stats>().PrimaryWeaponNames != null && unit.GetComponent<Stats>().PrimaryWeaponNames.Count > 0 && changeName)
            {
                //Usuwa posiadane bronie
                InventoryManager.Instance.RemoveAllWeaponsFromInventory();

                Unit.LastSelectedUnit = Unit.SelectedUnit != null ? Unit.SelectedUnit : null;
                Unit.SelectedUnit = unit;
                SaveAndLoadManager.Instance.IsLoading = true; // Tylko po to, żeby informacja o dobyciu broni i dodaniu do ekwipunku z metody GrabWeapon i LoadWeapon nie były wyświetlane w oknie wiadomości

                InventoryManager.Instance.GrabPrimaryWeapons();
            }

            unit.GetComponent<Unit>().DisplayUnitName();
            unit.GetComponent<Unit>().DisplayUnitHealthPoints();
        }

        //Aktualizuje wyświetlany panel ze statystykami
        UpdateUnitPanel(unit);
    }

    public void UpdateInitiative()
    {
        if (Unit.SelectedUnit == null) return;

        GameObject unit = Unit.SelectedUnit;
        Stats stats = unit.GetComponent<Stats>();

        //Ustala nową inicjatywę
        stats.Initiative = stats.I + (stats.CombatReflexes * 10) + UnityEngine.Random.Range(1, 11);

        //Uwzględnienie kary do Zręczności za pancerz
        if (stats.Armor_head >= 3 || stats.Armor_torso >= 3 || stats.Armor_arms >= 3 || stats.Armor_legs >= 3)
        {
            stats.Initiative -= 10;
        }

        //Aktualizuje kolejkę inicjatywy
        InitiativeQueueManager.Instance.InitiativeQueue[unit.GetComponent<Unit>()] = stats.I;
        InitiativeQueueManager.Instance.UpdateInitiativeQueue();

        UpdateUnitPanel(unit);
    }

    public void EditAttribute(GameObject textInput)
    {
        if (Unit.SelectedUnit == null) return;

        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();
        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();

        // Pobiera nazwę cechy z nazwy obiektu InputField (bez "_input")
        string attributeName = textInput.name.Replace("_input", "");

        // 1. Sprawdzamy, czy mamy do czynienia z umiejętnością z tablicy Melee
        if (attributeName.StartsWith("Melee_"))
        {
            string categoryName = attributeName.Replace("Melee_", "");
            if (Enum.TryParse<MeleeCategory>(categoryName, out var category))
            {
                // Pobiera wartość z pola tekstowego
                int value = int.TryParse(textInput.GetComponent<TMP_InputField>().text, out int meleeValue)
                            ? meleeValue
                            : 0;

                // Ustawia w słowniku
                stats.Melee[category] = value;
            }
        }
        // 2. Albo umiejętność z tablicy Ranged
        else if (attributeName.StartsWith("Ranged_"))
        {
            string categoryName = attributeName.Replace("Ranged_", "");
            if (Enum.TryParse<RangedCategory>(categoryName, out var category))
            {
                int value = int.TryParse(textInput.GetComponent<TMP_InputField>().text, out int rangedValue)
                            ? rangedValue
                            : 0;

                stats.Ranged[category] = value;
            }
        }
        // 3. A jeżeli to nie Melee_ ani Ranged_, wtedy korzystamy z refleksji
        else
        {
            // Szukamy zwykłego pola w klasie Stats
            FieldInfo field = stats.GetType().GetField(attributeName);

            // Jeżeli pole nie istnieje, kończymy metodę
            if (field == null)
            {
                Debug.Log($"Nie znaleziono pola '{attributeName}' w klasie Stats.");
                return;
            }

            // Zależnie od typu pola...
            if (field.FieldType == typeof(int) && textInput.GetComponent<UnityEngine.UI.Slider>() == null)
            {
                // int przez InputField
                int value = int.TryParse(textInput.GetComponent<TMP_InputField>().text, out int inputValue)
                            ? inputValue
                            : 0;

                field.SetValue(stats, value);

                if (attributeName == "Mag")
                {
                    DataManager.Instance.LoadAndUpdateSpells(); //Aktualizuje listę zaklęć
                }
            }
            else if (field.FieldType == typeof(int) && textInput.GetComponent<UnityEngine.UI.Slider>() != null)
            {
                // int przez Slider
                int value = (int)textInput.GetComponent<UnityEngine.UI.Slider>().value;
                field.SetValue(stats, value);
            }
            else if (field.FieldType == typeof(bool))
            {
                bool boolValue = textInput.GetComponent<UnityEngine.UI.Toggle>().isOn;
                field.SetValue(stats, boolValue);
            }
            else if (field.FieldType == typeof(string))
            {
                string value = textInput.GetComponent<TMP_InputField>().text;
                field.SetValue(stats, value);
            }
            else if (field.FieldType.IsEnum && textInput.GetComponent<TMP_Dropdown>() != null)
            {
                // Obsługa TMP_Dropdown dla enumów
                TMP_Dropdown dropdown = textInput.GetComponent<TMP_Dropdown>();
                Array enumValues = Enum.GetValues(field.FieldType);

                if (dropdown.value >= 0 && attributeName == "Size")
                {
                    stats.ChangeUnitSize(dropdown.value);
                }
                else if (dropdown.value >= 0 && dropdown.value < enumValues.Length)
                {
                    object selectedEnumValue = enumValues.GetValue(dropdown.value);
                    field.SetValue(stats, selectedEnumValue);
                }
            }
            else
            {
                Debug.Log($"Nie udało się zmienić wartości cechy '{attributeName}'.");
            }

            if (attributeName == "S" || attributeName == "Wt" || attributeName == "StrongBack" || attributeName == "Sturdy")
            {
                stats.CalculateMaxHealth();
                unit.DisplayUnitHealthPoints();
                stats.MaxEncumbrance = (stats.S + stats.Wt) / 10 + stats.StrongBack + (stats.Sturdy * 2);
            }
            else if (attributeName == "Hardy" || attributeName == "SW") // Talent Twardziel
            {
                stats.CalculateMaxHealth();
                unit.DisplayUnitHealthPoints();
            }
            else if(attributeName == "NaturalArmor")
            {
                InventoryManager.Instance.CheckForEquippedWeapons();
            }
            else if (attributeName == "Name")
            {
                unit.DisplayUnitName();
            }
        }

        UpdateUnitPanel(Unit.SelectedUnit);

        if (!SaveAndLoadManager.Instance.IsLoading)
        {
            //Aktualizuje pasek przewagi w bitwie
            int newOverall = stats.CalculateOverall();
            int difference = newOverall - stats.Overall;

            InitiativeQueueManager.Instance.CalculateDominance();
        }
    }
    #endregion

    #region Update unit panel (at the top of the screen)
    public void UpdateUnitPanel(GameObject unit)
    {
        if (unit == null || SaveAndLoadManager.Instance.IsLoading)
        {
            _unitPanel.SetActive(false);
            return;
        }
        else
        {
            _unitPanel.SetActive(true);

            //W trybie ukrywania statystyk, panel wrogich jednostek pozostaje wyłączony
            if (GameManager.IsStatsHidingMode && unit.CompareTag("EnemyUnit"))
            {
                _unitPanel.transform.Find("VerticalLayoutGroup/Stats_Panel/Stats_display").gameObject.SetActive(false);
            }
            else
            {
                _unitPanel.transform.Find("VerticalLayoutGroup/Stats_Panel/Stats_display").gameObject.SetActive(true);
            }

            //Ukrywa lub pokazuje nazwę jednostki w panelu
            if (GameManager.IsNamesHidingMode && !MultiScreenDisplay.Instance.PlayersCamera.gameObject.activeSelf && Display.displays.Length == 1)
            {
                _unitPanel.transform.Find("Name_input").gameObject.SetActive(false);
            }
            else
            {
                _unitPanel.transform.Find("Name_input").gameObject.SetActive(true);
            }
        }

        Stats stats = unit.GetComponent<Stats>();

        if (stats.MagicLanguage > 0)
        {
            _spellbookButton.interactable = true;
            DataManager.Instance.LoadAndUpdateSpells(); //Aktualizuje listę zaklęć, które może rzucić jednostka

            if (unit.GetComponent<Spell>() == null)
            {
                unit.AddComponent<Spell>();
            }
        }
        else
        {
            _spellbookButton.interactable = false;
            _spellListPanel.SetActive(false);
        }

        //_nameDisplay.text = stats.Name;
        _raceDisplay.text = stats.Race;

        _healthDisplay.text = stats.TempHealth + "/" + stats.MaxHealth;
        _healthBar.maxValue = stats.MaxHealth;
        _healthBar.value = stats.TempHealth;
        UpdateHealthBarColor(stats.TempHealth, stats.MaxHealth, _healthBar.transform.Find("Fill Area/Fill").GetComponent<UnityEngine.UI.Image>());

        _tokenDisplay.sprite = unit.transform.Find("Token").GetComponent<SpriteRenderer>().sprite;
        _tokenBorder.color = unit.tag == "EnemyUnit" ? new Color(0.59f, 0.1f, 0.19f, 1.0f) : new Color(0f, 0.54f, 0.17f, 1.0f);

        InventoryManager.Instance.DisplayEquippedWeaponsName();

        RoundsManager.Instance.DisplayActionsLeft();

        CombatManager.Instance.UpdateFrenzyButtonColor();
        CombatManager.Instance.UpdateDefensiveStanceButtonColor();
        CombatManager.Instance.UpdateAimButtonColor();
        MountsManager.Instance.UpdateMountButtonColor();

        LoadAttributes(unit);
    }

    private void UpdateHealthBarColor(float tempHealth, float maxHealth, UnityEngine.UI.Image image)
    {
        float percentage = tempHealth / maxHealth * 100;

        if (percentage <= 30)
        {
            image.color = new Color(0.81f, 0f, 0.137f); // Kolor czerwony, jeśli wartość <= 30%
        }
        else if (percentage > 30 && percentage <= 70)
        {
            image.color = new Color(1f, 0.6f, 0f); // Kolor pomarańczowy, jeśli wartość jest między 31% a 70%
        }
        else
        {
            image.color = new Color(0.3f, 0.65f, 0.125f); // Kolor zielony, jeśli wartość > 70%
        }
    }

    public void LoadAttributesByButtonClick()
    {
        if (Unit.SelectedUnit == null) return;

        GameObject unit = Unit.SelectedUnit;
        LoadAttributes(unit);
    }

    public void LoadAchievementsByButtonClick()
    {
        if (Unit.SelectedUnit == null) return;

        GameObject unit = Unit.SelectedUnit;
        LoadAchievements(unit);
    }

    public void LoadAttributes(GameObject unit)
    {
        GameObject[] attributeInputFields = GameObject.FindGameObjectsWithTag("Attribute");

        foreach (var inputField in attributeInputFields)
        {
            string attributeName = inputField.name.Replace("_input", "");
            Stats stats = unit.GetComponent<Stats>();

            if (attributeName.StartsWith("Melee_"))
            {
                string categoryName = attributeName.Replace("Melee_", "");
                if (Enum.TryParse<MeleeCategory>(categoryName, out var category))
                {
                    int meleeValue = stats.Melee.ContainsKey(category) ? stats.Melee[category] : 0;
                    SetInputFieldValue(inputField, meleeValue);
                }
                continue;
            }
            else if (attributeName.StartsWith("Ranged_"))
            {
                string categoryName = attributeName.Replace("Ranged_", "");
                if (Enum.TryParse<RangedCategory>(categoryName, out var category))
                {
                    int rangedValue = stats.Ranged.ContainsKey(category) ? stats.Ranged[category] : 0;
                    SetInputFieldValue(inputField, rangedValue);
                }
                continue;
            }

            FieldInfo field = stats.GetType().GetField(attributeName);
            if (field == null)
                continue;

            object value = field.GetValue(stats);

            if (field.FieldType == typeof(int))
            {
                SetInputFieldValue(inputField, (int)value);
            }
            else if (field.FieldType == typeof(bool))
            {
                SetToggleValue(inputField, (bool)value);
            }
            else if (field.FieldType == typeof(string))
            {
                SetInputFieldValue(inputField, (string)value);
            }
            else if (field.FieldType.IsEnum)
            {
                SetDropdownValue(inputField, value);
                continue;
            }

            if (attributeName == "Initiative")
            {
                InitiativeQueueManager.Instance.InitiativeQueue[unit.GetComponent<Unit>()] = stats.Initiative;
                InitiativeQueueManager.Instance.UpdateInitiativeQueue();
            }
        }
    }

    private void SetInputFieldValue(GameObject inputField, int value)
    {
        var tmpField = inputField.GetComponent<TMPro.TMP_InputField>();
        if (tmpField != null) tmpField.text = value.ToString();

        var slider = inputField.GetComponent<UnityEngine.UI.Slider>();
        if (slider != null) slider.value = value;
    }
    private void SetInputFieldValue(GameObject inputField, string value)
    {
        var tmpField = inputField.GetComponent<TMPro.TMP_InputField>();
        if (tmpField != null) tmpField.text = value;
    }
    private void SetToggleValue(GameObject inputField, bool value)
    {
        var toggle = inputField.GetComponent<UnityEngine.UI.Toggle>();
        if (toggle != null) toggle.isOn = value;
    }
    private void SetDropdownValue(GameObject inputField, object enumValue)
    {
        var dropdown = inputField.GetComponent<TMPro.TMP_Dropdown>();
        if (dropdown != null)
        {
            int index = Array.IndexOf(Enum.GetValues(enumValue.GetType()), enumValue);
            if (index >= 0) dropdown.value = index;
        }
    }

    public void ChangeTemporaryHealthPoints(int amount)
    {
        if (Unit.SelectedUnit == null) return;

        Unit.SelectedUnit.GetComponent<Stats>().TempHealth += amount;

        Unit.SelectedUnit.GetComponent<Unit>().DisplayUnitHealthPoints();

        UpdateUnitPanel(Unit.SelectedUnit);
    }

    public void LoadAchievements(GameObject unit)
    {
        // Wyszukuje wszystkie pola tekstowe i przyciski do ustalania statystyk postaci wewnatrz gry
        GameObject[] achievementGameObjects = GameObject.FindGameObjectsWithTag("Achievement");

        foreach (var obj in achievementGameObjects)
        {
            string achivementName = obj.name.Replace("_text", "");
            FieldInfo field = unit.GetComponent<Stats>().GetType().GetField(achivementName);

            if (field == null) continue;

            // Jeśli znajdzie takie pole, to zmienia wartość wyświetlanego tekstu na wartość tej cechy
            if (field.FieldType == typeof(int)) // to działa dla cech opisywanych wartościami int
            {
                int value = (int)field.GetValue(unit.GetComponent<Stats>());

                if (obj.GetComponent<TMP_Text>() != null)
                {
                    obj.GetComponent<TMP_Text>().text = value.ToString();
                }
            }
            else if (field.FieldType == typeof(string)) // to działa dla cech opisywanych wartościami string
            {
                string value = (string)field.GetValue(unit.GetComponent<Stats>());

                if (obj.GetComponent<TMP_Text>() != null)
                {
                    obj.GetComponent<TMP_Text>().text = value;
                }
            }
        }
    }
    #endregion

    #region Fear and terror mechanics
    public void LookForScaryUnits()
    {
        List<Unit> allEnemies = new List<Unit>();
        List<Unit> allPlayers = new List<Unit>();

        foreach (var pair in InitiativeQueueManager.Instance.InitiativeQueue)
        {
            if (pair.Key.CompareTag("EnemyUnit"))
            {
                allEnemies.Add(pair.Key);
            }
            else if (pair.Key.CompareTag("PlayerUnit"))
            {
                allPlayers.Add(pair.Key);
            }
        }

        // Sprawdzamy strach graczy od wrogów i wrogów od graczy
        ProcessFearAndTerror(allPlayers, allEnemies); // Gracze boją się wrogów
        ProcessFearAndTerror(allEnemies, allPlayers); // Wrogowie boją się graczy
    }

    private void ProcessFearAndTerror(List<Unit> checkingUnits, List<Unit> opposingUnits)
    {
        foreach (Unit unit in checkingUnits)
        {
            if (unit.IsFearTestPassed || unit.Broken > 0) continue;

            int highestTerrorValue = 0;
            Unit highestTerrorUnit = null;

            int highestFearValue = 0;
            Unit highestFearUnit = null;

            foreach (Unit opponent in opposingUnits)
            {
                int sizeDifference = opponent.GetComponent<Stats>().Size - unit.GetComponent<Stats>().Size;

                if ((sizeDifference > 1 || opponent.GetComponent<Stats>().Terror > 0) && !unit.IsTerrorTestPassed)
                {
                    int value = sizeDifference > opponent.GetComponent<Stats>().Terror ? sizeDifference : opponent.GetComponent<Stats>().Terror;

                    if (value > highestTerrorValue)
                    {
                        highestTerrorValue = value;
                        highestTerrorUnit = opponent;
                    }
                }
                else if (sizeDifference == 1 || opponent.GetComponent<Stats>().Fear > 0)
                {

                    int value = sizeDifference > opponent.GetComponent<Stats>().Fear ? sizeDifference : opponent.GetComponent<Stats>().Fear;

                    if (value > highestTerrorValue)
                    {
                        highestFearValue = value;
                        highestFearUnit = opponent;
                    }
                }
            }

            if (highestTerrorUnit != null)
            {
                unit.FearedUnits.Add(highestTerrorUnit);
                StartCoroutine(TerrorRoll(unit, highestTerrorUnit.GetComponent<Stats>().Name, highestTerrorValue));
            }
            else if (highestFearUnit != null)
            {
                unit.FearedUnits.Add(highestFearUnit);
                StartCoroutine(FearRoll(unit, highestFearUnit.GetComponent<Stats>().Name, 1));
            }
        }
    }

    public IEnumerator FearRoll(Unit unit, string opponentName, int value = 0)
    {
        if (unit.IsFearTestPassed || unit.GetComponent<Stats>().ImmunityToPsychology) yield break;

        int rollResult = 0;
        Stats stats = unit.GetComponent<Stats>();

        if (!GameManager.IsAutoDiceRollingMode && unit.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "opanowanie (strach)", result => rollResult = result));
            if (rollResult == 0) yield break;
        }
        else
        {
            rollResult = UnityEngine.Random.Range(1, 101);
        }

        int[] test = DiceRollManager.Instance.TestSkill("SW", stats, "Cool", 0, rollResult);
        int successLevel = test[1];

        // Zaktualizowanie listy wszystkich jednostek, których się boi
        foreach (var pair in InitiativeQueueManager.Instance.InitiativeQueue)
        {
            if(pair.Key == null) continue;

            if (!pair.Key.CompareTag(unit.tag))
            {
                if ((pair.Key.GetComponent<Stats>().Fear != 0 && pair.Key.GetComponent<Stats>().Fear > successLevel) || (pair.Key.GetComponent<Stats>().Size > stats.Size && pair.Key.GetComponent<Stats>().Size - stats.Size > successLevel))
                {
                    unit.FearedUnits.Add(pair.Key);
                }
                else if (unit.FearedUnits.Contains(pair.Key))
                {
                    unit.FearedUnits.Remove(pair.Key);
                }
            }
        }

        if (value != 0) unit.FearLevel = value;

        unit.FearLevel = Math.Max(0, unit.FearLevel - successLevel);

        // Upewnia się, że FearLevel nie przekroczy wartości value
        unit.FearLevel = Math.Min(unit.FearLevel, value);

        // Tworzenie stringa z nazwami przeciwników, których jednostka się boi
        string opponentsNames = unit.FearedUnits.Count > 0 ? string.Join(", ", unit.FearedUnits.Where(u => u != null).Select(u => u.GetComponent<Stats>().Name)) : "";

        if (unit.FearLevel == 0)
        {
            Debug.Log($"<color=#FF7F50>{stats.Name} zdał/a test strachu przed: {opponentName}.</color>");
            unit.IsFearTestPassed = true;
        }
        else
        {
            Debug.Log($"<color=#FF7F50>{stats.Name} nie zdał/a testu strachu przed: {opponentsNames}. Pozostałe poziomy strachu: {unit.FearLevel}</color>");
            Debug.Log($"<color=#FF7F50>Zbliżenie się źródła strachu lub próba zbliżenia się do niego wymaga wykonania Testu Opanowania +0. Wykonaj go samodzielnie. Niezdany test zwiększa poziom paniki o 1.</color>");
        }
    }

    public IEnumerator TerrorRoll(Unit unit, string opponentName, int value)
    {
        if (unit.IsTerrorTestPassed || unit.GetComponent<Stats>().ImmunityToPsychology) yield break;

        int rollResult = 0;
        Stats stats = unit.GetComponent<Stats>();

        if (!GameManager.IsAutoDiceRollingMode && unit.CompareTag("PlayerUnit"))
        {
            yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "opanowanie (groza)", result => rollResult = result));
            if (rollResult == 0) yield break;
        }
        else
        {
            rollResult = UnityEngine.Random.Range(1, 101);
        }

        int[] test = DiceRollManager.Instance.TestSkill("SW", stats, "Cool", 0, rollResult);
        int successValue = test[0];
        int successLevel = test[1];

        if(successValue > 0)
        {
            unit.IsTerrorTestPassed = true;
            Debug.Log($"<color=#FF7F50>{stats.Name} zdał/a test grozy przed {opponentName}. Następuje test strachu.</color>");
            StartCoroutine(FearRoll(unit, opponentName, value));
        }
        else
        {
            unit.Broken += value - successLevel;
            Debug.Log($"<color=#FF7F50>{stats.Name} nie zdał/a test grozy przed {opponentName}. Poziom paniki wzrasta o {value - successLevel}</color>");
        }
    }
    #endregion

    public bool BothTeamsExist()
    {
        bool enemyUnitExists = false;
        bool playerUnitExists = false;

        foreach (var pair in InitiativeQueueManager.Instance.InitiativeQueue)
        {
            Stats unitStats = pair.Key.GetComponent<Stats>();

            if (pair.Key.CompareTag("EnemyUnit")) enemyUnitExists = true;
            if (pair.Key.CompareTag("PlayerUnit")) playerUnitExists = true;
        }

        if (enemyUnitExists && playerUnitExists) return true;
        else return false;
    }
}
