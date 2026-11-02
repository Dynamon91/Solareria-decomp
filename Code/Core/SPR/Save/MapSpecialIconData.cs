using System;
using UnityEngine;

namespace SPR.Save;

[Serializable]
public class MapSpecialIconData
{
	[SerializeField]
	public int type;

	[SerializeField]
	public MapMarkerStatus status;
}
