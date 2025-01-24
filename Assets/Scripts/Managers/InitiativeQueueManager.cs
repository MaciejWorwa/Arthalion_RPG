using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using static System.Net.Mime.MediaTypeNames;

public class InitiativeQueueManager : MonoBehaviour
{
    // Prywatne statyczne pole przechowujące instancję
    private static InitiativeQueueManager instance;

    // Publiczny dostęp do instancji
    public static InitiativeQueueManager Instance
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
    public Dictionary <Unit, int> InitiativeQueue = new Dictionary<Unit, int>();
    public Unit ActiveUnit;
    public Transform InitiativeScrollViewContent;
    public Transform PlayersCamera_InitiativeScrollViewContent;
    [SerializeField] private GameObject _initiativeOptionPrefab; // Prefab odpowiadający każdej jednostce na liście inicjatywy
    private Color _defaultColor = new Color(0f, 0f, 0f, 0f); // Domyślny kolor przycisku
    private Color _selectedColor = new Color(0f, 0f, 0f, 0.5f); // Kolor wybranego przycisku (zaznaczonej jednostki)
    private Color _activeColor = new Color(0.15f, 1f, 0.45f, 0.2f); // Kolor aktywnego przycisku (jednostka, której tura obecnie trwa)
    private Color _selectedActiveColor = new Color(0.08f, 0.5f, 0.22f, 0.5f); // Kolor wybranego przycisku, gdy jednocześnie jest to aktywna jednostka
    public UnityEngine.UI.Slider DominanceBar; // Pasek przewagi sił w bitwie
    public int PlayersAdvantage;
    public int EnemiesAdvantage;
    [SerializeField] private TMP_InputField _playersAdvantageInput;
    [SerializeField] private TMP_InputField _enemiesAdvantageInput;

    private void Start()
    {
        _playersAdvantageInput.text = "0";
        _enemiesAdvantageInput.text = "0";
    }

    #region Initiative queue
    public void AddUnitToInitiativeQueue(Unit unit)
    {
        //Nie dodaje do kolejki inicjatywy jednostek, które są ukryte
        Collider2D collider = Physics2D.OverlapPoint(unit.gameObject.transform.position);
        if(collider.CompareTag("TileCover")) return;

        InitiativeQueue.Add(unit, unit.GetComponent<Stats>().Initiative);

        //Aktualizuje pasek przewagi w bitwie
        unit.GetComponent<Stats>().Overall = unit.GetComponent<Stats>().CalculateOverall();

        CalculateDominance(unit.GetComponent<Stats>().Overall, 0, unit.tag);
    }

    public void RemoveUnitFromInitiativeQueue(Unit unit)
    {
        InitiativeQueue.Remove(unit);

        //Aktualizuje pasek przewagi w bitwie
        unit.GetComponent<Stats>().Overall = unit.GetComponent<Stats>().CalculateOverall();
        CalculateDominance(0, unit.GetComponent<Stats>().Overall, unit.tag);
    }

    public void UpdateInitiativeQueue()
    {
        //Sortowanie malejąco według wartości inicjatywy
        InitiativeQueue = InitiativeQueue.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

        DisplayInitiativeQueue();
    }

    private void DisplayInitiativeQueue()
    {
        // Resetuje wyświetlaną kolejkę, usuwając wszystkie obiekty "dzieci"
        ResetScrollViewContent(InitiativeScrollViewContent);
        ResetScrollViewContent(PlayersCamera_InitiativeScrollViewContent);

        ActiveUnit = null;

        // Ustala wyświetlaną kolejkę inicjatywy
        foreach (var pair in InitiativeQueue)
        {
            // Dodaje jednostkę do głównej kolejki ScrollViewContent
            GameObject optionObj = CreateInitiativeOption(pair, InitiativeScrollViewContent, false);

            // Dodaje jednostkę do Players kolejki ScrollViewContent
            GameObject playersOptionObj = CreateInitiativeOption(pair, PlayersCamera_InitiativeScrollViewContent, true);

            // Sprawdza, czy jest aktywna tura dla tej jednostki
            if ((pair.Key.CanDoAction || pair.Key.CanMove) && ActiveUnit == null && pair.Key.IsTurnFinished != true)
            {
                ActiveUnit = pair.Key;
                SetOptionColor(optionObj, _activeColor);
                SetOptionColor(playersOptionObj, _activeColor);
            }

            // Wyróżnia zaznaczoną jednostkę
            if (Unit.SelectedUnit != null && pair.Key == Unit.SelectedUnit.GetComponent<Unit>())
            {
                Color selectedColor = pair.Key == ActiveUnit ? _selectedActiveColor : _selectedColor;
                SetOptionColor(optionObj, selectedColor);
                SetOptionColor(playersOptionObj, selectedColor);
            }
            else if (pair.Key != ActiveUnit)
            {
                SetOptionColor(optionObj, _defaultColor);
                SetOptionColor(playersOptionObj, _defaultColor);
            }
        }
    }

