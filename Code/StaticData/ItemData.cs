using System;
using UnityEngine;

[Serializable]
public class ItemData
{
	[field: SerializeField]
	public string ID { get; private set; }

	[field: SerializeField]
	public ItemType ItemType { get; private set; }
}
