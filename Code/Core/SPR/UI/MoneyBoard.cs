using DG.Tweening;
using Library;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class MoneyBoard : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI coinText;

	[SerializeField]
	private TextMeshProUGUI incomeText;

	[SerializeField]
	private float increaseDuration;

	[SerializeField]
	private float maintainDuration;

	[SerializeField]
	private float fadeOutDuration;

	private CanvasGroup canvasGroup;

	private HorizontalLayoutGroup layoutGroup;

	private RectTransform rect;

	private Sequence moneyUpdateSequence;

	private bool active;

	private float gatheringTimer;

	private int income;

	private int start;

	private float originPosX;

	public float MaintainDuration => maintainDuration;

	private bool IsGathering => gatheringTimer > 0f;

	private string Sign
	{
		get
		{
			if (income >= 0)
			{
				return "+";
			}
			return "-";
		}
	}

	private CanvasGroup CanvasGroup => canvasGroup ?? (canvasGroup = GetComponent<CanvasGroup>());

	private HorizontalLayoutGroup HorizontalLayoutGroup => layoutGroup ?? (layoutGroup = GetComponent<HorizontalLayoutGroup>());

	private RectTransform Rect => rect ?? (rect = GetComponent<RectTransform>());

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	public void Initialize()
	{
		coinText.text = "";
		CanvasGroup.alpha = 0f;
		active = false;
		gatheringTimer = 0f;
		income = 0;
		originPosX = Rect.anchoredPosition.x;
		ClearIncomeText();
	}

	public void Update()
	{
		if (IsGathering)
		{
			gatheringTimer -= Time.deltaTime;
			if (!IsGathering)
			{
				StartCalculate();
			}
		}
	}

	public void UpdateMoneyBoard(int current, int amount)
	{
		if (amount == 0)
		{
			return;
		}
		if (!IsGathering)
		{
			start = current;
			income = 0;
			if (moneyUpdateSequence.IsActive())
			{
				moneyUpdateSequence?.Complete();
			}
			else
			{
				coinText.text = start.ToString();
			}
		}
		income += amount;
		gatheringTimer = maintainDuration;
		if (!active)
		{
			incomeText.text = Sign + Mathf.Abs(income);
			incomeText.SetAlpha(1f);
			CanvasGroup.alpha = 1f;
		}
		LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
	}

	private void StartCalculate()
	{
		moneyUpdateSequence = DOTween.Sequence();
		moneyUpdateSequence.Join(coinText.DOIntText(start + income, increaseDuration));
		moneyUpdateSequence.Join(incomeText.DOIntTextWithPrefix(0, Sign, increaseDuration));
		Tween t = (active ? ((Tweener)incomeText.DOFade(0f, fadeOutDuration)) : ((Tweener)CanvasGroup.DOFade(0f, fadeOutDuration)));
		moneyUpdateSequence.Append(t);
	}

	public void ActivateContinuous(bool value)
	{
		if (value)
		{
			active = true;
			coinText.text = PlayerStat.Money.ToString();
			ClearIncomeText();
			CanvasGroup.DOFade(1f, fadeOutDuration);
			LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
		}
		else
		{
			active = false;
			CanvasGroup.DOFade(0f, fadeOutDuration);
		}
	}

	private void ClearIncomeText()
	{
		incomeText.text = string.Empty;
		incomeText.SetAlpha(0f);
	}

	private void SetAnchoredPosition(float posX)
	{
		Vector2 anchoredPosition = Rect.anchoredPosition;
		anchoredPosition.x = posX;
		Rect.anchoredPosition = anchoredPosition;
	}
}
