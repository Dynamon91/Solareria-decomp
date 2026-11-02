using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using SPR.Library;
using SPR.UI.Map;
using SPR.UI.Menu;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SPR;

[RequireComponent(typeof(BoxCollider2D))]
public class Entrance : MonoBehaviour, IEntrance
{
	[SerializeField]
	protected FieldID targetSceneID;

	[SerializeField]
	private Area connectArea;

	[SerializeField]
	private bool connectMap = true;

	[SerializeField]
	private Vector2 localInsidePosition;

	[Header("Light Settings")]
	[Tooltip("True라면 기본적으로 붙어있는 반응형 라이트가 동작하지 않습니다.")]
	[SerializeField]
	private bool alwaysDisableLight;

	private BoxCollider2D boxCollider;

	private EntranceLightDisabler lightDisabler;

	private const float DistanceMin = 15f;

	private const float DistanceMax = 30f;

	private const float VerticalMaxOuterRadius = 20f;

	private bool isInitialized;

	protected Direction EntranceDirection;

	private Vector2 outsidePosition;

	private Vector2 insidePosition;

	private float downDelta;

	private bool isNoGroundUnder;

	private SceneDataModel sceneDataModel;

	private Light2D light2D;

	private float lightVelocity;

	private float maxIntensity;

	public Area ConnectArea => connectArea;

	public virtual bool IsVertical => BoxCollider.size.x < BoxCollider.size.y;

	protected BoxCollider2D BoxCollider => boxCollider ?? (boxCollider = GetComponent<BoxCollider2D>());

	protected virtual FieldID TargetSceneID => targetSceneID;

	private static InputManager InputManager => Game.Instance.InputManager;

	protected virtual bool CanConnectMap => GetType().IsAssignableFrom(typeof(Entrance));

	public FieldConnectionInformation GetConnectionInfo()
	{
		Area mainArea = GetComponentInParent<FieldController>().MainArea;
		return new FieldConnectionInformation
		{
			connect = (connectMap && CanConnectMap),
			targetField = targetSceneID,
			direction = GetEntranceDirection(),
			entrancePosition = (IsVertical ? ((base.transform.position.y - BoxCollider.size.y / 2f).RoundToInt() - mainArea.AreaBox.LowerLeft.y) : ((base.transform.position.x - BoxCollider.size.x / 2f).RoundToInt() - mainArea.AreaBox.LowerLeft.x)),
			entranceRawPosition = GetEntranceWorldPosition() - mainArea.AreaBox.Center
		};
	}

	protected virtual Vector2 GetEntranceWorldPosition()
	{
		return (Vector2)base.transform.position - BoxCollider.size / 2f;
	}

	protected virtual void Start()
	{
		Initialize();
		CreateEntranceLight().Forget();
		lightDisabler = GetComponent<EntranceLightDisabler>();
	}

	private void Update()
	{
		if (light2D.IsNull())
		{
			return;
		}
		if (!alwaysDisableLight)
		{
			EntranceLightDisabler entranceLightDisabler = lightDisabler;
			if ((object)entranceLightDisabler == null || !entranceLightDisabler.ShouldDisable())
			{
				AdjustLightIntensity();
				return;
			}
		}
		light2D.enabled = false;
		light2D.intensity = 0f;
	}

	protected void Initialize()
	{
		if (!isInitialized)
		{
			EntranceDirection = GetEntranceDirection();
			outsidePosition = CalculateOutsidePosition();
			insidePosition = CalculateInsidePosition();
			sceneDataModel = GetSceneDataModel();
			isInitialized = true;
		}
	}

	protected virtual SceneDataModel GetSceneDataModel()
	{
		return SceneDataModel.MoveModel(TargetSceneID, base.name);
	}

	protected virtual void FixedUpdate()
	{
		if ((InputManager.ControlTarget == null || InputManager.IsCurrentControlTarget(Game.Instance.Player) || InputManager.IsCurrentControlTarget(Game.Instance.DevController) || InputManager.ControlTarget is MenuPanel) && PhysicsUtility.IsPlayerStay((Vector2)base.transform.position + BoxCollider.offset, BoxCollider.size))
		{
			LoadField();
		}
	}

