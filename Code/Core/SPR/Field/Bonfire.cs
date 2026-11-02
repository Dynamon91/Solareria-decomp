using Cinemachine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Library;
using SPR.Save;
using SPR.UI;
using SPR.UI.Map;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Field;

[RequireComponent(typeof(PlayableDirector))]
public class Bonfire : MonoBehaviour, IInteractable, IMappable
{
	public enum VisualState
	{
		None,
		Deactivate,
		Activate,
		Respawn
	}

	[SerializeField]
	private Npc restNpc;

	[SerializeField]
	private ParticleSystem saveLoopParticle;

	[Header("Timeline")]
	[SerializeField]
	private PlayableAsset activateTimeline;

	[SerializeField]
	private PlayableAsset respawnTimeline;

	[Header("Vcam Confiners")]
	[SerializeField]
	private CinemachineConfiner2D respawnVcamConfiner;

	[SerializeField]
	private CinemachineConfiner2D activateVcamConfiner;

	[SerializeField]
	private CinemachineConfiner2D interactVcamConfiner;

	[Header("References")]
	[SerializeField]
	private SkeletonAnimation warmhornSpine;

	[SerializeField]
	private Transform warmhornActivePositionHolder;

	[SerializeField]
	private Animator furnaceAnimator;

	[SerializeField]
	private Transform chairGroupTransform;

	[SerializeField]
	private Transform chairGroupActivePositionHolder;

	[SerializeField]
	private SkeletonAnimation totSpine;

	[Header("Interact Canvas")]
	[SerializeField]
	private int interactCanvasHeightLow = 8;

	[SerializeField]
	private int interactCanvasHeightHigh = 11;

	private PlayableDirector director;

	private Area connectArea;

	private FieldInteractCanvas interactCanvas;

	private Material warmhornMaterial;

	private const float PlayerChairDistance = 6.5f;

	private const float NPCChairDistance = 5f;

	private static readonly int WarmhornShaderGlow = Shader.PropertyToID("_Glow");

	private PlayableDirector Director => director ?? (director = GetComponent<PlayableDirector>());

	private Material WarmhornMaterial => warmhornMaterial ?? (warmhornMaterial = warmhornSpine.GetComponent<MeshRenderer>().material);

	private static PlayerController Player => Game.Instance.Player;

	private static UIManager UIManager => Game.Instance.UIManager;

	private static InputManager InputManager => Game.Instance.InputManager;

	private static WorldData WorldData => Game.Instance.SaveData.WorldData;

	public float CenterX => base.transform.position.x;

	public Area ConnectArea => connectArea ?? (connectArea = GetComponentInParent<Area>());

	public Vector2 PlayerPositionOnRespawnStart => (Vector2)base.transform.position + Vector2.up * 4.5f;

	public string Name => base.gameObject.name;

	public bool IsNull => BasicExtension.IsNull(this);

	public int InteractableOrder => 0;

	private string NpcSitAnimation
	{
		get
		{
			if (restNpc is Chasa)
			{
				return "normal/sit";
			}
			return "idle";
		}
	}

	private string NpcAppearAnimation
	{
		get
		{
			if (restNpc is Chasa)
			{
				return "scene/teleport_sit";
			}
			return NpcSitAnimation;
		}
	}

	public void AwakeObject(SaveData saveData)
	{
		respawnVcamConfiner.m_BoundingShape2D = ConnectArea.ConfineCollider;
		respawnVcamConfiner.gameObject.SetActive(value: false);
		activateVcamConfiner.m_BoundingShape2D = ConnectArea.ConfineCollider;
		activateVcamConfiner.gameObject.SetActive(value: false);
		interactVcamConfiner.m_BoundingShape2D = ConnectArea.ConfineCollider;
		interactVcamConfiner.gameObject.SetActive(value: false);
		FieldInteractCanvas.InstantiateCanvasAsync(base.transform, new Vector2(0f, interactCanvasHeightHigh), delegate(FieldInteractCanvas canvas)
		{
			interactCanvas = canvas;
		}).Forget();
	}

	public void LoadDefaultState()
	{
		SetVcamActive(0);
		if (WorldData.HasInteractedBonfire(Game.Instance.CurrentFieldID))
		{
			SetVisualActive(VisualState.Activate);
		}
		else
		{
			SetVisualActive(VisualState.Deactivate);
		}
	}

