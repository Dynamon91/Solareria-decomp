using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.ObjectPool;
using UnityEngine;

namespace SPR;

public class Verita : BossCharacter
{
	private enum Pattern
	{
		None,
		DownKick,
		RoundKick,
		SideKick,
		Arrow,
		Flame,
		SweepTumble,
		Rush,
		SpaceControl
	}

	[Header("디버깅")]
	[SerializeField]
	private Pattern pattern;

	[SerializeField]
	private bool debugBlackBackground;

	[Header("패턴")]
	[SerializeField]
	private GenericBossAttackSequence<Verita> downKickAttack;

	[SerializeField]
	private GenericBossAttackSequence<Verita> roundKickAttack;

	[SerializeField]
	private GenericBossAttackSequence<Verita> sideKickAttack;

	[SerializeField]
	private GenericBossAttackSequence<Verita> arrowAttack;

	[SerializeField]
	private GenericBossAttackSequence<Verita> flameAttack;

	[SerializeField]
	private GenericBossAttackSequence<Verita> sweepTumbleAttack;

	[SerializeField]
	private GenericBossAttackSequence<Verita> rushAttack;

	[SerializeField]
	private GenericBossAttackSequence<Verita> spaceControlAttack;

	[Header("화륜각 - 화살")]
	[SerializeField]
	private VeritaVerticalArrow.VeritaArrowParameter downArrowParameter;

	[SerializeField]
	private float downArrowGap = 2.2f;

	[Header("열파 - 화살")]
	[SerializeField]
	private VeritaHorizontalArrow.VeritaArrowParameter arrowParameter;

	[SerializeField]
	private List<float> arrowLocalHeights;

	[Header("염화지주")]
	[SerializeField]
	private Vector2 flameSize;

	[Header("공간 장악")]
	[SerializeField]
	private AreaBox spaceFlameBulletAreaBox;

	[SerializeField]
	private float spaceLocalAppearHeight;

	[Header("SFX")]
	[SerializeField]
	private EventReference arrowEvent;

	[SerializeField]
	private EventReference flameEvent;

	[Header("Reference")]
	[SerializeField]
	private GameObject flameFieldEffectGroup;

	private VeritaSpaceController spaceController;

	private bool isBlackOnStart;

	private bool isBlack;

	public const int NpcStateIndex = 0;

	public const int OpeningStateIndex = 1;

	public const int RetryInFirstPhaseStateIndex = 2;

	public const int SecondPhaseStateIndex = 3;

	public const int RetryInSecondPhaseStateIndex = 4;

	public const int ThirdPhaseStateIndex = 5;

	public const int RetryInThirdPhaseStateIndex = 6;

	public const int DefeatStateIndex = 7;

	public const int TruthStateIndex = 8;

	public const int AfterFightStateIndex = 9;

	private readonly List<IPoolableObject> poolableObjects = new List<IPoolableObject>();

	private int arrowDirection;

	private readonly List<IDamageable> arrowDamagedList = new List<IDamageable>();

	private readonly List<IDamageable> flameDamagedList = new List<IDamageable>();

	private VeritaSpaceController SpaceController => spaceController ?? (spaceController = base.transform.parent.GetComponentInChildren<VeritaSpaceController>(includeInactive: true));

	public override string Name => "Verita";

	private CutsceneState PhaseTwoTransitionState { get; set; }

	private CutsceneState PhaseThreeTransitionState { get; set; }

	private CutsceneState DefeatState { get; set; }

