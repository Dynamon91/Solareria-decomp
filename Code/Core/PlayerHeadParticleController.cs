using System;
using UnityEngine;

[Serializable]
public class PlayerHeadParticleController
{
	[SerializeField]
	private ParticleSystem backgroundParticle;

	[SerializeField]
	private float maxOffsetX;

	[SerializeField]
	private float minOffsetY;

	[SerializeField]
	private float maxOffsetY;

	[SerializeField]
	private float minDistortion;

	[SerializeField]
	private float maxDistortion;

	private Material backgroundParticleMaterial;

	private Material BackgroundParticleMaterial => backgroundParticleMaterial ?? (backgroundParticleMaterial = backgroundParticle.GetComponent<Renderer>().material);

	private static PlayerControlParameter pcp => PlayerControlParameter.Instance;

	public void Update(Vector2 velocity)
	{
		float t = Mathf.InverseLerp(0f, pcp.RunSpeed, Mathf.Abs(velocity.x));
		float value = Mathf.Sign(velocity.x) * Mathf.Lerp(0f, maxOffsetX, t);
		BackgroundParticleMaterial.SetFloat("_OffsetUvX", value);
		if (velocity.y > 0f)
		{
			float t2 = Mathf.InverseLerp(0f, pcp.JumpVelocity.y, velocity.y);
			float value2 = Mathf.Lerp(0f, minOffsetY, t2);
			BackgroundParticleMaterial.SetFloat("_OffsetUvY", value2);
		}
		else if (velocity.y < 0f)
		{
			float t3 = Mathf.InverseLerp(-pcp.TerminalSpeed, 0f, velocity.y);
			float value3 = Mathf.Lerp(maxOffsetY, 0f, t3);
			BackgroundParticleMaterial.SetFloat("_OffsetUvY", value3);
		}
		else
		{
			BackgroundParticleMaterial.SetFloat("_OffsetUvY", 0f);
		}
		float t4 = Mathf.InverseLerp(0f, pcp.RunSpeed, Mathf.Abs(velocity.x));
		float value4 = Mathf.Lerp(minDistortion, maxDistortion, t4);
		BackgroundParticleMaterial.SetFloat("_DistortAmount", value4);
	}
}
