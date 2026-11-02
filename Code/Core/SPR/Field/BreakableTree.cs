using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Library;
using Spine;
using UnityEngine;

namespace SPR.Field;

public class BreakableTree : StateSaveBreakableObject
{
	[Flags]
	private enum Direction
	{
		None = 0,
		Right = 1,
		Left = 2
	}

	[SerializeField]
	private Direction allowDirection;

	[SerializeField]
	private Fog fog;

	[SerializeField]
	private ParticleSystem idleParticle;

	[SerializeField]
	private ParticleSystem burnParticle;

	[SerializeField]
	private EventReference burnEvent;

	[Header("Fade")]
	[SerializeField]
	private float fadeMax = 0.32f;

	private const string HitNormalAnim = "hit_normal";

	private const string BreakAnimName1 = "hit";

	private const string BreakAnimName2 = "burn_hit_down";

	private const string BreakIdleAnimName = "burn_idle";

	private MeshRenderer meshRenderer;

	private BoxCollider2D boxCollider;

	private static readonly int FadeAmountID = Shader.PropertyToID("_FadeAmount");

	private static readonly int GlowID = Shader.PropertyToID("_Glow");

	private List<Vector2> groundRange;

	private MeshRenderer MeshRenderer => meshRenderer ?? (meshRenderer = GetComponentInChildren<MeshRenderer>());

	private BoxCollider2D BoxCollider => boxCollider ?? (boxCollider = GetComponent<BoxCollider2D>());

	private static FieldController FieldController => Game.Instance.FieldController;

	protected override string BreakAnimationName => "burn_hit_down";

	public override string ID => $"Breakable Tree_{Game.Instance.CurrentFieldID}_{base.transform.position.ToVector2Int()}";

	public bool IsBroken => Count == BrokenState;

	protected override void Initialize()
	{
		InitializeMaterial();
	}

	protected override void OnLoadBrokenState()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(FadeAmountID, fadeMax);
		MeshRenderer.SetPropertyBlock(materialPropertyBlock);
		ModifyCreaturePath(setGround: true);
		base.SkeletonAnimation.AnimationState.SetAnimation(0, "burn_idle", loop: false);
		if (!fog.IsNull())
		{
			fog.FadeInAsync(instant: true).Forget();
		}
	}

	protected override void OnLoadNormalState()
	{
		ModifyCreaturePath(setGround: false);
		base.SkeletonAnimation.AnimationState.SetAnimation(0, "idle", loop: true);
		idleParticle.Play();
		if (!fog.IsNull())
		{
			fog.FadeOutAsync(instant: true).Forget();
		}
	}

	private void ModifyCreaturePath(bool setGround)
	{
		if (groundRange == null)
		{
			groundRange = new List<Vector2>();
			Vector2 vector = (Vector2)base.transform.position - BoxCollider.size / 2f;
			Vector2 vector2 = (Vector2)base.transform.position + new Vector2(BoxCollider.size.x, 0f - BoxCollider.size.y) / 2f;
			if (boxCollider.size.x % 2f == 0f)
			{
				vector.x += 1f;
			}
			for (float num = vector.x; num <= vector2.x; num += 1f)
			{
				groundRange.Add(new Vector2(num, vector.y));
			}
		}
		FieldController.ModifyPathGraphs(groundRange, setGround, setGround);
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		if (hitInfo.AttackerType != HitType.Player)
		{
			return false;
		}
		Direction direction = (((Game.Instance.Player.transform.position - base.transform.position).x > 0f) ? Direction.Right : Direction.Left);
		if (!allowDirection.HasFlag(direction))
		{
			return false;
		}
		if (!hitInfo.AttackAttribute.HasFlag(AttackAttribute.HeavyAttack))
		{
			base.SkeletonAnimation.AnimationState.SetAnimation(0, "hit_normal", loop: false);
			return false;
		}
		return base.TryTakeDamage(hitInfo);
	}

	protected override void OnBreak()
	{
		ChangeState(BrokenState);
		ModifyCreaturePath(setGround: true);
		idleParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		PlayBreakEffectAsync().Forget();
		if (!fog.IsNull())
		{
			fog.FadeInAsync(instant: false).Forget();
		}
		LazySingleton<MessageSystem>.Instance.Publish(new ChargeAttackTutorialEvent());
	}

	protected override async UniTask PlayBreakAnimationAsync()
	{
		Spine.AnimationState animationState = base.SkeletonAnimation.AnimationState;
		float num = 0f;
		TrackEntry trackEntry = animationState.SetAnimation(0, "hit", loop: false);
		num += trackEntry.AnimationTime;
		trackEntry = animationState.AddAnimation(0, "burn_hit_down", loop: false, 0f);
		num += trackEntry.AnimationTime;
		animationState.AddAnimation(0, "burn_idle", loop: false, 0f);
		await UniTaskExtension.WaitForSeconds(num);
	}

	private void InitializeMaterial()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(FadeAmountID, -0.1f);
		MeshRenderer.SetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetFloat(GlowID, 0f);
		MeshRenderer.SetPropertyBlock(materialPropertyBlock);
	}

	private async UniTask PlayBreakEffectAsync()
	{
		burnParticle.Play();
		MaterialPropertyBlock block = new MaterialPropertyBlock();
		block.SetFloat(FadeAmountID, -0.1f);
		MeshRenderer.SetPropertyBlock(block);
		await SetMaterialWhile(60, -0.1f, fadeMax);
		block.SetFloat(FadeAmountID, fadeMax);
		MeshRenderer.SetPropertyBlock(block);
		async UniTask SetMaterialWhile(int durationFrame, float a, float b)
		{
			for (int frameCount = 0; frameCount < durationFrame; frameCount++)
			{
				block.SetFloat(FadeAmountID, Mathf.Lerp(a, b, (float)frameCount / (float)durationFrame));
				MeshRenderer.SetPropertyBlock(block);
				await UniTask.NextFrame(PlayerLoopTiming.LastFixedUpdate);
			}
		}
	}

	protected override void PlaySfx(bool isBreak)
	{
		if (isBreak)
		{
			BreakableObject.SoundManager.PlayOneShotAttached(burnEvent, base.transform);
		}
	}
}
