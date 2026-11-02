using System;
using UnityEngine;

namespace SPR.UI.Map;

[Serializable]
public struct MapSubtitleInfo
{
	[SerializeField]
	[ReadOnlyInspector]
	private string name;

	[SerializeField]
	public FieldSubtitle field;

	[SerializeField]
	public Sprite fillSprite;

	[SerializeField]
	public Color fillColor;

	[SerializeField]
	public Sprite borderSprite;

	[SerializeField]
	public Color borderColor;

	public string Name
	{
		set
		{
			name = value;
		}
	}
}
