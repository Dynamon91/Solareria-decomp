using System;

namespace Library;

public struct SubscribeInfo
{
	public IEventListener Subscriber { get; }

	public Type EventType { get; }

	public SubscribeInfo(Type eventType, IEventListener subscriber)
	{
		EventType = eventType;
		Subscriber = subscriber;
	}
}
