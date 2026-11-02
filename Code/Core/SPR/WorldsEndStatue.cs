using System;
using DG.Tweening;
using Library;
using SPR.Field;
using SPR.Library;
using SPR.Save;
using SPR.UI;
using Spine.Unity;
using UnityEngine;
using UnityEngine.VFX;

namespace SPR;

public class WorldsEndStatue : InteractableObject, IStateSavable
{
	public enum State
	{
		None,
		Interacted,
		ClearedInteracted,
		PrimordialFlameStatueInteracted
	}

	[SerializeField]
	private BossName bossName;

	[SerializeField]
	private VisualEffect[] visualEffects;

	[Header("Reference")]
	[SerializeField]
	private Animator animator;

	[SerializeField]
	private FieldInteractCanvas customInteractCanvas;

	private BoxCollider2D box2D;

	private SkeletonAnimation skeletonAnimation;

	private bool interacted;

	public string ID => base.name;

	public State CurrentState { get; private set; }

	public bool EventSubscribed => this.OnInteractAction != null;

	private BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	private SkeletonAnimation SkeletonAnimation => skeletonAnimation ?? (skeletonAnimation = GetComponent<SkeletonAnimation>());

	public event Action OnInteractAction;

	public override void AwakeObject(SaveData saveData)
	{
		if (bossName == BossName.PrimordialFlame)
		{
			base.gameObject.layer = LayerUtility.ObjectLayerIndex;
			base.BoxCollider.isTrigger = true;
			InteractCanvas = customInteractCanvas;
		}
		else
		{
			base.AwakeObject(saveData);
		}
	}

	public override void Interact()
	{
		if (bossName == BossName.PrimordialFlame)
		{
			InteractCanvas.GetComponent<CanvasGroup>().DOAlpha(0f, 0.5f).SetEase(Ease.Linear);
			interacted = true;
			OnInteract();
		}
		else
		{
			base.Interact();
		}
	}

	public override void InteractExit()
	{
		if (!interacted)
		{
			base.InteractExit();
		}
	}

	protected override void OnInteract()
	{
		this.OnInteractAction?.Invoke();
	}

	public void LoadState(int state)
	{
		CurrentState = (State)state;
		switch (CurrentState)
		{
		case State.Interacted:
			Box2D.enabled = false;
			EnableVisualEffects();
			break;
		case State.ClearedInteracted:
			if (bossName != BossName.PrimordialFlame)
			{
				SkeletonAnimation.AnimationState.SetAnimation(0, bossName.ToString().ToLower() + "_salute_loop", loop: false);
			}
			Box2D.enabled = false;
			EnableVisualEffects();
			break;
		case State.PrimordialFlameStatueInteracted:
			EnableVisualEffects();
			break;
		case State.None:
			break;
		}
	}

	public void ChangeState(int state)
	{
		LoadState(state);
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	public override void InteractEnter()
	{
		if (CurrentState != State.PrimordialFlameStatueInteracted)
		{
			base.InteractEnter();
		}
	}

	public void ClearAction()
	{
		this.OnInteractAction = null;
	}

	private void EnableVisualEffects()
	{
		VisualEffect[] array = visualEffects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play();
		}
	}
}
