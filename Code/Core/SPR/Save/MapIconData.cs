using System;
using System.Collections.Generic;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class MapIconData
{
	[SerializeField]
	public int type;

	[SerializeField]
	public List<Vector2> worldPositionConstants;
}
