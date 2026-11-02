using UnityEngine;

namespace SPR;

public class ImahenEntrance : Entrance
{
	[Header("Imahen Entrance")]
	[SerializeField]
	private Vector2 positionAfterCutscene;

	protected override bool CanConnectMap => true;

	private static bool IsAfterCutscene => Game.Instance.SaveData.WanderingData.IsWatchedCutscene("ImahenCutsceneProducer", 6);

	public override Vector2 GetPlayerSpawnPosition()
	{
		Vector2 playerSpawnPosition = base.GetPlayerSpawnPosition();
		if (!IsAfterCutscene)
		{
			return playerSpawnPosition;
		}
		return positionAfterCutscene;
	}

	public override Vector2 GetPlayerEnterEndPosition()
	{
		Vector2 playerEnterEndPosition = base.GetPlayerEnterEndPosition();
		if (!IsAfterCutscene)
		{
			return playerEnterEndPosition;
		}
		return positionAfterCutscene + Vector2.right;
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.DrawWireSphere(positionAfterCutscene, 0.5f);
	}
}
