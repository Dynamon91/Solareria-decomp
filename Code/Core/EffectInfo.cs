using System;
using UnityEngine;

[Serializable]
public struct EffectInfo
{
	public VFXEnum name;

	public ParticleSystem particle;

	public bool followSource;

	public Vector2 localPos;

	public EffectInfo(VFXEnum name, ParticleSystem particle, bool followSource, Vector2 localPos)
	{
		this.name = name;
		this.particle = particle;
		this.followSource = followSource;
		this.localPos = localPos;
	}
}
