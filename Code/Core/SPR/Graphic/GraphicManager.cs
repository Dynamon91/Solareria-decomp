using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Library;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SPR.Graphic;

public class GraphicManager : Manager
{
	public enum RenderFeatureName
	{
		ShockWave,
		Focus,
		ColorVignette,
		TimeStop,
		MoldinsInsanity
	}

	private class HealthVignette
	{
		public const float Threshold = 0.3f;

		public static readonly Color Color = new Color(0.4f, 0f, 0f);

		public const float RadiusNormal = 1f;

		public const float HitRadiusNormal = 0.4f;

		public const float RadiusLowHealth = 0.3f;

		public const float HitRadiusLowHealth = 0.1f;
	}

	[SerializeField]
	private ParryActionDirector parryActionDirector;

	private static readonly int PlayerHeadColorProperty = Shader.PropertyToID("_Color");

	private static readonly int PlayerHeadDistortionProperty = Shader.PropertyToID("_DistortionAmount");

	private readonly Color normalHeadColor = new Color(0f, 0.64705884f, 0.7490196f);

	private readonly Color darkHeadColor = new Color(0f, 0f, 0f);

	private const float NormalHeadDistortion = 1.6f;

	private const float DarkHeadDistortion = 1f;

	private const float VignetteVibrationDuration = 1f;

	private Material healthVignetteMaterial;

	private Sequence healthVignetteSequence;

	private static readonly int VignetteRadius = Shader.PropertyToID("_VignetteRadius");

	private Volume postProcessingVolume;

	private Material playerHeadMaterial;

	private float playerHeadColorIntensity;

	private Sequence vignetteFadeSequence;

	private Material HealthVignetteMaterial
	{
		get
		{
			if (healthVignetteMaterial.IsNull())
			{
				healthVignetteMaterial = GraphicHelper.GetRenderFeatureMaterial(RenderFeatureName.ColorVignette);
			}
			return healthVignetteMaterial;
		}
	}

	public Volume PPVolume
	{
		get
		{
			if (!postProcessingVolume.IsNull())
			{
				return postProcessingVolume;
			}
			Volume[] array = UnityEngine.Object.FindObjectsOfType<Volume>();
			int num = int.MinValue;
			Volume[] array2 = array;
			foreach (Volume volume in array2)
			{
				if (volume.priority > (float)num)
				{
					postProcessingVolume = volume;
					num = (int)volume.priority;
				}
			}
			return postProcessingVolume;
		}
	}

	private Material PlayerHeadMaterial
	{
		get
		{
			if (playerHeadMaterial.IsNull())
			{
				playerHeadMaterial = Player.SkeletonAnimation.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterials[2];
				playerHeadColorIntensity = PlayerHeadMaterial.GetColor(PlayerHeadColorProperty).GetIntensity();
			}
			return playerHeadMaterial;
		}
	}

	private static PlayerMover Player => Game.Instance.Player.mover;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	private static SettingData SettingData => Game.Instance.SettingData;

	public ParryActionDirector ParryActionDirector => parryActionDirector;

	protected override void OnInitialize()
	{
		DisableAllRenderFeature();
	}

	private void Update()
	{
		if (Game.IsInitialized && Game.Instance.CurrentFieldID.SectorName != SectorName.None)
		{
			UpdateHealthVignette();
		}
	}

	public override UniTask OnSceneLoaded(Scene loadedScene)
	{
		DisableAllRenderFeature();
		return default(UniTask);
	}

	private void OnDisable()
	{
		DisableAllRenderFeature();
	}

	public void DisableAllRenderFeature()
	{
		foreach (RenderFeatureName value in Enum.GetValues(typeof(RenderFeatureName)))
		{
			GraphicHelper.SetRenderFeatureActive(value, value: false);
		}
	}

	private void StartHealthVignette()
	{
	}

	private void UpdateHealthVignette()
	{
		GraphicHelper.SetRenderFeatureActive(RenderFeatureName.ColorVignette, value: true);
		HealthVignetteMaterial.SetColor("_Color", HealthVignette.Color);
		if (vignetteFadeSequence == null && (healthVignetteSequence == null || !healthVignetteSequence.IsActive()))
		{
			float value = ((GetHpPortion() > 0.3f) ? 1f : 0.3f);
			HealthVignetteMaterial.SetFloat(VignetteRadius, value);
		}
	}

