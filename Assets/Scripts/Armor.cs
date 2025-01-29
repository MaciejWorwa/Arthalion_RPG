using UnityEngine;

public class Armor : MonoBehaviour
{
    public int Id;

    [Header("Nazwa")]
    public string Name;

    [Header("Jako��")]
    public string Quality;

    [Header("Kategoria")]
    public string Category;

    [Header("Obci��enie")]
    public int Encumbrance; // Obci��enie

    [Header("Uszkodzenie")]
    public int Damage; // Uszkodzenie pancerza

    [Header("Cechy")]
    public bool Bulky; // Niepor�czny (zwi�ksza obci��enie o 1) ---------------------- (MECHANIKA DO WPROWADZENIA)
    public int Durable; // Wytrzyma�y (str. 292) ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Flexible; // Gi�tki  ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Impenetrable; // Nieprzebijalny ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Lightweight; // Por�czny (redukuje obci��enie o 1) ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Partial; // Cz�ciowy  ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Practical; // Praktyczny (redukuje poziom pora�ki o 1)
    public bool Shoddy; //Tandetny  ---------------------- (MECHANIKA DO WPROWADZENIA)
    public bool Unrielable; // Zawodny (zwi�ksza poziom pora�ki o 1)
    public bool WeakPoints; // Wra�liwe punkty  ---------------------- (MECHANIKA DO WPROWADZENIA)
}
