using Library;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PPBlender : MonoBehaviour
{
	[SerializeField]
	private Volume volume1;

	[SerializeField]
	private Volume volume2;

	[SerializeField]
	private Material blendMat;

	private bool isBlending;

	private Texture2D lut1;

	private Texture2D lut2;

	private Texture2D resultLut;

	private Camera mainCamera;

	private Transform CameraTransform
	{
		get
		{
			if (mainCamera.IsNull())
			{
				mainCamera = Camera.main;
			}
			return mainCamera.transform;
		}
	}

	private void LateUpdate()
	{
		float num = CalculateBlendFactor();
		if (!isBlending && num > 0f)
		{
			isBlending = true;
			OnBlendStart();
		}
		else if (isBlending && num == 0f)
		{
			isBlending = false;
			OnBlendEnd();
		}
		if (isBlending)
		{
			BlendAndApplyLut(num);
		}
	}

	private void OnBlendStart()
	{
		lut1 = (volume1.profile.TryGet<ColorLookup>(out var component) ? ((Texture2D)component.texture.value) : null);
		lut2 = (volume2.profile.TryGet<ColorLookup>(out var component2) ? ((Texture2D)component2.texture.value) : null);
	}

	private void OnBlendEnd()
	{
		if ((bool)lut1 && (bool)lut2)
		{
			volume1.profile.TryGet<ColorLookup>(out var component);
			component.texture.value = lut1;
			volume2.profile.TryGet<ColorLookup>(out var component2);
			component2.texture.value = lut2;
		}
	}

	private float CalculateBlendFactor()
	{
		bool isPlayerInside;
		float num = ComputeWeight(volume1, CameraTransform.position, out isPlayerInside);
		if (isPlayerInside)
		{
			SetVolume1PriorityHigher(isPlayerInside);
		}
		float num2 = ComputeWeight(volume2, CameraTransform.position, out isPlayerInside);
		if (isPlayerInside)
		{
			SetVolume1PriorityHigher(!isPlayerInside);
		}
		if (num == 0f || num2 == 0f)
		{
			return 0f;
		}
		float num3 = num + num2;
		return num2 / num3;
	}

	private void BlendAndApplyLut(float blendFactor)
	{
		if ((bool)lut1 && (bool)lut2 && (bool)volume1 && (bool)volume2)
		{
			blendMat.SetTexture("_LUT1", lut1);
			blendMat.SetTexture("_LUT2", lut2);
			blendMat.SetFloat("_Blend", blendFactor);
			int width = lut1.width;
			int height = lut1.height;
			RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			temporary.filterMode = FilterMode.Bilinear;
			temporary.wrapMode = TextureWrapMode.Clamp;
			Graphics.Blit(null, temporary, blendMat);
			RenderTexture.active = temporary;
			resultLut = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: true);
			resultLut.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
			resultLut.Apply();
			RenderTexture.active = null;
			RenderTexture.ReleaseTemporary(temporary);
			resultLut.wrapMode = TextureWrapMode.Clamp;
			resultLut.filterMode = FilterMode.Bilinear;
			if (volume1.profile.TryGet<ColorLookup>(out var component) && volume2.profile.TryGet<ColorLookup>(out var component2))
			{
				component.texture.value = resultLut;
				component.active = true;
				component2.texture.value = resultLut;
				component2.active = true;
			}
		}
	}

	private void SetVolume1PriorityHigher(bool value)
	{
		if (!isBlending)
		{
			volume1.priority = (value ? 1 : 0);
			volume2.priority = ((!value) ? 1 : 0);
		}
	}

	private static float ComputeWeight(Volume volume, Vector3 triggerPos, out bool isPlayerInside)
	{
		isPlayerInside = false;
		if (volume.IsNull())
		{
			return 0f;
		}
		VolumeProfile obj = (volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile);
		if (!volume.gameObject.activeInHierarchy)
		{
			return 0f;
		}
		if (!volume.enabled || obj.IsNull() || volume.weight <= 0f)
		{
			return 0f;
		}
		float num = Mathf.Clamp01(volume.weight);
		if (volume.isGlobal)
		{
			return num;
		}
		if (!volume.TryGetComponent<BoxCollider>(out var component))
		{
			return 0f;
		}
		if (!component.enabled)
		{
			return 0f;
		}
		float num2 = float.PositiveInfinity;
		float sqrMagnitude = (component.ClosestPoint(triggerPos) - triggerPos).sqrMagnitude;
		if (sqrMagnitude == 0f)
		{
			isPlayerInside = true;
		}
		if (sqrMagnitude < num2)
		{
			num2 = sqrMagnitude;
		}
		float num3 = volume.blendDistance * volume.blendDistance;
		if (num2 > num3)
		{
			num = 0f;
		}
		else if (num3 > 0f)
		{
			num *= 1f - num2 / num3;
		}
		return num;
	}
}
