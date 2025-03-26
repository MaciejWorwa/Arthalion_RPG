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
        if (GameManager.IsMousePressed)
        {
            if(Input.GetMouseButtonDown(1))
            {
                // Obrót o 90 stopni
                transform.rotation *= Quaternion.Euler(0, 0, 90);
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                MapEditor.Instance.RemoveElement(gameObject);
            }
        }

        // Jeżeli nie jesteśmy w kreatorze pola bitwy to funkcja usuwania i dodawania przeszkód jest wyłączona. 
        // Tak samo nie wywołujemy jej, gdy lewy przycisk myszy nie jest wciśnięty
        if (SceneManager.GetActiveScene().buildIndex != 0) return;

        if (GameManager.IsMousePressed)
        {
            if (MapEditor.IsElementRemoving || Input.GetKeyDown(KeyCode.Delete))
            {
                MapEditor.Instance.RemoveElement(gameObject);
            }
            else if (MapElementUI.SelectedElement != null)
            {
                Vector3 originalPosition = transform.position;
                BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
                float rotationZ = transform.eulerAngles.z; // Pobranie aktualnej rotacji obiektu

                // Pobranie pozycji kursora myszy w przestrzeni świata
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                if (boxCollider != null)
                {
                    if (boxCollider.size.y > boxCollider.size.x) // Obiekty pionowe (2x1)
                    {
                        if (rotationZ < 45 || (rotationZ >= 135 && rotationZ < 225) || rotationZ > 315)
                        {
                            originalPosition.y -= 0.5f;
                            if (mouseWorldPos.y > originalPosition.y) originalPosition.y += 1.0f;
                        }
                        else
                        {
                            originalPosition.x += 0.5f;
                            if (mouseWorldPos.x < originalPosition.x) originalPosition.x -= 1.0f;
                        }
                    }
                    else if (boxCollider.size.y < boxCollider.size.x) // Obiekty poziome (1x2)
                    {
                        if ((rotationZ >= 45 && rotationZ < 135) || (rotationZ >= 225 && rotationZ < 315))
                        {
                            originalPosition.y -= 0.5f;
                            if (mouseWorldPos.y > originalPosition.y) originalPosition.y += 1.0f;
                        }
                        else
                        {
                            originalPosition.x += 0.5f;
                            if (mouseWorldPos.x < originalPosition.x) originalPosition.x -= 1.0f;
                        }
                    }
                    else if (transform.localScale.x > 1.5f || (boxCollider.size.x > 1.7f && boxCollider.size.y > 1.7f)) // Obiekty 2x2
                    {
                        if ((rotationZ >= 45 && rotationZ < 135) || (rotationZ >= 225 && rotationZ < 315))
                        {
                            originalPosition.x += 0.5f;
                            originalPosition.y -= 0.5f;
                            if (mouseWorldPos.x < originalPosition.x) originalPosition.x -= 1.0f;
                            if (mouseWorldPos.y > originalPosition.y) originalPosition.y += 1.0f;
                        }
                        else
                        {
                            originalPosition.x -= 0.5f;
                            originalPosition.y += 0.5f;
                            if (mouseWorldPos.x > originalPosition.x) originalPosition.x += 1.0f;
                            if (mouseWorldPos.y < originalPosition.y) originalPosition.y -= 1.0f;
                        }
                    }
                }

                MapEditor.Instance.PlaceElementOnSelectedTile(originalPosition);
            }
        }
    }
}
