using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class SpritePrefabsCreator
{
    [MenuItem("Tools/Create Prefabs From Sprites")]
    public static void CreatePrefabsFromSelectedTextures()
    {
        // 1) Pobierz zaznaczone Texture2D
        Texture2D[] selectedTextures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);

        if (selectedTextures == null || selectedTextures.Length == 0)
        {
            Debug.LogWarning("Nie zaznaczono żadnych plików Texture2D w Project View!");
            return;
        }

        // 2) Ścieżka docelowa na prefaby
        string prefabFolder = "Assets/Resources/map_elements_prefabs";

        // Jeśli folder nie istnieje, tworzymy go
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "map_elements_prefabs");
        }

        // 3) Przechodzimy po każdej zaznaczonej teksturze
        foreach (Texture2D tex in selectedTextures)
        {
            // Uzyskujemy ścieżkę do pliku w Assets
            string path = AssetDatabase.GetAssetPath(tex);

            // 4) Ładujemy wszystkie sub‑assets (w tym sprite’y) spod tej ścieżki
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);

            // Lista sprite'ów
            List<Sprite> sprites = new List<Sprite>();
            foreach (Object obj in subAssets)
            {
                if (obj is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            if (sprites.Count == 0)
            {
                Debug.LogWarning($"Nie znaleziono sprite’ów w \"{tex.name}\" (ścieżka: {path})");
                continue;
            }

            // 5) Dla każdego sprite'a tworzymy prefab (lub pomijamy, jeśli istnieje)
            foreach (Sprite sp in sprites)
            {
                string prefabName = $"{sp.name}.prefab";
                string prefabPath = Path.Combine(prefabFolder, prefabName);

                // Sprawdzamy, czy w prefabPath już istnieje prefab
                // Jeśli tak, pomijamy
                GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (existingPrefab != null)
                {
                    Debug.Log($"Prefab \"{prefabName}\" już istnieje. Pomijam tworzenie.");
                    continue;
                }

                // --- Jeśli nie istnieje, tworzymy nowy prefab ---
                GameObject go = new GameObject(sp.name);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;

                // Dodajemy komponenty MapElement, MapElementUI i BoxCollider2D
                go.AddComponent<MapElement>();
                go.AddComponent<MapElementUI>();
                go.AddComponent<DraggableObject>();
                go.AddComponent<BoxCollider2D>();

                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);

                Debug.Log($"Utworzono prefab: {prefabPath}");
            }
        }

        // Odświeżenie AssetDatabase, by prefaby pojawiły się w Project
        AssetDatabase.Refresh();
    }
}
