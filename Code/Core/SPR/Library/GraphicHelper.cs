using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Library;
using SPR.Graphic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SPR.Library;

public static class GraphicHelper
{
	private static Camera mainCamera;

	private static List<FullScreenPassRendererFeature> renderFeatures;

	public static Color BadColor = new Color(0.8313726f, 0f, 0f, 1f);

	public static Color NormalColor = Color.white;

	public static Color GoodColor = new Color(0.6462264f, 0.9708658f, 1f, 1f);

	private static List<Vector2Int> resolutions;

	private static readonly List<Vector2Int> ResolutionsList = new List<Vector2Int>
	{
		new Vector2Int(1280, 720),
		new Vector2Int(1600, 900),
		new Vector2Int(1920, 1080),
		new Vector2Int(2560, 1440),
		new Vector2Int(3840, 2160)
	};

	public static Camera MainCamera
	{
		get
		{
			if (mainCamera.IsNull())
			{
				mainCamera = Camera.main;
			}
			return mainCamera;
		}
	}

	public static Color TransparentColor => new Color(1f, 1f, 1f, 0f);

	private static Resolution ScreenMaxResolution => Screen.resolutions.Last();

	public static IReadOnlyList<Vector2Int> Resolutions => ResolutionsList;

	public static bool IsRenderFeatureActive(GraphicManager.RenderFeatureName renderFeatureName)
	{
		return GetRenderFeature(renderFeatureName.ToString()).isActive;
	}

	public static void SetRenderFeatureActive(GraphicManager.RenderFeatureName renderFeatureName, bool value)
	{
		FullScreenPassRendererFeature renderFeature = GetRenderFeature(renderFeatureName.ToString());
		if (renderFeature.isActive != value)
		{
			renderFeature.SetActive(value);
		}
	}

	public static Material GetRenderFeatureMaterial(GraphicManager.RenderFeatureName renderFeatureName)
	{
		return GetRenderFeature(renderFeatureName.ToString()).passMaterial;
	}

	public static Vector2 GetNormalizedScreenPoint(Vector2 worldPosition)
	{
		Camera camera = MainCamera;
		if (camera.IsNull())
		{
			return worldPosition;
		}
		Vector3 vector = camera.WorldToScreenPoint(worldPosition);
		return new Vector2(vector.x / (float)Screen.width, vector.y / (float)Screen.height);
	}

	private static FullScreenPassRendererFeature GetRenderFeature(string name)
	{
		if (renderFeatures == null)
		{
			GetScriptableRendererData();
		}
		FullScreenPassRendererFeature fullScreenPassRendererFeature = renderFeatures.Find((FullScreenPassRendererFeature rf) => rf.name == name);
		if (!fullScreenPassRendererFeature.IsNull())
		{
			return fullScreenPassRendererFeature;
		}
		DebugLog.GeneralLogError(name + "에 해당하는 Full Screen Shader를 찾을 수 없습니다.");
		return null;
	}

	private static void GetScriptableRendererData()
	{
		UniversalRenderPipelineAsset universalRenderPipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;
		ScriptableRendererData[] obj = (ScriptableRendererData[])universalRenderPipelineAsset.GetType().GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(universalRenderPipelineAsset);
		renderFeatures = ((obj != null) ? obj[0] : null).rendererFeatures.Where((ScriptableRendererFeature x) => x is FullScreenPassRendererFeature).Cast<FullScreenPassRendererFeature>().ToList();
	}

	private static bool TryGetCamBounds(out Bounds camBound, float size = 2f)
	{
		camBound = default(Bounds);
		if ((object)MainCamera == null)
		{
			return false;
		}
		float num = (float)Screen.width / (float)Screen.height;
		float num2 = MainCamera.orthographicSize * 2f;
		float x = num2 * num;
		Vector3 center = new Vector3(MainCamera.transform.position.x, MainCamera.transform.position.y);
		camBound = new Bounds(center, new Vector3(x, num2) * size);
		return true;
	}

	public static bool IsInsideCamera(Vector3 pos, out bool result, float size = 2f)
	{
		result = false;
		if (!TryGetCamBounds(out var camBound, size))
		{
			return false;
		}
		result = camBound.Contains(pos);
		return true;
	}

	public static bool IsAnyInsideCamera(Vector2[] vertices, out bool result, float size = 2f)
	{
		result = false;
		if (!TryGetCamBounds(out var camBound, size))
		{
			return false;
		}
		result = vertices.Any((Vector2 t) => camBound.Contains(t));
		return true;
	}
}
