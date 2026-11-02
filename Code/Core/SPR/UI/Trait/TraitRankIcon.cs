using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Trait;

public class TraitRankIcon : MonoBehaviour
{
	[SerializeField]
	private Image background;

	[SerializeField]
	private Image fill;

	public void SetActive(bool value)
	{
		fill.gameObject.SetActive(value);
	}
}
