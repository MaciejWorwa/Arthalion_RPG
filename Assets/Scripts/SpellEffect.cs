using System.Collections.Generic;

public class SpellEffect
{
    // Nazwa zakl�cia, z kt�rego pochodzi dany efekt (przydatne przy logowaniu)
    public string SpellName;
    // Liczba rund, przez kt�re efekt b�dzie aktywny
    public int RemainingRounds;
    // S�ownik zawieraj�cy modyfikacje (klucz: nazwa statystyki, warto��: modyfikator)
    public Dictionary<string, int> StatModifiers;

    public SpellEffect(string spellName, int remainingRounds, Dictionary<string, int> statModifiers)
    {
        SpellName = spellName;
        RemainingRounds = remainingRounds;
        StatModifiers = statModifiers;
    }
}
