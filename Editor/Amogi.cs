using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using UnityEngine.Animations;

//Made by Dreadrith#3238
//Discord: https://discord.gg/ZsPfrGn
//Github: https://github.com/Dreadrith/DreadScripts
//Gumroad: https://gumroad.com/dreadrith
//Ko-fi: https://ko-fi.com/dreadrith

//This script is highly hardcoded.
//Expect it to break easily if changes are made to assets.
namespace DreadScripts.Amogi
{
    public class Amogi : EditorWindow
    {
        #region Constants
        private const string RESOURCE_MATERIALPATH = "Assets/hfcRed/Prefabs/Among Us Follower/Resources/Materials/Scripted Material.mat";
        private const string RESOURCE_MATERIALGUID = "7edba4dbb7dcb16489a8941729fbe27e";
        private const string PROPERTY_COLOR = "_Color";

        private const string RESOURCE_AMOGUSPREFABPATH = "Assets/hfcRed/Prefabs/Among Us Follower/Resources/Scripted Mogus.prefab";
        private const string RESOURCE_AMOGUSPREFABGUID = "5c186c85995fe264b83892b1addd8209";

        private const string RESOURCE_ORIGINPREFABPATH = "Assets/hfcRed/Prefabs/Among Us Follower/Resources/World.prefab";
        private const string RESOURCE_ORIGINPREFABGUID = "2fd2f41879112a74491b50d3f1cd85d0";

        private const string PATH_GENERATED_MATERIALS = "Assets/hfcRed/Prefabs/Among Us Follower/Generated Materials";

        private const string NAME_MOGUSCONTAINER = "Among Us Followers";
        private const string NAME_WORLDCONSTRAINT = "World Constraint";

        private const string IDENTIFIER_NAME = "[NAME]";
        private const string IDENTIFIER_FOLLOW = "Target";
        private const string IDENTIFIER_LOOKAT = "Look At";
        private const string IDENTIFIER_CONSTRAINT = "Constraints";
        private const string IDENTIFIER_MESH = "Mesh";

        private static readonly string[] NAME_LIST = {
            "Kevin",
            "Kevented",
            "David",
            "John",
            "Bob",
            "Suspicious Stanley",
            "Sussy_Baka",
            "Amon_Gus",
            "walter white yo",
            "ItsRed",
            "RedIsSus",
            "IAmNotRed",
            "VOTE_RED",
            "Orang",
            "Vincent Van Ghost",
            "ImLit3rallyDead",
            "xXSnapDragonXx",
            "Xx_Gh0st_xX",
            "T_DragonSlayer",
            "Zer0S4",
            "China #1",
            "Blxxdy Mary",
            "Pineapple Head",
            "WhiteBelly2008",
            "_VENTilator",
            "Princess Peaches Pie Marie Popo Von Meow",
            "Dame Luceal Stinker Smudge Fortune",
            "ISeeThemEverywhere",
            "GetOutOfMyHead",
            "AAAAAAAAA",
            "Cam",
            "JellyBean",
            "Cibbi",
            "Lin",
            "Rero",
            "Micheal Jackson HEE HEE",
            "The Spanish Inquisition",
            "Queen of England, Tea & Biscuits",
            "ඞ",
            "ᱷ",
            "ಡ",
            "ඣ"
        };
        #endregion

        #region GUI Automated

        private static Vector2 scroll = Vector2.zero;
        private static bool showSettings;
        private static bool showColoring;
        private static bool showCrewmates;
        private static bool showOffsets;

        private static bool init;
        #endregion

        #region Input

        private static GameObject avatar;

        private static Crewmate[] crewmates;
        private static float crewSpeed = 0.008f;

        private static Color singleColor;
        private static (string, Color)[] availableColors;

        private static e_ColoringMode coloringMode = e_ColoringMode.Shared;
        private enum e_ColoringMode
        {
            Single,
            Shared,
            Unique
        }

