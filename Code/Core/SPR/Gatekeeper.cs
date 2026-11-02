using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FMODUnity;
using Library;
using SPR.FSM.Base;
using SPR.Library;
using UnityEngine;

namespace SPR;

public class Gatekeeper : BossCharacter
{
	[Serializable]
	private class ComboSet
	{
		[SerializeField]
		public TimelineEffect attackPattern;

		[SerializeField]
		public int chance;

		[SerializeField]
		public ComboSet[] nextCombo;
	}

	private enum Pattern
	{
		None,
		ThunderStrike,
		FlameStrike,
		Combo,
		WarriorStrike,
		GreatSwordStrike,
		ThunderRage,
		FlameRage,
		WarriorStrikeEvo,
		GreatSwordStrikeEvo,
		ComboEvo
	}

	[Header("Debug")]
	[SerializeField]
	private Pattern pattern;

	[Header("패시브")]
	[Tooltip("패링 확률")]
	[SerializeField]
	private int parryChance;

	[Header("천둥의 격")]
	[Tooltip("번개 전조 시간")]
	[SerializeField]
	private int lightningSignDuration;

	[Tooltip("번개 지속 시간")]
	[SerializeField]
	private int lightningDuration;

	[Tooltip("번개 지속 시간")]
	[SerializeField]
	private float lightningWidth;

	[Tooltip("번개 지속 시간")]
	[SerializeField]
	private float lightningHeight;

	[Header("화염의 격")]
	[Tooltip("화염의 격 이동시간")]
	[SerializeField]
	private int flameStrikeMoveDuration;

	[Tooltip("화염 장판 지속 시간")]
	[SerializeField]
	private int flameFieldDuration;

	[Tooltip("화염 장판 공격 싸이클")]
	[SerializeField]
	private int flameFieldCycleFrame;

	[Header("연속 베기")]
	[Tooltip("천둥의 격 스킵 프레임")]
	[SerializeField]
	private int thunderStrikeSkipFrame;

	[Tooltip("화염의 격 스킵 프레임")]
	[SerializeField]
	private int flameStrikeSkipFrame;

	[Header("천둥의 격노")]
	[Tooltip("천둥의 격노 낙하")]
	[SerializeField]
	private int thunderRageDropDuration;

	[Header("화염의 격노")]
	[Tooltip("화염의 격노 검기 속도")]
	[SerializeField]
	private float flameEnergySpeed;

	[SerializeField]
	private Zone flameRageZone;

	[Header("장수의 격(진)")]
	[SerializeField]
	private Ease glideXEase;

	[SerializeField]
	private Ease glideYEase;

	[Tooltip("장수의 격(진) 점프 높이")]
	[SerializeField]
	private float warriorJumpHeight;

	[Tooltip("장수의 격(진) 활강 도착 거리(플레이어 기준)")]
	[SerializeField]
	private float warriorGlideDestDistance;

	[Tooltip("장수의 격(진) 점프 활강 프레임")]
	[SerializeField]
	private float warriorGlideDuration;

	[Tooltip("장수의 격(진) 상승 프레임")]
	[SerializeField]
	private float warriorRiseDuration;

	[Tooltip("장수의 격(진) 낙하 프레임")]
	[SerializeField]
	private float warriorDropDuration;

	[Tooltip("장수의 격(진) 낙하 x축 거리(현재 위치 기준)")]
	[SerializeField]
	private float warriorTopXDist;

	[Tooltip("장수의 격(진) 낙하 시작 높이")]
	[SerializeField]
	private float warriorTopHeight;

	[Tooltip("장수의 격(진) 백스텝 거리")]
	[SerializeField]
	private float warriorBackStepDist;

	[Tooltip("장수의 격(진) 백스텝 높이")]
	[SerializeField]
	private float warriorBackStepPower;

	[Tooltip("장수의 격(진) 백스텝 프레임")]
	[SerializeField]
	private int warriorBackStepFrame;

	[Header("Sequence")]
	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> thunderStrike;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> flameStrike;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> warriorStrike;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> greatSwordStrike;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> comboHorizontal;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> comboVertical;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> thunderRage;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> flameRage;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> warriorStrikeEvo;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> warriorStrikeEvoGrab;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> greatSwordStrikeEvo;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> greatSwordStrikeEvoGrab;