	public void SetVcamActive(int value)
	{
		switch (value)
		{
		case 1:
			respawnVcamConfiner.gameObject.SetActive(value: true);
			activateVcamConfiner.gameObject.SetActive(value: false);
			interactVcamConfiner.gameObject.SetActive(value: false);
			break;
		case 2:
			respawnVcamConfiner.gameObject.SetActive(value: false);
			activateVcamConfiner.gameObject.SetActive(value: true);
			interactVcamConfiner.gameObject.SetActive(value: false);
			break;
		case 3:
			respawnVcamConfiner.gameObject.SetActive(value: false);
			activateVcamConfiner.gameObject.SetActive(value: false);
			interactVcamConfiner.gameObject.SetActive(value: true);
			break;
		default:
			respawnVcamConfiner.gameObject.SetActive(value: false);
			activateVcamConfiner.gameObject.SetActive(value: false);
			interactVcamConfiner.gameObject.SetActive(value: false);
			break;
		}
	}

	public void PlayRespawnTimeline()
	{
		Director.playableAsset = respawnTimeline;
		Director.Play();
	}

	public void Interact()
	{
		FieldID currentFieldID = Game.Instance.CurrentFieldID;
		DebugLog.GeneralLog(currentFieldID.ToString() + "에서 모닥불과 상호작용합니다.");
		interactCanvas.SetActive(value: false);
		if (WorldData.HasInteractedBonfire(currentFieldID))
		{
			InteractAsync().Forget();
		}
		else
		{
			ActivateBonfireAsync().Forget();
		}
	}

	private async UniTask ActivateBonfireAsync()
	{
		NullControllable nullControllable = new NullControllable("Bonfire First Interaction");
		await InputManager.StackControlTarget(nullControllable);
		await Player.MoveXPositionAsync(CenterX, isRunning: false);
		Player.mover.Flip(1f);
		await UniTask.DelayFrame(20);
		SetVcamActive(2);
		WarmhornMaterial.DOKill();
		totSpine.transform.parent.position = Player.transform.position;
		Director.playableAsset = activateTimeline;
		Director.Play();
		Player.gameObject.SetActive(value: false);
		await UniTask.WaitUntil(() => Director.state != PlayState.Playing || Director.time >= Director.duration - 0.10000000149011612);
		Director.Stop();
		Player.mover.SynchronizeAnimation(totSpine);
		Player.gameObject.SetActive(value: true);
		Player.mover.PSM.ChangeState(Player.mover.PSM.IdleState);
		SetVcamActive(0);
		SetVisualActive(VisualState.Activate);
		await InputManager.RemoveControlTarget(nullControllable);
		Game.Instance.SaveBonfire();
	}

	private async UniTask InteractAsync()
	{
		NullControllable nullControllable = new NullControllable("Bonfire Save");
		await InputManager.StackControlTarget(nullControllable);
		await MovePlayerToChairPosition();
		Player.mover.PSM.ChangeState(Player.mover.PSM.SaveState);
		interactVcamConfiner.gameObject.SetActive(value: true);
		SetVisualActive(VisualState.Activate);
		await UIManager.FadeCanvas.FadeOut(1f);
		await InputManager.RemoveControlTarget(nullControllable);
		WorldData.AddRestedBonfireFieldID(Game.Instance.CurrentFieldID, Player.transform.position);
		Game.Instance.SaveField();
		await Game.Instance.LoadFieldAsync(SceneDataModel.SaveModel(Game.Instance.CurrentFieldID));
		if (Game.Instance.FieldController.TryFindBonfire(out var bonfire))
		{
			bool isFirst = (object)restNpc != null && WorldData.TryAddFirstRestNpc(restNpc.NpcName, Game.Instance.CurrentFieldID);
			bonfire.AppearNpc(isFirst).Forget();
		}
	}

	public void InteractEnter()
	{
		interactCanvas.SetActive(value: true);
	}

	public void InteractExit()
	{
		interactCanvas.SetActive(value: false);
	}

