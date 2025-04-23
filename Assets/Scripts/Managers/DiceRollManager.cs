using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DiceRollManager : MonoBehaviour
{
    // Prywatne statyczne pole przechowujące instancję
    private static DiceRollManager instance;

    // Publiczny dostęp do instancji
    public static DiceRollManager Instance
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

    // Zmienne do przechowywania wyniku
    public int ManualRollResult;
    public bool IsWaitingForRoll;

    public int RollModifier = 0;
    private bool _isRollModifierUpdating = false;

    [SerializeField] private UnityEngine.UI.Slider _modifierSlider;
    [SerializeField] private TMP_InputField _modifierInputField;

    [SerializeField] private GameObject _applyRollResultPanel;
    [SerializeField] private TMP_InputField _rollInputField;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && IsWaitingForRoll)
        {
            IsWaitingForRoll = false; // Przerywamy oczekiwanie
        }
    }

    public IEnumerator WaitForRollValue(Stats stats, string rollContext, Action<int> callback)
    {
        // Czekaj, aż inne rzuty się zakończą
        while (_applyRollResultPanel.activeSelf)
        {
            yield return null;
        }

        ManualRollResult = 0;

        if (_applyRollResultPanel != null)
        {
            _applyRollResultPanel.SetActive(true);
            _applyRollResultPanel.GetComponentInChildren<TMP_Text>().text = $"Wpisz wynik rzutu {stats.Name} na {rollContext}.";
        }

        if (_rollInputField != null)
        {
            _rollInputField.text = "";
        }

        while (ManualRollResult == 0)
        {
            yield return null;
        }

        if (_applyRollResultPanel != null)
        {
            _applyRollResultPanel.SetActive(false);
        }

        callback?.Invoke(ManualRollResult);
    }

    public void OnSubmitRoll()
    {
        if (_rollInputField != null && int.TryParse(_rollInputField.text, out int result))
        {
            ManualRollResult = result;
            IsWaitingForRoll = false; // Przerywamy oczekiwanie
            _rollInputField.text = ""; // Czyścimy pole
        }
    }

    // Funkcja sprawdzająca, czy liczba ma dwie identyczne cyfry
    public bool IsDoubleDigit(int number)
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

    public void SetRollModifier(GameObject gameObject)
    {
        if (_isRollModifierUpdating) return;
        _isRollModifierUpdating = true;

        if (gameObject.GetComponent<UnityEngine.UI.Slider>() != null)
        {
            int roundedValue = Mathf.RoundToInt(_modifierSlider.value) * 10; // Mnożenie wartości slidera x10
            _modifierInputField.text = roundedValue.ToString();
            RollModifier = roundedValue;
        }
        else
        {
            if (int.TryParse(_modifierInputField.text, out int value))
            {
                value = Mathf.Clamp(value, -30, 60);
                _modifierSlider.SetValueWithoutNotify(Mathf.RoundToInt(value / 10f)); // Dopasowanie wartości slidera bez wywołania eventu
                RollModifier = value;
            }
            else
            {
                _modifierInputField.text = "0";
                RollModifier = 0;
            }
        }

        _isRollModifierUpdating = false;
    }


    public void ResetRollModifier()
    {
        _modifierSlider.value = 0;
        _modifierInputField.text = "0";
        RollModifier = 0;
    }

    #region Attributes and skills tests
    public int[] TestSkill(string attributeName, Stats stats, string skillName = null, int modifier = 0, int rollResult = 0)
    {
        if (rollResult == 0)
        {
            rollResult = UnityEngine.Random.Range(1, 101);
        }

        // Uwzględnienie modyfikatora z panelu jednostki
        if (RollModifier != 0)
        {
            modifier += RollModifier;
        }

        // Pobieranie wartości umiejętności na podstawie nazwy
        int skillValue = 0;
        if (skillName != null)
        {
            // Jeśli skillName dotyczy broni białej lub dystansowej, pobierz go ze słownika
            if (Enum.TryParse(skillName, out MeleeCategory meleeCategory) && stats.Melee.ContainsKey(meleeCategory))
            {
                skillValue = stats.GetSkillModifier(stats.Melee, meleeCategory);
            }
            else if (Enum.TryParse(skillName, out RangedCategory rangedCategory) && stats.Ranged.ContainsKey(rangedCategory))
            {
                skillValue = stats.GetSkillModifier(stats.Ranged, rangedCategory);
            }
            else
            {
                var field = typeof(Stats).GetField(skillName);
                if (field != null)
                {
                    skillValue = (int)field.GetValue(stats);
                }
            }
        }

        // Pobieranie wartości atrybutu na podstawie nazwy
        int attributeValue = 0;
        var attributeField = typeof(Stats).GetField(attributeName);
        if (attributeField != null)
        {
            attributeValue = (int)attributeField.GetValue(stats);

            // Modyfikator za przeciążenie
            if (attributeName == "Zw")
            {
                int encumbrancePenalty = 0;
                if (stats.MaxEncumbrance - stats.CurrentEncumbrance < 0 && stats.CurrentEncumbrance < stats.MaxEncumbrance * 2)
                {
                    encumbrancePenalty = 10;
                }
                else if (stats.MaxEncumbrance - stats.CurrentEncumbrance < 0 && stats.CurrentEncumbrance < stats.MaxEncumbrance * 3)
                {
                    encumbrancePenalty = 20;
                }

                // Sprawdzamy, czy Zw nie spadnie poniżej 10
                if (attributeValue - encumbrancePenalty < 10)
                {
                    encumbrancePenalty = attributeValue - 10;
                }

                modifier -= encumbrancePenalty;

                if(encumbrancePenalty != 0)
                {
                    Debug.Log($"Modyfikator do Zwinności {stats.Name} za przeciążenie: {encumbrancePenalty}.");
                }
            }
        }
        
        if (stats.GetComponent<Unit>().Fatiqued > 0) modifier -= stats.GetComponent<Unit>().Fatiqued * 10; // Modyfikator za wyczerpanie
        else if (stats.GetComponent<Unit>().Poison > 0) modifier -= 10; // Modyfikator za truciznę

        // Modyfikator za dekoncentrującego przeciwnika w pobliżu
        foreach (var entry in InitiativeQueueManager.Instance.InitiativeQueue)
        {
            Unit unit = entry.Key;
            Stats distractingStats = unit.GetComponent<Stats>();
            if (distractingStats.Distracting && !ReferenceEquals(distractingStats, stats) && !unit.CompareTag(stats.tag))
            {
                float radius = (distractingStats.Wt / 10) / 2f;
                float distance = Vector2.Distance(unit.transform.position, stats.transform.position);

                if (distance <= radius)
                {
                    modifier -= 20;
                    Debug.Log($"{stats.Name} jest zdekoncentrowany przez {distractingStats.Name}.");
                    break; // tylko raz -20, nawet jeśli więcej jednostek dekoncentruje
                }
            }
        }

        if (modifier > 60) modifier = 60; // Górny limit modyfikatora
        if (modifier < -30) modifier = -30; // Dolny limit modyfikatora

        int successValue = skillValue + attributeValue + modifier - rollResult;
        int successLevel = (skillValue + attributeValue + modifier) / 10 - rollResult / 10;

        // Określenie koloru na podstawie poziomu sukcesu
        string successLevelColor = successValue >= 0 ? "green" : "red";

        // Tworzenie stringa dla modyfikatora
        string modifierString = modifier != 0 ? $" Modyfikator: {modifier}," : "";

        // Wyświetlenie wyniku
        if (skillName != null)
        {
            Debug.Log($"{stats.Name} rzuca na {skillName}: {rollResult}. Wartość umiejętności: {skillValue + attributeValue}.{modifierString} Poziomy sukcesu: <color={successLevelColor}>{successLevel}</color>.");
        }
        else
        {
            Debug.Log($"{stats.Name} rzuca na {attributeName}: {rollResult}. Wartość cechy: {attributeValue}.{modifierString} Poziomy sukcesu: <color={successLevelColor}>{successLevel}</color>.");
        }

        //Pech i szczęście
        if (IsDoubleDigit(rollResult))
        {
            if (successValue >= 0)
            {
                Debug.Log($"{stats.Name} wyrzucił <color=green>FUKSA</color>!");

                //Aktualizuje osiągnięcia
                stats.FortunateEvents++;
            }
            else
            {
                Debug.Log($"{stats.Name} wyrzucił <color=red>PECHA</color>!");

                //Aktualizuje osiągnięcia
                stats.UnfortunateEvents++;
            }
        }

        ResetRollModifier();

        return new int[] { successValue, successLevel };
    }
    #endregion
}
