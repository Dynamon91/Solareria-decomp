using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class InfectedFlowerBud : Creature
{
	[Header("추적")]
	[SerializeField]
	private int playerAroundRange;

	[SerializeField]
	private int chaseWaitMin;

	[SerializeField]
	private int chaseWaitMax;

	[Header("공격")]
	[SerializeField]
	private GenericAttackSequence<InfectedFlowerBud> attack;

	[SerializeField]
	private Zone darkZone;

	public override string Name => "InfectedFlowerBud";

	protected override void Awake()
	{
		base.Awake();
		base.ChaseState = new InfectedFlowerBudChaseState(this);
		base.AlertState = new InfectedFlowerBudAlertState(this, alertMinFrame, alertMaxFrame);
		AttackPatterns.TryAdd(attack, () => new List<AttackSequence> { attack });
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAction(ActionTiming.Begin, "Darkness", delegate(int duration, Parameter[] p)
		{
			Darkness(duration, p[0].Int).Forget();
		});
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	public override bool ShouldChangeToAttackState()
	{
		if (base.StateMachine.CurrentState != base.PatrolState)
		{
			return base.ShouldChangeToAttackState();
		}
		return false;
	}

	public Vector2 GetRandomDestination()
	{
		PathFinder.Node node = PathFinder.FindNode(Enemy.Player.GetPositionFromGroud());
		if (node == null)
		{
			return VisualBottom;
		}
		float num = node.Position.x - (float)playerAroundRange + (float)base.DetectPathWidth / 2f;
		float num2 = node.Position.x + (float)playerAroundRange - (float)base.DetectPathWidth / 2f;
		List<Vector2> list = new List<Vector2>();
		for (float num3 = num; num3 <= num2; num3 += 1f)
		{
			list.Add(new Vector2(num3, node.Position.y));
		}
		Vector2 vector = list[CryptoRandom.Range(0, list.Count)];
		if (PathFinder.FindNode(vector) != null)
		{
			return vector;
		}
		return VisualBottom;
	}

	public int GetRandomWaitFrame()
	{
		return CryptoRandom.Range(chaseWaitMin, chaseWaitMax + 1);
	}

	private async UniTaskVoid Darkness(int duration, int cycle)
	{
		int frame = 0;
		FrameTimer cycleTimer = new FrameTimer(cycle);
		PoolingParticle effect = Enemy.PoolManager.GetObject<PoolingParticle>("VFX/DarkFog.prefab", VisualCenter);
		effect.Play();
		for (; frame <= duration; frame++)
		{
			if (cycleTimer.IsDone && effect.IsAlive())
			{
				AttackUtility.AttackBox(VisualCenter, darkZone.Size, new HitInfo(this, "Darkness"), LayerUtility.PlayerLayer);
				cycleTimer.Reset();
			}
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
			cycleTimer.Tick();
		}
		effect.ParticleSystem.Stop(withChildren: true);
	}
}