	public IReadOnlyList<GenericBossAttackSequence<Verita>> BackgroundAttackSequences => new List<GenericBossAttackSequence<Verita>>(3) { downKickAttack, flameAttack, spaceControlAttack };

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { downKickAttack, roundKickAttack, sideKickAttack, arrowAttack, flameAttack, sweepTumbleAttack, rushAttack, spaceControlAttack };
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		PhaseTwoTransitionState = new CutsceneState(this, 3, blockControl: false, hideCanvas: false);
		PhaseThreeTransitionState = new CutsceneState(this, 5, blockControl: true);
		DefeatState = new CutsceneState(this, 7);
		base.StateMachine.ChangeState(base.IdleState);
		BeginParamActions.Add("SpawnFlameField", delegate(int _, Parameter[] param)
		{
			SpawnFlameField(param[0].Int, param[1].Int);
		});
		BeginParamActions.Add("AdditionalBlackWave", delegate
		{
			ActivateAdditionalExplosionWave();
		});
		TimelineBeginActions.Add("FlipToArrow", delegate
		{
			FlipToArrow();
		});
		TimelineBeginActions.Add("ActivateArrow", delegate
		{
			ActivateArrow();
		});
		BeginParamActions.Add("ActivateRushArrow", delegate(int _, Parameter[] param)
		{
			ActivateRushArrowsAsync(param[0].Int, param[1].Float, param[2].Int, param[3].Int).Forget();
		});
		BeginParamActions.Add("ActivateFlame", delegate(int _, Parameter[] param)
		{
			ActivateFlame(param[0].Int, param[1].Int, param[2].Int);
		});
		BeginParamActions.Add("ActivateFlameBullet", delegate(int _, Parameter[] param)
		{
			ActivateFlameBullet(param[0].Int, param[1].Int, param[2].Int);
		});
		TimelineBeginActions.Add("MoveToCenter", delegate
		{
			SetPositionToCenter();
		});
		TimelineBeginActions.Add("SpaceFalling", delegate(int duration)
		{
			base.transform.DOMoveY(base.SpawnPosition.y, Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
		});
		TimelineBeginActions.Add("ActiveImmortal", delegate
		{
			base.IsImmortal = true;
		});
		TimelineBeginActions.Add("InactiveImmortal", delegate
		{
			base.IsImmortal = false;
		});
		TimelineBeginActions.Add("ToggleBackgroundAtThree", delegate
		{
			ToggleBackgroundAtPhase(3);
		});
		TimelineBeginActions.Add("ToggleBackgroundAtThreeDownKick", delegate
		{
			ToggleBackgroundAtPhaseDownKick(3);
		});
		arrowDirection = 0;
		SetSkin("Dark");
		downKickAttack.OnStart += Phase3RemovePower;
		flameAttack.OnStart += Phase3RemovePower;
		spaceControlAttack.OnStart += Phase3RemovePower;
		spaceControlAttack.OnStart += ClearFlameDamagedList;
	}

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	protected override void OnIncreasePhase(int before, int after)
	{
		base.OnIncreasePhase(before, after);
		SetBackground(after == 2);
		if (after == 3)
		{
			base.Fatigue = base.InitialFatigue;
		}
	}

	public override bool TryPhaseTransition()
	{
		if (!IsGroggyPlaying && base.Phase == 1 && base.Health < base.Phase2Health)
		{
			CancelPattern();
			EnterCutsceneState(3);
			return true;
		}
		return false;
	}

	public override void OnEnterBounceState()
	{
		base.transform.DOKill();
		int signX = FacingDirection.Opposite().GetSignX();
		float endValue = Enemy.Player.VisualBottom.x + (float)signX * 5f;
		base.transform.DOMoveX(endValue, Time.fixedDeltaTime * 15f);
	}

	public override void OnEnterExecution()
	{
		CancelPattern();
		base.transform.DOKill();
		base.OnEnterExecution();
	}

