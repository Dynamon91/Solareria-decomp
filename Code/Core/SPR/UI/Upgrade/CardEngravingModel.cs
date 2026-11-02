using SPR.Save;

namespace SPR.UI.Upgrade;

public class CardEngravingModel : UpgradeModel
{
	public int CardID { get; private set; }

	public bool HasCard
	{
		get
		{
			CardData cardData;
			return PlayerData.TryGetCardData(CardID, out cardData);
		}
	}

	public CardData CardData
	{
		get
		{
			if (!PlayerData.TryGetCardData(CardID, out var cardData))
			{
				return null;
			}
			return cardData;
		}
	}

	private static PlayerData PlayerData => Game.Instance.SaveData.PlayerData;

	public CardEngravingModel(int cardID)
	{
		CardID = cardID;
	}
}
