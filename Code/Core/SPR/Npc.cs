using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using SPR.UI;
using SPR.UI.Field;
using SPR.UI.Map;
using SPR.UI.Memory;
using SPR.UI.Talk;
using UnityEngine;

namespace SPR;

public abstract class Npc : SpineObject, IInteractable, IMappable
{
	[SerializeField]
	protected ActorName npcName;

	[SerializeField]
	protected Vector2Int interactCanvasPosition;

	[SerializeField]
	protected float playerInteractPositionX;

	[Header("Camera")]
	[SerializeField]
	private Vector2 cameraOffset = Vector2.zero;

	[SerializeField]
	private float cameraSize = 12f;

	[Header("Sight")]
	[SerializeField]
	private Zone sightZone;

	[SerializeField]
	private float sightLeftPower;

	[SerializeField]
	private float sightRightPower;

	[SerializeField]
	private bool chaseSightOnTalk;

	[Header("Talk Key")]
	[SerializeField]
	protected string talkAnimationKey;

	[Header("Animation Delay")]
	[SerializeField]
	private float finishAnimationDelay;

	private List<string> scenarioIds = new List<string>();

	private FieldInteractCanvas interactCanvas;

	private Collider2D interactionArea;

	private Vector2 currentSight;

	private float currentSightX;

	private bool isTalking;

	private bool isChasing;

	protected bool wasCleared;

	private float maxSightDistance;

	private int interactableOrder;

	private const float BlendingDuration = 0.5f;

	private const float MoveSpeed = 7.2f;

	private TalkVcam talkVcam;

	protected string CurrentScenarioID => GetCurrentScenarioID();

	public string ID => $"{CurrentFieldID}_{npcName}";

	public override string Name => npcName.ToString();

	public bool IsNull => this.IsNull();

	public virtual int InteractableOrder => 5;

	public ActorName NpcName => npcName;

	protected static PlayerController Player => Game.Instance.Player;

	protected static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	protected static FieldID CurrentFieldID => Game.FieldID;

	protected static WorldData WorldData => Game.Instance.SaveData.WorldData;

	protected static TalkData TalkData => Game.Instance.SaveData.TalkData;

	protected static InputManager InputManager => Game.Instance.InputManager;

	protected static FieldCanvas FieldCanvas => Game.Instance.UIManager.FieldCanvas;

	protected static TalkCanvas TalkCanvas => Game.Instance.UIManager.TalkCanvas;

	protected static FadeCanvas FadeCanvas => Game.Instance.UIManager.FadeCanvas;

	protected static MenuMemoryPanel MemoryPanel => Game.Instance.UIManager.MenuCanvas.MemoryPanel;

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	private static ObjectPoolManager PoolManager => Game.Instance.ObjectPoolManager;

	protected string TalkPostfix
	{
		get
		{
			if (!talkAnimationKey.IsNullOrEmpty())
			{
				return "_" + talkAnimationKey;
			}
			return "";
		}
	}

	private Vector2 Displacement => Player.mover.VisualCenter - VisualBottom;

	private Zone RelativeSightZone => sightZone.GetZoneOf(this);

	private bool TryChaseSight
	{
		get
		{
			if (ShouldChaseSight)
			{
				return RelativeSightZone.Contains(Player.mover.VisualCenter);
			}
			return false;
		}
	}

	private float PlayerInteractPositionX => VisualBottom.x + (float)FacingDirection.GetSignX() * playerInteractPositionX;

	protected float InteractPositionX => playerInteractPositionX;

	protected bool ShouldChaseSight { get; set; } = true;

	protected override void Update()
	{
		base.Update();
		Util.CheckShownInCamera(CurrentScenarioID, VisualBottom);
	}

	public void LateUpdate()
	{
		if (TryChaseSight && !isChasing)
		{
			StartChasingSight(startChasing: true);
		}
		else if (!TryChaseSight && isChasing)
		{
			StartChasingSight(startChasing: false);
		}
		ChaseSight();
	}

	private void Awake()
	{
		maxSightDistance = Mathf.Min(VisualBottom.x - RelativeSightZone.LowerLeft.x, RelativeSightZone.UpperRight.x - VisualBottom.x);
	}