	protected override void OnEnterGroggyState()
	{
		CancelPattern();
		base.transform.DOKill();
		base.OnEnterGroggyState();
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		ClearPoolableObjects();
		isBlackOnStart = isBlack;
		List<int> list = new List<int>();
		list.AddRange(Enumerable.Repeat(0, downKickAttack.GetPower(this)));
		list.AddRange(Enumerable.Repeat(1, roundKickAttack.GetPower(this)));
		list.AddRange(Enumerable.Repeat(2, sideKickAttack.GetPower(this)));
		list.AddRange(Enumerable.Repeat(3, arrowAttack.GetPower(this)));
		list.AddRange(Enumerable.Repeat(4, isBlack ? spaceControlAttack.GetPower(this) : flameAttack.GetPower(this)));
		list.AddRange(Enumerable.Repeat(5, sweepTumbleAttack.GetPower(this)));
		int num = list[CryptoRandom.Range(0, list.Count)];
		List<BossAttackSequence> list2 = new List<BossAttackSequence>
		{
			downKickAttack,
			roundKickAttack,
			sideKickAttack,
			arrowAttack,
			isBlack ? spaceControlAttack : flameAttack,
			sweepTumbleAttack
		};
		for (int i = 0; i < list2.Count; i++)
		{
			if (i != num)
			{
				list2[i].CastObject<BossAttackSequence>().RisePower();
			}
		}
		return num switch
		{
			0 => new List<BossAttackSequence> { downKickAttack }, 
			1 => new List<BossAttackSequence> { roundKickAttack }, 
			2 => new List<BossAttackSequence>
			{
				sideKickAttack,
				CryptoRandom.RandomBool() ? arrowAttack : downKickAttack
			}, 
			3 => new List<BossAttackSequence> { arrowAttack }, 
			4 => new List<BossAttackSequence> { isBlack ? spaceControlAttack : flameAttack }, 
			5 => GetSweepTumbleAttack(isBlack), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return index switch
		{
			-1 => DefeatState, 
			3 => PhaseTwoTransitionState, 
			5 => PhaseThreeTransitionState, 
			7 => DefeatState, 
			_ => null, 
		};
	}

	private List<BossAttackSequence> GetSweepTumbleAttack(bool isBlackBackground)
	{
		List<BossAttackSequence> list = new List<BossAttackSequence> { sweepTumbleAttack };
		switch (CryptoRandom.Range(0, isBlackBackground ? 2 : 3))
		{
		case 0:
			list.Add(rushAttack);
			break;
		case 1:
			list.Add(sideKickAttack);
			list.Add(CryptoRandom.RandomBool() ? arrowAttack : downKickAttack);
			break;
		case 2:
			list.Add(flameAttack);
			break;
		}
		return list;
	}

	public override void MoveX(float velocityX, float deltaTime, bool moveForward)
	{
		if (!base.IsBounced && !IsInGroggyState)
		{
			if (base.StateMachine.CurrentState == base.IdleState)
			{
				SetAnimation(IdleAnimation);
				return;
			}
			Vector2 vector = (moveForward ? FacingDirection.ToVector2() : FacingDirection.Opposite().ToVector2());
			base.transform.position += (Vector3)vector * (velocityX * deltaTime);
		}
	}

	protected override void Die()
	{
		if (base.Phase < 3)
		{
			CancelPattern();
			EnterCutsceneState(5);
			health = new ClampedInt(base.Phase3Health, 0, base.Phase3Health);
		}
		else
		{
			base.Die();
		}
		ClearStatusEffect(force: true);
	}

	protected override void OnDie()
	{
		CancelPattern();
		downKickAttack.OnStart -= Phase3RemovePower;
		flameAttack.OnStart -= Phase3RemovePower;
		spaceControlAttack.OnStart -= Phase3RemovePower;
		spaceControlAttack.OnStart -= ClearFlameDamagedList;
	}

	public void SetBackground(bool shouldBlack, bool targetTop = false)
	{
		isBlack = shouldBlack;
		SpaceController.ChangeSpace(isBlack, targetTop);
	}

	private void ToggleBackground(bool targetTop = false)
	{
		if (base.StateMachine.CurrentState != DefeatState)
		{
			SetBackground(!isBlack, targetTop);
			if (base.Phase == 3)
			{
				base.Fatigue = 0f;
			}
		}
	}

	private void ToggleBackgroundAtPhase(int phaseNum)
	{
		if (base.Phase == phaseNum)
		{
			ToggleBackground();
		}
	}

	private void ToggleBackgroundAtPhaseDownKick(int phaseNum)
	{
		if (base.Phase == phaseNum)
		{
			ToggleBackground(isBlackOnStart);
		}
	}

	private void ClearPoolableObjects()
	{
		poolableObjects.RemoveAll((IPoolableObject x) => !x.GameObject.activeInHierarchy);
	}

	public void CancelPattern()
	{
		DisableVFXAll();
		poolableObjects.Where((IPoolableObject x) => x.GameObject.activeInHierarchy).ForEach(delegate(IPoolableObject x)
		{
			x.ReturnToPool();
		});
		poolableObjects.Clear();
	}

	private void FlipToArrow()
	{
		arrowDirection = (CryptoRandom.RandomBool() ? 1 : (-1));
		Flip(arrowDirection);
	}

	private void ActivateArrow()
	{
		arrowDamagedList.Clear();
		AreaBox areaBox = MyArea.AreaBox;
		if (arrowDirection == 0)
		{
			arrowDirection = (CryptoRandom.RandomBool() ? 1 : (-1));
		}
		bool movingRight = arrowDirection > 0;
		float num = 0f;
		int num2 = (movingRight ? areaBox.LowerLeft.x : areaBox.UpperRight.x);
		for (int i = 0; i < arrowLocalHeights.Count; i++)
		{
			float num3 = arrowLocalHeights[i];
			Vector3 pos = new Vector3(num2, VisualBottom.y + num3);
			VeritaHorizontalArrow veritaHorizontalArrow = Game.Instance.ObjectPoolManager.GetObject<VeritaHorizontalArrow>("Verita Horizontal Arrow.prefab", pos);
			veritaHorizontalArrow.Initialize(arrowParameter, isBlack, VisualCenter, areaBox, num3 - num, arrowDamagedList);
			AddPoolableObject(veritaHorizontalArrow);
			num = num3;
		}
		if (isBlack)
		{
			ActivateArrowPortalAsync((VisualCenter.x - (float)num2).GetSign()).Forget();
		}
		arrowDirection = 0;
		UniTaskExtension.WaitForFixedUpdatesOrCancel(arrowParameter.ShootDelayFrame).GetAwaiter().OnCompleted(delegate
		{
			if (!(base.StateMachine.CurrentState is CutsceneState))
			{
				int value = (((movingRight && !isBlack) || (!movingRight && isBlack)) ? 1 : 2);
				Enemy.SoundManager.PlayOneShotWithParameter(arrowEvent, "Local_Index", value);
			}
		});
	}

	private async UniTask ActivateArrowPortalAsync(int directionX)
	{
		await UniTaskExtension.WaitForFixedUpdatesOrCancel(arrowParameter.ShootDelayFrame);
		AreaBox areaBox = MyArea.AreaBox;
		float y = VisualBottom.y;
		float num = arrowLocalHeights[1];
		List<float> list = arrowLocalHeights;
		float y2 = y + (num + list[list.Count - 1]) / 2f;
		bool flag = directionX > 0;
		Vector3 pos = new Vector3(flag ? areaBox.LowerLeft.x : areaBox.UpperRight.x, y2);
		pos.x += (float)directionX * 1f;
		VeritaArrowPortal veritaArrowPortal = Game.Instance.ObjectPoolManager.GetObject<VeritaArrowPortal>("Verita Arrow Portal.prefab", pos);
		veritaArrowPortal.SetTimer(10);
		AddPoolableObject(veritaArrowPortal);
		pos.x = (flag ? areaBox.UpperRight.x : areaBox.LowerLeft.x);
		pos.x += (float)(-directionX) * 1f;
		VeritaArrowPortal veritaArrowPortal2 = Game.Instance.ObjectPoolManager.GetObject<VeritaArrowPortal>("Verita Arrow Portal.prefab", pos);
		veritaArrowPortal2.SetTimer(areaBox.Size.x / (int)(arrowParameter.ArrowSpeed * Time.fixedDeltaTime));
		AddPoolableObject(veritaArrowPortal2);
	}

	private void ActivateFlame(int duration, int signFrame, int cycleFrame, float posX = 0f, bool sound = true)
	{
		Vector2 vector = new Vector2((posX == 0f) ? Enemy.Player.VisualBottom.x : posX, VisualBottom.y + flameSize.y / 2f);
		VeritaFlame veritaFlame = Game.Instance.ObjectPoolManager.GetObject<VeritaFlame>("Verita Flame.prefab", vector);
		veritaFlame.Initalize(flameSize, duration, signFrame, cycleFrame, base.BossName, sound);
		AddPoolableObject(veritaFlame);
	}

	private void ClearFlameDamagedList()
	{
		flameDamagedList.Clear();
	}

	private void Phase3RemovePower()
	{
		if (base.Phase != 3)
		{
			return;
		}
		foreach (GenericBossAttackSequence<Verita> backgroundAttackSequence in BackgroundAttackSequences)
		{
			backgroundAttackSequence.RemovePower();
		}
	}

	private void ActivateFlameBullet(int delayFrame, int flameDuration, int cycleFrame)
	{
		List<float> list = new List<float>();
		float num = 14f;
		Vector2Int lowerLeft = MyArea.AreaBox.LowerLeft;
		Vector2Int upperRight = MyArea.AreaBox.UpperRight;
		for (float num2 = lowerLeft.x + CryptoRandom.Range(2, 10); !(num2 >= (float)upperRight.x); num2 += num)
		{
			list.Add(num2);
		}
		for (int i = 0; i < 5; i++)
		{
			Vector2 vector = new Vector2(list[i], base.SpawnPosition.y);
			VeritaFlameBullet veritaFlameBullet = Game.Instance.ObjectPoolManager.GetObject<VeritaFlameBullet>("Verita Flame Bullet.prefab", vector);
			veritaFlameBullet.Initialize(0f, delayFrame, flameDuration, cycleFrame, vector, flameDamagedList);
			ActivateFlame(flameDuration, delayFrame, cycleFrame, vector.x, i == 2);
			AddPoolableObject(veritaFlameBullet);
		}
	}

	private void SetPositionToCenter()
	{
		Vector2 vector = new Vector2(MyArea.AreaBox.Center.x, base.SpawnPosition.y + spaceLocalAppearHeight);
		base.transform.position = vector;
	}

	private async UniTask ActivateRushArrowsAsync(int arrowCount, float speed, int delayFrame, int appearDelayFrame)
	{
		float value = Enemy.Player.VisualCenter.x - (float)(Enemy.Player.FacingDirection.GetSignX() * 15);
		value = Mathf.Clamp(value, MyArea.AreaBox.LowerLeft.x, MyArea.AreaBox.UpperRight.x);
		Vector3 pos = new Vector3(value, VisualCenter.y);
		for (int i = 0; i < arrowCount; i++)
		{
			pos.y += 5f;
			VeritaAimArrow veritaAimArrow = Game.Instance.ObjectPoolManager.GetObject<VeritaAimArrow>("Verita Space Arrow.prefab", pos);
			veritaAimArrow.Initialize(speed, delayFrame);
			AddPoolableObject(veritaAimArrow);
			await UniTaskExtension.WaitForFixedUpdatesOrCancel(appearDelayFrame);
		}
	}

	private void SpawnFlameField(int duration, int cycle)
	{
		bool flag = ((pattern != Pattern.None) ? debugBlackBackground : isBlackOnStart);
		flameFieldEffectGroup.SetActive(!flag);
		if (!flag)
		{
			VeritaDownKickFlame veritaDownKickFlame = Game.Instance.ObjectPoolManager.GetObject<VeritaDownKickFlame>("Verita Down Kick Flame Effect.prefab", base.transform.position);
			veritaDownKickFlame.Area = MyArea;
			veritaDownKickFlame.SetMute();
			veritaDownKickFlame.PlayEffect();
			GatekeeperFlameField gatekeeperFlameField = Game.Instance.ObjectPoolManager.GetObject<GatekeeperFlameField>("Verita Flame Field.prefab", VisualBottom);
			gatekeeperFlameField.transform.Translate(new Vector3(0f, gatekeeperFlameField.Box2D.size.y / 2f));
			gatekeeperFlameField.Initialize(duration, cycle, (float)MyArea.AreaBox.Size.x * 2f, setParticle: false);
		}
	}

	private void ActivateAdditionalExplosionWave()
	{
		if ((pattern != Pattern.None) ? debugBlackBackground : isBlackOnStart)
		{
			arrowDamagedList.Clear();
			AreaBox areaBox = MyArea.AreaBox;
			float num = (float)areaBox.LowerLeft.x + downArrowGap / 2f;
			float y = areaBox.UpperRight.y;
			for (; !(num >= (float)areaBox.UpperRight.x); num += downArrowGap)
			{
				Vector3 pos = new Vector3(num, y);
				VeritaVerticalArrow veritaVerticalArrow = Game.Instance.ObjectPoolManager.GetObject<VeritaVerticalArrow>("Verita Vertical Arrow.prefab", pos);
				veritaVerticalArrow.Initialize(downArrowParameter, areaBox, downArrowGap, arrowDamagedList);
				AddPoolableObject(veritaVerticalArrow);
			}
		}
	}

	private void AddPoolableObject(IPoolableObject newObject)
	{
		if (poolableObjects.All((IPoolableObject po) => (object)po.GameObject != newObject.GameObject))
		{
			poolableObjects.Add(newObject);
		}
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		if (!arrowLocalHeights.IsNullOrEmpty())
		{
			foreach (float arrowLocalHeight in arrowLocalHeights)
			{
				Gizmos.color = Color.cyan;
				Gizmos.DrawCube(VisualBottom + new Vector2(0f, arrowLocalHeight), Vector3.one);
			}
		}
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireCube(VisualBottom + spaceFlameBulletAreaBox.Center, spaceFlameBulletAreaBox.Size.ToVector3());
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(VisualBottom + Vector2.up * spaceLocalAppearHeight, Vector3.one);
	}
}
