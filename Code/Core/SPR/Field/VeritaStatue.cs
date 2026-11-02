using Cysharp.Threading.Tasks;
using Library;
using UnityEngine;

namespace SPR.Field;

public class VeritaStatue : InteractableObject
{
	[Header("Reward")]
	[SerializeField]
	private string rewardItemName;

	[SerializeField]
	private Vector2Int dropStartLocalPos;

	[SerializeField]
	private Vector2Int dropEndLocalPos;

	private bool HasEverInteractedWithStatue => InteractableObject.WorldData.HasInteractedFieldObject(base.ObjectID);

	protected override void OnInteract()
	{
		if (!HasEverInteractedWithStatue)
		{
			DropRewardOnInteractEndAsync().Forget();
		}
		base.OnInteract();
	}

	private async UniTask DropRewardOnInteractEndAsync()
	{
		await UniTask.WaitUntil(() => InteractableObject.InputManager.IsCurrentControlTarget(InteractableObject.InteractionPanel));
		await UniTask.WaitUntil(() => !InteractableObject.InputManager.IsCurrentControlTarget(InteractableObject.InteractionPanel));
		Vector2 startPos = (Vector2)base.transform.position + (Vector2)dropStartLocalPos;
		Vector2Int endPos = ((Vector2)base.transform.position + (Vector2)dropEndLocalPos).ToVector2Int();
		await ItemObject.DropItemAsync(rewardItemName, startPos, endPos);
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Vector2 vector = (Vector2)base.transform.position + (Vector2)dropStartLocalPos;
		Vector2Int v = ((Vector2)base.transform.position + (Vector2)dropEndLocalPos).ToVector2Int();
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(vector, 1f);
		Gizmos.DrawWireSphere(v.ToVector3(), 1f);
	}
}
