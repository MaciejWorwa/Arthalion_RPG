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

        while (ManualRollResult == 0 || _applyRollResultPanel.activeSelf)
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
    public bool IsDoubleDigit(int number1, int number2)
    {
        if (number1 == number2) return true;
        else return false;
    }

    public void SetRollModifier(GameObject gameObject)
    {
        if (_isRollModifierUpdating) return;
        _isRollModifierUpdating = true;

        if (gameObject.GetComponent<UnityEngine.UI.Slider>() != null)
        {
            _modifierInputField.text = _modifierSlider.value.ToString();
            RollModifier = (int)_modifierSlider.value;
        }
        else
        {
            if (int.TryParse(_modifierInputField.text, out int value))
            {
                value = Mathf.Clamp(value, -10, 10);
                _modifierSlider.SetValueWithoutNotify(Mathf.RoundToInt(value)); // Dopasowanie wartości slidera bez wywołania eventu
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
    public int TestSkill(string attributeName, Stats stats, string skillName = null, int modifier = 0, int roll1 = 0, int roll2 = 0, int skillRoll = 0, int difficultyLevel = 0)
    {
        // Pobieranie wartości umiejętności na podstawie nazwy
        int skillValue = 0;
        if (skillName != null)
        {
            var field = typeof(Stats).GetField(skillName);
            if (field != null)
            {
                skillValue = (int)field.GetValue(stats);
            }
        }

        // Wyniki rzutów
        if (roll1 == 0 || roll2 == 0)
        {
            roll1 = UnityEngine.Random.Range(1, 11);
            roll2 = UnityEngine.Random.Range(1, 11);

            switch (skillValue)
            {
                case 1:
                    skillRoll = UnityEngine.Random.Range(1, 5);
                    break;
                case 2:
                    skillRoll = UnityEngine.Random.Range(1, 7);
                    break;
                case 3:
                    skillRoll = UnityEngine.Random.Range(1, 9);
                    break;
            }
        }

        // Uwzględnienie modyfikatora z panelu jednostki
        if (RollModifier != 0)
        {
            modifier += RollModifier;
        }

        // Pobieranie wartości cechy na podstawie nazwy
        int attributeValue = 0;
        var attributeField = typeof(Stats).GetField(attributeName);
        if (attributeField != null)
        {
            attributeValue = (int)attributeField.GetValue(stats);
        }
        
        //if (stats.GetComponent<Unit>().Fatiqued > 0) modifier -= stats.GetComponent<Unit>().Fatiqued * 10; // Modyfikator za wyczerpanie
        //else if (stats.GetComponent<Unit>().Poison > 0) modifier -= 10; // Modyfikator za truciznę

        //// Modyfikator za dekoncentrującego przeciwnika w pobliżu
        //foreach (var entry in InitiativeQueueManager.Instance.InitiativeQueue)
        //{
        //    Unit unit = entry.Key;
        //    Stats distractingStats = unit.GetComponent<Stats>();
        //    if (distractingStats.Distracting && !ReferenceEquals(distractingStats, stats) && !unit.CompareTag(stats.tag))
        //    {
        //        float radius = (distractingStats.Wt / 10) / 2f;
        //        float distance = Vector2.Distance(unit.transform.position, stats.transform.position);

        //        if (distance <= radius)
        //        {
        //            modifier -= 20;
        //            Debug.Log($"{stats.Name} jest zdekoncentrowany przez {distractingStats.Name}.");
        //            break; // tylko raz -20, nawet jeśli więcej jednostek dekoncentruje
        //        }
        //    }
        //}

        //if (modifier > 60) modifier = 60; // Górny limit modyfikatora
        //if (modifier < -30) modifier = -30; // Dolny limit modyfikatora


        int finalScore = roll1 + roll2 + skillRoll + attributeValue + modifier;

        string statName = skillName != null ? skillName : attributeName;
        string modifierString = modifier != 0 ? $" Inne modyfikatory: {modifier}." : "";
        string skillDiceString = skillValue != 0 ? $" + {skillRoll}" : "";
        string difficultyLevelString = difficultyLevel != 0 ? $"/{difficultyLevel}" : "";

        // Określenie koloru na podstawie sukcesu
        string color = finalScore >= difficultyLevel ? "green" : "red";

        // Wyświetlenie wyniku
        Debug.Log($"{stats.Name} rzuca na {statName}: {roll1} + {roll2}{skillDiceString} = {roll1 + roll2 + skillRoll}. Modyfikator z cechy: {attributeValue}.{modifierString} Łączny wynik: <color={color}>{finalScore}{difficultyLevelString}</color>.");


        if(difficultyLevel != 0)
        {
            //Pech i szczęście
            if (IsDoubleDigit(roll1, roll2))
            {
                if (finalScore >= difficultyLevel)
                {
                    Debug.Log($"{stats.Name} wyrzucił <color=green>SZCZĘŚCIE</color>!");

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
        }

        ResetRollModifier();

        return finalScore;
    }
    #endregion
}
