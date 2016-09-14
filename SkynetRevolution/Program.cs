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

            Link linkToCut = null;

            // If the agent sits on a node with only one link we want to cut that link
            if (agent.Position.Links.Count == 1)
                linkToCut = agent.Position.Links[0];

            // emergency cut. if the agent sits on a link with only one step to an an exit node we want to cut that link
            if (linkToCut == null)
                linkToCut = agent.Position.Links.Find(link => link.Nodes.Any(node => node.Exit));

            // cut links to exit nodes
            if (linkToCut == null)
                linkToCut = map.Nodes.Find(node => node.Exit && node.Links.Count > 0)?.Links.First();

            // or cut just any link
            if (linkToCut == null)
                linkToCut = map.Nodes.Find(nodes => nodes.Links.Count > 0)?.Links.First();

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