using UnityEngine;


namespace AlignedGames

{

    public class LightBlinkBehaviour : MonoBehaviour
    {
        public float interval = 1f; // Time for one full blink cycle (BaseColor <-> FadeColor)

        public Color BaseColor = Color.white;
        public Color FadeColor = Color.yellow;

        public float MinIntensity = 0.25f;
        public float MaxIntensity = 0.8f;

        public Light[] targetLights;
        public MeshRenderer[] targetMeshes;

        private float timer;

        void Update()
        {
            timer += Time.deltaTime;
            float t = Mathf.PingPong(timer / interval, 1f);

            Color lerpedColor = Color.Lerp(BaseColor, FadeColor, t);
            float lerpedIntensity = Mathf.Lerp(MinIntensity, MaxIntensity, t);
            float lerpedAlpha = Mathf.Lerp(0.2f, 0.7f, t);

            // Update lights
            foreach (var light in targetLights)
            {
                if (light != null)
                {
                    light.color = lerpedColor;
                    light.intensity = lerpedIntensity;
                }
            }

            // Update mesh materials
            foreach (var mesh in targetMeshes)
            {
                if (mesh != null && mesh.material != null)
                {
                    // Base color with alpha
                    Color baseWithAlpha = lerpedColor;
                    baseWithAlpha.a = lerpedAlpha;

                    // Ensure emission is enabled
                    mesh.material.EnableKeyword("_EMISSION");
                    mesh.material.SetColor("_EmissionColor", lerpedColor * lerpedIntensity / 2);

                    // Set alpha on material color
                    mesh.material.color = baseWithAlpha;

                    // Ensure shader supports transparency
                    mesh.material.SetFloat("_Mode", 3); // Set rendering mode to transparent
                    mesh.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mesh.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mesh.material.SetInt("_ZWrite", 0);
                    mesh.material.DisableKeyword("_ALPHATEST_ON");
                    mesh.material.EnableKeyword("_ALPHABLEND_ON");
                    mesh.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mesh.material.renderQueue = 3000;
                }
            }
        }
    }

}