	protected void LoadField()
	{
		if (base.name.Contains("Ending"))
		{
			Game.Instance.LoadEndingAsync().Forget();
		}
		else
		{
			Game.Instance.LoadFieldAsync(sceneDataModel).Forget();
		}
	}

	public virtual async UniTask MovePlayerOnEnterAsync()
	{
		Initialize();
		if (EntranceDirection == Direction.Up && localInsidePosition != Vector2.zero)
		{
			PlayerMover playerMover = Game.Instance.Player.mover;
			playerMover.PSM.ChangeState(new PlayerEntranceJumpState(playerMover.PSM));
			Vector2 start = outsidePosition;
			Vector2 target = insidePosition;
			float gravity = Mathf.Abs(Physics2D.gravity.y * playerMover.Gravity);
			Vector2 velocity = CalculateParabolaVelocity(start, target, target.y + 3f, gravity);
			playerMover.ApplyRelativeVelocity(velocity, faceJump: true);
			await UniTask.WaitUntil(() => playerMover.IsOnGround);
		}
		else
		{
			await Game.Instance.Player.MovePositionAsync(GetPlayerEnterEndPosition(), isRunning: true, silentY: false);
			if (EntranceDirection == Direction.Up && !isNoGroundUnder)
			{
				await UniTask.WaitUntil(() => Game.Instance.Player.mover.IsOnGround);
			}
		}
		Direction entranceDirection = EntranceDirection;
		if (entranceDirection == Direction.Up || entranceDirection == Direction.Down)
		{
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(10);
		}
	}

	public virtual Vector2 GetPlayerSpawnPosition()
	{
		Initialize();
		return outsidePosition;
	}

	public virtual Vector2 GetPlayerEnterEndPosition()
	{
		Initialize();
		return insidePosition;
	}

	private Vector2 CalculateOutsidePosition()
	{
		Vector2 result = base.transform.position;
		switch (EntranceDirection)
		{
		case Direction.Right:
			result += new Vector2(-2f, 1f - BoxCollider.size.y / 2f);
			break;
		case Direction.Left:
			result += new Vector2(2f, 1f - BoxCollider.size.y / 2f);
			break;
		case Direction.Up:
			result += new Vector2(0f, -4f);
			break;
		case Direction.Down:
			result += new Vector2(0f, 2f);
			break;
		}
		return result;
	}

	private Vector2 CalculateInsidePosition()
	{
		Vector2 result = base.transform.position;
		switch (EntranceDirection)
		{
		case Direction.Right:
		{
			float x = ((localInsidePosition.x != 0f) ? localInsidePosition.x : 2f);
			result += new Vector2(x, 1f - BoxCollider.size.y / 2f);
			break;
		}
		case Direction.Left:
		{
			float x = ((localInsidePosition.x != 0f) ? localInsidePosition.x : (-2f));
			result += new Vector2(x, 1f - BoxCollider.size.y / 2f);
			break;
		}
		case Direction.Up:
			if (localInsidePosition != Vector2.zero)
			{
				result += localInsidePosition;
			}
			else
			{
				result += new Vector2(0f, GetUpwardGroundDistance());
			}
			break;
		case Direction.Down:
		{
			float groundDistance = GetGroundDistance(Direction.Down);
			downDelta = Mathf.Clamp(groundDistance - 8.5f, 0f, 3f);
			base.transform.position -= new Vector3(0f, downDelta);
			result -= new Vector2(0f, 2.5f + downDelta);
			break;
		}
		}
		return result;
	}

	protected virtual Direction GetEntranceDirection()
	{
		Direction[] array = ((!IsVertical) ? new Direction[2]
		{
			Direction.Up,
			Direction.Down
		} : new Direction[2]
		{
			Direction.Right,
			Direction.Left
		});
		foreach (Direction direction in array)
		{
			Vector2 pos = (Vector2)base.transform.position + direction.ToVector2() * 2f;
			if (connectArea.IsInArea(pos))
			{
				return direction;
			}
		}
		DebugLog.GeneralLogError(base.name + "의 출구 방향을 찾을 수 없습니다.");
		return Direction.None;
	}

