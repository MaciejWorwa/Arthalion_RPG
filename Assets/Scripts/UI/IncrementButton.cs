using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class IncrementButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Button _button;
    [SerializeField] private int _incrementValue;
    [SerializeField] private string _valueName;
    private bool _isHeld = false;
    private Coroutine _repeatActionCoroutine;


    void Start()
    {
        _button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_button.interactable && !_isHeld) // Sprawdzanie, czy przycisk jest aktywny
        {
            _isHeld = true;

            if (_repeatActionCoroutine != null)
            {
                StopCoroutine(_repeatActionCoroutine);
            }
            _repeatActionCoroutine = StartCoroutine(RepeatAction());
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isHeld = false;
    }

    private IEnumerator RepeatAction()
    {
        while (_isHeld)
        {
            if(_valueName == "TempHealth")
            {
                //Modyfikuje liczbę punktów żywotności jednostki
                UnitsManager.Instance.ChangeTemporaryHealthPoints(_incrementValue);
            }
            else if (_valueName == "PlayersAdvantage")
            {
                InitiativeQueueManager.Instance.CalculateAdvantage("PlayerUnit", _incrementValue);
            }
            else if (_valueName == "EnemiesAdvantage")
            {
                InitiativeQueueManager.Instance.CalculateAdvantage("EnemyUnit", _incrementValue);
            }
            else if (_valueName == "ReloadLeft" && Unit.SelectedUnit != null)
            {
                Weapon weapon = Unit.SelectedUnit.GetComponent<Inventory>().EquippedWeapons[0];
                weapon.ReloadLeft = Mathf.Clamp(weapon.ReloadLeft - _incrementValue, 0, weapon.ReloadTime);
                InventoryManager.Instance.DisplayReloadTime();
            }

            yield return new WaitForSeconds(0.3f); // Czeka przed kolejnym wywołaniem
        }
    }
}
