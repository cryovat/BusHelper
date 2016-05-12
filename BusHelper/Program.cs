using System;
using System.Collections.Generic;
using System.Linq;
using GTFS;
using GTFS.Entities;
using GTFS.Entities.Enumerations;
using GTFS.IO;

namespace BusHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            var reader = new GTFSReader<GTFSFeed>();
            var path = args[0];
            var toStop = args[1];
            var fromStops = new HashSet<string>(args.Skip(2));

            var trips = new List<TripSummary>();

            using (var sources = new GTFSDirectorySource(path))
            {

                var feed = reader.Read(sources);
                var today = DateTime.Today;

                var routes = feed.Routes.ToDictionary(r => r.Id);
                var tripLookup = feed.Trips.ToLookup(t => t.RouteId);
                var stopNames = feed.Stops.ToDictionary(s => s.Id, s => s.Name);
                var stopLookup = feed.StopTimes.ToLookup(s => s.TripId);
                var datesLookup = feed.CalendarDates.Where(d => d.ExceptionType == ExceptionType.Added).ToLookup(d => d.ServiceId);

                foreach (var trip in feed.Trips)
                {
                    var route = routes[trip.RouteId];

                    foreach (var date in datesLookup[trip.ServiceId])
                    {
                        var hasFrom = false;
                        var hasTo = false;
                        var bt = new TripSummary
                        {
                            RouteNumber = route.ShortName,
                            RouteTerminal = trip.Headsign,
                            Date = date.Date
                        };

                        foreach (var stopTime in stopLookup[trip.Id].OrderBy(s => s.StopSequence))
                        {
                            bt.Stops.Add(new StopSummary
                            {
                                StopId = stopTime.StopId,
                                StopName = stopNames[stopTime.StopId],
                                Arrival = stopTime.ArrivalTime,
                                Departure = stopTime.DepartureTime
                            });

                            if (!hasFrom && fromStops.Contains(stopTime.StopId))
                            {
                                hasFrom = true;
                            }

                            if (hasFrom && !hasTo && stopTime.StopId == toStop)
                            {
                                hasTo = true;
                            }
                        }

                        if (hasFrom && hasTo) trips.Add(bt);
                    }
                }
            }

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var lines = new List<OutputLine>();

            foreach (var trip in trips)
            {
                var from = trip.Stops.First(s => fromStops.Contains(s.StopId));
                var to = trip.Stops.First(s => s.StopId == toStop);

                var fromDate = new DateTime(trip.Date.Year, trip.Date.Month, trip.Date.Day, 0, 0, 0, DateTimeKind.Local);
                var toDate = new DateTime(trip.Date.Year, trip.Date.Month, trip.Date.Day, 0, 0, 0, DateTimeKind.Local);

                fromDate = fromDate.AddSeconds(from.Departure.TotalSeconds);
                toDate = toDate.AddSeconds(to.Arrival.TotalSeconds);

                lines.Add(new OutputLine
                {
                    Departure = fromDate,
                    Arrival = toDate,
                    FromName = from.StopName,
                    ToName = to.StopName,
                    RouteNumber = trip.RouteNumber,
                    RouteTerminal = trip.RouteTerminal
                });
            }

            foreach (var line in lines.OrderBy(l => l.Departure))
            {
                Console.WriteLine("{0}|{1:O}|{2}|{3}|{4:O}|{5}|{6}|{7}", line.Departure.ToAwkSystime(), line.Departure, line.FromName, line.Arrival.ToAwkSystime(), line.Arrival, line.ToName, line.RouteNumber, line.RouteTerminal);
            }
        }

        public class OutputLine
        {
            public DateTime Departure { get; set; }
            public DateTime Arrival { get; set; }
            public string FromName { get; set; }
            public string ToName { get; set; }
            public string RouteNumber { get; set; }
            public string RouteTerminal { get; set; }
        }

        public class TripSummary
        {
            public string RouteNumber { get; set; }
            public string RouteTerminal { get; set; }
            public DateTime Date { get; set; }
            public List<StopSummary> Stops { get; } = new List<StopSummary>();

            public override string ToString()
            {
                return string.Format("{0:d) - {1}: {2}", Date, RouteNumber, RouteTerminal);
            }
        }

        public class StopSummary
        {
            public string StopId { get; set; }
            public string StopName { get; set; }
            public TimeOfDay Arrival { get; set; }
            public TimeOfDay Departure { get; set; }

            public override string ToString()
            {
                return string.Format("{0:d) - {1}", Departure, StopName);
            }
        }
    }

    public static class Extensions
    {
        public static long ToAwkSystime(this DateTime date)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((date.ToUniversalTime() - dateTime).TotalSeconds);
        }
    }
}
