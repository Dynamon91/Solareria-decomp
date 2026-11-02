using SPR.Field;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class AMBController : MonoBehaviour
{
	[SerializeField]
	private bool isSmallOnBigOff;

	private Area area;

	private bool isPlayerStay;

	private Area ParentArea => area ?? (area = GetComponentInParent<Area>());

	private static SoundManager SoundManager => Game.Instance.SoundManager;

	private bool IsHorizontalChanger
	{
		get
		{
			Vector3 localScale = base.transform.localScale;
			return localScale.x <= localScale.y;
		}
	}

	protected virtual void FixedUpdate()
	{
		if (!Game.Instance.Player.isDead)
		{
			bool flag = PhysicsUtility.IsPlayerStay(base.transform.position, base.transform.localScale);
			if (!flag && isPlayerStay)
			{
				OnPlayerExit();
			}
			isPlayerStay = flag;
		}
	}

	private void OnPlayerExit()
	{
		Vector2 visualCenter = Game.Instance.Player.mover.VisualCenter;
		bool flag = false;
		if (IsHorizontalChanger)
		{
			if (base.transform.position.x < visualCenter.x)
			{
				flag = true;
			}
		}
		else if (base.transform.position.y < visualCenter.y)
		{
			flag = true;
		}
		if ((flag && !isSmallOnBigOff) || (!flag && isSmallOnBigOff))
		{
			SoundManager.PlayAMB(ParentArea.SoundKey);
		}
		else
		{
			SoundManager.StopAMB();
		}
	}

	private void OnDrawGizmos()
	{
		Vector3 position = base.transform.position;
		Vector3 localScale = base.transform.localScale;
		Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
		Gizmos.DrawCube(position, localScale);
	}
}
