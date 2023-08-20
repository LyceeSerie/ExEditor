using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;

public class MaterialApplyer : EditorWindow
{
    [MenuItem("SerieIndustry/MaterialApplyer", false, 1)]
    private static void Open()
    {
        MaterialApplyer window = GetWindow<MaterialApplyer>();
    }

    DefaultAsset searchFolder = null;
    string path = null;
    string AlbedolLocallize = "BaseMap";
    string MaskLocallize = "MaskMap";
    string NormalLocallize = "Normal";
    string EmissionLocallize = "Emiss";
    string childFolderLocallize = "Standard";
    string subFolder = null;
    string commonFolderLocallize = "Common";
    string commonFolder = null;
    string message = null;

    private void OnGUI()
    {
        message = null;
        EditorGUILayout.LabelField("対象フォルダ");
        searchFolder = (DefaultAsset)EditorGUILayout.ObjectField("RootFolder", searchFolder, typeof(DefaultAsset), true);

        EditorGUILayout.LabelField("");
        EditorGUILayout.LabelField("各Map名のローカライズ");
        AlbedolLocallize = EditorGUILayout.TextField("BaseMapFileName", AlbedolLocallize);
        MaskLocallize = EditorGUILayout.TextField("MaskMapFileName", MaskLocallize);
        NormalLocallize = EditorGUILayout.TextField("NormalMapFileName", NormalLocallize);
        EmissionLocallize = EditorGUILayout.TextField("EmissionMapFileName", EmissionLocallize);

        EditorGUILayout.LabelField("");
        EditorGUILayout.LabelField("各フォルダ名のローカライズ");
        childFolderLocallize = EditorGUILayout.TextField("ChildFolderName", childFolderLocallize);
        commonFolderLocallize = EditorGUILayout.TextField("ChildFolderName", commonFolderLocallize);


        if (searchFolder != null)
        {
            path = AssetDatabase.GetAssetOrScenePath(searchFolder);
            string[] folderList = path.Split('/');

            if (folderList[folderList.Length - 1].Contains("."))
                searchFolder = null;

            try
            {
                subFolder = Directory.GetDirectories(path, childFolderLocallize, SearchOption.AllDirectories)[0];
                commonFolder = Directory.GetDirectories(path, commonFolderLocallize, SearchOption.AllDirectories)[0];
            }
            catch
            {
                subFolder = null;
                commonFolder = null;
            }
        }

        var checker =
            String.IsNullOrEmpty(path) ||
            String.IsNullOrEmpty(AlbedolLocallize) ||
            String.IsNullOrEmpty(MaskLocallize) ||
            String.IsNullOrEmpty(NormalLocallize) ||
            String.IsNullOrEmpty(childFolderLocallize) ||
            String.IsNullOrEmpty(subFolder) ||
            String.IsNullOrEmpty(commonFolder);

        if (String.IsNullOrEmpty(subFolder))
            message = "SubFolderが存在しません";

        EditorGUI.BeginDisabledGroup(checker);
        if (GUILayout.Button("Material Set"))
        {
            MeterialSet();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.LabelField(message);
    }

    private void MeterialSet()
    {
        Debug.Log("マテリアル適用処理開始");

        Texture2D maskMap = null;
        Texture2D nrmMap = null;
        Texture2D emissionMap = null;
        Shader shader = Shader.Find("Standard");

        var colorFolders = Directory.GetDirectories(subFolder, "*", SearchOption.AllDirectories);

        Debug.Log("RootFolder処理");

        // MaskMap取得
        var maskMapPath = Directory.EnumerateFiles($"{subFolder}/", $"*{MaskLocallize}*").FirstOrDefault();
        maskMap = AssetDatabase.LoadAssetAtPath<Texture2D>(maskMapPath);
        // MaskMap設定
        TextureImporter maskTextureImporter = AssetImporter.GetAtPath(maskMapPath) as TextureImporter;
        maskTextureImporter.textureType = TextureImporterType.Default;
        maskTextureImporter.sRGBTexture = false;
        maskTextureImporter.alphaIsTransparency = true;
        maskTextureImporter.streamingMipmaps = true;
        maskTextureImporter.SaveAndReimport();

        // NrmMap取得
        var nrmMapPath = Directory.EnumerateFiles($"{commonFolder}/", $"*{NormalLocallize}*").FirstOrDefault();
        nrmMap = AssetDatabase.LoadAssetAtPath<Texture2D>(nrmMapPath);
        // NrmMap設定
        TextureImporter nrmTextureImporter = AssetImporter.GetAtPath(nrmMapPath) as TextureImporter;
        nrmTextureImporter.textureType = TextureImporterType.NormalMap;
        nrmTextureImporter.streamingMipmaps = true;
        nrmTextureImporter.SaveAndReimport();

        // EmissionMap取得
        var emissionMapPath = Directory.EnumerateFiles($"{subFolder}/", $"*{EmissionLocallize}*").FirstOrDefault();
        emissionMap = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionMapPath);
        // EmissionMap設定
        if (emissionMap != null)
        {
            TextureImporter emissionTextureImporter = AssetImporter.GetAtPath(emissionMapPath) as TextureImporter;
            emissionTextureImporter.textureType = TextureImporterType.Default;
            emissionTextureImporter.streamingMipmaps = true;
            emissionTextureImporter.SaveAndReimport();
        }

        foreach (var folder in colorFolders)
        {
            if (maskMap != null && nrmMap != null)
            {
                Debug.Log("SubFolder処理");

                //マテリアル取得
                var materialPath = Directory.EnumerateFiles($"{folder}/", $"*.mat").FirstOrDefault();
                Material material = AssetDatabase.LoadAssetAtPath<Material>($"{materialPath}");
                var newMat = false;

                if (material == null)
                {
                    material = new Material(shader);
                    newMat = true;
                }
                material.shader = shader;

                // BaseMap取得/セット
                var basePath = Directory.EnumerateFiles($"{folder}/", $"*{AlbedolLocallize}*").FirstOrDefault();
                Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{basePath}");
                // BaseMap設定
                TextureImporter baseTextureImporter = AssetImporter.GetAtPath(basePath) as TextureImporter;
                baseTextureImporter.textureType = TextureImporterType.Default;
                baseTextureImporter.alphaIsTransparency = true;
                baseTextureImporter.streamingMipmaps = true;
                baseTextureImporter.SaveAndReimport();
                // BaseMapセット
                material.SetTexture("_MainTex", baseMap);

                // MaskMapセット
                material.SetTexture("_MetallicGlossMap", maskMap);

                // NrmMapセット                
                material.SetTexture("_BumpMap", nrmMap);
                material.EnableKeyword("_NORMALMAP");

                // EmissionMapセット
                if (emissionMap != null)
                {
                    material.SetTexture("_EmissionMap", emissionMap);
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", Color.black);
                }
                else
                {
                    // 不要なテクスチャが設定されていた場合削除する
                    material.SetTexture("_EmissionMap", null);
                    material.DisableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", Color.black);
                }

                int lastSlashIndex = folder.LastIndexOf('/');
                int lastBackslashIndex = folder.LastIndexOf('\\');
                int lastIndex = Math.Max(lastSlashIndex, lastBackslashIndex);
                string fileName = folder.Substring(lastIndex + 1);
                var folderName = AssetDatabase.GetAssetPath(searchFolder);
                folderName = folderName.Substring(folderName.LastIndexOf('/') + 1);

                // 保存処理
                if (newMat)
                {
                    AssetDatabase.CreateAsset(material, $"{folder}/{folderName}_{fileName}.mat");
                }
                else
                {
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssets();
                }
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.Log("テクスチャが取得できませんでした。");
            }
        }
    }
}