using System.Collections.Generic;
using SPR.Library;
using UnityEngine;

namespace SPR;

public abstract class DoorTrigger : FieldTrigger
{
	[SerializeField]
	private List<Vector2> localDoorPositions;

	[SerializeField]
	protected bool IsOpen;

	protected readonly List<Door> ConnectedDoors = new List<Door>();

	protected void InstantiateDoor(bool openOnAwake, string address = null)
	{
		foreach (Vector2 localDoorPosition in localDoorPositions)
		{
			Vector3 pos = base.transform.position + (Vector3)localDoorPosition;
			if (address == null)
			{
				address = "TrapDoor.prefab";
			}
			Door door = Game.Instance.ObjectPoolManager.GetObject<Door>(address, pos, base.transform);
			door.Trigger = this;
			door.SetDoorOpen(openOnAwake);
			ConnectedDoors.Add(door);
		}
	}

	protected void InitializeDoors(IEnumerable<Door> doors, bool open)
	{
		foreach (Door door in doors)
		{
			door.gameObject.SetActive(value: true);
			door.Trigger = this;
			door.SetDoorOpen(open);
			ConnectedDoors.Add(door);
		}
	}

	protected List<Vector3> GetDoorSizes()
	{
		List<Vector3> list = new List<Vector3>(localDoorPositions.Count);
		foreach (Vector2 localDoorPosition in localDoorPositions)
		{
			int colliderHeight = PhysicsUtility.GetColliderHeight(base.transform.position + (Vector3)localDoorPosition);
			list.Add(new Vector3(1f, colliderHeight));
		}
		return list;
	}

	protected override void OnDrawGizmos()
	{
		base.OnDrawGizmos();
		foreach (Vector2 localDoorPosition in localDoorPositions)
		{
			Vector3 vector = base.transform.position + (Vector3)localDoorPosition;
			int colliderHeight = PhysicsUtility.GetColliderHeight(vector);
			Gizmos.DrawWireCube(vector, new Vector3(1f, colliderHeight));
		}
	}
}
