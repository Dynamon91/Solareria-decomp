using Library;
using UnityEngine;

namespace SPR;

public class PlayerFlipFollower : MonoBehaviour
{
	private int sign;

	private PlayerController PlayerController => Game.Instance.Player;

	private void Start()
	{
		sign = PlayerController.transform.localScale.x.GetSign();
	}

	private void Update()
	{
		int num = PlayerController.transform.localScale.x.GetSign();
		if (sign != num)
		{
			Vector3 localScale = base.transform.localScale;
			localScale.x *= -1f;
			base.transform.localScale = localScale;
			sign = num;
		}
	}
}
