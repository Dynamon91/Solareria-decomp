using Spine.Unity.Examples;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SPR;

public class RenderTextureAlphaLightController : MonoBehaviour
{
	private SkeletonRenderTexture renderTextureComponent;

	private Light2D light2D;

	private float alpha;

	private float initialIntensity;

	private void Awake()
	{
		renderTextureComponent = GetComponentInChildren<SkeletonRenderTexture>();
		light2D = GetComponentInChildren<Light2D>();
		initialIntensity = light2D.intensity;
		LateUpdate();
	}

	private void LateUpdate()
	{
		alpha = renderTextureComponent.color.a;
		light2D.intensity = initialIntensity * Mathf.Clamp01(alpha);
	}
}
