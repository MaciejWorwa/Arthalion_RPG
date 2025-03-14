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

    [SerializeField] private GameObject _applyRollResultPanel;
    [SerializeField] private TMP_InputField _rollInputField;

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
}
