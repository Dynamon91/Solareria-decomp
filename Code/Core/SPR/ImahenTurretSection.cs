using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;

namespace SPR;

public class ImahenTurretSection : ImahenSection, IEventListener
{
	[Flags]
	private enum SpawnType
	{
		None = 0,
		[InspectorName("총알 포탑")]
		Turret1 = 1,
		[InspectorName("레이저 포탑")]
		Turret2 = 2,
		[InspectorName("소환 장치")]
		Spawner = 4,
		[InspectorName("보호 장치")]
		Shield = 8,
		[InspectorName("수리 장치")]
		Repair = 0x10,
		[InspectorName("자폭 장치")]
		Exploder = 0x20
	}

	private const string Turret1Address = "Imahen_Turret_1.prefab";

	private const string Turret2Address = "Imahen_Turret_2.prefab";

	private const string SpawnerAddress = "Imahen_Spawner.prefab";

	private const string ShieldGeneratorAddress = "Imahen_ShieldGenerator.prefab";

	private const string RepairMachineAddress = "Imahen_RepairMachine.prefab";

	private const string ExploderAddress = "Imahen_Exploder.prefab";

	[SerializeField]
	private SpawnType spawnType;

	[SerializeField]
	private int initialCoolTimeFrame;

	[SerializeField]
	private int coolTimeFrame;

	[SerializeField]
	private int[] coolTimePenalty;

	[SerializeField]
	private Transform placeholderParent;

	private readonly List<Transform> placeholders = new List<Transform>();

	private int activeMachineCount;

	private readonly List<string> spawnPool = new List<string>();

	private int frameCount;

	private int coolTime;

	private readonly List<ImahenMachineBase> machines = new List<ImahenMachineBase>();

	private static ObjectPoolManager ObjectPoolManager => Game.Instance.ObjectPoolManager;

	public override void Initialize()
	{
		if (spawnType.HasFlag(SpawnType.Turret1))
		{
			spawnPool.Add("Imahen_Turret_1.prefab");
		}
		if (spawnType.HasFlag(SpawnType.Turret2))
		{
			spawnPool.Add("Imahen_Turret_2.prefab");
		}
		if (spawnType.HasFlag(SpawnType.Spawner))
		{
			spawnPool.Add("Imahen_Spawner.prefab");
		}
		if (spawnType.HasFlag(SpawnType.Shield))
		{
			spawnPool.Add("Imahen_ShieldGenerator.prefab");
		}
		if (spawnType.HasFlag(SpawnType.Repair))
		{
			spawnPool.Add("Imahen_RepairMachine.prefab");
		}
		if (spawnType.HasFlag(SpawnType.Exploder))
		{
			spawnPool.Add("Imahen_Exploder.prefab");
		}
		foreach (Transform item in placeholderParent)
		{
			if (item.gameObject.activeSelf)
			{
				placeholders.Add(item);
			}
		}
		LazySingleton<MessageSystem>.Instance.Subscribe<ImahenMachineDestroyEvent>(this);
		coolTime = initialCoolTimeFrame;
		IsStopped = false;
	}

	public override void OnClear()
	{
		IsStopped = true;
		foreach (ImahenMachineBase machine in machines)
		{
			machine.TryTakeDamage(new HitInfo(Game.Instance.Player.mover, 999999f, 0f, 0f, AttackDirection.Facing, AttackAttribute.Default));
		}
	}

	private void OnDestroy()
	{
		LazySingleton<MessageSystem>.Instance.UnSubscribe<ImahenMachineDestroyEvent>(this);
	}

	private void FixedUpdate()
	{
		if (!IsStopped && activeMachineCount < coolTimePenalty.Length)
		{
			if (frameCount >= coolTime + coolTimePenalty[Mathf.Min(activeMachineCount, coolTimePenalty.Length - 1)])
			{
				Spawn();
				coolTime = coolTimeFrame;
				frameCount = 0;
			}
			frameCount++;
		}
	}

	private void Spawn()
	{
		activeMachineCount++;
		string key = spawnPool[CryptoRandom.Range(0, spawnPool.Count)];
		Transform[] array = placeholders.Where((Transform p) => p.childCount == 0).ToArray();
		Transform transform = array[CryptoRandom.Range(0, array.Length)];
		ImahenMachineBase imahenMachineBase = ObjectPoolManager.GetObject<ImahenMachineBase>(key, transform.position, transform.rotation, transform);
		imahenMachineBase.SetSection(this);
		imahenMachineBase.InitializeOnInstantiate(activeMachineCount, imahenMachineBase.Position);
		if (!machines.Contains(imahenMachineBase))
		{
			machines.Add(imahenMachineBase);
		}
	}

	private void OnDrawGizmos()
	{
		if (Application.isPlaying || placeholderParent.IsNull())
		{
			return;
		}
		foreach (Transform item in placeholderParent)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawCube(item.position + item.rotation * new Vector3(0f, 0.5f), Vector3.one);
		}
	}

	public void OnEvent(global::Library.Event e)
	{
		if (e is ImahenMachineDestroyEvent imahenMachineDestroyEvent && (object)this == imahenMachineDestroyEvent.ImahenSection)
		{
			activeMachineCount = Mathf.Max(0, activeMachineCount - 1);
		}
	}
}
