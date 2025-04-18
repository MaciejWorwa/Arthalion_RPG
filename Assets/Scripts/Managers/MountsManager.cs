using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;

public class MountsManager : MonoBehaviour
{
    // Prywatne statyczne pole przechowuj¹ce instancjê
    private static MountsManager instance;

    // Publiczny dostêp do instancji
    public static MountsManager Instance
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
            // Jeœli instancja ju¿ istnieje, a próbujemy utworzyæ kolejn¹, niszczymy nadmiarow¹
            Destroy(gameObject);
        }
    }

    public static Unit SelectedMount;
    public Unit ActiveMount;
    public Transform MountsScrollViewContent;
    [SerializeField] private GameObject _mountOptionPrefab; // Prefab odpowiadaj¹cy ka¿dej jednostce na liœcie wierzchowców
    private Color _defaultColor = new Color(0f, 0f, 0f, 0f); // Domyœlny kolor przycisku
    private Color _selectedColor = new Color(0f, 0f, 0f, 0.5f); // Kolor wybranego przycisku (zaznaczonej jednostki)
    private Color _activeColor = new Color(0.15f, 1f, 0.45f, 0.2f); // Kolor aktywnego przycisku (aktualnie dosiadany wierzchowiec)
    private Color _selectedActiveColor = new Color(0.08f, 0.5f, 0.22f, 0.5f); // Kolor wybranego przycisku, gdy jednoczeœnie jest to obecnie dosiadany wierzchowiec

    [SerializeField] private UnityEngine.UI.Button _mountButton;
    [SerializeField] private GameObject _mountsPanel; // Panel do wyboru wierzchowca

    public void DisplayMountsList()
    {
        if (Unit.SelectedUnit == null) return;

        // Resetuje wyœwietlan¹ kolejkê, usuwaj¹c wszystkie obiekty "dzieci"
        ResetScrollViewContent(MountsScrollViewContent);

        ActiveMount = null;

        // Ustala wyœwietlan¹ kolejkê inicjatywy
        foreach (Unit unit in UnitsManager.Instance.AllUnits)
        {
            if(unit.CompareTag(Unit.SelectedUnit.tag) && unit != Unit.SelectedUnit.GetComponent<Unit>() && Vector2.Distance(unit.transform.position, Unit.SelectedUnit.transform.position) < 1.5f && unit.GetComponent<Stats>().Size > Unit.SelectedUnit.GetComponent<Stats>().Size && !unit.HasRider)
            {
                // Dodaje jednostkê do g³ównej kolejki ScrollViewContent
                GameObject optionObj = CreateOption(unit, MountsScrollViewContent);

                // Sprawdza, czy jest to obecnie dosiadany mount przez aktywn¹ jednostkê
                if (Unit.SelectedUnit.GetComponent<Unit>().Mount == unit && Unit.SelectedUnit.GetComponent<Unit>().IsMounted)
                {
                    ActiveMount = unit;
                    SetOptionColor(optionObj, _activeColor);
                }

                // Wyró¿nia zaznaczon¹ jednostkê
                if (SelectedMount != null && unit == SelectedMount)
                {
                    Color selectedColor = unit == ActiveMount ? _selectedActiveColor : _selectedColor;
                    SetOptionColor(optionObj, selectedColor);
                }
                else if (unit != ActiveMount)
                {
                    SetOptionColor(optionObj, _defaultColor);
                }
            } 
        }
    }

    private void ResetScrollViewContent(Transform scrollViewContent)
    {
        for (int i = scrollViewContent.childCount - 1; i >= 0; i--)
        {
            Transform child = scrollViewContent.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    private GameObject CreateOption(Unit unit, Transform scrollViewContent)
    {
        GameObject optionObj = Instantiate(_mountOptionPrefab, scrollViewContent);

        // Odniesienie do nazwy postaci
        TextMeshProUGUI nameText = optionObj.transform.Find("Name_Text").GetComponent<TextMeshProUGUI>();
        nameText.text = unit.GetComponent<Stats>().Name;

        return optionObj;
    }

    private void SetOptionColor(GameObject optionObj, Color color)
    {
        optionObj.GetComponent<UnityEngine.UI.Image>().color = color;
    }

    public void GetOnSelectedMount()
    {
        if(Unit.SelectedUnit != null)
        {
            Unit unit = Unit.SelectedUnit.GetComponent<Unit>();

            if (unit.Mount != SelectedMount)
            {
                unit.Mount = SelectedMount;
                unit.MountId = SelectedMount.UnitId;
                GetOnMount();
                _mountsPanel.SetActive(false);
            }
            //else
            //{
            //    unit.Mount = null;
            //    unit.MountId = 0;
            //}

            DisplayMountsList();
            UpdateMountButtonColor();
            GridManager.Instance.HighlightTilesInMovementRange(unit.Stats);
        }
    }

    public void GetOnMountByButton()
    {
        GetOnMount();
    }

    public void GetOnMount(Unit unit = null, bool isLoading = false)
    {
        if (unit == null && Unit.SelectedUnit == null) return;
        if(unit == null)
        {
            unit = Unit.SelectedUnit.GetComponent<Unit>();
        }

        if(isLoading)
        {
            if (!unit.IsMounted) return;
            unit.Mount = FindMountById(unit.MountId);
            unit.Mount.HasRider = true;

            unit.Mount.transform.position = unit.transform.position;
            unit.Mount.transform.SetParent(unit.transform);
            InitiativeQueueManager.Instance.RemoveUnitFromInitiativeQueue(unit.Mount);
            unit.Mount.gameObject.SetActive(false);

            unit.Stats.TempSz = unit.Mount.GetComponent<Stats>().Sz;

            return;
        }

        // Jeœli nie znalaz³o, otwiera panel do wyboru wierzchowca
        if (unit.Mount == null)
        {
            if (MountsScrollViewContent.childCount == 0)
            {
                Debug.Log($"Musisz staæ obok potencjalnego wierzchowca, aby móc go dosi¹œæ.");
            }
            else _mountsPanel.SetActive(true);

            return;
        }

        unit.IsMounted = !unit.IsMounted;
        unit.Mount.HasRider = !unit.Mount.HasRider;

        if(unit.IsMounted)
        {
            unit.Mount.transform.position = unit.transform.position;
            unit.Mount.transform.SetParent(unit.transform);
            InitiativeQueueManager.Instance.RemoveUnitFromInitiativeQueue(unit.Mount);
            unit.Mount.gameObject.SetActive(false);

            unit.Stats.TempSz = unit.Mount.GetComponent<Stats>().Sz;

            Debug.Log($"{unit.Stats.Name} dosiad³/a {unit.Mount.GetComponent<Stats>().Name}.");
        }
        else
        {
            Vector2 riderPos = unit.transform.position;
            Vector2[] adjacentPositions = {
                    riderPos + Vector2.right,
                    riderPos + Vector2.left,
                    riderPos + Vector2.up,
                    riderPos + Vector2.down,
                    riderPos + new Vector2(1, 1),
                    riderPos + new Vector2(-1, -1),
                    riderPos + new Vector2(-1, 1),
                    riderPos + new Vector2(1, -1)
                };

            Vector2 newMountPosition = riderPos;
            foreach (var pos in adjacentPositions)
            {
                Collider2D[] colliders = Physics2D.OverlapPointAll(pos);
                foreach(Collider2D collider in colliders)
                {
                    if (collider != null && collider.gameObject.CompareTag("Tile") && !collider.gameObject.GetComponent<Tile>().IsOccupied)
                    {
                        newMountPosition = pos;
                        break;
                    }
                }
                if (newMountPosition != riderPos) break;
            }
            unit.Mount.transform.position = newMountPosition;

            unit.Mount.transform.SetParent(null);
            InitiativeQueueManager.Instance.AddUnitToInitiativeQueue(unit.Mount);
            unit.Mount.gameObject.SetActive(true);

            unit.Stats.TempSz = unit.Stats.Sz;
            Debug.Log($"{unit.Stats.Name} zsiad³/a z {unit.Mount.GetComponent<Stats>().Name}.");
            unit.Mount = null;
            unit.MountId = 0;
        }

        UpdateMountButtonColor();
        InitiativeQueueManager.Instance.UpdateInitiativeQueue();
        GridManager.Instance.CheckTileOccupancy();
        GridManager.Instance.HighlightTilesInMovementRange(unit.Stats);
    }

    public void LoadMountedUnits()
    {
        foreach (var unit in UnitsManager.Instance.AllUnits)
        {
            // Ustawiamy wierzchowce na niewidoczne
            if (unit.HasRider)
            {
                unit.gameObject.SetActive(false);
                Unit rider = null;
                foreach (Unit u in UnitsManager.Instance.AllUnits)
                {
                    if (u.MountId == unit.UnitId)
                    {
                        rider = u;
                        break;
                    }
                }
                if (rider != null)
                {
                    unit.transform.position = rider.transform.position;
                    unit.transform.SetParent(rider.transform);
                }
            }
        }
    }

    public void UpdateMountButtonColor()
    {
        if (Unit.SelectedUnit == null) return;
        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();

        if (unit.IsMounted)
        {
            _mountButton.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Color.green;

            //Wyœwietla ikonkê wierzchowca przy tokenie jednostki
            Unit.SelectedUnit.transform.Find("Canvas/Mount_image").gameObject.SetActive(true);
        }
        else
        {
            _mountButton.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Color.white;

            //Ukrywa ikonkê wierzchowca przy tokenie jednostki
            Unit.SelectedUnit.transform.Find("Canvas/Mount_image").gameObject.SetActive(false);
        }
    }

    public void DisplayAllMountIcons()
    {
        foreach (var pair in InitiativeQueueManager.Instance.InitiativeQueue)
        {
            if (pair.Key.IsMounted)
            {
                pair.Key.transform.Find("Canvas/Mount_image").gameObject.SetActive(true);
            }
        }
    }

    public Unit FindMountById(int Id)
    {
        foreach (Unit unit in UnitsManager.Instance.AllUnits)
        {
            if (unit.UnitId == Id)
            {
                return unit;
            }
        }
        return null;
    }
}
