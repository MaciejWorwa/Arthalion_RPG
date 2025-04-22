using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputFieldFilter : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private bool _isAttributeInput;
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
            {
                _inputField.onEndEdit.AddListener(ValidateDiceRollValue);
            }
        }
    }

    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        //if (_isAttributeInput)
        //{
        //    // Dozwolone cyfry, max 2 znaki, maksymalnie 99
        //    if (char.IsDigit(addedChar) && (text.Length < 2 || (text == "9" && addedChar <= '9')))
        //    {
        //        return addedChar;
        //    }
        //    return '\0';
        //}
        if (_isAttributeInput)
        {
            if (char.IsDigit(addedChar))
            {
                string newText = text.Insert(charIndex, addedChar.ToString());

                if (int.TryParse(newText, out int result) && result <= 199)
                {
                    return addedChar;
                }
            }
            return '\0';
        }
        else if (_isTwoDigitNumber)
        {
            // Pozwól na minus tylko jako pierwszy znak
            if (addedChar == '-' && text.Length == 0)
            {
                return addedChar;
            }

            // Pozwól na cyfry
            if (char.IsDigit(addedChar))
            {
                // Długość bez minusa
                int digitCount = text.StartsWith("-") ? text.Length - 1 : text.Length;

                if (digitCount < 2)
                {
                    return addedChar;
                }
            }

            return '\0'; // Inne znaki są niedozwolone
        }
        else if (_isMoneyInput)
        {
            // Dozwolone cyfry oraz + i -
            if (char.IsDigit(addedChar) || addedChar == '+' || addedChar == '-')
            {
                return addedChar;
            }
            return '\0';
        }
        else if (_isDiceRoll)
        {
            // Dozwolone tylko cyfry
            if (char.IsDigit(addedChar))
            {
                return addedChar;
            }
            return '\0';
        }
        else
        {
            // Domyślnie: cyfry, litery i spacje
            if (char.IsLetterOrDigit(addedChar) || char.IsWhiteSpace(addedChar))
            {
                return addedChar;
            }
            return '\0';
        }
    }

    private void ValidateDiceRollValue(string input)
    {
        if (int.TryParse(input, out int value))
        {
            value = Mathf.Clamp(value, 1, 100);
            _inputField.text = value.ToString();
        }
    }
}
