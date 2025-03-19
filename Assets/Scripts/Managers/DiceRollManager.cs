using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DiceRollManager : MonoBehaviour
{
    // Prywatne statyczne pole przechowuj¹ce instancjê
    private static DiceRollManager instance;

    // Publiczny dostêp do instancji
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
            // Jeœli instancja ju¿ istnieje, a próbujemy utworzyæ kolejn¹, niszczymy nadmiarow¹
            Destroy(gameObject);
        }
    }

    // Zmienne do przechowywania wyniku
    public int ManualRollResult;
    public bool IsWaitingForRoll;

    public int RollModifier = 0;
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

    public IEnumerator WaitForRollValue(Stats stats, string rollContext)
    {
        IsWaitingForRoll = true;
        ManualRollResult = 0;

        // Wyœwietl panel do wpisania wyniku
        if (_applyRollResultPanel != null)
        {
            _applyRollResultPanel.SetActive(true);
            _applyRollResultPanel.GetComponentInChildren<TMP_Text>().text = $"Wpisz wynik rzutu {stats.Name} na {rollContext}.";
        }

        // Wyzeruj pole tekstowe
        if (_rollInputField != null)
        {
            _rollInputField.text = "";
        }

        // Czekaj a¿ u¿ytkownik wpisze wartoœæ i kliknie Submit
        while (IsWaitingForRoll)
        {
            yield return null; // Czekaj na nastêpn¹ ramkê
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
            ManualRollResult = result;
            IsWaitingForRoll = false; // Przerywamy oczekiwanie
            _rollInputField.text = ""; // Czyœcimy pole
        }
    }

    // Funkcja sprawdzaj¹ca, czy liczba ma dwie identyczne cyfry
    public bool IsDoubleDigit(int number)
    {
        // Jeœli wynik to dok³adnie 100, równie¿ spe³nia warunek
        if (number == 100) return true;

        // Sprawdzenie dla liczb dwucyfrowych
        if (number >= 10 && number <= 99)
        {
            int tens = number / 10;  // Cyfra dziesi¹tek
            int ones = number % 10; // Cyfra jednoœci
            return tens == ones;    // Sprawdzenie, czy cyfry s¹ takie same
        }

        return false;
    }

    public void SetRollModifier(GameObject gameObject)
    {
        if (gameObject.GetComponent<UnityEngine.UI.Slider>() != null)
        {
            int roundedValue = Mathf.RoundToInt(_modifierSlider.value / 10f) * 10; // Zaokr¹glenie do wielokrotnoœci 10
            _modifierSlider.value = roundedValue;
            _modifierInputField.text = roundedValue.ToString();
        }
        else
        {
            if (int.TryParse(_modifierInputField.text, out int value))
            {
                value = Mathf.Clamp(value, -30, 60);
                value = Mathf.RoundToInt(value / 10f) * 10; // Zaokr¹glenie do najbli¿szej wielokrotnoœci 10
                _modifierSlider.value = value;
                _modifierInputField.text = value.ToString();
            }
            else
            {
                _modifierInputField.text = "0";
            }
        }

        RollModifier = (int)_modifierSlider.value;
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

        // Uwzglêdnienie modyfikatora z panelu jednostki
        if(RollModifier != 0)
        {
            modifier += RollModifier;
        }

        // Pobieranie wartoœci umiejêtnoœci na podstawie nazwy
        int skillValue = 0;
        if (skillName != null)
        {
            // Jeœli skillName dotyczy broni bia³ej lub dystansowej, pobierz go ze s³ownika
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

        // Pobieranie wartoœci atrybutu na podstawie nazwy
        int attributeValue = 0;
        var attributeField = typeof(Stats).GetField(attributeName);
        if (attributeField != null)
        {
            attributeValue = (int)attributeField.GetValue(stats);

            // Modyfikator za przeci¹¿enie
            if (attributeName == "Zw")
            {
                Debug.Log($"max obciazenie {stats.MaxEncumbrance}, currentEncumbrance {stats.CurrentEncumbrance}");

                int encumbrancePenalty = 0;
                if (stats.MaxEncumbrance - stats.CurrentEncumbrance < 0 && stats.CurrentEncumbrance < stats.MaxEncumbrance * 2)
                {
                    encumbrancePenalty = 10;
                }
                else if (stats.MaxEncumbrance - stats.CurrentEncumbrance < 0 && stats.CurrentEncumbrance < stats.MaxEncumbrance * 3)
                {
                    encumbrancePenalty = 20;
                }

                // Sprawdzamy, czy Zw nie spadnie poni¿ej 10
                if (attributeValue - encumbrancePenalty < 10)
                {
                    encumbrancePenalty = attributeValue - 10;
                }

                modifier -= encumbrancePenalty;
            }
        }

        // Modyfikator za wyczerpanie
        if (stats.GetComponent<Unit>().Fatiqued > 0) modifier -= stats.GetComponent<Unit>().Fatiqued * 10;
        else if (stats.GetComponent<Unit>().Poison > 0) modifier -= 10;

        int successValue = skillValue + attributeValue + modifier - rollResult;
        int successLevel = (skillValue + attributeValue + modifier) / 10 - rollResult / 10;

        // Okreœlenie koloru na podstawie poziomu sukcesu
        string successLevelColor = successValue > 0 ? "green" : "red";

        // Tworzenie stringa dla modyfikatora
        string modifierString = modifier != 0 ? $" Modyfikator: {modifier}," : "";

        // Wyœwietlenie wyniku
        if (skillName != null)
        {
            Debug.Log($"{stats.Name} rzuca na {skillName}: {rollResult}, Wartoœæ umiejêtnoœci: {skillValue + attributeValue},{modifierString} Poziomy sukcesu: <color={successLevelColor}>{successLevel}</color>");
        }
        else
        {
            Debug.Log($"{stats.Name} rzuca na {attributeName}: {rollResult}, Wartoœæ cechy: {attributeValue},{modifierString} Poziomy sukcesu: <color={successLevelColor}>{successLevel}</color>");
        }

        //Pech i szczêœcie
        if (IsDoubleDigit(rollResult))
        {
            if (successValue >= 0)
            {
                Debug.Log($"{stats.Name} wyrzuci³ <color=green>FUKSA</color>!");

                //Aktualizuje osi¹gniêcia
                stats.FortunateEvents++;
            }
            else
            {
                Debug.Log($"{stats.Name} wyrzuci³ <color=red>PECHA</color>!");

                //Aktualizuje osi¹gniêcia
                stats.UnfortunateEvents++;
            }
        }

        ResetRollModifier();

        return new int[] { successValue, successLevel };
    }
    #endregion
}
