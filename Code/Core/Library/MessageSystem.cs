using System.Collections.Generic;
using System.Linq;

namespace Library;

public class MessageSystem : LazySingleton<MessageSystem>
{
	private readonly List<SubscribeInfo> _infos = new List<SubscribeInfo>();

	public void Subscribe<T>(IEventListener listener) where T : Event
	{
		_infos.Add(new SubscribeInfo(typeof(T), listener));
	}

	public void SubscribeUnique<T>(IEventListener listener) where T : Event
	{
		UnSubscribe<T>(listener);
		Subscribe<T>(listener);
	}

	public void UnSubscribe<T>(IEventListener listener) where T : Event
	{
		foreach (SubscribeInfo item in _infos.Where((SubscribeInfo info) => info.EventType == typeof(T) && info.Subscriber == listener).ToList())
		{
			_infos.Remove(item);
		}
	}

	public void Publish(Event e)
	{
		foreach (SubscribeInfo item in _infos.Where((SubscribeInfo info) => info.EventType == e.GetType()).ToList())
		{
			item.Subscriber.OnEvent(e);
		}
	}

	public void Send(IEventListener target, Event e)
	{
		target.OnEvent(e);
	}
}