	public void SetVisualActive(VisualState state)
	{
		RelocateNpc();
		if (!BasicExtension.IsNull(warmhornSpine))
		{
			WarmhornMaterial.DOKill();
			switch (state)
			{
			case VisualState.Activate:
				saveLoopParticle.gameObject.SetActive(value: true);
				saveLoopParticle.Play();
				furnaceAnimator.Play("Enabled");
				warmhornSpine.SetOrSyncAnimation(warmhornSpine, "idle_loop");
				warmhornSpine.transform.position = warmhornActivePositionHolder.position;
				chairGroupTransform.position = chairGroupActivePositionHolder.position;
				interactCanvas?.ChangeHeight(interactCanvasHeightHigh);
				break;
			case VisualState.Respawn:
				saveLoopParticle.gameObject.SetActive(value: true);
				saveLoopParticle.Stop();
				furnaceAnimator.Play("Enabled");
				warmhornSpine.SetOrSyncAnimation(warmhornSpine, "idle_loop");
				warmhornSpine.transform.position = warmhornActivePositionHolder.position;
				chairGroupTransform.position = chairGroupActivePositionHolder.position;
				interactCanvas?.ChangeHeight(interactCanvasHeightHigh);
				break;
			default:
				furnaceAnimator.Play("Disabled");
				WarmhornMaterial.SetFloat(WarmhornShaderGlow, 5f);
				WarmhornMaterial.DOShaderFloatProperty(WarmhornShaderGlow, 3f, 0.5f).SetLoops(-1, LoopType.Yoyo);
				saveLoopParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
				interactCanvas?.ChangeHeight(interactCanvasHeightLow);
				break;
			}
		}
	}

	private float GetSaveInteractPosX(Vector2 playerPosition)
	{
		bool flag = playerPosition.x > CenterX;
		if (!BasicExtension.IsNull(restNpc) && restNpc.gameObject.activeInHierarchy)
		{
			flag = restNpc.transform.position.x <= CenterX;
		}
		return CenterX + (float)(flag ? 1 : (-1)) * 6.5f;
	}

	private void OnDrawGizmosSelected()
	{
		Vector3 position = base.transform.position;
		float y = (float)(position.y - 0.5f).RoundToInt() + 0.5f;
		Vector2 vector = new Vector2(position.x.RoundToInt(), y);
		base.transform.position = vector;
		Gizmos.color = Color.gray;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * interactCanvasHeightHigh, Vector3.one);
		Gizmos.color = Color.black;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * interactCanvasHeightLow, Vector3.one);
	}

	private async UniTask MovePlayerToChairPosition()
	{
		float saveInteractPosX = GetSaveInteractPosX(Player.mover.VisualCenter);
		await Player.MoveXPositionAsync(saveInteractPosX, isRunning: false);
		int sign = (CenterX - Player.mover.VisualCenter.x).GetSign();
		Player.mover.Flip(sign);
		await UniTask.DelayFrame(20);
	}

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		if (Game.FieldID.Equals(SectorName.SilentRuins, 0))
		{
			return null;
		}
		Vector2 position = base.transform.position.ToVector2() - center;
		return new MapObjectInfo(MapObjectType.Altar, position, 0);
	}

	private async UniTask AppearNpc(bool isFirst)
	{
		DebugLog.GeneralLog($"Appear Npc : isFirst = {isFirst}");
		if ((object)restNpc == null)
		{
			return;
		}
		if (!isFirst)
		{
			RelocateNpc();
			return;
		}
		Vector2 position = new Vector2(CenterX + (float)((CenterX > Player.transform.position.x) ? 1 : (-1)) * 5f, restNpc.transform.position.y);
		restNpc.gameObject.SetActive(value: false);
		restNpc.Position = position;
		restNpc.Flip(Player.transform.position.x - restNpc.Position.x);
		restNpc.AwakeObject(Game.Instance.SaveData);
		if (restNpc is Chasa)
		{
			await UniTask.WaitForSeconds(1f);
			restNpc.gameObject.SetActive(value: true);
			restNpc.EnableVFX(NpcAppearAnimation);
		}
		restNpc.gameObject.SetActive(value: true);
	}

	private void RelocateNpc()
	{
		if ((object)restNpc != null && WorldData.HasRestedBonfire(Game.Instance.CurrentFieldID))
		{
			Vector2 restedPosition = WorldData.GetRestedPosition(Game.Instance.CurrentFieldID);
			Vector2 position = new Vector2(CenterX + (float)((CenterX > restedPosition.x) ? 1 : (-1)) * 5f, restNpc.transform.position.y);
			restNpc.SynchronizeSelf(NpcSitAnimation);
			restNpc.Position = position;
			restNpc.Flip(restedPosition.x - restNpc.Position.x);
		}
	}

	public void SetCameraShakeActive(bool value)
	{
		activateVcamConfiner.GetComponent<CinemachineVirtualCamera>().GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = (value ? 2f : 0f);
	}
}
