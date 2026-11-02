using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using Library;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class MoldinGravityMachine : MoldinWeapon
{
	public enum EffectTarget
	{
		Machine,
		Wall,
		Ground
	}

	[SerializeField]
	private ParticleSystem[] startMachineParticles;

	[SerializeField]
	private ParticleSystem[] loopMachineParticles;

	[SerializeField]
	private ParticleSystem[] endMachineParticles;

	[SerializeField]
	private ParticleSystem startWallParticle;

	[SerializeField]
	private ParticleSystem loopWallParticle;

	[SerializeField]
	private ParticleSystem startPieceParticle;

	[SerializeField]
	private ParticleSystem loopPieceParticle;

	[SerializeField]
	private ParticleSystem endPieceParticle;

	[SerializeField]
	private EventReference boundEvent;

	private CancellationTokenSource cts;

	private EventInstance boundInstance;

	public override void InitializeBeforeUse()
	{
		base.InitializeBeforeUse();
		cts = new CancellationTokenSource();
	}

	public async UniTaskVoid PlayEffect(EffectTarget target)
	{
		switch (target)
		{
		case EffectTarget.Machine:
			startMachineParticles.ForEach(delegate(ParticleSystem p)
			{
				p.Play(withChildren: true);
			});
			await UniTask.WaitForSeconds(startMachineParticles[0].main.duration / 2f, ignoreTimeScale: false, PlayerLoopTiming.Update, cts.Token);
			loopMachineParticles.ForEach(delegate(ParticleSystem p)
			{
				p.Play(withChildren: true);
			});
			endMachineParticles.ForEach((ParticleSystem p) => p.GetComponent<BoneFollower>().enabled = true);
			break;
		case EffectTarget.Wall:
			startWallParticle.Play(withChildren: true);
			await UniTask.WaitForSeconds(0.5f, ignoreTimeScale: false, PlayerLoopTiming.Update, cts.Token);
			loopWallParticle.Play(withChildren: true);
			break;
		case EffectTarget.Ground:
		{
			ParticleSystem.MainModule main = startPieceParticle.main;
			main.simulationSpeed = 1f;
			startPieceParticle.Play(withChildren: true);
			break;
		}
		default:
			throw new ArgumentOutOfRangeException("target", target, null);
		}
	}

	public async UniTaskVoid SetGroundEffectSpeed(float speed)
	{
		await UniTask.WaitForSeconds(0.5f);
		ParticleSystem.MainModule main = startPieceParticle.main;
		main.simulationSpeed = speed;
	}

	public void StopEffect(EffectTarget target)
	{
		switch (target)
		{
		case EffectTarget.Machine:
			loopMachineParticles.ForEach(delegate(ParticleSystem p)
			{
				p.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			});
			endMachineParticles.ForEach(delegate(ParticleSystem p)
			{
				p.GetComponent<BoneFollower>().enabled = false;
				p.Play(withChildren: true);
			});
			break;
		case EffectTarget.Wall:
			startWallParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			loopWallParticle.Stop(withChildren: true);
			break;
		case EffectTarget.Ground:
		{
			ParticleSystem.MainModule main = startPieceParticle.main;
			main.simulationSpeed = 1f;
			startPieceParticle.Stop();
			endPieceParticle.Play(withChildren: true);
			break;
		}
		default:
			throw new ArgumentOutOfRangeException("target", target, null);
		}
	}

	public async UniTaskVoid ReturnToPoolAsync()
	{
		await UniTask.WaitWhile(() => endMachineParticles.Any((ParticleSystem p) => p.isPlaying) || loopWallParticle.isPlaying || endPieceParticle.isPlaying);
		cts.Cancel();
		cts = null;
		ReturnToPool();
	}

	public void PlayBoundSfx()
	{
		if (!boundInstance.isValid())
		{
			boundInstance = Game.Instance.SoundManager.PlayOneShotAttached(boundEvent, base.transform);
		}
	}

	public void StopBoundSfx()
	{
		boundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
		boundInstance.release();
	}
}
