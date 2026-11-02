using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class AutoScaledCanvas : MonoBehaviour
{
	private CanvasScaler canvasScaler;

	private CanvasScaler CanvasScaler => canvasScaler ?? (canvasScaler = GetComponent<CanvasScaler>());

	private float RefRatio => CanvasScaler.referenceResolution.x / CanvasScaler.referenceResolution.y;

	private void Awake()
	{
		MatchResolution();
	}

	public void MatchResolution()
	{
		float num = (float)Screen.width / (float)Screen.height;
		CanvasScaler.matchWidthOrHeight = ((!(num < RefRatio)) ? 1 : 0);
	}
}