	private float GetGroundDistance(Direction direction)
	{
		if (direction != Direction.Up && direction != Direction.Down)
		{
			return 0f;
		}
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, direction.ToVector2(), 2.1474836E+09f, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer);
		if ((bool)raycastHit2D)
		{
			return raycastHit2D.distance.SnapToHalf();
		}
		DebugLog.GeneralLogWarning($"{base.name}의 출입구에서 {direction} 방향으로 땅을 찾을 수 없습니다.");
		isNoGroundUnder = direction == Direction.Down;
		return 0f;
	}

	private float GetUpwardGroundDistance()
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, Direction.Up.ToVector2(), 2.1474836E+09f, LayerUtility.OneWayPlatformLayer);
		if ((bool)raycastHit2D)
		{
			return raycastHit2D.distance.SnapToHalf() + 3f;
		}
		return 5f;
	}

	private async UniTaskVoid CreateEntranceLight()
	{
		string address = Address.EntranceLight(Game.Instance.CurrentFieldID.SectorName);
		if ((object)light2D == null)
		{
			light2D = await AddressableHelper.InstantiateAsync<Light2D>(address, base.transform);
		}
		Vector2 normalized = (ConnectArea.AreaBox.Center - (Vector2)base.transform.position).normalized;
		maxIntensity = light2D.intensity;
		if (IsVertical)
		{
			int sign = normalized.x.GetSign();
			light2D.transform.localPosition = new Vector3(-sign * 2, (0f - BoxCollider.size.y) / 2f);
			light2D.transform.rotation = Quaternion.Euler(0f, 0f, (float)(-sign) * 60f);
			light2D.pointLightOuterRadius = Mathf.Min(BoxCollider.size.y + 22f, 20f);
			light2D.pointLightOuterAngle = 120f;
		}
		else
		{
			int sign2 = normalized.y.GetSign();
			float num = ((sign2 > 0) ? (BoxCollider.size.x / 4f) : (BoxCollider.size.x / 4f + downDelta));
			light2D.transform.localPosition = new Vector3(0f, (float)(-sign2) * num);
			light2D.transform.rotation = Quaternion.Euler(0f, 0f, (sign2 > 0) ? 0f : 180f);
			light2D.pointLightOuterRadius = BoxCollider.size.x + 22f;
			light2D.pointLightOuterAngle = 180f;
		}
		light2D.pointLightInnerAngle = 120f;
		light2D.intensity = 0f;
	}

	private void AdjustLightIntensity()
	{
		if ((object)light2D != null)
		{
			PlayerMover mover = Game.Instance.Player.mover;
			float num = Mathf.Clamp(Vector2.Distance((Vector2)base.transform.position + Vector2.down * BoxCollider.size.y / 2f, mover.VisualBottom), 15f, 30f);
			float target = maxIntensity * 2f - maxIntensity / 15f * num;
			float num2 = Mathf.SmoothDamp(light2D.intensity, target, ref lightVelocity, 0.3f);
			if (num2 > 0.01f)
			{
				light2D.enabled = true;
				light2D.intensity = num2;
			}
			else
			{
				light2D.enabled = false;
				light2D.intensity = 0f;
			}
		}
	}

	private Vector2 CalculateParabolaVelocity(Vector2 start, Vector2 target, float maxHeight, float gravity)
	{
		float num = target.x - start.x;
		float num2 = Mathf.Sqrt(2f * gravity * (maxHeight - start.y));
		float num3 = num2 / gravity;
		float num4 = Mathf.Sqrt(2f * (maxHeight - target.y) / gravity);
		float num5 = num3 + num4;
		return new Vector2(num / num5, num2);
	}

	protected virtual void OnDrawGizmosSelected()
	{
		if (localInsidePosition != Vector2.zero)
		{
			Gizmos.DrawSphere(base.transform.position + (Vector3)localInsidePosition, 0.5f);
		}
		Vector3 position = base.transform.position;
		base.transform.position = position.SnapToHalf();
	}

	private void OnValidate()
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast((Vector2)base.transform.position + localInsidePosition, Vector3.down, 2.1474836E+09f, (int)LayerUtility.PlatformLayer | (int)LayerUtility.OneWayPlatformLayer);
		if ((bool)raycastHit2D)
		{
			Vector2 vector = raycastHit2D.point.SnapToHalf();
			Vector2 vector2 = localInsidePosition;
			vector2.y = vector.y - base.transform.position.y;
			localInsidePosition = vector2;
		}
	}

	GameObject IEntrance.gameObject => base.gameObject;
}
