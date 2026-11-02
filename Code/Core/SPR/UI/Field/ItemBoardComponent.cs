using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Field;

public class ItemBoardComponent : MonoBehaviour
{
	[SerializeField]
	private Image itemImage;

	[SerializeField]
	private LocaleText itemText;

	[SerializeField]
	private GameObject traitPointEffect;

	[SerializeField]
	private TextMeshProUGUI countText;

	private Animator animator;

	private Action onAnimEnd;

	private const float DurationFrames = 190f;

	private const float MinTime = 0.10526316f;

	private const float MaxTime = 0.94736844f;

	private Animator Animator => animator ?? (animator = GetComponent<Animator>());

	public bool IsAnimating => base.gameObject.activeSelf;

	public bool CanAnimatingOnlyPlayer => !traitPointEffect.activeSelf;

	public void Initialize(Action onEnd)
	{
		base.gameObject.SetActive(value: false);
		traitPointEffect.SetActive(value: false);
		onAnimEnd = onEnd;
	}

	public void OnUpdate()
	{
		if (IsAnimating && Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 189f / 190f)
		{
			base.gameObject.SetActive(value: false);
			onAnimEnd?.Invoke();
		}
	}

	public void Activate(ItemBoardData data)
	{
		itemText.SetData(data.Key);
		itemImage.sprite = data.Sprite;
		itemImage.gameObject.SetActive((object)itemImage.sprite != null && !data.UseTraitPoint);
		countText.gameObject.SetActive(!string.IsNullOrEmpty(data.CountText));
		if (countText.gameObject.activeSelf)
		{
			countText.text = data.CountText;
		}
		traitPointEffect.SetActive(data.UseTraitPoint && !countText.gameObject.activeSelf);
		base.gameObject.SetActive(value: true);
		if (data.Id == "Inflare")
		{
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Ingame/SFX_UI_Level_UP");
		}
	}

	public void ActivateInflareEffect()
	{
		traitPointEffect.SetActive(value: true);
	}

	public async UniTask SkipAsync()
	{
		if (!IsAnimating)
		{
			return;
		}
		AnimatorStateInfo currentAnim = Animator.GetCurrentAnimatorStateInfo(0);
		if (!(currentAnim.normalizedTime >= 0.94736844f))
		{
			await UniTask.WaitUntil(() => currentAnim.normalizedTime >= 0.10526316f);
			Animator.Play(0, -1, 0.94736844f);
		}
	}
}