	public virtual void AwakeObject(SaveData saveData)
	{
		if (CheckTriggerDisable(ID))
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		scenarioIds = ScenarioHelper.GetScenarioKeys(CurrentScenarioID);
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas canvas)
		{
			interactCanvas = canvas;
		}).Forget();
		if (base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: false);
			base.gameObject.SetActive(value: true);
		}
	}

	public static bool CheckTriggerDisable(string id)
	{
		string id2 = "Trigger_NPC_" + id;
		if (Trigger.IsTriggerExist(id2))
		{
			return !Trigger.CheckTrigger(id2);
		}
		return false;
	}

	public virtual void Interact()
	{
		UniTask task = StartTalkAsync();
		InputManager.BlockControlForTask(task, $"Talk to {npcName}").Forget();
	}

	private async UniTask StartTalkAsync()
	{
		await PrepareForScenario();
		wasCleared = MenuMemoryPanel.IsMemoryCleared(npcName);
		StartTalk();
		if (!MenuMemoryPanel.IsMemoryKnown(npcName, TalkData))
		{
			MemoryPanel.UpdateMemory(npcName, isClear: false);
		}
		AddInteractCount(CurrentFieldID);
	}

	private async UniTask PrepareForScenario()
	{
		UniTask uniTask = MovePlayerBeforeTalkAsync();
		UniTask uniTask2 = MoveNPCBeforeTalkAsync();
		await UniTask.WhenAll(uniTask, uniTask2);
		ActivateTalkVcam();
		isTalking = true;
		PauseChasingSight();
		EnterTalkAnimation();
		await WaitBeforeTalk();
	}

	protected void ActivateTalkVcam()
	{
		talkVcam = PoolManager.GetObject<TalkVcam>("Talk Vcam.prefab");
		Vector2 vector = new Vector2((base.transform.position.x + Player.mover.VisualBottom.x) / 2f, Player.mover.VisualBottom.y + 6f);
		talkVcam.Activate(vector + cameraOffset, cameraSize);
	}

	protected virtual async UniTask MovePlayerBeforeTalkAsync()
	{
		if (Player.StatusEffectController.Contain<WetStatusEffect>())
		{
			Player.mover.SetAnimation("shake_off", loop: false);
			Player.mover.AddAnimation("idle");
			await UniTask.WaitUntil(() => Player.mover.GetCurrentAnim(0).Name == "idle");
			Player.StatusEffectController.RemoveEffect<WetStatusEffect>();
		}
		await Player.MoveXPositionAsync(PlayerInteractPositionX, isRunning: true);
		Player.mover.Flip((!(Displacement.x > 0f)) ? Direction.Right : Direction.Left);
		await UniTask.WaitUntil(() => Player.mover.GetCurrentAnim(0).Name == "idle");
	}

	protected virtual async UniTask MoveNPCBeforeTalkAsync()
	{
		await UniTask.CompletedTask;
	}

	protected virtual void StartTalk()
	{
		PlayTotAnimationOnPrepare(Player.mover);
		Scenario scenario = ScenarioHelper.GetScenario(CurrentScenarioID);
		TalkCanvas.StartTalk(scenario, FinishTalk);
	}

	protected virtual void AddInteractCount(FieldID fieldID)
	{
		TalkData.AddInteractCount(npcName, fieldID);
	}

	public virtual void InteractEnter()
	{
		interactCanvas.SetActive(value: true);
	}

	public virtual void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}

	protected virtual void PlayTotAnimationOnPrepare(PlayerMover mover)
	{
		mover.SetAnimation("cam_up");
	}

	protected virtual void PlayTotAnimationOnFinished(PlayerMover mover)
	{
		mover.SetAnimation("idle");
	}

	protected void FinishTalk()
	{
		PlayTotAnimationOnFinished(Player.mover);
		DisableTalkVcam();
		isTalking = false;
		MemoryPanel.TryUpdateClearedMemory(wasCleared, npcName);
		if (finishAnimationDelay == 0f)
		{
			Finish();
		}
		else
		{
			UniTask.WaitForSeconds(finishAnimationDelay).GetAwaiter().OnCompleted(Finish);
		}
		void Finish()
		{
			if (!isTalking)
			{
				OnFinishTalk();
			}
		}
	}

	public void DisableTalkVcam()
	{
		talkVcam?.Deactivate();
		talkVcam = null;
	}

	protected virtual void OnFinishTalk()
	{
		ResumeChasingSight();
		EnterIdleAnimation();
	}

	private void ChaseSight()
	{
		if (Time.timeScale != 0f)
		{
			currentSightX = GetSightVector();
			if (currentSightX == 0f)
			{
				base.SkeletonAnimation.AnimationState.SetEmptyAnimation(1, 0.5f);
			}
			else
			{
				base.SkeletonAnimation.AnimationState.SetAnimation(1, (currentSightX > 0f) ? "ct_l" : "ct_r", loop: true).MixDuration = 0.5f;
			}
		}
	}

	protected void StartChasingSight(bool startChasing)
	{
		isChasing = startChasing;
	}

	protected void PauseChasingSight()
	{
		if (!chaseSightOnTalk)
		{
			ShouldChaseSight = false;
			StartChasingSight(startChasing: false);
		}
	}

	protected void ResumeChasingSight()
	{
		if (!chaseSightOnTalk)
		{
			ShouldChaseSight = true;
			StartChasingSight(startChasing: true);
		}
	}

	private float GetSightVector()
	{
		if (!isChasing)
		{
			return 0f;
		}
		Vector2 displacement = Displacement;
		displacement.x *= ((base.transform.localScale.x > 0f) ? 1 : (-1));
		Vector2 normalized = displacement.normalized;
		float num = Mathf.Clamp(displacement.magnitude, 0f, maxSightDistance) / maxSightDistance;
		return normalized.x * num * ((displacement.x > 0f) ? sightLeftPower : sightRightPower);
	}

	protected virtual void EnterIdleAnimation()
	{
		SetAnimation("idle");
	}

	protected virtual void EnterTalkAnimation()
	{
		SetAnimation("idle_talk" + TalkPostfix);
	}

	protected virtual string GetCurrentScenarioID()
	{
		return $"{CurrentFieldID}_{npcName}";
	}

	protected virtual UniTask WaitBeforeTalk()
	{
		return UniTask.CompletedTask;
	}

	public async UniTask MoveAsync(Vector2 dest)
	{
		float duration = (dest - (Vector2)base.transform.position).magnitude / 7.2f;
		Vector3 dept = base.transform.position;
		AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
		for (float timer = 0f; timer < duration; timer += Time.fixedDeltaTime)
		{
			float t = curve.Evaluate(timer / duration);
			Vector2 vector = Vector2.Lerp(dept, dest, t);
			base.transform.position = vector;
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(1);
		}
	}

	protected void ChangeInteractCanvasPosition(Vector2 position)
	{
		interactCanvas.ChangePosX(position.x);
		interactCanvas.ChangeHeight(position.y);
	}

	public virtual async UniTask DisappearAsync()
	{
		await UniTask.Yield();
	}

	public void DisappearWithFade()
	{
		UniTask task = DisappearWithFadeAsync();
		InputManager.BlockControlForTask(task, Name + " Disappear").Forget();
		MemoryPanel.TryUpdateClearedMemory(wasCleared, npcName);
	}

	protected virtual async UniTask DisappearWithFadeAsync()
	{
		await FadeCanvas.FadeOut(0.5f);
		base.gameObject.SetActive(value: false);
		await UniTask.WaitForSeconds(1);
		FieldCanvas.HideAllAsync(instant: true).Forget();
		await FadeCanvas.FadeIn(0.5f);
		FieldCanvas.ShowAllAsync(instant: false).Forget();
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(RelativeSightZone.Center, RelativeSightZone.Size);
		Gizmos.color = new Color(0f, 0.75f, 1f, 1f);
		Gizmos.DrawWireSphere(new Vector2(PlayerInteractPositionX, RelativeSightZone.LowerLeft.y + 1f), 0.5f);
		Gizmos.color = Color.gray;
		Gizmos.DrawWireCube(base.transform.position + interactCanvasPosition.ToVector3(), Vector2.one);
		Vector3 position = base.transform.position;
		base.transform.position = position.RoundToInt();
	}

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		Vector2 position = base.transform.position.ToVector2() - center;
		return new MapObjectInfo(MapObjectType.Npc, position, (int)npcName, ID);
	}
}
