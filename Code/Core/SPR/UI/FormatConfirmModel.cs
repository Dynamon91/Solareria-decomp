using System;
using SPR.L10N;

namespace SPR.UI;

public struct FormatConfirmModel
{
	public string TitleKey { get; }

	public string MessageKey { get; }

	public string[] FormattedStrings { get; }

	public string ConfirmChoiceKey { get; }

	public string CancelChoiceKey { get; }

	public Action OnConfirm { get; }

	public Action OnCancel { get; }

	public FormatConfirmModel(string type, Action onConfirm, Action onCancel, params string[] formattedStrings)
	{
		TitleKey = LocaleKey.ConfirmPopupTitle(type);
		MessageKey = LocaleKey.ConfirmPopupDesc(type);
		FormattedStrings = formattedStrings;
		ConfirmChoiceKey = "UI_ConfirmPopup_Yes";
		CancelChoiceKey = "UI_ConfirmPopup_No";
		OnConfirm = onConfirm;
		OnCancel = onCancel;
	}
}
