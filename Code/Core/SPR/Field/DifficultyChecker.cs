using UnityEngine;

namespace SPR.Field;

public class DifficultyChecker : MonoBehaviour
{
	private void Start()
	{
		if (!GameControlParameter.Instance.ShouldShowAdditionalPlatform(Game.Instance.SaveData.Difficulty))
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
