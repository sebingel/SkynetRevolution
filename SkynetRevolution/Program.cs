using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

internal class Player
{
    private readonly IInputManager inputManager;

    public Player(IInputManager inputManager)
    {
        this.inputManager = inputManager;
    }

    private static void Main()
    {
        IInput input = new ConsoleInput();
        //IInput input =
        //    new TestInput(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
        //                  @"\..\..\..\Testcases\ep2_tc6.txt");
        IInputManager inputManager = new InputManager(input);

        new Player(inputManager).Start();
    }

    private void Start()
    {
        // Initialize Map
        Map map = inputManager.CreateMapFromInitialInput();

        // Create SkynetAgent
        SkynetAgent agent = new SkynetAgent();

        // game loop
        while (true)
        {
            inputManager.UpdateAgentFromRoundInput(agent, map);

            // if the agent sits on a node adjacent to an exit-node we cut the link between these two nodes
            if (agent.Position.Links.Any(l => l.Nodes.Any(n => n.Exit)))
            {
                SeverLink(agent.Position.Links.Find(l => l.Nodes.Any(n => n.Exit)));
                continue;
            }

            // calculate distances from agent
            BreadthFirstSearch bfs = new BreadthFirstSearch();
            bfs.CalculateDistances(agent.Position);

            // get all nodes with an exitnode as neighbor
            List<NodeDistance> exitNeighbors =
                bfs.NodeDistances.Where(nd => nd.Node.Neighbors.Any(n => n.Exit)).ToList();

            // find the max count of exit-neighbors to a node
            int maxExitNeighbors = bfs.NodeDistances.Max(nd => nd.Node.Neighbors.Count(n => n.Exit));

            if (maxExitNeighbors > 1)
            {
                // if there are nodes with more than one exit-neighbor
                // find exitNeighbors with more than one adjacent exit-node
                List<NodeDistance> multiExitNodes =
                    bfs.NodeDistances.Where(nd => nd.Node.Neighbors.Count(n => n.Exit) > 1).ToList();

                // if any of these multiExitNodes has a distance <= count of exit nodes we cut a link there
                NodeDistance urgentMultiExitNode =
                    multiExitNodes.FirstOrDefault(men => men.Distance <= men.Node.Neighbors.Count(n => n.Exit));
                if (urgentMultiExitNode != null)
                {
                    SeverLink(urgentMultiExitNode.Node.Links.First(x => x.Nodes.Any(y => y.Exit)));
                    continue;
                }

                // we get the nodes with the most exit-neighbors in its path to it
                IEnumerable<NodeDistance> maxExitNeighborNodes =
                    multiExitNodes.Where(nd => nd.Node.Neighbors.Count(n => n.Exit) == maxExitNeighbors);

                // we get the one with the most urgent ratio of exit-neighbors to normal nodes on its path
                Dictionary<NodeDistance, Tuple<int, int>> exitNeighborCountDic =
                    new Dictionary<NodeDistance, Tuple<int, int>>();
                foreach (NodeDistance node in maxExitNeighborNodes)
                {
                    exitNeighborCountDic.Add(node,
                        Tuple.Create(node.NodesOnPath.Count(nodes => nodes.Links.Any(l => l.Nodes.Any(n => n.Exit))),
                            node.NodesOnPath.Count(nodes => nodes.Links.All(l => l.Nodes.All(n => !n.Exit)))));
                }

                NodeDistance ndWithUrgentRatio =
                    exitNeighborCountDic.OrderByDescending(x => x.Value.Item1 - x.Value.Item2).First().Key;
                Link link = ndWithUrgentRatio.Node.Links.Find(l => l.Nodes.Any(n => n.Exit));
                SeverLink(link);

                continue;
            }
            else
            {
                // if we only have nodes with exactly one exit-neighbor we find the nearest
                // list is ordered so the first has min distance
                NodeDistance nearestExitNeighbor = exitNeighbors[0];
                SeverLink(nearestExitNeighbor.Node.Links.Find(l => l.Nodes.Any(n => n.Exit)));
                continue;
            }
        }
    }

    private void SeverLink(Link l)
    {
        foreach (Node node in l.Nodes)
        {
            node.Neighbors.Remove(l.Nodes.First(n => n.Id != node.Id));

            node.Links.Remove(l);
        }

        Console.WriteLine(l.ToString());
    }
}

public interface IInputManager
{
    Map CreateMapFromInitialInput();
    void UpdateAgentFromRoundInput(SkynetAgent agent, Map map);
}

public class InputManager : IInputManager
{
    private readonly IInput input;

    public InputManager(IInput input)
    {
        this.input = input;
    }

    public Map CreateMapFromInitialInput()
    {
        Map map = new Map();

        string[] inputs = input.GetInput().Split(' ');
        int nodeCount = int.Parse(inputs[0]); // the total number of nodes in the level, including the gateways
        int linkCount = int.Parse(inputs[1]); // the number of links
        int exitCounts = int.Parse(inputs[2]); // the number of exit gateways

        // Add nodes to map
        map.AddNodes(nodeCount);

        for (int i = 0; i < linkCount; i++)
        {
            inputs = input.GetInput().Split(' ');
            int n1 = int.Parse(inputs[0]); // N1 and N2 defines a link between these nodes
            int n2 = int.Parse(inputs[1]);

            Link link = new Link(map[n1], map[n2]);
            map[n1].Links.Add(link);
            map[n2].Links.Add(link);
            map[n1].Neighbors.Add(map[n2]);
            map[n2].Neighbors.Add(map[n1]);
        }

        for (int i = 0; i < exitCounts; i++)
        {
            int exitNode = int.Parse(input.GetInput()); // the index of a gateway node

            map.Nodes[exitNode].Exit = true;
        }

        return map;
    }

