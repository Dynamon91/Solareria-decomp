using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using Spine;
using UnityEngine;

namespace SPR;

public class ImahenSpawner : ImahenMachineBase
{
	[Flags]
	private enum CreatureType
	{
		[InspectorName("연구원 1")]
		A = 1,
		[InspectorName("연구원 2")]
		B = 2
	}

	[Header("소환")]
	[SerializeField]
	private int spawnCount;

	[SerializeField]
	private int maxSpawnCount = 10;

	[SerializeField]
	private int initialSpawnWaitFrame;

	[SerializeField]
	private int spawnCycleFrame;

	[SerializeField]
	private CreatureType spawnCreature;

	private FrameTimer timer;

	private CancellationTokenSource cts;

	private int currentSpawnCount;

	public override string Name => "ImahenSpawner";

	protected override void Awake()
	{
		base.Awake();
		timer = new FrameTimer(initialSpawnWaitFrame);
		cts = new CancellationTokenSource();
		AddAnimationEvent(AnimState.Complete, delegate(TrackEntry track)
		{
			SpawnResearcher(track).Forget();
		});
		AddAnimationEvent(AnimState.Complete, delegate(TrackEntry track)
		{
			if (track.Animation.Name == "operate")
			{
				SetAnimation("tube_loop", loop: true, 1f, 1);
			}
		});
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if (base.IsOperate && IsTriggered && spawnCount < maxSpawnCount)
		{
			if (timer.IsDone)
			{
				timer = new FrameTimer(spawnCycleFrame);
				SetAnimation("active_open", loop: false);
			}
			timer.Tick();
		}
	}

	protected override void OnTriggered()
	{
		base.OnTriggered();
		Operate();
	}

	protected override void Die()
	{
		base.Die();
		RemoveAnimation(1);
		cts.Cancel();
	}

	private async UniTaskVoid SpawnResearcher(TrackEntry track)
	{
		if (track.Animation.Name != "active_open")
		{
			return;
		}
		List<string> address = new List<string>();
		for (int i = 0; i < spawnCount; i++)
		{
			if (spawnCreature.HasFlag(CreatureType.A))
			{
				address.Add($"Infected Researcher_A_{CryptoRandom.Range(1, 4)}.prefab");
			}
			if (spawnCreature.HasFlag(CreatureType.B))
			{
				address.Add("Infected Researcher_B_1.prefab");
			}
			InfectedResearcher obj = await AddressableHelper.InstantiateAsync<InfectedResearcher>(address[CryptoRandom.Range(0, address.Count)], base.transform.position);
			obj.InitializeOnInstantiate(base.SortingOrder + UnityEngine.Object.FindObjectsOfType<Creature>().Length + 1, base.transform.position);
			obj.Activate();
			address.Clear();
			currentSpawnCount++;
		}
		SetAnimation("active_close", loop: false);
		AddAnimation("idle");
	}

	private void Operate()
	{
		SetAnimation("operate", loop: false);
		AddAnimation("idle");
	}
}
