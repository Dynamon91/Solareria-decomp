using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using UnityEngine;

namespace SPR;

public class PlayerItemFlame : MonoBehaviour
{
	private List<ParticleSystem> loopParticles;

	private ParticleSystem parentParticle;

	public void Initialize(Vector2 pos)
	{
		base.transform.position = pos;
		base.gameObject.SetActive(value: false);
		if (loopParticles == null)
		{
			loopParticles = new List<ParticleSystem>();
			base.gameObject.GetComponentsInChildren(includeInactive: false, loopParticles);
			parentParticle = GetComponent<ParticleSystem>();
			loopParticles.Remove(parentParticle);
		}
	}

	public async UniTask PlayAsync(int delayFrame, float moveSpeed, Vector2 endPos)
	{
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(delayFrame);
		loopParticles.ForEach(delegate(ParticleSystem p)
		{
			p.SetLoop(enabled: true);
		});
		base.gameObject.SetActive(value: true);
		parentParticle.Play();
		await base.transform.DOMove(endPos, moveSpeed).SetSpeedBased().SetEase(Ease.InOutSine)
			.WaitAsync();
		loopParticles.ForEach(delegate(ParticleSystem p)
		{
			p.SetLoop(enabled: false);
		});
	}
}
