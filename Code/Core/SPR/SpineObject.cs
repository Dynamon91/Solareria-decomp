using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Library;
using Spine;
using Spine.Unity;
using Spine.Unity.AttachmentTools;
using UnityEngine;

namespace SPR;

public abstract class SpineObject : MonoBehaviour, IAttackSource
{
	public enum AnimState
	{
		Start,
		End,
		Complete
	}

	[SerializeField]
	private bool useCulling = true;

	[SerializeField]
	protected Vector2 visualCenterPivot;

	private readonly SpineAtlasAsset atlasAsset;

	private MeshRenderer meshRenderer;

	private SkeletonAnimation skeletonAnimation;

	private SkeletonRenderer skeletonRenderer;

	private VFXController vfxController;

	private readonly List<SpineAnimEvent> _spineAnimEvents = new List<SpineAnimEvent>();

	private readonly Skin skin = new Skin("character-skin");

	private static readonly int TintColor = Shader.PropertyToID("_Color");

	private static readonly int HitColor = Shader.PropertyToID("_HitEffectColor");

	private static readonly int HitBlend = Shader.PropertyToID("_HitEffectBlend");

	private readonly Vector2[] vertices = new Vector2[4];

	private bool isFirstFrame = true;

	public int SortingLayerID
	{
		get
		{
			return MeshRenderer.sortingLayerID;
		}
		set
		{
			MeshRenderer.sortingLayerID = value;
		}
	}

	public int SortingOrder
	{
		get
		{
			return MeshRenderer.sortingOrder;
		}
		set
		{
			MeshRenderer.sortingOrder = value;
		}
	}

	public abstract string Name { get; }

	public virtual Direction FacingDirection
	{
		get
		{
			if (!(base.transform.localScale.x >= 0f))
			{
				return Direction.Left;
			}
			return Direction.Right;
		}
	}

	public virtual Bounds Bounds => MeshRenderer.bounds;

	public string SkinName { get; protected set; }

	public SkeletonAnimation SkeletonAnimation => skeletonAnimation ?? (skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(includeInactive: true));

	public SkeletonRenderer SkeletonRenderer => skeletonRenderer ?? (skeletonRenderer = GetComponentInChildren<SkeletonRenderer>());

	protected virtual MeshRenderer MeshRenderer => meshRenderer ?? (meshRenderer = GetComponentInChildren<MeshRenderer>());

	protected VFXController VfxController => vfxController ?? (vfxController = GetComponentInChildren<VFXController>());

	private Skeleton Skeleton => SkeletonRenderer.Skeleton;

	private SkeletonData SkeletonData => Skeleton.Data;

	public virtual Vector2 VisualCenter => (Vector2)base.transform.position + new Vector2(visualCenterPivot.x * (float)FacingDirection.GetSignX(), visualCenterPivot.y);

	public virtual Vector2 VisualBottom => (Vector2)base.transform.position + new Vector2(visualCenterPivot.x * (float)FacingDirection.GetSignX(), 0f);

	public Vector2 Position
	{
		get
		{
			return base.transform.position;
		}
		set
		{
			base.transform.position = value;
		}
	}

	public Vector2 VisualCenterPivot => visualCenterPivot;

	public Spine.Animation GetCurrentAnim(int trackIndex)
	{
		return SkeletonAnimation.AnimationState.GetCurrent(trackIndex).Animation;
	}

	public Vector3 GetBoneWorldPosition(string boneName)
	{
		return Skeleton.FindBone(boneName).GetWorldPosition(SkeletonRenderer.transform);
	}

	protected virtual void Update()
	{
		if (isFirstFrame)
		{
			isFirstFrame = false;
		}
		else if (useCulling && SkeletonAnimation.gameObject.activeInHierarchy)
		{
			CullingSpine();
		}
	}

	protected virtual void CullingSpine()
	{
		vertices[0] = new Vector2(MeshRenderer.bounds.min.x, MeshRenderer.bounds.min.y);
		vertices[1] = new Vector2(MeshRenderer.bounds.min.x, MeshRenderer.bounds.max.y);
		vertices[2] = new Vector2(MeshRenderer.bounds.max.x, MeshRenderer.bounds.min.y);
		vertices[3] = new Vector2(MeshRenderer.bounds.max.x, MeshRenderer.bounds.max.y);
		if (GraphicHelper.IsAnyInsideCamera(vertices, out var result))
		{
			SkeletonAnimation.enabled = result;
		}
	}