	[SerializeField]
	private GenericBossAttackSequence<Gatekeeper> comboEvo;

	[Header("Combo")]
	[SerializeField]
	private ComboSet comboSet;

	[SerializeField]
	private ComboSet comboEvoSet;

	[Header("SFX Events")]
	[SerializeField]
	private EventReference lightningEvent;

	[SerializeField]
	private EventReference flameFieldEvent;

	public const int OpeningStateIndex = 0;

	public const int AfterOpeningStateIndex = 1;

	public const int SecondPhaseStateIndex = 2;

	public const int AfterPhaseStateIndex = 3;

	public const int DyingStateIndex = 4;

	public const int AfterDyingStateIndex = 5;

	private float fireFieldWidth;

	private IAttackSource takeDamageAttackSource;

	private Sequence glideSequence;

	private Zone anchoredFlameRageZone;

	private readonly List<IDamageable> flameRageDamageList = new List<IDamageable>();

	public override string Name => "Gatekeeper";

	private PhaseTransitionCutsceneState PhaseTransitionState { get; set; }

	private CutsceneState DyingState { get; set; }

	public override bool CanMakeStuck
	{
		get
		{
			if (!(takeDamageAttackSource is PlayerBullet))
			{
				return TryParry();
			}
			return false;
		}
	}

