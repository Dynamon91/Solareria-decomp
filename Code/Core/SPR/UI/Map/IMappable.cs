using UnityEngine;

namespace SPR.UI.Map;

public interface IMappable
{
	MapObjectInfo GetMapObjectInfo(Vector2 center);
}
