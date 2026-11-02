using System;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Cutscene;
using SPR.Save;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace SPR.Field;

public class VeritaHologram : ServiceObject, IStateSavable, ILevelUpProvider
{
	private enum State
	{
		Deactivated,
		Activated
	}

	[Header("Cutscene")]
	[SerializeField]
	private bool onlyDirection;

	[SerializeField]
	private Animator lightAnimator;

	[Header("References")]
	[SerializeField]
	private TimelineAsset levelUpMotionTimeline;

	[SerializeField]
	private CutsceneTrigger firstActivateTrigger;

	[SerializeField]
	private ParticleSystem hologramEffect;

	[SerializeField]
	private GameObject sfxEmitter;

	[SerializeField]
	private RuntimeAnimatorController loopAnimator;

	private State currentState;

	private BoxCollider2D box2D;

	private PlayableDirector director;

	private Animator spineAnimator;

	public string ID => base.name;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	private PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	private Animator SpineAnimator => spineAnimator ?? (spineAnimator = base.SkeletonAnimation.GetComponent<Animator>());

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		if (onlyDirection)
		{
			firstActivateTrigger.gameObject.SetActive(value: false);
			ChangeState(1);
		}
		else
		{
			firstActivateTrigger.OnTriggered += delegate
			{
				ChangeState(1);
			};
		}
	}

	public override void Interact()
	{
		if (currentState != State.Deactivated)
		{
			base.Interact();
		}
	}

	public override void InteractEnter()
	{
		if (currentState != State.Deactivated)
		{
			base.InteractEnter();
		}
	}

	public void LoadState(int state)
	{
		if (VeritaCutsceneProducer.IsVeritaInfected)
		{
			currentState = State.Deactivated;
		}
		else
		{
			currentState = (State)state;
		}
		switch (currentState)
		{
		case State.Deactivated:
			SetAlpha(0f);
			firstActivateTrigger.gameObject.SetActive(value: true);
			Box2D.enabled = false;
			sfxEmitter.SetActive(value: false);
			break;
		case State.Activated:
			SetAlpha(1f);
			firstActivateTrigger.gameObject.SetActive(value: false);
			if (!onlyDirection)
			{
				Box2D.enabled = true;
				lightAnimator.Play("Device Light_Loop");
				hologramEffect.gameObject.SetActive(value: true);
				hologramEffect.Play(withChildren: true);
			}
			sfxEmitter.SetActive(value: true);
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public void ChangeState(int state)
	{
		if (!VeritaCutsceneProducer.IsVeritaInfected && currentState == State.Deactivated && state == 1)
		{
			Appear(state).Forget();
			return;
		}
		LoadState(state);
		if (!onlyDirection && Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	private async UniTaskVoid Appear(int state)
	{
		EnableVFX("appear");
		lightAnimator.Play("Device Light_On");
		await UniTask.WaitUntil(() => base.VfxController.IsTimelineEnd("appear"));
		if (!BasicExtension.IsNull(loopAnimator))
		{
			SpineAnimator.runtimeAnimatorController = loopAnimator;
		}
		LoadState(state);
		if (!onlyDirection && Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	private void SetAlpha(float value)
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(Shader.PropertyToID("_Alpha"), value);
		MeshRenderer.SetPropertyBlock(materialPropertyBlock);
	}

	public async UniTask PlayLevelUpMotionAsync()
	{
		Director.playableAsset = levelUpMotionTimeline;
		Director.Play();
		await UniTask.WaitWhile(() => Director.state == PlayState.Playing);
		SetAnimation("scene/final/stand");
	}
}
