using System;
using System.Collections;
using System.Collections.Generic;

namespace Priority_Queue;

public class SimplePriorityQueue<TItem, TPriority> : IPriorityQueue<TItem, TPriority>, IEnumerable<TItem>, IEnumerable
{
	private class SimpleNode : GenericPriorityQueueNode<TPriority>
	{
		public TItem Data { get; private set; }

		public SimpleNode(TItem data)
		{
			Data = data;
		}
	}

	private const int INITIAL_QUEUE_SIZE = 10;

	private readonly GenericPriorityQueue<SimpleNode, TPriority> _queue;

	private readonly Dictionary<TItem, IList<SimpleNode>> _itemToNodesCache;

	private readonly IList<SimpleNode> _nullNodesCache;

	public int Count
	{
		get
		{
			lock (_queue)
			{
				return _queue.Count;
			}
		}
	}

	public TItem First
	{
		get
		{
			lock (_queue)
			{
				if (_queue.Count <= 0)
				{
					throw new InvalidOperationException("Cannot call .First on an empty queue");
				}
				return _queue.First.Data;
			}
		}
	}

	public SimplePriorityQueue()
		: this((IComparer<TPriority>)Comparer<TPriority>.Default, (IEqualityComparer<TItem>)EqualityComparer<TItem>.Default)
	{
	}

	public SimplePriorityQueue(IComparer<TPriority> priorityComparer)
		: this((Comparison<TPriority>)priorityComparer.Compare, (IEqualityComparer<TItem>)EqualityComparer<TItem>.Default)
	{
	}

	public SimplePriorityQueue(Comparison<TPriority> priorityComparer)
		: this(priorityComparer, (IEqualityComparer<TItem>)EqualityComparer<TItem>.Default)
	{
	}

	public SimplePriorityQueue(IEqualityComparer<TItem> itemEquality)
		: this((IComparer<TPriority>)Comparer<TPriority>.Default, itemEquality)
	{
	}

	public SimplePriorityQueue(IComparer<TPriority> priorityComparer, IEqualityComparer<TItem> itemEquality)
		: this((Comparison<TPriority>)priorityComparer.Compare, itemEquality)
	{
	}

	public SimplePriorityQueue(Comparison<TPriority> priorityComparer, IEqualityComparer<TItem> itemEquality)
	{
		_queue = new GenericPriorityQueue<SimpleNode, TPriority>(10, priorityComparer);
		_itemToNodesCache = new Dictionary<TItem, IList<SimpleNode>>(itemEquality);
		_nullNodesCache = new List<SimpleNode>();
	}

	private SimpleNode GetExistingNode(TItem item)
	{
		if (item == null)
		{
			if (_nullNodesCache.Count <= 0)
			{
				return null;
			}
			return _nullNodesCache[0];
		}
		if (!_itemToNodesCache.TryGetValue(item, out var value))
		{
			return null;
		}
		return value[0];
	}

	private void AddToNodeCache(SimpleNode node)
	{
		if (node.Data == null)
		{
			_nullNodesCache.Add(node);
			return;
		}
		if (!_itemToNodesCache.TryGetValue(node.Data, out var value))
		{
			value = new List<SimpleNode>();
			_itemToNodesCache[node.Data] = value;
		}
		value.Add(node);
	}

	private void RemoveFromNodeCache(SimpleNode node)
	{
		IList<SimpleNode> value;
		if (node.Data == null)
		{
			_nullNodesCache.Remove(node);
		}
		else if (_itemToNodesCache.TryGetValue(node.Data, out value))
		{
			value.Remove(node);
			if (value.Count == 0)
			{
				_itemToNodesCache.Remove(node.Data);
			}
		}
	}

	public void Clear()
	{
		lock (_queue)
		{
			_queue.Clear();
			_itemToNodesCache.Clear();
			_nullNodesCache.Clear();
		}
	}

	public bool Contains(TItem item)
	{
		lock (_queue)
		{
			return (item == null) ? (_nullNodesCache.Count > 0) : _itemToNodesCache.ContainsKey(item);
		}
	}

	public TItem Dequeue()
	{
		lock (_queue)
		{
			if (_queue.Count <= 0)
			{
				throw new InvalidOperationException("Cannot call Dequeue() on an empty queue");
			}
			SimpleNode simpleNode = _queue.Dequeue();
			RemoveFromNodeCache(simpleNode);
			return simpleNode.Data;
		}
	}

	private SimpleNode EnqueueNoLockOrCache(TItem item, TPriority priority)
	{
		SimpleNode simpleNode = new SimpleNode(item);
		if (_queue.Count == _queue.MaxSize)
		{
			_queue.Resize(_queue.MaxSize * 2 + 1);
		}
		_queue.Enqueue(simpleNode, priority);
		return simpleNode;
	}

	public void Enqueue(TItem item, TPriority priority)
	{
		lock (_queue)
		{
			IList<SimpleNode> value;
			if (item == null)
			{
				value = _nullNodesCache;
			}
			else if (!_itemToNodesCache.TryGetValue(item, out value))
			{
				value = new List<SimpleNode>();
				_itemToNodesCache[item] = value;
			}
			SimpleNode item2 = EnqueueNoLockOrCache(item, priority);
			value.Add(item2);
		}
	}

