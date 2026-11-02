using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI.Map;

public class MapCollectionInfo : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI text;

	[SerializeField]
	private Image image;

	public void SetData(FieldSubtitle field)
	{
		MapCollection.GetCollection(field, Game.Instance.SaveData, out var total, out var current);
		float num = ((total == 0) ? 1f : ((float)current / (float)total));
		text.text = $"{Mathf.Round(num * 100f)}%";
		image.fillAmount = num;
	}
}
