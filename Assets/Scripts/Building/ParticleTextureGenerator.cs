using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SunnysideIsland.Building
{
    /// <summary>
    /// 파티클 텍스처 생성 유틸리티
    /// 런타임 또는 에디터에서 원형 파티클 텍스처 생성
    /// </summary>
    public static class ParticleTextureGenerator
    {
        /// <summary>
        /// 원형 소프트 파티클 텍스처 생성
        /// </summary>
        public static Texture2D CreateCircleTexture(int size = 64, bool softEdge = true)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    
                    if (distance < radius)
                    {
                        float alpha;
                        if (softEdge)
                        {
                            // Soft gradient edge
                            alpha = 1f - (distance / radius);
                            alpha = Mathf.Pow(alpha, 0.7f);
                        }
                        else
                        {
                            // Hard edge with small fade
                            float edgeWidth = radius * 0.1f;
                            alpha = distance > (radius - edgeWidth) ? 
                                (radius - distance) / edgeWidth : 1f;
                        }
                        
                        pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
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
        
        /// <summary>
        /// 불꽃 모양 텍스처 생성 (위쪽이 뾰족한 형태)
        /// </summary>
        public static Texture2D CreateFlameTexture(int size = 64)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, 0f); // Bottom center
            float maxRadius = size / 2f - 2f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    
                    // Flame shape calculation
                    float normalizedHeight = (float)y / size;
                    float widthAtHeight = maxRadius * (1f - normalizedHeight * 0.6f); // Taper toward top
                    
                    // Add some waviness
                    float wave = Mathf.Sin(normalizedHeight * Mathf.PI * 3f) * size * 0.05f * normalizedHeight;
                    
                    Vector2 flameCenter = new Vector2(center.x + wave, y);
                    float distance = Vector2.Distance(pos, flameCenter);
                    
                    if (distance < widthAtHeight)
                    {
                        float alpha = 1f - (distance / widthAtHeight);
                        alpha = Mathf.Pow(alpha, 0.8f);
                        
                        // Fade out at top
                        alpha *= (1f - normalizedHeight * 0.5f);
                        
                        pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
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
        
        /// <summary>
        /// 파티클용 머티리얼 생성
        /// </summary>
        public static Material CreateParticleMaterial(Texture2D texture, string shaderName = "Particles/Standard Unlit")
        {
            Material material = new Material(Shader.Find(shaderName));
            material.SetTexture("_MainTex", texture);
            material.SetFloat("_ColorMode", 0); // Multiply
            material.SetFloat("_InvFade", 1f);
            
            return material;
        }
        
        /// <summary>
        /// Sprites/Default 머티리얼 생성 (간단한 파티클용)
        /// </summary>
        public static Material CreateSimpleParticleMaterial(Texture2D texture)
        {
            Material material = new Material(Shader.Find("Sprites/Default"));
            material.SetTexture("_MainTex", texture);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            
            return material;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 텍스처를 에셋으로 저장
        /// </summary>
        [MenuItem("Sunnyside Island/Generate Particle Textures")]
        public static void GenerateAndSaveTextures()
        {
            string path = "Assets/Textures/Particles/";
            
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = "Assets/Textures";
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    AssetDatabase.CreateFolder("Assets", "Textures");
                }
                AssetDatabase.CreateFolder(parent, "Particles");
            }
            
            // Create and save circle texture
            Texture2D circleTex = CreateCircleTexture(64, true);
            byte[] circleBytes = circleTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path + "ParticleCircle.png", circleBytes);
            AssetDatabase.ImportAsset(path + "ParticleCircle.png");
            
            // Create and save flame texture
            Texture2D flameTex = CreateFlameTexture(64);
            byte[] flameBytes = flameTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path + "ParticleFlame.png", flameBytes);
            AssetDatabase.ImportAsset(path + "ParticleFlame.png");
            
            AssetDatabase.Refresh();
            
            Debug.Log("[ParticleTextureGenerator] Particle textures generated and saved!");
            EditorUtility.DisplayDialog("Success", "Particle textures generated!\nLocation: " + path, "OK");
        }
#endif
    }
}
