using System.Collections.Generic;
using System.Linq;
using SPR.Save;
using UnityEngine;

namespace SPR;

public class TrapTrigger : DoorTrigger
{
	private enum State
	{
		Ready,
		Trapped,
		Clear
	}

	[Space]
	[SerializeField]
	private List<EnemyPlaceHolder> characterPlaceHolders;

	private readonly List<Enemy> enemies = new List<Enemy>();

	private State state;

	public override void AwakeObject(SaveData saveData)
	{
		state = State.Ready;
		characterPlaceHolders.ForEach(delegate(EnemyPlaceHolder holder)
		{
			holder.AddInstantiateCallback(delegate(Enemy x)
			{
				enemies.Add(x);
			});
		});
		InstantiateDoor(openOnAwake: true);
	}

	public override void InteractEnter()
	{
		if (state == State.Ready)
		{
			state = State.Trapped;
			ConnectedDoors.ForEach(delegate(Door door)
			{
				door.SetDoorOpen(open: false);
			});
			OnTrapActivate();
		}
	}

	private void Update()
	{
		if (state == State.Trapped && !enemies.Any((Enemy x) => x.Health > 0))
		{
			state = State.Clear;
			ConnectedDoors.ForEach(delegate(Door door)
			{
				door.SetDoorOpen(open: true);
			});
		}
	}

	protected virtual void OnTrapActivate()
	{
	}
}
