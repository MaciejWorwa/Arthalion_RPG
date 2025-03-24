using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MapElement : MonoBehaviour
{
    public bool IsHighObstacle;
    public bool IsLowObstacle;
    public bool IsCollider;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void SetColliderState(bool state)
    {
        if (GetComponent<BoxCollider2D>() != null)
        {
            // Ustawienie kolidera w zależności od wartości IsCollider
            GetComponent<BoxCollider2D>().enabled = state;
        }
    }

    private void OnMouseUp()
    {
        // Jeżeli nie jesteśmy w kreatorze pola bitwy to funkcja dodawania przeszkód jest wyłączona. 
        if (SceneManager.GetActiveScene().buildIndex != 0) return;

        if (MapElementUI.SelectedElement != null)
        {
            MapEditor.Instance.PlaceElementOnSelectedTile(transform.position);
        }
    }

    private void OnMouseOver()
    {
        // Jeżeli nie jesteśmy w kreatorze pola bitwy to funkcja usuwania i dodawania przeszkód jest wyłączona. 
        // Tak samo nie wywołujemy jej, gdy lewy przycisk myszy nie jest wciśnięty
        if (SceneManager.GetActiveScene().buildIndex != 0) return;

        if (GameManager.IsMousePressed)
        {
            if (MapEditor.IsElementRemoving)
            {
                MapEditor.Instance.RemoveElement(gameObject);
            }
            else if(MapElementUI.SelectedElement != null)
            {
                MapEditor.Instance.PlaceElementOnSelectedTile(transform.position);
            }
        }
    }
}
