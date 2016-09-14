using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

internal class Player
{
    private static void Main()
    {
        // Initialize Map
        Map map = new Map();

        // Create SkynetAgent
        SkynetAgent agent = new SkynetAgent();

        string[] inputs = Console.ReadLine().Split(' ');
        int nodeCount = int.Parse(inputs[0]); // the total number of nodes in the level, including the gateways
        int linkCount = int.Parse(inputs[1]); // the number of links
        int exitCounts = int.Parse(inputs[2]); // the number of exit gateways

        // Add nodes to map
        map.AddNodes(nodeCount);

        for (int i = 0; i < linkCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            int n1 = int.Parse(inputs[0]); // N1 and N2 defines a link between these nodes
            int n2 = int.Parse(inputs[1]);

            Link link = new Link(map[n1], map[n2]);
            map[n1].Links.Add(link);
            map[n2].Links.Add(link);
        }

        for (int i = 0; i < exitCounts; i++)
        {
            int exitNode = int.Parse(Console.ReadLine()); // the index of a gateway node

            map.Nodes[exitNode].Exit = true;
        }

        // game loop
        while (true)
        {
            // The index of the node on which the Skynet agent is positioned this turn
            int agentPosition = int.Parse(Console.ReadLine());

            agent.Position = map[agentPosition];

            // emergency cut. if the agent sits on a link with only one step to an an exit node we want to cut that link
            Link linkToCut = agent.Position.Links.Find(link => link.Nodes.Any(node => node.Exit));

            // cut link to exit node with shortest path to agent
            linkToCut = new BreadthFirstSearch().Search(agent.Position);

            //// cut links to exit nodes (for non-exit-node with max links)
            //if (linkToCut == null)
            //{
            //    // Gets all Links leading to an exit
            //    IEnumerable<Link> exitLinks =
            //        map.Nodes.FindAll(
            //            node => !node.Exit && node.Links.Any(link => link.Nodes.Any(innerNode => innerNode.Exit)))
            //            .SelectMany(x => x.Links)
            //            .Where(link => link.Nodes.Any(n => n.Exit));

            //    // count the exit links for each node that has exit links
            //    Dictionary<Node, int> dic = new Dictionary<Node, int>();
            //    foreach (Node n in exitLinks.Select(link => link.Nodes.First(node => !node.Exit)))
            //    {
            //        if (!dic.ContainsKey(n))
            //            dic.Add(n, 1);
            //        else
            //            dic[n]++;
            //    }

            //    // Get the node with the most exit links
            //    Node target = dic.Aggregate((agg, next) => next.Value > agg.Value ? next : agg).Key;

            //    // cut a link on the target node
            //    linkToCut = target.Links.Find(x => x.Nodes.Any(y => y.Exit));
            //}

            SeverLink(linkToCut);
        }
    }

    private static void SeverLink(Link l)
    {
        foreach (Node node in l.Nodes)
            node.Links.Remove(l);

        Console.WriteLine(l.ToString());
    }
}

public class Node
{
    public int Id { get; }

    public List<Link> Links { get; }

    public bool Exit { get; set; }

    public Node(int id)
    {
        Id = id;
        Links = new List<Link>();
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

public class BreadthFirstSearch
{
    public Link Search(Node start)
    {
        Queue<Tuple<Node, Node>> queue = new Queue<Tuple<Node, Node>>();
        HashSet<Node> visitedNodes = new HashSet<Node>();

        queue.Enqueue(Tuple.Create<Node, Node>(start, null));

        while (true)
        {
            Tuple<Node, Node> n = queue.Dequeue();

            if (n.Item1.Exit)
                return n.Item2.Links.Find(link => link.Nodes.Contains(n.Item1) && link.Nodes.Contains(n.Item2));

            visitedNodes.Add(n.Item1);

            foreach (Node node in n.Item1.Links.SelectMany(link => link.Nodes))
            {
                if (!visitedNodes.Contains(node))
                    queue.Enqueue(Tuple.Create(node, n.Item1));
            }
        }
    }
}