using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class CreateDungGeunMoFontAsset
{
    private const string SourcePath = "Assets/Art/Font/DungGeunMo.ttf";
    private const string AssetPath = "Assets/Art/Font/DungGeunMo SDF.asset";

    [InitializeOnLoadMethod]
    private static void GenerateOnEditorLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath) == null)
                Generate();
        };
    }

    [MenuItem("Tools/Reel of Dungeon/Create DungGeunMo TMP Font")]
    public static void Generate()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
        if (sourceFont == null)
            throw new InvalidOperationException($"Font was not found at {SourcePath}");

        AssetDatabase.DeleteAsset(AssetPath);

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA_HINTED,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true
        );

        if (fontAsset == null)
            throw new InvalidOperationException("TMP failed to create the DungGeunMo font asset.");

        fontAsset.name = "DungGeunMo SDF";
        fontAsset.atlasTexture.name = "DungGeunMo SDF Atlas";
        fontAsset.material.name = "DungGeunMo SDF Material";

        const string initialCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 " +
            "!?.:,;+-*/()[]{}%#&'\"_<>=";
        fontAsset.TryAddCharacters(initialCharacters, out _);

        AssetDatabase.CreateAsset(fontAsset, AssetPath);
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created {AssetPath} with dynamic glyph support.");
    }
}
