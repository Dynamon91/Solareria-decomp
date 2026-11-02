using System.Linq;
using Cysharp.Threading.Tasks;
using SPR.Field;
using SPR.Library;
using SPR.Save;
using SPR.UI.Upgrade;
using UnityEngine;

namespace SPR;

public class Opus : ServiceNPC<Unit>
{
	[Header("Opus")]
	[SerializeField]
	private BoxCollider2D doorChecker;

	[SerializeField]
	private Vector2Int lookFrontCanvasPosition;

	private bool isServiceOn;

	private bool wasDoorAlive;

	private const string IdleAnimation = "turn_idle";

	private const string TalkAnimation = "idle_gaze";

	private static bool IsFirst => Trigger.CheckTrigger("Trigger_OpusShouldHello");

	private bool IsLookingPlayer => GetCurrentAnim(0).Name == "idle_gaze";

	protected override ServicePanel<Unit> ServicePanel => Npc.FieldCanvas.CardEngravingPanel;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		SetAnimation("turn_idle");
		isServiceOn = false;
		if (IsFirst)
		{
			wasDoorAlive = true;
		}
	}

	private void FixedUpdate()
	{
		if (!Npc.InputManager.IsCurrentControlTarget(Game.Instance.Player))
		{
			return;
		}
		if (IsFirst)
		{
			Collider2D[] source = AttackUtility.OverlapBoxAll(doorChecker.transform.position, doorChecker.size, LayerUtility.PlatformLayer);
			if (wasDoorAlive && !source.Any((Collider2D c) => c.TryGetComponent<BreakableWall>(out var _)))
			{
				Interact();
				wasDoorAlive = false;
				return;
			}
		}
		if (IsLookingPlayer && IsPlayerFarAwayFromOpus())
		{
			EnterIdleAnimation();
		}
	}

	public override void InteractEnter()
	{
		ChangeInteractCanvasPosition(IsLookingPlayer ? lookFrontCanvasPosition : interactCanvasPosition);
		base.InteractEnter();
	}

	public override void OnServiceOpening()
	{
		isServiceOn = true;
	}

	public override void OnServiceClosing()
	{
		isServiceOn = false;
	}

	protected override UniTask MovePlayerBeforeTalkAsync()
	{
		if (!IsFirst)
		{
			return base.MovePlayerBeforeTalkAsync();
		}
		return UniTask.WaitUntil(() => Npc.Player.mover.GetCurrentAnim(0).Name == "idle");
	}

	protected override void EnterTalkAnimation()
	{
		if (!IsFirst && !IsLookingPlayer)
		{
			SetAnimation("turn_normal", loop: false);
			AddAnimation("idle_gaze");
		}
	}

	protected override void EnterIdleAnimation()
	{
		if (!IsLookingPlayer || IsPlayerFarAwayFromOpus())
		{
			SetAnimation("turn_back", loop: false);
			AddAnimation("turn_idle");
		}
	}

	protected override void OnFinishTalk()
	{
		if (!isServiceOn)
		{
			base.OnFinishTalk();
		}
	}

	private bool IsPlayerFarAwayFromOpus()
	{
		return Vector2.Distance(VisualCenter, Npc.Player.mover.VisualCenter) > 21f;
	}
}
