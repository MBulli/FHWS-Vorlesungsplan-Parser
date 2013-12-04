using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DDay.iCal;
using DDay.iCal.Serialization.iCalendar;

namespace FHWSVPlan
{
    class iCalendarGenerator
    {
        public static string CreateCalendarString(VPlanRoot vplan)
        {
            iCalendar iCal = CreateCalendar(vplan);
            iCalendarSerializer seri = new iCalendarSerializer();
            return seri.SerializeToString(iCal);
        }

        private static iCalendar CreateCalendar(VPlanRoot vplan)
        {
            iCalendar iCal = new iCalendar();

            foreach (VPlanReading reading in vplan.Readings)
            {
                Event evn = new Event();

                string summary = string.Format("{0} {1}", reading.Type, vplan.CourseIdMap[reading.CourseID]);
                evn.Summary =  summary;
                evn.Location = reading.Room;
                evn.Start = new iCalDateTime(reading.Start);
                evn.Duration = reading.End - reading.Start;

                iCal.AddChild(evn);
            }
            
            return iCal;
        }
    }
}
