using System;
using UnityEngine;

public static class NameCleaner
{
    /// <summary>
    /// Jeœli nazwa sk³ada siê z wielokrotnego powtórzenia 
    /// jakiegoœ podci¹gu tokenów, zwraca ten podci¹g 
    /// (np. "Bare_Tree_Ashen_B7_10x10" 4x).
    /// W przeciwnym razie zwraca orygina³.
    /// </summary>
    public static string FindRepeatedPatternName(string originalName)
    {
        // 1) Rozdziel nazwê po podkreœlnikach
        string[] tokens = originalName.Split('_');
        int total = tokens.Length;
        if (total < 2) return originalName; // Nic do szukania, za krótka nazwa

        // 2) Przechodzimy przez wszystkie mo¿liwe d³ugoœci bloków
        //    od 1 do total
        for (int blockLength = 1; blockLength <= total; blockLength++)
        {
            // Czy total jest wielokrotnoœci¹ blockLength?
            if (total % blockLength != 0)
                continue; // Nie ma sensu dalej, bo blockLength nie dzieli siê w total

            // Spróbujmy zebraæ "wzorcowy" blok: tokeny [0..blockLength-1]
            bool allBlocksIdentical = true;

            // Porównujemy pozosta³e bloki do wzorca
            for (int start = blockLength; start < total; start += blockLength)
            {
                // Sprawdzamy, czy blok tokenów [start .. start+blockLength-1]
                // jest identyczny z [0..blockLength-1]
                for (int i = 0; i < blockLength; i++)
                {
                    if (!tokens[i].Equals(tokens[start + i], StringComparison.OrdinalIgnoreCase))
                    {
                        allBlocksIdentical = false;
                        break;
                    }
                }
                if (!allBlocksIdentical)
                    break;
            }

            // Jeœli wszystkie bloki siê zgadzaj¹, mamy nasz wzorzec
            if (allBlocksIdentical)
            {
                // 3) Zwracamy jeden blok [0..blockLength-1], 
                //    sklejony z powrotem do stringa z podkreœlnikami
                string[] firstBlock = new string[blockLength];
                Array.Copy(tokens, 0, firstBlock, 0, blockLength);

                // sklej w "Bare_Tree_Ashen_B7_10x10"
                return string.Join("_", firstBlock);
            }
        }

        // 4) Jeœli ¿adnego powtarzaj¹cego wzorca nie znaleziono,
        //    zwracamy orygina³
        return originalName;
    }
}
