using UnityEditor;

public class OpenLegacyBuildSettings
{
    [MenuItem("Tools/Open Legacy Build Settings")]
    public static void Open()
    {
        EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
    }
}
