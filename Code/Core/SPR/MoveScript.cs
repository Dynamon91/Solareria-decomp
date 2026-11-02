using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Library;
using SPR.UI.Talk;
using UnityEngine;

namespace SPR;

public class MoveScript : Script
{
	private readonly string targetName;

	private readonly Vector2 endPosition;

	private readonly bool wait;

	private readonly Direction endDirection;

	public override bool IsInstant => !wait;

	public MoveScript(IReadOnlyList<string> values)
	{
		targetName = values[0];
		wait = values[1] == "wait";
		endPosition = values[Script.ScenarioKOIndex].ParseToVector2();
		if (!Enum.TryParse<Direction>(values[Script.ScenarioENIndex], out endDirection))
		{
			endDirection = Direction.Right;
		}
	}

	public override void Run(TalkCanvas talkCanvas)
	{
		if (targetName == "Tot")
		{
			MovePlayerAsync(talkCanvas).Forget();
		}
		else
		{
			MoveCharacterAsync(talkCanvas).Forget();
		}
	}

	public override void Skip(TalkCanvas talkCanvas)
	{
		if (targetName == "Tot")
		{
			Game.Instance.Player.SetPosition(endPosition, endDirection == Direction.Right);
		}
		else
		{
			Npc npc = Script.FieldController.FindNpc(targetName);
			if ((object)npc != null)
			{
				npc.transform.position = endPosition;
			}
		}
		talkCanvas.RunNextScript_Skip();
	}

	private async UniTask MoveCharacterAsync(TalkCanvas talkCanvas)
	{
		if (!(targetName == "Tot"))
		{
			await MoveNPCAsync(talkCanvas);
		}
		else
		{
			await MovePlayerAsync(talkCanvas);
		}
		talkCanvas.RunNextScript();
	}

	private async UniTask MovePlayerAsync(TalkCanvas talkCanvas)
	{
		if (wait)
		{
			talkCanvas.IsTalking = false;
			await MoveTot();
		}
		else
		{
			MoveTot().Forget();
		}
		talkCanvas.RunNextScript();
		async UniTask MoveTot()
		{
			PlayerController player = Game.Instance.Player;
			await player.MoveXPositionAsync(endPosition.x, isRunning: true);
			player.mover.Flip(endDirection);
			for (int i = 0; i < 5; i++)
			{
				await UniTask.Yield();
			}
		}
	}

	private async UniTask MoveNPCAsync(TalkCanvas talkCanvas)
	{
		Npc npc = Script.FieldController.FindNpc(targetName);
		if ((object)npc != null)
		{
			if (wait)
			{
				talkCanvas.IsTalking = false;
				await npc.MoveAsync(endPosition);
			}
			else
			{
				npc.MoveAsync(endPosition).Forget();
			}
		}
	}
}
