using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace SunnysideIsland.Building.Editor
{
    /// <summary>
    /// Campfire Prefab 자동 생성 에디터 툴
    /// </summary>
    public class CampfirePrefabCreator : EditorWindow
    {
        private GameObject _stoneBasePrefab;
        private Sprite _stoneBaseSprite;
        private Sprite _woodSprite;
        
        [MenuItem("Sunnyside Island/Create Campfire Prefab")]
        public static void ShowWindow()
        {
            GetWindow<CampfirePrefabCreator>("Campfire Creator");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Campfire Prefab Creator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            GUILayout.Label("Sprites (Optional)", EditorStyles.boldLabel);
            _stoneBaseSprite = EditorGUILayout.ObjectField("Stone Base Sprite", _stoneBaseSprite, typeof(Sprite), false) as Sprite;
            _woodSprite = EditorGUILayout.ObjectField("Wood Sprite", _woodSprite, typeof(Sprite), false) as Sprite;
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Create Campfire Prefab", GUILayout.Height(40)))
            {
                CreateCampfirePrefab();
            }
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Instructions:", EditorStyles.boldLabel);
            GUILayout.Label("1. Click the button above to create prefab");
            GUILayout.Label("2. Assign sprites if you have custom ones");
            GUILayout.Label("3. Save the prefab to Assets/Prefabs/Building/");
            GUILayout.Label("4. Add to BuildingDatabase");
        }
        
        private void CreateCampfirePrefab()
        {
            // Create parent object
            GameObject campfire = new GameObject("Campfire");
            
            // Add Campfire component
            Campfire campfireComponent = campfire.AddComponent<Campfire>();
            
            // Create Stone Base
            GameObject stoneBase = new GameObject("StoneBase");
            stoneBase.transform.SetParent(campfire.transform);
            stoneBase.transform.localPosition = Vector3.zero;
            
            SpriteRenderer stoneRenderer = stoneBase.AddComponent<SpriteRenderer>();
            if (_stoneBaseSprite != null)
            {
                stoneRenderer.sprite = _stoneBaseSprite;
            }
            stoneRenderer.sortingOrder = 1;
            
            // Create Fire Particles
            GameObject fireParticles = new GameObject("FireParticles");
            fireParticles.transform.SetParent(campfire.transform);
            fireParticles.transform.localPosition = new Vector3(0, 0.15f, 0);
            
            ParticleSystem particleSystem = fireParticles.AddComponent<ParticleSystem>();
            CampfireParticles campfireParticles = fireParticles.AddComponent<CampfireParticles>();
            
            // Configure particle system
            ConfigureParticleSystem(particleSystem);
            
            // Create Spark Particles
            GameObject sparkParticles = new GameObject("SparkParticles");
            sparkParticles.transform.SetParent(campfire.transform);
            sparkParticles.transform.localPosition = new Vector3(0, 0.2f, 0);
            
            ParticleSystem sparkSystem = sparkParticles.AddComponent<ParticleSystem>();
            SparkParticles sparkComponent = sparkParticles.AddComponent<SparkParticles>();
            ConfigureSparkParticleSystem(sparkSystem);
            
            // Create Smoke Particles
            GameObject smokeParticles = new GameObject("SmokeParticles");
            smokeParticles.transform.SetParent(campfire.transform);
            smokeParticles.transform.localPosition = new Vector3(0, 0.3f, 0);
            
            ParticleSystem smokeSystem = smokeParticles.AddComponent<ParticleSystem>();
            SmokeParticles smokeComponent = smokeParticles.AddComponent<SmokeParticles>();
            ConfigureSmokeParticleSystem(smokeSystem);
            
            // Create Campfire Light
            GameObject campfireLight = new GameObject("CampfireLight");
            campfireLight.transform.SetParent(campfire.transform);
            campfireLight.transform.localPosition = new Vector3(0, 0.3f, 0);
            
            Light2D light2D = campfireLight.AddComponent<Light2D>();
            ConfigureLight2D(light2D);
            
            // Add Particle Connector (main controller)
            CampfireParticleConnector connector = campfire.AddComponent<CampfireParticleConnector>();
            
            // Setup Connector references
            SerializedObject serializedConnector = new SerializedObject(connector);
            SetField(serializedConnector, "_mainFireParticles", particleSystem);
            SetField(serializedConnector, "_sparkParticles", sparkSystem);
            SetField(serializedConnector, "_smokeParticles", smokeSystem);
            SetField(serializedConnector, "_campfireLight", light2D);
            SetField(serializedConnector, "_flickerRange", 0.15f);
            SetField(serializedConnector, "_flickerSpeed", 4f);
            serializedConnector.ApplyModifiedProperties();
            
            // Add BoxCollider2D for interaction
            BoxCollider2D collider = campfire.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.8f);
            collider.isTrigger = true;
            
            // Setup Campfire component references via SerializedObject
            SerializedObject serializedCampfire = new SerializedObject(campfireComponent);
            
            // Find fields and set values
            SetField(serializedCampfire, "_campfireParticles", campfireParticles);
            SetField(serializedCampfire, "_campfireLight", light2D);
            SetField(serializedCampfire, "_stoneBaseRenderer", stoneRenderer);
            SetField(serializedCampfire, "_woodCost", 2);
            SetField(serializedCampfire, "_totalDurationHours", 6f);
            SetField(serializedCampfire, "_lightIntensity", 0.8f);
            
            serializedCampfire.ApplyModifiedProperties();
            
            // Select in editor
            Selection.activeGameObject = campfire;
            EditorGUIUtility.PingObject(campfire);
            
            Debug.Log("[CampfirePrefabCreator] Campfire prefab with enhanced particles created! Save it to Assets/Prefabs/Building/");
        }
        
        private void ConfigureParticleSystem(ParticleSystem ps)
        {
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var colorOverLifetime = ps.colorOverLifetime;
            var sizeOverLifetime = ps.sizeOverLifetime;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            
            // Main module
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.7f, 0.2f, 1f),  // Yellow-orange
                new Color(1f, 0.4f, 0.1f, 1f)   // Orange-red
            );
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.3f; // Float upward
            
            // Emission
            emission.enabled = true;
            emission.rateOverTime = 25f;
            
            // Shape (Cone from bottom)
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.1f;
            shape.radiusThickness = 0f;
            shape.position = Vector3.zero;
            shape.rotation = new Vector3(-90f, 0, 0); // Point upward
            
            // Color over lifetime - fade out
            colorOverLifetime.enabled = true;
            Gradient colorGradient = new Gradient();
            colorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.8f, 0.2f, 0.05f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);
            
            // Size over lifetime - shrink
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(0.5f, 0.8f);
            sizeCurve.AddKey(1f, 0.3f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            // Renderer
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.OldestInFront;
            
            // Create a simple circular texture for particles
            Texture2D particleTexture = CreateCircleTexture(32);
            Material particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            particleMaterial.SetTexture("_MainTex", particleTexture);
            particleMaterial.SetFloat("_ColorMode", 0); // Multiply
            renderer.material = particleMaterial;
        }
        
        private void ConfigureSparkParticleSystem(ParticleSystem ps)
        {
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            
            // Main - short bursts
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            main.startSize = 0.04f;
            main.startColor = new Color(1f, 0.9f, 0.4f, 1f);
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.8f; // Fall down
            
            // Emission - bursts
            emission.enabled = true;
            emission.rateOverTime = 0;
            
            // Shape
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;
            shape.position = new Vector3(0, 0.2f, 0);
            
            // Renderer
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.OldestInFront;
            
            // Small bright texture
            Texture2D sparkTexture = CreateCircleTexture(16);
            Material sparkMaterial = new Material(Shader.Find("Sprites/Default"));
            sparkMaterial.SetTexture("_MainTex", sparkTexture);
            renderer.material = sparkMaterial;
        }
        
        private void ConfigureSmokeParticleSystem(ParticleSystem ps)
        {
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var colorOverLifetime = ps.colorOverLifetime;
            var sizeOverLifetime = ps.sizeOverLifetime;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            
            // Main - slow rising smoke
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
            main.startSpeed = 0f;
            main.startSize = 0.25f;
            main.maxParticles = 25;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.05f;
            
            // Smoke color gradient
            Gradient smokeGrad = new Gradient();
            smokeGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 0f),
                    new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 0.6f),
                    new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.3f, 0f),
                    new GradientAlphaKey(0.1f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            main.startColor = new ParticleSystem.MinMaxGradient(smokeGrad);
            
            // Emission
            emission.enabled = true;
            emission.rateOverTime = 0.15f;
            
            // Shape
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;
            shape.position = new Vector3(0, 0.3f, 0);
            
            // Color over lifetime
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(smokeGrad);
            
            // Size over lifetime - grow bigger
            sizeOverLifetime.enabled = true;
            AnimationCurve smokeSizeCurve = new AnimationCurve();
            smokeSizeCurve.AddKey(0f, 0.5f);
            smokeSizeCurve.AddKey(0.3f, 0.8f);
            smokeSizeCurve.AddKey(0.7f, 1.3f);
            smokeSizeCurve.AddKey(1f, 1.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, smokeSizeCurve);
            
            // Renderer
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.OldestInFront;
            renderer.sortingOrder = 5;
            
            // Soft texture
            Texture2D smokeTexture = CreateCircleTexture(64);
            Material smokeMaterial = new Material(Shader.Find("Sprites/Default"));
            smokeMaterial.SetTexture("_MainTex", smokeTexture);
            renderer.material = smokeMaterial;
        }
        
        private void ConfigureLight2D(Light2D light2D)
        {
            light2D.lightType = Light2D.LightType.Point;
            light2D.intensity = 0.8f;
            light2D.pointLightOuterAngle = 360f;
            light2D.pointLightInnerAngle = 360f;
            light2D.pointLightOuterRadius = 3f;
            light2D.pointLightInnerRadius = 0.5f;
            light2D.color = new Color(1f, 0.6f, 0.2f, 1f); // Warm orange
            light2D.falloffIntensity = 0.5f;
            light2D.shadowIntensity = 0.3f;
            light2D.shadowVolumeIntensity = 0f;
            light2D.shapeLightFalloffSize = 0.5f;
        }
        
        private Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            
            int center = size / 2;
            float radius = size / 2f - 2f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    
                    if (distance < radius)
                    {
                        float alpha = 1f - (distance / radius);
                        alpha = Mathf.Pow(alpha, 0.5f); // Soften the edge
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return texture;
        }
        
        private void SetField(SerializedObject obj, string fieldName, object value)
        {
            SerializedProperty prop = obj.FindProperty(fieldName);
            if (prop != null)
            {
                if (value is int) prop.intValue = (int)value;
                else if (value is float) prop.floatValue = (float)value;
                else if (value is bool) prop.boolValue = (bool)value;
                else if (value is string) prop.stringValue = (string)value;
                else if (value is Object) prop.objectReferenceValue = (Object)value;
            }
        }
    }
}
