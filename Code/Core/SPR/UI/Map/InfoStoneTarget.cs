using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.UI.Map;

public class InfoStoneTarget : MonoBehaviour, IMappable
{
	[SerializeField]
	private string infoStoneID;

	private IStateSavable target;

	private bool shouldCheck;

	private IStateSavable Target => target ?? (target = GetComponent<IStateSavable>());

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		Vector2 position = ((Target is IMappable mappable) ? mappable.GetMapObjectInfo(center).position : (base.transform.position.ToVector2() - center));
		return new MapObjectInfo(MapObjectType.Information, position, infoStoneID);
	}

	private void Awake()
	{
		if (!Trigger.CheckTrigger("Trigger_" + infoStoneID))
		{
			shouldCheck = true;
		}
	}

	private void Update()
	{
		if (shouldCheck && Trigger.CheckTrigger("Trigger_" + infoStoneID))
		{
			Game.Instance.SaveData.WanderingData.RemoveUnwatchedInfoStoneIcon(infoStoneID);
			shouldCheck = false;
		}
	}
}
