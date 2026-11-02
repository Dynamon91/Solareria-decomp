using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPR.UI.Map;

[Serializable]
public class FieldInformation
{
	[SerializeField]
	[ReadOnlyInspector]
	public string name;

	[SerializeField]
	public FieldID fieldID;

	[SerializeField]
	public Vector2Int lowerLeft;

	[SerializeField]
	public Vector2Int upperRight;

	[SerializeField]
	public List<FieldConnectionInformation> connections = new List<FieldConnectionInformation>();

	[SerializeField]
	public List<MapObjectInfo> mapObjects = new List<MapObjectInfo>();

	[SerializeField]
	public List<MapFogInfo> mapFogs = new List<MapFogInfo>();

	public bool IsFixed { get; private set; }

	public Vector2Int Size => upperRight - lowerLeft;

	public Vector2 Center => new Vector2(lowerLeft.x + upperRight.x, lowerLeft.y + upperRight.y) / 2f;

	public Vector2 Position { get; private set; }

	public void SetFixed(bool value)
	{
		IsFixed = value;
	}

	public void SetPosition(Vector2 value)
	{
		Position = value;
	}

	public bool HasConnectionWith(FieldID id)
	{
		return connections.Count((FieldConnectionInformation c) => c.targetField == id) > 0;
	}

	public FieldConnectionInformation GetConnection(FieldID id)
	{
		return connections.FirstOrDefault((FieldConnectionInformation c) => c.targetField == id);
	}

	public override string ToString()
	{
		return fieldID.ToString();
	}
}
