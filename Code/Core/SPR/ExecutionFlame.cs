using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SPR;

public class ExecutionFlame : HeatFlame
{
	private List<ParticleSystem> loopParticles;

	private List<TrailRenderer> trailRenderers;

	private CurveParameter curveParameter;

	public override string PrefabKey => "Execution Flame.prefab";

	public void Initialize(FlameParameter flameParameter, CurveParameter curveParameter)
	{
		this.curveParameter = curveParameter;
		if (trailRenderers == null)
		{
			trailRenderers = GetComponentsInChildren<TrailRenderer>().ToList();
		}
		Initialize(flameParameter);
	}

	protected override bool CanReturn()
	{
		return !particle.isPlaying;
	}

	protected override void OnFinishedMoving()
	{
		if (loopParticles == null)
		{
			loopParticles = new List<ParticleSystem>();
		}
		loopParticles.AddRange(from p in particle.GetComponentsInChildren<ParticleSystem>()
			where p.main.loop
			select p);
		loopParticles.ForEach(delegate(ParticleSystem p)
		{
			p.SetLoop(enabled: false);
		});
	}

	protected override void OnReturnToPool()
	{
		Game.Instance.ObjectPoolManager.Return(this);
		loopParticles.ForEach(delegate(ParticleSystem p)
		{
			p.SetLoop(enabled: true);
		});
		loopParticles.Clear();
		trailRenderers.ForEach(delegate(TrailRenderer tr)
		{
			tr.Clear();
		});
		trailRenderers.Clear();
	}

	protected override CurveParameter GetCurveParameter()
	{
		return curveParameter;
	}

	public static void ShootFlame(FlameParameter flameParameter, CurveParameter curveParameter)
	{
		if (HeatFlame.CanShoot(flameParameter.TargetEnemy))
		{
			Game.Instance.ObjectPoolManager.GetObject<ExecutionFlame>("Execution Flame.prefab", Game.Instance.Player.mover.VisualCenter).Initialize(flameParameter, curveParameter);
		}
	}
}
