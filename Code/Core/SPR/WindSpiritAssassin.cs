using System;
using System.Collections.Generic;
using FMODUnity;
using Library;
using SPR.FSM.Base;
using UnityEngine;

namespace SPR;

public class WindSpiritAssassin : Creature, IPierce
{
	[Header("Sequence")]
	[SerializeField]
	private GenericAttackSequence<WindSpiritAssassin> throwKnife;

	[SerializeField]
	private GenericAttackSequence<WindSpiritAssassin> windBlade;

	[SerializeField]
	private GenericAttackSequence<WindSpiritAssassin> flash;

	[Header("Reference")]
	[SerializeField]
	private GameObject windCurtain;

	[SerializeField]
	private EventReference unveilEvent;

	[SerializeField]
	private ParticleSystem dotParticle;

	private bool isImmortal;

	public override string Name => "WindSpiritAssassin";

	public override bool CanMove
	{
		get
		{
			if (!windCurtain.activeSelf)
			{
				return base.CanMove;
			}
			return false;
		}
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AddAttack(throwKnife, () => new List<AttackSequence> { throwKnife });
		AddAttack(windBlade, () => new List<AttackSequence> { windBlade });
		AddAttack(throwKnife, () => new List<AttackSequence> { throwKnife });
		AddAction(ActionTiming.Begin, "ThrowKnife", delegate(Parameter[] p)
		{
			ThrowKnife(p[0].Float);
		});
		AddAction(ActionTiming.Begin, "WindBlade", delegate(Parameter[] p)
		{
			WindBlade(p[0].Int);
		});
		AddAction(ActionTiming.Begin, "Invincible", (Action)delegate
		{
			Invincible(value: true);
		});
		AddAction(ActionTiming.End, "Invincible", (Action)delegate
		{
			Invincible(value: false);
		});
		windCurtain.SetActive(value: true);
		base.SkeletonAnimation.Skeleton.SetAlpha(0f);
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (isImmortal)
		{
			return false;
		}
		if (windCurtain.activeSelf)
		{
			base.SkeletonAnimation.Skeleton.SetAlpha(1f);
			windCurtain.SetActive(value: false);
			Enemy.SoundManager.PlayOneShotAttached(unveilEvent, base.transform);
		}
		return base.TryTakeDamage(hitInfo);
	}

	public void OnEnterPierce()
	{
		dotParticle.Play();
	}

	public void OnExitPierce()
	{
		dotParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { throwKnife, windBlade, flash };
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		if (!windCurtain.activeSelf)
		{
			return base.GetAttackSequences();
		}
		return new List<AttackSequence> { throwKnife };
	}

	private void ThrowKnife(float speed)
	{
		KnifeProjectile knifeProjectile = Enemy.PoolManager.GetObject<KnifeProjectile>("Wind Spirit Assassin_Knife.prefab", VisualCenter);
		Vector2 vector = Enemy.Player.VisualCenter - VisualCenter;
		if (vector.x * (float)FacingDirection.GetSignX() < 0f)
		{
			vector = FacingDirection.ToVector2();
		}
		knifeProjectile.Throw(vector.normalized, speed);
	}

	private void WindBlade(int throwKnifeChance)
	{
		EnableVFX("wind_blade");
		if (CryptoRandom.Range(1, 101) <= throwKnifeChance)
		{
			base.StateMachine.ChangeState(new AttackState(this, () => new List<AttackSequence> { throwKnife }, forceAttack: true));
		}
	}

	private void Invincible(bool value)
	{
		isImmortal = value;
		if (value)
		{
			CreatureCanvas.SetActive(value: false);
		}
	}
}
