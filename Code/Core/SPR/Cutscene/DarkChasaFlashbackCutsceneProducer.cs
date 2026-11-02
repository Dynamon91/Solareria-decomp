using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR.Cutscene;

public class DarkChasaFlashbackCutsceneProducer : SceneMontageCutsceneProducer
{
	[SerializeField]
	private List<GameObject> darkChasaActiveObjects;

	[Header("Skeleton")]
	[SerializeField]
	private SkeletonAnimation[] skeletonAnimations;

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		darkChasaActiveObjects.ForEach(delegate(GameObject go)
		{
			go.SetActive(value: true);
		});
		await UniTask.WaitForSeconds(1f);
		await base.OnStartTimelineAsync(state, token);
		SkeletonAnimation[] array = skeletonAnimations;
		foreach (SkeletonAnimation obj in array)
		{
			obj.PhysicsPositionInheritanceFactor = Vector2.one;
			obj.Skeleton.UpdateWorldTransform(Skeleton.Physics.Reset);
		}
	}

	protected override void OnEndTimeline(int state)
	{
		base.OnEndTimeline(state);
		CutsceneProducer.SoundManager.FadeOutBGM(2f + base.SustainDuration);
	}

	public override bool ShouldLoop(int index)
	{
		return CutsceneProducer.IsTalking;
	}

	public override void EnterLoop(int index)
	{
		StartScenarioInsideCutscene(index);
	}

	public override void ExitLoop(int index)
	{
	}
}
