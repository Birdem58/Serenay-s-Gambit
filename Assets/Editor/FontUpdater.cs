using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;

public class FontUpdater : EditorWindow
{
    [MenuItem("Tools/Update Project Fonts")]
    public static void UpdateFonts()
    {
        TMP_FontAsset targetTMPFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ART/Poppins-Medium SDF.asset");
        Font targetLegacyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/ART/Poppins-Medium.ttf");

        if (targetTMPFont == null)
        {
            Debug.LogError("Poppins-Medium SDF.asset not found at Assets/ART/Poppins-Medium SDF.asset");
            return;
        }

        if (targetLegacyFont == null)
        {
            Debug.LogError("Poppins-Medium.ttf not found at Assets/ART/Poppins-Medium.ttf");
            return;
        }

        // 1. Update TMP Settings (Double check)
        var settings = TMP_Settings.instance;
        if (settings != null)
        {
            SerializedObject serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("m_defaultFontAsset").objectReferenceValue = targetTMPFont;
            serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            Debug.Log("Updated TMP default font setting.");
        }

        // 2. Scan all scenes
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            
            bool sceneDirty = false;
            
            // Find all TMP Text components (including inactive ones)
            TMP_Text[] tmpTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            foreach (var txt in tmpTexts)
            {
                // Skip prefab assets or components not in the scene
                if (EditorUtility.IsPersistent(txt.gameObject))
                    continue;

                if (txt.font != targetTMPFont)
                {
                    Undo.RecordObject(txt, "Update TMP Font");
                    txt.font = targetTMPFont;
                    EditorUtility.SetDirty(txt);
                    sceneDirty = true;
                }
            }

            // Find legacy Text components (including inactive ones)
            UnityEngine.UI.Text[] legacyTexts = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Text>();
            foreach (var txt in legacyTexts)
            {
                if (EditorUtility.IsPersistent(txt.gameObject))
                    continue;

                if (txt.font != targetLegacyFont)
                {
                    Undo.RecordObject(txt, "Update Legacy Font");
                    txt.font = targetLegacyFont;
                    EditorUtility.SetDirty(txt);
                    sceneDirty = true;
                }
            }

            if (sceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Updated fonts in scene: {scenePath}");
            }
        }

        // 3. Scan all prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null) continue;

            bool prefabDirty = false;

            TMP_Text[] tmpTexts = prefabRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in tmpTexts)
            {
                if (txt.font != targetTMPFont)
                {
                    txt.font = targetTMPFont;
                    EditorUtility.SetDirty(txt);
                    prefabDirty = true;
                }
            }

            UnityEngine.UI.Text[] legacyTexts = prefabRoot.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var txt in legacyTexts)
            {
                if (txt.font != targetLegacyFont)
                {
                    txt.font = targetLegacyFont;
                    EditorUtility.SetDirty(txt);
                    prefabDirty = true;
                }
            }

            if (prefabDirty)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"Updated fonts in prefab: {prefabPath}");
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Font update completed successfully!");
    }
}
