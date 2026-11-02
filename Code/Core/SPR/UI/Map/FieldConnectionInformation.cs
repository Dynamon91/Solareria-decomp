using System;
using UnityEngine;

namespace SPR.UI.Map;

[Serializable]
public class FieldConnectionInformation
{
	[SerializeField]
	[ReadOnlyInspector]
	public string name;

	[SerializeField]
	public bool connect;

	[SerializeField]
	public FieldID targetField;

	[SerializeField]
	public Direction direction;

	[SerializeField]
	public int entrancePosition;

	[SerializeField]
	public Vector2 entranceRawPosition;
}
