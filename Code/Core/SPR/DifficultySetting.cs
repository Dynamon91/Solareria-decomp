using StaticData;
using UnityEngine;

namespace SPR;

public class DifficultySetting : MonoBehaviour
{
	[SerializeField]
	private Difficulty difficulty;

	public void SetDifficulty()
	{
		Game.Instance.Player.PlayerStat.ApplyDifficulty(difficulty);
		Game.Instance.Player.mover.ApplyDifficulty(difficulty);
		Game.Instance.SaveData.SetDifficulty(difficulty);
	}
}
