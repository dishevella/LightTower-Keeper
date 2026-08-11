using System;
using Animancer;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public sealed class InlineClipTuningDrawer : OdinAttributeDrawer<InlineClipTuningAttribute, AnimationClip>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        CharacterAnimationConfig config = InlineClipTuningGUI.GetConfig(Property);
        AnimationClip clip = ValueEntry.SmartValue;
        if (config == null || clip == null)
        {
            CallNextDrawer(label);
            return;
        }

        string title = label != null && !string.IsNullOrWhiteSpace(label.text)
            ? label.text
            : clip.name;
        AnimationClip selectedClip = InlineClipTuningGUI.Draw(config, clip, title);
        if (selectedClip != clip)
            ValueEntry.SmartValue = selectedClip;
    }
}

public sealed class InlineClipTuningArrayDrawer : OdinAttributeDrawer<InlineClipTuningAttribute, AnimationClip[]>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        // Odin already forwards field attributes to collection elements. Each item is
        // therefore rendered by InlineClipTuningDrawer; drawing it again here duplicates
        // controls and corrupts the surrounding IMGUI layout.
        CallNextDrawer(label);
    }
}

internal static class InlineClipTuningGUI
{
    public static CharacterAnimationConfig GetConfig(InspectorProperty property)
    {
        if (property == null || property.Tree == null || property.Tree.WeakTargets == null)
            return null;

        for (int i = 0; i < property.Tree.WeakTargets.Count; i++)
        {
            if (property.Tree.WeakTargets[i] is CharacterAnimationConfig config)
                return config;
        }

        return null;
    }

    public static AnimationClip Draw(
        CharacterAnimationConfig config,
        AnimationClip clip,
        string title)
    {
        CharacterAnimationConfig.MobilityClipTuning tuning = config.FindClipTuning(clip);
        if (tuning == null)
        {
            DrawMissingTuning(config, clip);
            return clip;
        }

        if (tuning.Transition == null)
            tuning.Transition = new ClipTransition { Clip = clip };

        SerializedProperty transitionProperty = FindTransitionProperty(config, tuning);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        AnimationClip selectedClip = clip;
        try
        {
            if (transitionProperty != null)
            {
                SerializedObject serializedConfig = transitionProperty.serializedObject;
                serializedConfig.UpdateIfRequiredOrScript();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(
                    transitionProperty,
                    new GUIContent(title, "Expand for timing controls. Use the eye button to preview this animation."),
                    true);
                if (EditorGUI.EndChangeCheck())
                {
                    bool dataChanged = serializedConfig.ApplyModifiedProperties();
                    if (dataChanged)
                    {
                        selectedClip = tuning.Transition != null ? tuning.Transition.Clip : null;

                        // The task field remains the clip assignment source. Changing the clip in
                        // Animancer's header switches that task without repurposing the old clip's tuning.
                        if (selectedClip != clip && tuning.Transition != null)
                            tuning.Transition.Clip = clip;

                        tuning.SyncEndTimeFromTransition();
                        EditorUtility.SetDirty(config);
                        GUI.changed = true;
                    }
                }

                if (transitionProperty.isExpanded)
                    DrawExpanded(config, tuning);
            }
            else
            {
                EditorGUILayout.HelpBox("Animancer transition data could not be located.", MessageType.Warning);
            }
        }
        finally
        {
            EditorGUILayout.EndVertical();
        }

        return selectedClip;
    }

