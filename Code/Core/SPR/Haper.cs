using System;
using System.Collections.Generic;
using System.Threading;
using SPR.FSM;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class Haper : Creature
{
	[SerializeField]
	private GenericAttackSequence<Haper> attack;

	[Header("파라미터")]
	[SerializeField]
	private Zone attackZone;

	[SerializeField]
	private float avoidDistance;

	[SerializeField]
	private int signDuration;

	private bool isImmortal;

	private CancellationTokenSource cts = new CancellationTokenSource();

	public override string Name => "Haper";

	public HaperJumpState JumpState { get; private set; }

	public HaperAmbushState HaperAmbushState { get; private set; }

	public HaperAvoidState AvoidState { get; private set; }

	public override EnemyState DefaultState => HaperAmbushState;

	public CancellationToken OnDieToken => cts.Token;

	protected override void Awake()
	{
		HaperAmbushState = new HaperAmbushState(this);
		JumpState = new HaperJumpState(this, attack);
		AvoidState = new HaperAvoidState(this);
		base.Awake();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		isImmortal = true;
		AddAction(ActionTiming.Begin, "EnableImmortal", (Action)delegate
		{
			isImmortal = true;
		});
		AddAction(ActionTiming.Begin, "DisableImmortal", (Action)delegate
		{
			isImmortal = false;
		});
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (!isImmortal)
		{
			return base.TryTakeDamage(hitInfo);
		}
		return false;
	}

	public override void AddPlayerDashAttackEffect(int durationFrame)
	{
		if (!isImmortal)
		{
			base.AddPlayerDashAttackEffect(durationFrame);
		}
	}

	public Vector2 GetAvoidPosition()
	{
		PathFinder.Node node = PathFinder.FindNode(VisualBottom);
		PathFinder.Node node2 = node.Shift(-1, (int)avoidDistance).Shift(1, base.DetectPathWidth / 2);
		PathFinder.Node node3 = node.Shift(1, (int)avoidDistance).Shift(-1, base.DetectPathWidth / 2);
		if (node2 == null)
		{
			return node3.Position;
		}
		if (node3 == null)
		{
			return node2.Position;
		}
		float num = Mathf.Abs(VisualBottom.x - node2.Position.x);
		float num2 = Mathf.Abs(VisualBottom.x - node3.Position.x);
		float num3 = Mathf.Abs(VisualBottom.x - Enemy.Player.VisualBottom.x);
		int num4 = 0;
		PathFinder.Node node4;
		if (VisualBottom.x > Enemy.Player.VisualBottom.x)
		{
			if (Mathf.Abs(num - num3) > num2)
			{
				node4 = node2;
				num4 = -1;
			}
			else
			{
				node4 = node3;
				num4 = 1;
			}
		}
		else if (Mathf.Abs(num2 - num3) > num)
		{
			node4 = node3;
			num4 = 1;
		}
		else
		{
			node4 = node2;
			num4 = -1;
		}
		if ((int)node4.LayerMask == (int)LayerUtility.OneWayPlatformLayer)
		{
			node4 = node4.ShiftEnd(num4, LayerUtility.PlatformLayer);
		}
		PathFinder.Node node5 = node4.ShiftEnd(-num4, LayerUtility.OneWayPlatformLayer);
		if (node5 == node4)
		{
			return node4.Position;
		}
		float num5 = Mathf.Abs(node5.Position.x - node4.Position.x);
		if (num5 < (float)base.DetectPathWidth / 2f)
		{
			node4 = node4.Shift(num4, Mathf.RoundToInt(num5) + 1);
		}
		return node4.Position;
	}

	protected override void OnDie()
	{
		base.OnDie();
		cts.Cancel();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Zone zoneOf = attackZone.GetZoneOf(this);
		Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
		Gizmos.DrawCube(zoneOf.Center, zoneOf.Size);
	}
}
