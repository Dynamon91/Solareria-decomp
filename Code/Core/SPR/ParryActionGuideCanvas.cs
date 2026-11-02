using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SPR;

public class ParryActionGuideCanvas : MonoBehaviour
{
	[SerializeField]
	private GameObject guideIcon;

	private RectTransform iconRectTransform;

	private Animator animator;

	private CanvasScaler canvasScaler;

	private Character targetEnemy;

	private RectTransform IconRectTransform => iconRectTransform ?? (iconRectTransform = guideIcon.GetComponent<RectTransform>());

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	private CanvasScaler CanvasScaler => canvasScaler ?? (canvasScaler = GetComponent<CanvasScaler>());

	private static Camera MainCam => Camera.main;

	public void Activate()
	{
		base.gameObject.SetActive(value: true);
		Animator.Play("highlight");
		guideIcon.gameObject.SetActive(value: true);
	}

	public void Deactivate()
	{
		DeactivateAsync().Forget();
	}

	private void LateUpdate()
	{
		if (base.gameObject.activeSelf && Animator.GetCurrentAnimatorStateInfo(0).IsName("disable"))
		{
			SetPosition();
		}
	}

	private async UniTaskVoid DeactivateAsync()
	{
		Animator.Play("disable");
		await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).IsName("disable") && Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);
		base.gameObject.SetActive(value: false);
	}

	public void SetPosition(Character target)
	{
		targetEnemy = target;
		SetPosition();
	}

	private void SetPosition()
	{
		Vector2 vector = ((targetEnemy is Charon) ? (targetEnemy.VisualCenter + Vector2.down) : (targetEnemy.VisualBottom + Vector2.down));
		Vector3 vector2 = MainCam.WorldToScreenPoint(vector);
		float num = CanvasScaler.referenceResolution.x / (float)Screen.width;
		vector2 *= num;
		IconRectTransform.anchoredPosition = vector2;
	}

	public void PlayTutorialGuideSfx()
	{
		if (guideIcon.activeInHierarchy)
		{
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_Tutorial_Ignition_KeyGuide");
		}
	}
}
