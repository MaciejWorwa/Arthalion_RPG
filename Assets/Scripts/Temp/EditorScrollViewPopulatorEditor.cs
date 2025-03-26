#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement; // Daje MarkSceneDirty

[CustomEditor(typeof(EditorScrollViewPopulator))]
public class EditorScrollViewPopulatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Rysujemy standardowe pola (contentParent, itemPrefab itp.)
        base.OnInspectorGUI();

        // Dodajemy przycisk
        if (GUILayout.Button("Wygeneruj obiekty w ScrollView (EDYTOR)"))
        {
            // Rzutujemy target na nasz skrypt
            EditorScrollViewPopulator populator = (EditorScrollViewPopulator)target;

            // Wywo�ujemy metod�, kt�ra generuje elementy w scenie
            populator.GenerateObjectsInEditor();

            // Oznaczamy scen� jako zmodyfikowan�, by Unity da�o opcj� zapisu
            EditorUtility.SetDirty(populator.gameObject);
            EditorSceneManager.MarkSceneDirty(populator.gameObject.scene);
        }
    }
}
#endif
