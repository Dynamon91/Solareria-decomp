using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Field;

public class PotionIcon : MonoBehaviour
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private Sprite available;

	[SerializeField]
	private Sprite used;

	public bool IsActive { get; private set; }

	public void SetUsable(bool isActive)
	{
		IsActive = isActive;
		icon.sprite = (IsActive ? available : used);
	}
}
