using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int UnitId; // Unikalny Id jednostki

    public static GameObject SelectedUnit;
    public static GameObject LastSelectedUnit;
    public string TokenFilePath;
    public Color DefaultColor;
    public Color HighlightColor;

    public bool IsSelected = false;
    public bool IsTurnFinished; // Określa, czy postać zakończyła swoją turę (bo mogła to zrobić, np. zostawiając jedną akcję)
    public bool IsRunning; // Biegnie
    public bool IsCharging; // Szarżuje
    public bool IsRetreating; // Wycofuje się
    public bool IsFrenzy; // Jest w trakcie szału bojowego

    [Header("Stany")]
    public int Ablaze; // Podpalenie
    public int Bleeding; // Krwawienie
    public int Blinded; // Oślepienie
    public int Broken; // Panika
    public int Deafened; // Ogłuszenie
    public int Entangled; // Pochwycenie
    public int Fatiqued; // Wyczerpanie
    public int Poison; // Zatrucie
    public int PoisonTestModifier; // Modyfikator do testów przeciw zatruciu (zależny od mocy trucizny)
    public bool Prone; // Powalenie
    public int Stunned; // Oszołomienie
    public bool Surprised; // Zaskoczenie
    public bool Unconscious; // Utrata Przytomności

    public int EntangledUnitId; // Cel unieruchomienia
    public int FeintedUnitId; // Cel finty

    public bool IsFearTestPassed; // Zdał test strachu
    public bool IsTerrorTestPassed; // Zdał test grozy
    public int FearLevel; // Poziom strachu
    public HashSet<Unit> FearedUnits = new HashSet<Unit>(); // Lista jednostek, których się boi

    //STARE
    public int SpellDuration; // Czas trwania zaklęcia mającego wpływ na tą jednostkę

    [Header("Modyfikatory")]
    public int AimingBonus;
    public int ChannelingModifier; // Poziom mocy zebrany poprzez splatanie magii
    public int DefensiveBonus;
    public int FeintModifier; // Modyfikator za fintę

    [Header("Dostępne działania")]
    public bool CanMove = true;
    public bool CanDoAction = true;
    public bool CanCastSpell = false;
    public bool CanDispell = false;

    public Stats Stats;
    public TMP_Text NameDisplay;
    public TMP_Text HealthDisplay;

    public Stats LastAttackerStats; // Ostatni przeciwnik, który zadał obrażenia tej jednostce (jest to niezbędne do aktualizowania osiągnięcia "Najsilniejszy pokonany przeciwnik" poza trybem automatycznej śmierci)

    public int MountId;
    public Unit Mount; // Wierzchowiec
    public bool IsMounted; // Zmienna określająca, czy jednostka w danej chwili dosiada wierzchowca, czy nie
    public bool HasRider; // Zmienna określająca, czy jednostka w danej chwili jest przez kogoś dosiadana

    void Start()
    {
        Stats = GetComponent<Stats>();

        DisplayUnitName();

        StartCoroutine(MovementManager.Instance.UpdateMovementRange(1, this));

        if (Stats.Name.Contains(Stats.Race)) // DO POKMINIENIA, JAKI INNY WARUNEK DAĆ, BO TEN NIE JEST IDEALNY, BO KTOŚ MOŻE NAZWAĆ ZAPISANEGO GOBLINA NP. "FAJNY GOBLIN"
        {
            Stats.TempHealth = Stats.MaxHealth;
        }

        DisplayUnitHealthPoints();

        //Aktualizuje kolejkę inicjatywy
        InitiativeQueueManager.Instance.UpdateInitiativeQueue();
    }
    private void OnMouseUp()
    {
        if (GameManager.Instance.IsPointerOverUI() || GameManager.IsMapHidingMode || UnitsManager.IsMultipleUnitsSelecting || MovementManager.Instance.IsMoving) return;

        SelectUnit();
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButton(1) && SelectedUnit != null && SelectedUnit != this.gameObject && !MagicManager.IsTargetSelecting)
        {
            StartCoroutine(CombatManager.Instance.OpenHitLocationPanel());
        }
        else if (Input.GetMouseButtonUp(1) && SelectedUnit != null && SelectedUnit != this.gameObject && !MagicManager.IsTargetSelecting)
        {
            //Sprawdza, czy atakowanym jest nasz sojusznik i czy tryb Friendly Fire jest aktywny
            if (GameManager.IsFriendlyFire == false && this.gameObject.CompareTag(SelectedUnit.tag))
            {
                Debug.Log("Nie możesz atakować swoich sojuszników. Jest to możliwe tylko w trybie Friendly Fire.");
                return;
            }

            if (Unconscious && SelectedUnit != null) // Gdy jednostka jest nieprzytomna to atak automatycznie oznacza śmierć
            {
                // Sprawdzamy, czy atakujący może wykonać akcję
                if (!SelectedUnit.GetComponent<Unit>().CanDoAction)
                {
                    Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
                    return;
                }

                RoundsManager.Instance.DoAction(SelectedUnit.GetComponent<Unit>());
                Debug.Log($"{SelectedUnit.GetComponent<Stats>().Name} atakuje {Stats.Name} w stanie nieprzytomności. <color=red>{Stats.Name} ginie.</color>");

                LastAttackerStats = SelectedUnit.GetComponent<Stats>();

                if (GameManager.IsAutoKillMode && !(GameManager.IsStatsHidingMode && Stats.gameObject.CompareTag("PlayerUnit")))
                {
                    CombatManager.Instance.HandleDeath(Stats, gameObject, SelectedUnit.GetComponent<Stats>());
                }
            }
            else if (CombatManager.Instance.AttackTypes["Grappling"]) // Zapasy
            {
                CombatManager.Instance.Grappling(SelectedUnit.GetComponent<Unit>(), this);
            }
            else if (IsMounted && Mount != null)
            {
                StartCoroutine(CombatManager.Instance.SelectRiderOrMount(this));
            }
            else // Atak
            {
                CombatManager.Instance.Attack(SelectedUnit.GetComponent<Unit>(), this, false);
            }
        }
        else if (Input.GetMouseButtonUp(1) && SelectedUnit != null && MagicManager.IsTargetSelecting)
        {
            //StartCoroutine(MagicManager.Instance.CastSpell(this.gameObject));
            MagicManager.Instance.Targets.Add(this.gameObject);

            if(SelectedUnit.GetComponent<Spell>() != null && MagicManager.Instance.Targets.Count < SelectedUnit.GetComponent<Spell>().Targets)
            {
                Debug.Log($"Wskaż kolejny cel. Musisz wybrać {SelectedUnit.GetComponent<Spell>().Targets} unikalnych celów. Wybrano {MagicManager.Instance.Targets.Count}.");
            }
        }
    }
    public void SelectUnit()
    {
        if (!gameObject.activeSelf) return;

        if (SelectedUnit == null)
        {
            SelectedUnit = this.gameObject;

            //Odświeża listę ekwipunku
            InventoryManager.Instance.InventoryScrollViewContent.GetComponent<CustomDropdown>().SelectedIndex = 0;
            InventoryManager.Instance.UpdateInventoryDropdown(SelectedUnit.GetComponent<Inventory>().AllWeapons, true);
            InventoryManager.Instance.DisplayEncumbrance(Stats);

            CombatManager.Instance.SetActionsButtonsInteractable();
        }
        else if (SelectedUnit == this.gameObject)
        {
            CombatManager.Instance.ChangeAttackType(); // Resetuje wybrany typ ataku
            StartCoroutine(MovementManager.Instance.UpdateMovementRange(1)); //Resetuje szarżę lub bieg, jeśli były aktywne
            MovementManager.Instance.Retreat(false); //Resetuje bezpieczny odwrót

            //Zamyka aktywne panele
            GameManager.Instance.HideActivePanels();

            LastSelectedUnit = SelectedUnit;
            SelectedUnit = null;
        }
        else
        {
            SelectedUnit.GetComponent<Unit>().IsSelected = false;

            ChangeUnitColor(SelectedUnit);
            LastSelectedUnit = SelectedUnit;
            SelectedUnit = this.gameObject;

            CombatManager.Instance.ChangeAttackType(); // Resetuje wybrany typ ataku
            StartCoroutine(MovementManager.Instance.UpdateMovementRange(1)); //Resetuje szarżę lub bieg, jeśli były aktywne   
            MovementManager.Instance.Retreat(false); //Resetuje bezpieczny odwrót    

            CombatManager.Instance.UpdateAimButtonColor();
            CombatManager.Instance.UpdateDefensiveStanceButtonColor();
            CombatManager.Instance.UpdateFrenzyButtonColor();

            //Odświeża listę ekwipunku
            InventoryManager.Instance.InventoryScrollViewContent.GetComponent<CustomDropdown>().SelectedIndex = 0;
            InventoryManager.Instance.UpdateInventoryDropdown(SelectedUnit.GetComponent<Inventory>().AllWeapons, true);
            InventoryManager.Instance.DisplayEncumbrance(Stats);
        }
        IsSelected = !IsSelected;
        ChangeUnitColor(this.gameObject);

        //// Jeżeli MountId nie jest puste, ale Mount tak, znajduje wierzchowca na podstawie Id
        //if (MountId != 0 && Mount == null)
        //{
        //    Mount = MountsManager.Instance.FindMountById(MountId);
        //}

        // Aktualizuje szybkość, jeśli jednostka dosiada wierzchowca
        if (IsMounted && Mount != null)
        {
            Stats.TempSz = Mount.GetComponent<Stats>().TempSz;
        }

        GridManager.Instance.HighlightTilesInMovementRange(Stats);

        //Wczytuje osiągnięcia jednostki
        UnitsManager.Instance.LoadAchievements(SelectedUnit);

        //Aktualizuje panel ze statystykami jednostki
        UnitsManager.Instance.UpdateUnitPanel(SelectedUnit);
        StatesManager.Instance.LoadUnitStates();

        //Zaznacza lub odznacza jednostkę na kolejce inicjatywy
        InitiativeQueueManager.Instance.UpdateInitiativeQueue();

        MountsManager.Instance.UpdateMountButtonColor();

        if (Broken > 0)
        {
            Debug.Log($"<color=#FF7F50>{Stats.Name} jest w stanie paniki. Poziom paniki: {Broken}</color>");
        }

        //Zresetowanie rzucania zaklęć
        MagicManager.Instance.ResetSpellCasting();
    }

    //public void ChangeUnitColor(GameObject unit)
    //{
    //    Renderer renderer = unit.GetComponent<Renderer>();

    //    //Ustawia wartość HighlightColor na jaśniejszą wersję DefaultColor. Trzeci parametr określa ilość koloru białego w całości.
    //    HighlightColor = Color.Lerp(DefaultColor, Color.yellow, 0.3f);

    //    renderer.material.color = IsSelected ? unit.GetComponent<Unit>().HighlightColor : unit.GetComponent<Unit>().DefaultColor;

    //    //Aktualizuje kolor tokena, jeśli nie jest wgrany żaden obraz
    //    if (unit.GetComponent<Unit>().TokenFilePath.Length < 1)
    //    {
    //        unit.transform.Find("Token").GetComponent<SpriteRenderer>().material.color = IsSelected ? unit.GetComponent<Unit>().HighlightColor : unit.GetComponent<Unit>().DefaultColor;
    //    }
    //}

    public void ChangeUnitColor(GameObject unit)
    {
        Material mat = unit.GetComponent<Renderer>().material;
        Unit unitComponent = unit.GetComponent<Unit>();

        // Ustawia kolor główny (diffuse)
        mat.SetColor("_Color", unitComponent.DefaultColor);

        // Włącza emisję przy zaznaczeniu
        Color emission = IsSelected ? unitComponent.DefaultColor * 1f : Color.black;
        mat.SetColor("_EmissionColor", emission);

        // Jeśli nie ma obrazka tokena, ustaw też jego kolor i emisję
        if (unitComponent.TokenFilePath.Length < 1)
        {
            SpriteRenderer tokenRenderer = unit.transform.Find("Token").GetComponent<SpriteRenderer>();
            tokenRenderer.material = mat; ;

            tokenRenderer.material.SetColor("_Color", unitComponent.DefaultColor);
            tokenRenderer.material.SetColor("_EmissionColor", emission * 0.8f);
        }
    }

    public void DisplayUnitName()
    {
        if (NameDisplay == null) return;

        if (Stats.Name != null && Stats.Name.Length > 1)
        {
            NameDisplay.text = Stats.Name;
        }
        else
        {
            NameDisplay.text = this.gameObject.name;
            Stats.Name = this.gameObject.name;
        }

        if (GameManager.IsNamesHidingMode)
        {
            HideUnitName();
        }
    }

    public void HideUnitName()
    {
        if (NameDisplay == null) return;

        NameDisplay.text = "";
    }

    public void DisplayUnitHealthPoints()
    {
        if (HealthDisplay == null) return;

        HealthDisplay.text = Stats.TempHealth + "/" + Stats.MaxHealth;

        if (GameManager.IsHealthPointsHidingMode || GameManager.IsStatsHidingMode && this.gameObject.CompareTag("EnemyUnit"))
        {
            HideUnitHealthPoints();
        }
        else
        {
            ResetUnitHealthState();
        }
    }

    public void HideUnitHealthPoints()
    {
        UpdateUnitHealthState();

        if (HealthDisplay == null) return;

        HealthDisplay.text = "";
    }

    private void UpdateUnitHealthState()
    {
        ResetUnitHealthState();

        //Wyświetla symbol obrazujący stan zdrowia jednostki
        if (Stats.TempHealth < 0)
        {
            gameObject.transform.Find("Canvas/Dead_image").gameObject.SetActive(true);
        }
        else if (Stats.TempHealth <= Stats.MaxHealth / 3)
        {
            gameObject.transform.Find("Canvas/Heavy_wounded_image").gameObject.SetActive(true);
        }
        else if (Stats.TempHealth < Stats.MaxHealth)
        {
            gameObject.transform.Find("Canvas/Wounded_image").gameObject.SetActive(true);
        }
    }

    private void ResetUnitHealthState()
    {
        transform.Find("Canvas/Dead_image").gameObject.SetActive(false);
        transform.Find("Canvas/Heavy_wounded_image").gameObject.SetActive(false);
        transform.Find("Canvas/Wounded_image").gameObject.SetActive(false);
    }
}
