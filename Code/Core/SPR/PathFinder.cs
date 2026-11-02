using System;
using System.Collections.Generic;
using System.Linq;
using Library;
using Priority_Queue;
using SPR.Library;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SPR;

public class PathFinder
{
	public class Node : FastPriorityQueueNode
	{
		public readonly Vector2 Position;

		private readonly List<Node> connectedList = new List<Node>();

		public Node Prev { get; set; }

		public float GCost { get; set; }

		public float HCost { get; set; }

		public float FCost => GCost + HCost;

		public LayerMask LayerMask { get; private set; }

		public IReadOnlyList<Node> ConnectedList => connectedList;

		public Node(Vector2 position)
		{
			Position = position;
		}

		public Node(Vector2 position, LayerMask layerMask)
			: this(position)
		{
			LayerMask = layerMask;
		}

		public void AddConnectedNode(Node node)
		{
			if (!(Position == node.Position) && connectedList.Find((Node n) => n.Position == node.Position) == null)
			{
				connectedList.Add(node);
			}
		}

		public void RemoveConnectedNode(Node node)
		{
			connectedList.Remove(node);
		}

		public Node Shift(int sign, int count, bool allowNull = false)
		{
			Node node = this;
			for (int i = 0; i <= count; i++)
			{
				Node node2 = node;
				foreach (Node connected in node.connectedList)
				{
					if ((sign < 0 && connected.Position.x < node.Position.x) || (sign > 0 && connected.Position.x > node.Position.x))
					{
						node = connected;
						break;
					}
				}
				if (node == node2)
				{
					if (!allowNull)
					{
						return node2;
					}
					return null;
				}
			}
			return node;
		}

		public Node Shift(int sign, int count, Vector2 target, bool allowNull = false)
		{
			Node node = this;
			float num = -1f;
			for (int i = 0; i <= count; i++)
			{
				Node node2 = node;
				Node node3 = node;
				Vector2 normalized = (target - node.Position).normalized;
				foreach (Node connected in node.connectedList)
				{
					Vector2 normalized2 = (connected.Position - node.Position).normalized;
					float num2 = Vector2.Dot(normalized, normalized2);
					if (num2 > num && ((sign < 0 && connected.Position.x < node.Position.x) || (sign > 0 && connected.Position.x > node.Position.x)))
					{
						num = num2;
						node3 = connected;
					}
				}
				if (i < count)
				{
					node = node3;
				}
				if (node == node2)
				{
					if (!allowNull)
					{
						return node2;
					}
					return null;
				}
				num = -1f;
			}
			return node;
		}

		public Node Shift(int sign, int count, Node limit)
		{
			Node node = this;
			for (int i = 0; i <= count; i++)
			{
				Node node2 = node;
				foreach (Node connected in node.connectedList)
				{
					if ((sign < 0 && connected.Position.x < node.Position.x) || (sign > 0 && connected.Position.x > node.Position.x))
					{
						node = connected;
						break;
					}
				}
				if (node == limit)
				{
					return node;
				}
				if (node == node2)
				{
					return node2;
				}
			}
			return node;
		}

		public Node ShiftEnd(int sign)
		{
			Node node = this;
			Node node2;
			do
			{
				node2 = node;
				foreach (Node connected in node.connectedList)
				{
					if ((sign < 0 && connected.Position.x < node.Position.x) || (sign > 0 && connected.Position.x > node.Position.x))
					{
						node = connected;
						break;
					}
				}
			}
			while (node != node2);
			return node2;
		}

		public Node ShiftEnd(int sign, LayerMask layerMask)
		{
			Node node = this;
			Node node2;
			do
			{
				node2 = node;
				foreach (Node connected in node.connectedList)
				{
					if ((sign < 0 && connected.Position.x < node.Position.x) || (sign > 0 && connected.Position.x > node.Position.x))
					{
						node = connected;
						if ((int)connected.LayerMask == (int)layerMask)
						{
							return node;
						}
						break;
					}
				}
			}
			while (node != node2);
			return node2;
		}
	}

	private readonly List<Vector2> ignoreList1 = new List<Vector2>
	{
		new Vector2(-20.5f, -11.25f),
		new Vector2(-19.5f, -10.75f)
	};

	private readonly List<Vector2> ignoreList2 = new List<Vector2>
	{
		new Vector2(-20.5f, -12f),
		new Vector2(-19.5f, -12f)
	};

	private const float Weight = 10f;

