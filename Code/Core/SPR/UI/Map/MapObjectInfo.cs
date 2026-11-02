using System;
using UnityEngine;

namespace SPR.UI.Map;

[Serializable]
public class MapObjectInfo
{
	[SerializeField]
	[ReadOnlyInspector]
	private string name;

	[SerializeField]
	public MapObjectType type;

	[SerializeField]
	public Vector2 position;

	[SerializeField]
	public int intValue;

	[SerializeField]
	public string stringValue;

	public MapObjectInfo(MapObjectType type, Vector2 position, int intValue)
	{
		this.type = type;
		this.position = position;
		this.intValue = intValue;
		name = $"{this.type}({this.intValue})";
	}

	public MapObjectInfo(MapObjectType type, Vector2 position, string stringValue)
	{
		this.type = type;
		this.position = position;
		this.stringValue = stringValue;
		name = $"{this.type}({this.stringValue})";
	}

	public MapObjectInfo(MapObjectType type, Vector2 position, int intValue, string stringValue)
	{
		this.type = type;
		this.position = position;
		this.intValue = intValue;
		this.stringValue = stringValue;
		name = $"{this.type}({this.intValue},{this.stringValue})";
	}
}
