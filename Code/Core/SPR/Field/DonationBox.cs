using Cysharp.Threading.Tasks;
using Library;
using SPR.Save;
using UnityEngine;

namespace SPR.Field;

public class DonationBox : ServiceObject
{
	[Header("기부금 최대치")]
	[SerializeField]
	private int donationThreshold;

	public int DonationThreshold => donationThreshold;

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		SetAnimation("static");
	}

	public override void Interact()
	{
		if (Game.Instance.SaveData.WanderingData.TotalDonation < donationThreshold)
		{
			base.Interact();
		}
		else
		{
			Game.Instance.UIManager.FadeCanvas.WarningBoard.ShowWarning("UI_DonationBox_IsFull");
		}
	}

	public override void OnServiceOpening()
	{
		SetAnimation("active", loop: false);
	}

	public override void OnServiceClosing()
	{
		SetAnimation("static");
	}

	public void DropTycheSeal()
	{
		Vector2 startPos = (Vector2)base.transform.position + Vector2.right;
		Vector2 visualCenter = Game.Instance.Player.mover.VisualCenter;
		Vector2Int endPos = new Vector2(startPos.x + Bounds.size.x / 2f, visualCenter.y).ToVector2Int();
		ItemObject.DropItemAsync("SealTyche", startPos, endPos).Forget();
	}
}
