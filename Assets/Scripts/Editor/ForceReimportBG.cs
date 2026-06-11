using UnityEditor;
using UnityEngine;

public static class ForceReimportBG
{
    [MenuItem("Solengard/Forcar Reimport Backgrounds")]
    static void Reimport()
    {
        AssetDatabase.ImportAsset("Assets/Art/UI/Backgrounds/menu_background.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/Art/UI/Backgrounds/menu_background_Steam.png", ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        Debug.Log("[Reimport] Backgrounds reimportados.");
        EditorUtility.DisplayDialog("Solengard", "Backgrounds reimportados. Agora rode Layout MainMenu.", "OK");
    }
}
