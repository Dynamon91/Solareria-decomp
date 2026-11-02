using System.Collections.Generic;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class Acapult : Creature
{
	[Tooltip("착탄 프레임")]
	[SerializeField]
	private int impactFrame;

	[Tooltip("포탄 공격 범위")]
	[SerializeField]
	private Zone explosionZone;

	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<Acapult> acapultAttack;

	[Header("Reference")]
	[SerializeField]
	private Transform explosion;

	private readonly List<IDamageable> damageableList = new List<IDamageable>();

	private Vector2 strikePosition;

	public override Direction FacingDirection
	{
		get
		{
			if (!((Enemy.Player.VisualBottom - strikePosition).normalized.x < 0f))
			{
				return Direction.Right;
			}
			return Direction.Left;
		}
	}

	public override string Name => "Acapult";

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAttack(acapultAttack, () => new List<AttackSequence> { acapultAttack });
		TimelineActions.Add("FireShell", delegate
		{
			FireShell();
		});
		TimelineActions.Add("Impact", delegate
		{
			Impact();
		});
	}

	public override bool IsPlayerInSightBound()
	{
		Zone zoneOf = patrolSightArea.GetZoneOf(this);
		return IsPlayerInBound(zoneOf.Center, zoneOf.Size);
	}

	private void FireShell()
	{
		Vector2 positionFromGroud = Enemy.Player.GetPositionFromGroud();
		strikePosition = new Vector2(Enemy.Player.VisualBottom.x, positionFromGroud.y);
		Zone sightZone = acapultAttack.GetSightZone(this);
		strikePosition.x = Mathf.Clamp(strikePosition.x, sightZone.LowerLeft.x, sightZone.UpperRight.x);
		strikePosition.y = Mathf.Clamp(strikePosition.y, sightZone.LowerLeft.y, sightZone.UpperRight.y);
		explosion.position = strikePosition;
		damageableList.Clear();
	}

	private void Impact()
	{
		Vector2 vector = new Vector2(0f, explosionZone.Size.y / 2f);
		AttackUtility.AttackBox(strikePosition + vector, explosionZone.Size, new HitInfo(this, "Shell"), LayerUtility.EnemyDefaultAttackLayer, damageableList);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { acapultAttack };
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Zone zoneOf = explosionZone.GetZoneOf(this);
		Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
		Gizmos.DrawCube(zoneOf.Center, zoneOf.Size);
	}
}
