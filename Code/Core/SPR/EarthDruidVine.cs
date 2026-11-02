using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class EarthDruidVine : PoolingObject, IAttackSource
{
	[SerializeField]
	private EventReference spreadEvent;

	private readonly List<IDamageable> damageList = new List<IDamageable>();

	private readonly List<EarthDruidVineObject> vineObjects = new List<EarthDruidVineObject>();

	public string Name => "EarthDruid_Vine";

	public Direction FacingDirection => Direction.None;

	public EarthDruid EarthDruid { get; set; }

	private static ObjectPoolManager PoolManager => Game.Instance.ObjectPoolManager;

	public override void InitializeBeforeUse()
	{
		damageList.Clear();
		base.InitializeBeforeUse();
	}

	public void Spread(Vector2 playerPosition, float vineWidth, int waitDuration, int riseDuration, int thornDuration, float speedFactor)
	{
		PathFinder.Node node = PathFinder.FindNode(playerPosition);
		PathFinder.Node node2 = node.Shift(-1, 1, allowNull: true);
		PathFinder.Node node3 = node.Shift(1, 1, allowNull: true);
		int num = 0;
		int num2 = 0;
		while ((float)num <= vineWidth / 2f && node2 != null)
		{
			EarthDruidVineObject earthDruidVineObject = PoolManager.GetObject<EarthDruidVineObject>("Earth Druid Vine_Object.prefab", node2.Position);
			vineObjects.Add(earthDruidVineObject);
			PoolManager.GetObject<PoolingParticle>("Earth Druid Vine_Smoke.prefab", earthDruidVineObject.transform.position).Play();
			num += (int)earthDruidVineObject.Width / 2;
			node2 = node2.Shift(-1, (int)earthDruidVineObject.Width / 2, allowNull: true);
		}
		while ((float)num2 <= vineWidth / 2f && node3 != null)
		{
			EarthDruidVineObject earthDruidVineObject2 = PoolManager.GetObject<EarthDruidVineObject>("Earth Druid Vine_Object.prefab", node3.Position);
			vineObjects.Add(earthDruidVineObject2);
			PoolManager.GetObject<PoolingParticle>("Earth Druid Vine_Smoke.prefab", earthDruidVineObject2.transform.position).Play();
			num2 += (int)earthDruidVineObject2.Width / 2;
			node3 = node3.Shift(1, (int)earthDruidVineObject2.Width / 2, allowNull: true);
		}
		HandleOverlap(waitDuration + riseDuration + thornDuration, speedFactor).Forget();
		Attack(waitDuration, riseDuration, thornDuration).Forget();
	}

	private async UniTaskVoid Attack(int waitDuration, int riseDuration, int thornDuration)
	{
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(waitDuration - 4);
		Game.Instance.SoundManager.PlayOneShotAttached(spreadEvent, base.transform);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(4);
		int frame = 0;
		damageList.Clear();
		for (; frame < riseDuration; frame++)
		{
			foreach (EarthDruidVineObject vineObject in vineObjects)
			{
				Vector3 position = vineObject.transform.position;
				Vector2 size = vineObject.Box2D.size;
				AttackUtility.AttackBox(position, size, new HitInfo(this), LayerUtility.PlayerLayer, damageList);
			}
			if (damageList.Count > 0)
			{
				EarthDruid.AddStunCount(1);
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(thornDuration);
		ReturnToPool();
	}

	private async UniTaskVoid HandleOverlap(int duration, float speedFactor)
	{
		Collider2D[] colliders = Array.Empty<Collider2D>();
		for (int frame = 0; frame < duration; frame++)
		{
			foreach (EarthDruidVineObject vineObject in vineObjects)
			{
				Vector3 position = vineObject.transform.position;
				Vector2 size = vineObject.Box2D.size;
				colliders = AttackUtility.OverlapBoxAll(position, size, LayerUtility.PlayerLayer);
			}
			Collider2D[] array = colliders;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].TryGetComponent<PlayerMover>(out var component))
				{
					component.MovementSpeedFactor = speedFactor;
				}
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
	}
}
