using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR.Field;

public class PrimordialFlameArea : Area
{
	[SerializeField]
	private SpriteRenderer background;

	[SerializeField]
	private SpriteRenderer holyExplosionBackground;

	public void Turn(float alpha, float seconds)
	{
		background.DOAlpha(alpha, seconds).SetEase(Ease.Linear);
	}

	public void TurnWhite(float alpha, float seconds)
	{
		holyExplosionBackground.DOAlpha(alpha, seconds).SetEase(Ease.Linear);
	}

	public void TurnColor(Color color, float alpha, float seconds, Ease ease)
	{
		Color color2 = background.color;
		background.color = new Color(color.r, color.g, color.b, color2.a);
		background.DOAlpha(alpha, seconds).SetEase(ease);
	}

	public void Flash(Color color, float alpha, float seconds)
	{
		Color color2 = background.color;
		background.color = new Color(color.r, color.g, color.b, color2.a);
		background.DOAlpha(alpha, seconds).SetEase(Ease.Flash, 2f);
	}
}
