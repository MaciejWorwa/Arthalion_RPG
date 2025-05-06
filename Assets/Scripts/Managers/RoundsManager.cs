using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class RoundsManager : MonoBehaviour
{
    // Prywatne statyczne pole przechowujące instancję
    private static RoundsManager instance;

    // Publiczny dostęp do instancji
    public static RoundsManager Instance
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
    public static int RoundNumber;
    [SerializeField] private TMP_Text _roundNumberDisplay;
    [SerializeField] private TMP_Text _playersRoundNumberDisplay;
    public UnityEngine.UI.Button NextRoundButton;
    [SerializeField] private UnityEngine.UI.Toggle _canDoActionToggle;
    [SerializeField] private GameObject _useFortunePointsButton;
    private bool _isFortunePointSpent; //informacja o tym, że punkt szczęścia został zużyty, aby nie można było ponownie go użyć do wczytania tego samego autozapisu

    private void Start()
    {
        RoundNumber = 0;
        _roundNumberDisplay.text = "Zaczynamy?";
        _playersRoundNumberDisplay.text = "";

        NextRoundButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Start";

        _useFortunePointsButton.SetActive(false);
    }

    public void NextRound()
    {
        RoundNumber++;
        _roundNumberDisplay.text = "Runda: " + RoundNumber;
        _playersRoundNumberDisplay.text = "Runda: " + RoundNumber;

        if (RoundNumber > 0)
        {
            NextRoundButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Następna runda";
        }

        Debug.Log($"<color=#4dd2ff>------------------------------------------------------------------------------------ RUNDA {RoundNumber} ------------------------------------------------------------------------------------</color>");

        //Aktualizuje przewagę, jeśli któraś ze stron wyraźnie dominuje siłą
        if (UnitsManager.Instance.BothTeamsExist())
        {
            InitiativeQueueManager.Instance.CalculateAdvantageBasedOnDominance();
        }

        // Jednostki z cechą "Niestabilny"
        List<Unit> unstableUnits = new List<Unit>();

        //Resetuje ilość dostępnych akcji dla wszystkich jednostek
        foreach (Unit unit in UnitsManager.Instance.AllUnits)
        {
            if (unit == null) continue;

            Stats stats = unit.GetComponent<Stats>();

            //Stosuje zdolności specjalne różnych jednostek (np. regeneracja żywotności trolla)
            stats.CheckForSpecialRaceAbilities();

            if (stats.Unstable) unstableUnits.Add(unit);

            unit.IsTurnFinished = false;
            unit.CanDoAction = true;
            SetCanDoActionToggle(true);
            unit.CanMove = true;
            MovementManager.Instance.SetCanMoveToggle(true);

            if (unit.Stunned > 0)
            {
                unit.CanDoAction = false;
            }
            if (unit.Unconscious)
            {
                unit.CanDoAction = false;
                unit.CanMove = false;
            }
            if (unit.Entangled > 0)
            {
                unit.CanMove = false;
            }
            if (stats.MagicLanguage > 0)
            {
                unit.CanCastSpell = true;
                unit.CanDispell = true;
            }

            //if (unit.SpellDuration > 0)
            //{
            //    unit.SpellDuration--;

            //    if (unit.SpellDuration == 0)
            //    {
            //        MagicManager.Instance.ResetSpellEffect(unit);
            //    }
            //}

            if (stats.ActiveSpellEffects != null && stats.ActiveSpellEffects.Count != 0)
            {
                stats.UpdateSpellEffects();
            }

            if (unit.EntangledUnitId != 0)
            {
                bool entangledUnitExist = false;

                foreach (var u in UnitsManager.Instance.AllUnits)
                {
                    if (u.UnitId == unit.EntangledUnitId && u.Entangled > 0)
                    {
                        entangledUnitExist = true;
                    }
                }

                if (!entangledUnitExist)
                {
                    unit.EntangledUnitId = 0;
                }
            }

            // Dla jednostek z talentem Waleczne Serce wykonujemy dodatkową próbę opanownia paniki
            if (stats.StoutHearted > 0 && unit.Broken > 0)
            {
                StartCoroutine(StatesManager.Instance.Broken(unit));
            }

            // Dla jednostek z talentem Atak wyprzedzający resetujemy pulę ataków
            if (stats.ReactionStrike > 0)
            {
                stats.ReactionStrikesLeft = stats.ReactionStrike;
            }

            // Dla jednostek z talentem Riposta resetujemy pulę ataków
            if (stats.Riposte > 0)
            {
                stats.RiposteAttacksLeft = stats.Riposte;
            }

            // Dla jednostek w Szale Bojowym resetujemy pulę ataków
            if (unit.IsFrenzy)
            {
                stats.FrenzyAttacksLeft = 2;
            }

            //Aktualizuje osiągnięcia
            stats.RoundsPlayed++;
        }

        // Uwzględnienie cechy Niestabilny
        if (unstableUnits.Count > 0)
        {
            System.Random random = new System.Random();
            Unit randomUnit = unstableUnits[random.Next(unstableUnits.Count)];
            Stats unstableStats = randomUnit.GetComponent<Stats>();

            bool applyDamage = false;
            int damage = 0;

            if (randomUnit.CompareTag("PlayerUnit") && InitiativeQueueManager.Instance.EnemiesAdvantage > InitiativeQueueManager.Instance.PlayersAdvantage)
            {
                damage = InitiativeQueueManager.Instance.EnemiesAdvantage - InitiativeQueueManager.Instance.PlayersAdvantage;
                applyDamage = true;
            }
            else if (randomUnit.CompareTag("EnemyUnit") && InitiativeQueueManager.Instance.PlayersAdvantage > InitiativeQueueManager.Instance.EnemiesAdvantage)
            {
                damage = InitiativeQueueManager.Instance.PlayersAdvantage - InitiativeQueueManager.Instance.EnemiesAdvantage;
                applyDamage = true;
            }

            if (applyDamage)
            {
                unstableStats.TempHealth -= damage;
                StartCoroutine(AnimationManager.Instance.PlayAnimation("damage", null, randomUnit.gameObject, damage));
                randomUnit.DisplayUnitHealthPoints();

                Debug.Log($"<color=#FF7F50>{unstableStats.Name} otrzymuje {damage} obrażeń w związku z cechą \"Niestabilny\".</color>");

                if (unstableStats.TempHealth <= 0)
                {
                    if (GameManager.IsAutoKillMode)
                    {
                        CombatManager.Instance.HandleDeath(unstableStats, unstableStats.gameObject, null);
                        Debug.Log($"<color=red>{unstableStats.Name} umiera.</color>");
                    }
                    else
                    {
                        Debug.Log($"<color=red>Żywotność {unstableStats.Name} spadła poniżej 0.</color>");
                    }
                }
            }
        }

        // Wykonuje testy grozy i strachu, jeśli na polu bitwy są jednostki straszne lub przerażające
        if (GameManager.IsFearIncluded == true)
        {
            UnitsManager.Instance.LookForScaryUnits();
        }

        InitiativeQueueManager.Instance.UpdateInitiativeQueue();

        //Odświeża panel jednostki, aby zaktualizowac ewentualną informację o długości trwania stanu (np. ogłuszenia) wybranej jednostki
        if (Unit.SelectedUnit != null)
        {
            UnitsManager.Instance.UpdateUnitPanel(Unit.SelectedUnit);
        }

        //Wybiera jednostkę zgodnie z kolejką inicjatywy, jeśli ten tryb jest włączony
        if (GameManager.IsAutoSelectUnitMode && InitiativeQueueManager.Instance.ActiveUnit != null)
        {
            InitiativeQueueManager.Instance.SelectUnitByQueue();
        }

        //Wykonuje automatyczną akcję za każdą jednostkę
        if (GameManager.IsAutoCombatMode)
        {
            StartCoroutine(AutoCombat());
        }
    }

    IEnumerator AutoCombat()
    {
        NextRoundButton.gameObject.SetActive(false);
        _useFortunePointsButton.SetActive(false);

        for (int i = 0; i < UnitsManager.Instance.AllUnits.Count; i++)
        {
            if (UnitsManager.Instance.AllUnits[i] == null || !InitiativeQueueManager.Instance.InitiativeQueue.ContainsKey(UnitsManager.Instance.AllUnits[i])) continue;

            InitiativeQueueManager.Instance.SelectUnitByQueue();
            yield return new WaitForSeconds(0.1f);

            Unit unit = null;
            if (Unit.SelectedUnit != null)
            {
                unit = Unit.SelectedUnit.GetComponent<Unit>();
            }
            else continue;

            // Jeśli jednostka to PlayerUnit i gramy w trybie ukrywania statystyk wrogów
            if (unit.CompareTag("PlayerUnit") && GameManager.IsStatsHidingMode)
            {
                // Czeka aż jednostka zakończy swoją turę
                yield return new WaitUntil(() => (unit.CanDoAction == false && unit.CanMove == false) || unit.IsTurnFinished);
                yield return new WaitForSeconds(0.6f);
            }
            else // Jednostki wrogów lub wszystkie jednostki, jeśli nie ukrywamy ich statystyk
            {
                //TYMCZASOWE - test algorytmów gentycznych
                if (ReinforcementLearningManager.Instance.IsLearning)
                {
                    if (unit.CompareTag("PlayerUnit"))
                    {
                        AutoCombatManager.Instance.Act(unit);
                    }
                    else
                    {
                        int iterationCount = 0;

                        while ((unit.CanDoAction || unit.CanMove) && !unit.IsTurnFinished && iterationCount < 5)
                        {
                            ReinforcementLearningManager.Instance.SimulateUnit(unit);
                            iterationCount++;
                        }
                        if (iterationCount >= 5 && !unit.IsTurnFinished)
                        {
                            FinishTurn();
                        }
                    }
                }
                else
                {
                    AutoCombatManager.Instance.Act(unit);
                }

                //AutoCombatManager.Instance.Act(unit);

                // Czeka, aż jednostka zakończy ruch
                yield return new WaitUntil(() => MovementManager.Instance.IsMoving == false);
                yield return new WaitForSeconds(0.6f);

                if (!unit.IsTurnFinished && (unit.CanDoAction || unit.CanMove))
                {
                    FinishTurn();
                }
            }
        }

        NextRoundButton.gameObject.SetActive(true);
        _useFortunePointsButton.SetActive(true);

        //DO SZKOLENIA AI
        if (ReinforcementLearningManager.Instance.IsLearning)
        {
            if (ReinforcementLearningManager.Instance.BothTeamsExist() == false || RoundNumber > 50)
            {
                // Iteruj po wszystkich jednostkach, które jeszcze żyją i są częścią drużyny Enemy
                foreach (Unit unit in UnitsManager.Instance.AllUnits)
                {
                    if (unit != null && unit.CompareTag("EnemyUnit") && unit.GetComponent<Stats>().TempHealth > 0)
                    {
                        ReinforcementLearningManager.Instance.AddTeamWinRewardForUnit(unit);
                    }
                }

                ReinforcementLearningManager.Instance.UpdateTeamWins();

                SaveAndLoadManager.Instance.SetLoadingType(true);
                SaveAndLoadManager.Instance.LoadGame("AIlearning");
            }

            yield return new WaitUntil(() => SaveAndLoadManager.Instance.IsLoading == false);

            GridManager.Instance.CheckTileOccupancy();
            NextRound();
        }
    }

    #region Units actions
    public void DoAction(Unit unit)
    {
        //Zapobiega zużywaniu akcji przed rozpoczęciem bitwy
        if (RoundNumber == 0) return;

        if (unit.CanDoAction)
        {
            // Automatyczny zapis, aby możliwe było użycie punktów szczęścia lub zepsucia
            if (!GameManager.IsAutoCombatMode)
            {
                SaveAndLoadManager.Instance.SaveUnits(UnitsManager.Instance.AllUnits, "autosave");
                _isFortunePointSpent = false;
            }

            unit.CanDoAction = false;
            DisplayActionsLeft();

            Debug.Log($"<color=green>{unit.GetComponent<Stats>().Name} wykonał/a akcję. </color>");

            //Zresetowanie szarży lub biegu, jeśli były aktywne (po zużyciu jednej akcji szarża i bieg nie mogą być możliwe)
            //MovementManager.Instance.UpdateMovementRange(1);

            //Resetuje pozycję obronną, jeśli była aktywna
            if (unit.GetComponent<Unit>().DefensiveBonus != 0)
            {
                CombatManager.Instance.DefensiveStance();
            }

            //W przypadku ręcznego zadawania obrażeń, czekamy na wpisanie wartości obrażeń przed zmianą jednostki (jednostka jest wtedy zmieniana w funkcji ExecuteAttack w CombatManager)
            if (!CombatManager.Instance.IsManualPlayerAttack && !unit.CanMove && !unit.CanDoAction)
            {
                FinishTurn();
            }

            return;
        }
        else
        {
            Debug.Log("Ta jednostka nie może w tej rundzie wykonać więcej akcji.");
            return;
        }
    }

    public void DisplayActionsLeft()
    {
        if (Unit.SelectedUnit == null)
        {
            _useFortunePointsButton.SetActive(false);
        }
        else
        {
            Unit unit = Unit.SelectedUnit.GetComponent<Unit>();

            SetCanDoActionToggle(unit.CanDoAction);
            MovementManager.Instance.SetCanMoveToggle(unit.CanMove);

            if (_isFortunePointSpent != true && !unit.CanDoAction && !GameManager.IsAutoCombatMode)
            {
                _useFortunePointsButton.SetActive(true);
            }
        }
    }

    public void UseFortunePoint()
    {
        if (Unit.SelectedUnit == null) return;

        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();
        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();

        if (unit.CanDoAction)
        {
            if (Unit.LastSelectedUnit == null) return;
            stats = Unit.LastSelectedUnit.GetComponent<Stats>();
        }

        if (stats.PS == 0)
        {
            Debug.Log($"{stats.Name} nie posiada Punktów Szczęścia, co skutkuje zwiększeniem Punktów Zepsucia. Wykonaj akcję ponownie.");
            stats.CorruptionPoints++;
        }
        else
        {
            Debug.Log($"{stats.Name} zużywa Punkt Szczęścia. Wykonaj akcję ponownie.");
            stats.PS--;
        }

        _isFortunePointSpent = true;

        SaveAndLoadManager.Instance.SaveFortunePoints("autosave", stats, stats.PS);
        SaveAndLoadManager.Instance.LoadGame("autosave");

        _useFortunePointsButton.SetActive(false);
    }

    //Zakończenie tury danej jednostki mimo tego, że ma jeszcze dostępne akcje
    public void FinishTurn()
    {
        if (Unit.SelectedUnit == null) return;

        Unit unit = Unit.SelectedUnit.GetComponent<Unit>();
        unit.IsTurnFinished = true;

        // Bierze pod uwagę efekty ewentualnych stanów postaci
        StatesManager.Instance.UpdateUnitStates(unit);

        if (unit.CanMove || unit.CanDoAction)
        {
            Debug.Log($"<color=green>{unit.Stats.Name} kończy swoją turę.</color>");
        }

        InitiativeQueueManager.Instance.SelectUnitByQueue();
    }
    #endregion

    public void LoadRoundsManagerData(RoundsManagerData data)
    {
        RoundNumber = data.RoundNumber;
        if (RoundNumber > 0)
        {
            _roundNumberDisplay.text = "Runda: " + RoundNumber;
            NextRoundButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Następna runda";
        }
        else
        {
            _roundNumberDisplay.text = "Zaczynamy?";
            NextRoundButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Start";
        }
    }

    public void SetCanDoActionToggle(bool canDoAction)
    {
        _canDoActionToggle.isOn = canDoAction;
    }
    public void SetCanDoActionByToggle()
    {
        if (Unit.SelectedUnit == null) return;
        Unit.SelectedUnit.GetComponent<Unit>().CanDoAction = _canDoActionToggle.isOn;
    }
}
