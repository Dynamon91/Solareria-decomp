using UnityEngine;

namespace SPR.UI;

public class SpBoard : MonoBehaviour
{
	[SerializeField]
	private RectTransform rect;

	private float initialHeight;

	private static PlayerStat playerStat => Game.Instance.Player.PlayerStat;

	public void Initialize()
	{
		initialHeight = rect.rect.height;
	}

	private void Update()
	{
		if (playerStat != null)
		{
			rect.sizeDelta = new Vector2(100f, playerStat.GetStat<Stamina>().Current / (float)playerStat.GetStat<Stamina>().MaxStamina * initialHeight);
		}
	}
}
