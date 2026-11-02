using System.Collections.Generic;
using FMODUnity;
using Library;
using UnityEngine;

namespace SPR;

public class Guardian : Creature
{
	[Header("스탯")]
	[SerializeField]
	private float shieldMaxHp;

	[Header("패턴")]
	[SerializeField]
	private GenericAttackSequence<Guardian> guard;

	[SerializeField]
	private GenericAttackSequence<Guardian> attack;

	[Header("Sound")]
	[SerializeField]
	private EventReference hitEvent;

	[SerializeField]
	private ParticleSystem shieldBrake;

	private bool isGuard;

	private float shieldHp;

	private HitInfo takeDamageInfo;

	public override string Name => "Guardian";

	public override bool CanMakeStuck
	{
		get
		{
			if (isGuard && FacingDirection.GetSignX() == (Enemy.Player.VisualCenter - VisualCenter).x.GetSign())
			{
				return !takeDamageInfo.AttackAttribute.HasFlag(AttackAttribute.BlockStuck);
			}
			return false;
		}
	}

	protected override int OriginDef
	{
		get
		{
			if (!isGuard)
			{
				return base.EnemyStatData.Def[0];
			}
			return base.EnemyStatData.Def[1] + base.EnemyStatData.Def[0];
		}
	}

	private bool IsShieldBroken => shieldHp <= 0f;

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		shieldHp = shieldMaxHp;
		TimelineBeginActions.Add("Guard", delegate
		{
			isGuard = !IsShieldBroken;
		});
		TimelineEndActions.Add("Guard", delegate
		{
			isGuard = false;
		});
		AddAttack(guard, () => new List<AttackSequence> { guard });
		AddAttack(attack, () => new List<AttackSequence> { attack });
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		takeDamageInfo = hitInfo;
		if (isGuard)
		{
			if (!hitInfo.AttackAttribute.HasFlag(AttackAttribute.BlockStuck))
			{
				AddStunCount(1);
			}
			shieldHp -= hitInfo.Damage;
			if (IsShieldBroken && base.SkeletonRenderer.Skeleton.Skin == base.SkeletonAnimation.Skeleton.Data.FindSkin("shield_normal"))
			{
				isGuard = false;
				DisableVFX("guard");
				RemoveAttack(guard);
				shieldBrake.Play();
				SetSkin("shield_broken");
				base.StateMachine.ChangeState(new EnemyBounceState(this, Util.SecondsToFrame(GetAnimDuration(base.BounceAnim)), useVFX: true, resetStun: false));
			}
		}
		return base.TryTakeDamage(hitInfo);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { attack };
	}

	protected override List<AttackSequence> GenerateCurrentAttackSequences()
	{
		bool num = !IsShieldBroken && CryptoRandom.RandomBool();
		List<AttackSequence> result = new List<AttackSequence> { attack };
		if (num)
		{
			return new List<AttackSequence> { guard, attack };
		}
		return result;
	}

	protected override void OnDamaged(HitInfo hitInfo)
	{
		if (!CanMakeStuck)
		{
			base.OnDamaged(hitInfo);
			return;
		}
		TryExplodeDashFlame();
		Enemy.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
	}
}
