using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.Cutscene;
using SPR.Field;
using Spine;
using UnityEngine;

namespace SPR;

public class ImahenCoreMachine : BossCharacter
{
	private const string Stage1Idle = "stage_1/idle";

	private const string Stage2Idle = "stage_2/idle";

	private const string Stage3Idle = "stage_3/idle";

	private const string Stage4Idle = "stage_4/idle";

	private const string Stage1Damage = "stage_1/damage";

	private const string Stage2Damage = "stage_2/damage";

	private const string Stage3Damage = "stage_3/damage";

	private const string Stage4Damage = "stage_4/destruction";

	public const string DestructedLoop = "stage_4/destruction_static";

	[SerializeField]
	private MeshRenderer[] meshRenderers;

	[SerializeField]
	private EventReference hitEvent;

	[SerializeField]
	private EventReference hitLoopEvent;

	[SerializeField]
	private EventReference brokenEvent;

	[SerializeField]
	private int hitCheckPeriod;

	[SerializeField]
	private GameObject solineLight;

	[SerializeField]
	private ParticleSystem repairParticle;

	[SerializeField]
	private ParticleSystem shieldLoopParticle;

	[SerializeField]
	private ParticleSystem shieldEndParticle;

	private EventInstance hitInstance;

	private float hitTimer;

	private CutsceneState dyingState;

	public override string Name => "ImahenCoreMachine";

	protected override int OriginDef
	{
		get
		{
			int activeObjectCount = Enemy.PoolManager.GetActiveObjectCount("Imahen_ShieldGenerator.prefab");
			if (activeObjectCount != 0)
			{
				return base.EnemyStatData.Def[1] * activeObjectCount;
			}
			return base.EnemyStatData.Def[0];
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (hitInstance.isValid())
		{
			hitInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			hitInstance.release();
		}
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		if (Game.Instance.SaveData.EnemyFightData.IsBossCleared(base.BossName))
		{
			SetAnimationInstantly("stage_4/destruction_static");
			HitBox.enabled = false;
			solineLight.gameObject.SetActive(value: false);
			return;
		}
		base.IdleState = new ImahenCoreMachineIdleState(this);
		base.transform.position = spawnPos;
		base.SpawnPosition = spawnPos;
		base.StateMachine.ChangeState(base.IdleState);
		AddAnimationEvent(AnimState.Complete, OnAnimationComplete);
		SetAnimation("pipe_contents_loop", loop: true, 1f, 1);
		dyingState = new CutsceneState(this, 3, blockControl: true);
	}

	protected override void OnFixedUpdate()
	{
		if (hitTimer <= 0f && hitInstance.isValid())
		{
			hitInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			hitInstance.release();
		}
		hitTimer -= Time.deltaTime;
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (health.Value <= 0)
		{
			return false;
		}
		bool result = base.TryTakeDamage(hitInfo);
		float num = (float)base.Health / (float)base.MaxHealth;
		string text = GetCurrentAnim(0).Name;
		if (num <= 0.4f && text.Equals("stage_1/idle"))
		{
			SetAnimation("stage_3/damage", loop: false);
			Enemy.SoundManager.PlayOneShotAttached(brokenEvent, base.transform);
		}
		return result;
	}

	public override void SetPropertyBlock(MaterialPropertyBlock block)
	{
		MeshRenderer[] array = meshRenderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetPropertyBlock(block);
		}
	}

	public void RegenHealth(int value)
	{
		health += value;
		OnChangeHealth(value);
		repairParticle.Play();
		BossCharacter.BossBoard.PlayHealTween(loop: false, GetType());
	}

	public void EnableShield()
	{
		if (!shieldLoopParticle.IsAlive())
		{
			shieldLoopParticle.Play();
		}
	}

	public void TryDisableShield()
	{
		if (Enemy.PoolManager.GetActiveObjectCount("Imahen_ShieldGenerator.prefab") - 1 <= 0)
		{
			shieldLoopParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			shieldEndParticle.Play();
		}
	}

	public void TurnOffLight()
	{
		solineLight.gameObject.SetActive(value: false);
	}

	protected override void OnDamaged(HitInfo hitInfo)
	{
		base.OnDamaged(hitInfo);
		if (hitInfo.Source is ImahenChaser)
		{
			hitTimer = (float)hitCheckPeriod / 60f;
			if (!hitInstance.isValid())
			{
				hitInstance = Enemy.SoundManager.PlayOneShotAttached(hitLoopEvent, base.transform);
			}
		}
		else
		{
			Enemy.SoundManager.PlayOneShotAttached(hitEvent, base.transform);
		}
	}

	protected override void OnDie()
	{
		HitBox.enabled = false;
		RemoveAnimation(1);
		MyArea.CastObject<ImahenArea>().Clear();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence>();
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		return new List<BossAttackSequence>();
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return (ImahenBossCutsceneProducer.State)index switch
		{
			ImahenBossCutsceneProducer.State.Dying => dyingState, 
			ImahenBossCutsceneProducer.State.Clear => dyingState, 
			_ => null, 
		};
	}

	private void OnAnimationComplete(TrackEntry track)
	{
		switch (track.Animation.Name)
		{
		case "stage_1/damage":
			SetAnimation("stage_2/idle");
			break;
		case "stage_2/damage":
			SetAnimation("stage_3/idle");
			break;
		case "stage_3/damage":
			SetAnimation("stage_4/idle");
			break;
		}
	}
}
