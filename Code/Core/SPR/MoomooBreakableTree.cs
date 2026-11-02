using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Field;
using SPR.Save;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class MoomooBreakableTree : SpineEnvironment, IStateSavable, IDamageable, IEventListener
{
	public enum State
	{
		None,
		Droop,
		Burn
	}

	private const float Min = -0.1f;

	private const float Max = 0.5f;

	[SerializeField]
	private bool isDoor;

	[SerializeField]
	private GameObject[] blockingObject;

	[SerializeField]
	private ParticleSystem[] particles;

	[SerializeField]
	private Animator doorLight;

	private State currentState;

	private BoxCollider2D box2D;

	public string ID => base.name;

	public HitType HitType => HitType.Object | HitType.Platform;

	public Vector2 VisualCenter => base.transform.position;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	protected override void Awake()
	{
		base.Awake();
		UseCulling = false;
		LazySingleton<MessageSystem>.Instance.Subscribe<MoomooDeathEvent>(this);
	}

	private void OnDestroy()
	{
		LazySingleton<MessageSystem>.Instance.UnSubscribe<MoomooDeathEvent>(this);
	}

	public void LoadState(int state)
	{
		currentState = (State)state;
		switch (currentState)
		{
		case State.None:
			base.SkeletonAnimations[0].state.SetAnimation(0, "idle", loop: false);
			if (isDoor)
			{
				doorLight.Play("Tree Door Back Light_Off");
			}
			return;
		case State.Burn:
			if (isDoor)
			{
				base.SkeletonAnimations[0].state.SetAnimation(0, "fire_idle", loop: false);
				Box2D.enabled = false;
				blockingObject.ForEach(delegate(GameObject x)
				{
					x.SetActive(value: false);
				});
				doorLight.Play("Tree Door Back Light_Off");
				return;
			}
			if (!isDoor)
			{
				base.SkeletonAnimations[0].state.SetAnimation(0, "fire_idle", loop: false);
				return;
			}
			break;
		case State.Droop:
			particles[0].Play();
			base.SkeletonAnimations[0].SetAnimationInstantly("droop_idle");
			if (!isDoor)
			{
				TurnDark();
			}
			else
			{
				doorLight.Play("Tree Door Back Light_On");
			}
			return;
		}
		throw new ArgumentOutOfRangeException();
	}

	public void ChangeState(int state)
	{
		currentState = (State)state;
		switch (currentState)
		{
		case State.None:
			base.SkeletonAnimations[0].state.SetAnimation(0, "idle", loop: false);
			break;
		case State.Droop:
			particles[0].Play();
			base.SkeletonAnimations[0].state.SetAnimation(0, "droop", loop: false);
			if (!isDoor)
			{
				TurnDark();
			}
			else
			{
				doorLight.Play("Tree Door Back Light_On");
			}
			break;
		case State.Burn:
			if (isDoor)
			{
				BurnDoor().Forget();
				doorLight.Play("Tree Door Back Light_Off");
				break;
			}
			if (!isDoor)
			{
				Burn();
				break;
			}
			goto default;
		default:
			throw new ArgumentOutOfRangeException();
		}
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	public bool TryTakeDamage(HitInfo hitInfo)
	{
		if (!isDoor)
		{
			return false;
		}
		if (currentState != State.Droop)
		{
			return false;
		}
		if (hitInfo.AttackAttribute.HasFlag(AttackAttribute.HeavyAttack))
		{
			Box2D.enabled = false;
			blockingObject.ForEach(delegate(GameObject x)
			{
				x.SetActive(value: false);
			});
			base.transform.GetComponentInParent<MoomooArea>().BurnTree();
		}
		else
		{
			TrackEntry trackEntry = base.SkeletonAnimations[0].AnimationState.SetAnimation(0, "hit_normal", loop: false);
			trackEntry.AnimationStart = 0f;
			trackEntry.MixDuration = 0f;
			base.SkeletonAnimations[0].Update(0f);
		}
		return true;
	}

	public void OnEvent(global::Library.Event e)
	{
		if (e is MoomooDeathEvent)
		{
			ChangeState(1);
		}
	}

	private void TurnDark()
	{
		int nameID = Shader.PropertyToID("_FillColor");
		int fillPhase = Shader.PropertyToID("_FillPhase");
		MaterialPropertyBlock mpb = new MaterialPropertyBlock();
		mpb.SetColor(nameID, Color.black);
		base.MeshRenderer.SetPropertyBlock(mpb);
		DOTween.To(() => mpb.GetFloat(fillPhase), delegate(float x)
		{
			mpb.SetFloat(fillPhase, x);
			base.MeshRenderer.SetPropertyBlock(mpb);
		}, 0.9f, GetComponentInChildren<SkeletonRenderer>().Skeleton.Data.FindAnimation("droop").Duration).SetEase(Ease.Linear);
	}

	private void Burn()
	{
		base.SkeletonAnimations[0].state.SetAnimation(0, "fire", loop: false);
		particles[0].Stop(withChildren: true);
		particles[1].Play();
	}

	private async UniTaskVoid BurnDoor()
	{
		particles[0].Stop(withChildren: true);
		particles[1].Play();
		int fadeAmountID = Shader.PropertyToID("_FadeAmount");
		MaterialPropertyBlock mpb = new MaterialPropertyBlock();
		mpb.SetFloat(fadeAmountID, -0.1f);
		base.MeshRenderer.SetPropertyBlock(mpb);
		DOTween.To(() => mpb.GetFloat(fadeAmountID), delegate(float x)
		{
			mpb.SetFloat(fadeAmountID, x);
			base.MeshRenderer.SetPropertyBlock(mpb);
		}, 0.5f, 0.5f).SetEase(Ease.Linear);
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(10);
		base.SkeletonAnimations[0].state.SetAnimation(0, "fire", loop: false);
	}
}
