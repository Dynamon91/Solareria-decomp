using SPR.UI;
using UnityEngine;

namespace SPR;

public class WarningForDemo : MonoBehaviour
{
	private static FadeCanvas FadeCanvas => Game.Instance.UIManager.FadeCanvas;

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.TryGetComponent<PlayerMover>(out var _))
		{
			FadeCanvas.WarningBoard.ShowWarning("UI_Elevator_Warning_ForDemo");
		}
	}
}
