using System.Text;
using Library;
using UnityEngine;

namespace SPR.Field;

public class AreaChanger : MonoBehaviour
{
	[SerializeField]
	private Area bigArea;

	[SerializeField]
	private Area smallArea;

	private static FieldController FieldController => Game.Instance.FieldController;

	private bool IsHorizontalChanger
	{
		get
		{
			Vector3 localScale = base.transform.localScale;
			return localScale.x <= localScale.y;
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (Game.Instance.Player.isDead)
		{
			return;
		}
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
		FieldController.ChangeAreaTo(flag ? bigArea : smallArea);
	}

	private void OnDrawGizmos()
	{
		Vector3 position = base.transform.position;
		Vector3 localScale = base.transform.localScale;
		Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
		Gizmos.DrawCube(position, localScale);
	}

	private void OnDrawGizmosSelected()
	{
		Vector3 position = base.transform.position;
		base.transform.position = position.SnapToHalf();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Area Changer ");
		stringBuilder.Append(smallArea.IsNull() ? "NULL" : smallArea.name);
		stringBuilder.Append(" - ");
		stringBuilder.Append(bigArea.IsNull() ? "NULL" : bigArea.name);
		base.gameObject.name = stringBuilder.ToString();
	}
}
