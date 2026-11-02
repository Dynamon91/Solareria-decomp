using DG.Tweening;
using StaticData;
using UnityEngine;

namespace SPR.UI;

public class SpSlider : EffectSlider
{
	private Tween noStaminaTween;

	private static PlayerStat PlayerStat => Game.Instance.Player.PlayerStat;

	public void UpdateBarSize(Vector2 baseSize)
	{
		Difficulty difficulty = Game.Instance.SaveData.Difficulty;
		float num = baseSize.x / (20f * Game.DataManager.GetCurrentDifficultyStat(StatType.Stamina.ToString(), difficulty));
		RectTransform barRectTransform = base.BarRectTransform;
		barRectTransform.sizeDelta = new Vector2(num * (float)PlayerStat.GetStat<Stamina>().MaxStamina, barRectTransform.rect.height);
		UpdateSlider(PlayerStat.GetStat<Stamina>().Current, PlayerStat.GetStat<Stamina>().MaxStamina);
	}

	public void PlayNoStaminaEffect()
	{
		if (!noStaminaTween.IsActive())
		{
			noStaminaTween?.Kill();
			noStaminaTween = base.BarRectTransform.DOShakePosition(0.5f, new Vector3(10f, 0f), 25, 90f, snapping: false, fadeOut: true, ShakeRandomnessMode.Harmonic);
			Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Common/SFX_UI_StaminaLack");
		}
	}
}
