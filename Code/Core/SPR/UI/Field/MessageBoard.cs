using System.Collections.Generic;
using System.Linq;
using SPR.L10N;
using SPR.UI.Memory;
using SPR.UI.Menu;
using UnityEngine;

namespace SPR.UI.Field;

public class MessageBoard : MonoBehaviour
{
	private class Message
	{
		public MessageType Type;

		public string Id;

		public MenuCategory GetTargetMenu()
		{
			return Type switch
			{
				MessageType.Character => MenuCategory.Memory, 
				MessageType.Boss => MenuCategory.Memory, 
				MessageType.Map => MenuCategory.Map, 
				_ => MenuCategory.System, 
			};
		}
	}

	[SerializeField]
	private LocaleText messageText;

	[SerializeField]
	private LocaleText shortCutText;

	private Animator animator;

	private readonly Queue<Message> messages = new Queue<Message>();

	private Message currentMessage;

	private static readonly int IdActivate = Animator.StringToHash("Activate");

	private const float DurationFrames = 160f;

	public bool IsPlaying => currentMessage != null;

	public MenuCategory TargetMenu => currentMessage.GetTargetMenu();

	public void Initialize()
	{
		if ((object)animator == null)
		{
			animator = GetComponent<Animator>();
		}
	}

	private static bool CanShow()
	{
		if (Game.Instance.InputManager.IsCurrentControlTarget(Game.Instance.Player) && !Game.Instance.Player.mover.PSM.CurrentState.Equals(Game.Instance.Player.mover.PSM.DieState))
		{
			return !IsCutsceneProducing();
		}
		return false;
	}

	private static bool IsCutsceneProducing()
	{
		IReadOnlyList<CutsceneProducer> cutsceneProducers = Game.Instance.FieldController.GetCutsceneProducers();
		if (cutsceneProducers.Count > 0)
		{
			return cutsceneProducers.Any((CutsceneProducer p) => p.IsProducing);
		}
		return false;
	}

	private void Update()
	{
		if (!IsPlaying)
		{
			if (CanShow() && messages.Count > 0)
			{
				currentMessage = messages.Dequeue();
				SetData(currentMessage.Type, currentMessage.Id);
				animator.SetTrigger(IdActivate);
				Game.Instance.SoundManager.PlayOneShot("event:/SFX/UI/Items/SFX_UI_Message_Appear");
			}
		}
		else if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 159f / 160f)
		{
			animator.Play(0, -1, 1f);
			currentMessage = null;
		}
	}

	public void Activate(ActorName actorName)
	{
		Activate(MessageType.Character, actorName.ToString());
	}

	public void Activate(BossName bossName)
	{
		Activate(MessageType.Boss, bossName.ToString());
	}

	public void Activate(MessageType type, string id = null)
	{
		messages.Enqueue(new Message
		{
			Type = type,
			Id = id
		});
	}

	private void SetData(MessageType type, string id)
	{
		string text = $"Message_Format_{type}";
		switch (type)
		{
		case MessageType.Character:
			messageText.SetDataFormat(text, Locale.GetLocaleString("Name_" + id));
			break;
		case MessageType.Boss:
			messageText.SetDataFormat(text, Locale.GetLocaleString("Boss_" + id));
			break;
		case MessageType.Map:
			messageText.SetData(text);
			break;
		default:
			DebugLog.GeneralLogError($"주어진 타입(MessageType.{type})을 텍스트로 변환할 수 없습니다!");
			messageText.SetData(text);
			break;
		}
		shortCutText.SetData("Message_Shortcut");
	}

	public bool TryGetMemoryData(out MemoryCategory category, out string id)
	{
		Message message = currentMessage;
		if (message != null)
		{
			switch (message.Type)
			{
			case MessageType.Character:
				category = MemoryCategory.Character;
				id = currentMessage.Id;
				return true;
			case MessageType.Boss:
				category = MemoryCategory.Boss;
				id = currentMessage.Id;
				return true;
			}
		}
		category = MemoryCategory.Character;
		id = null;
		return false;
	}
}
