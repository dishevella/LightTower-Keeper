using System;
using Animancer.Editor.Previews;
using UnityEditor;
using UnityEngine;

public sealed class CharacterAnimationPreviewWindow : EditorWindow
{
    private static readonly string[] SourceOptions = BuildOptions(typeof(CharacterAnimationSource));
    private static readonly string[] CategoryOptions = BuildOptions(typeof(MobilityAnimationCategory));

    [SerializeField] private CharacterAnimationConfig config;
    [SerializeField] private string search = string.Empty;
    [SerializeField] private int sourceFilter;
    [SerializeField] private int categoryFilter;
    [SerializeField] private Vector2 scroll;

    private SerializedObject serializedConfig;

    [MenuItem("Tools/Light Tower/Animation/Open Animation Preview")]
    public static void Open()
    {
        CharacterAnimationPreviewWindow window = GetWindow<CharacterAnimationPreviewWindow>();
        window.titleContent = new GUIContent("Animation Preview", TransitionPreviewWindow.Icon);
        window.minSize = new Vector2(560f, 360f);
        window.SetConfig(Selection.activeObject as CharacterAnimationConfig ??
            AssetDatabase.LoadAssetAtPath<CharacterAnimationConfig>(
                MobilityProLocomotionConfigurator.ConfigAssetPath));
        window.Show();
    }

    private void OnEnable()
    {
        if (config == null)
        {
            config = AssetDatabase.LoadAssetAtPath<CharacterAnimationConfig>(
                MobilityProLocomotionConfigurator.ConfigAssetPath);
        }

        RebuildSerializedObject();
    }

    private void OnGUI()
    {
        DrawToolbar();
        if (config == null || serializedConfig == null)
        {
            EditorGUILayout.HelpBox("Assign a Character Animation Config to browse its animations.", MessageType.Info);
            return;
        }

        serializedConfig.UpdateIfRequiredOrScript();
        SerializedProperty tunings = serializedConfig.FindProperty("ClipTunings");
        scroll = EditorGUILayout.BeginScrollView(scroll);
        int visibleCount = 0;
        for (int i = 0; i < tunings.arraySize; i++)
        {
            CharacterAnimationConfig.MobilityClipTuning tuning = config.ClipTunings[i];
            if (!MatchesFilters(tuning)) continue;
            DrawTuningRow(tunings.GetArrayElementAtIndex(i), tuning);
            visibleCount++;
        }

        if (visibleCount == 0)
            EditorGUILayout.HelpBox("No animations match the current filters.", MessageType.None);
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUI.BeginChangeCheck();
        CharacterAnimationConfig nextConfig = (CharacterAnimationConfig)EditorGUILayout.ObjectField(
            "Animation Config",
            config,
            typeof(CharacterAnimationConfig),
            false);
        if (EditorGUI.EndChangeCheck()) SetConfig(nextConfig);

        EditorGUILayout.BeginHorizontal();
        search = EditorGUILayout.TextField(GUIContent.none, search, GUI.skin.FindStyle("ToolbarSearchTextField"));
        sourceFilter = EditorGUILayout.Popup(sourceFilter, SourceOptions, GUILayout.Width(145f));
        categoryFilter = EditorGUILayout.Popup(categoryFilter, CategoryOptions, GUILayout.Width(165f));
        if (GUILayout.Button("Select Config", EditorStyles.miniButton, GUILayout.Width(90f)))
        {
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawTuningRow(
        SerializedProperty tuningProperty,
        CharacterAnimationConfig.MobilityClipTuning tuning)
    {
        SerializedProperty transition = tuningProperty.FindPropertyRelative("Transition");
        bool isPreviewing = TransitionPreviewWindow.IsPreviewing(transition);

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.MinHeight(58f));
        GUIContent eye = new GUIContent(
            TransitionPreviewWindow.Icon,
            isPreviewing ? "Close this animation preview" : "Preview this animation");
        Color previousColor = GUI.backgroundColor;
        if (isPreviewing) GUI.backgroundColor = new Color(0.45f, 0.8f, 1f);
        if (GUILayout.Button(eye, GUILayout.Width(42f), GUILayout.Height(42f)))
            TransitionPreviewWindow.OpenOrClose(transition);
        GUI.backgroundColor = previousColor;

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(tuning.DisplayName, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            $"{tuning.Source}  |  {tuning.Category}  |  {tuning.Role}",
            EditorStyles.miniLabel);
        string cameraText = tuning.OverrideCameraPosition
            ? $"Camera {tuning.CameraLocalPosition}  Speed {tuning.CameraBlendSpeed:0.##}"
            : "Camera uses the scene/default pose";
        EditorGUILayout.LabelField(
            $"Fade In {tuning.Transition.FadeDuration:0.###}  Fade Out {tuning.FadeOut:0.###}  " +
            $"End {tuning.EndNormalizedTime:0.###}  |  {cameraText}",
            EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(48f)))
        {
            Selection.activeObject = tuning.Clip;
            EditorGUIUtility.PingObject(tuning.Clip);
        }
        EditorGUILayout.EndHorizontal();
    }

    private bool MatchesFilters(CharacterAnimationConfig.MobilityClipTuning tuning)
    {
        if (tuning == null || tuning.Clip == null) return false;
        if (sourceFilter > 0 && (int)tuning.Source != sourceFilter - 1) return false;
        if (categoryFilter > 0 && (int)tuning.Category != categoryFilter - 1) return false;
        if (string.IsNullOrWhiteSpace(search)) return true;

        string query = search.Trim();
        return tuning.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
               (tuning.Role != null && tuning.Role.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void SetConfig(CharacterAnimationConfig value)
    {
        config = value;
        RebuildSerializedObject();
        Repaint();
    }

    private void RebuildSerializedObject()
    {
        serializedConfig = config != null ? new SerializedObject(config) : null;
    }

    private static string[] BuildOptions(Type enumType)
    {
        string[] names = Enum.GetNames(enumType);
        string[] options = new string[names.Length + 1];
        options[0] = "All";
        Array.Copy(names, 0, options, 1, names.Length);
        return options;
    }
}
