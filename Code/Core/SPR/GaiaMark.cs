using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Library;
using SPR.Library;
using Spine.Unity;
using UnityEngine;

namespace SPR;

public class GaiaMark : MonoBehaviour, IAttackSource
{
	[SerializeField]
	private Transform markTransform;

	[SerializeField]
	private List<SkeletonAnimation> marks;

	[SerializeField]
	private List<ParticleSystem> bloomParticles;

	private Vector3 localPosition;

	private int attackFrame;

	private FrameTimer timer;

	public string Name => "GaiaMark";

	public Direction FacingDirection => Direction.None;

	public int Count => marks.Count((SkeletonAnimation m) => m.gameObject.activeSelf);

	public bool CanExecute => marks.All((SkeletonAnimation m) => m.gameObject.activeSelf);

	private static PlayerMover Player => Game.Instance.Player.mover;

	private void Awake()
	{
		foreach (SkeletonAnimation mark in marks)
		{
			mark.gameObject.SetActive(value: false);
		}
		localPosition = base.transform.localPosition;
	}

	private void Update()
	{
		base.transform.position = Game.Instance.Player.mover.transform.position + localPosition;
	}

	private void FixedUpdate()
	{
		if (timer != null)
		{
			if (timer.IsDone)
			{
				RemoveMark();
				timer.Reset();
			}
			timer.Tick();
		}
	}

	public void SetAttackFrame(int value)
	{
		attackFrame = value;
	}

	public void AddCount(int duration)
	{
		if (timer == null)
		{
			timer = new FrameTimer(duration);
		}
		timer?.Reset();
		for (int i = 0; i < marks.Count; i++)
		{
			SkeletonAnimation skeletonAnimation = marks[i];
			if (!skeletonAnimation.gameObject.activeSelf)
			{
				if (i == marks.Count - 1)
				{
					Bloom();
				}
				skeletonAnimation.gameObject.SetActive(value: true);
				if (i > 0)
				{
					float duration2 = marks[0].AnimationState.GetCurrent(0).Animation.Duration;
					float num = marks[0].AnimationState.GetCurrent(0).TrackTime / duration2 - (float)i / 3f;
					num = duration2 * ((num < 0f) ? (1f - num) : num);
					skeletonAnimation.SetAnimationInstantly("idle", loop: true, num, 1f, Time.deltaTime);
				}
				break;
			}
		}
	}

	public void RemoveMark()
	{
		timer?.Reset();
		for (int num = marks.Count - 1; num >= 0; num--)
		{
			if (marks[num].gameObject.activeSelf)
			{
				marks[num].gameObject.SetActive(value: false);
				break;
			}
		}
	}

	public async UniTaskVoid Execute()
	{
		Player.PSM.ChangeState(new PlayerStaggerHitState(Player.PSM)
		{
			Duration = Util.SecondsToFrame(Player.GetAnimDuration("gaia_mark")),
			NextState = Player.PSM.IdleState,
			KnockBackAllowed = false,
			Animation = "gaia_mark",
			AnimationLoop = false
		});
		await UniTask.WaitForSeconds(Util.FrameToSeconds(attackFrame));
		Player.TryTakeDamage(new HitInfo(this));
		marks.ForEach(delegate(SkeletonAnimation m)
		{
			m.gameObject.SetActive(value: false);
		});
		timer = null;
	}

	public void Clear()
	{
		timer = null;
		foreach (SkeletonAnimation mark in marks)
		{
			mark.gameObject.SetActive(value: false);
		}
	}

	private void Bloom()
	{
		foreach (ParticleSystem bloomParticle in bloomParticles)
		{
			bloomParticle.Play(withChildren: true);
		}
	}
}
