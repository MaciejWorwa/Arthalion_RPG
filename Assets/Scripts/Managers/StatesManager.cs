using UnityEngine;

public class StatesManager : MonoBehaviour
{
  // Prywatne statyczne pole przechowujące instancję
    private static StatesManager instance;

    // Publiczny dostęp do instancji
    public static StatesManager Instance
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

    public void HandleUnitStates(Unit unit)
    {
        Bleeding(unit); // Krwawienie
        Broken(unit); // Panika
        Deafened(unit); // Ogłuszenie
        Poison(unit); // Zatrucie
        Stunned(unit); // Oszołomienie
        unit.Surprised = false;
    }

    private void Bleeding(Unit unit)
    {
        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();

        if(stats.TempHealth > 0)
        {
            stats.TempHealth -= unit.Bleeding;
        }
        else
        {
            unit.Unconscious = true; // Utrata Przytomności
        }
    }

    private void Broken(Unit unit)
    {
        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();
        bool isEngagedInCombat = CombatManager.Instance.AdjacentOpponents(unit.transform.position, unit.tag).Count > 0 ? true : false;

        if(unit.Broken > 0 && !isEngagedInCombat)
        {
            int successLevel = UnitsManager.Instance.TestSkill("SW", stats);
            if(successLevel > 0)
            {
                unit.Broken = Mathf.Max(0, unit.Broken - successLevel);
            }

            if(unit.Broken == 0) unit.Fatiqued ++; // Zwiększenie Wyczerpania
        }
    }

    private void Deafened(Unit unit)
    {
        if(unit.Deafened > 0) unit.Deafened --;
    }

    private void Poison(Unit unit)
    {
        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();

        if(unit.Poison > 0)
        {
            int successLevel = UnitsManager.Instance.TestSkill("Odp", stats);
            if(successLevel > 0)
            {
                unit.Poison = Mathf.Max(0, unit.Poison - successLevel);
            }

            if(unit.Poison == 0) unit.Fatiqued ++; // Zwiększenie Wyczerpania
        }

        if(stats.TempHealth > 0)
        {
            stats.TempHealth -= unit.Poison;
        }
        else
        {
            unit.Unconscious = true; // Utrata Przytomności
        }


    }

    private void Stunned(Unit unit)
    {
        Stats stats = Unit.SelectedUnit.GetComponent<Stats>();

        if(unit.Stunned > 0)
        {
            int successLevel = UnitsManager.Instance.TestSkill("Odp", stats);
            if(successLevel > 0)
            {
                unit.Stunned = Mathf.Max(0, unit.Stunned - successLevel);
            }

            if(unit.Stunned == 0) unit.Fatiqued ++; // Zwiększenie Wyczerpania
        }
    }

    public void StandUp(Unit unit)
    {
        unit.Prone = false;
        unit.CanMove = false;
        MovementManager.Instance.SetCanMoveToggle(false);
        Debug.Log($"<color=green>{unit.GetComponent<Stats>().Name} podnosi się z ziemi.</color>");
    }
}