    private void ResetScrollViewContent(Transform scrollViewContent)
    {
        for (int i = scrollViewContent.childCount - 1; i >= 0; i--)
        {
            Transform child = scrollViewContent.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    private GameObject CreateInitiativeOption(KeyValuePair<Unit, int> pair, Transform scrollViewContent, bool IsPlayersCamera_InitiativeQueue)
    {
        GameObject optionObj = Instantiate(_initiativeOptionPrefab, scrollViewContent);

        // Odniesienie do nazwy postaci
        TextMeshProUGUI nameText = optionObj.transform.Find("Name_Text").GetComponent<TextMeshProUGUI>();
        nameText.text = pair.Key.GetComponent<Stats>().Name;

        // Odniesienie do wartości inicjatywy
        TextMeshProUGUI initiativeText = optionObj.transform.Find("Initiative_Text").GetComponent<TextMeshProUGUI>();
        initiativeText.text = pair.Value.ToString();

        return optionObj;
    }

    private void SetOptionColor(GameObject optionObj, Color color)
    {
        optionObj.GetComponent<UnityEngine.UI.Image>().color = color;
    }

    public void SelectUnitByQueue()
    {
        StartCoroutine(InvokeSelectUnitCoroutine());
            
        IEnumerator InvokeSelectUnitCoroutine()
        {
            yield return new WaitForSeconds(0.05f);

            //Czeka ze zmianą postaci, aż obecna postać zakończy ruch
            while (MovementManager.Instance.IsMoving == true)
            {
                yield return null; // Czekaj na następną klatkę
            }

            DisplayInitiativeQueue();

            //Gdy jest aktywny tryb automatycznego wybierania postaci na podstawie kolejki inicjatywy to taka postać jest wybierana. Jeżeli wszystkie wykonały akcje to następuje kolejna runda
            if (GameManager.IsAutoSelectUnitMode && ActiveUnit != null && ActiveUnit.gameObject != Unit.SelectedUnit)
            {
                ActiveUnit.SelectUnit();
            }
            else if (GameManager.IsAutoSelectUnitMode && ActiveUnit == null && !GameManager.IsAutoCombatMode || GameManager.IsStatsHidingMode && ActiveUnit == null)
            {
                RoundsManager.Instance.NextRound();
            }     
        }
    }
    #endregion
    
    public void CalculateDominance(int addedOverall, int substractedOverall, string unitTag)
    {
        // Aktualizacja maksymalnej wartości przewagi
        if(DominanceBar.maxValue == 1) // Początkowa wartość Slidera (nie da sie ustawić na 0)
        { 
            DominanceBar.maxValue = addedOverall;
        }
        else
        {
            DominanceBar.maxValue += addedOverall - substractedOverall;
        }

        // Aktualizacja wartości przewagi gracza (tylko jeśli jednostka należy do gracza)
        if (unitTag == "PlayerUnit")
        {
            DominanceBar.value += addedOverall - substractedOverall;
        }

        // Aktywacja paska, jeśli ma sens go wyświetlać
        if (DominanceBar.maxValue > 1 && !DominanceBar.gameObject.activeSelf && !GameManager.IsStatsHidingMode)
        {
            DominanceBar.gameObject.SetActive(true);
        }
    }

    public void CalculateAdvantageBasedOnDominance()
    {
        // Zaktualizowanie przewag grupowych za różnicę sił
        if(DominanceBar.value < DominanceBar.maxValue / 3)
        {
            CalculateAdvantage("PlayerUnit", -1);
            CalculateAdvantage("EnemyUnit", 1);
            Debug.Log($"Przewaga przeciwników została zwiększona, a sojuszników zmniejszona o 1.");
        }
        else if(DominanceBar.value * 3 > DominanceBar.maxValue * 2)
        {
            CalculateAdvantage("EnemyUnit", -1);
            CalculateAdvantage("PlayerUnit", 1);
            Debug.Log($"Przewaga sojuszników została zwiększona, a przeciwników zmniejszona o 1.");
        }
    }

    public void CalculateAdvantage(string unitTag, int value)
    {
        if (unitTag == "PlayerUnit")
        {
            PlayersAdvantage = Mathf.Max(0, PlayersAdvantage + value);
            _playersAdvantageInput.text = PlayersAdvantage.ToString();
        }
        else if (unitTag == "EnemyUnit")
        {
            EnemiesAdvantage = Mathf.Max(0, EnemiesAdvantage + value);
            _enemiesAdvantageInput.text = EnemiesAdvantage.ToString();
        }
    }

    public void SetAdvantageByInput(TMP_InputField inputField)
    {
        if(inputField == _playersAdvantageInput)
        {
            PlayersAdvantage = int.TryParse(inputField.text, out int inputValue) ? inputValue : 0;
        }
        else if(inputField == _enemiesAdvantageInput)
        {
            EnemiesAdvantage = int.TryParse(inputField.text, out int inputValue) ? inputValue : 0;
        }
    }
}
