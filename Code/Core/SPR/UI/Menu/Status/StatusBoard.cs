using System.Collections.Generic;
using SPR.Save;
using SPR.UI.Trait;
using UnityEngine;

namespace SPR.UI.Menu.Status;

public class StatusBoard : MonoBehaviour
{
	[Header("Player Info")]
	[SerializeField]
	private StatusLabel levelLabel;

	[SerializeField]
	private StatusLabel expLabel;

	[SerializeField]
	private StatusLabel pointLabel;

	[SerializeField]
	private StatusLabel moneyLabel;

	[SerializeField]
	private StatusLabel warmhornLabel;

	[SerializeField]
	private GuardianStoneLabel guardianStoneLabel;

	[Header("Stats")]
	[SerializeField]
	private List<TraitStatusDetailElement> statusDetailElements;

	public void SetData()
	{
		PlayerStat playerStat = Game.Instance.Player.PlayerStat;
		PlayerData playerData = Game.Instance.SaveData.PlayerData;
		levelLabel?.SetLevel(playerStat.Level);
		pointLabel?.SetFillData(playerData.AvailableTraitPoint, playerData.TotalTraitPoint);
		moneyLabel?.SetData(playerStat.Money);
		WarmhornCount stat = playerStat.GetStat<WarmhornCount>();
		warmhornLabel?.SetFillData(stat.Current, stat.MaxPotionCount);
		guardianStoneLabel?.SetData();
		statusDetailElements.ForEach(delegate(TraitStatusDetailElement element)
		{
			element.Initialize();
			element.ResetData();
		});
		if (playerStat.IsMaxLevel)
		{
			expLabel?.SetFillData(playerStat.GetAbsoluteExperience().Value, playerStat.GetAbsoluteExperience().Value);
			expLabel?.GetComponentInChildren<StatusState>().SetData(StatusState.State.Max);
		}
		else
		{
			expLabel?.SetFillData(playerStat.GetAbsoluteExperience().Value, playerStat.GetAbsoluteExperience().Max);
			expLabel?.GetComponentInChildren<StatusState>().SetData(StatusState.State.Normal);
		}
	}

	public void Focus(bool value)
	{
		base.gameObject.SetActive(value);
		if (value)
		{
			SetData();
		}
	}

	public void SetDropStatus()
	{
		expLabel.GetComponentInChildren<StatusState>().SetData(StatusState.State.Drop);
		moneyLabel.GetComponentInChildren<StatusState>().SetData(StatusState.State.Drop);
	}
}
