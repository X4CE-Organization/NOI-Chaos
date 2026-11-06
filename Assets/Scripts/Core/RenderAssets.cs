using UnityEngine;

namespace NOIChaos
{
    /// <summary>
    /// Runtime-created materials must reference a serialized template. Otherwise
    /// Unity can strip the Standard shader from player builds and everything
    /// renders with the magenta error material.
    /// </summary>
    public static class RenderAssets
    {
        private const string StandardMaterialPath = "NOI/Standard";
        private const string ParticleMaterialPath = "NOI/Particle";

        private static Material standardTemplate;
        private static Material particleTemplate;

        public static Material CreateStandard(Color color, float metallic, float gloss, float emission = 0f)
        {
            Material material = CreateFromTemplate(ref standardTemplate, StandardMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                {
                    Debug.LogError("[NOI Chaos] Standard material template is missing from Resources.");
                    return null;
                }
                material = new Material(shader);
            }

            material.name = "NOI runtime standard";
            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", gloss);
            if (emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
            return material;
        }

        public static Material CreateParticleMaterial(Color color)
        {
            Material material = CreateFromTemplate(ref particleTemplate, ParticleMaterialPath);
            if (material == null)
            {
                return null;
            }

            material.name = "NOI runtime particles";
            material.color = color;
            return material;
        }

        private static Material CreateFromTemplate(ref Material cache, string resourcePath)
        {
            if (cache == null)
            {
                cache = Resources.Load<Material>(resourcePath);
            }
            return cache != null ? new Material(cache) : null;
        }
    }
}
