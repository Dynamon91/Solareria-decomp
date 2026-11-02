using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FSM;
using Library;
using SPR.FSM;
using SPR.Field;
using SPR.Library;
using SPR.Save;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SPR;

public abstract class PlayerDashState : PlayerUpgradeableState
{
	private readonly List<InputItem> inputItems = new List<InputItem>();

	private static readonly List<Type> NextInputItemTypes = new List<Type>
	{
		typeof(JumpUpInputItem),
		typeof(DoubleJumpUpInputItem),
		typeof(AshBreakInputItem),
		typeof(AshBlastInputItem),
		typeof(BasicAttackInputItem),
		typeof(ChargeAttackInputItem),
		typeof(FirstAirAttackInputItem),
		typeof(FastDownInputItem),
		typeof(JumpDashInputItem),
		typeof(RollingInputItem),
		typeof(GuardInputItem),
		typeof(SpecialParryInputItem),
		typeof(OverheatInputItem)
	};

	private const string DashAttackID = "dash_attack";

	private bool canAttack;

	private List<Enemy> enemies;

	protected bool CanPassWall { get; private set; }

	protected static SoundManager SoundManager => Game.Instance.SoundManager;

	protected PlayerDashState(PlayerStateMachine machine)
		: base(machine)
	{
	}

	protected override void OnEnter(IState prevState)
	{
		psm.SetStateOptionActive(StateOption.Dodge | StateOption.GravityFree, active: true);
		PlayerState.PlayerController.StatusEffectController.GetEffect<WetStatusEffect>()?.RemoveDuration();
		if (canAttack && enemies == null)
		{
			enemies = new List<Enemy>();
		}
		if (inputItems.Count != 0)
		{
			return;
		}
		foreach (Type nextInputItemType in NextInputItemTypes)
		{
			inputItems.Add(PlayerMover.PSM.GetInputItem(nextInputItemType));
		}
	}

	protected override void OnUpgrade(bool upgrade)
	{
		if (!upgrade)
		{
			CanPassWall = false;
			canAttack = false;
			return;
		}
		PlayerData playerData = Game.Instance.SaveData.PlayerData;
		if (!CanPassWall && playerData.CardDataList.Any((CardData data) => data.ID == 1007))
		{
			CanPassWall = true;
		}
		if (!canAttack && playerData.CardDataList.Any((CardData data) => data.ID == 1003))
		{
			canAttack = true;
		}
	}

	protected override void OnFixedUpdate(float fdt)
	{
		if (!canAttack)
		{
			return;
		}
		Vector3 position = PlayerMover.transform.position;
		Collider2D[] array = AttackUtility.OverlapBoxAll(size: PlayerMover.ColliderSize - Vector2.one * 0.25f, center: position, layer: ~(int)LayerUtility.PlayerLayer);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].TryGetComponent<Enemy>(out var component) && !enemies.Contains(component))
			{
				enemies.Add(component);
			}
		}
	}

	protected override void OnExit(IState nextState)
	{
		psm.SetStateOptionActive(StateOption.Dodge | StateOption.GravityFree, active: false);
		if (CanPassWall)
		{
			PlayerMover.gameObject.layer = LayerUtility.PlayerLayerIndex;
		}
		if (!canAttack)
		{
			return;
		}
		int duration = Game.DataManager.GetPlayerBurnData("dash_attack").Duration;
		foreach (Enemy enemy in enemies)
		{
			enemy.AddPlayerDashAttackEffect(duration);
		}
		enemies.Clear();
	}

	protected void ChangeNextState()
	{
		if (!psm.TryChangeSkillState(inputItems))
		{
			psm.ChangeState(PlayerMover.IsOnGround ? psm.IdleState : psm.FallState);
		}
	}

	protected async UniTask<bool> PassWallAsync(float speed, int movingFrame, CancellationToken token)
	{
		Vector2 direction = PlayerMover.FacingDirection.ToVector2();
		float num = speed * (float)movingFrame * Time.fixedDeltaTime;
		Vector2 endPosition = PlayerMover.VisualCenter + direction * num;
		float num2 = 0f;
		float num3 = 0f;
		GameObject hitObject;
		Vector2 collidePosition = PhysicsUtility.GetCollidePosition(direction, PlayerMover.VisualCenter, PlayerMover.ColliderSize, out hitObject, num);
		if (hitObject == null)
		{
			return false;
		}
		if (hitObject.TryGetComponent<Platform>(out var component))
		{
			Tilemap component2 = component.GetComponent<Tilemap>();
			Vector3Int vector3Int = new Vector3Int(Mathf.RoundToInt(collidePosition.x), Mathf.RoundToInt(collidePosition.y));
			Vector3Int vector3Int2 = vector3Int;
			bool flag = false;
			int sign = direction.x.GetSign();
			while (!(Mathf.Abs((float)vector3Int.x - PlayerMover.VisualCenter.x) > num))
			{
				if (component2.HasTile(vector3Int))
				{
					flag = true;
					break;
				}
				vector3Int.x += sign;
			}
			if (flag)
			{
				int num4 = 1;
				while (true)
				{
					vector3Int.x += sign;
					if (Mathf.Abs((float)vector3Int.x - PlayerMover.VisualCenter.x) > num)
					{
						return false;
					}
					if (num4 > 3)
					{
						return false;
					}
					if (!component2.HasTile(vector3Int))
					{
						break;
					}
					num4++;
				}
				num3 = num4;
				num2 = (float)vector3Int2.x + (float)num4 / 2f * (float)sign;
			}
		}
		else
		{
			if (hitObject.TryGetComponent<IImpassable>(out var _))
			{
				return false;
			}
			if (hitObject.TryGetComponent<Collider2D>(out var component4) && component4.bounds.size.x > 3f)
			{
				return false;
			}
			num3 = component4.bounds.size.x;
			num2 = component4.bounds.center.x;
		}
		Vector2 overlapCenter = new Vector2(num2 + (1f + num3) / 2f * direction.x, PlayerMover.transform.position.y);
		if ((bool)Physics2D.OverlapBox(overlapCenter, PlayerMover.ColliderSize * 0.7f, 0f, LayerUtility.PlatformLayer))
		{
			return false;
		}
		PlayerMover.gameObject.layer = LayerUtility.DefaultLayerIndex;
		bool moving = true;
		for (int i = 0; i < movingFrame; i++)
		{
			await PlayerUniTaskUtil.WaitOneFrame();
			if (token.IsCancellationRequested)
			{
				return false;
			}
			Vector2 visualCenter = PlayerMover.VisualCenter;
			if (PlayerMover.gameObject.layer != LayerUtility.PlayerLayerIndex && ((direction.x >= 0f && visualCenter.x > overlapCenter.x) || (direction.x < 0f && visualCenter.x < overlapCenter.x)))
			{
				PlayerMover.gameObject.layer = LayerUtility.PlayerLayerIndex;
			}
			if (moving && ((direction.x >= 0f && visualCenter.x > endPosition.x) || (direction.x < 0f && visualCenter.x < endPosition.x)))
			{
				moving = false;
				PlayerMover.ResetRelativeVelocity();
			}
		}
		return true;
	}
}
