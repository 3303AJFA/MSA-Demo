using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using System.Collections.Generic;

public static class SceneDumper
{
    static readonly HashSet<string> SkipComponents = new HashSet<string>
    {
        "Transform", "CanvasRenderer", "MeshRenderer", "MeshCollider",
        "BoxCollider", "CapsuleCollider", "MeshFilter"
    };

    static readonly HashSet<string> SkipProps = new HashSet<string>
    {
        "m_Script", "m_ObjectHideFlags", "m_Material", "m_RaycastTarget",
        "m_RaycastPadding", "m_Maskable", "m_FillCenter", "m_FillMethod",
        "m_FillAmount", "m_FillClockwise", "m_FillOrigin", "m_UseSpriteMesh",
        "m_PixelsPerUnitMultiplier", "m_PreserveAspect",
        // TMP bulk skip
        "m_isRightToLeft", "m_fontAsset", "m_sharedMaterial", "m_fontMaterial",
        "m_enableVertexGradient", "m_colorMode", "m_fontColorGradientPreset",
        "m_spriteAsset", "m_tintAllSprites", "m_StyleSheet", "m_TextStyleHashCode",
        "m_overrideHtmlColors", "m_faceColor", "m_fontSizeBase",
        "m_enableAutoSizing", "m_fontSizeMin", "m_fontSizeMax",
        "m_characterSpacing", "m_horizontalScale", "m_wordSpacing",
        "m_lineSpacing", "m_lineSpacingMax", "m_paragraphSpacing",
        "m_charWidthMaxAdj", "m_TextWrappingMode", "m_wordWrappingRatios",
        "m_overflowMode", "m_linkedTextComponent", "m_parentLinkedComponent",
        "m_enableKerning", "m_enableExtraPadding", "m_checkPaddingRequired",
        "m_isRichText", "m_EmojiFallbackSupport", "m_parseCtrlCharacters",
        "m_isOrthographic", "m_isCullingEnabled", "m_horizontalMapping",
        "m_verticalMapping", "m_uvLineOffset", "m_geometrySortingOrder",
        "m_IsTextObjectScaleStatic", "m_VertexBufferAutoSizeReduction",
        "m_useMaxVisibleDescender", "m_pageToDisplay",
        "m_isUsingLegacyAnimationComponent", "m_isVolumetricText",
        "m_hasFontAssetChanged", "m_baseMaterial", "m_maskOffset",
        // Renderer details
        "m_CastShadows", "m_ReceiveShadows", "m_DynamicOccludee",
        "m_StaticShadowCaster", "m_MotionVectors", "m_LightProbeUsage",
        "m_ReflectionProbeUsage", "m_RayTracingMode", "m_RayTraceProcedural",
        "m_RenderingLayerMask", "m_RendererPriority", "m_LightmapIndex",
        "m_LightmapIndexDynamic", "m_LightmapTilingOffset",
        "m_LightmapTilingOffsetDynamic", "m_ScaleInLightmap",
        "m_ReceiveGI", "m_PreserveUVs", "m_IgnoreNormalsForChartDetection",
        "m_ImportantGI", "m_StitchLightmapSeams", "m_SelectedEditorRenderState",
        "m_MinimumChartSize", "m_AutoUVMaxDistance", "m_AutoUVMaxAngle",
        "m_LightmapParameters", "m_SortingLayerID", "m_SortingLayer",
        "m_SortingOrder", "m_AdditionalVertexStreams",
        // Camera bulk
        "m_ShutterSpeed", "m_Aperture", "m_FocusDistance", "m_FocalLength",
        "m_BladeCount", "m_Curvature", "m_BarrelClipping", "m_Anamorphism",
        "m_SensorSize", "m_LensShift", "m_GateFitMode", "m_FOVAxisMode",
        "iso", "m_ProjectionMatrixMode",
        // Light bulk
        "m_Cookie", "m_DrawHalo", "m_Flare", "m_BakingOutput",
        "m_AreaSize", "m_BounceIntensity", "m_ShadowAngle",
        "m_LuxAtDistance", "m_EnableSpotReflector",
        // URP Additional data bulk
        "m_Version", "m_RequiresDepthTexture", "m_RequiresColorTexture"
    };

