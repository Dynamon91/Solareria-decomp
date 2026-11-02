using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMOD.Studio;
using FMODUnity;
using Library;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR.Field;

public abstract class InteractableLever : MonoBehaviour, IStateSavable, IInteractable
{
	[SerializeField]
	private Vector2Int interactCanvasPosition = new Vector2Int(0, 5);

	[SerializeField]
	private Transform handleTransform;

	[SerializeField]
	private EventReference interactEvent;

	[Space]
	[SerializeField]
	private int startFrameCount = 60;

	[SerializeField]
	private int pullFrameCount = 60;

	protected static readonly int Lock = Animator.StringToHash("Lock");

	protected const int DefaultState = 0;

	public const int UnlockState = 1;

	private const float StartSpeedFactor = 0.5f;

	protected int CurrentState;

	protected float InteractDistance;

	private FieldInteractCanvas interactCanvas;

	private Area area;

	private Animator animator;

	private bool isHandleMoving;

	private EventInstance interactInstance;

	public abstract string ID { get; }

	public string Name => ID;

	public bool IsNull => base.gameObject.IsNull();

	public virtual int InteractableOrder => 0;

	protected virtual bool CanInteractOnce => true;

	protected Area Area => area ?? (area = GetComponentInParent<Area>());

	protected Animator Animator => animator ?? (animator = GetComponent<Animator>());

	public void AwakeObject(SaveData saveData)
	{
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
		{
			interactCanvas = result;
		}).Forget();
	}

	public virtual void Interact()
	{
		PlayerStateMachine pSM = Game.Instance.Player.mover.PSM;
		if ((object)pSM.BurstInputItem.TargetEnemy == null && !isHandleMoving && (!CanInteractOnce || CurrentState != 1) && (pSM.CurrentState == pSM.IdleState || pSM.CurrentState == pSM.WalkState || pSM.CurrentState == pSM.RunState))
		{
			interactCanvas.SetActive(value: false);
			InteractAsync().Forget();
		}
	}

	public virtual void InteractEnter()
	{
		if (!isHandleMoving && (!CanInteractOnce || CurrentState == 0))
		{
			interactCanvas.SetActive(value: true);
		}
	}

	public void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}

	public virtual void LoadState(int state)
	{
		Animator.speed = 1f;
		Animator.SetBool(Lock, state == 0);
		CurrentState = state;
		InteractDistance = -2f;
	}

	public void ChangeState(int state)
	{
		CurrentState = state;
		if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
		{
			data.SaveObjectState(ID, state);
			OnChangeState(state);
		}
	}

	protected virtual void OnChangeState(int state)
	{
	}

	protected async UniTask CancelAsync()
	{
		interactCanvas.SetActive(value: true);
		await OnCancelAsync();
	}

	private async UniTask InteractAsync()
	{
		isHandleMoving = true;
		if (!(await PullAsync()))
		{
			await CancelAsync();
		}
		else
		{
			await ActivateAsync();
		}
		isHandleMoving = false;
	}

	private async UniTask<bool> PullAsync()
	{
		PlayerStateMachine psm = Game.Instance.Player.mover.PSM;
		PlayerLeverInteractState state = new PlayerLeverInteractState(psm, base.transform.position.x, InteractDistance, 0.5f, startFrameCount, pullFrameCount);
		psm.ChangeState(state);
		LazySingleton<MessageSystem>.Instance.Publish(new LeverPullEvent());
		await UniTask.WaitUntil(() => state.IsPulling || !state.CanControl);
		if (!state.IsPulling)
		{
			return false;
		}
		Animator.SetBool(Lock, value: false);
		Animator.speed = 60f / (float)pullFrameCount;
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(pullFrameCount);
		Animator.speed = 1f;
		await UniTask.WaitWhile(() => state.CanControl);
		return psm.CurrentState == state;
	}

	private async UniTask ActivateAsync()
	{
		handleTransform.DOComplete();
		await UniTask.WaitUntil(() => Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.99f);
		await OnActivateAsync();
	}

	protected abstract UniTask OnActivateAsync();

	protected virtual async UniTask OnCancelAsync()
	{
		await ReturnHandleAsync();
	}

	protected async UniTask ReturnHandleAsync(float angle = 0f)
	{
		Animator.enabled = false;
		Animator.speed = 1f;
		Animator.SetBool(Lock, value: true);
		if (interactInstance.isValid())
		{
			interactInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
			interactInstance.release();
		}
		await handleTransform.DOLocalRotate(new Vector3(0f, 0f, angle), 1f).WaitAsync();
		Animator.enabled = true;
	}

	public void PlayInteractSFX()
	{
		interactInstance = Game.Instance.SoundManager.PlayOneShotAttached(interactEvent, base.transform);
	}
}