	public static readonly List<HashSet<Node>> ConnectedNodeList = new List<HashSet<Node>>();

	private readonly Tilemap tilemap;

	private static readonly HashSet<Node> ClosedNodes = new HashSet<Node>();

	private static FastPriorityQueue<Node> _priorityQueue;

	private static readonly Dictionary<Type, FastPriorityQueue<Node>> PriorityQueueCache = new Dictionary<Type, FastPriorityQueue<Node>>();

	public static List<Node> NodeList { get; private set; } = new List<Node>();

	public PathFinder(Tilemap tilemap)
	{
		this.tilemap = tilemap;
		PriorityQueueCache.Clear();
	}

	public void BakeGraph(Tilemap tilemap)
	{
		NodeList.Clear();
		BoundsInt cellBounds = tilemap.cellBounds;
		for (int i = cellBounds.xMin; i < cellBounds.xMax; i++)
		{
			for (int j = cellBounds.yMin; j < cellBounds.yMax + 1; j++)
			{
				MakeNode(i, j, LayerUtility.OneWayPlatformLayer);
			}
		}
		_priorityQueue = new FastPriorityQueue<Node>(NodeList.Count);
		foreach (Node node in NodeList)
		{
			ConnectAroundNode(node);
		}
	}

	public void BakeGraph(int xMin, int xMax, int yMin, int yMax)
	{
		NodeList.Clear();
		for (int i = xMin; i < xMax; i++)
		{
			for (int j = yMin; j < yMax + 1; j++)
			{
				MakeNode(i, j, LayerUtility.OneWayPlatformLayer);
			}
		}
		_priorityQueue = new FastPriorityQueue<Node>(NodeList.Count);
		foreach (Node node in NodeList)
		{
			ConnectAroundNode(node);
		}
	}

	public List<Node> BakeAirGraph(Type type)
	{
		BoundsInt cellBounds = tilemap.cellBounds;
		Dictionary<Vector2, Node> dictionary = new Dictionary<Vector2, Node>();
		new List<Node>();
		for (int i = cellBounds.xMin; i < cellBounds.xMax; i++)
		{
			for (int j = cellBounds.yMin; j < cellBounds.yMax + 1; j++)
			{
				if (!HasTile(new Vector3Int(i, j, 0)))
				{
					Node value = new Node(new Vector2(i, j));
					dictionary.Add(new Vector2(i, j), value);
				}
			}
		}
		PriorityQueueCache.Add(type, new FastPriorityQueue<Node>(dictionary.Count));
		foreach (var (_, node2) in dictionary)
		{
			ConnectAroundNode(dictionary, node2);
		}
		return dictionary.Values.ToList();
	}

	public static Stack<Node> FindPath(Vector2 start, Vector2 end, Type type = null)
	{
		ClosedNodes.Clear();
		FastPriorityQueue<Node> fastPriorityQueue = (((object)type == null) ? _priorityQueue : PriorityQueueCache[type]);
		Node node = FindNode(start);
		Node node2 = FindNode(end);
		ResetGCost();
		node.GCost = 0f;
		node.HCost = 0f;
		node.Prev = null;
		fastPriorityQueue.Enqueue(node, 0f);
		while (fastPriorityQueue.Count > 0)
		{
			Node node3 = fastPriorityQueue.Dequeue();
			if (node3 == node2)
			{
				fastPriorityQueue.Clear();
				return GetPath(node, node3);
			}
			if (ClosedNodes.Contains(node3))
			{
				continue;
			}
			foreach (Node connected in node3.ConnectedList)
			{
				if (ClosedNodes.Contains(connected) || fastPriorityQueue.Contains(connected))
				{
					continue;
				}
				connected.Prev = node3;
				if (Vector2.Distance(node3.Position, connected.Position) > 1f)
				{
					connected.GCost = node3.GCost + (float)Mathf.FloorToInt(Mathf.Sqrt(Mathf.Pow(10f, 2f) + Mathf.Pow(10f, 2f)));
				}
				else if ((float)(connected.Position - node3.Position).normalized.x.GetSign() == 0f)
				{
					connected.GCost = node3.GCost + 20f;
				}
				else
				{
					connected.GCost = node3.GCost + 10f;
				}
				connected.HCost = Heuristic(connected.Position, node2.Position);
				if (!ClosedNodes.Contains(connected))
				{
					if (fastPriorityQueue.Contains(connected))
					{
						fastPriorityQueue.UpdatePriority(connected, connected.FCost);
					}
					else
					{
						fastPriorityQueue.Enqueue(connected, connected.FCost);
					}
				}
			}
			ClosedNodes.Add(node3);
		}
		fastPriorityQueue.Clear();
		return new Stack<Node>();
	}

