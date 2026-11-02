using Coffee.UIEffects;
using FMODUnity;
using Library;
using SPR.Field;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace SPR;

public class VeritaSpaceController : MonoBehaviour
{
	[SerializeField]
	private Light2D globalLight;

	[SerializeField]
	private Color lightDarkColor;

	[SerializeField]
	private float dissolveDuration;

	[SerializeField]
	private Texture transitionBrightToDark;

	[SerializeField]
	private Texture transitionDarkToBright;

	[SerializeField]
	private EventReference enableEvent;

	[SerializeField]
	private EventReference disableEvent;

	private Verita verita;

	private Camera spaceCamera;

	private RawImage textureRenderer;

	private RectTransform textureTransform;

	private UIDissolve uiDissolve;

	private Camera mainCamera;

	private Area myArea;

	private bool isDissolving;

	private bool dissolveTargetState;

	private Color lightDefaultColor;

	private int overlappingMask;

	private Vector2 cameraAnchorMin;

	private bool targetTop;

	private Verita Verita => verita ?? (verita = base.transform.parent.GetComponentInChildren<Verita>(includeInactive: true));

	private Camera SpaceCamera => spaceCamera ?? (spaceCamera = GetComponentInChildren<Camera>(includeInactive: true));

	private RawImage TextureRenderer => textureRenderer ?? (textureRenderer = GetComponentInChildren<RawImage>(includeInactive: true));

	private RectTransform TextureTransform => textureTransform ?? (textureTransform = TextureRenderer.GetComponent<RectTransform>());

	private UIDissolve UIDissolve => uiDissolve ?? (uiDissolve = GetComponentInChildren<UIDissolve>(includeInactive: true));

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private RenderTexture ResultTexture => TextureRenderer.texture as RenderTexture;

	private float TargetValue => (!dissolveTargetState) ? 1 : 0;

	private float CurrentValue
	{
		get
		{
			return UIDissolve.effectFactor;
		}
		set
		{
			UIDissolve.effectFactor = value;
			globalLight.color = Color.Lerp(lightDarkColor, lightDefaultColor, UIDissolve.effectFactor);
		}
	}

	private void Awake()
	{
		lightDefaultColor = globalLight.color;
		myArea = GetComponentInParent<Area>();
	}

	public void ChangeSpace(bool isActive, bool targetTop = false)
	{
		this.targetTop = false;
		if (isActive)
		{
			mainCamera = Camera.main;
			if ((object)mainCamera == null)
			{
				DebugLog.GeneralLogError("베리타 공간 장악용 카메라가 따라갈 메인 카메라가 존재하지 않습니다!");
				return;
			}
			SpaceCamera.targetTexture.Release();
			SpaceCamera.targetTexture.width = mainCamera.pixelWidth;
			SpaceCamera.targetTexture.height = mainCamera.pixelHeight;
			SpaceCamera.targetTexture.Create();
			ResultTexture.Release();
			ResultTexture.width = mainCamera.pixelWidth * 2;
			ResultTexture.height = mainCamera.pixelHeight * 2;
			ResultTexture.Create();
			RenderPipelineManager.beginCameraRendering += CallbackOnPreRender;
			RenderPipelineManager.endCameraRendering += CallbackOnPostRender;
			SynchronizeCamera();
			base.gameObject.SetActive(value: true);
			StartDissolve(isOn: true);
			SoundManager.PlayOneShot(enableEvent);
			SoundManager.PlayAMB("VeritaSpace");
		}
		else
		{
			if ((object)mainCamera == null)
			{
				mainCamera = Camera.main;
				overlappingMask = mainCamera.cullingMask;
			}
			EnableMainCamera();
			StartDissolve(isOn: false);
			SoundManager.PlayOneShot(disableEvent);
			SoundManager.PlayAMB(myArea.SoundKey);
		}
	}

	private void SynchronizeCamera()
	{
		if ((object)mainCamera != null)
		{
			SpaceCamera.transform.SynchronizeValues(mainCamera.transform);
			SpaceCamera.orthographicSize = mainCamera.orthographicSize;
		}
	}

	private void Dissolve()
	{
		if (!isDissolving)
		{
			return;
		}
		if (Mathf.Approximately(CurrentValue, TargetValue))
		{
			EndDissolve();
			return;
		}
		float num = Time.deltaTime * Mathf.Sign(TargetValue - CurrentValue) / dissolveDuration / Game.GetTimeScale();
		CurrentValue += num;
		if (Mathf.Approximately(CurrentValue, TargetValue))
		{
			EndDissolve();
		}
	}

	private void StartDissolve(bool isOn)
	{
		dissolveTargetState = isOn;
		UIDissolve.transitionTexture = (isOn ? transitionBrightToDark : transitionDarkToBright);
		isDissolving = true;
	}

	private void EndDissolve()
	{
		CurrentValue = TargetValue;
		isDissolving = false;
		if (dissolveTargetState)
		{
			DisableMainCamera();
			return;
		}
		RenderPipelineManager.beginCameraRendering -= CallbackOnPreRender;
		RenderPipelineManager.endCameraRendering -= CallbackOnPostRender;
		mainCamera = null;
		base.gameObject.SetActive(value: false);
	}

	private void DisableMainCamera()
	{
		overlappingMask = mainCamera.cullingMask;
		mainCamera.cullingMask = 8;
		mainCamera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
	}

	private void EnableMainCamera()
	{
		mainCamera.cullingMask = overlappingMask;
		mainCamera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
	}

	private void CallbackOnPreRender(ScriptableRenderContext context, Camera camera)
	{
		if ((object)camera == SpaceCamera)
		{
			SynchronizeCamera();
		}
	}

	private void CallbackOnPostRender(ScriptableRenderContext context, Camera camera)
	{
		if ((object)camera == SpaceCamera)
		{
			Graphics.Blit(SpaceCamera.targetTexture, ResultTexture, new Vector2(2f, 2f), cameraAnchorMin);
			SetAnchor();
			Dissolve();
		}
	}

	private void SetAnchor()
	{
		Vector3 vector = (targetTop ? new Vector3(0.5f, 1f, 0f) : SpaceCamera.WorldToViewportPoint(Verita.transform.position));
		cameraAnchorMin = new Vector2(Mathf.Clamp01(vector.x) - 1f, Mathf.Clamp01(vector.y) - 1f);
		TextureTransform.anchorMin = cameraAnchorMin;
		TextureTransform.anchorMax = cameraAnchorMin + new Vector2(2f, 2f);
	}
}