        private static Vector3 startPosOffset = new Vector3(0.4f, 0.01f, 0);
        private static Vector3 midPosOffset = new Vector3(0, 0, -0.5f);

        private static Vector3 startLookAtTargetPos = new Vector3(-1 / 4f, 0, 3 / 4f);
        private static Vector3 midLookAtTargetPos = new Vector3(0, 0, 3);
        private static bool lookAtNext = true;
        #endregion


        [MenuItem("hfcRed/Prefabs/Sussy Amogus")]
        private static void m_ShowWindow()
        {
            GetWindow<Amogi>("Sussy Baka Window").titleContent.image = EditorGUIUtility.IconContent("Slider Icon").image;
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                avatar = (GameObject)EditorGUILayout.ObjectField(CustomGUI.Content.avatarContent, avatar, typeof(GameObject), true);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                m_DrawDummycount(CustomGUI.Content.countContent, crewmates.Length, m_RefreshCrewmateArray);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                showSettings = EditorGUILayout.Foldout(showSettings, "Advanced Settings");
                if (showSettings)
                {
                    using (new CustomGUI.CustomIndent())
                        m_DrawSettings();
                }
            }


            using (new EditorGUI.DisabledGroupScope(!avatar))
            {
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("<color=#FFBD62><b>Get Sussy!</b></color>", CustomGUI.Styles.sussyStyleButton, GUILayout.Height(36)))
                        m_InitiateMurder();