    private static void DrawExpanded(
        CharacterAnimationConfig config,
        CharacterAnimationConfig.MobilityClipTuning tuning)
    {
        EditorGUILayout.LabelField(
            $"{tuning.Source}  |  {tuning.Category}  |  {tuning.Role}",
            EditorStyles.miniLabel);

        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 155f;
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("Exit", EditorStyles.boldLabel);
        float fadeOut = Mathf.Max(0f, EditorGUILayout.FloatField("Fade Out", tuning.FadeOut));
        float blendOutTime = EditorGUILayout.Slider(
            "Fade Out Start",
            tuning.BlendOutNormalizedTime,
            0f,
            2f);

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
        bool overrideRuntime = EditorGUILayout.Toggle("Override Runtime", tuning.OverrideRuntime);
        bool applyFootIK = EditorGUILayout.Toggle("Apply Foot IK", tuning.ApplyFootIK);

        EditorGUILayout.Space(2f);
        EditorGUILayout.LabelField("Camera Position", EditorStyles.boldLabel);
        bool overrideCamera = EditorGUILayout.Toggle(
            "Use For This Animation",
            tuning.OverrideCameraPosition);
        Vector3 cameraPosition = tuning.CameraLocalPosition;
        float cameraBlendSpeed = tuning.CameraBlendSpeed;
        using (new EditorGUI.DisabledScope(!overrideCamera))
        {
            cameraPosition = EditorGUILayout.Vector3Field("Camera Local Position", cameraPosition);
            cameraBlendSpeed = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField("Position Blend Speed", cameraBlendSpeed));
        }

        bool changed = EditorGUI.EndChangeCheck();
        EditorGUIUtility.labelWidth = previousLabelWidth;
        if (!changed) return;

        Undo.RecordObject(config, "Edit inline animation parameters");
        tuning.FadeOut = fadeOut;
        tuning.BlendOutNormalizedTime = blendOutTime;
        tuning.OverrideRuntime = overrideRuntime;
        tuning.ApplyFootIK = applyFootIK;
        tuning.OverrideCameraPosition = overrideCamera;
        tuning.CameraLocalPosition = cameraPosition;
        tuning.CameraBlendSpeed = cameraBlendSpeed;

        EditorUtility.SetDirty(config);
        GUI.changed = true;
    }

    private static void DrawMissingTuning(CharacterAnimationConfig config, AnimationClip clip)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("No parameters are registered for this animation.", EditorStyles.miniLabel);
        if (GUILayout.Button("Create Parameters", EditorStyles.miniButton, GUILayout.Width(120f)))
            CreateTuning(config, clip);
        EditorGUILayout.EndHorizontal();
    }

    private static void CreateTuning(CharacterAnimationConfig config, AnimationClip clip)
    {
        Undo.RecordObject(config, "Create animation parameters");
        CharacterAnimationConfig.MobilityClipTuning tuning =
            new CharacterAnimationConfig.MobilityClipTuning
            {
                Source = CharacterAnimationSource.ProjectOriginal,
                Category = MobilityAnimationCategory.Other,
                Role = "Custom / " + clip.name,
                Transition = new ClipTransition
                {
                    Clip = clip,
                    FadeDuration = 0.12f
                },
                FadeOut = 0.12f,
                BlendOutNormalizedTime = 0.85f,
                EndNormalizedTime = 1f,
                OverrideRuntime = true,
                ApplyFootIK = true
            };
        tuning.ApplyEndTimeToTransition();

        int count = config.ClipTunings != null ? config.ClipTunings.Length : 0;
        Array.Resize(ref config.ClipTunings, count + 1);
        config.ClipTunings[count] = tuning;
        EditorUtility.SetDirty(config);
        GUI.changed = true;
    }

    private static SerializedProperty FindTransitionProperty(
        CharacterAnimationConfig config,
        CharacterAnimationConfig.MobilityClipTuning tuning)
    {
        if (config.ClipTunings == null) return null;

        int index = Array.IndexOf(config.ClipTunings, tuning);
        if (index < 0) return null;

        SerializedObject serializedConfig = new SerializedObject(config);
        serializedConfig.UpdateIfRequiredOrScript();
        SerializedProperty tunings = serializedConfig.FindProperty("ClipTunings");
        if (tunings == null || index >= tunings.arraySize) return null;
        return tunings.GetArrayElementAtIndex(index).FindPropertyRelative("Transition");
    }

}
