using Cysharp.Threading.Tasks;
using SPR.Save;
using SPR.UI;
using UnityEngine;

namespace SPR.Field;

public class GiantElevator : InteractableObject, IService, IEntrance
{
	[Header("Service")]
	[SerializeField]
	private ServiceInfo serviceInfo;

	[SerializeField]
	private float interactLocalPosX;

	[Header("GiantElevator")]
	[SerializeField]
	private FieldID targetSceneID;

	[SerializeField]
	private Area connectArea;

	private SceneDataModel sceneDataModel;

	private FieldServiceCanvas serviceCanvas;

	public Area ConnectArea => connectArea;

	public bool IsVertical => false;

	private static PlayerMover PlayerMover => Game.Instance.Player.mover;

	public GameObject ServiceGameObject => base.gameObject;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		sceneDataModel = GetSceneDataModel();
		InstantiateServiceCanvasAsync().Forget();
	}

	public Vector2 GetPlayerSpawnPosition()
	{
		return (Vector2)base.transform.position + Vector2.up;
	}

	public Vector2 GetPlayerEnterEndPosition()
	{
		return (Vector2)base.transform.position + Vector2.up;
	}

	public async UniTask MoveSceneAsync(GameObject vcam)
	{
		vcam.SetActive(value: true);
		await UniTask.WaitForSeconds(0.5f);
		PlayerMover.SetAnimation("turn_back_2", loop: false);
		PlayerMover.AddAnimation("walk_turn_back");
		await UniTask.WaitForSeconds(1.5f);
		LoadField();
	}

	private SceneDataModel GetSceneDataModel()
	{
		string iD = InteractableObject.GetID("GiantElevator");
		return SceneDataModel.MoveModel(targetSceneID, iD);
	}

	private void LoadField()
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

	protected override void OnInteract()
	{
		InteractAsync().Forget();
	}

	private async UniTask InteractAsync()
	{
		await InteractableObject.InputManager.BlockControlForTask(MovePlayerOnInteractAsync(), "Move Player For Interaction");
		PlayerMover.SetAnimation("cam_up");
		serviceCanvas.PlaceOnRight(playerIsOnLeft: true);
		InteractableObject.InputManager.StackControlTarget(serviceCanvas).Forget();
	}

	private async UniTask MovePlayerOnInteractAsync()
	{
		await Game.Instance.Player.MoveXPositionAsync(base.transform.position.x + interactLocalPosX, isRunning: true);
		PlayerMover.Flip(Direction.Right);
		await UniTask.WaitUntil(() => PlayerMover.GetCurrentAnim(0).Name == "idle");
	}

	public void OnServiceOpening()
	{
	}

	public void OnServiceClosing()
	{
	}

	private async UniTask InstantiateServiceCanvasAsync()
	{
		serviceCanvas = await AddressableHelper.InstantiateAsync<FieldServiceCanvas>("Field Service Canvas.prefab", base.transform);
		serviceCanvas.Initialize(this, serviceInfo);
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Vector2 vector = base.transform.position;
		Vector2 vector2 = new Vector2(6f, 6.2f);
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(vector + serviceInfo.canvasPivot - new Vector2(0f, vector2.y / 2f), vector2);
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube(vector + new Vector2(interactLocalPosX, 2f), new Vector2(2f, 4f));
	}

	GameObject IEntrance.gameObject => base.gameObject;
}
