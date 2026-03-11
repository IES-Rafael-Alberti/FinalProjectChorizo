using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Text;
using System.Collections.Generic;

public class SceneExporter : EditorWindow
{
    [MenuItem("Tools/Copy Scenes and Prefabs to Clipboard")]
    public static void CopyDataToClipboard()
    {
        // Guardamos si hay cambios sin guardar
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Operación cancelada para no perder los cambios no guardados.");
            return;
        }

        string originalScenePath = SceneManager.GetActiveScene().path;
        StringBuilder sb = new StringBuilder();
        
        // 1. Validamos que las carpetas existan
        bool hasScenesFolder = AssetDatabase.IsValidFolder("Assets/Scenes");
        bool hasPrefabsFolder = AssetDatabase.IsValidFolder("Assets/Prefabs");

        if (!hasScenesFolder && !hasPrefabsFolder)
        {
            Debug.LogError("No se encontraron las carpetas 'Assets/Scenes' ni 'Assets/Prefabs'. ¡Asegúrate de que se llaman exactamente así!");
            return;
        }

        // 2. Buscamos Escenas y Prefabs en esas carpetas específicas
        string[] sceneGuids = hasScenesFolder ? AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }) : new string[0];
        string[] prefabGuids = hasPrefabsFolder ? AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }) : new string[0];

        int totalAssets = sceneGuids.Length + prefabGuids.Length;
        int currentIndex = 0;

        // ==========================================
        // PROCESAR ESCENAS
        // ==========================================
        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EditorUtility.DisplayProgressBar("Copiando Datos", $"Procesando Escena: {path}", (float)currentIndex / totalAssets);

            try
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                
                sb.AppendLine("\n=======================================================");
                sb.AppendLine($"--- ESCENA: {scene.name} ---");
                sb.AppendLine("=======================================================\n");

                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject go in rootObjects)
                {
                    DumpGameObject(go, sb, "");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error procesando la escena {path}: {e.Message}");
            }
            currentIndex++;
        }

        // ==========================================
        // PROCESAR PREFABS
        // ==========================================
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EditorUtility.DisplayProgressBar("Copiando Datos", $"Procesando Prefab: {path}", (float)currentIndex / totalAssets);

            try
            {
                GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefabRoot != null)
                {
                    sb.AppendLine("\n=======================================================");
                    sb.AppendLine($"--- PREFAB ORIGINAL: {prefabRoot.name} ---");
                    sb.AppendLine("=======================================================\n");
                    
                    DumpGameObject(prefabRoot, sb, "");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error procesando el prefab {path}: {e.Message}");
            }
            currentIndex++;
        }

        // 3. Limpiamos y restauramos la escena original
        EditorUtility.ClearProgressBar();
        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        // 4. Copiamos al portapapeles
        GUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log($"¡Éxito! {sceneGuids.Length} escenas y {prefabGuids.Length} prefabs copiados al portapapeles. (Total de caracteres: {sb.Length})");
    }

    private static void DumpGameObject(GameObject go, StringBuilder sb, string indent)
    {
        string layerName = LayerMask.LayerToName(go.layer);
        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(go);
        
        sb.AppendLine($"{indent}- {go.name} (Tag: {go.tag}, Layer: {layerName}, Activo: {go.activeSelf}, Static: {go.isStatic}, EsPrefab: {isPrefab})");
        
        Component[] components = go.GetComponents<Component>();
        foreach (Component comp in components)
        {
            if (comp == null) continue;

            if (comp is Transform t)
            {
                sb.AppendLine($"{indent}   [Transform] Pos: {t.localPosition} | Rot: {t.localEulerAngles} | Escala: {t.localScale}");
            }
            else
            {
                sb.AppendLine($"{indent}   [{comp.GetType().Name}]");
                DumpProperties(comp, sb, indent + "      ");
            }
        }

        foreach (Transform child in go.transform)
        {
            DumpGameObject(child.gameObject, sb, indent + "  ");
        }
    }

    private static void DumpProperties(Component comp, StringBuilder sb, string indent)
    {
        SerializedObject so = new SerializedObject(comp);
        SerializedProperty prop = so.GetIterator();
        bool enterChildren = true;
        
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false; 
            if (prop.name == "m_ObjectHideFlags" || prop.name == "m_Script") continue;

            string valueStr = GetPropertyValueAsString(prop);
            sb.AppendLine($"{indent}{prop.name}: {valueStr}");
        }
    }

    private static string GetPropertyValueAsString(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer: return prop.intValue.ToString();
            case SerializedPropertyType.Boolean: return prop.boolValue.ToString();
            case SerializedPropertyType.Float: return prop.floatValue.ToString();
            case SerializedPropertyType.String: return $"\"{prop.stringValue}\"";
            case SerializedPropertyType.Color: return prop.colorValue.ToString();
            case SerializedPropertyType.Vector2: return prop.vector2Value.ToString();
            case SerializedPropertyType.Vector3: return prop.vector3Value.ToString();
            case SerializedPropertyType.Enum: 
                return prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumDisplayNames.Length ? prop.enumDisplayNames[prop.enumValueIndex] : prop.enumValueIndex.ToString();
            case SerializedPropertyType.ObjectReference:
                return prop.objectReferenceValue != null ? $"{prop.objectReferenceValue.name} ({prop.objectReferenceValue.GetType().Name})" : "Null";
            default:
                return $"[Tipo: {prop.propertyType}]";
        }
    }
}