using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Beethoven5DemoSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Beethoven5Demo.unity";
    private const int SectionPerformerCount = 7;
    private static readonly Vector3 ConductorFocusPoint = new(0f, 1.45f, 6f);
    private static readonly string[] StringsKeywords = { "violin", "viola", "cello", "violoncello", "contrabass", "doublebass", "double_bass" };
    private static readonly string[] WoodwindsKeywords = { "flute", "oboe", "clarinet", "bassoon" };
    private static readonly string[] BrassKeywords = { "french_horn", "french horn", "english_horn", "english horn", "coranglais", "cor anglais", "horn" };
    private static readonly string[] PercussionKeywords = { "timpani", "kettledrum" };

    [MenuItem("Tools/Orchestra Conductor/Create Beethoven 5 Demo Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureFolders();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.23f, 0.24f, 0.28f);

        Material placeholderMaterial = CreatePlaceholderMaterial();
        StemAssignmentResult stemAssignments = CollectStemAssignments();

        GameObject gameManager = new("GameManager");
        OrchestraManager orchestraManager = gameManager.AddComponent<OrchestraManager>();

        GameObject orchestraRoot = new("Orchestra");
        GameObject environmentRoot = new("Environment");
        CreateEnvironment(environmentRoot.transform, placeholderMaterial);

        GameObject player = CreatePlayer();

        SectionBuildData strings = CreateSection(
            orchestraRoot.transform,
            "Strings",
            ConductorFocusPoint,
            SectionPerformerCount,
            9.15f,
            124f,
            142f,
            placeholderMaterial,
            stemAssignments.Strings);

        SectionBuildData woodwinds = CreateSection(
            orchestraRoot.transform,
            "Woodwinds",
            ConductorFocusPoint,
            SectionPerformerCount,
            9.55f,
            96f,
            112f,
            placeholderMaterial,
            stemAssignments.Woodwinds);

        SectionBuildData brass = CreateSection(
            orchestraRoot.transform,
            "Brass",
            ConductorFocusPoint,
            SectionPerformerCount,
            9.55f,
            68f,
            84f,
            placeholderMaterial,
            stemAssignments.Brass);

        SectionBuildData percussion = CreateSection(
            orchestraRoot.transform,
            "Percussion",
            ConductorFocusPoint,
            SectionPerformerCount,
            9.15f,
            40f,
            58f,
            placeholderMaterial,
            stemAssignments.Percussion);

        HudBuildData hudData = CreateHud();

        SerializedObject managerSerializedObject = new(orchestraManager);
        AssignSection(managerSerializedObject, "strings", strings);
        AssignSection(managerSerializedObject, "woodwinds", woodwinds);
        AssignSection(managerSerializedObject, "brass", brass);
        AssignSection(managerSerializedObject, "percussion", percussion);
        managerSerializedObject.FindProperty("hud").objectReferenceValue = hudData.Hud;
        managerSerializedObject.FindProperty("finishedText").objectReferenceValue = hudData.FinishedText;
        managerSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));

        LogAssignmentSummary(stemAssignments);
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Audio");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Scripts");
        EnsureFolder("Assets/UI");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent ?? "Assets", folderName);
    }

    private static Material CreatePlaceholderMaterial()
    {
        const string materialPath = "Assets/Materials/SectionPlaceholder.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        material = new Material(shader)
        {
            color = new Color(0.35f, 0.37f, 0.42f, 1f)
        };
        material.EnableKeyword("_EMISSION");
        AssetDatabase.CreateAsset(material, materialPath);
        return material;
    }

    private static Material CreateOrLoadMaterial(string path, Color color, Color emissionColor, float smoothness = 0.35f)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        material = new Material(shader)
        {
            color = color
        };
        material.EnableKeyword("_EMISSION");
        material.SetColor("_BaseColor", color);
        material.SetColor("_EmissionColor", emissionColor);
        material.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material LoadRequiredMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            throw new InvalidOperationException($"Required material is missing: {path}");
        }

        return material;
    }

    private static void CreateEnvironment(Transform root, Material placeholderMaterial)
    {
        // Mirrors the exact material assets currently assigned in the hand-tuned scene.
        Material parquetMaterial = LoadRequiredMaterial("Assets/Materials/Materials/rectangular_parquet_diff_4k.mat");
        Material carpetWallMaterial = LoadRequiredMaterial("Assets/Materials/Materials/1K-casino_carpet_4-displacement.mat");
        Material wallMaterial = LoadRequiredMaterial("Assets/Materials/HallWall.mat");
        Material ceilingMaterial = LoadRequiredMaterial("Assets/Materials/Ceiling.mat");
        Material chairWoodMaterial = LoadRequiredMaterial("Assets/Materials/ChairWood.mat");

        GameObject directionalLight = new("Directional Light");
        directionalLight.transform.SetParent(root);
        directionalLight.transform.rotation = Quaternion.Euler(32f, -16f, 0f);
        Light light = directionalLight.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.16f;
        light.color = new Color(0.92f, 0.92f, 0.95f);
        light.shadows = LightShadows.Soft;

        // Mirrors the latest hand-tuned scene lights while preserving the warmer, lower stage focus.
        CreateStageWashLight(root, "WashLeftFront", new Vector3(-12.135f, 5.69f, 9.545f), new Vector3(-11.4637f, 5.0646f, 9.9428f), 1200f);
        CreateStageWashLight(root, "WashRightFront", new Vector3(12.135f, 5.69f, 9.545f), new Vector3(11.4637f, 5.0646f, 9.9428f), 1200f);
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(root);
        floor.transform.localScale = new Vector3(5f, 1f, 6f);
        floor.GetComponent<Renderer>().sharedMaterial = parquetMaterial;

        GameObject stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stage.name = "Stage";
        stage.transform.SetParent(root);
        stage.transform.position = new Vector3(0f, 0.35f, 17.5f);
        stage.transform.localScale = new Vector3(27.597599f, 0.7f, 22f);
        stage.GetComponent<Renderer>().sharedMaterial = carpetWallMaterial;

        GameObject podium = GameObject.CreatePrimitive(PrimitiveType.Cube);
        podium.name = "Podium";
        podium.transform.SetParent(root);
        podium.transform.position = new Vector3(-0.0375f, 0.38f, 6.1311f);
        podium.transform.localScale = new Vector3(3.0746784f, 1.070867f, 2.7379665f);
        podium.GetComponent<Renderer>().sharedMaterial = carpetWallMaterial;

        CreateHallShell(root, parquetMaterial, carpetWallMaterial, wallMaterial, ceilingMaterial);
        CreateAudienceBlocks(root, chairWoodMaterial, parquetMaterial);
    }

    private static void CreateStageWashLight(Transform parent, string name, Vector3 position, Vector3 target, float intensity)
    {
        GameObject lightObject = new(name);
        lightObject.transform.SetParent(parent);
        lightObject.transform.position = position;
        lightObject.transform.rotation = Quaternion.LookRotation((target - position).normalized, Vector3.up);

        Light spot = lightObject.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.intensity = intensity;
        spot.range = 31f;
        spot.spotAngle = 76f;
        spot.innerSpotAngle = 50f;
        spot.color = new Color(1f, 0.92f, 0.84f);
        spot.shadows = LightShadows.Soft;
    }

    private static void ApplySurfaceTextures(
        Material material,
        string baseMapPath = null,
        string normalMapPath = null,
        string maskMapPath = null,
        string occlusionMapPath = null,
        Vector2? textureScale = null,
        IReadOnlyList<string> fallbackNames = null)
    {
        if (material == null)
        {
            return;
        }

        Texture2D baseMap = LoadTexture(baseMapPath)
            ?? FindTextureAsset(fallbackNames, excludeNames: new[] { "nor", "normal", "rough", "ao", "metal", "disp", "height", "arm" });
        if (baseMap != null)
        {
            material.SetTexture("_BaseMap", baseMap);
            material.SetTexture("_MainTex", baseMap);
        }

        Texture2D normalMap = LoadTexture(normalMapPath)
            ?? FindTextureAsset(fallbackNames, includeNames: new[] { "nor", "normal" });
        if (normalMap != null)
        {
            material.SetTexture("_BumpMap", normalMap);
            material.EnableKeyword("_NORMALMAP");
        }

        Texture2D maskMap = LoadTexture(maskMapPath)
            ?? FindTextureAsset(fallbackNames, includeNames: new[] { "rough", "arm" });
        if (maskMap != null)
        {
            material.SetTexture("_MetallicGlossMap", maskMap);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        Texture2D occlusionMap = LoadTexture(occlusionMapPath)
            ?? FindTextureAsset(fallbackNames, includeNames: new[] { "ao", "occlusion" });
        if (occlusionMap != null)
        {
            material.SetTexture("_OcclusionMap", occlusionMap);
        }

        if (textureScale.HasValue)
        {
            Vector2 scale = textureScale.Value;
            material.SetTextureScale("_BaseMap", scale);
            material.SetTextureScale("_MainTex", scale);
            material.SetTextureScale("_BumpMap", scale);
            material.SetTextureScale("_MetallicGlossMap", scale);
            material.SetTextureScale("_OcclusionMap", scale);
        }

        EditorUtility.SetDirty(material);
    }

    private static Texture2D LoadTexture(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    private static Texture2D FindTextureAsset(
        IReadOnlyList<string> preferredNames,
        IReadOnlyList<string> includeNames = null,
        IReadOnlyList<string> excludeNames = null)
    {
        IEnumerable<Texture2D> textures = AssetDatabase
            .FindAssets("t:Texture2D", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
            .Where(texture => texture != null);

        foreach (Texture2D texture in textures)
        {
            string normalized = texture.name.Replace("-", " ").Replace("_", " ").ToLowerInvariant();
            bool matchesPreferred = preferredNames.Any(name => normalized.Contains(name));
            bool matchesInclude = includeNames == null || includeNames.Any(name => normalized.Contains(name));
            bool matchesExclude = excludeNames != null && excludeNames.Any(name => normalized.Contains(name));

            if (matchesPreferred && matchesInclude && !matchesExclude)
            {
                return texture;
            }
        }

        return null;
    }

    private static GameObject CreatePlayer()
    {
        GameObject player = new("Player");
        player.transform.position = new Vector3(0f, 1.45f, 4.5f);
        Vector3 flatForward = new Vector3(ConductorFocusPoint.x - player.transform.position.x, 0f, 15f);
        if (flatForward.sqrMagnitude > 0.001f)
        {
            player.transform.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        }

        SimpleFPSController controller = player.AddComponent<SimpleFPSController>();

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform);
        cameraObject.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        cameraObject.transform.localRotation = Quaternion.identity;

        Camera cameraComponent = cameraObject.AddComponent<Camera>();
        cameraComponent.clearFlags = CameraClearFlags.Skybox;
        cameraObject.AddComponent<AudioListener>();

        SerializedObject controllerSerializedObject = new(controller);
        controllerSerializedObject.FindProperty("cameraPivot").objectReferenceValue = cameraObject.transform;
        controllerSerializedObject.FindProperty("initialPitch").floatValue = 0f;
        controllerSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    private static SectionBuildData CreateSection(
        Transform orchestraRoot,
        string sectionName,
        Vector3 conductorPoint,
        int placeholderCount,
        float radius,
        float startAngleDegrees,
        float endAngleDegrees,
        Material placeholderMaterial,
        IReadOnlyList<AudioClip> assignedClips)
    {
        GameObject sectionRoot = new(sectionName);
        sectionRoot.transform.SetParent(orchestraRoot);

        List<AudioSource> sources = new();
        List<Renderer> renderers = new();
        int frontRowCount = Mathf.Min(3, placeholderCount);
        int backRowCount = Mathf.Max(0, placeholderCount - frontRowCount);
        float midAngleDegrees = (startAngleDegrees + endAngleDegrees) * 0.5f;
        float midAngleRadians = midAngleDegrees * Mathf.Deg2Rad;
        Vector3 sectionCenter = new(
            conductorPoint.x + Mathf.Cos(midAngleRadians) * radius,
            0.95f,
            conductorPoint.z + Mathf.Sin(midAngleRadians) * radius);
        Vector3 inward = new(conductorPoint.x - sectionCenter.x, 0f, conductorPoint.z - sectionCenter.z);
        Vector3 sectionForward = inward.sqrMagnitude > 0.001f ? inward.normalized : Vector3.back;
        Vector3 sectionRight = Vector3.Cross(Vector3.up, sectionForward).normalized;

        sectionRoot.transform.position = sectionCenter;
        sectionRoot.transform.rotation = Quaternion.LookRotation(sectionForward, Vector3.up);

        for (int i = 0; i < placeholderCount; i++)
        {
            string performerName = i < assignedClips.Count && assignedClips[i] != null
                ? assignedClips[i].name
                : $"{sectionName}_Player_{i + 1}";
            GameObject performer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            performer.name = performerName;
            performer.transform.SetParent(sectionRoot.transform);

            bool isFrontRow = i < frontRowCount;
            int rowIndex = isFrontRow ? i : i - frontRowCount;
            int rowCount = isFrontRow ? frontRowCount : backRowCount;
            float rowWidth = isFrontRow ? 2.8f : 4.2f;
            float spacing = rowCount <= 1 ? 0f : rowWidth / (rowCount - 1);
            float localX = rowCount <= 1 ? 0f : -rowWidth * 0.5f + spacing * rowIndex;
            float localZ = isFrontRow ? 0.9f : -0.95f;
            Vector3 worldPosition = sectionCenter + sectionRight * localX + sectionForward * localZ;
            performer.transform.position = worldPosition;
            performer.transform.localScale = new Vector3(0.72f, 0.95f, 0.72f);
            Vector3 lookTarget = new(conductorPoint.x, performer.transform.position.y, conductorPoint.z);
            performer.transform.LookAt(lookTarget);

            foreach (Renderer renderer in performer.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = placeholderMaterial;
                renderers.Add(renderer);
            }

            AttachMusicStand(performer.transform, placeholderMaterial);

            AudioSource source = performer.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            if (i < assignedClips.Count)
            {
                source.clip = assignedClips[i];
            }
            sources.Add(source);
        }

        GameObject label = new($"{sectionName}Label");
        label.transform.SetParent(sectionRoot.transform);
        label.transform.localPosition = new Vector3(0f, 1.75f, -2.35f);
        label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        TextMeshPro text = label.AddComponent<TextMeshPro>();
        text.text = sectionName.ToUpperInvariant();
        text.fontSize = 3.8f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.95f, 0.95f, 0.9f);

        OrchestraSectionVisual visual = sectionRoot.AddComponent<OrchestraSectionVisual>();
        SerializedObject visualSerializedObject = new(visual);
        SerializedProperty renderersProperty = visualSerializedObject.FindProperty("targetRenderers");
        renderersProperty.arraySize = renderers.Count;
        for (int i = 0; i < renderers.Count; i++)
        {
            renderersProperty.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        }
        visualSerializedObject.ApplyModifiedPropertiesWithoutUndo();
        visual.SetActiveVisual(false);

        return new SectionBuildData(sectionName, sources.ToArray(), visual);
    }

    private static void AttachMusicStand(Transform performer, Material performerMaterial)
    {
        Material standMaterial = CreateOrLoadMaterial(
            "Assets/Materials/MusicStand.mat",
            new Color(0.08f, 0.08f, 0.1f),
            Color.black,
            0.18f);

        GameObject standPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        standPole.name = $"{performer.name}_StandPole";
        standPole.transform.SetParent(performer, false);
        standPole.transform.localPosition = new Vector3(0f, -0.35f, 0.82f);
        standPole.transform.localScale = new Vector3(0.06f, 0.52f, 0.06f);
        standPole.GetComponent<Renderer>().sharedMaterial = standMaterial;

        GameObject standTray = GameObject.CreatePrimitive(PrimitiveType.Cube);
        standTray.name = $"{performer.name}_StandTray";
        standTray.transform.SetParent(performer, false);
        standTray.transform.localPosition = new Vector3(0f, 0.18f, 0.85f);
        standTray.transform.localScale = new Vector3(0.46f, 0.09f, 0.28f);
        standTray.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        standTray.GetComponent<Renderer>().sharedMaterial = standMaterial;

        GameObject standLip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        standLip.name = $"{performer.name}_StandLip";
        standLip.transform.SetParent(standTray.transform, false);
        standLip.transform.localPosition = new Vector3(0f, -0.32f, 0.44f);
        standLip.transform.localScale = new Vector3(0.82f, 0.24f, 0.12f);
        standLip.GetComponent<Renderer>().sharedMaterial = standMaterial;
    }

    private static void CreateHallShell(
        Transform root,
        Material parquetMaterial,
        Material wallCarpetMaterial,
        Material cornerFillMaterial,
        Material ceilingMaterial)
    {
        CreateBox(root, "BackWall", new Vector3(0f, 5.8f, 24.88f), new Vector3(35.2f, 13.933921f, 1.2f), parquetMaterial);
        CreateBox(root, "LeftWallLower", new Vector3(-14.5f, 4.6f, 12.5f), new Vector3(1.3f, 9.2f, 42.8f), wallCarpetMaterial);
        CreateBox(root, "RightWallLower", new Vector3(14.5f, 4.6f, 12.5f), new Vector3(1.3f, 9.2f, 42.8f), wallCarpetMaterial);
        CreateBox(root, "AudienceBackWallLower", new Vector3(0f, 4.2f, -18.8f), new Vector3(35.2f, 8.4f, 1.4f), wallCarpetMaterial);
        CreateBox(root, "AudienceBackWallUpper", new Vector3(0f, 11.4f, -18.8f), new Vector3(35.2f, 6.2f, 1.4f), wallCarpetMaterial);
        CreateBox(root, "AudienceLeftWallRear", new Vector3(-14.5f, 9.2f, -9.8f), new Vector3(1.3f, 18.4f, 18.8f), wallCarpetMaterial);
        CreateBox(root, "AudienceRightWallRear", new Vector3(14.5f, 9.2f, -9.8f), new Vector3(1.3f, 18.4f, 18.8f), wallCarpetMaterial);
        CreateBox(root, "AudienceLeftCornerFill", new Vector3(-14.5f, 11.2f, -18.5f), new Vector3(1.3f, 6.6f, 1.8f), cornerFillMaterial);
        CreateBox(root, "AudienceRightCornerFill", new Vector3(14.5f, 11.2f, -18.5f), new Vector3(1.3f, 6.6f, 1.8f), cornerFillMaterial);
        CreateBox(root, "LeftWallUpper", new Vector3(-14.5f, 10.8f, 12.5f), new Vector3(1.2f, 3.4f, 42.8f), wallCarpetMaterial);
        CreateBox(root, "RightWallUpper", new Vector3(14.5f, 10.8f, 12.5f), new Vector3(1.2f, 3.4f, 42.8f), wallCarpetMaterial);
        CreateBox(root, "CeilingStage", new Vector3(0f, 12.64f, 18.5f), new Vector3(29.8f, 1.4976f, 24f), ceilingMaterial);
        CreateBox(root, "CeilingAudience", new Vector3(0f, 13.8f, -4f), new Vector3(31.4f, 3.283113f, 31f), ceilingMaterial);
        CreateBox(root, "AudienceCeilingRear", new Vector3(0f, 13.8f, -18f), new Vector3(31.4f, 0.8f, 3f), ceilingMaterial);
        CreateBox(root, "BackTrim", new Vector3(0f, 1.5f, 24.46f), new Vector3(28.5f, 0.3f, 0.35f), parquetMaterial);
        CreateBox(root, "LeftTrim", new Vector3(-13.75f, 2.5f, 16.6f), new Vector3(0.3f, 1.8f, 30f), parquetMaterial);
        CreateBox(root, "RightTrim", new Vector3(13.75f, 2.5f, 16.6f), new Vector3(0.3f, 1.8f, 30f), parquetMaterial);

        CreateOrientedBox(root, "FrontRiser_1", new Vector3(6.855694f, 0.14f, 11.356255f), Quaternion.Euler(0f, -38f, 0f), new Vector3(2.4f, 0.28f, 2.5f), wallCarpetMaterial);
        CreateOrientedBox(root, "FrontRiser_2", new Vector3(4.9485693f, 0.14f, 13.155533f), Quaternion.Euler(0f, -55.333336f, 0f), new Vector3(2.4f, 0.28f, 2.5f), wallCarpetMaterial);
        CreateOrientedBox(root, "FrontRiser_3", new Vector3(2.591994f, 0.14f, 14.304913f), Quaternion.Euler(0f, -72.66667f, 0f), new Vector3(2.4f, 0.28f, 2.5f), wallCarpetMaterial);
        CreateOrientedBox(root, "FrontRiser_4", new Vector3(-0.00000038028907f, 0.14f, 14.7f), Quaternion.Euler(0f, -90f, 0f), new Vector3(2.4f, 0.28f, 2.5f), wallCarpetMaterial);
        CreateOrientedBox(root, "FrontRiser_5", new Vector3(-2.5919938f, 0.14f, 14.304913f), Quaternion.Euler(0f, -107.33333f, 0f), new Vector3(2.4f, 0.28f, 2.5f), wallCarpetMaterial);
        CreateOrientedBox(root, "FrontRiser_6", new Vector3(-4.948569f, 0.14f, 13.155534f), Quaternion.Euler(0f, -124.66667f, 0f), new Vector3(2.4f, 0.28f, 2.5f), wallCarpetMaterial);
        CreateOrientedBox(root, "FrontRiser_7", new Vector3(-6.855694f, 0.14f, 11.356255f), Quaternion.Euler(0f, -142f, 0f), new Vector3(2.4f, 0.28f, 2.5f), wallCarpetMaterial);

        CreateOrientedBox(root, "MidRiser_1", new Vector3(8.12854f, 0.25f, 13.84964f), Quaternion.Euler(0f, -44f, 0f), new Vector3(2.4f, 0.5f, 2.8f), wallCarpetMaterial);
        CreateOrientedBox(root, "MidRiser_2", new Vector3(5.7634807f, 0.25f, 15.719686f), Quaternion.Euler(0f, -59.333336f, 0f), new Vector3(2.4f, 0.5f, 2.8f), wallCarpetMaterial);
        CreateOrientedBox(root, "MidRiser_3", new Vector3(2.9881065f, 0.25f, 16.897762f), Quaternion.Euler(0f, -74.66667f, 0f), new Vector3(2.4f, 0.5f, 2.8f), wallCarpetMaterial);
        CreateOrientedBox(root, "MidRiser_4", new Vector3(-0.00000049393867f, 0.25f, 17.3f), Quaternion.Euler(0f, -90f, 0f), new Vector3(2.4f, 0.5f, 2.8f), wallCarpetMaterial);
        CreateOrientedBox(root, "MidRiser_5", new Vector3(-2.988106f, 0.25f, 16.897762f), Quaternion.Euler(0f, -105.33333f, 0f), new Vector3(2.4f, 0.5f, 2.8f), wallCarpetMaterial);
        CreateOrientedBox(root, "MidRiser_6", new Vector3(-5.7634797f, 0.25f, 15.7196865f), Quaternion.Euler(0f, -120.66667f, 0f), new Vector3(2.4f, 0.5f, 2.8f), wallCarpetMaterial);
        CreateOrientedBox(root, "MidRiser_7", new Vector3(-8.128539f, 0.25f, 13.849641f), Quaternion.Euler(0f, -136f, 0f), new Vector3(2.4f, 0.5f, 2.8f), wallCarpetMaterial);
    }

    private static void CreateAudienceBlocks(Transform root, Material seatMaterial, Material trimMaterial)
    {
        for (int row = 0; row < 20; row++)
        {
            float z = 0.5f - row * 0.95f;
            for (int col = 0; col < 28; col++)
            {
                float x = -13.5f + col * 1.0f;
                CreateBox(root, $"AudienceSeat_Main_{row}_{col}", new Vector3(x, 0.36f, z), new Vector3(0.62f, 0.72f, 0.62f), seatMaterial);
            }
        }

        for (int row = 0; row < 12; row++)
        {
            float z = -0.4f - row * 0.95f;
            for (int col = 0; col < 8; col++)
            {
                float x = -17.4f + col * 0.86f;
                CreateBox(root, $"AudienceSeat_Left_{row}_{col}", new Vector3(x, 0.36f, z), new Vector3(0.58f, 0.72f, 0.58f), seatMaterial);
            }
        }

        for (int row = 0; row < 12; row++)
        {
            float z = -0.4f - row * 0.95f;
            for (int col = 0; col < 8; col++)
            {
                float x = 17.4f - col * 0.86f;
                CreateBox(root, $"AudienceSeat_Right_{row}_{col}", new Vector3(x, 0.36f, z), new Vector3(0.58f, 0.72f, 0.58f), seatMaterial);
            }
        }

        CreateBox(root, "AudienceBarrier", new Vector3(0f, 0.78f, 5.2f), new Vector3(18f, 0.22f, 0.35f), trimMaterial);
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static GameObject CreateOrientedBox(
        Transform parent,
        string name,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Material material)
    {
        GameObject box = CreateBox(parent, name, position, scale, material);
        box.transform.rotation = rotation;
        return box;
    }

    private static StemAssignmentResult CollectStemAssignments()
    {
        List<AudioClip> allClips = AssetDatabase
            .FindAssets("t:AudioClip", new[] { "Assets/Audio" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<AudioClip>(path))
            .Where(clip => clip != null)
            .OrderBy(clip => clip.name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<AudioClip> strings = ExtractMatches(allClips, StringsKeywords);
        List<AudioClip> woodwinds = ExtractMatches(allClips, WoodwindsKeywords);
        List<AudioClip> brass = ExtractMatches(allClips, BrassKeywords);
        List<AudioClip> percussion = ExtractMatches(allClips, PercussionKeywords);

        List<AudioClip> unmatched = allClips
            .Except(strings)
            .Except(woodwinds)
            .Except(brass)
            .Except(percussion)
            .ToList();

        return new StemAssignmentResult(strings, woodwinds, brass, percussion, unmatched);
    }

    private static List<AudioClip> ExtractMatches(
        List<AudioClip> source,
        IReadOnlyList<string> keywords,
        Func<AudioClip, bool> extraFilter = null)
    {
        return source
            .Where(clip => keywords.Any(keyword => NameContains(clip.name, keyword)))
            .Where(clip => extraFilter == null || extraFilter(clip))
            .OrderBy(clip => clip.name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool NameContains(string value, params string[] candidates)
    {
        string normalized = value.Replace("-", " ").Replace("_", " ").ToLowerInvariant();
        return candidates.Any(candidate => normalized.Contains(candidate.Replace("_", " ").ToLowerInvariant()));
    }

    private static void LogAssignmentSummary(StemAssignmentResult result)
    {
        Debug.Log(
            $"Beethoven5Demo scene created at {ScenePath}.\n" +
            $"Auto-assigned stems:\n" +
            $"- Strings: {FormatClipList(result.Strings)}\n" +
            $"- Woodwinds: {FormatClipList(result.Woodwinds)}\n" +
            $"- Brass: {FormatClipList(result.Brass)}\n" +
            $"- Percussion: {FormatClipList(result.Percussion)}");

        if (result.Unmatched.Count > 0)
        {
            Debug.LogWarning("Unmatched audio clips left unassigned:\n- " + string.Join("\n- ", result.Unmatched.Select(clip => clip.name)));
        }

        if (result.Brass.Count == 0)
        {
            Debug.LogWarning("No brass stem was confidently matched in Assets/Audio. A Brass placeholder was still created, so you can assign that source manually in the scene if needed.");
        }
    }

    private static string FormatClipList(IReadOnlyList<AudioClip> clips)
    {
        return clips.Count == 0 ? "(none)" : string.Join(", ", clips.Select(clip => clip.name));
    }

    private static HudBuildData CreateHud()
    {
        GameObject canvasObject = new("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject hudRoot = new("HUD");
        hudRoot.transform.SetParent(canvasObject.transform, false);
        OrchestraHUD hud = hudRoot.AddComponent<OrchestraHUD>();

        TMP_Text songTitle = CreateScreenText(canvasObject.transform, "SongTitle", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), 30f, FontStyles.Bold);
        songTitle.text = "BEETHOVEN\nSYMPHONY NO. 5 - I. ALLEGRO CON BRIO";
        songTitle.alignment = TextAlignmentOptions.Center;

        TMP_Text controlsLeft = CreateScreenText(canvasObject.transform, "ControlsPanelLeft", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(22f, 22f), 15f, FontStyles.Normal);
        controlsLeft.alignment = TextAlignmentOptions.BottomLeft;
        controlsLeft.text = "[1] STRINGS\n[2] WOODWINDS\n[3] BRASS\n[4] PERCUSSION";

        TMP_Text controlsRight = CreateScreenText(canvasObject.transform, "ControlsPanelRight", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 22f), 15f, FontStyles.Normal);
        controlsRight.alignment = TextAlignmentOptions.BottomRight;
        controlsRight.text = "[SPACE] TUTTI\n[R] RESTART\n[ESC] RELEASE MOUSE";

        TMP_Text timeline = CreateScreenText(canvasObject.transform, "TimelineText", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 112f), 20f, FontStyles.Bold);
        timeline.alignment = TextAlignmentOptions.BottomRight;

        TMP_Text stateText = CreateScreenText(canvasObject.transform, "StateText", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 82f), 14f, FontStyles.Normal);
        stateText.alignment = TextAlignmentOptions.BottomRight;

        TMP_Text finishedText = CreateScreenText(canvasObject.transform, "FinishedText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 34f, FontStyles.Bold);
        finishedText.alignment = TextAlignmentOptions.Center;
        finishedText.text = "Performance Finished\nPress R to restart";
        finishedText.gameObject.SetActive(false);

        SerializedObject hudSerializedObject = new(hud);
        hudSerializedObject.FindProperty("timelineText").objectReferenceValue = timeline;
        hudSerializedObject.FindProperty("stateText").objectReferenceValue = stateText;
        hudSerializedObject.FindProperty("songTitleText").objectReferenceValue = songTitle;
        hudSerializedObject.FindProperty("controlsLeftText").objectReferenceValue = controlsLeft;
        hudSerializedObject.FindProperty("controlsRightText").objectReferenceValue = controlsRight;
        hudSerializedObject.FindProperty("finishedText").objectReferenceValue = finishedText;
        hudSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        return new HudBuildData(hud, finishedText);
    }

    private static TMP_Text CreateScreenText(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        float fontSize,
        FontStyles fontStyle)
    {
        GameObject textObject = new(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(anchorMax.x, anchorMin.y);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(360f, 120f);
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = new Color(0.97f, 0.97f, 0.95f);
        return text;
    }

    private static void AssignSection(SerializedObject managerSerializedObject, string propertyName, SectionBuildData sectionData)
    {
        SerializedProperty sectionProperty = managerSerializedObject.FindProperty(propertyName);
        sectionProperty.FindPropertyRelative("sectionName").stringValue = sectionData.SectionName.ToUpperInvariant();
        sectionProperty.FindPropertyRelative("visual").objectReferenceValue = sectionData.Visual;

        SerializedProperty sourcesProperty = sectionProperty.FindPropertyRelative("sources");
        sourcesProperty.arraySize = sectionData.Sources.Length;
        for (int i = 0; i < sectionData.Sources.Length; i++)
        {
            sourcesProperty.GetArrayElementAtIndex(i).objectReferenceValue = sectionData.Sources[i];
        }
    }

    private readonly struct SectionBuildData
    {
        public SectionBuildData(string sectionName, AudioSource[] sources, OrchestraSectionVisual visual)
        {
            SectionName = sectionName;
            Sources = sources;
            Visual = visual;
        }

        public string SectionName { get; }
        public AudioSource[] Sources { get; }
        public OrchestraSectionVisual Visual { get; }
    }

    private readonly struct HudBuildData
    {
        public HudBuildData(OrchestraHUD hud, TMP_Text finishedText)
        {
            Hud = hud;
            FinishedText = finishedText;
        }

        public OrchestraHUD Hud { get; }
        public TMP_Text FinishedText { get; }
    }

    private readonly struct StemAssignmentResult
    {
        public StemAssignmentResult(
            List<AudioClip> strings,
            List<AudioClip> woodwinds,
            List<AudioClip> brass,
            List<AudioClip> percussion,
            List<AudioClip> unmatched)
        {
            Strings = strings;
            Woodwinds = woodwinds;
            Brass = brass;
            Percussion = percussion;
            Unmatched = unmatched;
        }

        public List<AudioClip> Strings { get; }
        public List<AudioClip> Woodwinds { get; }
        public List<AudioClip> Brass { get; }
        public List<AudioClip> Percussion { get; }
        public List<AudioClip> Unmatched { get; }
    }
}
