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

            // TODO: Den nächsten Exit suchen. Wenn steps bis zum nächsten Exit >= 2, dann den nächsten Node suchen, der auf mehrere Exits geht.

            // cut links to exit nodes (for non-exit-node with more than one link to an exit node)
            if (linkToCut == null)
            {
                // Gets all Links leading to the nearest exit from a node with more than one exit links
                List<Tuple<Link, int>> exitLinks = new BreadthFirstSearch().Search(agent.Position).ToList();

                if (exitLinks.Any())
                {
                    // count the exit links for each node that has exit links
                    Dictionary<Node, int> dic = new Dictionary<Node, int>();
                    foreach (Node n in exitLinks.Select(link => link.Item1.Nodes.First(node => !node.Exit)))
                    {
                        if (!dic.ContainsKey(n))
                            dic.Add(n, 1);
                        else
                            dic[n]++;
                    }

                    // Get the node with the most exit links
                    Node target = dic.Aggregate((agg, next) => next.Value > agg.Value ? next : agg).Key;

                    // cut a link on the target node
                    linkToCut = target.Links.Find(x => x.Nodes.Any(y => y.Exit));
                }
            }

            if (linkToCut == null)
                linkToCut = new BreadthFirstSearch().Search(agent.Position).First().Item1;

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
    public IEnumerable<Tuple<Link, int>> Search(Node start)
    {
        bool stop = false;

        Queue<Tuple<Node, Node, int>> queue = new Queue<Tuple<Node, Node, int>>();
        HashSet<Node> visitedNodes = new HashSet<Node>();

        queue.Enqueue(Tuple.Create<Node, Node, int>(start, null, 0));

        while (queue.Any())
        {
            Tuple<Node, Node, int> n = queue.Dequeue();

            visitedNodes.Add(n.Item1);

            if (n.Item1.Exit)
            {
                stop = true;
                yield return
                    Tuple.Create(
                        n.Item2.Links.Find(link => link.Nodes.Contains(n.Item1) && link.Nodes.Contains(n.Item2)),
                        n.Item3);
            }

            foreach (Node node in n.Item1.Links.SelectMany(link => link.Nodes))
            {
                if (!stop &&
                    !visitedNodes.Contains(node))
                    queue.Enqueue(Tuple.Create(node, n.Item1, n.Item3 + 1));
            }
        }
    }
}