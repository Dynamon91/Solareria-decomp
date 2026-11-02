using UnityEngine;

namespace SPR.UI.Map;

public class MapCursor : MonoBehaviour
{
	private Animator animator;

	private CanvasGroup canvasGroup;

	private static readonly int Open = Animator.StringToHash("Open");

	private static readonly int Close = Animator.StringToHash("Close");

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	public CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public void SetFocus(bool value)
	{
		int id = (value ? Close : Open);
		int trigger = (value ? Open : Close);
		Animator.ResetTrigger(id);
		Animator.SetTrigger(trigger);
	}
}
