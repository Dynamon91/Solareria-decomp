using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Field;
using SPR.Save;
using UnityEngine;

namespace SPR;

public class Arte : Npc
{
	[SerializeField]
	private Vector2Int letterLocalSpawnPos;

	[SerializeField]
	private GameObject objectAfter;

	[SerializeField]
	private GameObject environmentBefore;

	[SerializeField]
	private GameObject environmentAfter;

	private Collider2D coll;

	private Collider2D Coll => coll ?? (coll = GetComponent<Collider2D>());

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		SetAnimation(base.SkeletonAnimation.AnimationName);
		if (saveData.TalkData.GetInteractCount(ActorName.Arte) > 0)
		{
			Coll.enabled = false;
		}
		bool activeSelf = base.gameObject.activeSelf;
		environmentBefore.SetActive(activeSelf);
		environmentAfter.SetActive(!activeSelf);
		objectAfter.SetActive(!activeSelf);
		if (!activeSelf)
		{
			Vector2Int spawnPos = base.transform.position.ToVector2Int() + letterLocalSpawnPos;
			TrySpawnUniqueWorldItemAsync("MuhaLetter", spawnPos).Forget();
		}
	}

	protected override void EnterTalkAnimation()
	{
	}

	protected override void EnterIdleAnimation()
	{
	}

	protected override void OnFinishTalk()
	{
		base.OnFinishTalk();
		Coll.enabled = false;
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
	}
}
