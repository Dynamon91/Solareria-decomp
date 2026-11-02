using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR;

public class Myoso : Npc
{
	[Header("Myoso")]
	[SerializeField]
	private Transform baranDebrisPosition;

	private static bool IsBaranScene => Npc.CurrentFieldID.Equals(SectorName.SilentRuins, 9);

	public override void AwakeObject(SaveData saveData)
	{
		if (IsBaranScene)
		{
			Vector3 position = base.transform.position;
			base.transform.position = new Vector3(baranDebrisPosition.position.x, position.y, position.z);
			SetAnimation("scene/baran/idle_cry");
		}
		base.AwakeObject(saveData);
	}

	protected override void PlayTotAnimationOnPrepare(PlayerMover mover)
	{
		if (IsBaranScene)
		{
			mover.SetAnimation("see_myoso");
		}
		else
		{
			base.PlayTotAnimationOnPrepare(mover);
		}
	}

	public override async UniTask DisappearAsync()
	{
		string animationName = (IsBaranScene ? "scene/baran/normal_leave_hand" : "scene/elev/leave_2");
		SetAnimation(animationName, loop: false);
		await UniTaskExtension.WaitForSeconds(2f);
		base.gameObject.SetActive(value: false);
	}
}