	public void ApplyHitEffect()
	{
		if (SettingData.HealthVignette)
		{
			healthVignetteSequence?.Kill();
			bool flag = GetHpPortion() <= 0.3f;
			float normal = (flag ? 0.3f : 1f);
			float endValue = (flag ? 0.1f : 0.4f);
			healthVignetteSequence = DOTween.Sequence();
			healthVignetteSequence.AppendCallback(delegate
			{
				HealthVignetteMaterial.SetFloat(VignetteRadius, normal);
			}).Append(HealthVignetteMaterial.DOFloat(endValue, VignetteRadius, 0.08f)).AppendInterval(0.26f)
				.Append(HealthVignetteMaterial.DOFloat(normal, VignetteRadius, 0.18f));
		}
	}

	public async UniTask HideVignetteAsync(float duration)
	{
		vignetteFadeSequence?.Kill();
		healthVignetteSequence?.Kill();
		float normal = 1f;
		vignetteFadeSequence = DOTween.Sequence();
		await vignetteFadeSequence.Append(HealthVignetteMaterial.DOFloat(normal, VignetteRadius, duration)).AppendCallback(delegate
		{
			HealthVignetteMaterial.SetFloat(VignetteRadius, normal);
		}).WaitAsync();
	}

	public async UniTask ShowVignetteAsync(float duration)
	{
		vignetteFadeSequence.Kill();
		float hpPortion = GetHpPortion();
		float radius = ((hpPortion > 0.3f) ? 1f : 0.3f);
		vignetteFadeSequence = DOTween.Sequence();
		await vignetteFadeSequence.Append(HealthVignetteMaterial.DOFloat(radius, VignetteRadius, duration)).AppendCallback(delegate
		{
			HealthVignetteMaterial.SetFloat(VignetteRadius, radius);
			vignetteFadeSequence = null;
		}).WaitAsync();
	}

	private static float GetHpPortion()
	{
		if (!SettingData.HealthVignette)
		{
			return 1f;
		}
		return (float)PlayerStat.GetStat<Health>().CurrentHealth / (float)PlayerStat.GetStat<Health>().MaxHealthWithDark;
	}

	public void SetTimeStopFeature(bool active)
	{
		GraphicHelper.SetRenderFeatureActive(RenderFeatureName.TimeStop, active);
	}

	private void UpdateDarknessVignette()
	{
		float num = PlayerStat.GetStat<Health>().Darkness / (float)PlayerStat.GetStat<Health>().MaxHealth;
		if (PPVolume.profile.TryGet<Vignette>(out var component))
		{
			if (num <= 0f)
			{
				component.active = false;
				PlayerHeadMaterial.SetColor(PlayerHeadColorProperty, normalHeadColor.ApplyIntensity(playerHeadColorIntensity));
				PlayerHeadMaterial.SetFloat(PlayerHeadDistortionProperty, 1.6f);
				return;
			}
			component.active = true;
			Vector2 normalizedScreenPoint = GraphicHelper.GetNormalizedScreenPoint(Player.VisualCenter);
			float x = CalculateVignetteIntensity(num);
			component.center.Override(normalizedScreenPoint);
			component.intensity.Override(x);
			component.smoothness.Override(1f);
			Color color = new Color(Mathf.Lerp(normalHeadColor.r, darkHeadColor.r, num), Mathf.Lerp(normalHeadColor.g, darkHeadColor.g, num), Mathf.Lerp(normalHeadColor.b, darkHeadColor.b, num), 1f);
			PlayerHeadMaterial.SetColor(PlayerHeadColorProperty, color.ApplyIntensity(playerHeadColorIntensity));
			PlayerHeadMaterial.SetFloat(PlayerHeadDistortionProperty, Mathf.Lerp(1.6f, 1f, num));
		}
	}

	private float CalculateVignetteIntensity(float darkPortion)
	{
		float num = -0.2f * Mathf.Abs(Time.time % 1f - 0.5f);
		return Mathf.Max(0f, 0.7f * darkPortion + num);
	}

	private float SafeSmoothDamp(float current, float target, ref float currentVelocity, float smoothTime)
	{
		float deltaTime = Time.deltaTime;
		float deltaTime2 = ((deltaTime > 0f) ? deltaTime : (1f / 60f));
		return Mathf.SmoothDamp(current, target, ref currentVelocity, smoothTime, float.PositiveInfinity, deltaTime2);
	}
}
