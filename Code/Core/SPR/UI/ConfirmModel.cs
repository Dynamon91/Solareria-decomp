using System;
using SPR.L10N;

namespace SPR.UI;

public struct ConfirmModel
{
	public string TitleKey { get; }

	public string MessageKey { get; }

	public string ConfirmChoiceKey { get; }

	public string CancelChoiceKey { get; }

	public Action OnConfirm { get; }

	public Action OnCancel { get; }

	public ConfirmModel(string type, Action onConfirm, Action onCancel = null)
	{
		TitleKey = LocaleKey.ConfirmPopupTitle(type);
		MessageKey = LocaleKey.ConfirmPopupDesc(type);
		ConfirmChoiceKey = "UI_ConfirmPopup_Yes";
		CancelChoiceKey = "UI_ConfirmPopup_No";
		OnConfirm = onConfirm;
		OnCancel = onCancel;
	}

	public ConfirmModel(string titleKey, string messageKey, Action onConfirm, Action onCancel = null)
	{
		TitleKey = LocaleKey.ConfirmPopupTitle(titleKey);
		MessageKey = LocaleKey.ConfirmPopupDesc(messageKey);
		ConfirmChoiceKey = "UI_ConfirmPopup_Yes";
		CancelChoiceKey = "UI_ConfirmPopup_No";
		OnConfirm = onConfirm;
		OnCancel = onCancel;
	}

	public ConfirmModel(string titleKey, string messageKey, string confirmChoiceKey, string cancelChoiceKey, Action onConfirm, Action onCancel)
	{
		OnConfirm = onConfirm;
		OnCancel = onCancel;
		TitleKey = titleKey;
		MessageKey = messageKey;
		ConfirmChoiceKey = confirmChoiceKey;
		CancelChoiceKey = cancelChoiceKey;
	}
}
