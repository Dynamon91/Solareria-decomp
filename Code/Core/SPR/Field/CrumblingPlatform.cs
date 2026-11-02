using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using UnityEngine;

namespace SPR.Field;

public class CrumblingPlatform : CutsceneProducer, IGround
{
	[SerializeField]
	private int localBoxWidth;

	[Header("Cutscene")]
	[SerializeField]
	private GameObject fakeFloor;

	[SerializeField]
	private CinemachineImpulseSource impulseSource;

	private BoxCollider2D coll;

	private const int IntactState = 0;

	private const int DebrisState = 1;

	public override string ID => $"{base.ID}_{Game.Instance.CurrentFieldID}";

	private bool IsCrumbling => IsProducing;

	private Vector2 LocalBoxCenter => new Vector2(0f, Coll.offset.y + Coll.size.y / 2f);

	private Vector2 LocalBoxSize => new Vector2(localBoxWidth, 1f);

	private BoxCollider2D Coll => coll ?? (coll = GetComponent<BoxCollider2D>());

	public SoundManager.GroundMaterialType GroundMaterial => SoundManager.GroundMaterialType.Rock;

	protected override void SetProducerState(int state)
	{
		switch (state)
		{
		case 0:
			SetIntactState();
			break;
		case 1:
			SetDebrisState();
			break;
		}
	}

	protected override async UniTask OnStartTimelineAsync(int state, CancellationToken token)
	{
		if (state == 0)
		{
			Coll.enabled = false;
			impulseSource.GenerateImpulse();
			await UniTask.CompletedTask;
		}
	}

	protected override void OnEndTimeline(int state)
	{
	}

	protected override void OnSkipTimeline(int state)
	{
	}

	private void Update()
	{
		if (base.CurrentState == 0 && !IsCrumbling && PhysicsUtility.IsPlayerStay((Vector2)base.transform.position + LocalBoxCenter, LocalBoxSize))
		{
			PlayTimelineAsync(0, blockControl: false, hideCanvas: false).Forget();
		}
	}

	private void SetIntactState()
	{
		Coll.enabled = true;
		fakeFloor.SetActive(value: true);
	}

	private void SetDebrisState()
	{
		Coll.enabled = false;
		fakeFloor.SetActive(value: false);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(base.transform.position + (Vector3)LocalBoxCenter, LocalBoxSize);
		Vector3 position = base.transform.position;
		base.transform.position = position.SnapToHalf();
	}
}
