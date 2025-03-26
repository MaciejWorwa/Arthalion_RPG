using System;
using UnityEngine;

public static class NameCleaner
{
    /// <summary>
    /// Je�li nazwa sk�ada si� z wielokrotnego powt�rzenia 
    /// jakiego� podci�gu token�w, zwraca ten podci�g 
    /// (np. "Bare_Tree_Ashen_B7_10x10" 4x).
    /// W przeciwnym razie zwraca orygina�.
    /// </summary>
    public static string FindRepeatedPatternName(string originalName)
    {
        // 1) Rozdziel nazw� po podkre�lnikach
        string[] tokens = originalName.Split('_');
        int total = tokens.Length;
        if (total < 2) return originalName; // Nic do szukania, za kr�tka nazwa

        // 2) Przechodzimy przez wszystkie mo�liwe d�ugo�ci blok�w
        //    od 1 do total
        for (int blockLength = 1; blockLength <= total; blockLength++)
        {
            // Czy total jest wielokrotno�ci� blockLength?
            if (total % blockLength != 0)
                continue; // Nie ma sensu dalej, bo blockLength nie dzieli si� w total

            // Spr�bujmy zebra� "wzorcowy" blok: tokeny [0..blockLength-1]
            bool allBlocksIdentical = true;

            // Por�wnujemy pozosta�e bloki do wzorca
            for (int start = blockLength; start < total; start += blockLength)
            {
                // Sprawdzamy, czy blok token�w [start .. start+blockLength-1]
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

            // Je�li wszystkie bloki si� zgadzaj�, mamy nasz wzorzec
            if (allBlocksIdentical)
            {
                // 3) Zwracamy jeden blok [0..blockLength-1], 
                //    sklejony z powrotem do stringa z podkre�lnikami
                string[] firstBlock = new string[blockLength];
                Array.Copy(tokens, 0, firstBlock, 0, blockLength);

                // sklej w "Bare_Tree_Ashen_B7_10x10"
                return string.Join("_", firstBlock);
            }
        }

        // 4) Je�li �adnego powtarzaj�cego wzorca nie znaleziono,
        //    zwracamy orygina�
        return originalName;
    }
}
