#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class ForcePlayFromBoot
{
    static ForcePlayFromBoot()
    {
        var boot = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/00_Boot.unity");
        if (boot != null)
            EditorSceneManager.playModeStartScene = boot;
    }
}
#endif
