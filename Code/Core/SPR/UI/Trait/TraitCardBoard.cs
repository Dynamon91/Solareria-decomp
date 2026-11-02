using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.UI.Trait;

public abstract class TraitCardBoard : MonoBehaviour
{
	private const float FocusScale = 1.2f;

	private const Ease RotateEase = Ease.Linear;

	protected const float Duration = 1f / 6f;

	[SerializeField]
	private CanvasGroup marker;

	[SerializeField]
	private int focusOffset = 1;

	[SerializeField]
	[Range(0f, 1f)]
	private float unfocusedAlpha;

	private RectTransform rect;

	private RadialLayoutGroup radial;

	private CanvasGroup canvasGroup;

	private Sequence rotateSeq;

	private Sequence fadeSeq;

	private Sequence markerSeq;

	private RectTransform RectTransform => rect ?? (rect = GetComponent<RectTransform>());

	private RadialLayoutGroup RadialLayoutGroup => radial ?? (radial = GetComponent<RadialLayoutGroup>());

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	public int Index { get; protected set; }

	protected List<TraitCardElement> Cards { get; private set; }

	protected int Count => RadialLayoutGroup.Count;

	protected float AngleStep => RadialLayoutGroup.AngleStep;

	public TraitCardElement Current => Cards[Index];

	public int CurrentID => Current.ID;

	private static RuntimeData RuntimeData => Game.Instance.RuntimeData;

	protected abstract int FirstID { get; }

	protected abstract bool UsingActivationEffect { get; }

	public virtual void Initialize()
	{
		Index = 0;
		if (!Cards.IsNullOrEmpty())
		{
			return;
		}
		Cards = GetComponentsInChildren<TraitCardElement>().ToList();
		foreach (var (traitCardElement, num) in Cards.Select((TraitCardElement c, int i) => (c: c, i: i)))
		{
			traitCardElement.SetData(num + FirstID, refreshData: false);
		}
	}

	public virtual void OnActivate(bool focusCurrent)
	{
		foreach (TraitCardElement card in Cards)
		{
			card.RefreshData();
		}
		SetActive(focusCurrent, instant: true);
	}

	public void OnDeactivate()
	{
		Cards.ForEach(delegate(TraitCardElement card)
		{
			card.DeactivateUnwatchedEffect(instant: true);
		});
	}

	public async UniTask RotateToActive(int direction)
	{
		int currentID = CurrentID;
		int i;
		for (i = direction; i != 0; i += direction)
		{
			int num = i + currentID;
			while (num <= 0 || num > Count)
			{
				if (num <= 0)
				{
					num += Count;
				}
				else if (num > Count)
				{
					num -= Count;
				}
			}
			if (Game.Instance.SaveData.PlayerData.TryGetCardData(num, out var _))
			{
				break;
			}
		}
		await Rotate(i);
	}

	public virtual async UniTask Rotate(int direction, bool instant = false)
	{
		await UpdateRotation(focusCurrent: true, instant, direction);
	}

	protected async UniTask UpdateRotation(bool focusCurrent = true, bool instant = false, int direction = 0)
	{
		float currentZ = RectTransform.rotation.eulerAngles.z;
		Index += direction;
		float targetZ = (float)(Index - focusOffset) * AngleStep;
		while (Index < 0 || Index >= Count)
		{
			if (Index < 0)
			{
				Index += Count;
			}
			else if (Index >= Count)
			{
				Index -= Count;
			}
		}
		while (!(targetZ >= 0f) || !(targetZ < 360f))
		{
			if (targetZ < 0f)
			{
				targetZ += 360f;
			}
			else if (targetZ >= 360f)
			{
				targetZ -= 360f;
			}
		}
		if (instant)
		{
			RectTransform.rotation = Quaternion.Euler(0f, 0f, targetZ);
			for (int i = 0; i < Count; i++)
			{
				Cards[i].RectTransform.localScale = GetScale(i);
				Cards[i].CanvasGroup.alpha = GetAlpha(i);
			}
			Current.transform.SetAsLastSibling();
			return;
		}
		rotateSeq?.Kill();
		rotateSeq = DOTween.Sequence();
		float num = targetZ - currentZ;
		if (num * (float)direction < 0f)
		{
			num += 360f * Mathf.Sign(direction);
		}
		float currentDiff = 0f;
		rotateSeq.Join(DOTween.To(() => currentDiff, delegate(float value)
		{
			currentDiff = value;
			RectTransform.rotation = Quaternion.Euler(0f, 0f, currentZ + currentDiff);
		}, num, 1f / 6f).SetEase(Ease.Linear)).OnComplete(delegate
		{
			RectTransform.rotation = Quaternion.Euler(0f, 0f, targetZ);
		});
		for (int num2 = 0; num2 < Count; num2++)
		{
			rotateSeq.Join(Cards[num2].RectTransform.DOScale(GetScale(num2), 1f / 6f).SetEase(MenuTraitPanel.GetEase(!IsFocus(num2)))).Join(Cards[num2].CanvasGroup.DOAlpha(GetAlpha(num2), 1f / 6f).SetEase(MenuTraitPanel.GetEase(!IsFocus(num2))));
		}
		UniTask.WaitForSeconds(1f / 12f).GetAwaiter().OnCompleted(delegate
		{
			Current.transform.SetAsLastSibling();
		});
		if (direction != 0)
		{
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Trait/SFX_UI_TraitPoint_Select");
		}
		await rotateSeq.WaitAsync();
		float GetAlpha(int i2)
		{
			if (!IsFocus(i2))
			{
				return unfocusedAlpha;
			}
			return 1f;
		}
		Vector3 GetScale(int i2)
		{
			if (!IsFocus(i2))
			{
				return Vector3.one;
			}
			return new Vector3(1.2f, 1.2f, 1f);
		}
		bool IsFocus(int num3)
		{
			if (focusCurrent)
			{
				return num3 == Index;
			}
			return false;
		}
	}

	public void SetActive(bool value, bool instant = false)
	{
		float num = (value ? 1f : 0.1f);
		float num2 = (value ? 1f : 0f);
		Ease ease = Ease.Linear;
		if (instant)
		{
			CanvasGroup.alpha = num;
			marker.alpha = num2;
		}
		else
		{
			fadeSeq?.Kill();
			fadeSeq = DOTween.Sequence().Join(CanvasGroup.DOAlpha(num, 1f / 6f).SetEase(ease)).Join(marker.DOAlpha(num2, 1f / 6f).SetEase(ease));
		}
		UpdateRotation(value, instant).Forget();
		if (!UsingActivationEffect)
		{
			return;
		}
		if (value)
		{
			Cards.ForEach(delegate(TraitCardElement card)
			{
				card.TryPlayFirstActivation(instant ? 0f : (1f / 6f));
			});
		}
		else
		{
			Cards.ForEach(delegate(TraitCardElement card)
			{
				card.DeactivateUnwatchedEffect(instant: false);
			});
		}
	}
}
