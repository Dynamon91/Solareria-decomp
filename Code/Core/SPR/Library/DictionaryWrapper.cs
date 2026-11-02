using System;
using System.Collections.Generic;
using UnityEngine;

namespace SPR.Library;

[Serializable]
public class DictionaryWrapper<TKey, TValue>
{
	[Serializable]
	public class KeyValueSet
	{
		[SerializeField]
		private TKey key;

		[SerializeField]
		private TValue value;

		public TKey Key => key;

		public TValue Value
		{
			get
			{
				return value;
			}
			set
			{
				this.value = value;
			}
		}

		public KeyValueSet(TKey key, TValue value)
		{
			this.key = key;
			this.value = value;
		}
	}

	[SerializeField]
	protected List<KeyValueSet> dict = new List<KeyValueSet>();

	public IReadOnlyCollection<KeyValueSet> KeyValuePairs => dict;

	public TValue this[TKey key]
	{
		get
		{
			return GetKeyValueSet(key).Value;
		}
		set
		{
			GetKeyValueSet(key).Value = value;
		}
	}

	private KeyValueSet GetKeyValueSet(TKey key)
	{
		return dict.Find((KeyValueSet d) => object.Equals(d.Key, key));
	}

	public bool ContainsKey(TKey key)
	{
		return dict.Exists((KeyValueSet d) => object.Equals(d.Key, key));
	}

	public virtual void AddKey(TKey key)
	{
		dict.Add(new KeyValueSet(key, default(TValue)));
	}

	public virtual void RemoveKey(TKey key)
	{
		if (ContainsKey(key))
		{
			dict.Remove(GetKeyValueSet(key));
		}
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		value = default(TValue);
		if (ContainsKey(key))
		{
			value = this[key];
		}
		if (value != null)
		{
			return !value.Equals(null);
		}
		return false;
	}
}
