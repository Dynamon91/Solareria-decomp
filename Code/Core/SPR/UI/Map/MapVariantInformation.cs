using System;
using System.Collections.Generic;
using UnityEngine;

namespace SPR.UI.Map;

[Serializable]
public class MapVariantInformation
{
	[SerializeField]
	[ReadOnlyInspector]
	private string name;

	[SerializeField]
	public FieldID targetField;

	[SerializeField]
	public string objectId;

	[SerializeField]
	public List<MapVariantInfo> variants;

	public void SetName()
	{
		name = $"{targetField} ({objectId})";
		foreach (MapVariantInfo variant in variants)
		{
			variant.SetName();
		}
	}
}
