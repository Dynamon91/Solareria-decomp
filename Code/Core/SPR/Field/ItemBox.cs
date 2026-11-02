using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using LapinLibrary;
using Library;
using SPR.Save;
using SPR.UI;
using SPR.UI.Map;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Field;

public class ItemBox : MonoBehaviour, IStateSavable, IInteractable, IMappable
{
	[SerializeField]
	private Vector2Int interactCanvasPosition = new Vector2Int(0, 5);

	[SerializeField]
	private string itemID;

	[SerializeField]
	private MeshRenderer dissolveRenderer;

	[Header("References")]
	[SerializeField]
	protected Transform lightMaskTransform;

	[SerializeField]
	protected List<SpriteRenderer> lightRenderers;

	[Header("Sfx")]
	[SerializeField]
	protected EventReference pushEvent;

	[SerializeField]
	protected EventReference openEvent;

	private BoxCollider2D coll;

	private PlayableDirector director;

	private SkeletonAnimation sa;

	private const int DefaultState = 0;

	private const int OpenState = 1;

	private const int PushDelayFrame = 30;

	private const int PushDurationFrame = 180;

	private const int ItemDropDelayFrame = 250;

	private const string CloseLoopAnim = "close_loop";

	private const string OpenLoopAnim = "open_loop";

	private static readonly int FadeAmountID = Shader.PropertyToID("_FadeAmount");

	private Tween materialBlockTween;

	private FieldInteractCanvas interactCanvas;

	private bool isInteracting;

	private EventInstance pushInstance;

	private static readonly int Alpha = Shader.PropertyToID("_Alpha");

	private static readonly int Glow = Shader.PropertyToID("_Glow");

	public string ID => "ItemBox_" + Game.FieldID.ToString() + "_" + itemID;

	public string Name => ID;

	public bool IsNull => BasicExtension.IsNull(base.gameObject);

	public int InteractableOrder => 0;

	private BoxCollider2D BoxColl => coll ?? (coll = GetComponent<BoxCollider2D>());

	protected PlayableDirector PlayableDirector => director ?? (director = GetComponentInChildren<PlayableDirector>());

	private SkeletonAnimation SkeletonAnimation => sa ?? (sa = GetComponentInChildren<SkeletonAnimation>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private void OnDrawGizmosSelected()
	{
		Vector3 position = base.transform.position;
		position.x = (float)Mathf.CeilToInt(position.x) - 0.5f;
		position.y = (float)Mathf.CeilToInt(position.y) - 0.5f;
		base.transform.position = position;
	}

	public void LoadState(int state)
	{
		bool flag = state == 0;
		BoxColl.enabled = flag;
		SkeletonAnimation.AnimationState.SetAnimation(0, flag ? "close_loop" : "open_loop", loop: true);
		if (flag)
		{
			FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
			{
				interactCanvas = result;
			}).Forget();
		}
	}

