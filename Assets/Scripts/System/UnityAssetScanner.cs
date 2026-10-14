using UnityEngine;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UnityAssetScanner : MonoBehaviour
{
    // This creates a right-click menu context option inside the Inspector component
    [ContextMenu("Scan Hierarchy and Assets")]
    public void ScanProjectAndHierarchy()
    {
        string filePath = Path.Combine(Application.dataPath, "Unity_Asset_Scan_Report.txt");
        List<string> reportLines = new List<string>();

        reportLines.Add("=== MONOBEHAVIOUR SCRIPTS IN SCENE ===");

        Dictionary<string, List<string>> scriptToObjectsMap = new Dictionary<string, List<string>>();
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in allObjects)
        {
#if UNITY_EDITOR
            if (obj.hideFlags == HideFlags.None && !EditorUtility.IsPersistent(obj))
            {
                MonoBehaviour[] components = obj.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour comp in components)
                {
                    if (comp == null) continue;

                    string scriptName = comp.GetType().Name;
                    string gameObjectName = obj.name;

                    if (!scriptToObjectsMap.ContainsKey(scriptName))
                    {
                        scriptToObjectsMap[scriptName] = new List<string>();
                    }

                    if (!scriptToObjectsMap[scriptName].Contains(gameObjectName))
                    {
                        scriptToObjectsMap[scriptName].Add(gameObjectName);
                    }
                }
            }
#endif
        }

        foreach (KeyValuePair<string, List<string>> kvp in scriptToObjectsMap)
        {
            string joinedObjects = string.Join(",", kvp.Value);
            reportLines.Add($"{kvp.Key}({joinedObjects})");
        }

        reportLines.Add("");
        reportLines.Add("=== SCRIPTABLEOBJECT TYPES IN PROJECT ===");

        HashSet<string> scriptableObjectTypes = new HashSet<string>();

#if UNITY_EDITOR
        string[] allAssetGUIDs = AssetDatabase.FindAssets("t:ScriptableObject");
        foreach (string guid in allAssetGUIDs)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject scriptableAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

            if (scriptableAsset != null)
            {
                scriptableObjectTypes.Add(scriptableAsset.GetType().Name);
            }
        }
#endif

        foreach (string soType in scriptableObjectTypes)
        {
            reportLines.Add(soType);
        }

        File.WriteAllLines(filePath, reportLines);

#if UNITY_EDITOR
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Scan Complete", "Report generated successfully!\nSaved to: Assets/Unity_Asset_Scan_Report.txt", "OK");
#endif
    }
}
