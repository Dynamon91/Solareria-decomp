using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using Library;
using PathCreation;
using SPR.FSM;
using SPR.Library;
using SPR.ObjectPool;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SPR;

public class Mucon : FlyingCreature, IPoolableObject
{
	public const string Address = "Mucon.prefab";

	[Header("포자 군주")]
	[SerializeField]
	private int triggerValue;

	[SerializeField]
	private int spreadCount;

	[SerializeField]
	private int spreadCycleFrame;

	[Header("부활")]
	[SerializeField]
	private int reviveDelayFrame;

	[Header("경로 설정")]
	[SerializeField]
	private int samePathDirectionCount = 10;

	[Header("References")]
	[SerializeField]
	private Transform spherePosition;

	[SerializeField]
	private EventReference spreadEvent;

	[SerializeField]
	private EventReference idleEvent;

	private AerialPathNode nextNode;

	private MuconMoveState moveState;

	private bool isReverse;

	private PathFinder pathFinder;

	private Enemy target;

	private static List<PathFinder.Node> graph;

	private static List<PathFinder.Node> newBornPath;

	private PathCreator pathCreator;

	private EventInstance idleInstance;

	private MuconSphere sphere;

	private PathCreator pathObj;

	public override string Name => "Mucon";

	public override EnemyState DefaultState => moveState;

	public string PrefabKey => "Mucon.prefab";

	public GameObject GameObject => base.gameObject;

	public bool IsNewBorn { get; private set; }

	public MuconReviveState ReviveState { get; private set; }

	public bool ReviveComplete
	{
		get
		{
			if ((object)target != null)
			{
				if ((object)sphere != null)
				{
					return sphere.IsDestroyed;
				}
				return false;
			}
			return true;
		}
	}

	private PathCreator PathCreator => pathCreator ?? (pathCreator = GetComponent<PathCreator>());

	public void InitializeBeforeUse()
	{
		base.gameObject.SetActive(value: true);
	}

