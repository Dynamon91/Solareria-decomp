using System;
using Library;

namespace SPR;

public class SalesTrigger : Trigger
{
	private readonly ActorName npcName;

	private readonly int aboveNum;

	public SalesTrigger(string target, string value)
	{
		if (!Enum.TryParse<ActorName>(target, out npcName))
		{
			DebugLog.GeneralLog(target + "을 ActorName으로 파싱할 수 없습니다!");
		}
		else if (!value.TryParseInt(out aboveNum))
		{
			DebugLog.GeneralLog(value + "를 정수로 파싱할 수 없습니다!");
		}
	}

	public override bool CheckTrigger()
	{
		if (Game.Instance.FieldController.FindNpc(npcName.ToString()) is StoreNPC storeNPC)
		{
			return storeNPC.PreviousSalesCount >= aboveNum;
		}
		DebugLog.GeneralLogError($"{npcName}은 상점 NPC가 아닙니다! 일단 false로 처리합니다.");
		return false;
	}
}
