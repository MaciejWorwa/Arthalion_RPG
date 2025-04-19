using System.Collections.Generic;

public class SpellEffect
{
    // Nazwa zaklęcia, z którego pochodzi dany efekt (przydatne przy logowaniu)
    public string SpellName;
    // Liczba rund, przez które efekt będzie aktywny
    public int RemainingRounds;
    // Słownik zawierający modyfikacje (klucz: nazwa statystyki, wartość: modyfikator)
    public Dictionary<string, int> StatModifiers;

    public SpellEffect(string spellName, int remainingRounds, Dictionary<string, int> statModifiers)
    {
        SpellName = spellName;
        RemainingRounds = remainingRounds;
        StatModifiers = statModifiers;
    }
}
