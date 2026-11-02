using System.Collections.Generic;
using Coffee.UIEffects;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Menu.Status;

public class HeatDetailInfoIcon : MonoBehaviour
{
	[SerializeField]
	private UIShadow shadow;

	[SerializeField]
	private List<Sprite> heatSprites;

	private Image image;

	private Image Image => image ?? (image = GetComponent<Image>());

	public void SetLevel(int targetLevel)
	{
		if (PlayerStat.HeatMaxLevel < targetLevel)
		{
			SetInactiveState();
			return;
		}
		Image.sprite = heatSprites[targetLevel];
		Image.color = Color.white;
		shadow.enabled = false;
	}

	public void SetInactiveState()
	{
		Image.sprite = heatSprites[0];
		Image.color = Color.black;
		shadow.enabled = true;
	}
}
