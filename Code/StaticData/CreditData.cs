using System;
using UnityEngine;

[Serializable]
public class CreditData
{
	[field: SerializeField]
	public int ID { get; private set; }

	[field: SerializeField]
	public string Content1 { get; private set; }

	[field: SerializeField]
	public string Content2 { get; private set; }
}