                    Color og = GUI.backgroundColor;
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button(CustomGUI.Content.trashIcon, CustomGUI.Styles.iconStyleButton, GUILayout.Height(36), GUILayout.Width(36)))
                        m_EjectCrewmates();
                    GUI.backgroundColor = og;
                }
            }

            DreadCredit();
            EditorGUILayout.EndScrollView();
        }



        private static void m_InitiateMurder()
        {

            if (crewmates.Length > 100 && !EditorUtility.DisplayDialog("Caution", "Generating more than 100 crewmates might cause your CPU to call an EMERGENCY MEETING!", "Continue", "Cancel"))
            {
                return;
            }

            var amogusPrefab = m_ReadyResource<GameObject>(RESOURCE_AMOGUSPREFABPATH, RESOURCE_AMOGUSPREFABGUID);
            var originPrefab = m_ReadyResource<GameObject>(RESOURCE_ORIGINPREFABPATH, RESOURCE_ORIGINPREFABGUID);
            var poiMatPath = AssetDatabase.GetAssetPath(m_ReadyResource<Material>(RESOURCE_MATERIALPATH, RESOURCE_MATERIALGUID));
            ReadyPath(PATH_GENERATED_MATERIALS);
            foreach (var c in crewmates) c.m_RefreshColor();

            //Root Hierarchy Generation 
            m_ReadyChild(avatar.transform, NAME_MOGUSCONTAINER, out GameObject mainRoot);
            if (!m_ReadyChild(mainRoot.transform, NAME_WORLDCONSTRAINT, out GameObject worldGameObject))
            {
                var con = worldGameObject.AddComponent<ParentConstraint>();
                con.AddSource(new ConstraintSource() { weight = 1, sourceTransform = originPrefab.transform });

                con.translationAtRest = Vector3.zero;
                con.rotationAtRest = Vector3.zero;

                var allAxis = Axis.X | Axis.Y | Axis.Z;
                con.translationAxis = allAxis;
                con.rotationAxis = allAxis;

                con.locked = true;
                con.constraintActive = true;
            }

            Transform previousFollowTarget = null;
            Transform previousConstraint = null;

            List<(string, Color, SkinnedMeshRenderer)> pathColorToMesh = new List<(string, Color, SkinnedMeshRenderer)>();

            try
            {
                AssetDatabase.StartAssetEditing();
                int currentIndex = 0;
                float progressStep = 1f / crewmates.Length;
                foreach (var mate in crewmates)
                {
                    EditorUtility.DisplayProgressBar("Getting Sussy", $"Hiring {mate.name} ({currentIndex}/{crewmates.Length})", progressStep * currentIndex++);
                    //Mogus Hierarchy Generation
                    bool isFirst = previousFollowTarget == null;

                    var newMate = Instantiate(amogusPrefab);

                    if (!isFirst)
                    {
                        foreach (var a in newMate.GetComponentsInChildren<AudioSource>(true))
                            DestroyImmediate(a);
                    }

                    foreach (var t in newMate.GetComponentsInChildren<Transform>())
                        t.name = t.name.Replace(IDENTIFIER_NAME, mate.name);

                    var followTarget = m_NaiveFind(newMate, IDENTIFIER_FOLLOW);
                    var lookAtTarget = m_NaiveFind(newMate, IDENTIFIER_LOOKAT);
                    var constraint = m_NaiveFind(newMate, IDENTIFIER_CONSTRAINT);
                    var mesh = m_NaiveFind(newMate, IDENTIFIER_MESH);

                    constraint.parent = worldGameObject.transform;

                    if (isFirst)
                    {
                        followTarget.parent = mainRoot.transform;
                        followTarget.localPosition = startPosOffset;
                        lookAtTarget.localPosition = startLookAtTargetPos;
                        constraint.localPosition = startPosOffset;
                        constraint.LookAt(lookAtTarget);
                    }
                    else
                    {
                        followTarget.parent = previousConstraint;
                        followTarget.position = previousFollowTarget.position + midPosOffset;
                        constraint.position = previousFollowTarget.position + midPosOffset;
                        if (lookAtNext)
                        {
                            lookAtTarget.parent = previousConstraint;
                            lookAtTarget.localPosition = Vector3.zero;
                        }
                        else lookAtTarget.localPosition = midLookAtTargetPos;

                        constraint.LookAt(lookAtTarget);
                    }

                    var posCon = constraint.GetComponent<PositionConstraint>();
                    var source = posCon.GetSource(0);
                    source.weight = crewSpeed;
                    posCon.SetSource(0, source);

                    previousFollowTarget = followTarget;
                    previousConstraint = constraint;
                    DestroyImmediate(newMate);

                    //Material Generation and Assignment
                    SkinnedMeshRenderer renderer = mesh.GetComponent<SkinnedMeshRenderer>();
                    if (!renderer || renderer.sharedMaterials.Length <= 0) continue;

                    Color currentColor = TruncateColor(mate.color);
                    string colorString = $"{currentColor.r}{currentColor.g}{currentColor.b}".Replace(".", "");
                    string matName = $"{colorString}.mat";
                    string matPath = $"{PATH_GENERATED_MATERIALS}/{matName}";
                    Material currentMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (!currentMat) AssetDatabase.CopyAsset(poiMatPath, matPath);

                    pathColorToMesh.Add((matPath, currentColor, renderer));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh();

            foreach (var (path, color, renderer) in pathColorToMesh)
            {
                var currentMat = AssetDatabase.LoadAssetAtPath<Material>(path);
                currentMat.SetColor(PROPERTY_COLOR, color);

                var array = renderer.sharedMaterials;
                array[0] = currentMat;
                renderer.sharedMaterials = array;

            }


            GreenLog("Finished hiring crewmates!");
        }

        private static void m_EjectCrewmates()
        {
            if (!EditorUtility.DisplayDialog("Caution", $"Deleting crewmates from {avatar.name}. Continue?", "Continue", "Cancel")) return;
            Transform crew = avatar.transform.Find(NAME_MOGUSCONTAINER);
            if (!crew)
            {
                YellowLog("No crewmates found.");
                return;
            }

            Undo.DestroyObjectImmediate(crew.gameObject);
            GreenLog("Crewmates removed!");
        }

        private static void m_RefreshCrewmateArray(int newCount)
        {
            m_RefreshArray(ref crewmates, newCount, _ => new Crewmate());
        }

        private static void m_RefreshColoringArray(int newCount) => m_RefreshColoringArray(newCount, true);

        private static void m_RefreshColoringArray(int newCount, bool refreshCrewmates)
        {
            m_RefreshArray(ref availableColors, newCount, i => ($"Color {i + 1}", m_GetRandomColor()));

            if (refreshCrewmates)
            {
                foreach (var mate in crewmates)
                {
                    if (mate.sharedColorIndex >= newCount)
                        mate.sharedColorIndex = m_GetRandomSharedColorIndex();
                }
            }
        }


        private static void m_RefreshArray<T>(ref T[] array, int newCount, Func<int, T> func)
        {
            T[] newArray = new T[newCount];
            for (int i = 0; i < newArray.Length; i++)
            {
                if (i < array.Length) newArray[i] = array[i];
                else newArray[i] = func(i);
            }

            array = newArray;
        }
        #region GUI Methods
        private static void m_DrawSettings()
        {
            m_DrawCrewmates();
            EditorGUILayout.Space();
            m_DrawColoring();
            EditorGUILayout.Space();
            m_DrawOffsets();
        }

        private static void m_DrawColoring()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                showColoring = EditorGUILayout.Foldout(showColoring, "Coloring");
                if (showColoring)
                {
                    CustomGUI.DrawSeparator();
                    using (new CustomGUI.CustomIndent())
                    {
                        coloringMode = (e_ColoringMode)EditorGUILayout.EnumPopup(CustomGUI.Content.coloringContent, coloringMode);

                        switch (coloringMode)
                        {
                            case e_ColoringMode.Single:

                                EditorGUILayout.Space();
                                singleColor = EditorGUILayout.ColorField("Single Color: ", singleColor);
                                break;
                            case e_ColoringMode.Shared:
                                EditorGUILayout.Space();
                                using (new GUILayout.HorizontalScope())
                                {
                                    m_DrawDummycount(CustomGUI.Content.coloringPoolContent, availableColors.Length, m_RefreshColoringArray);
                                    if (GUILayout.Button("Randomize Colors"))
                                    {
                                        for (int i = 0; i < availableColors.Length; i++)
                                            availableColors[i].Item2 = m_GetRandomColor();
                                    }

                                }
                                for (int i = 0; i < availableColors.Length; i++)
                                {
                                    using (new GUILayout.HorizontalScope())
                                    {
                                        availableColors[i].Item1 = EditorGUILayout.TextField($"#{i + 1}", availableColors[i].Item1);
                                        availableColors[i].Item2 = EditorGUILayout.ColorField(availableColors[i].Item2);
                                    }
                                }
                                break;

                        }
                    }
                }
            }
        }

        private static void m_DrawCrewmates()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                showCrewmates = EditorGUILayout.Foldout(showCrewmates, "Crewmates");
                if (showCrewmates)
                {
                    CustomGUI.DrawSeparator();
                    using (new CustomGUI.CustomIndent())
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            crewSpeed = Mathf.Max(EditorGUILayout.FloatField(CustomGUI.Content.crewSpeedContent, crewSpeed), 0.0001f);
                            if (GUILayout.Button("Randomize Colors"))
                                foreach (var c in crewmates)
                                    c.m_RandomizeColor();
                        }
                        var colorNamesArray = availableColors.Select(a => a.Item1).ToArray();
                        for (int i = 0; i < crewmates.Length; i++)
                        {
                            var mate = crewmates[i];
                            using (new GUILayout.HorizontalScope())
                            {
                                mate.name = EditorGUILayout.TextField($"#{i + 1}", mate.name);
                                switch (coloringMode)
                                {
                                    case e_ColoringMode.Shared:
                                        mate.sharedColorIndex = EditorGUILayout.Popup(mate.sharedColorIndex, colorNamesArray);
                                        EditorGUILayout.ColorField(GUIContent.none, availableColors[mate.sharedColorIndex].Item2, false, false, false, GUILayout.Width(14), GUILayout.Height(14));
                                        break;
                                    case e_ColoringMode.Unique:
                                        mate.color = EditorGUILayout.ColorField(mate.color);
                                        break;
                                }
                            }
                        }
                    }


                }


            }
        }

        private static void m_DrawOffsets()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                showOffsets = EditorGUILayout.Foldout(showOffsets, "Offsets");
                if (showOffsets)
                {
                    CustomGUI.DrawSeparator();
                    using (new CustomGUI.CustomIndent())
                    {
                        startPosOffset = EditorGUILayout.Vector3Field(CustomGUI.Content.startPosOffsetContent, startPosOffset);
                        midPosOffset = EditorGUILayout.Vector3Field(CustomGUI.Content.midPosOffsetContent, midPosOffset);
                        EditorGUILayout.Space();
                        startLookAtTargetPos = EditorGUILayout.Vector3Field(CustomGUI.Content.startLookAtOffsetContent, startLookAtTargetPos);

                        if (!lookAtNext) midLookAtTargetPos = EditorGUILayout.Vector3Field(CustomGUI.Content.midLookAtOffsetContent, midLookAtTargetPos);
                        lookAtNext = EditorGUILayout.Toggle(CustomGUI.Content.lookAtNextContent, lookAtNext);
                    }
                }

            }
        }
        #endregion


        #region Helper Methods

        private static void m_DrawDummycount(GUIContent label, int dummyCount, Action<int> action)
        {
            EditorGUI.BeginChangeCheck();
            int c = dummyCount;
            c = Mathf.Max(EditorGUILayout.DelayedIntField(label, c), 1);
            if (EditorGUI.EndChangeCheck())
                action(c);
        }

        private static string m_GetRandomName() => NAME_LIST[Random.Range(0, NAME_LIST.Length)];
        private static Color m_GetRandomColor()
        {
            float RandomZeroOne() => Random.Range(0f, 1f);

            return new Color(RandomZeroOne(), RandomZeroOne(), RandomZeroOne());
        }
        private static int m_GetRandomSharedColorIndex()
            => Random.Range(0, availableColors.Length);


        public static T m_ReadyResource<T>(string resourcePath, string resourceGUID = "", string msg = "") where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(resourcePath);
            if (asset) return asset;

            if (!string.IsNullOrWhiteSpace(resourceGUID))
            {
                asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(resourceGUID));
                if (asset) return asset;
            }

            if (string.IsNullOrWhiteSpace(msg)) msg = $"Required Resources Asset at Resource Path {resourcePath} not found! Execution may not proceed.";
            EditorUtility.DisplayDialog("Error", msg, "Heck");
            throw new NullReferenceException(msg);
        }

        public static Transform m_NaiveFind(GameObject root, string identifier) =>
            root.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.IndexOf(identifier, StringComparison.OrdinalIgnoreCase) >= 0);

        public static bool m_ReadyChild(Transform root, string name, out GameObject child)
        {
            child = root.Find(name)?.gameObject;
            if (child) return true;

            child = new GameObject(name)
            {
                transform =
                {
                    parent = root,
                    localPosition = Vector3.zero,
                    localRotation = Quaternion.identity,
                    localScale = Vector3.one
                }
            };

            return false;
        }

        public static Color TruncateColor(Color c)
        {
            float round(float f) => Mathf.RoundToInt(f * 100) / 100f;
            return new Color(round(c.r), round(c.g), round(c.b));
        }

        public static void GreenLog(string msg)
        {
            Debug.Log($"<color=green>[Amogi]</color> {msg}");
        }
        public static void YellowLog(string msg)
        {
            Debug.LogWarning($"<color=yellow>[Amogi]</color> {msg}");
        }

        private static void ReadyPath(string folderPath)
        {
            if (Directory.Exists(folderPath)) return;
            Directory.CreateDirectory(folderPath);
            AssetDatabase.ImportAsset(folderPath);
        }

        internal static void DreadCredit()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Made By Dreadrith#3238", "boldlabel"))
                    Application.OpenURL("https://linktr.ee/Dreadrith");
            }
        }
        #endregion

        #region Automated Methods

        private void OnEnable()
        {
            avatar = avatar ?? FindObjectOfType<Animator>()?.transform.root.gameObject;
            if (!init)
            {
                init = true;
                singleColor = m_GetRandomColor();

                availableColors = Array.Empty<(string, Color)>();
                m_RefreshColoringArray(2, false);

                crewmates = Array.Empty<Crewmate>();
                m_RefreshCrewmateArray(3);
            }
        }
        #endregion

        public class Crewmate
        {
            public Color color;
            public int sharedColorIndex;
            public string name;

            public Crewmate()
            {
                m_RandomizeName();
                m_RandomizeColor();
            }

            public void m_RefreshColor()
            {
                switch (coloringMode)
                {
                    case e_ColoringMode.Single:
                        color = singleColor;
                        break;
                    case e_ColoringMode.Shared:
                        color = availableColors[sharedColorIndex].Item2;
                        break;
                }
            }

            public void m_RandomizeName() => name = m_GetRandomName();

            public void m_RandomizeColor()
            {
                sharedColorIndex = m_GetRandomSharedColorIndex();
                color = m_GetRandomColor();
            }
        }

    }

    public static class CustomGUI
    {
        public static class Styles
        {
            public static GUIStyle sussyStyleButton = new GUIStyle(GUI.skin.button) { fontSize = 16, richText = true };
            public static GUIStyle iconStyleButton = new GUIStyle(GUI.skin.button) { padding = new RectOffset(), margin = new RectOffset(1, 1, 1, 1) };
        }
        public static class Content
        {
            public static GUIContent avatarContent = new GUIContent("Avatar", "The unfortunate soul that will contain the crewmates");
            public static GUIContent countContent = new GUIContent("Crew Count", "How many crewmates?");
            public static GUIContent crewSpeedContent = new GUIContent("Crew Speed", "How fast the crewmates follow you.");
            public static GUIContent coloringContent = new GUIContent("Color Mode", "How should the crewmates be colored?\nSingle: All crewmates use a single material\nShared: Crewmates may share material from a limited pool\nUnique: Each crewmate has its own unique color material");
            public static GUIContent coloringPoolContent = new GUIContent("Color Pool Count", "Colors that the crewmates may use");

            public static GUIContent startPosOffsetContent = new GUIContent("Start Pos Offset", "The positional offset of the first crewmate relative to the Avatar");
            public static GUIContent midPosOffsetContent = new GUIContent("Mid Pos Offset", "The positional offset of the amogi in between each other");

            public static GUIContent startLookAtOffsetContent = new GUIContent("Start LookAt Pos", "The position of the look at target of the first crewmate");
            public static GUIContent midLookAtOffsetContent = new GUIContent("Mid LookAt Pos", "The position of the look at target of 2nd and above crewmates");
            public static GUIContent lookAtNextContent = new GUIContent("Look At Previous Mate", "2nd and above crewmates Look At target should be the previous crewmate");

            public static GUIContent trashIcon = new GUIContent(EditorGUIUtility.IconContent("TreeEditor.Trash")) { tooltip = "Eject all Crewmates" };
        }

        public static void DrawSeparator(int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(thickness + padding));
            r.height = thickness;
            r.y += padding / 2f;
            r.x -= 2;
            r.width += 6;
            ColorUtility.TryParseHtmlString(EditorGUIUtility.isProSkin ? "#595959" : "#858585", out Color lineColor);
            EditorGUI.DrawRect(r, lineColor);
        }

        public class CustomIndent : IDisposable
        {
            private static int currentIndentValue = 0;
            private int myIndent;
            public CustomIndent(int indentValue = 10)
            {
                myIndent = indentValue;
                currentIndentValue += indentValue;

                EditorGUILayout.BeginHorizontal();

                using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(currentIndentValue)))
                    GUILayout.Space(currentIndentValue);

                EditorGUILayout.BeginVertical();
            }
            public void Dispose()
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                currentIndentValue -= myIndent;
            }
        }
    }
}
