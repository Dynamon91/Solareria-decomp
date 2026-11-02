using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;

namespace SPR.UI.Map;

public class MiniMapGenerator : MonoBehaviour
{
	[SerializeField]
	[ReadOnlyInspector]
	private List<UnconnectedInformation> unconnectedInformations;

	public bool TryGetUnconnectedPlayerPosition(FieldID target, out FieldID source, out Vector2 pos)
	{
		UnconnectedInformation unconnectedInformation = unconnectedInformations.FirstOrDefault((UnconnectedInformation i) => i.target == target);
		if (unconnectedInformation == null)
		{
			source = default(FieldID);
			pos = default(Vector2);
			return false;
		}
		source = unconnectedInformation.source;
		pos = unconnectedInformation.entrancePosition;
		return true;
	}

	public void DebugInformations()
	{
		unconnectedInformations.DebugGeneralLog("UnconnectedInformations", (UnconnectedInformation ui) => ui.name);
	}
}
