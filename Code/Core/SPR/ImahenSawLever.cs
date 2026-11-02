using Cysharp.Threading.Tasks;
using SPR.Field;
using UnityEngine;

namespace SPR;

public class ImahenSawLever : InteractableLever
{
	[SerializeField]
	private ImahenChaser imahenChaser;

	private BoxCollider2D box2D;

	public override string ID => $"{Game.Instance.CurrentFieldID}_Lever_{base.transform.position.ToString()}";

	public BoxCollider2D Box2D => box2D ?? (box2D = GetComponent<BoxCollider2D>());

	protected override async UniTask OnActivateAsync()
	{
		imahenChaser.ReturnPosition();
		await ReturnHandleAsync();
	}
}
