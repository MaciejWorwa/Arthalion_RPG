using System.Collections.Generic;

public class SpellEffect
{
    // Nazwa zaklêcia, z którego pochodzi dany efekt (przydatne przy logowaniu)
    public string SpellName;
    // Liczba rund, przez które efekt bêdzie aktywny
    public int RemainingRounds;
    // S³ownik zawieraj¹cy modyfikacje (klucz: nazwa statystyki, wartoœæ: modyfikator)
    public Dictionary<string, int> StatModifiers;

    public SpellEffect(string spellName, int remainingRounds, Dictionary<string, int> statModifiers)
    {
        SpellName = spellName;
        RemainingRounds = remainingRounds;
        StatModifiers = statModifiers;
    }
}
