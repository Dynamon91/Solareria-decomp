using System;
using System.Linq;
using Library;
using UnityEngine;

namespace SPR;

public struct HitInfo
{
	public readonly IAttackSource Source;

	public readonly int Damage;

	public readonly int FireDamage;

	public float GroggyDamage;

	public readonly float DarkDamage;

	public readonly HitType TypeToAttack;

	public readonly HitType AttackerType;

	public readonly AttackAttribute AttackAttribute;

	public Vector2 KnockBack { get; set; }

	public Vector2 GuardKnockBack { get; set; }

	public Vector2 ParryKnockBack { get; set; }

	public bool IsDirectAttack
	{
		get
		{
			IAttackSource source = Source;
			return source is PlayerMover || source is HeatFlame;
		}
	}

	public bool IgnoreStun { get; private set; }

	public string AttackName { get; }

	public AttackDirection AttackDirection { get; set; }

	public bool FixDirection { get; set; }

	private static int PlayerMaxHealth => Game.Instance.Player.PlayerStat.GetStat<Health>().MaxHealth;

	public HitInfo(IAttackSource attackSource, string attackName = "")
	{
		AttackName = (attackName.IsNullOrEmpty() ? attackSource.Name : (attackSource.Name + "_" + attackName));
		EnemyData enemyDataByClearCount = Game.DataManager.GetEnemyDataByClearCount(Game.Instance.SaveData.ClearCount, AttackName);
		if (enemyDataByClearCount == null)
		{
			DebugLog.GeneralLogError(AttackName + "에 해당하는 EnemyData를 찾을 수 없습니다! HitInfo를 기본 값으로 생성합니다.");
		}
		Source = attackSource;
		Damage = ((enemyDataByClearCount.Damage < 1f) ? Mathf.CeilToInt((float)PlayerMaxHealth * enemyDataByClearCount.Damage) : Mathf.RoundToInt(enemyDataByClearCount.Damage));
		Damage = Mathf.CeilToInt((float)Damage * DataManager.Instance.GetEnemyMultiplier(Game.Instance.SaveData.ClearCount).Attack);
		GroggyDamage = 0f;
		DarkDamage = enemyDataByClearCount.DarkDamage;
		AttackDirection = enemyDataByClearCount.AttackDirection;
		TypeToAttack = enemyDataByClearCount.HitTypes.Aggregate(HitType.None, (HitType current, HitType hitType) => current | hitType);
		AttackAttribute = enemyDataByClearCount.AttackAttributes.Aggregate(AttackAttribute.None, (AttackAttribute current, AttackAttribute attribute) => current | attribute);
		KnockBack = new Vector2(enemyDataByClearCount.KnockBack[0], enemyDataByClearCount.KnockBack[1]);
		GuardKnockBack = new Vector2(enemyDataByClearCount.GuardKnockBack[0], enemyDataByClearCount.GuardKnockBack[1]);
		ParryKnockBack = new Vector2(enemyDataByClearCount.ParryKnockBack[0], enemyDataByClearCount.ParryKnockBack[1]);
		IgnoreStun = true;
		FireDamage = 0;
		HitType attackerType = ((!(attackSource is Enemy)) ? HitType.Object : HitType.Creature);
		AttackerType = attackerType;
		FixDirection = false;
	}

	public HitInfo(IAttackSource attackSource, float damage, AttackDirection direction, AttackAttribute attackAttribute)
	{
		Source = attackSource;
		Damage = Mathf.CeilToInt(damage);
		FireDamage = 0;
		GroggyDamage = 0f;
		DarkDamage = 0f;
		AttackDirection = direction;
		TypeToAttack = HitType.Player;
		AttackAttribute = attackAttribute;
		KnockBack = Vector2.zero;
		GuardKnockBack = Vector2.zero;
		ParryKnockBack = Vector2.zero;
		AttackName = string.Empty;
		IgnoreStun = true;
		HitType attackerType = ((!(attackSource is Enemy)) ? HitType.Object : HitType.Creature);
		AttackerType = attackerType;
		FixDirection = false;
	}

	public HitInfo(IAttackSource attackSource, float damage, AttackDirection direction, AttackAttribute attackAttribute, int guardKnockBack, float darkDamage = 0f)
	{
		Source = attackSource;
		Damage = Mathf.CeilToInt(damage);
		GroggyDamage = 0f;
		DarkDamage = darkDamage;
		AttackDirection = direction;
		TypeToAttack = HitType.Player;
		AttackAttribute = attackAttribute;
		KnockBack = Vector2.zero;
		GuardKnockBack = direction.GetDirection(Source.FacingDirection).ToVector2() * guardKnockBack;
		ParryKnockBack = Vector2.zero;
		AttackerType = HitType.Creature;
		AttackName = string.Empty;
		IgnoreStun = true;
		FireDamage = 0;
		FixDirection = false;
	}

	public HitInfo(IAttackSource attackSource, float damage, float fireDamage, float groggyDamage, AttackDirection direction, AttackAttribute attackAttribute)
	{
		Source = attackSource;
		Damage = Mathf.CeilToInt(damage);
		FireDamage = Mathf.CeilToInt(fireDamage);
		GroggyDamage = groggyDamage;
		DarkDamage = 0f;
		AttackDirection = direction;
		TypeToAttack = HitType.Creature | HitType.Boss | HitType.Object | HitType.Platform;
		AttackAttribute = attackAttribute;
		KnockBack = Vector2.zero;
		GuardKnockBack = Vector2.zero;
		ParryKnockBack = Vector2.zero;
		AttackerType = HitType.Player;
		AttackName = string.Empty;
		IgnoreStun = true;
		FixDirection = false;
	}

	public Direction GetDirection(Vector2 targetPosition)
	{
		switch (AttackDirection)
		{
		case AttackDirection.Facing:
			return Source.FacingDirection;
		case AttackDirection.OppositeFacing:
			return Source.FacingDirection.Opposite();
		case AttackDirection.Both:
			return Direction.None;
		case AttackDirection.Target:
			if (!(targetPosition.x - Source.CastObject<MonoBehaviour>().transform.position.x < 0f))
			{
				return Direction.Right;
			}
			return Direction.Left;
		default:
			throw new ArgumentOutOfRangeException("AttackDirection", AttackDirection, null);
		}
	}

	public void SetIgnoreStun(bool value)
	{
		IgnoreStun = value;
	}

	public void SetKnockBack(Vector2 knockBack)
	{
		KnockBack = knockBack;
	}

	public void SetGroggyDamage(float value)
	{
		GroggyDamage = value;
	}
}
