using UnityEngine;
using UnityEngine.UI; // bo u�ywamy Image

public class EditorScrollViewPopulator : MonoBehaviour
{
    [Header("Content ScrollView")]
    public Transform contentParent;  // Wska� tu w Inspectorze: ScrollView -> Viewport -> Content

    [Header("Szablon elementu (ItemUI)")]
    public GameObject itemPrefab;    // Wska� prefab z dzieckiem "Image"

    /// <summary>
    /// Metoda wywo�ywana w edytorze (przez Custom Editor),
    /// kt�ra tworzy obiekty na sta�e w scenie.
    /// </summary>
    public void GenerateObjectsInEditor()
    {
        if (contentParent == null || itemPrefab == null)
        {
            Debug.LogWarning("Brak contentParent lub itemPrefab!");
            return;
        }

        // 1) Wczytujemy wszystkie prefaby z folderu "Resources/map_assets"
        var loadedPrefabs = Resources.LoadAll<GameObject>("map_elements_prefabs");
        if (loadedPrefabs == null || loadedPrefabs.Length == 0)
        {
            Debug.LogWarning("Nie znaleziono prefab�w w Resources/map_assets!");
            return;
        }

        // (Opcjonalnie) usuwamy stare dzieci, by nie dublowa�
        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            // U�ywamy DestroyImmediate, by skasowa� obiekty w edytorze od razu
            DestroyImmediate(contentParent.GetChild(i).gameObject);
        }

        // 2) Iterujemy po wszystkich prefabach
        foreach (var prefab in loadedPrefabs)
        {
            if (prefab == null) continue;

            // 2a) Tworzymy kopi� itemPrefab w edytorze jako dziecko contentParent
            GameObject newItem = Instantiate(itemPrefab, contentParent);

            // 2b) Nadajemy nazw� tak�, jak prefab
            newItem.name = prefab.name;

            // 2c) Znajdujemy w newItem dziecko "Image"
            var imageTransform = newItem.transform.Find("Image");
            if (imageTransform != null)
            {
                var image = imageTransform.GetComponent<Image>();
                if (image != null)
                {

                    string rawName = prefab.name;
                    // Oczyszczona nazwa, wykrycie powtarzaj�cego si� wzorca
                    string cleanName = NameCleaner.FindRepeatedPatternName(rawName);

                    string path = "Sprites/Map_elements/New/" + cleanName;
                    Debug.Log($"Pr�buj� wczyta�: {path}");
                    Sprite sprite = Resources.Load<Sprite>(path);

                    if (sprite == null)
                    {
                        Debug.LogWarning($"Nie znaleziono sprite'a przy �cie�ce: {path}");
                    }
                    else
                    {
                        Debug.Log($"Wczytano sprite: {sprite.name}");
                        image.sprite = sprite;
                    }

                }
            }
        }

        Debug.Log($"Utworzono {loadedPrefabs.Length} element�w w {contentParent.name}");
    }
}
