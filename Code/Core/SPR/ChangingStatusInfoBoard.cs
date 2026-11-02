using SPR.L10N;
using SPR.UI;
using TMPro;
using UnityEngine;

namespace SPR;

public class ChangingStatusInfoBoard : MonoBehaviour
{
	public struct StatusChangeInfo
	{
		public string StatLocaleKey { get; }

		public float Current { get; }

		public float Delta { get; }

		public bool IsIncreaseBeneficial { get; }

		public StatusChangeInfo(StatType statType, float current, float delta, bool isIncreaseBeneficial)
		{
			StatLocaleKey = LocaleKey.StatName(statType);
			Current = current;
			Delta = delta;
			IsIncreaseBeneficial = isIncreaseBeneficial;
		}
	}

	[SerializeField]
	private LocaleText statName;

	[SerializeField]
	private TextMeshProUGUI currentStatText;

	[SerializeField]
	private TextMeshProUGUI deltaStatText;

	private static readonly Color IncreaseColor = new Color(0.5f, 1f, 1f, 1f);

	private static readonly Color DecreaseColor = new Color(1f, 0.5f, 0.5f, 1f);

	public void SetData(StatusChangeInfo info)
	{
		statName.SetData(info.StatLocaleKey);
		currentStatText.text = $"{info.Current}";
		deltaStatText.text = string.Format("{0}{1}", (info.Delta >= 0f) ? "+" : "-", info.Delta);
		deltaStatText.color = ((info.IsIncreaseBeneficial == info.Delta > 0f) ? IncreaseColor : DecreaseColor);
	}
}
