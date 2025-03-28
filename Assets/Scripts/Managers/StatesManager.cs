using System.Collections;
using System.Reflection;
using TMPro;
using UnityEngine;

public class StatesManager : MonoBehaviour
{
    // Prywatne statyczne pole przechowujące instancję
    private static StatesManager instance;

    // Publiczny dostęp do instancji
    public static StatesManager Instance
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

    public void UpdateUnitStates(Unit unit)
    {
        Ablaze(unit); // Podpalenie
        StartCoroutine(Bleeding(unit)); // Krwawienie
        Blinded(unit); // Oślepienie
        StartCoroutine(Broken(unit)); // Panika
        Deafened(unit); // Ogłuszenie
        StartCoroutine(Poison(unit)); // Zatrucie
        StartCoroutine(Stunned(unit)); // Oszołomienie
        unit.Surprised = false;
    }

    private void Ablaze(Unit unit)
    {
        if (unit.Ablaze == 0) return;
        Stats stats = unit.GetComponent<Stats>();

        // Obrażenia od ognia
        int damage = unit.Ablaze - 1 + UnityEngine.Random.Range(1, 11);

        // Znalezienie najmniejszej wartości pancerza spośród wszystkich części ciała
        int minArmor = Mathf.Min(stats.Armor_head, stats.Armor_arms, stats.Armor_torso, stats.Armor_legs);

        stats.TempHealth -= Mathf.Max(1, damage - minArmor - stats.Wt / 10);
        Debug.Log($"<color=#FF7F50>{stats.Name} traci {Mathf.Max(1, damage - minArmor - stats.Wt / 10)} punktów żywotności w wyniku podpalenia.</color>");
        unit.DisplayUnitHealthPoints();

        Debug.Log($"Obrazenia od ognia: {unit.Ablaze - 1} + losowa liczba. Łącznie {damage}");
        Debug.Log($"minimalna zbroja: {minArmor} bonus z WT: {stats.Wt / 10}");
    }

    private IEnumerator Bleeding(Unit unit)
    {
        Stats stats = unit.GetComponent<Stats>();

        if (unit.Bleeding - stats.Implacable <= 0) yield break;

        if (stats.TempHealth > 0)
        {
            stats.TempHealth -= unit.Bleeding - stats.Implacable;
            Debug.Log($"<color=#FF7F50>{stats.Name} traci {unit.Bleeding - stats.Implacable} punktów żywotności w wyniku krwawienia.</color>");
            unit.DisplayUnitHealthPoints();
        }
        else if (!unit.Unconscious)
        {
            unit.Unconscious = true; // Utrata Przytomności
            StartCoroutine(CombatManager.Instance.FrenzyCoroutine(false, unit)); //Zresetowanie szału bojowego
            Debug.Log($"<color=#FF7F50>{stats.Name} traci przytomność w wyniku krwawienia.</color>");
        }
        else
        {
            int rollResult = 0;
            if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "śmierć w wyniku krwawienia", result => rollResult = result));
                if (rollResult == 0) yield break;

