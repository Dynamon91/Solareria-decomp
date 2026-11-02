using Library;
using UnityEngine;
using UnityEngine.UI;

namespace SPR.UI;

public class AutoScaledGridCell : MonoBehaviour
{
	[SerializeField]
	private float baseCellHeight = 100f;

	[SerializeField]
	private float baseRectHeight = 270f;

	private GridLayoutGroup layoutGroup;

	private RectTransform rect;

	private GridLayoutGroup GridLayoutGroup => layoutGroup ?? (layoutGroup = GetComponent<GridLayoutGroup>());

	private RectTransform RectTransform => rect ?? (rect = GetComponent<RectTransform>());

	private void Start()
	{
		layoutGroup = GetComponent<GridLayoutGroup>();
		UpdateCellSize();
	}

	private void OnValidate()
	{
		UpdateCellSize();
	}

	private void UpdateCellSize()
	{
		if (!GridLayoutGroup.IsNull() && !RectTransform.IsNull())
		{
			float num = RectTransform.rect.height / baseRectHeight;
			GridLayoutGroup.cellSize = Vector2.one * (baseCellHeight * num);
		}
	}
}
