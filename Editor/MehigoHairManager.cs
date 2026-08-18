using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

public class MehigoHairProjectDataV4 : ScriptableObject
{
    public enum IconMode
    {
        Default,
        CustomTexture,
        SceneCapture
    }

    [Serializable]
    public class MaterialSlotData
    {
        public string rendererPath;
        public int materialIndex;
        public Material material;
    }

    [Serializable]
    public class MaterialPresetData
    {
        public string menuName = "Preset";
        public Texture2D icon;
        public List<MaterialSlotData> slots = new List<MaterialSlotData>();
    }

    public enum BlendShapeControlMode
    {
        Toggle,
        RadialPuppet
    }

    public enum ActivationMode
    {
        ControlHairRoot,
        ControlExistingWrapper,
        DoNotControlObject
    }

    [Serializable]
    public class BlendShapeData
    {
        public string menuName;
        public IconMode iconMode = IconMode.Default;
        public Texture2D icon;
        public string rendererPath;
        public string blendShapeName;
        public BlendShapeControlMode controlMode = BlendShapeControlMode.Toggle;
        public float onValue = 100f;
        public bool saved = true;
    }

    [Serializable]
    public class HairData
    {
        public string menuName;
        public IconMode iconMode = IconMode.Default;
        public Texture2D icon;
        public string hairPath;
        public bool preserveExistingAnimator = true;
        public ActivationMode activationMode = ActivationMode.ControlHairRoot;
        public string activationTargetPath;
        public List<string> linkedObjectPaths = new List<string>();
        public List<BlendShapeData> blendShapes = new List<BlendShapeData>();
        public List<MaterialPresetData> materialPresets = new List<MaterialPresetData>();
    }

    public string avatarAssetGuid;
    public string rootMenuName;
    public string hairParameterName;
    public string generatedRootName;
    public string saveFolder;
    public bool savedHairParameter;
    public List<HairData> hairs = new List<HairData>();
}

public class MehigoHairGeneratorV4 : EditorWindow
{
    public const string ToolName = "mehigo Hair Manager";
    public const string ToolVersion = "1.0.1";
    [Serializable]
    private class MaterialSlotEntry
    {
        public Renderer renderer;
        public int materialIndex;
        public Material material;
    }

    [Serializable]
    private class MaterialPreset
    {
        public string menuName = "Preset";
        public Texture2D icon;
        public List<MaterialSlotEntry> slots = new List<MaterialSlotEntry>();
        [NonSerialized] public bool foldout = true;
    }

    private enum IconMode
    {
        Default,
        CustomTexture,
        SceneCapture
    }

    private enum OptimizationMode
    {
        Safe,
        Optimized,
        LetAAOHandleIt
    }

    private enum EditorLanguage
    {
        Thai,
        English
    }

    private EditorLanguage language = EditorLanguage.Thai;
    private OptimizationMode optimizationMode = OptimizationMode.Safe;

    private string T(string th, string en)
    {
        return language == EditorLanguage.Thai ? th : en;
    }
    private enum BlendShapeControlMode
    {
        Toggle,
        RadialPuppet
    }

    private enum ActivationMode
    {
        ControlHairRoot,
        ControlExistingWrapper,
        DoNotControlObject
    }

    private class ConflictItem
    {
        public string hairName;
        public string sourceName;
        public string path;
        public string property;
        public string reason;
    }

    [Serializable]
    private class BlendShapeOption
    {
        public string menuName = "BlendShape";
        public IconMode iconMode = IconMode.Default;
        public Texture2D icon;
        public SkinnedMeshRenderer renderer;
        public string blendShapeName = "";
        public BlendShapeControlMode controlMode = BlendShapeControlMode.Toggle;
        public float onValue = 100f;
        public bool saved = true;
    }

    [Serializable]
    private class HairEntry
    {
        public string menuName = "Hair";
        public IconMode iconMode = IconMode.Default;
        public Texture2D icon;
        public GameObject hairObject;

        public bool preserveExistingAnimator = true;
        public ActivationMode activationMode = ActivationMode.ControlHairRoot;
        public GameObject activationTarget;

        public List<GameObject> linkedObjects = new List<GameObject>();
        public List<BlendShapeOption> blendShapes = new List<BlendShapeOption>();
        public List<MaterialPreset> materialPresets = new List<MaterialPreset>();

        [NonSerialized] public bool foldout = true;
        [NonSerialized] public bool compatibilityFoldout = true;
        [NonSerialized] public bool autoDetectActivation = true;
        [NonSerialized] public bool linkedFoldout;
        [NonSerialized] public bool blendFoldout;
        [NonSerialized] public bool materialFoldout;
    }

    private VRCAvatarDescriptor avatar;
    private GameObject existingPrefabOrAvatar;

    private string rootMenuName = "Hair Style";
    private string hairParameterName = "mehigo_HairIndex";
    private string generatedRootName = "mehigo Hair Selector";
    private string saveFolder = "Assets/mehigo/HairManager";
    private bool savedHairParameter = true;

    private readonly List<HairEntry> hairs = new List<HairEntry>();
    private readonly List<ConflictItem> conflicts = new List<ConflictItem>();

    private Vector2 scroll;
    private int selectedTab;
    private bool scanComplete;
    private bool showAdvancedProject;
    private bool compactHairCards;

    private int perfTriangles;
    private int perfRenderers;
    private int perfSkinnedMeshes;
    private int perfMaterialSlots;
    private int perfBlendShapes;
    private int perfAnimatorComponents;
    private int perfGeneratedParameters;
    private int perfGeneratedLayers;
    private bool perfAnalyzed;

    private bool aaoDetected;
    private bool aaoTraceAndOptimizeDetected;
    private bool aaoMergeMaterialDetected;
    private bool aaoMergeSkinnedMeshDetected;
    private int aaoAnimatedMaterialPresetCount;
    private int aaoHairToggleCount;

    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle cardStyle;
    private GUIStyle titleStyle;
    private GUIStyle badgeStyle;
    private GUIStyle subtleBoxStyle;

    [MenuItem("Tools/mehigo/Hair Manager")]
    public static void Open()
    {
        GetWindow<MehigoHairGeneratorV4>("mehigo Hair Manager");
    }

    private void OnEnable()
    {
        minSize = new Vector2(640, 660);
    }

    private void InitStyles()
    {
        if (headerStyle != null) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            margin = new RectOffset(0, 0, 5, 8)
        };

        titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13
        };

        sectionStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(12, 12, 10, 10),
            margin = new RectOffset(0, 0, 4, 8)
        };

        cardStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(12, 12, 10, 10),
            margin = new RectOffset(0, 0, 5, 8)
        };

        badgeStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(6, 6, 2, 2)
        };

        subtleBoxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(8, 8, 6, 6),
            margin = new RectOffset(0, 0, 3, 6)
        };
    }

    private void OnGUI()
    {
        InitStyles();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{ToolName}  v{ToolVersion}", headerStyle);
        GUILayout.FlexibleSpace();

        if (GUILayout.Toggle(language == EditorLanguage.Thai, "ไทย", EditorStyles.miniButtonLeft, GUILayout.Width(48)))
            language = EditorLanguage.Thai;

        if (GUILayout.Toggle(language == EditorLanguage.English, "ENG", EditorStyles.miniButtonRight, GUILayout.Width(48)))
            language = EditorLanguage.English;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            T(
                "แก้ไขของเดิม • โหมด Compatibility • ตรวจ Conflict • Modular Avatar",
                "Editable Setup • Compatibility Mode • Conflict Scanner • Modular Avatar"
            ),
            EditorStyles.miniLabel
        );

        EditorGUILayout.Space(6);

        selectedTab = GUILayout.Toolbar(
            selectedTab,
            new[]
            {
                T("ข้อมูล Avatar", "Avatar Info"),
                T("ทรงผม", "Hair Styles"),
                T("ตรวจความเข้ากันได้", "Compatibility"),
                T("Performance", "Performance"),
                T("สร้าง / อัปเดต", "Generate")
            },
            GUILayout.Height(28)
        );

        EditorGUILayout.Space(8);

        DrawTopSummary();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        switch (selectedTab)
        {
            case 0: DrawProjectTab(); break;
            case 1: DrawHairTab(); break;
            case 2: DrawCompatibilityTab(); break;
            case 3: DrawPerformanceTab(); break;
            case 4: DrawGenerateTab(); break;
        }

        EditorGUILayout.EndScrollView();
    }


    private void DrawTopSummary()
    {
        EditorGUILayout.BeginHorizontal(subtleBoxStyle);

        EditorGUILayout.LabelField(
            avatar != null
                ? $"Avatar: {avatar.name}"
                : T("ยังไม่ได้เลือก Avatar", "No Avatar Selected"),
            EditorStyles.boldLabel
        );

        GUILayout.FlexibleSpace();

        GUILayout.Label(T($"ทรงผม {hairs.Count}", $"Hair {hairs.Count}"), badgeStyle);
        GUILayout.Label(T($"BlendShape {CountBlendShapes()}", $"BlendShape {CountBlendShapes()}"), badgeStyle);

        GUILayout.Label(
            scanComplete
                ? (conflicts.Count == 0
                    ? "Conflict 0"
                    : $"Conflict {conflicts.Count}")
                : T("ยังไม่สแกน", "Not Scanned"),
            badgeStyle
        );

        EditorGUILayout.EndHorizontal();
    }

    private void DrawSectionHeader(string title, string subtitle = null)
    {
        EditorGUILayout.LabelField(title, titleStyle);

        if (!string.IsNullOrWhiteSpace(subtitle))
            EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);

        EditorGUILayout.Space(4);
    }

    private string GetActivationSummary(HairEntry hair)
    {
        switch (hair.activationMode)
        {
            case ActivationMode.ControlHairRoot:
                return T("Root", "Root");

            case ActivationMode.ControlExistingWrapper:
                return hair.activationTarget != null
                    ? $"Wrapper: {hair.activationTarget.name}"
                    : "Wrapper";

            case ActivationMode.DoNotControlObject:
                return T("ไม่ควบคุม", "No Control");
        }

        return "-";
    }

    // ---------------------------------------------------------------------
    // PROJECT TAB
    // ---------------------------------------------------------------------

    private void DrawProjectTab()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        DrawSectionHeader(
            T("ข้อมูล Avatar", "Avatar Info"),
            T("เลือก Avatar หรือโหลด setup เดิม", "Select an avatar or load an existing setup")
        );

        GameObject previousPrefabOrAvatar = existingPrefabOrAvatar;

        existingPrefabOrAvatar = (GameObject)EditorGUILayout.ObjectField(
            T("Prefab / Avatar", "Prefab / Avatar"),
            existingPrefabOrAvatar,
            typeof(GameObject),
            true
        );

        if (previousPrefabOrAvatar != existingPrefabOrAvatar &&
            existingPrefabOrAvatar != null)
        {
            AutoDetectAvatarDescriptor(existingPrefabOrAvatar);
        }

        GUI.enabled = existingPrefabOrAvatar != null;

        if (GUILayout.Button(
            T("โหลด Existing Setup", "Load Existing Setup"),
            GUILayout.Height(30)))
        {
            LoadExistingSetup(existingPrefabOrAvatar);
        }

        GUI.enabled = true;

        if (existingPrefabOrAvatar != null)
        {
            if (avatar != null)
            {
                EditorGUILayout.HelpBox(
                    T(
                        $"ตรวจพบ Avatar Descriptor: {avatar.name}",
                        $"Detected Avatar Descriptor: {avatar.name}"
                    ),
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    T(
                        "ไม่พบ VRCAvatarDescriptor ใน Object ที่เลือก",
                        "No VRCAvatarDescriptor was found in the selected object."
                    ),
                    MessageType.Warning
                );
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(sectionStyle);
        DrawSectionHeader(
            T("ตั้งค่าหลัก", "Main Settings"),
            T("แสดงเฉพาะค่าที่ใช้บ่อย", "Frequently used settings")
        );

        avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
            "Avatar Descriptor",
            avatar,
            typeof(VRCAvatarDescriptor),
            true
        );

        rootMenuName = EditorGUILayout.TextField(
            T("ชื่อเมนูหลัก", "Root Menu Name"),
            rootMenuName
        );

        savedHairParameter = EditorGUILayout.Toggle(
            T("จำทรงผมที่เลือก", "Save Selected Hair"),
            savedHairParameter
        );

        EditorGUILayout.Space(4);

        showAdvancedProject = EditorGUILayout.Foldout(
            showAdvancedProject,
            T("ตั้งค่าขั้นสูง", "Advanced Settings"),
            true
        );

        if (showAdvancedProject)
        {
            EditorGUILayout.BeginVertical(subtleBoxStyle);

            hairParameterName = EditorGUILayout.TextField(
                T("พารามิเตอร์ทรงผม", "Hair Parameter"),
                hairParameterName
            );

            generatedRootName = EditorGUILayout.TextField(
                T("ชื่อ Object ที่สร้าง", "Generated Object"),
                generatedRootName
            );

            saveFolder = EditorGUILayout.TextField(
                T("โฟลเดอร์บันทึก", "Save Folder"),
                saveFolder
            );

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(sectionStyle);
        DrawSectionHeader(T("Non-destructive", "Non-destructive"));

        EditorGUILayout.HelpBox(
            T(
                "mehigo สร้าง asset แยก และให้ Modular Avatar merge ตอน build โดยไม่แก้ FX / Parameters / Menu เดิมโดยตรง",
                "mehigo generates separate assets and lets Modular Avatar merge them at build time without directly editing the original FX / Parameters / Menu."
            ),
            MessageType.Info
        );

        EditorGUILayout.EndVertical();
    }

    private void DrawHairTab()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(T($"ทรงผม ({hairs.Count})", $"Hair Styles ({hairs.Count})"), titleStyle);

        GUILayout.FlexibleSpace();

        compactHairCards = GUILayout.Toggle(
            compactHairCards,
            T("ย่อ", "Compact"),
            EditorStyles.miniButton,
            GUILayout.Width(70)
        );

        if (GUILayout.Button(T("+ เพิ่มทรงผม", "+ Add Hair"), GUILayout.Width(90)))
        {
            hairs.Add(new HairEntry
            {
                menuName = "Hair " + (hairs.Count + 1)
            });
            scanComplete = false;
        }

        if (GUILayout.Button(T("เพิ่มที่เลือก", "Add Selected"), GUILayout.Width(100)))
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                if (go == null || hairs.Any(h => h.hairObject == go))
                    continue;

                hairs.Add(new HairEntry
                {
                    menuName = go.name,
                    hairObject = go,
                    activationTarget = go
                });
            }

            scanComplete = false;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        int removeIndex = -1;

        for (int i = 0; i < hairs.Count; i++)
        {
            HairEntry hair = hairs[i];

            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.BeginHorizontal();

            hair.foldout = EditorGUILayout.Foldout(
                hair.foldout,
                $"Index {i} • {SafeName(hair.menuName, "Hair")}",
                true,
                EditorStyles.foldoutHeader
            );

            if (GUILayout.Button("▲", GUILayout.Width(28)) && i > 0)
            {
                Swap(i, i - 1);
                scanComplete = false;
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("▼", GUILayout.Width(28)) && i < hairs.Count - 1)
            {
                Swap(i, i + 1);
                scanComplete = false;
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("X", GUILayout.Width(28)))
                removeIndex = i;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                hair.hairObject != null
                    ? hair.hairObject.name
                    : T("ยังไม่ได้เลือก Object", "No Object"),
                EditorStyles.miniLabel
            );

            GUILayout.FlexibleSpace();
            GUILayout.Label(GetActivationSummary(hair), badgeStyle);

            if (hair.materialPresets.Count > 0)
                GUILayout.Label(T($"สี {hair.materialPresets.Count}", $"Color {hair.materialPresets.Count}"), badgeStyle);

            if (hair.blendShapes.Count > 0)
                GUILayout.Label($"BS {hair.blendShapes.Count}", badgeStyle);

            EditorGUILayout.EndHorizontal();

            if (compactHairCards)
                hair.foldout = false;

            if (hair.foldout)
            {
                hair.menuName = EditorGUILayout.TextField(T("ชื่อปุ่มเมนู", "Menu Button Name"), hair.menuName);

                DrawIconSelector(
                    T("ไอคอนปุ่ม", "Button Icon"),
                    ref hair.iconMode,
                    ref hair.icon,
                    $"Hair_{i}_{SanitizeFileName(SafeName(hair.menuName, "Hair"))}"
                );

                GameObject oldHair = hair.hairObject;

                hair.hairObject = (GameObject)EditorGUILayout.ObjectField(
                    T("Object ทรงผม", "Hair Object"),
                    hair.hairObject,
                    typeof(GameObject),
                    true
                );

                if (oldHair != hair.hairObject)
                {
                    if (hair.autoDetectActivation)
                        AutoDetectActivationMode(hair);
                    else if (hair.activationMode == ActivationMode.ControlHairRoot)
                        hair.activationTarget = hair.hairObject;

                    scanComplete = false;
                }

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Auto Detect", GUILayout.Height(24)))
                {
                    AutoDetectActivationMode(hair);
                    scanComplete = false;
                }

                if (GUILayout.Button(T("สแกน Material", "Scan Materials"), GUILayout.Height(24)))
                {
                    ScanDefaultMaterialPreset(hair);
                    scanComplete = false;
                }

                EditorGUILayout.EndHorizontal();

                DrawCompatibilitySettings(hair);
                DrawLinkedObjects(hair);
                DrawBlendShapes(hair);
                DrawMaterialPresets(hair, i);
            }

            EditorGUILayout.EndVertical();
        }

        if (removeIndex >= 0)
        {
            hairs.RemoveAt(removeIndex);
            scanComplete = false;
        }

        if (hairs.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Add hair styles or load an existing v4 setup.",
                MessageType.Info
            );
        }
    }

    private void DrawCompatibilitySettings(HairEntry hair)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        hair.compatibilityFoldout = EditorGUILayout.Foldout(
            hair.compatibilityFoldout,
            "Compatibility",
            true
        );

        if (hair.compatibilityFoldout)
        {
            hair.preserveExistingAnimator = EditorGUILayout.Toggle(
                new GUIContent(
                    T("รักษา Animator เดิม", "Preserve Existing Animator"),
                    "mehigo only controls properties explicitly configured here. Existing Animator/MA assets remain untouched."
                ),
                hair.preserveExistingAnimator
            );

            ActivationMode oldMode = hair.activationMode;

            hair.autoDetectActivation = EditorGUILayout.Toggle(
                T("Auto Detect Activation", "Auto Detect Activation"),
                hair.autoDetectActivation
            );

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(
                T("ตรวจอัตโนมัติใหม่", "Re-Detect"),
                GUILayout.Width(110)))
            {
                AutoDetectActivationMode(hair);
                scanComplete = false;
            }

            EditorGUILayout.LabelField(
                T(
                    "ตรวจจาก Hair Root, Parent, Renderer และ Animator",
                    "Detects from Hair Root, parent, renderers and animators"
                ),
                EditorStyles.miniLabel
            );

            EditorGUILayout.EndHorizontal();

            if (hair.autoDetectActivation)
            {
                AutoDetectActivationMode(hair);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.EnumPopup(
                    T("โหมดที่ตรวจพบ", "Detected Mode"),
                    hair.activationMode
                );

                if (hair.activationMode == ActivationMode.ControlExistingWrapper)
                {
                    EditorGUILayout.ObjectField(
                        T("Wrapper ที่ตรวจพบ", "Detected Wrapper"),
                        hair.activationTarget,
                        typeof(GameObject),
                        true
                    );
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                string[] activationLabels =
                    language == EditorLanguage.Thai
                        ? new[] { "คุม Root ของทรงผม", "คุม Wrapper ที่มีอยู่", "ไม่คุม Object" }
                        : new[] { "Control Hair Root", "Control Existing Wrapper", "Do Not Control Object" };

                hair.activationMode = (ActivationMode)EditorGUILayout.Popup(
                    T("โหมดเปิด/ปิด", "Activation Mode"),
                    (int)hair.activationMode,
                    activationLabels
                );
            }

            DrawActivationRecommendation(hair);

            if (oldMode != hair.activationMode)
            {
                if (hair.activationMode == ActivationMode.ControlHairRoot)
                    hair.activationTarget = hair.hairObject;

                scanComplete = false;
            }

            switch (hair.activationMode)
            {
                case ActivationMode.ControlHairRoot:
                    hair.activationTarget = hair.hairObject;

                    EditorGUILayout.HelpBox(
                        "mehigo animates m_IsActive on the Hair Object itself. Use only when the hair's own animator does not animate that same root object.",
                        MessageType.None
                    );
                    break;

                case ActivationMode.ControlExistingWrapper:
                    hair.activationTarget = (GameObject)EditorGUILayout.ObjectField(
                        T("Wrapper ที่มีอยู่แล้ว", "Existing Wrapper"),
                        hair.activationTarget,
                        typeof(GameObject),
                        true
                    );

                    EditorGUILayout.HelpBox(
                        "Recommended when the hair package already has its own Animator/Modular Avatar setup. " +
                        "Choose an existing parent/wrapper that is safe for mehigo to enable/disable. v4 never moves the hair automatically.",
                        MessageType.Info
                    );
                    break;

                case ActivationMode.DoNotControlObject:
                    hair.activationTarget = null;

                    EditorGUILayout.HelpBox(
                        "mehigo changes HairIndex/menu only and does not animate this hair object's Active state. " +
                        "Use this if the original package already handles visibility.",
                        MessageType.Info
                    );
                    break;
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawLinkedObjects(HairEntry hair)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();

        hair.linkedFoldout = EditorGUILayout.Foldout(
            hair.linkedFoldout,
            T($"Object ที่ผูกไว้ ({hair.linkedObjects.Count})", $"Linked Objects ({hair.linkedObjects.Count})"),
            true
        );

        if (GUILayout.Button("+", GUILayout.Width(30)))
        {
            hair.linkedObjects.Add(null);
            scanComplete = false;
        }

        EditorGUILayout.EndHorizontal();

        if (hair.linkedFoldout)
        {
            EditorGUILayout.LabelField(
                "mehigo explicitly animates these objects. Conflict Scanner checks them against existing animations.",
                EditorStyles.miniLabel
            );

            int remove = -1;

            for (int j = 0; j < hair.linkedObjects.Count; j++)
            {
                EditorGUILayout.BeginHorizontal();

                GameObject old = hair.linkedObjects[j];

                hair.linkedObjects[j] = (GameObject)EditorGUILayout.ObjectField(
                    $"Object {j}",
                    hair.linkedObjects[j],
                    typeof(GameObject),
                    true
                );

                if (old != hair.linkedObjects[j])
                    scanComplete = false;

                if (GUILayout.Button("-", GUILayout.Width(28)))
                    remove = j;

                EditorGUILayout.EndHorizontal();
            }

            if (remove >= 0)
            {
                hair.linkedObjects.RemoveAt(remove);
                scanComplete = false;
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBlendShapes(HairEntry hair)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();

        hair.blendFoldout = EditorGUILayout.Foldout(
            hair.blendFoldout,
            T($"ปุ่ม BlendShape ({hair.blendShapes.Count})", $"BlendShape Buttons ({hair.blendShapes.Count})"),
            true
        );

        if (GUILayout.Button(T("+ เพิ่ม", "+ Add"), GUILayout.Width(60)))
        {
            hair.blendShapes.Add(new BlendShapeOption
            {
                menuName = "BlendShape " + (hair.blendShapes.Count + 1)
            });

            scanComplete = false;
        }

        EditorGUILayout.EndHorizontal();

        if (hair.blendFoldout)
        {
            int remove = -1;

            for (int j = 0; j < hair.blendShapes.Count; j++)
            {
                BlendShapeOption bs = hair.blendShapes[j];

                EditorGUILayout.BeginVertical(cardStyle);
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField($"BlendShape {j}", EditorStyles.boldLabel);

                if (GUILayout.Button(T("ลบ", "Remove"), GUILayout.Width(65)))
                    remove = j;

                EditorGUILayout.EndHorizontal();

                bs.menuName = EditorGUILayout.TextField(T("ชื่อปุ่ม", "Button Name"), bs.menuName);

                string[] controlModeLabels =
                    language == EditorLanguage.Thai
                        ? new[] { "ปุ่มเปิด/ปิด (Toggle)", "วงล้อปรับค่า (Radial)" }
                        : new[] { "Toggle", "Radial Puppet" };

                bs.controlMode = (BlendShapeControlMode)EditorGUILayout.Popup(
                    T("รูปแบบการควบคุม", "Control Type"),
                    (int)bs.controlMode,
                    controlModeLabels
                );

                SkinnedMeshRenderer oldRenderer = bs.renderer;

                bs.renderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
                    "Renderer",
                    bs.renderer,
                    typeof(SkinnedMeshRenderer),
                    true
                );

                if (oldRenderer != bs.renderer)
                    scanComplete = false;

                if (bs.renderer != null && bs.renderer.sharedMesh != null)
                {
                    Mesh mesh = bs.renderer.sharedMesh;

                    if (mesh.blendShapeCount > 0)
                    {
                        string[] names = new string[mesh.blendShapeCount];

                        for (int k = 0; k < names.Length; k++)
                            names[k] = mesh.GetBlendShapeName(k);

                        int current = Array.IndexOf(names, bs.blendShapeName);
                        if (current < 0) current = 0;

                        int selected = EditorGUILayout.Popup("BlendShape", current, names);

                        if (bs.blendShapeName != names[selected])
                            scanComplete = false;

                        bs.blendShapeName = names[selected];

                        bs.onValue = EditorGUILayout.Slider(
                            bs.controlMode == BlendShapeControlMode.RadialPuppet
                                ? T("ค่าสูงสุดของวงล้อ", "Radial Max Value")
                                : T("ค่าเมื่อเปิด", "ON Value"),
                            bs.onValue,
                            0f,
                            100f
                        );

                        bs.saved = EditorGUILayout.Toggle(T("บันทึกค่า", "Saved"), bs.saved);

                        if (bs.controlMode == BlendShapeControlMode.RadialPuppet)
                        {
                            EditorGUILayout.HelpBox(
                                T(
                                    "วงล้อจะใช้ Float Parameter ค่า 0-1 แล้วแปลงเป็น BlendShape 0 ถึงค่าสูงสุดที่กำหนด",
                                    "Radial Puppet uses a Float parameter from 0-1 and maps it to BlendShape 0 through the configured maximum value."
                                ),
                                MessageType.None
                            );
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            "Selected mesh has no BlendShapes.",
                            MessageType.Warning
                        );
                    }
                }

                EditorGUILayout.EndVertical();
            }

            if (remove >= 0)
            {
                hair.blendShapes.RemoveAt(remove);
                scanComplete = false;
            }
        }

        EditorGUILayout.EndVertical();
    }


    private void DrawMaterialPresets(HairEntry hair, int hairIndex)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        hair.materialFoldout = EditorGUILayout.Foldout(
            hair.materialFoldout,
            T(
                $"สีผม / Material Preset ({hair.materialPresets.Count})",
                $"Hair Color / Material Presets ({hair.materialPresets.Count})"
            ),
            true
        );

        if (GUILayout.Button(T("สแกน Material", "Scan Materials"), GUILayout.Width(110)))
        {
            ScanDefaultMaterialPreset(hair);
            scanComplete = false;
        }
        EditorGUILayout.EndHorizontal();

        if (hair.materialFoldout)
        {
            EditorGUILayout.HelpBox(
                T(
                    "Default Preset จะจำ Material ทุก slot ใต้ Hair Root อัตโนมัติ Preset ใหม่จะคัดลอกรายการนี้มาให้ครบ แล้วแก้ Material เฉพาะส่วนที่ต้องการได้",
                    "Default Preset snapshots every material slot under the Hair Root. New presets copy that slot layout so you can replace only the materials you want."
                ),
                MessageType.Info
            );

            if (hair.materialPresets.Count == 0)
            {
                if (GUILayout.Button(T("สร้าง Default Material Preset", "Create Default Material Preset")))
                    ScanDefaultMaterialPreset(hair);
            }
            else
            {
                DrawMaterialPresetSlots(hair.materialPresets[0], true);

                int remove = -1;
                for (int p = 1; p < hair.materialPresets.Count; p++)
                {
                    MaterialPreset preset = hair.materialPresets[p];
                    EditorGUILayout.BeginVertical(cardStyle);
                    EditorGUILayout.BeginHorizontal();
                    preset.foldout = EditorGUILayout.Foldout(
                        preset.foldout,
                        preset.menuName,
                        true,
                        EditorStyles.foldoutHeader
                    );
                    if (GUILayout.Button(T("ลบ", "Remove"), GUILayout.Width(60)))
                        remove = p;
                    EditorGUILayout.EndHorizontal();

                    if (preset.foldout)
                    {
                        preset.menuName = EditorGUILayout.TextField(
                            T("ชื่อปุ่ม", "Button Name"),
                            preset.menuName
                        );

                        preset.icon = (Texture2D)EditorGUILayout.ObjectField(
                            T("ไอคอน", "Icon"),
                            preset.icon,
                            typeof(Texture2D),
                            false
                        );

                        DrawMaterialPresetSlots(preset, false);
                    }

                    EditorGUILayout.EndVertical();
                }

                if (remove >= 1)
                    hair.materialPresets.RemoveAt(remove);

                if (GUILayout.Button(T("+ เพิ่ม Material Preset", "+ Add Material Preset")))
                    AddMaterialPresetFromDefault(hair);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawMaterialPresetSlots(MaterialPreset preset, bool isDefault)
    {
        if (preset == null || preset.slots == null)
            return;

        if (isDefault)
            EditorGUILayout.LabelField(T("Default Material Preset", "Default Material Preset"), EditorStyles.boldLabel);

        for (int i = 0; i < preset.slots.Count; i++)
        {
            MaterialSlotEntry slot = preset.slots[i];

            EditorGUILayout.BeginHorizontal();

            string rendererName =
                slot.renderer != null
                    ? slot.renderer.name
                    : T("Renderer หาย", "Missing Renderer");

            EditorGUILayout.LabelField(
                $"{rendererName} [Slot {slot.materialIndex}]",
                GUILayout.MinWidth(180)
            );

            EditorGUI.BeginDisabledGroup(isDefault);
            slot.material = (Material)EditorGUILayout.ObjectField(
                slot.material,
                typeof(Material),
                false
            );
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }
    }

    private void ScanDefaultMaterialPreset(HairEntry hair)
    {
        if (hair.hairObject == null)
        {
            Debug.LogError("[mehigo] Hair Object is required before scanning materials.");
            return;
        }

        List<MaterialSlotEntry> slots = new List<MaterialSlotEntry>();

        foreach (Renderer renderer in hair.hairObject.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                slots.Add(new MaterialSlotEntry
                {
                    renderer = renderer,
                    materialIndex = i,
                    material = materials[i]
                });
            }
        }

        MaterialPreset defaultPreset = new MaterialPreset
        {
            menuName = "Default",
            slots = slots
        };

        if (hair.materialPresets.Count == 0)
            hair.materialPresets.Add(defaultPreset);
        else
            hair.materialPresets[0] = defaultPreset;

        for (int p = 1; p < hair.materialPresets.Count; p++)
        {
            MaterialPreset preset = hair.materialPresets[p];
            List<MaterialSlotEntry> rebuilt = new List<MaterialSlotEntry>();

            foreach (MaterialSlotEntry defaultSlot in slots)
            {
                MaterialSlotEntry existing = preset.slots.FirstOrDefault(
                    s => s.renderer == defaultSlot.renderer &&
                         s.materialIndex == defaultSlot.materialIndex
                );

                rebuilt.Add(new MaterialSlotEntry
                {
                    renderer = defaultSlot.renderer,
                    materialIndex = defaultSlot.materialIndex,
                    material = existing != null ? existing.material : defaultSlot.material
                });
            }

            preset.slots = rebuilt;
        }

        Debug.Log($"[mehigo] Scanned {slots.Count} material slots for {hair.hairObject.name}.");
    }

    private void AddMaterialPresetFromDefault(HairEntry hair)
    {
        if (hair.materialPresets.Count == 0)
            ScanDefaultMaterialPreset(hair);

        if (hair.materialPresets.Count == 0)
            return;

        MaterialPreset preset = new MaterialPreset
        {
            menuName = "Color " + hair.materialPresets.Count,
            slots = hair.materialPresets[0].slots
                .Select(s => new MaterialSlotEntry
                {
                    renderer = s.renderer,
                    materialIndex = s.materialIndex,
                    material = s.material
                })
                .ToList()
        };

        hair.materialPresets.Add(preset);
    }

    private void AutoDetectActivationMode(HairEntry hair)
    {
        if (hair == null || hair.hairObject == null)
        {
            return;
        }

        Transform root = hair.hairObject.transform;
        Transform parent = root.parent;

        // Default safest assumption: the selected object is the complete hairstyle root.
        hair.activationMode = ActivationMode.ControlHairRoot;
        hair.activationTarget = hair.hairObject;

        // If the selected root already contains several renderers/children or its own animator,
        // it is likely self-contained and should remain ControlHairRoot.
        bool rootLooksSelfContained =
            root.childCount > 0 ||
            hair.hairObject.GetComponentsInChildren<Renderer>(true).Length > 1 ||
            hair.hairObject.GetComponentInChildren<Animator>(true) != null;

        if (rootLooksSelfContained)
        {
            return;
        }

        if (parent == null || avatar == null || parent == avatar.transform)
        {
            return;
        }

        // Look for sibling content suggesting that the selected Hair Object is only one part
        // of a larger hair package under a wrapper parent.
        int siblingRenderers = 0;
        int siblingAnimators = 0;
        int siblingObjects = 0;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform sibling = parent.GetChild(i);

            if (sibling == root)
                continue;

            siblingObjects++;

            if (sibling.GetComponentInChildren<Renderer>(true) != null)
                siblingRenderers++;

            if (sibling.GetComponentInChildren<Animator>(true) != null)
                siblingAnimators++;
        }

        bool parentLooksLikeWrapper =
            siblingRenderers > 0 ||
            siblingAnimators > 0 ||
            siblingObjects >= 2;

        if (parentLooksLikeWrapper)
        {
            hair.activationMode = ActivationMode.ControlExistingWrapper;
            hair.activationTarget = parent.gameObject;
            return;
        }

        // If the root has no renderer at all but parent does, parent is also likely the wrapper.
        bool rootHasRenderer =
            hair.hairObject.GetComponentInChildren<Renderer>(true) != null;

        bool parentHasRenderer =
            parent.GetComponentInChildren<Renderer>(true) != null;

        if (!rootHasRenderer && parentHasRenderer)
        {
            hair.activationMode = ActivationMode.ControlExistingWrapper;
            hair.activationTarget = parent.gameObject;
        }
    }

    private void DrawActivationRecommendation(HairEntry hair)
    {
        if (hair == null || hair.hairObject == null)
        {
            EditorGUILayout.HelpBox(
                T("คำแนะนำ: เลือก Hair Object ก่อน แล้ว mehigo จะช่วยแนะนำโหมดที่เหมาะสม",
                  "Recommendation: Select a Hair Object first and mehigo will suggest a suitable mode."),
                MessageType.None);
            return;
        }

        Transform root = hair.hairObject.transform;
        Transform parent = root.parent;
        bool hasAnimator = hair.hairObject.GetComponentInChildren<Animator>(true) != null;
        bool hasMultipleRenderers = hair.hairObject.GetComponentsInChildren<Renderer>(true).Length > 1;
        bool parentLooksLikeWrapper = false;

        if (parent != null && avatar != null && parent != avatar.transform)
        {
            for (int n = 0; n < parent.childCount; n++)
            {
                Transform sibling = parent.GetChild(n);
                if (sibling == root) continue;

                if (sibling.GetComponentInChildren<Renderer>(true) != null ||
                    sibling.GetComponentInChildren<Animator>(true) != null)
                {
                    parentLooksLikeWrapper = true;
                    break;
                }
            }
        }

        string msg;
        MessageType type = MessageType.Info;

        if (hair.activationMode == ActivationMode.DoNotControlObject)
        {
            msg = T(
                "เหมาะเมื่อมีระบบอื่นควบคุมการเปิด/ปิดทรงผมอยู่แล้ว mehigo จะไม่ Animate Active ของ Hair Object",
                "Use this when another system already controls hair visibility. mehigo will not animate the Hair Object active state.");
        }
        else if (parentLooksLikeWrapper)
        {
            msg = T(
                $"พบ Parent \"{parent.name}\" ที่มี Object/Renderer อื่นร่วมกับ Hair Root อาจเป็น Wrapper ของชุดผม ลองปิด Parent นี้ใน Hierarchy ถ้าผมหายครบทั้งชุด แนะนำ Control Existing Wrapper",
                $"Parent \"{parent.name}\" contains other objects/renderers alongside the Hair Root and may be the hairstyle wrapper. Disable it in the Hierarchy; if the complete hairstyle disappears, Control Existing Wrapper is recommended.");
            type = MessageType.Warning;
        }
        else if (root.childCount > 0 || hasMultipleRenderers || hasAnimator)
        {
            msg = T(
                "Hair Object ดูเป็น Root ของชุดผมอยู่แล้ว ถ้าปิด Object นี้แล้วผมหายครบทั้งชุด แนะนำ Control Hair Root",
                "The selected Hair Object appears to be the hairstyle root. If disabling it hides the complete hairstyle, Control Hair Root is recommended.");
        }
        else
        {
            msg = T(
                "ลองปิด Hair Object ใน Hierarchy: ถ้าผมหายครบใช้ Control Hair Root ถ้ายังมีชิ้นส่วนเหลือ ให้เลือก Parent ที่ครอบทั้งชุดเป็น Existing Wrapper",
                "Disable the Hair Object in the Hierarchy: if the whole hairstyle disappears, use Control Hair Root. If pieces remain, use the parent containing the full set as Existing Wrapper.");
        }

        EditorGUILayout.HelpBox(
            T("คำแนะนำจาก mehigo:\n", "mehigo Recommendation:\n") + msg,
            type);
    }

    // ---------------------------------------------------------------------
    // COMPATIBILITY TAB
    // ---------------------------------------------------------------------

    private void DrawCompatibilityTab()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        EditorGUILayout.LabelField(T("ตัวตรวจ Conflict", "Conflict Scanner"), titleStyle);

        EditorGUILayout.HelpBox(
            "The scanner looks for Animator Controllers and Modular Avatar Merge Animators under each hair, " +
            "reads their AnimationClip curve bindings, and compares them with properties mehigo will animate.",
            MessageType.Info
        );

        GUI.enabled = avatar != null && hairs.Count > 0;

        if (GUILayout.Button(T("สแกน Animator / MA Conflict", "Scan Animator / MA Conflicts"), GUILayout.Height(38)))
            ScanConflicts();

        GUI.enabled = true;

        EditorGUILayout.EndVertical();

        if (!scanComplete)
        {
            EditorGUILayout.HelpBox(
                T(
                "ควรสแกนใหม่หลังเปลี่ยน Hair, Wrapper, Linked Objects หรือ BlendShapes",
                "Run the scanner after changing Hair, Wrapper, Linked Objects, or BlendShapes."
            ),
                MessageType.None
            );
            return;
        }

        if (conflicts.Count == 0)
        {
            EditorGUILayout.HelpBox(
                T(
                "ไม่พบ property ที่ชนกันโดยตรงใน Animator/MA ที่ v4 ตรวจสอบได้",
                "No direct property conflicts were found in the Animator/MA controllers that v4 could inspect."
            ),
                MessageType.Info
            );
            return;
        }

        EditorGUILayout.BeginVertical(sectionStyle);
        EditorGUILayout.LabelField(T($"Conflict ที่อาจเกิดขึ้น ({conflicts.Count})", $"Potential Conflicts ({conflicts.Count})"), titleStyle);

        foreach (ConflictItem conflict in conflicts)
        {
            EditorGUILayout.BeginVertical(cardStyle);

            EditorGUILayout.LabelField(
                $"{conflict.hairName} • {conflict.sourceName}",
                EditorStyles.boldLabel
            );

            EditorGUILayout.LabelField("Path", conflict.path);
            EditorGUILayout.LabelField("Property", conflict.property);
            EditorGUILayout.HelpBox(conflict.reason, MessageType.Warning);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }

    private void ScanConflicts()
    {
        conflicts.Clear();

        if (avatar == null)
        {
            Debug.LogError("[mehigo] Select an Avatar first.");
            return;
        }

        for (int hairIndex = 0; hairIndex < hairs.Count; hairIndex++)
        {
            HairEntry hair = hairs[hairIndex];

            if (hair.hairObject == null)
                continue;

            HashSet<string> mehigoProperties = BuildMehigoPropertySetForHair(hairIndex);
            List<RuntimeAnimatorController> controllers = FindControllersUnderHair(hair.hairObject);

            foreach (RuntimeAnimatorController controller in controllers.Distinct())
            {
                if (controller == null) continue;

                foreach (AnimationClip clip in controller.animationClips.Distinct())
                {
                    if (clip == null) continue;

                    foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        string absolutePath = ConvertHairRelativePathToAvatarPath(
                            hair.hairObject,
                            binding.path
                        );

                        string key = MakePropertyKey(absolutePath, binding.propertyName);

                        if (mehigoProperties.Contains(key))
                        {
                            conflicts.Add(new ConflictItem
                            {
                                hairName = SafeName(hair.menuName, $"Hair {hairIndex}"),
                                sourceName = controller.name + " / " + clip.name,
                                path = absolutePath,
                                property = binding.propertyName,
                                reason =
                                    "Existing animation and mehigo write the same property. " +
                                    "Use an Existing Wrapper, remove this property from mehigo, or let the original package control visibility."
                            });
                        }
                    }
                }
            }
        }

        scanComplete = true;
        Repaint();

        Debug.Log($"[mehigo] Conflict scan complete. Found {conflicts.Count} potential conflicts.");
    }

    private HashSet<string> BuildMehigoPropertySetForHair(int hairIndex)
    {
        HashSet<string> set = new HashSet<string>();
        HairEntry hair = hairs[hairIndex];

        GameObject activation = GetActivationTarget(hair);

        if (activation != null)
            set.Add(MakePropertyKey(GetAvatarPath(activation), "m_IsActive"));

        // All linked objects are animated by every hair selection clip.
        foreach (HairEntry h in hairs)
        {
            foreach (GameObject linked in h.linkedObjects)
            {
                if (linked != null)
                    set.Add(MakePropertyKey(GetAvatarPath(linked), "m_IsActive"));
            }
        }

        foreach (BlendShapeOption bs in hair.blendShapes)
        {
            if (bs.renderer == null || string.IsNullOrWhiteSpace(bs.blendShapeName))
                continue;

            set.Add(
                MakePropertyKey(
                    GetAvatarPath(bs.renderer.gameObject),
                    "blendShape." + bs.blendShapeName
                )
            );
        }

        return set;
    }

    private List<RuntimeAnimatorController> FindControllersUnderHair(GameObject hairRoot)
    {
        List<RuntimeAnimatorController> result = new List<RuntimeAnimatorController>();

        foreach (Animator animator in hairRoot.GetComponentsInChildren<Animator>(true))
        {
            if (animator.runtimeAnimatorController != null)
                result.Add(animator.runtimeAnimatorController);
        }

        Type mergeAnimatorType =
            FindType("nadena.dev.modular_avatar.core.ModularAvatarMergeAnimator");

        if (mergeAnimatorType != null)
        {
            foreach (Component component in hairRoot.GetComponentsInChildren(mergeAnimatorType, true))
            {
                SerializedObject so = new SerializedObject(component);
                SerializedProperty animatorProp = so.FindProperty("animator");

                if (animatorProp != null &&
                    animatorProp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    RuntimeAnimatorController controller =
                        animatorProp.objectReferenceValue as RuntimeAnimatorController;

                    if (controller != null)
                        result.Add(controller);
                }
            }
        }

        return result;
    }

    private string ConvertHairRelativePathToAvatarPath(
        GameObject hairRoot,
        string relativePath)
    {
        string hairPath = GetAvatarPath(hairRoot);

        if (string.IsNullOrEmpty(relativePath))
            return hairPath;

        if (string.IsNullOrEmpty(hairPath))
            return relativePath;

        return hairPath + "/" + relativePath;
    }

    private string MakePropertyKey(string path, string property)
    {
        return (path ?? "") + "||" + (property ?? "");
    }



    private void DetectAAOCompatibility()
    {
        aaoDetected = false;
        aaoTraceAndOptimizeDetected = false;
        aaoMergeMaterialDetected = false;
        aaoMergeSkinnedMeshDetected = false;
        aaoAnimatedMaterialPresetCount = 0;
        aaoHairToggleCount = 0;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string assemblyName = assembly.GetName().Name ?? "";

            if (assemblyName.IndexOf("avatar-optimizer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                assemblyName.IndexOf("AvatarOptimizer", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                aaoDetected = true;
                break;
            }
        }

        foreach (HairEntry hair in hairs)
        {
            if (GetActivationTarget(hair) != null)
                aaoHairToggleCount++;

            if (hair.materialPresets.Count > 1)
                aaoAnimatedMaterialPresetCount++;
        }

        if (avatar == null)
            return;

        Component[] components =
            avatar.GetComponentsInChildren<Component>(true);

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            Type type = component.GetType();
            string fullName = type.FullName ?? type.Name;
            string assemblyName = type.Assembly.GetName().Name ?? "";

            bool isAAO =
                fullName.IndexOf("AvatarOptimizer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                assemblyName.IndexOf("avatar-optimizer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                assemblyName.IndexOf("AvatarOptimizer", StringComparison.OrdinalIgnoreCase) >= 0;

            if (!isAAO)
                continue;

            aaoDetected = true;

            string simple = type.Name;

            if (simple.IndexOf("TraceAndOptimize", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullName.IndexOf("TraceAndOptimize", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                aaoTraceAndOptimizeDetected = true;
            }

            if (simple.IndexOf("MergeMaterial", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullName.IndexOf("MergeMaterial", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                aaoMergeMaterialDetected = true;
            }

            if (simple.IndexOf("MergeSkinnedMesh", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullName.IndexOf("MergeSkinnedMesh", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                aaoMergeSkinnedMeshDetected = true;
            }
        }

        if (optimizationMode == OptimizationMode.Optimized)
        {
            optimizationMode = OptimizationMode.Safe;
        }
    }

    // ---------------------------------------------------------------------
    // PERFORMANCE TAB
    // ---------------------------------------------------------------------

    private void DrawPerformanceTab()
    {
        DetectAAOCompatibility();

        if (optimizationMode == OptimizationMode.Optimized)
            optimizationMode = OptimizationMode.Safe;

        int selectorLayers = hairs.Count > 0 ? 1 : 0;
        int blendShapeLayers = CountBlendShapes();
        int materialLayers = CountMaterialParameters();
        int standardTotal = selectorLayers + blendShapeLayers + materialLayers;

        EditorGUILayout.BeginVertical(sectionStyle);
        DrawSectionHeader(
            T("Animator Optimization", "Animator Optimization"),
            T(
                "ใช้โครง Animator ที่เสถียร แล้วให้ AAO optimize ต่อเมื่อมี",
                "Use a stable Animator layout, then let AAO optimize it when available"
            )
        );

        EditorGUILayout.HelpBox(
            T(
                "Direct BlendTree Optimized Mode ถูกปิดใช้งาน เพราะพบการรบกวน BlendShape/Radial ข้ามส่วนของ Avatar ในการใช้งานจริง",
                "Direct BlendTree Optimized Mode is disabled because real avatar tests showed cross-influence between BlendShape/Radial controls."
            ),
            MessageType.Warning
        );

        EditorGUILayout.BeginHorizontal();

        DrawOptimizationModeCard(
            OptimizationMode.Safe,
            T("โหมดมาตรฐาน", "Standard"),
            T(
                "1 BlendShape control = 1 Layer\nRadial ใช้ 1D BlendTree แบบเดิม\nเสถียรและ Debug ง่าย",
                "1 BlendShape control = 1 Layer\nRadial keeps the proven 1D BlendTree layout\nStable and easy to debug"
            ),
            standardTotal,
            !aaoTraceAndOptimizeDetected
        );

        GUILayout.Space(6);

        DrawOptimizationModeCard(
            OptimizationMode.LetAAOHandleIt,
            T("ให้ AAO จัดการ", "Let AAO Handle It"),
            T(
                "mehigo สร้าง Standard เหมือนกัน\nAAO Optimize Animator ตอน Build\nแนะนำเมื่อมี Trace and Optimize",
                "mehigo still generates Standard\nAAO Optimize Animator runs at build time\nRecommended with Trace and Optimize"
            ),
            standardTotal,
            aaoTraceAndOptimizeDetected
        );

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginVertical(subtleBoxStyle);
        EditorGUILayout.LabelField(
            T("Generated Animator (ก่อน AAO Build)", "Generated Animator (before AAO build)"),
            EditorStyles.boldLabel
        );

        DrawPerfRow(T("Hair Selector Layers", "Hair Selector Layers"), selectorLayers.ToString());
        DrawPerfRow(T("BlendShape Layers", "BlendShape Layers"), blendShapeLayers.ToString());
        DrawPerfRow(T("Material Layers", "Material Layers"), materialLayers.ToString());
        DrawPerfRow(T("รวม", "Total"), standardTotal.ToString());

        if (optimizationMode == OptimizationMode.LetAAOHandleIt)
        {
            EditorGUILayout.HelpBox(
                aaoTraceAndOptimizeDetected
                    ? T(
                        "จำนวน Layer หลัง AAO Build ขึ้นกับผล Optimize Animator ของ AAO จึงไม่แสดงตัวเลขเดาล่วงหน้า",
                        "The final layer structure after AAO build depends on AAO's Animator optimization, so mehigo does not guess a post-build layer count."
                    )
                    : T(
                        "ยังไม่พบ Trace and Optimize — โหมดนี้จะทำงานเหมือน Standard จนกว่าจะเพิ่ม AAO Trace and Optimize",
                        "Trace and Optimize was not detected — this behaves like Standard until AAO Trace and Optimize is added."
                    ),
                aaoTraceAndOptimizeDetected ? MessageType.Info : MessageType.Warning
            );
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.HelpBox(
            aaoTraceAndOptimizeDetected
                ? T("mehigo แนะนำ: ให้ AAO จัดการ", "mehigo recommends: Let AAO Handle It")
                : T("mehigo แนะนำ: โหมดมาตรฐาน", "mehigo recommends: Standard"),
            MessageType.Info
        );

        EditorGUILayout.EndVertical();

        DrawAAOCompatibilityPanel();

        EditorGUILayout.BeginVertical(sectionStyle);
        DrawSectionHeader(
            T("Performance Analyzer", "Performance Analyzer"),
            T(
                "วิเคราะห์ Mesh, Material, BlendShape และสิ่งที่ mehigo จะสร้าง",
                "Analyze meshes, materials, BlendShapes, and generated mehigo content"
            )
        );

        if (GUILayout.Button(
            T("วิเคราะห์ Performance", "Analyze Performance"),
            GUILayout.Height(36)))
        {
            AnalyzePerformance();
        }

        EditorGUILayout.EndVertical();

        if (!perfAnalyzed)
        {
            EditorGUILayout.HelpBox(
                T(
                    "กด Analyze Performance เพื่อดูรายละเอียดของ Avatar",
                    "Press Analyze Performance to inspect the avatar."
                ),
                MessageType.None
            );
            return;
        }

        EditorGUILayout.BeginVertical(sectionStyle);
        DrawSectionHeader(T("ผลวิเคราะห์", "Analysis"));

        DrawPerfRow(T("Triangles ของ Hair", "Hair Triangles"), perfTriangles.ToString("N0"));
        DrawPerfRow(T("Renderer", "Renderers"), perfRenderers.ToString());
        DrawPerfRow(T("Skinned Mesh", "Skinned Meshes"), perfSkinnedMeshes.ToString());
        DrawPerfRow(T("Material Slots", "Material Slots"), perfMaterialSlots.ToString());
        DrawPerfRow(T("BlendShapes ใน Mesh", "Mesh BlendShapes"), perfBlendShapes.ToString());
        DrawPerfRow(T("Animator Components", "Animator Components"), perfAnimatorComponents.ToString());
        DrawPerfRow(T("mehigo Parameters", "mehigo Parameters"), perfGeneratedParameters.ToString());
        DrawPerfRow(T("mehigo Animator Layers", "mehigo Animator Layers"), perfGeneratedLayers.ToString());

        EditorGUILayout.Space(6);

        string impact = GetPerformanceImpactLabel();

        EditorGUILayout.HelpBox(
            T("ภาพรวม: ", "Overall: ") + impact,
            impact.Contains("สูง") || impact.Contains("High")
                ? MessageType.Warning
                : MessageType.Info
        );

        EditorGUILayout.EndVertical();
    }

    private void DrawAAOCompatibilityPanel()
    {
        EditorGUILayout.BeginVertical(sectionStyle);

        DrawSectionHeader(
            T("Avatar Optimizer Compatibility", "Avatar Optimizer Compatibility")
        );

        if (!aaoDetected)
        {
            EditorGUILayout.HelpBox(
                T(
                    "ไม่พบ Avatar Optimizer ในโปรเจกต์/Avatar นี้",
                    "Avatar Optimizer was not detected for this project/avatar."
                ),
                MessageType.None
            );

            EditorGUILayout.EndVertical();
            return;
        }

        DrawCompatibilityStatus(
            "Trace and Optimize",
            aaoTraceAndOptimizeDetected,
            aaoTraceAndOptimizeDetected
                ? T("แนะนำใช้โหมด ให้ AAO จัดการ", "Let AAO Handle It is recommended")
                : T("ยังไม่พบ component บน Avatar", "Component not found on the avatar")
        );

        if (aaoHairToggleCount > 0)
        {
            EditorGUILayout.HelpBox(
                T(
                    $"mehigo มี {aaoHairToggleCount} Hair activation target(s) ที่ animate Active state — AAO Merge Skinned Mesh/Auto Merge อาจต้องรักษา enablement animation ให้ถูกต้อง ควรตรวจผลใน Play Mode หลัง Build",
                    $"mehigo has {aaoHairToggleCount} hair activation target(s) animating active state — AAO Merge Skinned Mesh/Auto Merge must preserve enablement correctly. Verify the result in Play Mode after build."
                ),
                MessageType.Warning
            );
        }

        if (aaoAnimatedMaterialPresetCount > 0)
        {
            EditorGUILayout.HelpBox(
                T(
                    $"พบ Material Preset {aaoAnimatedMaterialPresetCount} ชุด — หลีกเลี่ยง AAO Merge Material บน Renderer/slot ที่ mehigo สลับ Material เพราะ AAO Merge Material ไม่รองรับ animation replacing materials",
                    $"{aaoAnimatedMaterialPresetCount} Material Preset set(s) detected — avoid AAO Merge Material on renderers/slots where mehigo replaces materials, because AAO Merge Material does not support animations replacing materials."
                ),
                MessageType.Warning
            );
        }

        if (aaoMergeMaterialDetected)
        {
            EditorGUILayout.HelpBox(
                T(
                    "ตรวจพบ AAO Merge Material component บน Avatar — ตรวจให้แน่ใจว่าไม่ได้อยู่บน Renderer ที่ใช้ mehigo Material Preset",
                    "AAO Merge Material component detected — make sure it is not applied to a renderer used by mehigo Material Presets."
                ),
                MessageType.Warning
            );
        }

        if (aaoMergeSkinnedMeshDetected)
        {
            EditorGUILayout.HelpBox(
                T(
                    "ตรวจพบ AAO Merge Skinned Mesh component — ถ้า Hair visibility ใช้ Active/Enable animation ให้ตรวจ Copy Enablement Animation / merge targets ของ AAO",
                    "AAO Merge Skinned Mesh component detected — if hair visibility uses Active/Enable animations, review AAO Copy Enablement Animation / merge targets."
                ),
                MessageType.Warning
            );
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCompatibilityStatus(
        string label,
        bool ok,
        string detail)
    {
        EditorGUILayout.BeginHorizontal(subtleBoxStyle);
        EditorGUILayout.LabelField(
            (ok ? "✓ " : "• ") + label,
            EditorStyles.boldLabel,
            GUILayout.Width(180)
        );

        EditorGUILayout.LabelField(
            detail,
            EditorStyles.wordWrappedMiniLabel
        );
        EditorGUILayout.EndHorizontal();
    }


    private void DrawOptimizationModeCard(
        OptimizationMode mode,
        string title,
        string description,
        int estimatedLayers,
        bool recommended)
    {
        bool selected = optimizationMode == mode;

        EditorGUILayout.BeginVertical(
            selected ? sectionStyle : subtleBoxStyle,
            GUILayout.MinHeight(155)
        );

        EditorGUILayout.BeginHorizontal();

        bool toggleSelected = EditorGUILayout.Toggle(
            selected,
            GUILayout.Width(18)
        );

        EditorGUILayout.LabelField(
            title,
            EditorStyles.boldLabel
        );

        GUILayout.FlexibleSpace();

        if (recommended)
        {
            GUILayout.Label(
                T("แนะนำ", "Recommended"),
                badgeStyle
            );
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            description,
            EditorStyles.wordWrappedMiniLabel,
            GUILayout.MinHeight(60)
        );

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(
            $"Estimated Layers: {estimatedLayers}",
            EditorStyles.boldLabel
        );

        if (GUILayout.Button(
            selected
                ? T("กำลังใช้งาน", "Selected")
                : T("เลือกโหมดนี้", "Select"),
            GUILayout.Height(26)))
        {
            optimizationMode = mode;
            perfAnalyzed = false;
        }

        if (toggleSelected && !selected)
        {
            optimizationMode = mode;
            perfAnalyzed = false;
        }

        EditorGUILayout.EndVertical();
    }


    private void DrawAnimatorComparisonRow(
        string label,
        int standard,
        int optimized,
        bool bold = false)
    {
        GUIStyle style =
            bold
                ? EditorStyles.boldLabel
                : EditorStyles.label;

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(
            label,
            style,
            GUILayout.MinWidth(150)
        );

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(
            T($"มาตรฐาน: {standard}", $"Standard: {standard}"),
            style,
            GUILayout.Width(120)
        );

        EditorGUILayout.LabelField(
            T($"Direct: {optimized}", $"Direct: {optimized}"),
            style,
            GUILayout.Width(110)
        );

        EditorGUILayout.EndHorizontal();
    }

    private void DrawPerfRow(string label, string value)
    {
        EditorGUILayout.BeginHorizontal(subtleBoxStyle);
        EditorGUILayout.LabelField(label, GUILayout.Width(220));
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(value, EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
    }

    private void AnalyzePerformance()
    {
        DetectAAOCompatibility();

        perfTriangles = 0;
        perfRenderers = 0;
        perfSkinnedMeshes = 0;
        perfMaterialSlots = 0;
        perfBlendShapes = 0;
        perfAnimatorComponents = 0;

        HashSet<Renderer> uniqueRenderers = new HashSet<Renderer>();
        HashSet<Animator> uniqueAnimators = new HashSet<Animator>();

        foreach (HairEntry hair in hairs)
        {
            if (hair.hairObject == null)
                continue;

            foreach (Renderer renderer in hair.hairObject.GetComponentsInChildren<Renderer>(true))
            {
                if (!uniqueRenderers.Add(renderer))
                    continue;

                perfRenderers++;
                perfMaterialSlots += renderer.sharedMaterials != null
                    ? renderer.sharedMaterials.Length
                    : 0;

                SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;

                if (skinned != null)
                {
                    perfSkinnedMeshes++;

                    Mesh mesh = skinned.sharedMesh;
                    if (mesh != null)
                    {
                        perfTriangles += CountMeshTriangles(mesh);
                        perfBlendShapes += mesh.blendShapeCount;
                    }
                }
                else
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                        perfTriangles += CountMeshTriangles(filter.sharedMesh);
                }
            }

            foreach (Animator animator in hair.hairObject.GetComponentsInChildren<Animator>(true))
            {
                if (uniqueAnimators.Add(animator))
                    perfAnimatorComponents++;
            }
        }

        perfGeneratedParameters = 1 + CountBlendShapes() + CountMaterialParameters();

        int hairSelectorLayers = hairs.Count > 0 ? 1 : 0;
        int materialLayers = CountMaterialParameters();

        int blendShapeLayers = CountBlendShapes();

        perfGeneratedLayers =
            hairSelectorLayers +
            materialLayers +
            blendShapeLayers;

        perfAnalyzed = true;
        Repaint();
    }

    private int CountMeshTriangles(Mesh mesh)
    {
        if (mesh == null)
            return 0;

        int triangles = 0;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            try
            {
                triangles += (int)(mesh.GetIndexCount(i) / 3);
            }
            catch
            {
            }
        }

        return triangles;
    }

    private string GetPerformanceImpactLabel()
    {
        int score = 0;

        if (perfTriangles > 70000) score += 2;
        else if (perfTriangles > 35000) score += 1;

        if (perfSkinnedMeshes > 8) score += 2;
        else if (perfSkinnedMeshes > 4) score += 1;

        if (perfMaterialSlots > 24) score += 2;
        else if (perfMaterialSlots > 12) score += 1;

        if (perfGeneratedLayers > 20) score += 2;
        else if (perfGeneratedLayers > 10) score += 1;

        if (score >= 5)
            return T("ผลกระทบค่อนข้างสูง", "High impact");

        if (score >= 2)
            return T("ผลกระทบปานกลาง", "Moderate impact");

        return T("ผลกระทบค่อนข้างต่ำ", "Low impact");
    }

    // ---------------------------------------------------------------------
    // GENERATE TAB
    // ---------------------------------------------------------------------

    private void DrawGenerateTab()
    {
        EditorGUILayout.BeginVertical(sectionStyle);
        DrawSectionHeader(
            T("ตรวจสอบก่อนสร้าง", "Preflight"),
            T("เช็กสถานะสำคัญก่อน Generate", "Review key status before generating")
        );

        DrawPreflightRow(
            "Avatar",
            avatar != null ? avatar.name : T("ยังไม่ได้เลือก", "Not selected"),
            avatar != null
        );

        DrawPreflightRow(
            T("จำนวนทรงผม", "Hair Styles"),
            hairs.Count.ToString(),
            hairs.Count > 0
        );

        DrawPreflightRow(
            T("Performance", "Performance"),
            perfAnalyzed
                ? GetPerformanceImpactLabel()
                : T("ยังไม่วิเคราะห์", "Not analyzed"),
            perfAnalyzed
        );

        DrawPreflightRow(
            T("Compatibility Scan", "Compatibility Scan"),
            scanComplete
                ? (conflicts.Count == 0
                    ? T("ผ่าน", "Passed")
                    : T($"พบ {conflicts.Count} จุด", $"{conflicts.Count} issue(s)"))
                : T("ยังไม่สแกน", "Not scanned"),
            scanComplete && conflicts.Count == 0
        );

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(sectionStyle);

        bool valid = ValidateInput(false);
        GUI.enabled = valid;

        if (GUILayout.Button(
            T("สร้าง / อัปเดต mehigo Setup", "Generate / Update mehigo Setup"),
            GUILayout.Height(50)))
        {
            if (!scanComplete)
                ScanConflicts();

            Generate();
        }

        GUI.enabled = true;

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(T("สแกน Conflict", "Scan Conflicts"), GUILayout.Height(30)))
            ScanConflicts();

        if (GUILayout.Button(T("บันทึก Config", "Save Config"), GUILayout.Height(30)))
        {
            if (ValidateInput(false))
            {
                EnsureFolder(saveFolder);
                SaveProjectData();
                AssetDatabase.SaveAssets();
                Debug.Log("[mehigo] Config saved.");
            }
        }

        EditorGUILayout.EndHorizontal();

        if (!valid)
        {
            EditorGUILayout.HelpBox(
                T(
                    "ข้อมูลยังไม่ครบ กรุณาตรวจ Avatar Info และ Hair Styles",
                    "Required data is missing. Check Avatar Info and Hair Styles."
                ),
                MessageType.Warning
            );
        }

        if (optimizationMode == OptimizationMode.LetAAOHandleIt &&
            !aaoTraceAndOptimizeDetected)
        {
            EditorGUILayout.HelpBox(
                T(
                    "เลือก 'ให้ AAO จัดการ' แต่ยังไม่พบ Trace and Optimize บน Avatar — mehigo จะยังสร้าง Standard Animator ได้ แต่จะไม่มี AAO Animator optimization ต่อจนกว่าจะเพิ่ม Trace and Optimize",
                    "'Let AAO Handle It' is selected but Trace and Optimize was not found on the avatar — mehigo will still build the Standard Animator, but AAO Animator optimization will not run until Trace and Optimize is added."
                ),
                MessageType.Warning
            );
        }

        if (conflicts.Count > 0)
        {
            EditorGUILayout.HelpBox(
                T(
                    "พบ Potential Conflict ควรตรวจหน้า Compatibility ก่อน Generate",
                    "Potential conflicts were found. Review Compatibility before generating."
                ),
                MessageType.Warning
            );
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPreflightRow(string label, string value, bool ok)
    {
        EditorGUILayout.BeginHorizontal(subtleBoxStyle);
        EditorGUILayout.LabelField(label, GUILayout.Width(180));
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(value, ok ? EditorStyles.boldLabel : EditorStyles.miniBoldLabel);
        EditorGUILayout.EndHorizontal();
    }

    // ---------------------------------------------------------------------
    // LOAD / SAVE CONFIG
    // ---------------------------------------------------------------------

    private void AutoDetectAvatarDescriptor(GameObject go)
    {
        if (go == null)
        {
            avatar = null;
            return;
        }

        VRCAvatarDescriptor descriptor =
            go.GetComponent<VRCAvatarDescriptor>();

        if (descriptor == null)
        {
            descriptor = go.GetComponentInParent<VRCAvatarDescriptor>(true);
        }

        if (descriptor == null)
        {
            descriptor = go.GetComponentInChildren<VRCAvatarDescriptor>(true);
        }

        avatar = descriptor;

        if (avatar != null)
        {
            Debug.Log(
                $"[mehigo] Auto-detected Avatar Descriptor: {avatar.name}"
            );
        }
        else
        {
            Debug.LogWarning(
                "[mehigo] No VRCAvatarDescriptor found in the selected object."
            );
        }

        Repaint();
    }

    private void SetAvatarFromObject(GameObject go)
    {
        AutoDetectAvatarDescriptor(go);

        if (avatar == null)
        {
            Debug.LogError(
                "[mehigo] Selected object has no VRCAvatarDescriptor."
            );
        }
    }

    private void LoadExistingSetup(GameObject go)
    {
        SetAvatarFromObject(go);

        if (avatar == null) return;

        string avatarGuid = GetAvatarAssetGuid(avatar.gameObject);
        MehigoHairProjectDataV4 data = FindProjectData(avatarGuid);

        if (data == null)
        {
            Debug.LogWarning(
                "[mehigo] No v4 config was found for this avatar. Use As Avatar, configure once, then Generate/Save."
            );
            return;
        }

        rootMenuName = data.rootMenuName;
        hairParameterName = data.hairParameterName;
        generatedRootName = data.generatedRootName;
        saveFolder = data.saveFolder;
        savedHairParameter = data.savedHairParameter;

        hairs.Clear();

        foreach (MehigoHairProjectDataV4.HairData hd in data.hairs)
        {
            HairEntry entry = new HairEntry
            {
                menuName = hd.menuName,
                iconMode = (IconMode)hd.iconMode,
                icon = hd.icon,
                hairObject = ResolveAvatarPath(hd.hairPath),
                preserveExistingAnimator = hd.preserveExistingAnimator,
                activationMode = (ActivationMode)hd.activationMode,
                activationTarget = ResolveAvatarPath(hd.activationTargetPath)
            };

            foreach (string path in hd.linkedObjectPaths)
                entry.linkedObjects.Add(ResolveAvatarPath(path));

            foreach (MehigoHairProjectDataV4.BlendShapeData bd in hd.blendShapes)
            {
                GameObject rendererObject = ResolveAvatarPath(bd.rendererPath);

                entry.blendShapes.Add(new BlendShapeOption
                {
                    menuName = bd.menuName,
                    iconMode = (IconMode)bd.iconMode,
                    icon = bd.icon,
                    renderer = rendererObject != null
                        ? rendererObject.GetComponent<SkinnedMeshRenderer>()
                        : null,
                    blendShapeName = bd.blendShapeName,
                    controlMode = (BlendShapeControlMode)bd.controlMode,
                    onValue = bd.onValue,
                    saved = bd.saved
                });
            }


            foreach (MehigoHairProjectDataV4.MaterialPresetData mpd in hd.materialPresets)
            {
                MaterialPreset mp = new MaterialPreset
                {
                    menuName = mpd.menuName,
                    icon = mpd.icon
                };

                foreach (MehigoHairProjectDataV4.MaterialSlotData sd in mpd.slots)
                {
                    GameObject rendererObject = ResolveAvatarPath(sd.rendererPath);

                    mp.slots.Add(new MaterialSlotEntry
                    {
                        renderer = rendererObject != null
                            ? rendererObject.GetComponent<Renderer>()
                            : null,
                        materialIndex = sd.materialIndex,
                        material = sd.material
                    });
                }

                entry.materialPresets.Add(mp);
            }

            hairs.Add(entry);
        }

        conflicts.Clear();
        scanComplete = false;
        selectedTab = 1;
        Repaint();

        Debug.Log(
            $"[mehigo] Loaded v4 setup: {hairs.Count} hair styles, {CountBlendShapes()} BlendShape buttons."
        );
    }

    private MehigoHairProjectDataV4 SaveProjectData()
    {
        string avatarGuid = GetAvatarAssetGuid(avatar.gameObject);
        string configFolder = saveFolder + "/Config";

        EnsureFolder(configFolder);

        string key = string.IsNullOrWhiteSpace(avatarGuid)
            ? SanitizeFileName(avatar.name)
            : avatarGuid;

        string path = $"{configFolder}/mehigo_v4_{key}.asset";

        MehigoHairProjectDataV4 data =
            AssetDatabase.LoadAssetAtPath<MehigoHairProjectDataV4>(path);

        if (data == null)
        {
            data = CreateInstance<MehigoHairProjectDataV4>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.avatarAssetGuid = avatarGuid;
        data.rootMenuName = rootMenuName;
        data.hairParameterName = hairParameterName;
        data.generatedRootName = generatedRootName;
        data.saveFolder = saveFolder;
        data.savedHairParameter = savedHairParameter;
        data.hairs.Clear();

        foreach (HairEntry h in hairs)
        {
            MehigoHairProjectDataV4.HairData hd =
                new MehigoHairProjectDataV4.HairData
                {
                    menuName = h.menuName,
                    iconMode = (MehigoHairProjectDataV4.IconMode)h.iconMode,
                    icon = h.icon,
                    hairPath = GetAvatarPath(h.hairObject),
                    preserveExistingAnimator = h.preserveExistingAnimator,
                    activationMode = (MehigoHairProjectDataV4.ActivationMode)h.activationMode,
                    activationTargetPath = GetAvatarPath(h.activationTarget)
                };

            foreach (GameObject linked in h.linkedObjects)
                hd.linkedObjectPaths.Add(GetAvatarPath(linked));

            foreach (BlendShapeOption bs in h.blendShapes)
            {
                hd.blendShapes.Add(
                    new MehigoHairProjectDataV4.BlendShapeData
                    {
                        menuName = bs.menuName,
                        iconMode = (MehigoHairProjectDataV4.IconMode)bs.iconMode,
                        icon = bs.icon,
                        rendererPath = GetAvatarPath(
                            bs.renderer != null ? bs.renderer.gameObject : null
                        ),
                        blendShapeName = bs.blendShapeName,
                        controlMode = (MehigoHairProjectDataV4.BlendShapeControlMode)bs.controlMode,
                        onValue = bs.onValue,
                        saved = bs.saved
                    }
                );
            }


            foreach (MaterialPreset mp in h.materialPresets)
            {
                MehigoHairProjectDataV4.MaterialPresetData mpd =
                    new MehigoHairProjectDataV4.MaterialPresetData
                    {
                        menuName = mp.menuName,
                        icon = mp.icon
                    };

                foreach (MaterialSlotEntry slot in mp.slots)
                {
                    mpd.slots.Add(new MehigoHairProjectDataV4.MaterialSlotData
                    {
                        rendererPath = GetAvatarPath(
                            slot.renderer != null ? slot.renderer.gameObject : null
                        ),
                        materialIndex = slot.materialIndex,
                        material = slot.material
                    });
                }

                hd.materialPresets.Add(mpd);
            }

            data.hairs.Add(hd);
        }

        EditorUtility.SetDirty(data);
        return data;
    }

    private MehigoHairProjectDataV4 FindProjectData(string avatarGuid)
    {
        string[] guids = AssetDatabase.FindAssets("t:MehigoHairProjectDataV4");

        MehigoHairProjectDataV4 fallback = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            MehigoHairProjectDataV4 data =
                AssetDatabase.LoadAssetAtPath<MehigoHairProjectDataV4>(path);

            if (data == null) continue;

            if (!string.IsNullOrWhiteSpace(avatarGuid) &&
                data.avatarAssetGuid == avatarGuid)
            {
                return data;
            }

            if (fallback == null &&
                data.generatedRootName == generatedRootName)
            {
                fallback = data;
            }
        }

        return fallback;
    }

    private string GetAvatarAssetGuid(GameObject avatarObject)
    {
        GameObject source = avatarObject;

        if (PrefabUtility.IsPartOfPrefabInstance(avatarObject))
        {
            GameObject corresponding =
                PrefabUtility.GetCorrespondingObjectFromSource(avatarObject);

            if (corresponding != null)
                source = corresponding;
        }

        string path = AssetDatabase.GetAssetPath(source);

        if (string.IsNullOrWhiteSpace(path))
        {
            GameObject nearest =
                PrefabUtility.GetNearestPrefabInstanceRoot(avatarObject);

            if (nearest != null)
            {
                string prefabPath =
                    PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(nearest);

                if (!string.IsNullOrWhiteSpace(prefabPath))
                    path = prefabPath;
            }
        }

        return string.IsNullOrWhiteSpace(path)
            ? ""
            : AssetDatabase.AssetPathToGUID(path);
    }

    private string GetAvatarPath(GameObject go)
    {
        if (go == null || avatar == null) return "";

        return AnimationUtility.CalculateTransformPath(
            go.transform,
            avatar.transform
        );
    }

    private GameObject ResolveAvatarPath(string path)
    {
        if (avatar == null) return null;

        if (string.IsNullOrEmpty(path))
            return avatar.gameObject;

        Transform found = avatar.transform.Find(path);
        return found != null ? found.gameObject : null;
    }

    // ---------------------------------------------------------------------
    // GENERATION
    // ---------------------------------------------------------------------

    private void Generate()
    {
        if (!ValidateInput(true))
            return;

        Type mergeAnimatorType =
            FindType("nadena.dev.modular_avatar.core.ModularAvatarMergeAnimator");

        Type parametersType =
            FindType("nadena.dev.modular_avatar.core.ModularAvatarParameters");

        Type menuInstallerType =
            FindType("nadena.dev.modular_avatar.core.ModularAvatarMenuInstaller");

        if (mergeAnimatorType == null ||
            parametersType == null ||
            menuInstallerType == null)
        {
            EditorUtility.DisplayDialog(
                "Modular Avatar not found",
                "Install Modular Avatar before using mehigo Hair Manager.",
                "OK"
            );
            return;
        }

        EnsureFolder(saveFolder);

        AnimatorController controller = CreateAnimatorController();
        VRCExpressionsMenu installerMenu = CreateInstallerMenu();

        GameObject maRoot = GetOrCreateMARoot();

        Component mergeAnimator = GetOrAddComponent(maRoot, mergeAnimatorType);
        Component parameters = GetOrAddComponent(maRoot, parametersType);
        Component menuInstaller = GetOrAddComponent(maRoot, menuInstallerType);

        ConfigureMergeAnimator(mergeAnimator, controller);
        ConfigureParameters(parameters);
        ConfigureMenuInstaller(menuInstaller, installerMenu);

        SaveProjectData();

        EditorUtility.SetDirty(maRoot);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = maRoot;

        Debug.Log(
            $"[mehigo] v4 updated: {hairs.Count} hair styles, " +
            $"{CountBlendShapes()} BlendShape buttons, {conflicts.Count} potential conflict(s)."
        );
    }

    private GameObject GetOrCreateMARoot()
    {
        Transform existing = avatar.transform.Find(generatedRootName);

        if (existing != null)
            return existing.gameObject;

        GameObject root = new GameObject(generatedRootName);

        Undo.RegisterCreatedObjectUndo(
            root,
            "Create mehigo Hair Selector"
        );

        root.transform.SetParent(avatar.transform, false);
        return root;
    }

    // ---------------------------------------------------------------------
    // ANIMATOR
    // ---------------------------------------------------------------------

    private AnimatorController CreateAnimatorController()
    {
        string path = $"{saveFolder}/mehigo_HairSelector.controller";
        string legacyPath = $"{saveFolder}/mehigo_HairSelector_v4.controller";

        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

        if (controller == null &&
            AssetDatabase.LoadAssetAtPath<AnimatorController>(legacyPath) != null)
        {
            string migrationError = AssetDatabase.MoveAsset(legacyPath, path);

            if (string.IsNullOrEmpty(migrationError))
            {
                controller =
                    AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            }
            else
            {
                Debug.LogWarning(
                    $"[mehigo] Could not rename legacy controller: {migrationError}"
                );
            }
        }

        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        while (controller.layers.Length > 0)
            controller.RemoveLayer(0);

        foreach (AnimatorControllerParameter p in controller.parameters.ToArray())
            controller.RemoveParameter(p);

        controller.AddParameter(
            hairParameterName,
            AnimatorControllerParameterType.Int
        );

        // Internal-only parameter used by Optimized Direct BlendTrees.
        // It is not exposed through VRChat Expression Parameters.
        AnimatorControllerParameter internalOne =
            new AnimatorControllerParameter
            {
                name = "mehigo_InternalOne",
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 1f
            };

        controller.AddParameter(internalOne);

        for (int h = 0; h < hairs.Count; h++)
        {
            for (int b = 0; b < hairs[h].blendShapes.Count; b++)
            {
                controller.AddParameter(
                    GetBlendParameterName(h, b),
                    hairs[h].blendShapes[b].controlMode == BlendShapeControlMode.RadialPuppet
                        ? AnimatorControllerParameterType.Float
                        : AnimatorControllerParameterType.Bool
                );
            }

            if (hairs[h].materialPresets.Count > 1)
            {
                controller.AddParameter(
                    GetMaterialParameterName(h),
                    AnimatorControllerParameterType.Int
                );
            }
        }

        CreateHairLayer(controller);

        // Stable policy: always generate the proven Standard BlendShape layout.
        // If AAO is selected, AAO may optimize the stable controller later at build time.
        CreateBlendShapeLayers(controller);

        CreateMaterialPresetLayers(controller);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private void CreateHairLayer(AnimatorController controller)
    {
        AnimatorStateMachine sm = new AnimatorStateMachine
        {
            name = "Hair Selector"
        };

        AssetDatabase.AddObjectToAsset(sm, controller);

        AnimatorControllerLayer layer = new AnimatorControllerLayer
        {
            name = "Hair Selector",
            defaultWeight = 1f,
            stateMachine = sm
        };

        controller.AddLayer(layer);

        for (int i = 0; i < hairs.Count; i++)
        {
            AnimationClip clip = CreateHairAnimation(i);

            AnimatorState state = sm.AddState($"Hair {i}");
            state.motion = clip;
            state.writeDefaultValues = false;

            AnimatorStateTransition transition =
                sm.AddAnyStateTransition(state);

            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;

            transition.AddCondition(
                AnimatorConditionMode.Equals,
                i,
                hairParameterName
            );

            if (i == 0)
                sm.defaultState = state;
        }

        EditorUtility.SetDirty(sm);
    }

    private void CreateBlendShapeLayers(AnimatorController controller)
    {
        for (int hairIndex = 0; hairIndex < hairs.Count; hairIndex++)
        {
            HairEntry hair = hairs[hairIndex];

            for (int bsIndex = 0; bsIndex < hair.blendShapes.Count; bsIndex++)
            {
                BlendShapeOption bs = hair.blendShapes[bsIndex];
                string param = GetBlendParameterName(hairIndex, bsIndex);

                AnimatorStateMachine sm = new AnimatorStateMachine
                {
                    name = $"BS {hairIndex}-{bsIndex}"
                };

                AssetDatabase.AddObjectToAsset(sm, controller);

                AnimatorControllerLayer layer = new AnimatorControllerLayer
                {
                    name = $"Hair {hairIndex} - {SafeName(bs.menuName, "BlendShape")}",
                    defaultWeight = 1f,
                    stateMachine = sm
                };

                controller.AddLayer(layer);

                AnimatorState off = sm.AddState("OFF");
                off.motion = CreateBlendShapeAnimation(hairIndex, bsIndex, false);
                off.writeDefaultValues = false;
                sm.defaultState = off;

                if (bs.controlMode == BlendShapeControlMode.Toggle)
                {
                    AnimatorState on = sm.AddState("ON");
                    on.motion = CreateBlendShapeAnimation(hairIndex, bsIndex, true);
                    on.writeDefaultValues = false;

                    AnimatorStateTransition toOn = sm.AddAnyStateTransition(on);
                    toOn.hasExitTime = false;
                    toOn.duration = 0f;
                    toOn.canTransitionToSelf = false;
                    toOn.AddCondition(AnimatorConditionMode.Equals, hairIndex, hairParameterName);
                    toOn.AddCondition(AnimatorConditionMode.If, 0, param);

                    AnimatorStateTransition paramOff = on.AddTransition(off);
                    paramOff.hasExitTime = false;
                    paramOff.duration = 0f;
                    paramOff.AddCondition(AnimatorConditionMode.IfNot, 0, param);

                    AnimatorStateTransition wrongHair = on.AddTransition(off);
                    wrongHair.hasExitTime = false;
                    wrongHair.duration = 0f;
                    wrongHair.AddCondition(
                        AnimatorConditionMode.NotEqual,
                        hairIndex,
                        hairParameterName
                    );
                }
                else
                {
                    BlendTree tree = new BlendTree
                    {
                        name = $"Radial {hairIndex}-{bsIndex}",
                        blendType = BlendTreeType.Simple1D,
                        blendParameter = param,
                        useAutomaticThresholds = false
                    };

                    AssetDatabase.AddObjectToAsset(tree, controller);

                    AnimationClip minClip = CreateBlendShapeAnimation(hairIndex, bsIndex, false);
                    AnimationClip maxClip = CreateBlendShapeAnimation(hairIndex, bsIndex, true);

                    tree.AddChild(minClip, 0f);
                    tree.AddChild(maxClip, 1f);

                    AnimatorState radial = sm.AddState("RADIAL");
                    radial.motion = tree;
                    radial.writeDefaultValues = false;

                    AnimatorStateTransition toRadial = off.AddTransition(radial);
                    toRadial.hasExitTime = false;
                    toRadial.duration = 0f;
                    toRadial.AddCondition(
                        AnimatorConditionMode.Equals,
                        hairIndex,
                        hairParameterName
                    );

                    AnimatorStateTransition wrongHair = radial.AddTransition(off);
                    wrongHair.hasExitTime = false;
                    wrongHair.duration = 0f;
                    wrongHair.AddCondition(
                        AnimatorConditionMode.NotEqual,
                        hairIndex,
                        hairParameterName
                    );

                    EditorUtility.SetDirty(tree);
                }

                EditorUtility.SetDirty(sm);
            }
        }
    }



    private void CreateOptimizedBlendShapeLayers(AnimatorController controller)
    {
        for (int hairIndex = 0; hairIndex < hairs.Count; hairIndex++)
        {
            HairEntry hair = hairs[hairIndex];

            if (hair.blendShapes.Count == 0)
                continue;

            AnimatorStateMachine sm = new AnimatorStateMachine
            {
                name = $"Hair {hairIndex} Optimized BlendShapes"
            };

            AssetDatabase.AddObjectToAsset(sm, controller);

            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = $"Hair {hairIndex} - BlendShapes (Optimized)",
                defaultWeight = 1f,
                stateMachine = sm
            };

            controller.AddLayer(layer);

            // OFF always resets the BlendShapes owned by this hairstyle.
            AnimatorState off = sm.AddState("OFF");
            off.motion = CreateOptimizedBlendShapeResetClip(hairIndex);
            off.writeDefaultValues = false;
            sm.defaultState = off;

            // Canonical Direct BlendTree layout:
            //
            // Base Reset clip -> mehigo_InternalOne = 1
            // BS0 target clip -> mehigo_Hx_BS0 = 0..1
            // BS1 target clip -> mehigo_Hx_BS1 = 0..1
            //
            // Each target clip writes ONLY its own BlendShape property.
            // The base clip writes 0 to every BlendShape controlled by this hair.
            BlendTree direct = new BlendTree
            {
                name = $"Hair {hairIndex} BlendShapes Direct",
                blendType = BlendTreeType.Direct,
                useAutomaticThresholds = false
            };

            AssetDatabase.AddObjectToAsset(direct, controller);

            // Direct BlendTrees used for independent BlendShape weights must not
            // normalize their children. Unity 2022 does not expose this as a
            // public BlendTree property in every version, so set the serialized
            // field when available.
            SerializedObject directSerialized = new SerializedObject(direct);
            SerializedProperty normalizedProperty =
                directSerialized.FindProperty("m_NormalizedBlendValues");

            if (normalizedProperty != null)
            {
                normalizedProperty.boolValue = false;
                directSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning(
                    "[mehigo] Could not disable Normalize Blend Values on Direct BlendTree. " +
                    "Use Standard or Let AAO Handle It if the optimized controls behave incorrectly."
                );
            }

            AnimationClip resetClip =
                CreateOptimizedBlendShapeResetClip(hairIndex);

            direct.AddChild(resetClip);

            ChildMotion[] children = direct.children;
            int resetIndex = children.Length - 1;
            children[resetIndex].directBlendParameter = "mehigo_InternalOne";
            children[resetIndex].timeScale = 1f;
            direct.children = children;

            for (int bsIndex = 0; bsIndex < hair.blendShapes.Count; bsIndex++)
            {
                BlendShapeOption bs = hair.blendShapes[bsIndex];

                if (string.IsNullOrWhiteSpace(bs.blendShapeName) ||
                    !IsValidOptimizedBlendShapeRenderer(hairIndex, bs))
                    continue;

                string param =
                    GetBlendParameterName(hairIndex, bsIndex);

                AnimationClip targetClip =
                    CreateOptimizedSingleBlendShapeClip(
                        hairIndex,
                        bsIndex,
                        true
                    );

                direct.AddChild(targetClip);

                children = direct.children;
                int targetIndex = children.Length - 1;

                // Direct mode maps this parameter directly to this child's weight.
                // 0 => Base Reset wins
                // 0.5 => 50% of the configured BlendShape value
                // 1 => full configured BlendShape value
                children[targetIndex].directBlendParameter = param;
                children[targetIndex].timeScale = 1f;
                direct.children = children;
            }

            AnimatorState active = sm.AddState("ACTIVE");
            active.motion = direct;
            active.writeDefaultValues = false;

            AnimatorStateTransition toActive =
                off.AddTransition(active);

            toActive.hasExitTime = false;
            toActive.duration = 0f;
            toActive.hasFixedDuration = true;
            toActive.AddCondition(
                AnimatorConditionMode.Equals,
                hairIndex,
                hairParameterName
            );

            AnimatorStateTransition toOff =
                active.AddTransition(off);

            toOff.hasExitTime = false;
            toOff.duration = 0f;
            toOff.hasFixedDuration = true;
            toOff.AddCondition(
                AnimatorConditionMode.NotEqual,
                hairIndex,
                hairParameterName
            );

            EditorUtility.SetDirty(direct);
            EditorUtility.SetDirty(sm);
        }
    }

    private void ClearGeneratedAnimationClip(AnimationClip clip)
    {
        if (clip == null)
            return;

        // Important for regenerated optimized clips:
        // GetOrCreateClip reuses the existing .anim asset. Without clearing
        // it first, bindings from an older renderer / BlendShape selection
        // can remain in the clip and animate unrelated meshes (for example
        // a face SkinnedMeshRenderer).
        clip.ClearCurves();

        EditorCurveBinding[] objectBindings =
            AnimationUtility.GetObjectReferenceCurveBindings(clip);

        foreach (EditorCurveBinding binding in objectBindings)
        {
            AnimationUtility.SetObjectReferenceCurve(
                clip,
                binding,
                null
            );
        }

        AnimationUtility.SetAnimationEvents(
            clip,
            Array.Empty<AnimationEvent>()
        );
    }

    private bool IsValidOptimizedBlendShapeRenderer(
        int hairIndex,
        BlendShapeOption bs)
    {
        if (bs == null ||
            bs.renderer == null ||
            avatar == null ||
            hairIndex < 0 ||
            hairIndex >= hairs.Count)
            return false;

        HairEntry hair = hairs[hairIndex];

        if (hair.hairObject == null)
            return false;

        Transform rendererTransform = bs.renderer.transform;

        // The renderer must belong to this hairstyle. This prevents an
        // accidentally assigned face/body renderer from being written into
        // the optimized hair BlendTree.
        if (rendererTransform != hair.hairObject.transform &&
            !rendererTransform.IsChildOf(hair.hairObject.transform))
        {
            Debug.LogWarning(
                $"[mehigo] Optimized BlendShape skipped: '{bs.renderer.name}' " +
                $"is not under Hair '{hair.hairObject.name}'."
            );
            return false;
        }

        // It also must resolve beneath the current avatar root.
        if (rendererTransform != avatar.transform &&
            !rendererTransform.IsChildOf(avatar.transform))
        {
            Debug.LogWarning(
                $"[mehigo] Optimized BlendShape skipped: '{bs.renderer.name}' " +
                "is not under the selected Avatar."
            );
            return false;
        }

        Mesh mesh = bs.renderer.sharedMesh;

        if (mesh == null ||
            mesh.GetBlendShapeIndex(bs.blendShapeName) < 0)
        {
            Debug.LogWarning(
                $"[mehigo] Optimized BlendShape skipped: " +
                $"'{bs.blendShapeName}' was not found on '{bs.renderer.name}'."
            );
            return false;
        }

        return true;
    }

    private AnimationClip CreateOptimizedBlendShapeResetClip(int hairIndex)
    {
        HairEntry hair = hairs[hairIndex];

        AnimationClip clip = GetOrCreateClip(
            $"{saveFolder}/Hair_{hairIndex}_BS_OPT_RESET.anim",
            $"Hair_{hairIndex}_BS_OPT_RESET"
        );

        ClearGeneratedAnimationClip(clip);

        foreach (BlendShapeOption bs in hair.blendShapes)
        {
            if (string.IsNullOrWhiteSpace(bs.blendShapeName) ||
                !IsValidOptimizedBlendShapeRenderer(hairIndex, bs))
                continue;

            string rendererPath =
                AnimationUtility.CalculateTransformPath(
                    bs.renderer.transform,
                    avatar.transform
                );

            EditorCurveBinding binding =
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + bs.blendShapeName
                );

            AnimationUtility.SetEditorCurve(
                clip,
                binding,
                AnimationCurve.Constant(0f, 0f, 0f)
            );
        }

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private AnimationClip CreateOptimizedSingleBlendShapeClip(
        int hairIndex,
        int blendShapeIndex,
        bool enabled)
    {
        HairEntry hair = hairs[hairIndex];

        if (blendShapeIndex < 0 ||
            blendShapeIndex >= hair.blendShapes.Count)
            return null;

        BlendShapeOption bs = hair.blendShapes[blendShapeIndex];

        string suffix = enabled ? "ON" : "OFF";

        AnimationClip clip = GetOrCreateClip(
            $"{saveFolder}/Hair_{hairIndex}_BS_OPT_{blendShapeIndex}_{suffix}.anim",
            $"Hair_{hairIndex}_BS_OPT_{blendShapeIndex}_{suffix}"
        );

        ClearGeneratedAnimationClip(clip);

        if (string.IsNullOrWhiteSpace(bs.blendShapeName) ||
            !IsValidOptimizedBlendShapeRenderer(hairIndex, bs))
        {
            EditorUtility.SetDirty(clip);
            return clip;
        }

        string rendererPath =
            AnimationUtility.CalculateTransformPath(
                bs.renderer.transform,
                avatar.transform
            );

        EditorCurveBinding binding =
            EditorCurveBinding.FloatCurve(
                rendererPath,
                typeof(SkinnedMeshRenderer),
                "blendShape." + bs.blendShapeName
            );

        float target = enabled ? bs.onValue : 0f;

        AnimationUtility.SetEditorCurve(
            clip,
            binding,
            AnimationCurve.Constant(0f, 0f, target)
        );

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private AnimationClip CreateEmptyClip(string path, string clipName)
    {
        return GetOrCreateClip(path, clipName);
    }

    private void CreateMaterialPresetLayers(AnimatorController controller)
    {
        for (int hairIndex = 0; hairIndex < hairs.Count; hairIndex++)
        {
            HairEntry hair = hairs[hairIndex];

            if (hair.materialPresets.Count <= 1)
                continue;

            string param = GetMaterialParameterName(hairIndex);

            AnimatorStateMachine sm = new AnimatorStateMachine
            {
                name = $"Hair {hairIndex} Material"
            };

            AssetDatabase.AddObjectToAsset(sm, controller);

            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = $"Hair {hairIndex} - Material Preset",
                defaultWeight = 1f,
                stateMachine = sm
            };

            controller.AddLayer(layer);

            for (int presetIndex = 0; presetIndex < hair.materialPresets.Count; presetIndex++)
            {
                AnimatorState state = sm.AddState(
                    presetIndex == 0
                        ? "Default"
                        : SafeName(hair.materialPresets[presetIndex].menuName, $"Preset {presetIndex}")
                );

                state.motion = CreateMaterialPresetAnimation(hairIndex, presetIndex);
                state.writeDefaultValues = false;

                AnimatorStateTransition transition = sm.AddAnyStateTransition(state);
                transition.hasExitTime = false;
                transition.duration = 0f;
                transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.Equals, hairIndex, hairParameterName);
                transition.AddCondition(AnimatorConditionMode.Equals, presetIndex, param);

                if (presetIndex == 0)
                    sm.defaultState = state;
            }

            EditorUtility.SetDirty(sm);
        }
    }

    private AnimationClip CreateMaterialPresetAnimation(int hairIndex, int presetIndex)
    {
        HairEntry hair = hairs[hairIndex];
        MaterialPreset preset = hair.materialPresets[presetIndex];

        AnimationClip clip = GetOrCreateClip(
            $"{saveFolder}/Hair_{hairIndex}_Material_{presetIndex}.anim",
            $"Hair_{hairIndex}_Material_{presetIndex}"
        );

        foreach (MaterialSlotEntry slot in preset.slots)
        {
            if (slot.renderer == null)
                continue;

            string rendererPath = AnimationUtility.CalculateTransformPath(
                slot.renderer.transform,
                avatar.transform
            );

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
                rendererPath,
                typeof(Renderer),
                $"m_Materials.Array.data[{slot.materialIndex}]"
            );

            AnimationUtility.SetObjectReferenceCurve(
                clip,
                binding,
                new[]
                {
                    new ObjectReferenceKeyframe
                    {
                        time = 0f,
                        value = slot.material
                    }
                }
            );
        }

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private AnimationClip CreateHairAnimation(int selectedIndex)
    {
        string path = $"{saveFolder}/Hair_{selectedIndex}.anim";

        AnimationClip clip =
            GetOrCreateClip(path, $"Hair_{selectedIndex}");

        // Animate only explicit activation targets.
        for (int i = 0; i < hairs.Count; i++)
        {
            GameObject target = GetActivationTarget(hairs[i]);

            if (target != null)
                SetActiveCurve(clip, target, i == selectedIndex);
        }

        // Linked objects remain explicit opt-in.
        HashSet<GameObject> allLinked = new HashSet<GameObject>();

        foreach (HairEntry h in hairs)
        {
            foreach (GameObject linked in h.linkedObjects)
            {
                if (linked != null)
                    allLinked.Add(linked);
            }
        }

        foreach (GameObject linked in allLinked)
        {
            bool active =
                hairs[selectedIndex].linkedObjects.Contains(linked);

            SetActiveCurve(clip, linked, active);
        }

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private GameObject GetActivationTarget(HairEntry hair)
    {
        switch (hair.activationMode)
        {
            case ActivationMode.ControlHairRoot:
                return hair.hairObject;

            case ActivationMode.ControlExistingWrapper:
                return hair.activationTarget;

            case ActivationMode.DoNotControlObject:
            default:
                return null;
        }
    }

    private AnimationClip CreateBlendShapeAnimation(
        int hairIndex,
        int bsIndex,
        bool enabled)
    {
        BlendShapeOption bs = hairs[hairIndex].blendShapes[bsIndex];

        string path =
            $"{saveFolder}/BS_{hairIndex}_{bsIndex}_{(enabled ? "ON" : "OFF")}.anim";

        AnimationClip clip =
            GetOrCreateClip(
                path,
                $"BS_{hairIndex}_{bsIndex}_{(enabled ? "ON" : "OFF")}"
            );

        string rendererPath =
            AnimationUtility.CalculateTransformPath(
                bs.renderer.transform,
                avatar.transform
            );

        EditorCurveBinding binding =
            EditorCurveBinding.FloatCurve(
                rendererPath,
                typeof(SkinnedMeshRenderer),
                "blendShape." + bs.blendShapeName
            );

        AnimationUtility.SetEditorCurve(
            clip,
            binding,
            AnimationCurve.Constant(
                0f,
                0f,
                enabled ? bs.onValue : 0f
            )
        );

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private AnimationClip GetOrCreateClip(
        string path,
        string clipName)
    {
        AnimationClip clip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        if (clip == null)
        {
            clip = new AnimationClip { name = clipName };
            AssetDatabase.CreateAsset(clip, path);
        }
        else
        {
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
        }

        return clip;
    }

    private void SetActiveCurve(
        AnimationClip clip,
        GameObject go,
        bool active)
    {
        if (go == null) return;

        string objectPath =
            AnimationUtility.CalculateTransformPath(
                go.transform,
                avatar.transform
            );

        EditorCurveBinding binding =
            EditorCurveBinding.FloatCurve(
                objectPath,
                typeof(GameObject),
                "m_IsActive"
            );

        AnimationUtility.SetEditorCurve(
            clip,
            binding,
            AnimationCurve.Constant(
                0f,
                0f,
                active ? 1f : 0f
            )
        );
    }

    // ---------------------------------------------------------------------
    // MENU
    // ---------------------------------------------------------------------

    private VRCExpressionsMenu CreateInstallerMenu()
    {
        VRCExpressionsMenu hairRoot = CreateHairRootMenu();

        string path = $"{saveFolder}/mehigo_InstallerMenu_v4.asset";

        VRCExpressionsMenu installer =
            GetOrCreateMenuAsset(
                path,
                "mehigo Hair Installer"
            );

        installer.controls.Clear();

        installer.controls.Add(
            new VRCExpressionsMenu.Control
            {
                name = rootMenuName,
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = hairRoot
            }
        );

        EditorUtility.SetDirty(installer);
        return installer;
    }

    private VRCExpressionsMenu CreateHairRootMenu()
    {
        List<VRCExpressionsMenu.Control> controls =
            new List<VRCExpressionsMenu.Control>();

        for (int i = 0; i < hairs.Count; i++)
        {
            HairEntry hair = hairs[i];

            if (hair.blendShapes.Count == 0)
            {
                controls.Add(
                    new VRCExpressionsMenu.Control
                    {
                        name = SafeName(hair.menuName, $"Hair {i}"),
                        icon = hair.icon,
                        type = VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameter = new VRCExpressionsMenu.Control.Parameter
                        {
                            name = hairParameterName
                        },
                        value = i
                    }
                );
            }
            else
            {
                controls.Add(
                    new VRCExpressionsMenu.Control
                    {
                        name = SafeName(hair.menuName, $"Hair {i}"),
                        icon = hair.icon,
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = CreateHairStyleSubMenu(i)
                    }
                );
            }
        }

        return BuildPagedMenu(
            $"{saveFolder}/HairRoot_v4",
            rootMenuName,
            controls
        );
    }

    private VRCExpressionsMenu CreateHairStyleSubMenu(int hairIndex)
    {
        HairEntry hair = hairs[hairIndex];

        List<VRCExpressionsMenu.Control> controls =
            new List<VRCExpressionsMenu.Control>();

        controls.Add(
            new VRCExpressionsMenu.Control
            {
                name = "Use " + SafeName(hair.menuName, $"Hair {hairIndex}"),
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter
                {
                    name = hairParameterName
                },
                value = hairIndex
            }
        );

        for (int j = 0; j < hair.blendShapes.Count; j++)
        {
            BlendShapeOption bs = hair.blendShapes[j];

            if (bs.controlMode == BlendShapeControlMode.RadialPuppet)
            {
                controls.Add(
                    new VRCExpressionsMenu.Control
                    {
                        name = SafeName(bs.menuName, $"BlendShape {j + 1}"),
                        icon = bs.icon,
                        type = VRCExpressionsMenu.Control.ControlType.RadialPuppet,
                        subParameters = new[]
                        {
                            new VRCExpressionsMenu.Control.Parameter
                            {
                                name = GetBlendParameterName(hairIndex, j)
                            }
                        }
                    }
                );
            }
            else
            {
                controls.Add(
                    new VRCExpressionsMenu.Control
                    {
                        name = SafeName(bs.menuName, $"BlendShape {j + 1}"),
                        icon = bs.icon,
                        type = VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameter = new VRCExpressionsMenu.Control.Parameter
                        {
                            name = GetBlendParameterName(hairIndex, j)
                        },
                        value = 1f
                    }
                );
            }
        }

        if (hair.materialPresets.Count > 1)
        {
            controls.Add(
                new VRCExpressionsMenu.Control
                {
                    name = T("สีผม", "Hair Color"),
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = CreateMaterialPresetSubMenu(hairIndex)
                }
            );
        }

        return BuildPagedMenu(
            $"{saveFolder}/Hair_{hairIndex}_Controls_v4",
            SafeName(hair.menuName, $"Hair {hairIndex}"),
            controls
        );
    }


    private VRCExpressionsMenu CreateMaterialPresetSubMenu(int hairIndex)
    {
        HairEntry hair = hairs[hairIndex];
        List<VRCExpressionsMenu.Control> controls =
            new List<VRCExpressionsMenu.Control>();

        string param = GetMaterialParameterName(hairIndex);

        for (int i = 0; i < hair.materialPresets.Count; i++)
        {
            MaterialPreset preset = hair.materialPresets[i];

            controls.Add(
                new VRCExpressionsMenu.Control
                {
                    name = i == 0
                        ? T("ค่าเริ่มต้น", "Default")
                        : SafeName(preset.menuName, $"Preset {i}"),
                    icon = preset.icon,
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameter = new VRCExpressionsMenu.Control.Parameter
                    {
                        name = param
                    },
                    value = i
                }
            );
        }

        return BuildPagedMenu(
            $"{saveFolder}/Hair_{hairIndex}_MaterialMenu",
            T("สีผม", "Hair Color"),
            controls
        );
    }

    private VRCExpressionsMenu BuildPagedMenu(
        string pathPrefix,
        string title,
        List<VRCExpressionsMenu.Control> controls)
    {
        const int max = 8;

        if (controls.Count <= max)
        {
            VRCExpressionsMenu menu =
                GetOrCreateMenuAsset(pathPrefix + ".asset", title);

            menu.controls =
                new List<VRCExpressionsMenu.Control>(controls);

            EditorUtility.SetDirty(menu);
            return menu;
        }

        const int contentCount = 7;

        int pageCount =
            Mathf.CeilToInt(
                controls.Count / (float)contentCount
            );

        List<VRCExpressionsMenu> pages =
            new List<VRCExpressionsMenu>();

        for (int p = 0; p < pageCount; p++)
        {
            VRCExpressionsMenu page =
                GetOrCreateMenuAsset(
                    $"{pathPrefix}_Page_{p + 1}.asset",
                    $"{title} {p + 1}/{pageCount}"
                );

            page.controls.Clear();
            pages.Add(page);
        }

        for (int p = 0; p < pageCount; p++)
        {
            int start = p * contentCount;
            int end =
                Mathf.Min(
                    start + contentCount,
                    controls.Count
                );

            for (int i = start; i < end; i++)
                pages[p].controls.Add(controls[i]);

            pages[p].controls.Add(
                new VRCExpressionsMenu.Control
                {
                    name =
                        p == pageCount - 1
                            ? "< First Page"
                            : "Next >",
                    type =
                        VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = pages[(p + 1) % pageCount]
                }
            );

            EditorUtility.SetDirty(pages[p]);
        }

        return pages[0];
    }

    private VRCExpressionsMenu GetOrCreateMenuAsset(
        string path,
        string assetName)
    {
        VRCExpressionsMenu menu =
            AssetDatabase.LoadAssetAtPath<VRCExpressionsMenu>(path);

        if (menu == null)
        {
            menu = CreateInstance<VRCExpressionsMenu>();
            menu.name = assetName;
            menu.controls =
                new List<VRCExpressionsMenu.Control>();

            AssetDatabase.CreateAsset(menu, path);
        }
        else
        {
            menu.name = assetName;

            if (menu.controls == null)
                menu.controls =
                    new List<VRCExpressionsMenu.Control>();
        }

        return menu;
    }

    // ---------------------------------------------------------------------
    // MODULAR AVATAR
    // ---------------------------------------------------------------------

    private void ConfigureMergeAnimator(
        Component component,
        AnimatorController controller)
    {
        SerializedObject so = new SerializedObject(component);

        SetObjectReference(so, new[] { "animator" }, controller);
        SetEnumByName(so, new[] { "layerType" }, "FX");
        SetEnumByName(so, new[] { "pathMode" }, "Absolute");
        SetBool(so, new[] { "deleteAttachedAnimator" }, false);
        SetBool(so, new[] { "matchAvatarWriteDefaults" }, true);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private void ConfigureParameters(Component component)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty parametersProp =
            so.FindProperty("parameters");

        if (parametersProp == null ||
            !parametersProp.isArray)
        {
            Debug.LogError(
                "[mehigo] Could not configure MA Parameters."
            );
            return;
        }

        parametersProp.arraySize =
            1 + CountBlendShapes() + CountMaterialParameters();

        int index = 0;

        ConfigureMAParameter(
            parametersProp.GetArrayElementAtIndex(index++),
            hairParameterName,
            "Int",
            savedHairParameter
        );

        for (int h = 0; h < hairs.Count; h++)
        {
            for (int b = 0; b < hairs[h].blendShapes.Count; b++)
            {
                ConfigureMAParameter(
                    parametersProp.GetArrayElementAtIndex(index++),
                    GetBlendParameterName(h, b),
                    hairs[h].blendShapes[b].controlMode == BlendShapeControlMode.RadialPuppet
                        ? "Float"
                        : "Bool",
                    hairs[h].blendShapes[b].saved
                );
            }

            if (hairs[h].materialPresets.Count > 1)
            {
                ConfigureMAParameter(
                    parametersProp.GetArrayElementAtIndex(index++),
                    GetMaterialParameterName(h),
                    "Int",
                    true
                );
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private void ConfigureMAParameter(
        SerializedProperty element,
        string parameterName,
        string syncTypeName,
        bool saved)
    {
        SetRelativeString(
            element,
            "nameOrPrefix",
            parameterName
        );

        SetRelativeString(
            element,
            "remapTo",
            ""
        );

        SetRelativeBool(
            element,
            "internalParameter",
            false
        );

        SetRelativeBool(
            element,
            "isPrefix",
            false
        );

        SetRelativeBool(
            element,
            "localOnly",
            false
        );

        SetRelativeFloat(
            element,
            "defaultValue",
            0f
        );

        SetRelativeBool(
            element,
            "saved",
            saved
        );

        SetRelativeBool(
            element,
            "hasExplicitDefaultValue",
            true
        );

        SerializedProperty syncType =
            element.FindPropertyRelative("syncType");

        if (syncType != null &&
            syncType.propertyType ==
            SerializedPropertyType.Enum)
        {
            int enumIndex =
                Array.IndexOf(
                    syncType.enumNames,
                    syncTypeName
                );

            if (enumIndex >= 0)
                syncType.enumValueIndex = enumIndex;
        }
    }

    private void ConfigureMenuInstaller(
        Component component,
        VRCExpressionsMenu menu)
    {
        SerializedObject so =
            new SerializedObject(component);

        bool success =
            SetObjectReference(
                so,
                new[]
                {
                    "menuToAppend",
                    "menuToInstall",
                    "menu"
                },
                menu
            );

        if (!success)
        {
            Debug.LogError(
                "[mehigo] Could not assign MA Menu Installer menu."
            );
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }


    // ---------------------------------------------------------------------
    // ICONS / SCENE CAPTURE
    // ---------------------------------------------------------------------

    private void DrawIconSelector(
        string label,
        ref IconMode mode,
        ref Texture2D icon,
        string captureBaseName)
    {
        Texture2D capturedResult;
        if (MehigoSceneCapturePreviewWindow.TryConsumeCapture(captureBaseName, out capturedResult))
        {
            if (capturedResult != null)
                icon = capturedResult;
        }

        string[] modeLabels =
            language == EditorLanguage.Thai
                ? new[] { "ค่าเริ่มต้น", "เลือก Texture", "Capture จาก Scene" }
                : new[] { "Default", "Custom Texture", "Capture From Scene" };

        mode = (IconMode)EditorGUILayout.Popup(
            label,
            (int)mode,
            modeLabels
        );

        if (mode == IconMode.Default)
        {
            icon = null;

            EditorGUILayout.LabelField(
                T(
                    "ใช้ไอคอนค่าเริ่มต้นของ VRChat",
                    "Uses VRChat's default button appearance"
                ),
                EditorStyles.miniLabel
            );
        }
        else if (mode == IconMode.CustomTexture)
        {
            icon = (Texture2D)EditorGUILayout.ObjectField(
                T("Texture", "Texture"),
                icon,
                typeof(Texture2D),
                false
            );
        }
        else
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField(
                T("รูป Capture", "Captured Icon"),
                icon,
                typeof(Texture2D),
                false
            );

            if (GUILayout.Button(
                T("Preview / Capture", "Preview / Capture"),
                GUILayout.Width(120)))
            {
                MehigoSceneCapturePreviewWindow.Open(
                    captureBaseName,
                    saveFolder,
                    language == EditorLanguage.Thai
                );
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                T(
                    "ใช้มุมกล้องจาก Scene View ปัจจุบัน จัดมุมให้เรียบร้อยก่อนกด Capture",
                    "Captures the current Scene View camera. Frame the shot before pressing Capture."
                ),
                MessageType.None
            );
        }
    }

    // ---------------------------------------------------------------------
    // VALIDATION / HELPERS
    // ---------------------------------------------------------------------

    private bool ValidateInput(bool log)
    {
        if (avatar == null)
        {
            if (log)
                Debug.LogError(
                    "[mehigo] Avatar Descriptor is required."
                );

            return false;
        }

        if (hairs.Count == 0)
        {
            if (log)
                Debug.LogError(
                    "[mehigo] Add at least one hair."
                );

            return false;
        }

        if (string.IsNullOrWhiteSpace(saveFolder) ||
            !saveFolder.StartsWith("Assets"))
        {
            if (log)
                Debug.LogError(
                    "[mehigo] Save Folder must be inside Assets."
                );

            return false;
        }

        for (int i = 0; i < hairs.Count; i++)
        {
            HairEntry h = hairs[i];

            if (h.hairObject == null ||
                !IsUnderAvatar(h.hairObject))
            {
                if (log)
                    Debug.LogError(
                        $"[mehigo] Invalid Hair Object at Index {i}."
                    );

                return false;
            }

            if (h.activationMode ==
                ActivationMode.ControlExistingWrapper)
            {
                if (h.activationTarget == null ||
                    !IsUnderAvatar(h.activationTarget))
                {
                    if (log)
                        Debug.LogError(
                            $"[mehigo] Hair Index {i} requires a valid Existing Wrapper."
                        );

                    return false;
                }
            }

            foreach (GameObject linked in h.linkedObjects)
            {
                if (linked == null ||
                    !IsUnderAvatar(linked))
                {
                    if (log)
                        Debug.LogError(
                            $"[mehigo] Invalid Linked Object at Index {i}."
                        );

                    return false;
                }
            }

            foreach (BlendShapeOption bs in h.blendShapes)
            {
                if (bs.renderer == null ||
                    bs.renderer.sharedMesh == null ||
                    string.IsNullOrWhiteSpace(bs.blendShapeName) ||
                    !IsUnderAvatar(bs.renderer.gameObject) ||
                    bs.renderer.sharedMesh.GetBlendShapeIndex(
                        bs.blendShapeName
                    ) < 0)
                {
                    if (log)
                        Debug.LogError(
                            $"[mehigo] Invalid BlendShape entry at Hair Index {i}."
                        );

                    return false;
                }
            }
        }

        return true;
    }

    private bool IsUnderAvatar(GameObject go)
    {
        return go != null &&
               (
                   go.transform == avatar.transform ||
                   go.transform.IsChildOf(avatar.transform)
               );
    }

    private void Swap(int a, int b)
    {
        HairEntry temp = hairs[a];
        hairs[a] = hairs[b];
        hairs[b] = temp;
    }

    private int CountBlendShapes()
    {
        return hairs.Sum(
            h => h.blendShapes.Count
        );
    }

    private string GetBlendParameterName(
        int hairIndex,
        int bsIndex)
    {
        return $"mehigo_H{hairIndex}_BS{bsIndex}";
    }

    private string GetMaterialParameterName(int hairIndex)
    {
        return $"mehigo_H{hairIndex}_Mat";
    }

    private int CountMaterialParameters()
    {
        return hairs.Count(h => h.materialPresets.Count > 1);
    }

    private string SafeName(
        string value,
        string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }

    private string SanitizeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');

        return value.Replace(" ", "_");
    }

    private Type FindType(string fullName)
    {
        foreach (Assembly assembly in
                 AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = assembly.GetType(fullName);

            if (t != null)
                return t;
        }

        return null;
    }

    private Component GetOrAddComponent(
        GameObject go,
        Type type)
    {
        Component existing =
            go.GetComponent(type);

        return existing != null
            ? existing
            : Undo.AddComponent(go, type);
    }

    private bool SetObjectReference(
        SerializedObject so,
        string[] names,
        UnityEngine.Object value)
    {
        foreach (string name in names)
        {
            SerializedProperty prop =
                so.FindProperty(name);

            if (prop != null &&
                prop.propertyType ==
                SerializedPropertyType.ObjectReference)
            {
                prop.objectReferenceValue = value;
                return true;
            }
        }

        return false;
    }

    private bool SetEnumByName(
        SerializedObject so,
        string[] names,
        string enumName)
    {
        foreach (string name in names)
        {
            SerializedProperty prop =
                so.FindProperty(name);

            if (prop == null ||
                prop.propertyType !=
                SerializedPropertyType.Enum)
                continue;

            int index =
                Array.IndexOf(
                    prop.enumNames,
                    enumName
                );

            if (index >= 0)
            {
                prop.enumValueIndex = index;
                return true;
            }
        }

        return false;
    }

    private bool SetBool(
        SerializedObject so,
        string[] names,
        bool value)
    {
        foreach (string name in names)
        {
            SerializedProperty prop =
                so.FindProperty(name);

            if (prop != null &&
                prop.propertyType ==
                SerializedPropertyType.Boolean)
            {
                prop.boolValue = value;
                return true;
            }
        }

        return false;
    }

    private void SetRelativeString(
        SerializedProperty parent,
        string name,
        string value)
    {
        SerializedProperty p =
            parent.FindPropertyRelative(name);

        if (p != null &&
            p.propertyType ==
            SerializedPropertyType.String)
        {
            p.stringValue = value;
        }
    }

    private void SetRelativeBool(
        SerializedProperty parent,
        string name,
        bool value)
    {
        SerializedProperty p =
            parent.FindPropertyRelative(name);

        if (p != null &&
            p.propertyType ==
            SerializedPropertyType.Boolean)
        {
            p.boolValue = value;
        }
    }

    private void SetRelativeFloat(
        SerializedProperty parent,
        string name,
        float value)
    {
        SerializedProperty p =
            parent.FindPropertyRelative(name);

        if (p != null &&
            p.propertyType ==
            SerializedPropertyType.Float)
        {
            p.floatValue = value;
        }
    }

    private void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i]))
                continue;

            string next =
                current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]
                );
            }

            current = next;
        }
    }
}


public class MehigoSceneCapturePreviewWindow : EditorWindow
{
    private static readonly Dictionary<string, Texture2D> PendingCaptures =
        new Dictionary<string, Texture2D>();

    private string captureBaseName;
    private string saveFolder;
    private bool thai;

    private Texture2D previewTexture;
    private string statusMessage = "";

    private const int PreviewSize = 320;
    private const int CaptureSize = 256;

    public static void Open(
        string captureBaseName,
        string saveFolder,
        bool thai)
    {
        MehigoSceneCapturePreviewWindow window =
            GetWindow<MehigoSceneCapturePreviewWindow>(
                true,
                thai ? "ตัวอย่างภาพ Capture" : "Capture Preview",
                true
            );

        window.captureBaseName = captureBaseName;
        window.saveFolder = saveFolder;
        window.thai = thai;

        window.minSize = new Vector2(380, 470);
        window.maxSize = new Vector2(520, 620);

        window.RefreshPreview();
        window.ShowUtility();
    }

    public static bool TryConsumeCapture(string key, out Texture2D texture)
    {
        if (PendingCaptures.TryGetValue(key, out texture))
        {
            PendingCaptures.Remove(key);
            return true;
        }

        texture = null;
        return false;
    }

    private string T(string th, string en)
    {
        return thai ? th : en;
    }

    private void OnDisable()
    {
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            T("Preview จาก Scene View", "Scene View Preview"),
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            T(
                "ภาพนี้คือมุมที่จะถูกใช้เป็นไอคอน 1:1",
                "This is the 1:1 framing that will be used for the icon."
            ),
            EditorStyles.miniLabel
        );

        EditorGUILayout.Space(8);

        Rect previewRect = GUILayoutUtility.GetRect(
            PreviewSize,
            PreviewSize,
            GUILayout.ExpandWidth(true)
        );

        float side = Mathf.Min(previewRect.width, previewRect.height);
        Rect squareRect = new Rect(
            previewRect.x + (previewRect.width - side) * 0.5f,
            previewRect.y,
            side,
            side
        );

        EditorGUI.DrawRect(
            squareRect,
            new Color(0.12f, 0.12f, 0.12f, 1f)
        );

        if (previewTexture != null)
        {
            GUI.DrawTexture(
                squareRect,
                previewTexture,
                ScaleMode.ScaleToFit,
                false
            );
        }
        else
        {
            GUI.Label(
                squareRect,
                T(
                    "ยังไม่มี Preview\nเปิด Scene View แล้วกด Refresh Preview",
                    "No preview yet\nOpen Scene View and press Refresh Preview"
                ),
                new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                }
            );
        }

        EditorGUILayout.Space(8);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(
                statusMessage,
                MessageType.Info
            );
        }

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
            T("Refresh Preview", "Refresh Preview"),
            GUILayout.Height(32)))
        {
            RefreshPreview();
        }

        GUI.enabled = previewTexture != null;

        if (GUILayout.Button(
            T("Capture & ใช้งาน", "Capture & Use"),
            GUILayout.Height(32)))
        {
            Texture2D captured = SaveCurrentSceneCapture();

            if (captured != null)
            {
                PendingCaptures[captureBaseName] = captured;
                Close();
            }
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button(
            T("ยกเลิก", "Cancel"),
            GUILayout.Height(26)))
        {
            Close();
        }

        EditorGUILayout.Space(8);

        EditorGUILayout.HelpBox(
            T(
                "ถ้าต้องการเปลี่ยนมุม ให้กลับไปขยับ Scene View แล้วกด Refresh Preview อีกครั้ง",
                "To change the framing, move the Scene View camera, then press Refresh Preview again."
            ),
            MessageType.None
        );
    }

    private void RefreshPreview()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView == null || sceneView.camera == null)
        {
            statusMessage = T(
                "ไม่พบ Scene View ที่ใช้งานอยู่",
                "No active Scene View was found."
            );

            Repaint();
            return;
        }

        Texture2D newPreview = RenderSceneViewToTexture(
            sceneView.camera,
            PreviewSize
        );

        if (newPreview == null)
        {
            statusMessage = T(
                "ไม่สามารถสร้าง Preview ได้",
                "Could not create the preview."
            );

            Repaint();
            return;
        }

        if (previewTexture != null)
            DestroyImmediate(previewTexture);

        previewTexture = newPreview;

        statusMessage = T(
            "อัปเดต Preview แล้ว",
            "Preview refreshed."
        );

        Repaint();
    }

    private Texture2D SaveCurrentSceneCapture()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView == null || sceneView.camera == null)
        {
            EditorUtility.DisplayDialog(
                "mehigo",
                T(
                    "ไม่พบ Scene View ที่ใช้งานอยู่",
                    "No active Scene View was found."
                ),
                "OK"
            );

            return null;
        }

        Texture2D captureTexture = RenderSceneViewToTexture(
            sceneView.camera,
            CaptureSize
        );

        if (captureTexture == null)
            return null;

        EnsureFolderStatic(saveFolder);

        string iconFolder = saveFolder + "/Icons";
        EnsureFolderStatic(iconFolder);

        string fileName =
            SanitizeFileNameStatic(captureBaseName) + "_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") +
            ".png";

        string assetPath = iconFolder + "/" + fileName;
        string absolutePath = Path.GetFullPath(assetPath);

        byte[] png = captureTexture.EncodeToPNG();
        DestroyImmediate(captureTexture);

        File.WriteAllBytes(absolutePath, png);

        AssetDatabase.ImportAsset(
            assetPath,
            ImportAssetOptions.ForceUpdate
        );

        TextureImporter importer =
            AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = CaptureSize;
            importer.SaveAndReimport();
        }

        Texture2D imported =
            AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

        Selection.activeObject = imported;

        Debug.Log(
            $"[mehigo] Scene icon captured: {assetPath}"
        );

        return imported;
    }

    private Texture2D RenderSceneViewToTexture(
        Camera sourceCamera,
        int size)
    {
        if (sourceCamera == null)
            return null;

        RenderTexture rt = new RenderTexture(
            size,
            size,
            24,
            RenderTextureFormat.ARGB32
        );

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = sourceCamera.targetTexture;

        try
        {
            sourceCamera.targetTexture = rt;
            sourceCamera.Render();

            RenderTexture.active = rt;

            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false
            );

            texture.ReadPixels(
                new Rect(0, 0, size, size),
                0,
                0
            );

            texture.Apply();

            return texture;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[mehigo] Preview render failed: " + ex
            );

            return null;
        }
        finally
        {
            sourceCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            rt.Release();
            DestroyImmediate(rt);
        }
    }

    private static void EnsureFolderStatic(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i]))
                continue;

            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]
                );
            }

            current = next;
        }
    }

    private static string SanitizeFileNameStatic(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');

        return value.Replace(" ", "_");
    }
}