	public static Stack<Node> FindPath(List<Node> graph, Vector2 start, Vector2 end, Type type = null)
	{
		ClosedNodes.Clear();
		FastPriorityQueue<Node> fastPriorityQueue = (((object)type == null) ? _priorityQueue : PriorityQueueCache[type]);
		Node node = FindNode(graph, start);
		Node node2 = FindNode(graph, end);
		ResetGCost();
		node.GCost = 0f;
		node.HCost = 0f;
		node.Prev = null;
		fastPriorityQueue.Enqueue(node, 0f);
		while (fastPriorityQueue.Count > 0)
		{
			Node node3 = fastPriorityQueue.Dequeue();
			if (node3 == node2)
			{
				fastPriorityQueue.Clear();
				return GetPath(node, node3);
			}
			if (ClosedNodes.Contains(node3))
			{
				continue;
			}
			foreach (Node connected in node3.ConnectedList)
			{
				if (ClosedNodes.Contains(connected) || fastPriorityQueue.Contains(connected))
				{
					continue;
				}
				connected.Prev = node3;
				if (Vector2.Distance(node3.Position, connected.Position) > 1f)
				{
					connected.GCost = node3.GCost + (float)Mathf.FloorToInt(Mathf.Sqrt(Mathf.Pow(10f, 2f) + Mathf.Pow(10f, 2f)));
				}
				else
				{
					connected.GCost = node3.GCost + 10f;
				}
				connected.HCost = Heuristic(connected.Position, node2.Position);
				if (!ClosedNodes.Contains(connected))
				{
					if (fastPriorityQueue.Contains(connected))
					{
						fastPriorityQueue.UpdatePriority(connected, connected.FCost);
					}
					else
					{
						fastPriorityQueue.Enqueue(connected, connected.FCost);
					}
				}
			}
			ClosedNodes.Add(node3);
		}
		fastPriorityQueue.Clear();
		return new Stack<Node>();
	}

	public static Node FindNode(Vector2 position, int sign = 0)
	{
		position.x += sign;
		float num = float.MaxValue;
		float num2 = Mathf.Sqrt(2f);
		Node result = null;
		for (int i = 0; i < NodeList.Count; i++)
		{
			float num3 = Vector2.Distance(NodeList[i].Position, position);
			if (!(num3 > num2) && num > num3)
			{
				num = num3;
				result = NodeList[i];
			}
		}
		return result;
	}

	public static Node FindNode(Vector2 position, float distanceLimit, int sign = 0)
	{
		position.x += sign;
		float num = float.MaxValue;
		Node result = null;
		for (int i = 0; i < NodeList.Count; i++)
		{
			float num2 = Vector2.Distance(NodeList[i].Position, position);
			if ((!(distanceLimit > 0f) || !(num2 > distanceLimit)) && num > num2)
			{
				num = num2;
				result = NodeList[i];
			}
		}
		return result;
	}

	public static Node FindNode(IReadOnlyList<Node> graph, Vector2 position, int sign = 0)
	{
		position.x += sign;
		float num = float.MaxValue;
		float num2 = Mathf.Sqrt(2f);
		Node result = null;
		for (int i = 0; i < graph.Count; i++)
		{
			float num3 = Vector2.Distance(graph[i].Position, position);
			if (!(num3 > num2) && num > num3)
			{
				num = num3;
				result = graph[i];
			}
		}
		return result;
	}

	public void ModifyGraph(List<Vector2> range, LayerMask nonTileLayer, bool setGround)
	{
		if (setGround)
		{
			List<Node> list = new List<Node>();
			foreach (Vector2 item in range)
			{
				int num = Mathf.FloorToInt(item.x);
				if (range.Count % 2 == 0)
				{
					num--;
				}
				list.Add(MakeNode(num, (int)item.y, nonTileLayer));
			}
			{
				foreach (Node item2 in list)
				{
					if (item2 != null)
					{
						ConnectAroundNode(item2);
					}
				}
				return;
			}
		}
		foreach (Vector2 item3 in range)
		{
			Node node = FindNode(item3);
			if (node == null)
			{
				continue;
			}
			foreach (Node connected in node.ConnectedList)
			{
				connected.RemoveConnectedNode(node);
			}
			NodeList.Remove(node);
			foreach (HashSet<Node> item4 in ConnectedNodeList.Where((HashSet<Node> connectedNodes) => connectedNodes.Contains(node)))
			{
				item4.Remove(node);
			}
		}
	}