	public void ChangeState(int state)
	{
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
		}
	}

	public void AwakeObject(SaveData saveData)
	{
	}

	private void Update()
	{
		Util.CheckShownInCamera(ID, base.transform.position);
	}

	public void Interact()
	{
		if (!isInteracting)
		{
			interactCanvas.SetActive(value: false);
			InteractAsync().Forget();
		}
	}

	public void InteractEnter()
	{
		if (!isInteracting)
		{
			interactCanvas.SetActive(value: true);
		}
	}

	public void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}

	private async UniTask InteractAsync()
	{
		isInteracting = true;
		BoxColl.enabled = false;
		PlayerStateMachine psm = Game.Instance.Player.mover.PSM;
		int sign = base.transform.localScale.x.GetSign();
		float x = base.transform.position.x;
		PlayerBoxInteractState boxInteractState = new PlayerBoxInteractState(psm, x + 2.5f * (float)(-sign), x, 0.5f, 30, 180, 250);
		psm.ChangeState(boxInteractState);
		await UniTask.WaitUntil(() => boxInteractState.IsBoxPushed || psm.CurrentState != boxInteractState);
		if (boxInteractState.IsBoxPushed)
		{
			CancellationTokenSource tokenSource = new CancellationTokenSource();
			CancellationToken token = TaskUtil.RefreshToken(ref tokenSource);
			PlayPushAsync(token).Forget();
			pushInstance = SoundManager.PlayOneShotAttached(pushEvent, base.transform);
			bool result = true;
			for (int i = 0; i < 180; i++)
			{
				if (psm.CurrentState != boxInteractState)
				{
					result = false;
					break;
				}
				if (!pushInstance.isValid())
				{
					_ = i;
					_ = 60;
				}
				await UniTaskExtension.WaitOneFrame();
			}
			if (result)
			{
				SoundManager.PlayOneShot(openEvent);
				await DropAsync();
			}
			else
			{
				tokenSource.Cancel();
				pushInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
				pushInstance.release();
				await ResetAsync(1f);
			}
		}
		else
		{
			await UniTask.WaitForSeconds(1);
			BoxColl.enabled = true;
		}
		isInteracting = false;
	}

	private async UniTask DropAsync()
	{
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(70);
		await DropItemAsync();
		ChangeState(1);
	}

	protected virtual async UniTask ResetAsync(float duration)
	{
		SkeletonAnimation.SetAnimation("close_loop", 0, loop: true, 0f, 1f, isImmediate: false, duration);
		MaterialPropertyBlock block = new MaterialPropertyBlock();
		materialBlockTween = block.DOFloatPropertyBlock(FadeAmountID, 0.5f, duration, dissolveRenderer);
		foreach (SpriteRenderer lightRenderer in lightRenderers)
		{
			Material material = lightRenderer.material;
			material.DOFloat(0f, Alpha, duration);
			material.DOFloat(0f, Glow, duration);
		}
		await UniTask.WaitForSeconds(duration);
		BoxColl.enabled = true;
	}

	protected virtual async UniTask PlayPushAsync(CancellationToken token)
	{
		SkeletonAnimation.AnimationState.SetAnimation(0, "push", loop: false);
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		materialPropertyBlock.SetFloat(FadeAmountID, 0.5f);
		dissolveRenderer.SetPropertyBlock(materialPropertyBlock);
		materialBlockTween = materialPropertyBlock.DOFloatPropertyBlock(FadeAmountID, -0.0015f, 3f, dissolveRenderer);
		await UniTaskExtension.WaitUntilOrCancel(() => SkeletonAnimation.IsAnimationComplete(), token);
		if (token.IsCancellationRequested)
		{
			materialBlockTween.Kill();
			return;
		}
		SkeletonAnimation.AnimationState.SetAnimation(0, "open", loop: false);
		Sequence lightSeq = GetLightSequence();
		await UniTaskExtension.WhenAllOrCancelBy(materialBlockTween.WaitAsync(token), lightSeq.WaitAsync(token), () => token.IsCancellationRequested);
		if (token.IsCancellationRequested)
		{
			materialBlockTween.Kill();
			lightSeq.Kill();
		}
		else
		{
			OnCompletePush();
		}
	}

	protected virtual Sequence GetLightSequence()
	{
		Sequence sequence = DOTween.Sequence();
		sequence.AppendCallback(delegate
		{
			Vector3 position = lightMaskTransform.position;
			position.y = 9f;
			lightMaskTransform.position = position;
		});
		sequence.AppendInterval(Util.FrameToSeconds(10f)).Append(lightMaskTransform.DOLocalMoveY(4f, Util.FrameToSeconds(7f)).SetEase(Ease.Linear)).Append(lightMaskTransform.DOLocalMoveY(2.64f, Util.FrameToSeconds(8f)))
			.Append(lightMaskTransform.DOLocalMoveY(2.46f, Util.FrameToSeconds(10f)))
			.Append(lightMaskTransform.DOLocalMoveY(0.7f, Util.FrameToSeconds(20f)));
		foreach (SpriteRenderer lightRenderer in lightRenderers)
		{
			Material material = lightRenderer.material;
			sequence.InsertCallback(0f, delegate
			{
				material.SetFloat(Alpha, 0f);
				material.SetFloat(Glow, 0f);
			});
			sequence.Insert(Util.FrameToSeconds(10f), material.DOFloat(0.8f, Alpha, Util.FrameToSeconds(45f))).Join(material.DOFloat(2.5f, Glow, Util.FrameToSeconds(45f)));
		}
		return sequence;
	}

	protected virtual void OnCompletePush()
	{
		SkeletonAnimation.AnimationState.SetAnimation(0, "open_loop", loop: true);
		foreach (SpriteRenderer lightRenderer in lightRenderers)
		{
			Material material = lightRenderer.material;
			material.DOFloat(0f, Alpha, Util.FrameToSeconds(105f));
			material.DOFloat(0f, Glow, Util.FrameToSeconds(105f));
		}
	}

	private async UniTask DropItemAsync()
	{
		int sign = base.transform.localScale.x.GetSign();
		Vector2 startPos = (Vector2)base.transform.position + new Vector2(-sign, 0f);
		Vector2 visualCenter = Game.Instance.Player.mover.VisualCenter;
		Vector2Int endPos = new Vector2(startPos.x + BoxColl.size.x / 2f * (float)(-sign), visualCenter.y).ToVector2Int();
		await ItemObject.DropItemAsync(itemID, startPos, endPos);
	}

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		Vector2 position = base.transform.position.ToVector2() - new Vector2(0f, BoxColl.size.y / 2f) - center;
		return new MapObjectInfo(itemID.Contains("Relic") ? MapObjectType.Relics : MapObjectType.ItemBox, position, ID);
	}
}