                //yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "śmierć w wyniku krwawienia"));
                //rollResult = DiceRollManager.Instance.ManualRollResult;
            }

            int rollDifficulty = unit.Bleeding * 10;
            if (rollResult < rollDifficulty)
            {
                Debug.Log($"<color=#FF7F50>{stats.Name} wykonuje rzut obronny przed śmiercią w wyniku krwawienia. Wynik rzutu: {rollResult} Modyfikator: {-rollDifficulty}. {stats.Name} umiera.</color>");

                if (GameManager.IsAutoKillMode)
                {
                    // Usuwanie jednostki
                    UnitsManager.Instance.DestroyUnit(unit.gameObject);
                }
            }
            else
            {
                Debug.Log($"<color=#FF7F50>{stats.Name} wykonuje rzut obronny przed śmiercią w wyniku krwawienia. Wynik rzutu: {rollResult} Modyfikator: {-rollDifficulty}. {stats.Name} nadal żyje.</color>");
            }

            if (DiceRollManager.Instance.IsDoubleDigit(rollResult))
            {
                Debug.Log($"<color=#FF7F50>{stats.Name} wyrzucił/a dublet. Krwawienie zmniejsza się o 1 poziom.</color>");
                unit.Bleeding--;

                if (unit.Bleeding == 0) unit.Fatiqued++; // Zwiększenie Wyczerpania
            }
        }
    }
    private void Blinded(Unit unit)
    {
        if (unit.Blinded > 0) unit.Blinded--;
    }

    public IEnumerator Broken(Unit unit)
    {
        Stats stats = unit.GetComponent<Stats>();
        bool isEngagedInCombat = CombatManager.Instance.AdjacentOpponents(unit.transform.position, unit.tag).Count > 0 ? true : false;

        if (unit.Broken > 0 && !isEngagedInCombat)
        {
            int rollResult = 0;
            if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "opanowanie", result => rollResult = result));
                if (rollResult == 0) yield break;

                //yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "opanowanie"));
                //rollResult = DiceRollManager.Instance.ManualRollResult;
            }

            int successLevel = DiceRollManager.Instance.TestSkill("SW", stats, "Cool", stats.StoutHearted * 10, rollResult)[1];
            if (successLevel > 0)
            {
                unit.Broken = Mathf.Max(0, unit.Broken - successLevel);
            }

            if (unit.Broken == 0)
            {
                unit.Fatiqued++; // Zwiększenie Wyczerpania
                unit.IsFearTestPassed = true;
                unit.IsTerrorTestPassed = true;
                Debug.Log($"<color=#FF7F50>{stats.Name} udało się opanować panikę. Poziom wyczerpania wzrasta o 1.</color>");
            }
            else
            {
                Debug.Log($"<color=#FF7F50>{stats.Name} próbuje opanować panikę. Pozostałe poziomy paniki: {unit.Broken}.</color>");
            }
        }
    }

    private void Deafened(Unit unit)
    {
        if (unit.Deafened > 0) unit.Deafened--;
    }

    private IEnumerator Poison(Unit unit)
    {
        if (unit.Poison == 0) yield break;
        Stats stats = unit.GetComponent<Stats>();

        if (unit.Poison > 0)
        {
            int rollResult = 0;
            if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "odporność", result => rollResult = result));
                if (rollResult == 0) yield break;

                //yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "odporność"));
                //rollResult = DiceRollManager.Instance.ManualRollResult;
            }

            int successLevel = DiceRollManager.Instance.TestSkill("Wt", stats, "Endurance", 0, rollResult)[1];
            if (successLevel > 0)
            {
                unit.Poison = Mathf.Max(0, unit.Poison - successLevel);
            }

            if (unit.Poison == 0)
            {
                unit.Fatiqued++; // Zwiększenie Wyczerpania
                Debug.Log($"<color=#FF7F50>{stats.Name} udało się wygrać z zatruciem. Poziom wyczerpania wzrasta o 1.</color>");
                yield break;
            }
            else
            {
                Debug.Log($"<color=#FF7F50>{stats.Name} walczy z zatruciem. Pozostałe poziomy zatrucia: {unit.Poison}.</color>");
            }
        }

        if (stats.TempHealth > 0)
        {
            stats.TempHealth -= unit.Poison;
            Debug.Log($"<color=#FF7F50>{stats.Name} traci {unit.Poison} punktów żywotności w wyniku zatrucia.</color>");
            unit.DisplayUnitHealthPoints();
        }
        else
        {
            unit.Unconscious = true; // Utrata Przytomności
            StartCoroutine(CombatManager.Instance.FrenzyCoroutine(false, unit)); //Zresetowanie szału bojowego
            Debug.Log($"<color=#FF7F50>{stats.Name} traci przytomność w wyniku zatrucia.</color>");
        }
    }

    public void Entangled(Unit unit, int value = 0)
    {
        if (value > 0)
        {
            unit.Entangled += value;
        }

        if (unit.Entangled > 0) unit.CanMove = false;
    }


    public void Prone(Unit unit, bool value = true)
    {
        Stats stats = unit.GetComponent<Stats>();

        unit.Prone = value;
        if (unit.Unconscious)
        {
            Debug.Log($"<color=#FF7F50>{stats.Name} zostaje powalony.</color>");
        }
        else
        {
            unit.Prone = false;
            unit.CanMove = false;
            MovementManager.Instance.SetCanMoveToggle(false);
            Debug.Log($"<color=green>{unit.GetComponent<Stats>().Name} podnosi się z ziemi.</color>");
        }
    }

    private IEnumerator Stunned(Unit unit)
    {
        Stats stats = unit.GetComponent<Stats>();

        if (unit.Stunned > 0)
        {
            int rollResult = 0;
            if (!GameManager.IsAutoDiceRollingMode && stats.CompareTag("PlayerUnit"))
            {
                yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "odporność", result => rollResult = result));
                if (rollResult == 0) yield break;

                //yield return StartCoroutine(DiceRollManager.Instance.WaitForRollValue(stats, "odporność"));
                //rollResult = DiceRollManager.Instance.ManualRollResult;
            }

            int successLevel = DiceRollManager.Instance.TestSkill("Wt", stats, "Endurance", 0, rollResult)[1];
            if (successLevel > 0)
            {
                unit.Stunned = Mathf.Max(0, unit.Stunned - successLevel);
            }

            if (unit.Stunned == 0)
            {
                unit.Fatiqued++; // Zwiększenie Wyczerpania
                Debug.Log($"<color=#FF7F50>{stats.Name} udało się wyjść ze stanu oszołomienia. Poziom wyczerpania wzrasta o 1.</color>");
            }
            else
            {
                Debug.Log($"<color=#FF7F50>{stats.Name} walczy z oszołomieniem. Pozostałe poziomy oszołomienia: {unit.Stunned}.</color>");
            }
        }
    }

    public void Unconscious(Unit unit, bool value = true)
    {
        Stats stats = unit.GetComponent<Stats>();

        unit.Unconscious = value;
        if (unit.Unconscious)
        {
            Debug.Log($"<color=#FF7F50>{stats.Name} traci przytomność.</color>");
            StartCoroutine(CombatManager.Instance.FrenzyCoroutine(false, unit)); //Zresetowanie szału bojowego
        }
        else
        {
            Debug.Log($"<color=#FF7F50>{stats.Name} odzyskuje przytomność.</color>");
        }
    }

    public void SetUnitState(GameObject textInput)
    {
        if (Unit.SelectedUnit == null) return;

        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();

        // Pobiera nazwę cechy z nazwy obiektu InputField (bez "_input")
        string stateName = textInput.name.Replace("_input", "");

        // Szukamy pola
        FieldInfo field = unit.GetType().GetField(stateName);

        // Jeżeli pole nie istnieje, kończymy metodę
        if (field == null)
        {
            Debug.Log($"Nie znaleziono pola '{stateName}'.");
            return;
        }

        // Zależnie od typu pola...
        if (field.FieldType == typeof(int) && textInput.GetComponent<UnityEngine.UI.Slider>() == null)
        {
            // int przez InputField
            int value = int.TryParse(textInput.GetComponent<TMP_InputField>().text, out int inputValue) ? inputValue : 0;

            field.SetValue(unit, value);
        }
        else if (field.FieldType == typeof(bool))
        {
            bool boolValue = textInput.GetComponent<UnityEngine.UI.Toggle>().isOn;
            field.SetValue(unit, boolValue);
        }
        else
        {
            Debug.Log($"Nie udało się zmienić wartości stanu '{stateName}'.");
        }

        //UnitsManager.Instance.UpdateUnitPanel(unit);
    }

    public void LoadUnitStates()
    {
        if (Unit.SelectedUnit == null) return;
        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();

        GameObject[] statesInputFields = GameObject.FindGameObjectsWithTag("State");

        foreach (var inputField in statesInputFields)
        {
            // Wyciągamy nazwę stanu z nazwy obiektu InputField
            string stateName = inputField.name.Replace("_input", "");

            FieldInfo field = unit.GetType().GetField(stateName);
            if (field == null)
                continue;

            if (field.FieldType == typeof(int))
            {
                int value = (int)field.GetValue(unit);

                if (inputField.GetComponent<TMPro.TMP_InputField>() != null)
                {
                    inputField.GetComponent<TMPro.TMP_InputField>().text = value.ToString();
                }
            }
            else if (field.FieldType == typeof(bool))
            {
                bool value = (bool)field.GetValue(unit);
                inputField.GetComponent<UnityEngine.UI.Toggle>().isOn = value;
            }
        }
    }
}
