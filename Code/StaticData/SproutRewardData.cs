using System;
using UnityEngine;

[Serializable]
public class SproutRewardData
{
	[field: SerializeField]
	public int ID { get; private set; }

	[field: SerializeField]
	public string ItemID { get; private set; }

	[field: SerializeField]
	public ItemType ItemType { get; private set; }
}
