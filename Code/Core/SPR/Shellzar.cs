using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using SPR.Library;
using Spine;
using UnityEngine;

namespace SPR;

public class Shellzar : Creature
{
	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<Shellzar> regeneration;

	[SerializeField]
	private GenericAttackSequence<Shellzar> waterBeam;

	[SerializeField]
	private GenericAttackSequence<Shellzar> swing;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private EventReference regenEvent;

	private List<AttackSequence> regenerationSequence;

	private CancellationTokenSource cts;

	public override string Name => "Shellzar";

	protected override int OriginDef
	{
		get
		{
			if (!(base.StateMachine.CurrentState is ShellzarRegenerateState) || FacingDirection.GetSignX() != (Enemy.Player.Position.x - base.Position.x).GetSign())
			{
				return base.EnemyStatData.Def[0];
			}
			return base.EnemyStatData.Def[1];
		}
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		regenerationSequence = new List<AttackSequence> { regeneration };
		AddAttack(waterBeam, () => new List<AttackSequence> { waterBeam });
		AddAttack(swing, () => new List<AttackSequence> { swing });
		AddAction(ActionTiming.Begin, "ChangeRegenerateState", delegate(Parameter[] p)
		{
			base.StateMachine.ChangeState(new ShellzarRegenerateState(this, p[0].Int, p[1].Int));
		});
		AddAction(ActionTiming.Begin, "WaterBeam", delegate(int duration, Parameter[] p)
		{
			WaterBeam(duration, p[0].Float, p[1].Float, p[2].Int).Forget();
		});
		AddAnimationEvent(AnimState.Complete, OnRegenerationComplete);
		cts = new CancellationTokenSource();
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		if (health.Value < health.Max)
		{
			AddAttack(regeneration, () => regenerationSequence);
		}
		else
		{
			RemoveAttack(regeneration);
		}
		return base.GetAttackSequences();
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (base.StateMachine.CurrentState is ShellzarRegenerateState && hitInfo.AttackAttribute.HasFlag(AttackAttribute.HeavyAttack) && !(GetCurrentAnim(0).Name == "regeneration_stiff"))
		{
			SetAnimation("regeneration_stiff", loop: false);
		}
		return base.TryTakeDamage(hitInfo);
	}

	protected override void OnDie()
	{
		base.OnDie();
		RemoveAnimationEvent(AnimState.Complete, OnRegenerationComplete);
		cts.Cancel();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { regeneration, waterBeam, swing };
	}

	public void HealthRegen(int value)
	{
		Enemy.PoolManager.GetObject<PoolingParticle>("Shellzar_regeneration.prefab", VisualCenter).Play();
		animator.Play("shellzar_regeneration");
		Enemy.SoundManager.PlayOneShotAttached(regenEvent, base.transform);
		health += Mathf.RoundToInt((float)base.MaxHealth * ((float)Mathf.Clamp(value, 1, 100) / 100f));
		OnChangeHealth(value);
	}

	private void OnRegenerationComplete(TrackEntry track)
	{
		string text = track.Animation.Name;
		if (text == "regeneration_idle_end" || text == "regeneration_stiff")
		{
			StopHealTween(GetType());
			base.StateMachine.ChangeState(base.PatrolState);
		}
	}

	private async UniTaskVoid WaterBeam(int duration, float maxDistance, float height, int cycleFrame)
	{
		Vector3 center = GetBoneWorldPosition("head");
		RaycastHit2D raycastHit2D = Physics2D.BoxCast(center, Vector2.one, 0f, FacingDirection.ToVector2(), maxDistance, LayerUtility.PlatformLayer);
		Vector2 size = new Vector2(raycastHit2D ? (raycastHit2D.distance + 0.5f) : maxDistance, height);
		center += new Vector3((float)FacingDirection.GetSignX() * size.x / 2f, 0f);
		int frame = 0;
		FrameTimer timer = new FrameTimer(cycleFrame);
		List<IDamageable> list = new List<IDamageable>();
		for (; frame < duration; frame++)
		{
			if (timer.IsDone)
			{
				list.Clear();
				timer.Reset();
			}
			AttackUtility.AttackBox(center, size, new HitInfo(this, "WaterBeam"), LayerUtility.PlayerLayer, list);
			await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cts.Token);
			timer.Tick();
		}
	}
}