	public static void UpdateConnectedPath()
	{
		HashSet<Node> closed = new HashSet<Node>();
		int num = 0;
		ConnectedNodeList.Clear();
		for (int i = 0; i < NodeList.Count; i++)
		{
			if (!closed.Contains(NodeList[i]))
			{
				Node node = NodeList[i];
				ConnectedNodeList.Add(new HashSet<Node>());
				HashSet<Node> connectedNodes = ConnectedNodeList[num];
				connectedNodes.Add(node);
				FindConnectedNode(node, ref closed, ref connectedNodes);
				num++;
			}
		}
	}

	private Node MakeNode(int x, int y, LayerMask nonTileLayer)
	{
		Node node = null;
		if (!HasTile(new Vector3Int(x, y, 0)) && HasTile(new Vector3Int(x, y - 1, 0)))
		{
			RaycastHit2D raycastHit2D = Physics2D.Raycast(new Vector2((float)x + 0.5f, y), Vector2.down, 1.5f, LayerUtility.PlatformLayer);
			if (!raycastHit2D)
			{
				return null;
			}
			node = new Node(raycastHit2D.point.SnapToQuarter(), LayerUtility.PlatformLayer);
			NodeList.Add(node);
		}
		else if (!HasTile(new Vector3Int(x, y, 0)))
		{
			RaycastHit2D raycastHit2D2 = Physics2D.Raycast(new Vector2((float)x + 0.5f, (float)y + 0.5f), Vector2.down, 1f, nonTileLayer);
			if (!raycastHit2D2)
			{
				return null;
			}
			node = new Node(raycastHit2D2.point.SnapToQuarter(), nonTileLayer);
			NodeList.Add(node);
		}
		return node;
	}

	private bool HasTile(Vector3Int point)
	{
		return tilemap.HasTile(point);
	}

	private void ConnectAroundNode(Node node)
	{
		float maxDistance = Mathf.Sqrt(2f);
		foreach (Node item in NodeList.Where((Node n) => node != n && Vector2.Distance(node.Position, n.Position) <= maxDistance))
		{
			if (!(SceneManager.GetActiveScene().name == "Earthford_03") || ((!ignoreList1.Contains(node.Position) || !ignoreList2.Contains(item.Position)) && (!ignoreList2.Contains(node.Position) || !ignoreList1.Contains(item.Position))))
			{
				item.AddConnectedNode(node);
				node.AddConnectedNode(item);
			}
		}
	}

	private static void ConnectAroundNode(Dictionary<Vector2, Node> graph, Node node)
	{
		for (float num = node.Position.x - 1f; num <= node.Position.x + 1f; num += 1f)
		{
			for (float num2 = node.Position.y - 1f; num2 <= node.Position.y + 1f; num2 += 1f)
			{
				if (graph.TryGetValue(new Vector2(num, num2), out var value))
				{
					node.AddConnectedNode(value);
					value.AddConnectedNode(node);
				}
			}
		}
	}

	private static void FindConnectedNode(Node target, ref HashSet<Node> closed, ref HashSet<Node> connectedNodes)
	{
		if (closed.Contains(target) || target.ConnectedList.Count <= 0)
		{
			return;
		}
		closed.Add(target);
		foreach (Node connected in target.ConnectedList)
		{
			connectedNodes.Add(connected);
			FindConnectedNode(connected, ref closed, ref connectedNodes);
		}
	}

	private static void ResetGCost()
	{
		for (int i = 0; i < NodeList.Count; i++)
		{
			NodeList[i].GCost = 2.1474836E+09f;
		}
	}

	private static float Heuristic(Vector2 start, Vector2 end)
	{
		return Mathf.Abs(start.x - end.x) + Mathf.Abs(start.y - end.y);
	}

	private static Stack<Node> GetPath(Node startNode, Node endNode)
	{
		Stack<Node> stack = new Stack<Node>();
		Node node = endNode;
		stack.Push(node);
		while (node.Prev != null && node.Prev != startNode)
		{
			stack.Push(node.Prev);
			node = node.Prev;
		}
		return stack;
	}
}
