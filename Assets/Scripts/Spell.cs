using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spell : MonoBehaviour
{
    public int Id;
    public string Name;
    public string Arcane;
    public string[] Type; // np. offensive, buff, armor-ignoring, magic-missile 
    public int CastingNumber; //poziom mocy
    public float Range; // zasi�g
    public int Strength; // si�a zakl�cia
    public int AreaSize; // obszar dzia�ania
    public int Duration; // czas trwania zakl�cia

    public bool SaveTestRequiring; // okre�la, czy zakl�cie powoduje konieczno�� wykonania testu obronnego
    //public int AttributeValue; // okre�la o ile s� zmieniane cechy opisane w tabeli Attribute
    //public string[] Attribute; // okre�la cech�, jaka jest testowana podczas pr�by oparcia si� zakl�ciu lub cech� na kt�r� wp�ywa zakl�cie (np. podnosi j� lub obni�a). Czasami jest to wi�cej cech, np. Pancerz Etery wp�ywa na ka�d� z lokalizacji
    //public Dictionary<string, int> Attributes = new(); // <-- zamiast Attribute + AttributeValue

    public List<AttributePair> Attributes;  // U�ywamy List<AttributePair>, nie s�ownika.


    public bool ArmourIgnoring; // ignoruj�cy zbroj�
    public bool MetalArmourIgnoring; // ignoruj�cy zbroj�
    public bool WtIgnoring; // ignoruj�cy wytrzyma�o��
    //public bool Stunning;  // og�uszaj�cy
    //public bool Paralyzing; // wprowadzaj�cy w stan bezbronno�ci
}
