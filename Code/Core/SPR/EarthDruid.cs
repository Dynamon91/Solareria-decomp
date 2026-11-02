using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using SPR.FSM.Base;
using UnityEngine;

namespace SPR;

public class EarthDruid : Creature
{
	[Header("Sequences")]
	[SerializeField]
	private GenericAttackSequence<EarthDruid> thorn;

	[SerializeField]
	private GenericAttackSequence<EarthDruid> energy;

	[SerializeField]
	private GenericAttackSequence<EarthDruid> eat;

	[Header("References")]
	[SerializeField]
	private EventReference idleVoiceEvent;

	private EventInstance idleVoiceInstance;

	public override string Name => "EarthDruid";

	public override Bounds Bounds => HitBox.bounds;

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		AttackPatterns.TryAdd(thorn, () => new List<AttackSequence> { thorn });
		AttackPatterns.TryAdd(energy, () => new List<AttackSequence> { energy });
		AttackPatterns.TryAdd(eat, () => new List<AttackSequence> { eat });
		AddAction(ActionTiming.Begin, "Vine", delegate(Parameter[] p)
		{
			Vine(p[0].Float, p[1].Int, p[2].Int, p[3].Int, p[4].Float);
		});
		AddAction(ActionTiming.Begin, "Wave", delegate(Parameter[] p)
		{
			Wave(p[0].Float);
		});
	}

	public override List<AttackSequence> GetAttackSequences()
	{
		return base.GetAttackSequences();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { thorn, energy, eat };
	}

	protected override void OnFixedUpdate()
	{
		PlayIdleVoice();
	}

	private void Vine(float width, int waitDuration, int riseDuration, int thornDuration, float speedFactor)
	{
		EarthDruidVine earthDruidVine = Enemy.PoolManager.GetObject<EarthDruidVine>("Earth Druid Vine.prefab", Enemy.Player.GetPositionFromGroud());
		earthDruidVine.EarthDruid = this;
		earthDruidVine.Spread(Enemy.Player.GetPositionFromGroud(), width, waitDuration, riseDuration, thornDuration, speedFactor);
	}

	private void Wave(float speed)
	{
		Enemy.PoolManager.GetObject<MoomooStompWave>("Earth Druid Wave.prefab", GetBoneWorldPosition("fx_weapon_1")).Emit(speed).Forget();
	}

	private void PlayIdleVoice()
	{
		idleVoiceInstance.getPlaybackState(out var state);
		bool flag = state != PLAYBACK_STATE.STOPPED;
		IState currentState = base.StateMachine.CurrentState;
		if (currentState is PatrolState || currentState is ChaseState || currentState is AlertState)
		{
			if (!flag)
			{
				idleVoiceInstance = Enemy.SoundManager.PlayOneShotAttached(idleVoiceEvent, base.transform);
			}
		}
		else if (flag)
		{
			idleVoiceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			idleVoiceInstance.release();
		}
	}
}
