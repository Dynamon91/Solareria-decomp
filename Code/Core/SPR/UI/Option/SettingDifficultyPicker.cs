using Cysharp.Threading.Tasks;
using DG.Tweening;
using SPR.L10N;
using StaticData;
using UnityEngine;

namespace SPR.UI.Option;

public class SettingDifficultyPicker : SettingPicker
{
	[SerializeField]
	private GameObject confirmKeyGuide;

	[SerializeField]
	private LocaleText difficultyDesc;

	private bool hasDifficulty;

	private Difficulty CurrentDifficulty => (Difficulty)(Current + 1);

	private int DifficultyNum => Mathf.Clamp((int)(Game.Instance.SaveData.Difficulty - 1), 0, 3);

	private static InputManager InputManager => Game.Instance.InputManager;

	private static ConfirmPopup ConfirmPopup => Game.Instance.UIManager.FieldCanvas.ConfirmPopup;

	public override void Save()
	{
		hasDifficulty = Current != DifficultyNum;
		difficultyDesc.SetData(LocaleKey.DifficultyDesc(CurrentDifficulty));
		SetAlpha();
	}

	protected override void OnSynchronize()
	{
		Current = DifficultyNum;
		hasDifficulty = false;
		difficultyDesc.SetData(LocaleKey.DifficultyDesc(CurrentDifficulty));
		SetAlpha();
	}

	public override void OnFocus(bool value)
	{
		base.OnFocus(value);
		confirmKeyGuide.SetActive(value);
	}

	public override void OnConfirm()
	{
		if (!hasDifficulty)
		{
			DOTween.Kill(localeText.RectTransform, complete: true);
			Game.Instance.UIManager.FadeCanvas.WarningBoard.ShowWarning("UI_Difficulty_Same_Warning");
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_NegativePopup");
			localeText.RectTransform.DOShakePosition(1f, new Vector3(10f, 0f), 10, 90f, snapping: false, fadeOut: true, ShakeRandomnessMode.Harmonic);
		}
		else
		{
			ConfirmModel model = new ConfirmModel("Difficulty", Game.Instance.SaveData.Difficulty switch
			{
				Difficulty.Story => "Difficulty_FromStory", 
				Difficulty.Main => "Difficulty_FromLegend", 
				_ => CurrentDifficulty switch
				{
					Difficulty.Story => "Difficulty_ToStory", 
					Difficulty.Main => "Difficulty_ToLegend", 
					_ => "Difficulty", 
				}, 
			}, ChangeDifficulty);
			ConfirmPopup.SetModel(model);
			InputManager.StackControlTarget(ConfirmPopup).Forget();
		}
	}

	private void ChangeDifficulty()
	{
		Game.Instance.SetDifficulty(CurrentDifficulty, settingInGame: true);
		Game.Instance.Respawn(null).Forget();
		UniTask.WaitUntil(() => Game.Instance.UIManager.FadeCanvas.IsBlack).GetAwaiter().OnCompleted(delegate
		{
			Game.Instance.UIManager.OptionCanvas.DeactivateForce().Forget();
		});
	}

	private void SetAlpha()
	{
		bool flag = Current != DifficultyNum;
		localeText.SetAlpha(flag ? 1f : 0.4f);
		difficultyDesc.SetAlpha(flag ? 1f : 0.4f);
	}
}
