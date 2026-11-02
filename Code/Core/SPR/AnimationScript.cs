using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.UI.Talk;

namespace SPR;

public class AnimationScript : Script
{
	private enum Order
	{
		None,
		Loop,
		Wait,
		Add
	}

	private readonly string targetName;

	private readonly Order order;

	private readonly string animationName;

	public override bool IsInstant => order != Order.Wait;

	public AnimationScript(IReadOnlyList<string> values)
	{
		targetName = values[0];
		animationName = values[Script.ScenarioKOIndex];
		order = values[1].ToLowerInvariant() switch
		{
			"loop" => Order.Loop, 
			"wait" => Order.Wait, 
			"add" => Order.Add, 
			_ => Order.None, 
		};
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		PlayAnimationAsync(talkCanvas).Forget();
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		SpineObject spineObject = ((!(targetName == "Tot")) ? ((SpineObject)Script.FieldController.FindNpc(targetName)) : ((SpineObject)Game.Instance.Player.mover));
		if (spineObject.IsNull())
		{
			DebugLog.ScenarioLogError(targetName + " 캐릭터를 찾을 수 없습니다.");
			return;
		}
		switch (order)
		{
		case Order.None:
		case Order.Wait:
			spineObject.SetAnimation(animationName, loop: false);
			break;
		case Order.Loop:
		case Order.Add:
			spineObject.SetAnimation(animationName);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		talkCanvas.RunNextScript_Skip();
	}

	private async UniTask PlayAnimationAsync(TalkCanvas talkCanvas)
	{
		SpineObject spineObject = ((!(targetName == "Tot")) ? ((SpineObject)Script.FieldController.FindNpc(targetName)) : ((SpineObject)Game.Instance.Player.mover));
		if (spineObject.IsNull())
		{
			DebugLog.ScenarioLogError(targetName + " 캐릭터를 찾을 수 없습니다.");
			return;
		}
		switch (order)
		{
		case Order.None:
			talkCanvas.IsTalking = true;
			spineObject.SetAnimation(animationName, loop: false);
			break;
		case Order.Loop:
			talkCanvas.IsTalking = true;
			spineObject.SetAnimation(animationName);
			break;
		case Order.Wait:
			talkCanvas.IsTalking = false;
			await spineObject.SetAnimationAsync(animationName);
			break;
		case Order.Add:
			talkCanvas.IsTalking = true;
			spineObject.AddAnimation(animationName);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		talkCanvas.RunNextScript();
	}
}
