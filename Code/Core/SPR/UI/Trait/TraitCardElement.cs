using Coffee.UIEffects;
using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Trait;

public abstract class TraitCardElement : MonoBehaviour
{
	[SerializeField]
	private UIParticle activateParticle;

	[SerializeField]
	private UIShiny uiShiny;

	[SerializeField]
	protected RectTransform notiIcon;

	private Animator animator;

	private RectTransform rectTransform;

	private CanvasGroup canvasGroup;

	protected TraitData StaticData;

	protected Image IconImage;

	private bool isWatched;

	private bool isActivatedFirst;

	protected bool IsPlayingAnimation;

	private Tween loopTween;

	protected Animator Animator => animator ?? (animator = GetComponent<Animator>());

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public int ID { get; private set; }

	protected static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	public virtual void SetData(int id, bool refreshData = true)
	{
		ID = id;
		StaticData = Game.DataManager.GetTraitData(ID);
		isWatched = !Game.Instance.SaveData.WanderingData.IsUnwatchedCoreStone(ID);
		isActivatedFirst = true;
		IsPlayingAnimation = false;
		if (refreshData)
		{
			RefreshData();
		}
	}

	public abstract void RefreshData();

	protected void SetIconImage(GameObject gameObject)
	{
		if ((object)IconImage == null)
		{
			IconImage = gameObject.GetComponent<Image>();
		}
		IconImage.sprite = AddressableHelper.GetAsset<Sprite>("CoreStone", "Corestone_" + StaticData.Name);
	}

	public void OnFocus()
	{
		UpdateNotiIcon();
		if (isWatched)
		{
			KillLoopTween();
			Animator.ResetTrigger("Open");
			Animator.SetTrigger("Close");
			IsPlayingAnimation = false;
		}
	}

	public bool TryPlayFirstActivation(float delay = 0f)
	{
		UpdateNotiIcon();
		if (isWatched)
		{
			return false;
		}
		Animator.ResetTrigger("Close");
		Animator.SetTrigger("Open");
		IsPlayingAnimation = true;
		if (isActivatedFirst)
		{
			isActivatedFirst = false;
			if (delay > 0f)
			{
				UniTask.WaitForSeconds(delay).GetAwaiter().OnCompleted(PlayFirstActivation);
			}
			else
			{
				PlayFirstActivation();
			}
		}
		else
		{
			PlayLoopEffect();
		}
		return true;
	}

	public void DeactivateUnwatchedEffect(bool instant)
	{
		if (IsPlayingAnimation)
		{
			IsPlayingAnimation = false;
			Animator.ResetTrigger("Open");
			Animator.SetTrigger("Close");
			if (instant)
			{
				Animator.Rebind();
			}
			KillLoopTween();
			notiIcon.gameObject.SetActive(value: false);
		}
	}

	private void UpdateNotiIcon()
	{
		isWatched = !Game.Instance.SaveData.WanderingData.IsUnwatchedCoreStone(ID);
		if (!notiIcon.IsNull())
		{
			notiIcon.localScale = Vector3.one / 0.7f;
			notiIcon.gameObject.SetActive(!isWatched);
		}
	}

	private void PlayFirstActivation()
	{
		activateParticle.Play();
		float alpha = CanvasGroup.alpha;
		CanvasGroup.alpha = 0f;
		CanvasGroup.DOAlpha(alpha, Time.fixedDeltaTime * 20f).OnComplete(PlayLoopEffect);
	}

	private void PlayLoopEffect()
	{
		uiShiny.effectFactor = 0f;
		loopTween = DOTween.To(() => uiShiny.effectFactor, delegate(float x)
		{
			uiShiny.effectFactor = x;
		}, 1f, 1.25f).SetEase(Ease.InOutQuad).SetLoops(-1, LoopType.Restart);
	}

	private void KillLoopTween()
	{
		if (loopTween.IsActive())
		{
			loopTween.Kill();
			uiShiny.effectFactor = 0f;
		}
	}

	private void OnDisable()
	{
		KillLoopTween();
	}
}
