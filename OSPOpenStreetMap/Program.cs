using OsmSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MoreLinq;
using System.Threading.Tasks;
using System.Device.Location;
using System.IO;
using System.Text.RegularExpressions;
using OsmSharp.Streams;

namespace OSPOpenStreetMapTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            //Input file
            string filePath = "", tagKey = "", tagValue = "";
            if (args.Any() && File.Exists(args.First()))
                filePath = args.First();
            else
            {
                do
                {
                    Console.WriteLine("Please specify the location of your OSM  data PBF file:");
                    filePath = Console.ReadLine();
                } while (!File.Exists(filePath));
            }

            //Input filter
            if (args.Length > 0)
            {
                var match = Regex.Match(args.Last(), "(?<key>[a-z]+)=(?<value>[a-z]+)");
                if (match.Success)
                {
                    tagKey = match.Groups["key"].Value;
                    tagValue = match.Groups["value"].Value;
                }
            }
            if (string.IsNullOrWhiteSpace(tagKey) || string.IsNullOrWhiteSpace(tagValue))
            {
                Console.WriteLine("Please specify the tag you want your ways filtered by");
                Console.WriteLine("Key: ");
                tagKey = Console.ReadLine();
                Console.WriteLine("Value: ");
                tagValue = Console.ReadLine();
                Console.WriteLine($"Processing all ways tagged {tagKey}={tagValue}");
            }

            //Process
            using (var waysStream = File.OpenRead(filePath))
            using (var nodesStream = File.OpenRead(filePath))
            {
                var ways = new WayEnumerable(new PBFOsmStreamSource(waysStream))
                    .Where(w => w.Tags.ContainsKey(tagKey) && w.Tags[tagKey] == tagValue)
                    .ToArray();

                var totalLength = ProcessWays(ways, new PBFOsmStreamSource(nodesStream));

                Console.WriteLine($"Total length: {Math.Floor(totalLength / 1000)}km");
            }

            Console.WriteLine($"Took {watch.Elapsed}");
            Console.ReadLine();
        }

        private static double ProcessWays(IEnumerable<Way> ways, OsmStreamSource source)
        {
            var nodesNeeded = ways
                .SelectMany(w => w.Nodes)
                .Distinct();
            var nodes = FindNodes(nodesNeeded, source);

            return ways.Select(w =>
                w.Nodes
                    .Select(id => nodes[id])
                    .Pairwise((from, to) => Distance(from, to))
                    .Sum()
            ).Sum();
        }

        public static double Distance(Tuple<double, double> from, Tuple<double, double> to)
        {
            return new GeoCoordinate(from.Item1, from.Item2).GetDistanceTo(new GeoCoordinate(to.Item1, to.Item2));
        }

        public static Dictionary<long, Tuple<double, double>> FindNodes(IEnumerable<long> nodeIds, OsmStreamSource source)
        {
            Queue<long> idQueue = new Queue<long>();
            foreach (long id in nodeIds.OrderBy(n => n))
                idQueue.Enqueue(id);

            var nodes = new Dictionary<long, Tuple<double, double>>();

            while (idQueue.Count > 0 && source.MoveNextNode())
            {
                if (source.Current().Id == idQueue.Peek())
                {
                    nodes.Add(idQueue.Dequeue(),
                        new Tuple<double, double>((source.Current() as Node).Latitude.Value,
                            (source.Current() as Node).Longitude.Value));
                }
            }
            return nodes;
        }
    }
}