	protected override float Width => HitBox.size.x * 2.5f;

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		PhaseTransitionState = new PhaseTransitionCutsceneState(this, 2, blockControl: false, hideCanvas: false);
		DyingState = new CutsceneState(this, 4);
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		base.StateMachine.ChangeState(base.IdleState);
		AddAction(ActionTiming.Begin, "GenerateLightning", delegate(int duration, Parameter[] p)
		{
			GenerateLightning(duration, p[0].Float);
		});
		AddAction(ActionTiming.Begin, "GenerateFlameField", (Action)delegate
		{
			fireFieldWidth = VisualBottom.x;
		});
		AddAction(ActionTiming.End, "GenerateFlameField", (Action)delegate
		{
			fireFieldWidth = Mathf.Abs(VisualBottom.x - fireFieldWidth);
			GenerateFlameField();
		});
		AddAction(ActionTiming.Begin, "MoveAreaEnd", MoveAreaEnd);
		AddAction(ActionTiming.Begin, "ThunderRageDrop", ThunderRageDrop);
		AddAction(ActionTiming.Begin, "LookDestination", (Action)delegate
		{
			LookAt(((Enemy.Player.Position.x > MyArea.AreaBox.Center.x) ? MyArea.AreaBox.LowerLeft : MyArea.AreaBox.UpperRight).ToVector3());
		});
		AddAction(ActionTiming.Begin, "RageJump", delegate(int duration, Parameter[] p)
		{
			Jump(duration, p[0].Float);
		});
		AddAction(ActionTiming.Begin, "FlameRageAttack", delegate(int duration, Parameter[] p)
		{
			FlameRageAttackBegin(duration, p[0].Float);
		});
		AddAction(ActionTiming.OnFrame, "FlameRageAttack", (Action<int, Parameter[]>)delegate
		{
			FlameRageAttackOnFrame();
		});
		AddAction(ActionTiming.Begin, "DischargeFire", DischargeFire);
		AddAction(ActionTiming.OnFrame, "TryGrabPlayer", TryGrabPlayer);
		AddAction(ActionTiming.Begin, "GlideX", GlideX);
		AddAction(ActionTiming.Begin, "HoldPlayer", (Action)delegate
		{
			Enemy.Player.SetGravityFree(active: true);
		});
		AddAction(ActionTiming.End, "HoldPlayer", (Action)delegate
		{
			Enemy.Player.SetGravityFree(active: false);
		});
		AddAction(ActionTiming.OnFrame, "HoldPlayer", HoldPlayer);
		AddAction(ActionTiming.Begin, "WarriorRise", WarriorRise);
		AddAction(ActionTiming.Begin, "WarriorDrop", WarriorDrop);
		AddAction(ActionTiming.Begin, "BackStep", (Action)delegate
		{
			BackStep(warriorBackStepDist, warriorBackStepPower, warriorBackStepFrame);
		});
	}

	public override bool TryTakeDamage(HitInfo hitInfo)
	{
		takeDamageAttackSource = hitInfo.Source;
		if (takeDamageAttackSource is PlayerBullet playerBullet && TryParry())
		{
			playerBullet.Reverse();
			return false;
		}
		return base.TryTakeDamage(hitInfo);
	}

	public override bool TryPhaseTransition()
	{
		if (!IsGroggyPlaying && base.Phase == 1 && base.Health < base.Phase2Health)
		{
			EnterCutsceneState(2);
			return true;
		}
		return false;
	}

	public override void OnEnterBounceState()
	{
		DOTween.Kill(base.transform);
		ForceDrop();
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		DOTween.Kill(base.transform);
		ForceDrop();
	}

	public override void OnPlayerHit(HitInfo hitInfo)
	{
		if (hitInfo.AttackName.Contains("GreatSword_Strike_Evo_Grab") && Enemy.Player.CanGrab && Enemy.Player.PSM.CurrentState == Enemy.Player.PSM.HitState && !Enemy.Player.CanParry(hitInfo))
		{
			PlayerStaggerHitState newState = new PlayerStaggerHitState(Enemy.Player.PSM)
			{
				Duration = greatSwordStrikeEvoGrab.Duration,
				NextState = Enemy.Player.PSM.DownState,
				CancelByOtherAttack = true,
				TargetPosition = Enemy.Player.Position
			};
			Enemy.Player.PSM.ChangeState(newState);
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { greatSwordStrikeEvoGrab }));
		}
	}

	public override void OnAttackEnd(IState nextState)
	{
		if (nextState is BossIdleState || nextState is EnemyBounceState || nextState is GroggyState)
		{
			BossAttackState bossAttackState = base.StateMachine.CurrentState.CastObject<BossAttackState>();
			if (bossAttackState.LastSequence == warriorStrikeEvoGrab)
			{
				base.Fatigue = warriorStrikeEvo.Fatigue;
			}
			else if (bossAttackState.LastSequence == greatSwordStrikeEvoGrab)
			{
				base.Fatigue = greatSwordStrike.Fatigue;
			}
			else
			{
				base.Fatigue = bossAttackState.LastSequenceFatigue;
			}
		}
	}

	protected override void OnDie()
	{
		ClearPlayerStagger();
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence>
		{
			thunderStrike, flameStrike, warriorStrike, greatSwordStrike, comboHorizontal, comboVertical, thunderRage, flameRage, warriorStrikeEvo, greatSwordStrikeEvo,
			comboEvo
		};
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		if (pattern != Pattern.None)
		{
			return pattern switch
			{
				Pattern.None => new List<BossAttackSequence>(), 
				Pattern.ThunderStrike => new List<BossAttackSequence> { thunderStrike }, 
				Pattern.FlameStrike => new List<BossAttackSequence> { flameStrike }, 
				Pattern.Combo => GetAttackFromComboSet(comboSet), 
				Pattern.WarriorStrike => new List<BossAttackSequence> { warriorStrike }, 
				Pattern.GreatSwordStrike => new List<BossAttackSequence> { greatSwordStrike }, 
				Pattern.ThunderRage => new List<BossAttackSequence> { thunderRage }, 
				Pattern.FlameRage => new List<BossAttackSequence> { flameRage }, 
				Pattern.WarriorStrikeEvo => new List<BossAttackSequence> { warriorStrikeEvo }, 
				Pattern.GreatSwordStrikeEvo => new List<BossAttackSequence> { greatSwordStrikeEvo }, 
				Pattern.ComboEvo => GetAttackFromComboSet(comboEvoSet), 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}
		return GetSequences();
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return index switch
		{
			-1 => DyingState, 
			2 => PhaseTransitionState, 
			4 => DyingState, 
			_ => null, 
		};
	}

	private List<BossAttackSequence> GetSequences()
	{
		List<int> list = new List<int>();
		if (base.Phase == 1)
		{
			list.AddRange(Enumerable.Repeat(0, thunderStrike.GetPower(this)));
			list.AddRange(Enumerable.Repeat(1, flameStrike.GetPower(this)));
			list.AddRange(Enumerable.Repeat(2, warriorStrike.GetPower(this)));
			list.AddRange(Enumerable.Repeat(3, greatSwordStrike.GetPower(this)));
			list.AddRange(Enumerable.Repeat(4, comboHorizontal.GetPower(this)));
			int num = list[CryptoRandom.Range(0, list.Count)];
			List<BossAttackSequence> list2 = new List<BossAttackSequence> { thunderStrike, flameStrike, warriorStrike, greatSwordStrike, comboHorizontal };
			for (int i = 0; i < list2.Count; i++)
			{
				if (i != num)
				{
					list2[i].CastObject<BossAttackSequence>().RisePower();
				}
			}
			return num switch
			{
				0 => new List<BossAttackSequence> { thunderStrike }, 
				1 => new List<BossAttackSequence> { flameStrike }, 
				2 => new List<BossAttackSequence> { warriorStrike }, 
				3 => new List<BossAttackSequence> { greatSwordStrike }, 
				4 => GetAttackFromComboSet(comboSet), 
				_ => throw new ArgumentOutOfRangeException(), 
			};
		}
		list.AddRange(Enumerable.Repeat(0, thunderRage.GetPower(this)));
		list.AddRange(Enumerable.Repeat(1, flameRage.GetPower(this)));
		list.AddRange(Enumerable.Repeat(2, warriorStrikeEvo.GetPower(this)));
		list.AddRange(Enumerable.Repeat(3, greatSwordStrikeEvo.GetPower(this)));
		list.AddRange(Enumerable.Repeat(4, comboEvo.GetPower(this)));
		int num2 = list[CryptoRandom.Range(0, list.Count)];
		List<BossAttackSequence> list3 = new List<BossAttackSequence> { thunderRage, flameRage, warriorStrikeEvo, greatSwordStrikeEvo, comboEvo };
		for (int j = 0; j < list3.Count; j++)
		{
			if (j != num2)
			{
				list3[j].CastObject<BossAttackSequence>().RisePower();
			}
		}
		return list[CryptoRandom.Range(0, list.Count)] switch
		{
			0 => new List<BossAttackSequence> { thunderRage }, 
			1 => new List<BossAttackSequence> { flameRage }, 
			2 => new List<BossAttackSequence> { warriorStrikeEvo }, 
			3 => new List<BossAttackSequence> { greatSwordStrikeEvo }, 
			4 => GetAttackFromComboSet(comboEvoSet), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private bool TryParry()
	{
		if (base.StateMachine.CurrentState.GetType() != typeof(BossIdleState))
		{
			return false;
		}
		if (Enemy.Player.PSM.CurrentState == Enemy.Player.PSM.ChargeAttackState)
		{
			return false;
		}
		Vector2 normalized = (Enemy.Player.VisualBottom - VisualBottom).normalized;
		if (FacingDirection.GetSignX() == normalized.x.GetSign() && Enemy.Player.FacingDirection.GetSignX() != FacingDirection.GetSignX())
		{
			if (CryptoRandom.Range(1, 101) > parryChance)
			{
				return false;
			}
			SetAnimation(CryptoRandom.Range(0, 3) switch
			{
				0 => "parrying", 
				1 => "parrying_2", 
				2 => "parrying_3", 
				_ => "parrying", 
			}, loop: false);
			return true;
		}
		return false;
	}

	private List<BossAttackSequence> GetAttackFromComboSet(ComboSet combo, bool isRepeat = false)
	{
		List<BossAttackSequence> list = new List<BossAttackSequence>();
		if (combo.attackPattern == comboHorizontal.TimelineEffect && isRepeat)
		{
			list.AddRange(GetAttackFromComboSet(combo, isRepeat: true));
			return list;
		}
		GenericBossAttackSequence<Gatekeeper> item = base.AllAttackSequences.Find((AttackSequence s) => s.IsSameTimelineEffect(combo.attackPattern)).CastObject<GenericBossAttackSequence<Gatekeeper>>();
		list.Add(item);
		if (combo.nextCombo.Length == 0)
		{
			return list;
		}
		int num = CryptoRandom.Range(1, 101);
		int num2 = 0;
		ComboSet[] nextCombo = combo.nextCombo;
		foreach (ComboSet comboSet in nextCombo)
		{
			num2 += comboSet.chance;
			if (num <= num2)
			{
				list.AddRange(GetAttackFromComboSet(comboSet));
				break;
			}
		}
		return list;
	}

	private void GenerateLightning(int duration, float speed)
	{
		LightningAttack thunder = Enemy.PoolManager.GetObject<LightningAttack>("Gatekeeper_Thunder.prefab", VisualBottom);
		float num = (float)(-FacingDirection.GetSignX()) * lightningWidth / 2f;
		thunder.transform.position += new Vector3((float)FacingDirection.GetSignX() * speed * Time.fixedDeltaTime * (float)duration + num, 0f);
		thunder.Activate(lightningSignDuration, lightningDuration).Forget();
		UniTaskExtension.WaitForFixedUpdatesOrCancel(lightningSignDuration).GetAwaiter().OnCompleted(delegate
		{
			Enemy.SoundManager.PlayOneShotAttached(lightningEvent, thunder.transform);
		});
	}

	private void GenerateFlameField()
	{
		Vector2 vector = new Vector2(VisualBottom.x + (float)FacingDirection.Opposite().GetSignX() * fireFieldWidth / 2f, base.SpawnPosition.y);
		GatekeeperFlameField gatekeeperFlameField = Enemy.PoolManager.GetObject<GatekeeperFlameField>("Gatekeeper Flame Field.prefab", vector);
		gatekeeperFlameField.Initialize(flameFieldDuration, flameFieldCycleFrame, fireFieldWidth + Width * 2f);
		Enemy.SoundManager.PlayOneShotAttached(flameFieldEvent, gatekeeperFlameField.transform);
	}

	private void MoveAreaEnd()
	{
		float x = (((float)FacingDirection.GetSignX() < 0f) ? ((float)MyArea.AreaBox.LowerLeft.x + Width) : ((float)MyArea.AreaBox.UpperRight.x - Width));
		ShortcutExtensions.DOMove(endValue: new Vector3(x, base.SpawnPosition.y), target: base.transform, duration: Util.FrameToSeconds(flameStrikeMoveDuration)).SetUpdate(UpdateType.Fixed).SetEase(Ease.Linear);
	}

	private void Jump(int duration, float height)
	{
		Vector3 endValue = base.transform.position + new Vector3(0f, height);
		base.transform.DOMove(endValue, Util.FrameToSeconds(duration)).SetEase(Ease.InSine);
	}

	private void ThunderRageDrop()
	{
		Vector3 endValue = new Vector3(MyArea.AreaBox.Center.x, base.SpawnPosition.y);
		base.transform.DOMove(endValue, Util.FrameToSeconds(thunderRageDropDuration)).SetEase(Ease.InSine);
	}

	private void FlameRageAttackBegin(int duration, float distance)
	{
		anchoredFlameRageZone = flameRageZone.GetZoneOf(this);
		flameRageDamageList.Clear();
		AreaBox areaBox = MyArea.AreaBox;
		float value = base.Position.x + (float)FacingDirection.GetSignX() * distance;
		value = Mathf.Clamp(value, (float)areaBox.LowerLeft.x + Width, (float)areaBox.UpperRight.x - Width);
		ShortcutExtensions.DOMove(endValue: new Vector3(value, base.SpawnPosition.y), target: base.transform, duration: Util.FrameToSeconds(duration)).SetEase(Ease.Linear);
	}

	private void FlameRageAttackOnFrame()
	{
		HitInfo hitInfo = new HitInfo(this, "Flame_Rage");
		hitInfo.SetIgnoreStun(value: false);
		AttackUtility.AttackBox(anchoredFlameRageZone.Center, anchoredFlameRageZone.Size, hitInfo, LayerUtility.PlayerLayer, flameRageDamageList);
	}

	private void DischargeFire()
	{
		int num = (((float)FacingDirection.GetSignX() < 0f) ? MyArea.AreaBox.LowerLeft.x : MyArea.AreaBox.UpperRight.x);
		Vector3 vector = new Vector3(num, VisualCenter.y);
		GatekeeperFlameEnergy gatekeeperFlameEnergy = Enemy.PoolManager.GetObject<GatekeeperFlameEnergy>("Gatekeeper Flame Energy.prefab", VisualCenter);
		Vector3 localScale = gatekeeperFlameEnergy.transform.localScale;
		gatekeeperFlameEnergy.transform.localScale = new Vector3(Mathf.Abs(localScale.x) * (float)FacingDirection.GetSignX(), localScale.y, localScale.z);
		gatekeeperFlameEnergy.Discharge(flameEnergySpeed, vector);
	}

	private void GlideX(int duration)
	{
		float num = warriorGlideDestDistance * (float)FacingDirection.GetSignX();
		float value = Enemy.Player.VisualBottom.x + num;
		value = Mathf.Clamp(value, (float)MyArea.AreaBox.LowerLeft.x + Width, (float)MyArea.AreaBox.UpperRight.x - Width);
		glideSequence = DOTween.Sequence();
		glideSequence.Append(base.transform.DOMoveX(value, Util.FrameToSeconds(warriorGlideDuration)).SetEase(glideXEase)).Join(base.transform.DOMoveY(base.SpawnPosition.y, Util.FrameToSeconds(warriorGlideDuration)).SetEase(glideYEase));
	}

	private void TryGrabPlayer()
	{
		Zone sightZone = warriorStrikeEvo.GetSightZone(this);
		Collider2D collider2D = AttackUtility.OverlapBox(sightZone.Center, sightZone.Size, LayerUtility.PlayerLayer);
		if ((object)collider2D != null && collider2D.TryGetComponent<PlayerMover>(out var component) && component.CanHit)
		{
			PlayerStaggerHitState newState = new PlayerStaggerHitState(Enemy.Player.PSM)
			{
				Duration = 45,
				NextState = Enemy.Player.PSM.DownState,
				TargetPosition = Enemy.Player.Position
			};
			Enemy.Player.PSM.ChangeState(newState);
			base.StateMachine.ChangeState(new BossAttackState(this, () => new List<AttackSequence> { warriorStrikeEvoGrab }));
		}
	}

	private void WarriorRise()
	{
		Vector3 vector = base.transform.position + new Vector3(warriorTopXDist, warriorTopHeight);
		DOTween.Kill(base.transform);
		DOTween.Sequence().Append(base.transform.DOMoveX(vector.x, Util.FrameToSeconds(warriorRiseDuration)).SetEase(Ease.Linear)).Join(base.transform.DOMoveY(vector.y, Util.FrameToSeconds(warriorRiseDuration)).SetEase(Ease.OutCirc));
	}

	private void WarriorDrop()
	{
		float value = base.transform.position.x + warriorTopXDist * (float)FacingDirection.GetSignX();
		value = Mathf.Clamp(value, (float)MyArea.AreaBox.LowerLeft.x + Width, (float)MyArea.AreaBox.UpperRight.x - Width);
		Vector3 endValue = new Vector3(value, base.SpawnPosition.y);
		DOTween.Kill(base.transform);
		Sequence sequence = DOTween.Sequence();
		sequence.Append(base.transform.DOMoveX(endValue.x, Util.FrameToSeconds(warriorDropDuration) / 2f).SetEase(Ease.Linear)).Append(base.transform.DOMove(endValue, Util.FrameToSeconds(warriorDropDuration) / 2f)).SetEase(Ease.OutCirc);
		sequence.OnComplete(delegate
		{
			Enemy.Player.TryTakeDamage(new HitInfo(this, "Warrior_Strike_Evo"));
		});
	}

	private void HoldPlayer()
	{
		Vector3 boneWorldPosition = GetBoneWorldPosition("hold");
		if (Enemy.Player.PSM.CurrentState is PlayerStaggerHitState playerStaggerHitState)
		{
			playerStaggerHitState.TargetPosition = boneWorldPosition;
		}
	}

	private void BackStep(float distance, float backStepJumpPower, int backStepFrame)
	{
		ShortcutExtensions.DOJump(endValue: new Vector3(VisualBottom.x + distance * (float)(-FacingDirection.GetSignX()), base.SpawnPosition.y), target: base.transform, jumpPower: backStepJumpPower, numJumps: 1, duration: Util.FrameToSeconds(backStepFrame)).SetEase(Ease.Linear);
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		Gizmos.color = Color.cyan;
		if (Application.isPlaying)
		{
			Gizmos.DrawWireCube(anchoredFlameRageZone.Center, anchoredFlameRageZone.Size);
			return;
		}
		Zone zoneOf = flameRageZone.GetZoneOf(this);
		Gizmos.DrawWireCube(zoneOf.Center, zoneOf.Size);
	}
}
