using System;
using UnityEngine;

namespace SPR.UI.Map;

[Serializable]
internal struct FullMapPlayerOffset
{
	[SerializeField]
	public FieldSubtitle subtitle;

	[SerializeField]
	public Vector2 offset;
}