    public void UpdateAgentFromRoundInput(SkynetAgent agent, Map map)
    {
        // The index of the node on which the Skynet agent is positioned this turn
        int agentPosition = int.Parse(input.GetInput());

        agent.Position = map[agentPosition];
    }
}

public interface IInput
{
    string GetInput();
}

public class ConsoleInput : IInput
{
    #region Implementation of IInput

    public string GetInput()
    {
        string line = Console.ReadLine();
        Console.Error.WriteLine(line);
        return line;
    }

    #endregion
}

public class Node
{
    public int Id { get; }

    public List<Link> Links { get; }

    public List<Node> Neighbors { get; }

    public bool Exit { get; set; }

    public Node(int id)
    {
        Id = id;
        Links = new List<Link>();
        Neighbors = new List<Node>();
    }

    public override string ToString()
    {
        return Id.ToString();
    }
}

public class Link
{
    public ReadOnlyCollection<Node> Nodes { get; }

    public Link(Node n1, Node n2)
    {
        Nodes = new ReadOnlyCollection<Node>(new List<Node> { n1, n2 });
    }

    public override string ToString()
    {
        return $"{Nodes[0]} {Nodes[1]}";
    }
}

public class Map
{
    public List<Node> Nodes { get; }

    public Node this[int n] => Nodes[n];

    public Map()
    {
        Nodes = new List<Node>();
    }

    public void AddNodes(int n)
    {
        for (int i = 0; i < n; i++)
            Nodes.Add(new Node(i));
    }
}

public class SkynetAgent
{
    public Node Position { get; set; }
}

public class NodeDistance
{
    public Node Node { get; }

    public int Distance { get; set; }

    public List<Node> NodesOnPath { get; set; }

    public NodeDistance(Node node)
    {
        NodesOnPath = new List<Node>();
        Node = node;
    }
}

public class BreadthFirstSearch
{
    private List<NodeDistance> nodeDistances;

    private class BfsNodeDistance : NodeDistance
    {
        public Node PredecessorNode { get; set; }

        public BfsNodeDistance(Node node) : base(node)
        {}
    }

    public IEnumerable<NodeDistance> NodeDistances => nodeDistances.OrderBy(nd => nd.Distance);

    public void CalculateDistances(Node start)
    {
        nodeDistances = new List<NodeDistance>();

        Queue<BfsNodeDistance> queue = new Queue<BfsNodeDistance>();
        HashSet<Node> visitedNodes = new HashSet<Node>();

        queue.Enqueue(new BfsNodeDistance(start) { PredecessorNode = null, Distance = 0 });

        while (queue.Any())
        {
            BfsNodeDistance bfsNode = queue.Dequeue();
            visitedNodes.Add(bfsNode.Node);

            foreach (Node neighbor in bfsNode.Node.Neighbors)
            {
                if (!visitedNodes.Contains(neighbor) &&
                    queue.All(nq => nq.Node != neighbor) &&
                    !neighbor.Exit)
                {
                    BfsNodeDistance bfsNodeDistance = new BfsNodeDistance(neighbor)
                    {
                        PredecessorNode = bfsNode.Node,
                        Distance = bfsNode.Distance + 1
                    };
                    bfsNodeDistance.NodesOnPath.AddRange(bfsNode.NodesOnPath);
                    bfsNodeDistance.NodesOnPath.Add(bfsNode.Node);

                    queue.Enqueue(bfsNodeDistance);
                }
            }

            nodeDistances.Add(new NodeDistance(bfsNode.Node)
            {
                Distance = bfsNode.Distance,
                NodesOnPath = bfsNode.NodesOnPath
            });
        }
    }

    public IEnumerable<Tuple<Link, int>> Search(Node start)
    {
        Queue<Tuple<Node, Node, int>> queue = new Queue<Tuple<Node, Node, int>>();
        HashSet<Node> visitedNodes = new HashSet<Node>();

        queue.Enqueue(Tuple.Create<Node, Node, int>(start, null, 0));

        while (queue.Any())
        {
            Tuple<Node, Node, int> n = queue.Dequeue();

            visitedNodes.Add(n.Item1);

            if (n.Item1.Exit)
            {
                foreach (Link l in n.Item1.Links)
                    yield return Tuple.Create(l, n.Item3);

                //yield return
                //    Tuple.Create(
                //        n.Item2.Links.Find(link => link.Nodes.Contains(n.Item1) && link.Nodes.Contains(n.Item2)),
                //        n.Item3);
            }

            foreach (Node node in n.Item1.Links.SelectMany(link => link.Nodes))
            {
                if (!visitedNodes.Contains(node) &&
                    queue.All(tup => tup.Item1 != node))
                    queue.Enqueue(Tuple.Create(node, n.Item1, n.Item3 + 1));
            }
        }
    }
}