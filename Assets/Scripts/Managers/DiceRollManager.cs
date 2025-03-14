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

    [SerializeField] private GameObject _applyRollResultPanel;
    [SerializeField] private TMP_InputField _rollInputField;

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
}
