using Cysharp.Threading.Tasks;
using SPR.Save;
using UnityEngine;

namespace SPR;

public class Cor : Npc
{
	private Collider2D col;

	private const string DyingIdleAnimation = "scene/farewell_idle";

	private const string DyingStartAnimation = "scene/farewell_see_tot_start";

	private const string DyingFirstAnimation = "scene/farewell_see_tot_loop";

	private const string DeathLoopAnimation = "scene/farewell_death_loop";

	private const string FillPhaseProperty = "_FillPhase";

	private const string FillColorProperty = "_FillColor";

	private static readonly int FillPhasePropertyID = Shader.PropertyToID("_FillPhase");

	private static readonly int FillColorPropertyID = Shader.PropertyToID("_FillColor");

	private static bool IsHurt => Npc.CurrentFieldID.Equals(SectorName.MucoraFortress, 25);

	private static bool IsDying => Npc.CurrentFieldID.Equals(SectorName.Earthford, 28);

	private static string TalkStartAnimation
	{
		get
		{
			if (!IsHurt)
			{
				if (!IsDying)
				{
					return "look";
				}
				return "scene/farewell_see_tot_start";
			}
			return "scene/talk_start";
		}
	}

	private static string TalkLoopAnimation
	{
		get
		{
			if (!IsHurt)
			{
				if (!IsDying)
				{
					return "look_loop";
				}
				return "scene/farewell_see_tot_loop";
			}
			return "scene/talk_loop";
		}
	}

	private static string IdleStartAnimation
	{
		get
		{
			if (!IsHurt)
			{
				return "idle_connect";
			}
			return "scene/talk_end";
		}
	}

	private static string IdleLoopAnimation
	{
		get
		{
			if (!IsHurt)
			{
				return "idle";
			}
			return "scene/down_loop";
		}
	}

	public override void AwakeObject(SaveData saveData)
	{
		base.AwakeObject(saveData);
		col = GetComponentInChildren<Collider2D>();
		if (IsHurt)
		{
			SetAnimation("scene/down_loop");
		}
		if (IsDying)
		{
			if (Npc.TalkData.GetTalkCount("Earthford_28_Cor") > 0)
			{
				SetAnimation("scene/farewell_death_loop");
				Die();
				Darken();
			}
			else
			{
				SetAnimation("DyingIdleAnimation");
				StartChasingSight(startChasing: false);
			}
		}
	}

	protected override void EnterTalkAnimation()
	{
		if (!(base.SkeletonAnimation.AnimationName == TalkLoopAnimation))
		{
			SetAnimation(TalkStartAnimation, loop: false);
			AddAnimation(TalkLoopAnimation);
		}
	}

	protected override void EnterIdleAnimation()
	{
		if (IsDying)
		{
			SetAnimation("scene/farewell_death_loop");
			return;
		}
		SetAnimation(IdleStartAnimation, loop: false);
		AddAnimation(IdleLoopAnimation);
	}

	protected override void OnFinishTalk()
	{
		base.OnFinishTalk();
		if (IsHurt)
		{
			col.enabled = false;
		}
		if (IsDying)
		{
			Die();
		}
	}

	protected override async UniTask WaitBeforeTalk()
	{
		if (IsDying)
		{
			await UniTask.WaitUntil(() => base.SkeletonAnimation.AnimationName == "scene/farewell_see_tot_loop");
		}
		else
		{
			await base.WaitBeforeTalk();
		}
	}

	protected override void AddInteractCount(FieldID fieldID)
	{
		if (!IsHurt)
		{
			base.AddInteractCount(fieldID);
		}
	}

	private void Die()
	{
		base.ShouldChaseSight = false;
		col.enabled = false;
	}

	private void Darken()
	{
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		MeshRenderer.GetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetFloat(FillPhasePropertyID, 0.9f);
		materialPropertyBlock.SetColor(FillColorPropertyID, Color.black);
		MeshRenderer.SetPropertyBlock(materialPropertyBlock);
	}
}
