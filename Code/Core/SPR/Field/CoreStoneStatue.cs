using System;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using SPR.Save;
using SPR.UI;
using SPR.UI.Map;
using Spine.Unity.Playables;
using UnityEngine;
using UnityEngine.Playables;

namespace SPR.Field;

public class CoreStoneStatue : MonoBehaviour, IStateSavable, IInteractable, IMappable
{
	private enum State
	{
		None,
		Interacted
	}

	[Header("Core Stone")]
	[SerializeField]
	private string coreStone;

	[Header("Reference")]
	[SerializeField]
	private Vector2Int interactCanvasPosition;

	[SerializeField]
	private Transform playerPosition;

	[SerializeField]
	private CinemachineVirtualCamera startVcam;

	[SerializeField]
	private CinemachineVirtualCamera endVcam;

	[SerializeField]
	private Animator animator;

	private State currentState;

	private FieldInteractCanvas interactCanvas;

	private PlayableDirector playableDirector;

	public string ID => Util.GetID(Game.FieldID, "CoreStoneStatue");

	public string Name => ID;

	public bool IsNull => BasicExtension.IsNull(base.gameObject);

	public int InteractableOrder => 0;

	private PlayableDirector PlayableDirector => playableDirector ?? (playableDirector = GetComponent<PlayableDirector>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	public void AwakeObject(SaveData saveData)
	{
		playerPosition.gameObject.SetActive(value: false);
	}

	public void LoadState(int state)
	{
		currentState = (State)state;
		switch (currentState)
		{
		case State.None:
			FieldInteractCanvas.InstantiateCanvasAsync(base.transform, interactCanvasPosition, delegate(FieldInteractCanvas result)
			{
				interactCanvas = result;
			}).Forget();
			animator.Play("loop");
			break;
		case State.Interacted:
			animator.Play("off");
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public void ChangeState(int state)
	{
		currentState = (State)state;
		switch (currentState)
		{
		case State.Interacted:
		{
			if (Game.Instance.SaveData.WorldData.TryGetSectorData(out var data))
			{
				data.SaveObjectState(ID, state);
			}
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		case State.None:
			break;
		}
	}

	public void Interact()
	{
		if (currentState != State.Interacted)
		{
			interactCanvas.SetActive(value: false);
			Game.Instance.InputManager.BlockControlForTask(InteractAsync(), "Interact CoreStone Statue").Forget();
		}
	}

	public void InteractEnter()
	{
		if (currentState != State.Interacted)
		{
			interactCanvas.SetActive(value: true);
		}
	}

	public void InteractExit()
	{
		if (currentState != State.Interacted)
		{
			interactCanvas.SetActive(value: false);
		}
	}

	private async UniTask InteractAsync()
	{
		ChangeState(1);
		PlayerController player = Game.Instance.Player;
		await player.MoveXPositionAsync(playerPosition.position.x, isRunning: true);
		player.transform.SynchronizePositionDirection(playerPosition);
		CinemachineBrain cineBrain = GraphicHelper.MainCamera.GetComponent<CinemachineBrain>();
		startVcam.Priority = cineBrain.ActiveVirtualCamera.Priority + 1;
		startVcam.gameObject.SetActive(value: true);
		await UniTask.Yield();
		await UniTask.WaitWhile(() => cineBrain.IsBlending);
		startVcam.gameObject.SetActive(value: false);
		PlayableDirector.TrackBinding<CinemachineTrack>("Cinemachine Track", cineBrain);
		PlayableDirector.TrackBinding<SpineAnimationStateTrack>("Spine Animation State Track", player.mover.SkeletonAnimation);
		animator.SetBool("Active", value: true);
		PlayableDirector.Play();
		SoundManager.SetFadeBGMVolume(0.7f);
		await UniTask.WaitWhile(() => PlayableDirector.state == PlayState.Playing);
		endVcam.gameObject.SetActive(value: true);
		CinemachineBlendDefinition.Style originStyle = cineBrain.m_DefaultBlend.m_Style;
		cineBrain.m_DefaultBlend.m_Style = CinemachineBlendDefinition.Style.Cut;
		await UniTask.Yield();
		SoundManager.SetFadeBGMVolume(1f);
		cineBrain.m_DefaultBlend.m_Style = originStyle;
		endVcam.gameObject.SetActive(value: false);
		new PureItem(coreStone).AddToPlayerData();
	}

	private void OnDrawGizmosSelected()
	{
		Vector3 center = base.transform.position + interactCanvasPosition.ToVector3();
		Gizmos.color = Color.white;
		Gizmos.DrawWireCube(center, Vector3.one);
		Vector3 position = base.transform.position;
		position.x = (float)Mathf.CeilToInt(position.x) - 0.5f;
		position.y = (float)Mathf.CeilToInt(position.y) - 0.5f;
		base.transform.position = position;
	}

	public MapObjectInfo GetMapObjectInfo(Vector2 center)
	{
		float y = 10.5f;
		Vector2 position = base.transform.position.ToVector2() - new Vector2(0f, y) - center;
		return new MapObjectInfo(MapObjectType.CorestoneStatue, position, ID);
	}
}
