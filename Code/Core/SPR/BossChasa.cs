using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using UnityEngine;

namespace SPR;

public class BossChasa : BossCharacter
{
	private enum Pattern
	{
		None,
		PoseA,
		PoseB,
		PoseY,
		Batto
	}

	public const int DyingStateIndex = 6;

	[Header("패턴 디버깅")]
	[SerializeField]
	private Pattern patternDebug;

	[Header("공격")]
	[SerializeField]
	private GenericBossAttackSequence<BossChasa> poseA;

	[SerializeField]
	private GenericBossAttackSequence<BossChasa> poseB;

	[SerializeField]
	private GenericBossAttackSequence<BossChasa> poseY;

	[SerializeField]
	private GenericBossAttackSequence<BossChasa> bladeBeamAttack;

	[SerializeField]
	private GenericBossAttackSequence<BossChasa> teleport;

	[Header("파라미터")]
	[SerializeField]
	private int knifeThrowChance;

	private List<Vector2> groundPositions;

	private ChasaShadow shadow;

	public override string Name => "Chasa";

	public override string GroggyStartAnim => "groggy";

	public override string ID => Name;

	protected override float Width => base.Width * 2f;

	private Vector2 GroundCenter => groundPositions[groundPositions.Count / 2];

	private CutsceneState DyingState { get; set; }

	private void LateUpdate()
	{
		AdjustXPosition();
	}

	public override void InitializeOnInstantiate(int sortingOrder, Vector2 spawnPos)
	{
		base.InitializeOnInstantiate(sortingOrder, spawnPos);
		groundPositions = GetGroundPositions();
		DyingState = new CutsceneState(this, 6, blockControl: true);
		base.StateMachine.ChangeState(base.IdleState);
		AddAction(ActionTiming.Begin, "TeleportAroundPlayer", (Action<int, Parameter[]>)delegate
		{
			SetVisible(visible: false);
			EmitTeleportParticle("VFX/Chasa_Teleport_In.prefab");
		});
		AddAction(ActionTiming.End, "TeleportAroundPlayer", delegate(Parameter[] p)
		{
			TeleportAroundPlayer(p[0].Float);
			EmitTeleportParticle("VFX/Chasa_Teleport_Out.prefab");
		});
		TimelineActions.Add("SpawnShadow", delegate
		{
			SpawnShadow();
		});
		AddAction(ActionTiming.Begin, "BattoTeleport", TeleportForBatto);
		BeginParamActions.Add("DischargeBladeBeam", delegate(int _, Parameter[] param)
		{
			DischargeBladeBeam(param[0].Float);
		});
		TimelineEndActions.Add("Teleport", delegate
		{
			EmitTeleportParticle("VFX/Chasa_Teleport_Out.prefab");
		});
		TimelineActions.Add("Teleport", delegate
		{
			Teleport();
		});
	}

	public void InitializeStat()
	{
		ResetStatus();
		SetAnimationInstantly("idle");
	}

	protected override List<AttackSequence> GetAllAttackSequences()
	{
		return new List<AttackSequence> { poseA, poseB, poseY, bladeBeamAttack };
	}

