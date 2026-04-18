using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class URPAutoTextureAssigner : EditorWindow
{
    private DefaultAsset textureFolder;
    private bool convertToURP = true;
    private bool enableAlphaClipForPlants = true;
    private bool doubleSidedForPlants = true;
    private float plantCutoff = 0.5f;

    [MenuItem("Tools/URP/Auto Assign Textures To Selected Materials")]
    public static void ShowWindow()
    {
        GetWindow<URPAutoTextureAssigner>("URP Texture Assigner");
    }

    private void OnGUI()
    {
        GUILayout.Label("URP Auto Texture Assigner", EditorStyles.boldLabel);
        GUILayout.Space(8);

        textureFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Texture Folder",
            textureFolder,
            typeof(DefaultAsset),
            false
        );

        convertToURP = EditorGUILayout.Toggle("Convert Shader To URP/Lit", convertToURP);
        enableAlphaClipForPlants = EditorGUILayout.Toggle("Alpha Clip For Plants", enableAlphaClipForPlants);
        doubleSidedForPlants = EditorGUILayout.Toggle("Double Sided For Plants", doubleSidedForPlants);
        plantCutoff = EditorGUILayout.Slider("Plant Cutoff", plantCutoff, 0f, 1f);

        GUILayout.Space(10);

        if (GUILayout.Button("Process Selected Materials", GUILayout.Height(30)))
        {
            ProcessSelectedMaterials();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "1. ��ѡ�в�����\n" +
            "2. ָ�� Textures �ļ���\n" +
            "3. �� Process\n\n" +
            "���Զ�����ƥ�� BaseMap / NormalMap�����Ѳ���ת�� URP/Lit��",
            MessageType.Info
        );
    }

    private void ProcessSelectedMaterials()
    {
        if (textureFolder == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Texture Folder first.", "OK");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(textureFolder);
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        List<Texture2D> textures = textureGuids
            .Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(t => t != null)
            .ToList();

        if (textures.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No textures found in the selected folder.", "OK");
            return;
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (convertToURP && urpLit == null)
        {
            EditorUtility.DisplayDialog("Error", "Cannot find shader: Universal Render Pipeline/Lit", "OK");
            return;
        }

        int processed = 0;

        foreach (Object obj in Selection.objects)
        {
            if (!(obj is Material mat)) continue;

            Undo.RecordObject(mat, "Auto Assign URP Textures");

            if (convertToURP)
            {
                mat.shader = urpLit;
            }

            AssignTexturesToMaterial(mat, textures);
            SetupSpecialMaterialOptions(mat);

            EditorUtility.SetDirty(mat);
            processed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done", $"Processed {processed} material(s).", "OK");
    }

    private void AssignTexturesToMaterial(Material mat, List<Texture2D> textures)
    {
        string matName = NormalizeName(mat.name);
        string[] matTokens = GetTokens(matName);

        Texture2D bestBaseMap = null;
        Texture2D bestNormalMap = null;

        int bestBaseScore = -999;
        int bestNormalScore = -999;

        foreach (Texture2D tex in textures)
        {
            string texNameRaw = tex.name;
            string texName = NormalizeName(texNameRaw);
            string[] texTokens = GetTokens(texName);

            int tokenScore = GetTokenMatchScore(matTokens, texTokens);

            bool looksLikeNormal = IsNormalTextureName(texNameRaw);
            bool looksLikeMask = IsMaskLikeTextureName(texNameRaw);

            if (!looksLikeNormal && !looksLikeMask)
            {
                int score = tokenScore + GetBaseBonus(matName, texName);
                if (score > bestBaseScore)
                {
                    bestBaseScore = score;
                    bestBaseMap = tex;
                }
            }

            if (looksLikeNormal)
            {
                int score = tokenScore + 20;
                if (score > bestNormalScore)
                {
                    bestNormalScore = score;
                    bestNormalMap = tex;
                }
            }
        }

        if (bestBaseMap != null && bestBaseScore > 0)
        {
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", bestBaseMap);

            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", bestBaseMap);

            Debug.Log($"[BaseMap] {mat.name} <= {bestBaseMap.name}");
        }
        else
        {
            Debug.LogWarning($"[BaseMap Not Found] {mat.name}");
        }

        if (bestNormalMap != null && bestNormalScore > 0)
        {
            string normalPath = AssetDatabase.GetAssetPath(bestNormalMap);
            TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }

            mat.EnableKeyword("_NORMALMAP");

            if (mat.HasProperty("_BumpMap"))
                mat.SetTexture("_BumpMap", bestNormalMap);

            Debug.Log($"[NormalMap] {mat.name} <= {bestNormalMap.name}");
        }
    }

    private void SetupSpecialMaterialOptions(Material mat)
    {
        string lower = mat.name.ToLower();

        bool isPlant =
            lower.Contains("bush") ||
            lower.Contains("grass") ||
            lower.Contains("leaf") ||
            lower.Contains("foliage") ||
            lower.Contains("pine") ||
            lower.Contains("plant") ||
            lower.Contains("branch");

        if (!isPlant) return;

        if (enableAlphaClipForPlants)
        {
            SetAlphaClip(mat, true, plantCutoff);
        }

        if (doubleSidedForPlants)
        {
            SetDoubleSided(mat, true);
        }
    }

    private void SetAlphaClip(Material mat, bool enabled, float cutoff)
    {
        if (mat.HasProperty("_AlphaClip"))
            mat.SetFloat("_AlphaClip", enabled ? 1f : 0f);

        if (mat.HasProperty("_Cutoff"))
            mat.SetFloat("_Cutoff", cutoff);

        if (enabled)
            mat.EnableKeyword("_ALPHATEST_ON");
        else
            mat.DisableKeyword("_ALPHATEST_ON");
    }

    private void SetDoubleSided(Material mat, bool enabled)
    {
        // URP Lit һ�� Cull = 0 ����˫��
        if (mat.HasProperty("_Cull"))
            mat.SetFloat("_Cull", enabled ? 0f : 2f);

        // ĳЩ�汾���� Render Face
        if (mat.HasProperty("_RenderFace"))
            mat.SetFloat("_RenderFace", enabled ? 2f : 0f);
    }

    private string NormalizeName(string input)
    {
        string s = input.ToLower();

        s = s.Replace("material", "");
        s = s.Replace("mat", "");
        s = s.Replace("triplanar", "");
        s = s.Replace("tripplanar", "");
        s = s.Replace("instance", "");
        s = s.Replace("albedo", "");
        s = s.Replace("basecolor", "");
        s = s.Replace("diffuse", "");
        s = s.Replace("normalgl", "normal");
        s = s.Replace("normaldx", "normal");
        s = s.Replace("_", " ");
        s = s.Replace("-", " ");

        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    private string[] GetTokens(string input)
    {
        return input
            .Split(' ')
            .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length > 1)
            .ToArray();
    }

    private int GetTokenMatchScore(string[] a, string[] b)
    {
        int score = 0;
        foreach (string x in a)
        {
            foreach (string y in b)
            {
                if (x == y) score += 10;
                else if (y.Contains(x) || x.Contains(y)) score += 4;
            }
        }
        return score;
    }

    private bool IsNormalTextureName(string name)
    {
        string n = name.ToLower();
        return n.Contains("normal") ||
               n.EndsWith("_n") ||
               n.Contains("_n_") ||
               n.Contains("norm");
    }

    private bool IsMaskLikeTextureName(string name)
    {
        string n = name.ToLower();
        return n.Contains("mask") ||
               n.Contains("metal") ||
               n.Contains("rough") ||
               n.Contains("ao") ||
               n.Contains("height") ||
               n.Contains("opacity") ||
               n.Contains("spec");
    }

    private int GetBaseBonus(string matName, string texName)
    {
        int bonus = 0;

        if (texName.Contains("albedo") || texName.Contains("basecolor") || texName.Contains("diffuse"))
            bonus += 15;

        if (matName.Contains("snow") && texName.Contains("snow"))
            bonus += 8;

        if (matName.Contains("ice") && texName.Contains("ice"))
            bonus += 8;

        if (matName.Contains("rock") && texName.Contains("rock"))
            bonus += 8;

        if (matName.Contains("grass") && texName.Contains("grass"))
            bonus += 8;

        if (matName.Contains("bush") && texName.Contains("bush"))
            bonus += 8;

        if (matName.Contains("pine") && texName.Contains("pine"))
            bonus += 8;

        return bonus;
    }
}