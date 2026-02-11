using UnityEngine;
using UnityEditor;
using System.Linq;

public static class ReverseAnimClipTool
{
    [MenuItem("Tools/Animation/Create Reversed Clip")]
    public static void CreateReversedClip()
    {
        var src = Selection.activeObject as AnimationClip;
        if (src == null) // 선택한 것이 애니메이션 클립이 아니라면 
        {
            EditorUtility.DisplayDialog("Create Reversed Clip", "AnimationClip을 선택한 뒤 실행하세요.", "OK");
            return;
        }

        // 새 클립 생성
        var dst = new AnimationClip
        {
            frameRate = src.frameRate,
            name = src.name + "_Reversed"
        };

        float length = src.length;

        // 모든 float 커브 복사 + 시간 반전
        var bindings = AnimationUtility.GetCurveBindings(src);
        foreach (var b in bindings)
        {
            var curve = AnimationUtility.GetEditorCurve(src, b);
            if (curve == null || curve.keys.Length == 0) continue;

            var keys = curve.keys
                .Select(k =>
                {
                    var nk = new Keyframe(length - k.time, k.value, -k.outTangent, -k.inTangent);
                    nk.weightedMode = k.weightedMode;
                    nk.inWeight = k.outWeight;
                    nk.outWeight = k.inWeight;
                    return nk;
                })
                .OrderBy(k => k.time)
                .ToArray();

            var newCurve = new AnimationCurve(keys);
            AnimationUtility.SetEditorCurve(dst, b, newCurve);
        }

        // ObjectReference 커브(스프라이트 등)도 반전
        var objBindings = AnimationUtility.GetObjectReferenceCurveBindings(src);
        foreach (var b in objBindings)
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(src, b);
            if (keys == null || keys.Length == 0) continue;

            var newKeys = keys
                .Select(k => new ObjectReferenceKeyframe
                {
                    time = length - k.time,
                    value = k.value
                })
                .OrderBy(k => k.time)
                .ToArray();

            AnimationUtility.SetObjectReferenceCurve(dst, b, newKeys);
        }

        // 저장
        string srcPath = AssetDatabase.GetAssetPath(src);
        string folder = System.IO.Path.GetDirectoryName(srcPath);
        string dstPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{dst.name}.anim");
        AssetDatabase.CreateAsset(dst, dstPath);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Create Reversed Clip", $"생성 완료:\n{dstPath}", "OK");
        Selection.activeObject = dst;
    }
}