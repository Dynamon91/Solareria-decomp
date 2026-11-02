using System;
using UnityEngine;

namespace SPR.UI.Map;

[Serializable]
public class UnconnectedInformation
{
	[SerializeField]
	public string name;

	[SerializeField]
	public FieldID source;

	[SerializeField]
	public FieldID target;

	[SerializeField]
	public Vector2 entrancePosition;

	public UnconnectedInformation(FieldID source, FieldID target, Vector2 entrancePosition)
	{
		this.source = source;
		this.target = target;
		this.entrancePosition = entrancePosition;
		name = $"{this.source} -> {this.target} {this.entrancePosition}";
	}
}
