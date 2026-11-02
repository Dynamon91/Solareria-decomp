using System;
using UnityEngine;

namespace SPR.UI.Map;

[Serializable]
public class MapVariantInfo
{
	[SerializeField]
	[ReadOnlyInspector]
	private string name;

	[SerializeField]
	public int state;

	[SerializeField]
	public Vector2 offset;

	public void SetName()
	{
		name = $"[{state}] : {offset}";
	}
}