	private bool IsSameAnimationRequest(TrackEntry trackEntry, AnimationRequest request)
	{
		if (trackEntry == null)
		{
			return false;
		}
		if ((trackEntry.Animation?.Name ?? string.Empty) == request.Name && trackEntry.Loop == request.Loop)
		{
			return Mathf.Abs(trackEntry.TimeScale - request.TimeScale) <= Mathf.Epsilon;
		}
		return false;
	}

	public void SetAnimation(string animationName, bool loop = true, float timeScale = 1f, int trackIndex = 0, float startTime = 0f, bool vfxEnable = true)
	{
		if (vfxEnable)
		{
			EnableVFX(animationName, loop);
		}
		SetAnimation(new AnimationRequest(animationName ?? "", loop, timeScale, startTime), trackIndex);
	}

	private void SetAnimation(AnimationRequest request, int trackIndex = 0)
	{
		TrackEntry trackEntry = SkeletonAnimation.AnimationState?.GetCurrent(trackIndex);
		if (!IsSameAnimationRequest(trackEntry, request))
		{
			if (SkeletonData.FindAnimation(request.Name) == null)
			{
				DebugLog.GeneralLogWarning(base.gameObject.name + "에게 " + request.Name + " 애니메이션이 존재하지 않습니다! SetAnimation을 무시합니다.");
				return;
			}
			TrackEntry trackEntry2 = SkeletonAnimation.AnimationState.SetAnimation(trackIndex, request.Name ?? "", request.Loop);
			trackEntry2.TimeScale = request.TimeScale;
			trackEntry2.AnimationStart = request.StartTime;
		}
	}

	public async UniTask SetAnimationAsync(string animationName, float timeScale = 1f, int trackIndex = 0, float startTime = 0f, bool vfxEnable = true, CancellationToken token = default(CancellationToken))
	{
		if (vfxEnable)
		{
			EnableVFX(animationName);
		}
		SetAnimation(new AnimationRequest(animationName ?? "", loop: false, timeScale, startTime), trackIndex);
		await UniTask.WaitUntil(() => IsAnimComplete(trackIndex, animationName), PlayerLoopTiming.Update, token);
	}

	public void SetAnimationInstantly(string animationName, bool loop = true, float startTime = 0f, float timeScale = 1f)
	{
		TrackEntry trackEntry = SkeletonAnimation.AnimationState.SetAnimation(0, animationName ?? "", loop);
		trackEntry.TimeScale = timeScale;
		trackEntry.AnimationStart = startTime;
		trackEntry.MixDuration = 0f;
		SkeletonAnimation.Update(0f);
	}

	public void SetAnimationInstantly(string animationName, bool loop, int trackIndex, float startTime = 0f, float timeScale = 1f)
	{
		TrackEntry trackEntry = SkeletonAnimation.AnimationState.SetAnimation(trackIndex, animationName ?? "", loop);
		trackEntry.TimeScale = timeScale;
		trackEntry.AnimationStart = startTime;
		trackEntry.MixDuration = 0f;
		SkeletonAnimation.Update(0f);
	}

	public void AddAnimation(string animationName, bool loop = true, float timeScale = 1f)
	{
		AddAnimation(new AnimationRequest(animationName ?? "", loop, timeScale, 0f));
	}

	private void AddAnimation(AnimationRequest request)
	{
		TrackEntry current = SkeletonAnimation.AnimationState.GetCurrent(0);
		if (!IsSameAnimationRequest(current, request))
		{
			if (SkeletonData.FindAnimation(request.Name) == null)
			{
				DebugLog.GeneralLogWarning(base.gameObject.name + "에게 " + request.Name + " 애니메이션이 존재하지 않습니다! AddAnimation을 무시합니다.");
			}
			else
			{
				SkeletonAnimation.AnimationState.AddAnimation(0, request.Name ?? "", request.Loop, 0f);
			}
		}
	}

	public void RemoveAnimation(int track = 0)
	{
		SkeletonAnimation.AnimationState.ClearTrack(track);
	}

	public void AddSkin(string skinName)
	{
		Skin skin = SkeletonData.FindSkin(skinName ?? "");
		this.skin.AddSkin(skin);
	}

	public void SetSkin(string skinName)
	{
		Skeleton.SetSkin(skinName);
		Skeleton.SetSlotsToSetupPose();
	}

