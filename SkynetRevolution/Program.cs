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
        //                  @"\..\..\..\Testcases\ep2_tc4.txt");
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

            // emergency cut. if the agent sits on a link with only one step to an an exit node we want to cut that link
            Link linkToCut = agent.Position.Links.Find(link => link.Nodes.Any(node => node.Exit));

            if (linkToCut == null)
            {
                // calculate distances from agent
                BreadthFirstSearch bfs = new BreadthFirstSearch();
                bfs.CalculateDistances(agent.Position);

                // get all nodes with an exitnode as neighbor
                List<NodeDistance> exitNeighbors =
                    bfs.NodeDistances.Where(nd => nd.Node.Neighbors.Any(n => n.Exit)).ToList();
                // list is ordered so the first has min distance
                NodeDistance nearestExitNeighbor = exitNeighbors[0];

                // find out if there are more than one node with an exit neighbor on the minimum level
                List<NodeDistance> exitNeighborsWithMinDist =
                    exitNeighbors.FindAll(distance => distance.Distance == nearestExitNeighbor.Distance);
                // if there are more than one node with an exit neighbor on the minimum level we choose the one with the most exit neigbors
                if (exitNeighborsWithMinDist.Count > 1)
                {
                    nearestExitNeighbor =
                        exitNeighborsWithMinDist.OrderByDescending(
                            distance => distance.Node.Neighbors.Count(node => node.Exit)).First();
                }

                linkToCut = nearestExitNeighbor.Node.Links.Find(l => l.Nodes.Any(n => n.Exit));

                if (nearestExitNeighbor.Distance > 2)
                {
                    int maxExitNeighbors = bfs.NodeDistances.Max(nd => nd.Node.Neighbors.Count(n => n.Exit));
                    IEnumerable<NodeDistance> maxExitNeighborNodes =
                        bfs.NodeDistances.Where(nd => nd.Node.Neighbors.Count(n => n.Exit) == maxExitNeighbors);
                    NodeDistance nearestMultiExitNeighbor = maxExitNeighborNodes.OrderBy(n => n.Distance).First();

                    linkToCut = nearestMultiExitNeighbor.Node.Links.Find(l => l.Nodes.Any(n => n.Exit));
                }

                //// Tuple<Link, CountToReach>
                //List<Tuple<Link, int>> exitLinks = new BreadthFirstSearch().Search(agent.Position).ToList();
                //Tuple<Link, int> nearestExitLinkTuple = exitLinks.First();
                //if (nearestExitLinkTuple.Item2 < 0)
                //    linkToCut = nearestExitLinkTuple.Item1;
                //else
                //{
                //    // Tuple<Node, LinkCount, CountToReach>
                //    List<Tuple<Node, int, int>> myList = new List<Tuple<Node, int, int>>();
                //    foreach (Tuple<Link, int> exitLink in exitLinks)
                //    {
                //        Node n = exitLink.Item1.Nodes.First(x => !x.Exit);
                //        int index = myList.FindIndex(x => x.Item1 == n);
                //        if (index != -1)
                //        {
                //            myList[index] = Tuple.Create(myList[index].Item1, myList[index].Item2 + 1,
                //                myList[index].Item3);
                //        }
                //        else
                //            myList.Add(Tuple.Create(n, 1, exitLink.Item2));
                //    }

                //    Tuple<Node, int, int> target =
                //        myList.Aggregate(
                //            (current, next) => next.Item3 < current.Item3 && next.Item2 >= 2 ? next : current);

                //    linkToCut = target.Item1.Links.Find(l => l.Nodes.Any(n => n.Exit));
                //}
            }

            SeverLink(linkToCut);
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
    public Node Node { get; set; }
    public int Distance { get; set; }
}

public class BreadthFirstSearch
{
    private List<NodeDistance> nodeDistances;

    private class BfsNodeDistance : NodeDistance
    {
        public Node PredecessorNode { get; set; }
    }

    public IEnumerable<NodeDistance> NodeDistances => nodeDistances.OrderBy(nd => nd.Distance);

    public void CalculateDistances(Node start)
    {
        nodeDistances = new List<NodeDistance>();

        Queue<BfsNodeDistance> queue = new Queue<BfsNodeDistance>();
        HashSet<Node> visitedNodes = new HashSet<Node>();

        queue.Enqueue(new BfsNodeDistance { Node = start, PredecessorNode = null, Distance = 0 });

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
                    queue.Enqueue(new BfsNodeDistance
                    {
                        Node = neighbor,
                        PredecessorNode = bfsNode.Node,
                        Distance = bfsNode.Distance + 1
                    });
                }
            }

            nodeDistances.Add(new NodeDistance { Node = bfsNode.Node, Distance = bfsNode.Distance });
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