	protected override List<BossAttackSequence> CreateBossAttackSequences()
	{
		if (patternDebug != Pattern.None)
		{
			return patternDebug switch
			{
				Pattern.PoseA => new List<BossAttackSequence> { poseA, teleport }, 
				Pattern.PoseB => new List<BossAttackSequence> { poseB, teleport }, 
				Pattern.PoseY => new List<BossAttackSequence> { poseY }, 
				Pattern.Batto => new List<BossAttackSequence> { bladeBeamAttack }, 
				_ => null, 
			};
		}
		List<int> list = new List<int>();
		list.AddRange(Enumerable.Repeat(0, poseA.GetPower(this) + poseB.GetPower(this)));
		list.AddRange(Enumerable.Repeat(1, poseY.GetPower(this)));
		list.AddRange(Enumerable.Repeat(2, bladeBeamAttack.GetPower(this)));
		int num = list[CryptoRandom.Range(0, list.Count)];
		for (int i = 0; i < base.AllAttackSequences.Count; i++)
		{
			if (i == num)
			{
				if (num == 0)
				{
					i = 1;
				}
			}
			else
			{
				base.AllAttackSequences[i].CastObject<BossAttackSequence>().RisePower();
			}
		}
		return num switch
		{
			0 => new List<BossAttackSequence>
			{
				CryptoRandom.RandomBool() ? poseA : poseB,
				teleport
			}, 
			1 => new List<BossAttackSequence> { poseY }, 
			2 => new List<BossAttackSequence> { bladeBeamAttack }, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public override void OnEnterExecution()
	{
		base.OnEnterExecution();
		SetVisible(visible: true);
		shadow?.ReturnToPool();
	}

	protected override void OnEnterGroggyState()
	{
		base.OnEnterGroggyState();
		SetVisible(visible: true);
		shadow?.ReturnToPool();
	}

	protected override CutsceneState GetTargetState(int index)
	{
		return index switch
		{
			-1 => DyingState, 
			6 => DyingState, 
			_ => null, 
		};
	}

	private void TeleportAroundPlayer(float distance)
	{
		int num = -(Enemy.Player.VisualBottom - VisualBottom).normalized.x.GetSign();
		num = ((num != 0) ? num : (MyArea.AreaBox.Center - VisualCenter).normalized.x.GetSign());
		base.transform.position = new Vector3(Enemy.Player.VisualBottom.x + (float)num * distance, base.SpawnPosition.y);
		SetVisible(visible: true);
	}

	private void TeleportForBatto()
	{
		EmitTeleportParticle("VFX/Chasa_Teleport_In.prefab");
		if (Enemy.Player.VisualCenter.x < GroundCenter.x)
		{
			List<Vector2> list = groundPositions;
			Vector3 position = new Vector3(list[list.Count - 1].x, base.SpawnPosition.y);
			base.transform.position = position;
		}
		else
		{
			Vector3 position = new Vector3(groundPositions[0].x, base.SpawnPosition.y);
			base.transform.position = position;
		}
		EmitTeleportParticle("VFX/Chasa_Teleport_Out.prefab");
	}

	private void DischargeBladeBeam(float speed)
	{
		Vector2 normalized = (MyArea.AreaBox.Center - VisualCenter).normalized;
		Vector3 vector;
		if (normalized.x.GetSign() >= 0)
		{
			List<Vector2> list = groundPositions;
			vector = new Vector3(list[list.Count - 1].x, VisualCenter.y);
		}
		else
		{
			vector = new Vector3(groundPositions[0].x, VisualCenter.y);
		}
		Vector3 end = vector;
		BeamAttack beamAttack = Enemy.PoolManager.GetObject<BeamAttack>("ChasaBladeBeam.prefab", VisualCenter);
		beamAttack.transform.localScale = new Vector3(normalized.x.GetSign(), 1f, 1f);
		beamAttack.Initialize(speed, end);
	}

	private void Teleport()
	{
		base.transform.position = groundPositions[CryptoRandom.Range(0, groundPositions.Count)];
	}

	private List<Vector2> GetGroundPositions()
	{
		float num = (float)MyArea.AreaBox.LowerLeft.x + Width;
		float num2 = (float)MyArea.AreaBox.UpperRight.x - Width;
		List<Vector2> list = new List<Vector2>();
		for (float num3 = num; num3 < num2; num3 += 1f)
		{
			list.Add(new Vector2(num3, base.SpawnPosition.y));
		}
		return list;
	}

	private void SpawnShadow()
	{
		shadow = Enemy.PoolManager.GetObject<ChasaShadow>("Chasa_Shadow.prefab", VisualBottom);
		shadow.SetArea(MyArea);
		shadow.PoseYAttack();
		shadow.SpawnPosition = base.SpawnPosition;
	}

	private void EmitTeleportParticle(string address)
	{
		Enemy.PoolManager.GetObject<PoolingParticle>(address, VisualCenter).Play();
	}
}