    [MenuItem("Tools/MSA/Dump Scene Hierarchy")]
    public static void DumpActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        var sb = new StringBuilder();
        sb.AppendLine($"Scene: {scene.name} | {System.DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine(new string('-', 50));

        foreach (var root in scene.GetRootGameObjects())
            DumpObject(root, sb, 0);

        string dir = Path.Combine(Application.dataPath, "..", "docs");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"scene_dump_{scene.name}.txt");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"[MSA] Scene dump: {path} ({sb.Length / 1024}KB)");
    }

    static void DumpObject(GameObject go, StringBuilder sb, int depth)
    {
        string indent = new string(' ', depth * 2);
        string active = go.activeSelf ? "" : " [OFF]";
        sb.AppendLine($"{indent}{go.name}{active}");

        foreach (var comp in go.GetComponents<Component>())
        {
            if (comp == null) { sb.AppendLine($"{indent}  [Missing Script]"); continue; }
            string type = comp.GetType().Name;

            if (type == "RectTransform")
            {
                var rt = (RectTransform)comp;
                sb.AppendLine($"{indent}  [RT] pos=({rt.anchoredPosition.x:F0},{rt.anchoredPosition.y:F0}) size=({rt.sizeDelta.x:F0}x{rt.sizeDelta.y:F0}) anchor=({rt.anchorMin.x:F1},{rt.anchorMin.y:F1})-({rt.anchorMax.x:F1},{rt.anchorMax.y:F1}) scale=({rt.localScale.x:F1},{rt.localScale.y:F1},{rt.localScale.z:F1})");
                continue;
            }

            if (type == "Transform")
            {
                var t = (Transform)comp;
                if (t.localPosition != Vector3.zero || t.localEulerAngles != Vector3.zero || t.localScale != Vector3.one)
                    sb.AppendLine($"{indent}  [T] pos=({t.localPosition.x:F1},{t.localPosition.y:F1},{t.localPosition.z:F1}) rot=({t.localEulerAngles.x:F0},{t.localEulerAngles.y:F0},{t.localEulerAngles.z:F0}) scale=({t.localScale.x:F1},{t.localScale.y:F1},{t.localScale.z:F1})");
                continue;
            }

            if (SkipComponents.Contains(type)) continue;

            sb.Append($"{indent}  [{type}]");
            DumpFields(comp, sb, indent);
        }

        for (int i = 0; i < go.transform.childCount; i++)
            DumpObject(go.transform.GetChild(i).gameObject, sb, depth + 1);
    }

    static void DumpFields(Component comp, StringBuilder sb, string indent)
    {
        var so = new SerializedObject(comp);
        var prop = so.GetIterator();
        bool any = false;

        if (prop.NextVisible(true))
        {
            do
            {
                if (SkipProps.Contains(prop.name)) continue;
                string val = GetVal(prop);
                if (val == null) continue;
                if (!any) { sb.AppendLine(); any = true; }
                sb.AppendLine($"{indent}    {prop.displayName} = {val}");
            } while (prop.NextVisible(false));
        }

        if (!any) sb.AppendLine();
    }

    static string GetVal(SerializedProperty p)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer: return p.intValue.ToString();
            case SerializedPropertyType.Float: return p.floatValue.ToString("F2");
            case SerializedPropertyType.Boolean: return p.boolValue.ToString();
            case SerializedPropertyType.String:
                return string.IsNullOrEmpty(p.stringValue) ? null : $"\"{p.stringValue}\"";
            case SerializedPropertyType.Enum:
                var names = p.enumDisplayNames;
                int idx = p.enumValueIndex;
                return (idx >= 0 && idx < names.Length) ? names[idx] : $"({p.intValue})";
            case SerializedPropertyType.ObjectReference:
                if (p.objectReferenceValue == null) return "<none>";
                return $"{p.objectReferenceValue.name} ({p.objectReferenceValue.GetType().Name})";
            case SerializedPropertyType.Color:
                return $"#{ColorUtility.ToHtmlStringRGBA(p.colorValue)}";
            default: return null;
        }
    }
}
