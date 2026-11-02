using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR;

public class MyosoSpear : PoolingObject
{
	public enum Target
	{
		Baran,
		Creature
	}

	[Header("VFX")]
	[SerializeField]
	private ParticleSystem cast;

	[SerializeField]
	private ParticleSystem trail;

	[SerializeField]
	private ParticleSystem hitBaran;

	[SerializeField]
	private ParticleSystem hitCreature;

	[Header("Impulse")]
	[SerializeField]
	private CinemachineImpulseSource impulseSource;

	private SpriteRenderer spriteRenderer;

	private SpriteRenderer SpriteRenderer => spriteRenderer ?? (spriteRenderer = GetComponentInChildren<SpriteRenderer>());

	public async UniTask Cast(int fadeDuration)
	{
		SpriteRenderer.SetAlpha(0f);
		cast.Play();
		await SpriteRenderer.DOAlpha(1f, Util.FrameToSeconds(fadeDuration)).SetEase(Ease.Linear).WaitAsync();
		cast.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		trail.Play();
	}

	public void Hit(Target target)
	{
		impulseSource.GenerateImpulse();
		switch (target)
		{
		case Target.Baran:
			hitBaran.Play();
			break;
		case Target.Creature:
			hitCreature.Play();
			break;
		}
	}
}
