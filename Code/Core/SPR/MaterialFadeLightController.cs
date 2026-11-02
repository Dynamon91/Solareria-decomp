using Library;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SPR;

public class MaterialFadeLightController : MonoBehaviour
{
	private MeshRenderer meshRenderer;

	private Light2D light2D;

	private MaterialPropertyBlock materialPropertyBlock;

	private float alpha;

	private const string FadeProperty = "_Alpha";

	private float initialIntensity;

	private static readonly int FadePropertyID = Shader.PropertyToID("_Alpha");

	private bool hasFade;

	private void Awake()
	{
		meshRenderer = GetComponentInChildren<MeshRenderer>();
		light2D = GetComponentInChildren<Light2D>();
		materialPropertyBlock = new MaterialPropertyBlock();
		if (!light2D.IsNull())
		{
			initialIntensity = light2D.intensity;
		}
	}

	private void LateUpdate()
	{
		materialPropertyBlock.Clear();
		meshRenderer.GetPropertyBlock(materialPropertyBlock);
		if (!light2D.IsNull())
		{
			if (materialPropertyBlock.isEmpty)
			{
				light2D.intensity = initialIntensity;
				return;
			}
			alpha = materialPropertyBlock.GetFloat(FadePropertyID);
			light2D.intensity = initialIntensity * Mathf.Clamp01(alpha);
		}
	}
}
