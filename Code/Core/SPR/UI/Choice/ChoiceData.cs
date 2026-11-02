using System;

namespace SPR.UI.Choice;

public struct ChoiceData
{
	private readonly Action onSelectChoice;

	public string ChoiceText { get; private set; }

	public bool ShowNotiIcon { get; private set; }

	public bool IsDefaultChoice { get; private set; }

	public ChoiceData(string choiceText, bool showNotiIcon, Action onSelectChoice = null, bool isDefault = false)
	{
		ChoiceText = choiceText;
		ShowNotiIcon = showNotiIcon;
		this.onSelectChoice = onSelectChoice;
		IsDefaultChoice = isDefault;
	}

	public void OnSelectChoice()
	{
		onSelectChoice?.Invoke();
	}
}
