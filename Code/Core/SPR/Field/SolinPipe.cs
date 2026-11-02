using UnityEngine;

namespace SPR.Field;

public class SolinPipe : MonoBehaviour
{
	private enum Direction
	{
		Left,
		Right
	}

	[SerializeField]
	private Direction defaultDirection;

	private Animator anim;

	private const string LeftStop = "Left_Stop";

	private const string RightStop = "Right_Stop";

	private const string LeftMove = "Left";

	private const string RightMove = "Right";

	private Animator Animator
	{
		get
		{
			if ((object)anim != null)
			{
				return anim;
			}
			anim = GetComponent<Animator>();
			anim.enabled = true;
			return anim;
		}
	}

	public void SetDefault(bool isOpen)
	{
		string stateName = ((isOpen == (defaultDirection == Direction.Left)) ? "Left_Stop" : "Right_Stop");
		Animator.Play(stateName);
	}

	public void Move(bool isOpen)
	{
		string stateName = ((isOpen == (defaultDirection == Direction.Left)) ? "Left" : "Right");
		Animator.Play(stateName);
	}
}
