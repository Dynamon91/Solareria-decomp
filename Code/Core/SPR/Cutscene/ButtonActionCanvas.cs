using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.Cutscene;

public class ButtonActionCanvas : MonoBehaviour
{
	[SerializeField]
	private Image fillImage;

	[SerializeField]
	private TextMeshProUGUI actionKeyText;

	private RectTransform rect;

	private Animator animator;

	private static readonly int Press = Animator.StringToHash("Press");

	private static readonly int Success = Animator.StringToHash("Success");

	private static readonly int Fail = Animator.StringToHash("Fail");

	private RectTransform RectTransform => rect ?? (rect = GetComponent<RectTransform>());

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private static Camera MainCam => Camera.main;

	public void SetActive(bool value, float fillValue = 0f)
	{
		base.gameObject.SetActive(value);
		if (value)
		{
			SetFill(fillValue);
		}
	}

	public void SetFill(float fillValue)
	{
		fillImage.fillAmount = fillValue;
	}

	public void SetText(string text)
	{
		actionKeyText.text = text;
	}

	public void SetPosition(Vector2 worldPosition)
	{
		Vector3 vector = MainCam.WorldToScreenPoint(worldPosition);
		RectTransform.anchoredPosition = new Vector2(vector.x - (float)MainCam.pixelWidth / 2f, vector.y - (float)MainCam.pixelHeight / 2f);
	}

	public void OnPress()
	{
		Animator.ResetTrigger(Press);
		Animator.SetTrigger(Press);
	}

	public async UniTask OnFinishAsync(bool isSuccess)
	{
		Animator.ResetTrigger(Press);
		Animator.SetTrigger(isSuccess ? Success : Fail);
		await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f);
	}
}
