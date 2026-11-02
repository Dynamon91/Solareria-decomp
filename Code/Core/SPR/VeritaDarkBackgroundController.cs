using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SPR;

public class VeritaDarkBackgroundController : MonoBehaviour
{
	[SerializeField]
	private float fadeDuration;

	[SerializeField]
	private Volume globalVolume;

	[SerializeField]
	private Light2D parallaxLight;

	[Header("Values")]
	[SerializeField]
	private float postExposureDefault;

	[SerializeField]
	private float postExposureDark;

	[SerializeField]
	private float contrastDefault;

	[SerializeField]
	private float contrastDark;

	[SerializeField]
	private float contributionDefault;

	[SerializeField]
	private float contributionDark;

	[SerializeField]
	private float lightIntensityDefault;

	[SerializeField]
	private float lightIntensityDark;

	private SpriteRenderer spriteRenderer;

	private VolumeProfile volumeProfile;

	private MaterialPropertyBlock materialPropertyBlock;

	private const string FadeProperty = "_FadeAmount";

	private static readonly int FadePropertyID = Shader.PropertyToID("_FadeAmount");

	private float timer = float.PositiveInfinity;

	private bool isOn;

	private static float GetTargetValue(bool isActive)
	{
		if (!isActive)
		{
			return 1f;
		}
		return -0.1f;
	}

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		volumeProfile = globalVolume.profile;
		materialPropertyBlock = new MaterialPropertyBlock();
		SetProperty(GetTargetValue(isActive: false));
	}

	private void Update()
	{
		if (!(timer > fadeDuration))
		{
			SetProperty(Mathf.Lerp(GetTargetValue(!isOn), GetTargetValue(isOn), timer / fadeDuration));
			timer += Time.deltaTime / Game.GetTimeScale();
			if (timer >= fadeDuration)
			{
				SetProperty(GetTargetValue(isOn));
			}
		}
	}

	public void SetActive(bool isActive, bool instant = false)
	{
		isOn = isActive;
		if (instant)
		{
			SetProperty(GetTargetValue(isOn));
			timer = float.PositiveInfinity;
		}
		else
		{
			SetProperty(GetTargetValue(!isOn));
			timer = 0f;
		}
	}

	private void SetProperty(float value)
	{
		materialPropertyBlock.Clear();
		materialPropertyBlock.SetFloat(FadePropertyID, value);
		spriteRenderer.SetPropertyBlock(materialPropertyBlock);
		float t = (1f - value) / 1.1f;
		if (volumeProfile.TryGet<ColorAdjustments>(out var component))
		{
			component.postExposure.Interp(postExposureDefault, postExposureDark, t);
			component.contrast.Interp(contrastDefault, contrastDark, t);
		}
		if (volumeProfile.TryGet<ColorLookup>(out var component2))
		{
			component2.contribution.Interp(contributionDefault, contributionDark, t);
		}
		parallaxLight.intensity = Mathf.Lerp(lightIntensityDefault, lightIntensityDark, t);
		DebugLog.GeneralLog($"{component.postExposure}, {component.contrast}, {component2.contribution}, {parallaxLight.intensity}");
	}
}
