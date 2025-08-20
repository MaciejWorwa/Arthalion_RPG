using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputFieldFilter : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private bool _isSkillOrTalentInput;
    [SerializeField] private bool _isTwoDigitNumber;
    [SerializeField] private bool _isMoneyInput;
    [SerializeField] private bool _isDiceRoll;

    private void Start()
    {
        _inputField = GetComponent<TMP_InputField>();

        if (_inputField != null)
        {
            _inputField.onValidateInput += ValidateInput;

            if (_isDiceRoll)
                _inputField.onEndEdit.AddListener(ValidateDiceRollValue);

            if (_isSkillOrTalentInput)
                _inputField.onEndEdit.AddListener(ValidateSkillOrTalentValue);
        }
    }

    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        // *** SKILL / TALENT: 1 cyfra, tylko 0–3 ***
        if (_isSkillOrTalentInput)
        {
            if (char.IsDigit(addedChar))
            {
                string newText = text.Insert(charIndex, addedChar.ToString());
                if (newText.Length <= 1 && addedChar >= '0' && addedChar <= '3')
                    return addedChar;
            }
            return '\0';
        }
        else if (_isTwoDigitNumber)
        {
            if (addedChar == '-' && text.Length == 0) return addedChar;

            if (char.IsDigit(addedChar))
            {
                int digitCount = text.StartsWith("-") ? text.Length - 1 : text.Length;
                if (digitCount < 2) return addedChar;
            }
            return '\0';
        }
        else if (_isMoneyInput)
        {
            if (char.IsDigit(addedChar) || addedChar == '+' || addedChar == '-') return addedChar;
            return '\0';
        }
        else if (_isDiceRoll)
        {
            // Pozwól tylko na 1–10 na etapie wpisywania
            if (!char.IsDigit(addedChar)) return '\0';

            string newText = text.Insert(charIndex, addedChar.ToString());

            if (newText.Length == 1)
                return addedChar; // pojedyncza cyfra (0–9; 0 skoryguje onEndEdit do 1..10)

            if (newText.Length == 2 && newText == "10")
                return addedChar; // jedyna dozwolona dwucyfrowa wartość

            return '\0'; // blokuj 11–99 i >2 znaki
        }
        else
        {
            if (char.IsLetterOrDigit(addedChar) || char.IsWhiteSpace(addedChar)) return addedChar;
            return '\0';
        }
    }

    private void ValidateDiceRollValue(string input)
    {
        if (int.TryParse(input, out int value))
        {
            value = Mathf.Clamp(value, 1, 10);
            _inputField.text = value.ToString();
        }
    }

    // Dodatkowe zabezpieczenie: clamp 0–3 po zakończeniu edycji
    private void ValidateSkillOrTalentValue(string input)
    {
        if (!int.TryParse(input, out int v)) v = 0;
        v = Mathf.Clamp(v, 0, 3);
        _inputField.text = v.ToString();
    }
}
