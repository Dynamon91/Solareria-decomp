using System;

namespace Library;

public class LazySingleton<T>
{
	private static readonly Lazy<T> _instance = new Lazy<T>();

	public static T Instance => _instance.Value;
}
