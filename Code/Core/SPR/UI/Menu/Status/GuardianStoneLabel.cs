using StaticData;
using UnityEngine;

namespace SPR.UI.Menu.Status;

public class GuardianStoneLabel : MonoBehaviour
{
	public void SetData()
	{
		base.gameObject.SetActive(Game.Instance.SaveData.Difficulty == Difficulty.Story);
	}
}