	public bool EnqueueWithoutDuplicates(TItem item, TPriority priority)
	{
		lock (_queue)
		{
			IList<SimpleNode> list;
			if (item == null)
			{
				if (_nullNodesCache.Count > 0)
				{
					return false;
				}
				list = _nullNodesCache;
			}
			else
			{
				if (_itemToNodesCache.ContainsKey(item))
				{
					return false;
				}
				list = new List<SimpleNode>();
				_itemToNodesCache[item] = list;
			}
			SimpleNode item2 = EnqueueNoLockOrCache(item, priority);
			list.Add(item2);
			return true;
		}
	}

	public void Remove(TItem item)
	{
		lock (_queue)
		{
			SimpleNode simpleNode;
			IList<SimpleNode> value;
			if (item == null)
			{
				if (_nullNodesCache.Count == 0)
				{
					TItem val = item;
					throw new InvalidOperationException("Cannot call Remove() on a node which is not enqueued: " + val);
				}
				simpleNode = _nullNodesCache[0];
				value = _nullNodesCache;
			}
			else
			{
				if (!_itemToNodesCache.TryGetValue(item, out value))
				{
					TItem val = item;
					throw new InvalidOperationException("Cannot call Remove() on a node which is not enqueued: " + val);
				}
				simpleNode = value[0];
				if (value.Count == 1)
				{
					_itemToNodesCache.Remove(item);
				}
			}
			_queue.Remove(simpleNode);
			value.Remove(simpleNode);
		}
	}

	public void UpdatePriority(TItem item, TPriority priority)
	{
		lock (_queue)
		{
			SimpleNode existingNode = GetExistingNode(item);
			if (existingNode == null)
			{
				TItem val = item;
				throw new InvalidOperationException("Cannot call UpdatePriority() on a node which is not enqueued: " + val);
			}
			_queue.UpdatePriority(existingNode, priority);
		}
	}

	public TPriority GetPriority(TItem item)
	{
		lock (_queue)
		{
			SimpleNode existingNode = GetExistingNode(item);
			if (existingNode == null)
			{
				TItem val = item;
				throw new InvalidOperationException("Cannot call GetPriority() on a node which is not enqueued: " + val);
			}
			return existingNode.Priority;
		}
	}

	public bool TryFirst(out TItem first)
	{
		if (_queue.Count > 0)
		{
			lock (_queue)
			{
				if (_queue.Count > 0)
				{
					first = _queue.First.Data;
					return true;
				}
			}
		}
		first = default(TItem);
		return false;
	}

	public bool TryDequeue(out TItem first)
	{
		if (_queue.Count > 0)
		{
			lock (_queue)
			{
				if (_queue.Count > 0)
				{
					SimpleNode simpleNode = _queue.Dequeue();
					first = simpleNode.Data;
					RemoveFromNodeCache(simpleNode);
					return true;
				}
			}
		}
		first = default(TItem);
		return false;
	}

	public bool TryRemove(TItem item)
	{
		lock (_queue)
		{
			SimpleNode simpleNode;
			IList<SimpleNode> value;
			if (item == null)
			{
				if (_nullNodesCache.Count == 0)
				{
					return false;
				}
				simpleNode = _nullNodesCache[0];
				value = _nullNodesCache;
			}
			else
			{
				if (!_itemToNodesCache.TryGetValue(item, out value))
				{
					return false;
				}
				simpleNode = value[0];
				if (value.Count == 1)
				{
					_itemToNodesCache.Remove(item);
				}
			}
			_queue.Remove(simpleNode);
			value.Remove(simpleNode);
			return true;
		}
	}

	public bool TryUpdatePriority(TItem item, TPriority priority)
	{
		lock (_queue)
		{
			SimpleNode existingNode = GetExistingNode(item);
			if (existingNode == null)
			{
				return false;
			}
			_queue.UpdatePriority(existingNode, priority);
			return true;
		}
	}

	public bool TryGetPriority(TItem item, out TPriority priority)
	{
		lock (_queue)
		{
			SimpleNode existingNode = GetExistingNode(item);
			if (existingNode == null)
			{
				priority = default(TPriority);
				return false;
			}
			priority = existingNode.Priority;
			return true;
		}
	}

	public IEnumerator<TItem> GetEnumerator()
	{
		List<TItem> list = new List<TItem>();
		lock (_queue)
		{
			foreach (SimpleNode item in _queue)
			{
				list.Add(item.Data);
			}
		}
		return list.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public bool IsValidQueue()
	{
		lock (_queue)
		{
			foreach (IList<SimpleNode> value in _itemToNodesCache.Values)
			{
				foreach (SimpleNode item in value)
				{
					if (!_queue.Contains(item))
					{
						return false;
					}
				}
			}
			foreach (SimpleNode item2 in _queue)
			{
				if (GetExistingNode(item2.Data) == null)
				{
					return false;
				}
			}
			return _queue.IsValidQueue();
		}
	}
}
