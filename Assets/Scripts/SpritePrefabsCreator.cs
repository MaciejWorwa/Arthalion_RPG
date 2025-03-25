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
        string prefabFolder = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // 3) Przechodzimy po każdej zaznaczonej teksturze
        foreach (Texture2D tex in selectedTextures)
        {
            // Uzyskujemy ścieżkę do pliku
            string path = AssetDatabase.GetAssetPath(tex);

            // 4) Ładujemy wszystkie sub‑assets (w tym sprite’y)
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
                Debug.LogWarning($"Nie znaleziono sprite’ów w \"{tex.name}\"");
                continue;
            }

            // 5) Dla każdego sprite'a tworzymy prefab
            foreach (Sprite sp in sprites)
            {
                // Nazwa prefab to np. "textureName_spriteName" lub samo sprite.name
                string prefabName = $"{tex.name}_{sp.name}.prefab";
                string prefabPath = Path.Combine(prefabFolder, prefabName);

                // Tymczasowy GameObject
                GameObject go = new GameObject(sp.name);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;

                // Zapisujemy prefab
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);

                // Usuwamy z pamięci
                Object.DestroyImmediate(go);

                Debug.Log($"Utworzono prefab: {prefabPath}");
            }
        }

        AssetDatabase.Refresh();
    }
}
