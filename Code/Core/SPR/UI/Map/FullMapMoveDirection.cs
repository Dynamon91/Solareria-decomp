using System;
using System.Collections.Generic;
using UnityEngine;

namespace SPR.UI.Map;

[Serializable]
internal struct FullMapMoveDirection
{
	[SerializeField]
	public FieldSubtitle subtitle;

	[SerializeField]
	public Direction direction;

	[SerializeField]
	public List<FieldSubtitle> targets;
}
