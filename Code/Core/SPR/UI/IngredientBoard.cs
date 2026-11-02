using System.Collections.Generic;
using Library;
using UnityEngine;

namespace SPR.UI;

public class IngredientBoard : MonoBehaviour
{
	private RectTransform rectTransform;

	private List<IngredientCell> ingredientCells = new List<IngredientCell>();

	public RectTransform RectTransform => rectTransform ?? (rectTransform = GetComponent<RectTransform>());

	public void SetData(Dictionary<string, int> data)
	{
		Clear();
		if (data == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> datum in data)
		{
			IngredientCell ingredientCell = Game.Instance.ObjectPoolManager.GetObject<IngredientCell>("Ingredient Cell.prefab", base.transform);
			ingredientCell.transform.localScale = Vector3.one;
			ingredientCell.SetData(datum.Key, datum.Value);
			ingredientCells.Add(ingredientCell);
		}
	}

	public void SetData(string itemName, int count, bool instant = false)
	{
		Clear();
		IngredientCell ingredientCell = Game.Instance.ObjectPoolManager.GetObject<IngredientCell>("Ingredient Cell.prefab", base.transform);
		ingredientCell.transform.localScale = Vector3.one;
		ingredientCell.SetData(itemName, count, instant);
		ingredientCells.Add(ingredientCell);
	}

	public void Clear()
	{
		ingredientCells.ForEach(delegate(IngredientCell cell)
		{
			if (!cell.IsNull())
			{
				cell.ReturnToPool();
			}
		});
		ingredientCells.Clear();
		foreach (Transform item in base.transform)
		{
			Object.Destroy(item.gameObject);
		}
	}
}
