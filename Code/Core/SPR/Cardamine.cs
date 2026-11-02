using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using SPR.Save;
using UnityEngine;

namespace SPR;

public class Cardamine : StoreNPC
{
	[Header("Cardamine")]
	[SerializeField]
	private Vector2Int letterLocalSpawnPos;

	[SerializeField]
	private Vector2Int diaryLocalSpawnPos;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		if (ShouldCardamineLeave())
		{
			Vector2Int spawnPos = base.transform.position.ToVector2Int() + letterLocalSpawnPos;
			TrySpawnUniqueWorldItemAsync("CardamineLetter", spawnPos).Forget();
			Vector2Int spawnPos2 = base.transform.position.ToVector2Int() + diaryLocalSpawnPos;
			TrySpawnUniqueWorldItemAsync("OldDiaryThird", spawnPos2).Forget();
		}
	}

	protected override void EnterIdleAnimation()
	{
		SetAnimation("idle_" + talkAnimationKey);
	}

	public override void OnServiceOpening()
	{
	}

	public override void OnServiceClosing()
	{
	}

	protected override void PlayTotAnimationOnPrepare(PlayerMover mover)
	{
		mover.SetAnimation("idle");
	}

	private bool ShouldCardamineLeave()
	{
		return Npc.CheckTriggerDisable(base.ID);
	}

	private async UniTask TrySpawnUniqueWorldItemAsync(string itemID, Vector2Int spawnPos)
	{
		if (!Npc.PlayerData.HasEverObtainedItem(itemID, out var _) && (!Npc.WorldData.TryGetSectorData(Npc.CurrentFieldID.SectorName, out var data) || !data.DropItemIDList.Any((string id) => Util.ParseID(id).id == itemID)))
		{
			await ItemObject.SpawnItemAsync(itemID, spawnPos);
		}
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireCube((base.transform.position.ToVector2Int() + letterLocalSpawnPos).ToVector3(), Vector3.one);
		Gizmos.DrawWireCube((base.transform.position.ToVector2Int() + diaryLocalSpawnPos).ToVector3(), Vector3.one);
	}
}
