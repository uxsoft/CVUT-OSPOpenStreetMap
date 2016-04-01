using OsmSharp;
using OsmSharp.Streams;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSPOpenStreetMapTester
{
    public class WayEnumerator : IEnumerator<Way>
    {
        private readonly OsmStreamSource Source;

        public WayEnumerator(OsmStreamSource source)
        {
            this.Source = source;
        }

        public Way Current
        {
            get
            {
                return Source.Current() as Way;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Source.Current();
            }
        }

        public void Dispose()
        {
            Source.Dispose();
        }

        public bool MoveNext()
        {
            return Source.MoveNextWay();
        }

        public void Reset()
        {
            Source.Reset();
        }
    }

    public class WayEnumerable : IEnumerable<Way>
    {
        private readonly OsmStreamSource Source;

        public WayEnumerable(OsmStreamSource source)
        {
            this.Source = source;
        }

        public IEnumerator<Way> GetEnumerator()
        {
            return new WayEnumerator(Source);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new WayEnumerator(Source);
        }
    }
}
