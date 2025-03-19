using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DiceRollManager : MonoBehaviour
{
    // Prywatne statyczne pole przechowuj�ce instancj�
    private static DiceRollManager instance;

    // Publiczny dost�p do instancji
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
            // Je�li instancja ju� istnieje, a pr�bujemy utworzy� kolejn�, niszczymy nadmiarow�
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

        // Wy�wietl panel do wpisania wyniku
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

        // Czekaj a� u�ytkownik wpisze warto�� i kliknie Submit
        while (IsWaitingForRoll)
        {
            yield return null; // Czekaj na nast�pn� ramk�
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
            _rollInputField.text = ""; // Czy�cimy pole
        }
    }

    // Funkcja sprawdzaj�ca, czy liczba ma dwie identyczne cyfry
    public bool IsDoubleDigit(int number)
    {
        // Je�li wynik to dok�adnie 100, r�wnie� spe�nia warunek
        if (number == 100) return true;

        // Sprawdzenie dla liczb dwucyfrowych
        if (number >= 10 && number <= 99)
        {
            int tens = number / 10;  // Cyfra dziesi�tek
            int ones = number % 10; // Cyfra jedno�ci
            return tens == ones;    // Sprawdzenie, czy cyfry s� takie same
        }

        return false;
    }

    public void SetRollModifier(GameObject gameObject)
    {
        if (gameObject.GetComponent<UnityEngine.UI.Slider>() != null)
        {
            int roundedValue = Mathf.RoundToInt(_modifierSlider.value / 10f) * 10; // Zaokr�glenie do wielokrotno�ci 10
            _modifierSlider.value = roundedValue;
            _modifierInputField.text = roundedValue.ToString();
        }
        else
        {
            if (int.TryParse(_modifierInputField.text, out int value))
            {
                value = Mathf.Clamp(value, -30, 60);
                value = Mathf.RoundToInt(value / 10f) * 10; // Zaokr�glenie do najbli�szej wielokrotno�ci 10
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

        // Uwzgl�dnienie modyfikatora z panelu jednostki
        if(RollModifier != 0)
        {
            modifier += RollModifier;
        }

        // Pobieranie warto�ci umiej�tno�ci na podstawie nazwy
        int skillValue = 0;
        if (skillName != null)
        {
            // Je�li skillName dotyczy broni bia�ej lub dystansowej, pobierz go ze s�ownika
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

        // Pobieranie warto�ci atrybutu na podstawie nazwy
        int attributeValue = 0;
        var attributeField = typeof(Stats).GetField(attributeName);
        if (attributeField != null)
        {
            attributeValue = (int)attributeField.GetValue(stats);

            // Modyfikator za przeci��enie
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

                // Sprawdzamy, czy Zw nie spadnie poni�ej 10
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

        // Okre�lenie koloru na podstawie poziomu sukcesu
        string successLevelColor = successValue > 0 ? "green" : "red";

        // Tworzenie stringa dla modyfikatora
        string modifierString = modifier != 0 ? $" Modyfikator: {modifier}," : "";

        // Wy�wietlenie wyniku
        if (skillName != null)
        {
            Debug.Log($"{stats.Name} rzuca na {skillName}: {rollResult}, Warto�� umiej�tno�ci: {skillValue + attributeValue},{modifierString} Poziomy sukcesu: <color={successLevelColor}>{successLevel}</color>");
        }
        else
        {
            Debug.Log($"{stats.Name} rzuca na {attributeName}: {rollResult}, Warto�� cechy: {attributeValue},{modifierString} Poziomy sukcesu: <color={successLevelColor}>{successLevel}</color>");
        }

        //Pech i szcz�cie
        if (IsDoubleDigit(rollResult))
        {
            if (successValue >= 0)
            {
                Debug.Log($"{stats.Name} wyrzuci� <color=green>FUKSA</color>!");

                //Aktualizuje osi�gni�cia
                stats.FortunateEvents++;
            }
            else
            {
                Debug.Log($"{stats.Name} wyrzuci� <color=red>PECHA</color>!");

                //Aktualizuje osi�gni�cia
                stats.UnfortunateEvents++;
            }
        }

        ResetRollModifier();

        return new int[] { successValue, successLevel };
    }
    #endregion
}
