using TMPro;
using UnityEngine;

namespace SPR.UI.Menu;

public class StatusState : MonoBehaviour
{
	public enum State
	{
		Normal,
		Drop,
		Max
	}

	[SerializeField]
	private LocaleText nameText;

	[SerializeField]
	private TextMeshProUGUI valueText;

	private readonly Color normalColor = Color.white;

	private readonly Color dropColor = new Color(40f / 51f, 14f / 85f, 14f / 85f);

	private readonly Color maxColor = new Color(29f / 85f, 29f / 85f, 29f / 85f);

	public void SetData(State state)
	{
		switch (state)
		{
		case State.Normal:
			nameText.SetColor(normalColor);
			valueText.color = normalColor;
			break;
		case State.Drop:
			if (!(valueText.color == maxColor))
			{
				nameText.SetColor(dropColor);
				valueText.color = dropColor;
			}
			break;
		case State.Max:
			nameText.SetColor(maxColor);
			valueText.color = maxColor;
			break;
		}
	}

	public void OnDisable()
	{
		nameText.SetColor(normalColor);
		valueText.color = normalColor;
	}
}
