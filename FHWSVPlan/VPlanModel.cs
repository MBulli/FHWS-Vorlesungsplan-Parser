using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FHWSVPlan
{
    class VPlanRoot
    {
        public readonly VPlanMetadata Header;
        public readonly List<VPlanReading> Readings;
        public readonly Dictionary<string, string> CourseIdMap;

        public VPlanRoot()
        {
            Header = new VPlanMetadata();
            Readings = new List<VPlanReading>();
            CourseIdMap = new Dictionary<string, string>();
        }
    }

    class VPlanMetadata
    {
        public readonly int FileVersion;

        public string Name;
        public DateTime LastUpdated;
        public DateTime Created;

        public VPlanMetadata()
        {
            FileVersion = 1;
            Created = DateTime.Now;
        }
    }

    class VPlanReading
    {
        public string HtmlNodeId; // only for debug

        public DateTime Start;
        public DateTime End;

        public string Type;
        public string Title;
        public string CourseID;
        public string Lecturer;
        public string Room;

        public override string ToString()
        {
            return string.Format("{0} - {1}; {2} {3}; {4}; {5}; [{6}]", Start, End, Type, Title, Lecturer, Room, HtmlNodeId);
        }
    }
}