	public void ReturnToPool()
	{
		Enemy.PoolManager.Return(this);
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		if (newBornPath == null)
		{
			return;
		}
		foreach (PathFinder.Node item in newBornPath)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(item.Position, 0.25f);
		}
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		moveState = new MuconMoveState(this);
		ReviveState = new MuconReviveState(this);
		pathFinder = new PathFinder(Enemy.Platform.GetComponent<Tilemap>());
		graph = pathFinder.BakeAirGraph(typeof(MuconSphere));
		newBornPath = pathFinder.BakeAirGraph(GetType());
		if (!IsNewBorn)
		{
			nextNode = Game.Instance.FieldController.AerialPath.Root;
			base.StateMachine.ChangeState(moveState);
		}
	}

	public void Move(float deltaTime)
	{
		LookAt(nextNode.Position);
		base.transform.position = Vector3.MoveTowards(base.transform.position, nextNode.Position, walkFrontSpeed * deltaTime);
		PlayIdleSfx();
	}

	public void CheckArrival()
	{
		if (!(base.transform.position != nextNode.Position))
		{
			nextNode = nextNode.GetNextNode(isReverse);
			if (nextNode.IsRoot || nextNode.IsEnd)
			{
				isReverse = !isReverse;
			}
			LookAt(nextNode.Position);
		}
	}

	public bool CheckDeadCreature()
	{
		Zone zoneOf = patrolSightArea.GetZoneOf(this);
		Bounds bounds = new Bounds(zoneOf.Center, zoneOf.Size);
		foreach (Enemy deadEnemy in Game.Instance.FieldController.DeadEnemies)
		{
			if (deadEnemy.IsDead && bounds.Intersects(deadEnemy.Bounds))
			{
				target = deadEnemy;
				Debug.Log("Dead creature: " + target.Name);
				return true;
			}
		}
		return false;
	}

	public async UniTaskVoid Revive()
	{
		LookAt(target.Position);
		SetAnimationInstantly("death_reviver_start", loop: false);
		await UniTaskExtension.WaitForSeconds(GetAnimDuration("death_reviver_start"));
		SetAnimation("death_reviver_loop");
		List<Vector2> list = new List<Vector2>
		{
			GetBoneWorldPosition("Face"),
			spherePosition.position
		};
		List<Vector2> list2 = (from x in PathFinder.FindPath(graph, spherePosition.position, target.GetBoneWorldPosition("hip"), GetType())
			select x.Position).ToList();
		Vector2 vector = (list2[2] - list2[1]).normalized;
		int num = 0;
		for (int num2 = 1; num2 < list2.Count; num2++)
		{
			Vector2 normalized = (list2[num2] - list2[num2 - 1]).normalized;
			if (normalized == vector && num < samePathDirectionCount)
			{
				num++;
				continue;
			}
			num = 0;
			vector = normalized;
			list.Add(list2[num2]);
		}
		list.Add(target.GetBoneWorldPosition("hip"));
		GameObject go = new GameObject();
		pathObj = go.AddComponent<PathCreator>();
		pathObj.bezierPath = new BezierPath(list, PathSpace.xy);
		for (int num3 = 0; num3 < pathObj.bezierPath.NumPoints; num3++)
		{
			pathObj.bezierPath.SetPoint(num3, pathObj.bezierPath[num3], suppressPathModifiedEvent: true);
		}
		sphere = Enemy.PoolManager.GetObject<MuconSphere>("Mucon Sphere.prefab", VisualCenter);
		await sphere.Move(pathObj, 10f);
		Object.Destroy(go);
		SetAnimation("death_reviver_end", loop: false);
		AddAnimation("idle");
		target.SetAnimation("death_revive", loop: false);
		TryTakeDamage(new HitInfo(this, "ConsumeHealth"));
		PoolingParticle effect = Enemy.PoolManager.GetObject<PoolingParticle>("Spore_Resurrection.prefab", target.GetBoneWorldPosition("hip"));
		effect.Play();
		target.transform.DORotate(new Vector3(0f, 0f, 0f), GetAnimDuration("death_revive")).SetEase(Ease.Linear);
		await UniTaskExtension.WaitForSeconds(target.GetAnimDuration("death_revive"));
		effect.Stop(withChildren: true);
		target.CastObject<Creature>().Resurrect();
		target = null;
	}

	public void OnReviveExit()
	{
		sphere = null;
		Object.Destroy(pathObj);
		pathObj = null;
	}

	public void NewBorn()
	{
		IsNewBorn = true;
		InitializeOnInstantiate(Game.Instance.FieldController.Enemies.Count() + 1, base.transform.position);
		nextNode = Game.Instance.FieldController.AerialPath.GetCloseNode(base.transform.position);
		base.StateMachine.ChangeState(moveState);
		Game.Instance.FieldController.AddEnemy(this);
		MoveToDestination().Forget();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence>();
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence>();
	}

	protected override void OnChangeHealth(int delta)
	{
		base.OnChangeHealth(delta);
		if ((health.Value + delta) / triggerValue > health.Value / triggerValue)
		{
			SpreadSmallMucon().Forget();
		}
	}

	protected override void OnDie()
	{
		base.OnDie();
		ForceStopDeathFire();
		Enemy.PoolManager.GetObject<PoolingParticle>("Death Fire_Air.prefab", VisualCenter).Play();
	}

	private async UniTaskVoid SpreadSmallMucon()
	{
		Enemy.PoolManager.GetObject<PoolingParticle>("Spore_Passive.prefab", VisualCenter).Play();
		Enemy.SoundManager.PlayOneShotAttached(spreadEvent, base.transform);
		for (int i = 0; i < spreadCount; i++)
		{
			Enemy.PoolManager.GetObject<SmallMucon>("Small Mucon.prefab", VisualCenter, base.transform.parent).Spread();
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(spreadCycleFrame);
		}
	}

	private async UniTask MoveToDestination()
	{
		Stack<PathFinder.Node> path = PathFinder.FindPath(newBornPath, base.Position, nextNode.Position, GetType());
		PathFinder.Node nextPathNode = path.Pop();
		while (path.Count > 0)
		{
			Vector2 vector = AwayFromPlatform(nextPathNode.Position);
			base.transform.position = Vector3.MoveTowards(base.Position, vector, walkFrontSpeed * Time.fixedDeltaTime);
			if (base.Position == vector)
			{
				nextPathNode = path.Pop();
				LookAt(AwayFromPlatform(nextPathNode.Position));
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
		}
		IsNewBorn = false;
	}

	public void PlayIdleSfx()
	{
		if (!idleInstance.isValid())
		{
			idleInstance = Enemy.SoundManager.PlayOneShotAttached(idleEvent, base.transform);
			idleInstance.release();
		}
	}

	private Vector2 AwayFromPlatform(Vector2 position)
	{
		float num = HitBox.size.x / 2f;
		Vector2[] array = new Vector2[4]
		{
			Vector2.left,
			Vector2.right,
			Vector2.up,
			Vector2.down
		};
		foreach (Vector2 vector in array)
		{
			RaycastHit2D raycastHit2D = Physics2D.Raycast(position, vector, num, LayerUtility.PlatformLayer);
			if ((bool)raycastHit2D)
			{
				position += -vector * (num - raycastHit2D.distance);
			}
		}
		return position;
	}
}
