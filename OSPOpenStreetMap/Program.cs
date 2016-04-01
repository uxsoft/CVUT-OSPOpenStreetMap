using OsmSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MoreLinq;
using System.Threading.Tasks;
using System.Device.Location;

namespace OSPOpenStreetMapTester
{
    class Program
    {
        const string PBFPATH = @"C:\Users\me\Downloads\czech-republic-latest.osm.pbf";

        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            Console.WriteLine("Please specify the tag you want your ways filtered by");
            Console.WriteLine("Key: ");
            string tagKey = Console.ReadLine();
            Console.WriteLine("Value: ");
            string tagValue = Console.ReadLine();
            Console.WriteLine($"Processing all ways tagged {tagKey}={tagValue}");

            using (var file = System.IO.File.OpenRead(PBFPATH))
            {
                var source = new OsmSharp.Streams.PBFOsmStreamSource(file);

                var totalLength = ProcessWays(new WayEnumerable(source)
                    .Where(w => w.Tags.ContainsKey(tagKey) && w.Tags[tagKey] == tagValue));

                Console.WriteLine($"Total length: {Math.Floor(totalLength / 1000)}km");
            }

            Console.WriteLine($"Took {watch.Elapsed}");
            Console.ReadLine();
        }

        private static double ProcessWays(IEnumerable<Way> ways)
        {
            var waysFixed = ways.ToArray();

            var nodesNeeded = waysFixed.SelectMany(w => w.Nodes).Distinct();
            var nodes = FindNodes(nodesNeeded);

            return waysFixed.Select(w =>
                w.Nodes
                    .Select(id => nodes[id])
                    .Pairwise((from, to) => DistanceEstimateInMeter(from.Item1, from.Item2, to.Item1, to.Item2))
                    .Sum()
            ).Sum();
        }

        public static double DistanceEstimateInMeter(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            return new GeoCoordinate(latitude1, longitude1).GetDistanceTo(new GeoCoordinate(latitude2, longitude2));
        }

        public static Dictionary<long, Tuple<double, double>> FindNodes(IEnumerable<long> nodeIds)
        {
            Queue<long> idQueue = new Queue<long>();
            foreach (long id in nodeIds.OrderBy(n => n))
                idQueue.Enqueue(id);

            var nodes = new Dictionary<long, Tuple<double, double>>();

            using (var file = System.IO.File.OpenRead(PBFPATH))
            {
                var source = new OsmSharp.Streams.PBFOsmStreamSource(file);
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
}