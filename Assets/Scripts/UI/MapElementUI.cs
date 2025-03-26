using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapElementUI : MonoBehaviour, IPointerClickHandler
{
    public static Image SelectedElementImage;
    public static GameObject SelectedElement;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("DDDDDDDD");
        MapEditor.Instance.RemoveElementsMode(false);

        //Zresetuj kolor poprzednio wybranego elementu
        if (SelectedElementImage != null)
        {
            ResetColor(SelectedElementImage);
        }

        //Jeżeli ponownie klikniemy na ten sam element to wychodzimy z trybu dodawania elementów
        if(SelectedElement == Resources.Load<GameObject>($"map_elements_prefabs/{this.gameObject.name}"))
        {
            MapEditor.Instance.ResetAllSelectedElements();
            return;
        }

        // Odniesienie do wybranego elementu w panelu
        SelectedElementImage = this.GetComponent<Image>();

        // Odniesienie do prefabu wybranego elementu
        SelectedElement = Resources.Load<GameObject>($"map_elements_prefabs/{this.gameObject.name}");

        //Zmień kolor nowo wybranego elementu
        HighlightElement(SelectedElementImage);

        Debug.Log("Wybierz pole, na którym chcesz umieścić wybrany element otoczenia. Przytrzymując lewy przycisk myszy i przesuwając po mapie, możesz umieszczać wiele elementów naraz.");
    }

    public void ResetColor(Image image)
    {
        image.color = new Color(0f, 0f, 0f, 0.4f);
    }

    public void HighlightElement(Image image)
    {
        image.color = new Color(0f, 0.8f, 0f, 0.35f);
        //image.color = new Color(0.47f, 0.6f, 0.725f, 1f);
    }
}
