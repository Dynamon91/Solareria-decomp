using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StoreItemData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public int Price { get; private set; }

	[field: SerializeField]
	public int Quantity { get; private set; }

	[field: SerializeField]
	public List<string> Ingredient { get; private set; }

	[field: SerializeField]
	public List<int> IngredientNum { get; private set; }

	[field: SerializeField]
	public string AvailableTrigger { get; private set; }

	[field: SerializeField]
	public ConditionType ConditionType { get; private set; }

	[field: SerializeField]
	public string Condition { get; private set; }

	public StoreItemData()
	{
		ID = string.Empty;
		Price = 0;
		Quantity = 0;
		Ingredient = new List<string>();
		IngredientNum = new List<int>();
		AvailableTrigger = string.Empty;
		ConditionType = ConditionType.None;
		Condition = string.Empty;
	}

	public StoreItemData(string id, int price, int quantity)
	{
		ID = id;
		Price = price;
		Quantity = quantity;
		Ingredient = new List<string>();
		IngredientNum = new List<int>();
		AvailableTrigger = string.Empty;
		ConditionType = ConditionType.None;
		Condition = string.Empty;
	}

	public StoreItemData(StoreItemData origin, int targetQuantity)
	{
		ID = origin.ID;
		Price = origin.Price;
		Quantity = targetQuantity;
		Ingredient = new List<string>(origin.Ingredient);
		IngredientNum = new List<int>(origin.IngredientNum);
		AvailableTrigger = origin.AvailableTrigger;
		ConditionType = origin.ConditionType;
		Condition = origin.Condition;
	}

	public Dictionary<string, int> GetIngredientData()
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		for (int i = 0; i < Ingredient.Count; i++)
		{
			if (!dictionary.ContainsKey(Ingredient[i]))
			{
				dictionary.Add(Ingredient[i], IngredientNum[i]);
			}
		}
		return dictionary;
	}
}