	public void SetAttachment(string slotName, string regionName)
	{
		Atlas atlas = atlasAsset.GetAtlas();
		if (atlas != null)
		{
			Slot slot = Skeleton.FindSlot(slotName);
			AtlasRegion atlasRegion = atlas.FindRegion(regionName);
			float scale = SkeletonRenderer.skeletonDataAsset.scale;
			if (atlasRegion == null)
			{
				slot.Attachment = null;
				return;
			}
			RegionAttachment attachment = atlasRegion.ToRegionAttachment(atlasRegion.name, scale);
			slot.Attachment = attachment;
		}
	}

	public void AddAnimationEvent(AnimState state, Spine.AnimationState.TrackEntryDelegate entryDelegate)
	{
		switch (state)
		{
		case AnimState.Start:
			SkeletonAnimation.AnimationState.Start += entryDelegate;
			break;
		case AnimState.End:
			SkeletonAnimation.AnimationState.End += entryDelegate;
			break;
		case AnimState.Complete:
			SkeletonAnimation.AnimationState.Complete += entryDelegate;
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public void RemoveAnimationEvent(AnimState state, Spine.AnimationState.TrackEntryDelegate entryDelegate)
	{
		switch (state)
		{
		case AnimState.Start:
			SkeletonAnimation.AnimationState.Start -= entryDelegate;
			break;
		case AnimState.Complete:
			SkeletonAnimation.AnimationState.Complete -= entryDelegate;
			break;
		default:
			throw new ArgumentOutOfRangeException("state", state, null);
		}
	}

	public void AddSpineAnimEvent(string eventName, Action func)
	{
		if (_spineAnimEvents.Find((SpineAnimEvent e) => e.Equals(eventName, func)) == null)
		{
			SpineAnimEvent spineAnimEvent = new SpineAnimEvent(eventName, func);
			_spineAnimEvents.Add(spineAnimEvent);
			SkeletonAnimation.AnimationState.Event += spineAnimEvent.HandleSpineAnimEvent;
		}
	}

	public void RemoveSpineAnimEvent(string eventName)
	{
		SpineAnimEvent spineAnimEvent = _spineAnimEvents.Find((SpineAnimEvent e) => e.EventName == eventName);
		if (spineAnimEvent != null)
		{
			SkeletonAnimation.AnimationState.Event -= spineAnimEvent.HandleSpineAnimEvent;
			_spineAnimEvents.Remove(spineAnimEvent);
		}
	}

	public void PauseAnimation()
	{
		skeletonAnimation.AnimationState.TimeScale = 0f;
	}

	public void ResumeAnimation()
	{
		skeletonAnimation.AnimationState.TimeScale = 1f;
	}

	public void EnableVFX(string animationName, bool loop = false, float time = 0f)
	{
		VfxController.Play(animationName, loop, time);
	}

	public void DisableVFX(string animationName)
	{
		VfxController.Stop(animationName);
	}

	public void DisableVFXAll()
	{
		VfxController.StopAll();
	}

	public Material GetMaterial()
	{
		return SkeletonAnimation.SkeletonDataAsset.atlasAssets[0].PrimaryMaterial;
	}

	public void SetOverrideMaterial(Material mat)
	{
		SkeletonAnimation.CustomMaterialOverride[GetMaterial()] = mat;
	}

	public void ResetMaterial()
	{
		SkeletonAnimation.CustomMaterialOverride.Remove(GetMaterial());
	}

	public virtual void SetPropertyBlock(MaterialPropertyBlock block)
	{
		MeshRenderer.SetPropertyBlock(block);
	}

	public bool IsAnimComplete(int trackIndex)
	{
		return SkeletonAnimation.AnimationState.GetCurrent(trackIndex).IsComplete;
	}

	public bool IsAnimComplete(int trackIndex, string animName)
	{
		if (SkeletonData.FindAnimation(animName) == null)
		{
			DebugLog.GeneralLogError(animName + " not found in " + Name + "'s skeleton");
			return false;
		}
		if (GetCurrentAnim(trackIndex).Name == animName)
		{
			return IsAnimComplete(trackIndex);
		}
		return false;
	}

	public float GetAnimDuration(string animName)
	{
		Spine.Animation animation = SkeletonData.FindAnimation(animName);
		if (animation == null)
		{
			DebugLog.GeneralLogWarning(animName + " not found in " + Name + "'s skeleton");
		}
		return animation?.Duration ?? 0f;
	}

	public void ClearAnimation(int track = 0)
	{
		SkeletonAnimation.AnimationState.ClearTrack(track);
		SkeletonRenderer.Skeleton.SetToSetupPose();
	}

	public TrackEntry GetCurrentTrack(int track = 0)
	{
		return SkeletonAnimation.AnimationState.GetCurrent(track);
	}

	public async UniTaskVoid PauseAndJumpAnimation(int frame, CancellationToken token, int track = 0)
	{
		SkeletonAnimation.timeScale = 0f;
		float currentTime = GetCurrentTrack(track).TrackTime;
		float pauseFrame = (float)frame / 60f;
		await UniTask.WaitForSeconds(pauseFrame, ignoreTimeScale: false, PlayerLoopTiming.Update, token);
		GetCurrentTrack(track).TrackTime = currentTime + pauseFrame;
		SkeletonAnimation.Update(0f);
		SkeletonAnimation.timeScale = 1f;
	}

	public bool HasAnimation(string animationName)
	{
		foreach (Spine.Animation animation in SkeletonData.Animations)
		{
			if (animation.Name == animationName)
			{
				return true;
			}
		}
		return false;
	}

	protected virtual async UniTask DamageEffectAsync()
	{
		if (Game.Instance.SettingData.DamageEffect)
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			materialPropertyBlock.SetFloat(HitBlend, 0f);
			SetPropertyBlock(materialPropertyBlock);
			DG.Tweening.Sequence sequence = DOTween.Sequence();
			sequence.Append(DOFill(materialPropertyBlock, 1f, 5).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed)).Append(DOFill(materialPropertyBlock, 0f, 2).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed));
			await sequence.WaitAsync();
		}
		Tween DOFill(MaterialPropertyBlock propertyBlock, float endValue, int frame)
		{
			return DOTween.To(() => propertyBlock.GetFloat(HitBlend), delegate(float x)
			{
				propertyBlock.SetFloat(HitBlend, x);
				SetPropertyBlock(propertyBlock);
			}, endValue, (float)frame / 60f);
		}
	}

	public void Flip()
	{
		Flip(FacingDirection.Opposite());
	}

	public void Flip(float dir)
	{
		if (dir != 0f)
		{
			Direction direction = ((dir > 0f) ? Direction.Right : Direction.Left);
			Flip(direction);
		}
	}

	public void Flip(Direction direction)
	{
		Vector3 localScale = base.transform.localScale;
		if (direction == Direction.Left || direction == Direction.Right)
		{
			Vector3 localScale2 = ((direction == Direction.Right) ? new Vector3(Mathf.Abs(localScale.x), localScale.y, localScale.z) : new Vector3(0f - Mathf.Abs(localScale.x), localScale.y, localScale.z));
			base.transform.localScale = localScale2;
		}
	}

	private void OnDisable()
	{
		VfxController?.StopAll();
	}

	public async UniTaskVoid SetColorAsync(Color color, float fillPhase, int frame)
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetColor(HitColor, color);
		materialPropertyBlock.SetFloat(HitBlend, 0f);
		SetPropertyBlock(materialPropertyBlock);
		DG.Tweening.Sequence sequence = DOTween.Sequence();
		sequence.Append(DOFill(materialPropertyBlock, fillPhase, frame).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed));
		await sequence.WaitAsync();
		Tween DOFill(MaterialPropertyBlock propertyBlock, float endValue, int duration)
		{
			return DOTween.To(() => propertyBlock.GetFloat(HitBlend), delegate(float x)
			{
				propertyBlock.SetFloat(HitBlend, x);
				SetPropertyBlock(propertyBlock);
			}, endValue, (float)duration / 60f);
		}
	}

	protected async UniTask SetTintColorAsync(Color color, int frame)
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetColor(TintColor, Color.white);
		SetPropertyBlock(materialPropertyBlock);
		DG.Tweening.Sequence sequence = DOTween.Sequence();
		sequence.Append(DOTint(materialPropertyBlock, color, frame).SetEase(Ease.Linear).SetUpdate(UpdateType.Fixed));
		await sequence.WaitAsync();
		Tween DOTint(MaterialPropertyBlock mpb, Color endValue, int num)
		{
			return DOTween.To(() => mpb.GetColor(TintColor), delegate(Color x)
			{
				mpb.SetColor(TintColor, x);
				SetPropertyBlock(mpb);
			}, endValue, (float)num / 60f);
		}
	}

	protected virtual void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawSphere(VisualCenter, 0.3f);
	}
}
