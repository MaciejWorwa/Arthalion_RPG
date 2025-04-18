using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class MountSelector : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        string unitName = transform.Find("Name_Text").GetComponent<TMP_Text>().text;

        foreach (KeyValuePair<Unit, int> pair in InitiativeQueueManager.Instance.InitiativeQueue)
        {
            if (pair.Key.GetComponent<Stats>().Name == unitName)
            {
                MountsManager.SelectedMount = pair.Key;
            }
        }

        if (MountsManager.SelectedMount != null)
        {
            MountsManager.Instance.DisplayMountsList();
        }
    }
}
