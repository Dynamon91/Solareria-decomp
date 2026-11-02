using Library;
using UnityEngine;
using UnityEngine.Rendering;

namespace SPR;

public class ScreenGlitchEffect : MonoBehaviour
{
	private Camera glitchCamera;

	private Camera mainCamera;

	private void Awake()
	{
		glitchCamera = GetComponentInChildren<Camera>(includeInactive: true);
		mainCamera = Camera.main;
	}

	private void OnEnable()
	{
		glitchCamera.targetTexture.Release();
		glitchCamera.targetTexture.width = mainCamera.pixelWidth;
		glitchCamera.targetTexture.height = mainCamera.pixelHeight;
		glitchCamera.targetTexture.Create();
		RenderPipelineManager.beginCameraRendering += CallbackOnPreRender;
		SynchronizeCamera();
	}

	private void OnDisable()
	{
		RenderPipelineManager.beginCameraRendering -= CallbackOnPreRender;
	}

	private void CallbackOnPreRender(ScriptableRenderContext arg1, Camera arg2)
	{
		SynchronizeCamera();
	}

	private void SynchronizeCamera()
	{
		if ((object)mainCamera != null)
		{
			glitchCamera.transform.SynchronizeValues(mainCamera.transform);
			glitchCamera.orthographicSize = mainCamera.orthographicSize;
		}
	}
}
