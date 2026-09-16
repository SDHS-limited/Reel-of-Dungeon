#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerAttackAnimatorBuilder
{
    private const string SpritePath =
        "Assets/Resources/AttackAnimations/WizardAttack.png";
    private const string ClipPath =
        "Assets/Resources/AttackAnimations/WizardAttack.anim";
    private const string ControllerPath =
        "Assets/Resources/AttackAnimations/WizardAttack.controller";

    static PlayerAttackAnimatorBuilder()
    {
        EditorApplication.delayCall += EnsureAnimatorAssets;
    }

    private static void EnsureAnimatorAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null)
            return;

        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(SpritePath)
            .OfType<Sprite>()
            .OrderBy(sprite => GetFrameNumber(sprite.name))
            .ToArray();

        if (frames.Length == 0)
        {
            Debug.LogWarning("Player attack sprites were not imported yet.");
            return;
        }

        AnimationClip clip = new AnimationClip
        {
            name = "WizardAttack",
            frameRate = 18f
        };

        ObjectReferenceKeyframe[] keys =
            new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / clip.frameRate,
                value = frames[i]
            };
        }

        keys[keys.Length - 1] = new ObjectReferenceKeyframe
        {
            time = frames.Length / clip.frameRate,
            value = frames[frames.Length - 1]
        };

        EditorCurveBinding spriteBinding = EditorCurveBinding.PPtrCurve(
            string.Empty,
            typeof(SpriteRenderer),
            "m_Sprite"
        );
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keys);
        AssetDatabase.CreateAsset(clip, ClipPath);

        AnimatorController controller =
            AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorState state =
            controller.layers[0].stateMachine.AddState("Player Attack");
        state.motion = clip;
        state.writeDefaultValues = false;
        controller.layers[0].stateMachine.defaultState = state;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static int GetFrameNumber(string spriteName)
    {
        int separator = spriteName.LastIndexOf('_');
        return separator >= 0 &&
               int.TryParse(spriteName.Substring(separator + 1), out int number)
            ? number
            : 0;
    }
}
#endif
