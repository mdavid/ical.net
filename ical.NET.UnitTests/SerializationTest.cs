using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Ical.Net;
using Ical.Net.DataTypes;
using Ical.Net.Interfaces;
using Ical.Net.Interfaces.Components;
using Ical.Net.Interfaces.DataTypes;
using Ical.Net.Interfaces.General;
using Ical.Net.Interfaces.Serialization;
using Ical.Net.Serialization;
using Ical.Net.Serialization.iCalendar.Serializers;
using Ical.Net.Serialization.iCalendar.Serializers.Other;
using NUnit.Framework;

namespace ical.NET.UnitTests
{
    [TestFixture]
    public class SerializationTest
    {
        public static void CompareCalendars(ICalendar cal1, ICalendar cal2)
        {
            CompareComponents(cal1, cal2);

            Assert.AreEqual(cal1.Children.Count, cal2.Children.Count, "Children count is different between calendars.");

            for (var i = 0; i < cal1.Children.Count; i++)
            {
                var component1 = cal1.Children[i] as ICalendarComponent;
                var component2 = cal2.Children[i] as ICalendarComponent;
                if (component1 != null && component2 != null)
                {
                    CompareComponents(component1, component2);
                }
            }
        }

        public static void CompareComponents(ICalendarComponent cb1, ICalendarComponent cb2)
        {
            foreach (var p1 in cb1.Properties)
            {
                var isMatch = false;
                foreach (var p2 in cb2.Properties.AllOf(p1.Name))
                {
                    try
                    {
                        Assert.AreEqual(p1, p2, "The properties '" + p1.Name + "' are not equal.");
                        if (p1.Value is IComparable)
                            Assert.AreEqual(0, ((IComparable)p1.Value).CompareTo(p2.Value), "The '" + p1.Name + "' property values do not match.");
                        else if (p1.Value is IEnumerable)
                            CompareEnumerables((IEnumerable)p1.Value, (IEnumerable)p2.Value, p1.Name);
                        else
                            Assert.AreEqual(p1.Value, p2.Value, "The '" + p1.Name + "' property values are not equal.");

                        isMatch = true;
                        break;
                    }
                    catch { }
                }

                Assert.IsTrue(isMatch, "Could not find a matching property - " + p1.Name + ":" + (p1.Value?.ToString() ?? string.Empty));                    
            }

            Assert.AreEqual(cb1.Children.Count, cb2.Children.Count, "The number of children are not equal.");
            for (var i = 0; i < cb1.Children.Count; i++)
            {
                var child1 = cb1.Children[i] as ICalendarComponent;
                var child2 = cb2.Children[i] as ICalendarComponent;
                if (child1 != null && child2 != null)
                    CompareComponents(child1, child2);
                else
                    Assert.AreEqual(child1, child2, "The child objects are not equal.");
            }
        }

        public static void CompareEnumerables(IEnumerable a1, IEnumerable a2, string value)
        {
            if (a1 == null && a2 == null)
                return;

            Assert.IsFalse((a1 == null && a2 != null) || (a1 != null && a2 == null), value + " do not match - one item is null");

            var enum1 = a1.GetEnumerator();
            var enum2 = a2.GetEnumerator();

            while (enum1.MoveNext() && enum2.MoveNext())
                Assert.AreEqual(enum1.Current, enum2.Current, value + " do not match");
        }

        /// <summary>
        /// At times, this may throw a WebException if an internet connection is not present.
        /// This is safely ignored.
        /// </summary>
        [Test, ExpectedException(typeof(WebException))]
        public void Attachment4()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\Attachment4.ics")[0];
            ProgramTest.TestCal(iCal);

            var evt = iCal.Events["uuid1153170430406"];
            Assert.IsNotNull(evt, "Event could not be accessed by UID");

            var a = evt.Attachments[0];
            a.LoadDataFromUri();
            Assert.IsNotNull(a.Data);
            Assert.AreNotEqual(0, a.Data.Length);

            var ms = new MemoryStream();
            ms.SetLength(a.Data.Length);
            a.Data.CopyTo(ms.GetBuffer(), 0);

            var iCal1 = Calendar.LoadFromStream(ms)[0];
            Assert.IsNotNull(iCal1, "Attached iCalendar did not load correctly");

            throw new WebException();
        }

        [Test, Category("Serialization")]
        public void Attendee1()
        {
            var iCal = Calendar.LoadFromFile(typeof(Calendar), @"Calendars\Serialization\Attendee1.ics", Encoding.UTF8)[0];
            Assert.AreEqual(1, iCal.Events.Count);
            
            var evt = iCal.Events.First();
            // Ensure there are 2 attendees
            Assert.AreEqual(2, evt.Attendees.Count);            

            var attendee1 = evt.Attendees[0];
            var attendee2 = evt.Attendees[1];

            // Values
            Assert.AreEqual(new Uri("mailto:joecool@example.com"), attendee1.Value);
            Assert.AreEqual(new Uri("mailto:ildoit@example.com"), attendee2.Value);

            // MEMBERS
            Assert.AreEqual(1, attendee1.Members.Count);
            Assert.AreEqual(0, attendee2.Members.Count);
            Assert.AreEqual(new Uri("mailto:DEV-GROUP@example.com"), attendee1.Members[0]);

            // DELEGATED-FROM
            Assert.AreEqual(0, attendee1.DelegatedFrom.Count);
            Assert.AreEqual(1, attendee2.DelegatedFrom.Count);
            Assert.AreEqual(new Uri("mailto:immud@example.com"), attendee2.DelegatedFrom[0]);

            // DELEGATED-TO
            Assert.AreEqual(0, attendee1.DelegatedTo.Count);
            Assert.AreEqual(0, attendee2.DelegatedTo.Count);
        }

        /// <summary>
        /// Tests that multiple parameters of the
        /// same name are correctly aggregated into
        /// a single list.
        /// </summary>
        [Test, Category("Serialization")]
        public void Attendee2()
        {
            var iCal = Calendar.LoadFromFile(typeof(Calendar), @"Calendars\Serialization\Attendee2.ics", Encoding.UTF8)[0];
            Assert.AreEqual(1, iCal.Events.Count);

            var evt = iCal.Events.First();
            // Ensure there is 1 attendee
            Assert.AreEqual(1, evt.Attendees.Count);

            var attendee1 = evt.Attendees[0];

            // Values
            Assert.AreEqual(new Uri("mailto:joecool@example.com"), attendee1.Value);

            // MEMBERS
            Assert.AreEqual(3, attendee1.Members.Count);
            Assert.AreEqual(new Uri("mailto:DEV-GROUP@example.com"), attendee1.Members[0]);
            Assert.AreEqual(new Uri("mailto:ANOTHER-GROUP@example.com"), attendee1.Members[1]);
            Assert.AreEqual(new Uri("mailto:THIRD-GROUP@example.com"), attendee1.Members[2]);
        }

        /// <summary>
        /// Tests that Lotus Notes-style properties are properly handled.
        /// https://sourceforge.net/tracker/?func=detail&aid=2033495&group_id=187422&atid=921236
        /// Sourceforge bug #2033495
        /// </summary>
        [Test, Category("Serialization")]
        public void Bug2033495()
        {
            var iCal = Calendar.LoadFromFile(typeof(Calendar), @"Calendars\Serialization\Bug2033495.ics", Encoding.UTF8)[0];
            Assert.AreEqual(1, iCal.Events.Count);
            Assert.AreEqual(iCal.Properties["X-LOTUS-CHILD_UID"].Value, "XXX");
        }

        /// <summary>
        /// Tests bug #2938007 - involving the HasTime property in IDateTime values.
        /// See https://sourceforge.net/tracker/?func=detail&aid=2938007&group_id=187422&atid=921236
        /// </summary>
        [Test, Category("Serialization")]
        public void Bug2938007()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\Bug2938007.ics")[0];
            Assert.AreEqual(1, iCal.Events.Count);

            var evt = iCal.Events.First();
            Assert.AreEqual(true, evt.Start.HasTime);
            Assert.AreEqual(true, evt.End.HasTime);

            foreach (var o in evt.GetOccurrences(new CalDateTime(2010, 1, 17, 0, 0, 0), new CalDateTime(2010, 2, 1, 0, 0, 0)))
            {
                Assert.AreEqual(true, o.Period.StartTime.HasTime);
                Assert.AreEqual(true, o.Period.EndTime.HasTime);
            }
        }

        /// <summary>
        /// Tests bug #3177278 - Serialize closes stream
        /// See https://sourceforge.net/tracker/?func=detail&aid=3177278&group_id=187422&atid=921236
        /// </summary>
        [Test, Category("Serialization")]
        public void Bug3177278()
        {
            var calendar = new Calendar();
            var serializer = new CalendarSerializer();

            var ms = new MemoryStream();
            serializer.Serialize(calendar, ms, Encoding.UTF8);

            Assert.IsTrue(ms.CanWrite);
        }

        /// <summary>
        /// Tests bug #3211934 - Bug in iCalendar.cs - UnauthorizedAccessException
        /// See https://sourceforge.net/tracker/?func=detail&aid=3211934&group_id=187422&atid=921236
        /// </summary>
        [Test, Category("Serialization")]
        public void Bug3211934()
        {
            var calendar = new Calendar();
            var serializer = new CalendarSerializer();

            var filename = "Bug3211934.ics";

            if (File.Exists(filename))
            {
                // Reset the file attributes and delete
                File.SetAttributes(filename, FileAttributes.Normal);
                File.Delete(filename);
            }

            serializer.Serialize(calendar, filename);

            // Set the file as read-only
            File.SetAttributes(filename, FileAttributes.ReadOnly);

            // Load the calendar from file, and ensure the read-only attribute doesn't affect the load
            var calendars = Calendar.LoadFromFile(filename, Encoding.UTF8, serializer);
            Assert.IsNotNull(calendars);

            // Reset the file attributes and delete
            File.SetAttributes(filename, FileAttributes.Normal);
            File.Delete(filename);
        }

        /// <summary>
        /// Tests that a mixed-case VERSION property is loaded properly
        /// </summary>
        [Test, Category("Serialization")]
        public void CaseInsensitive4()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\CaseInsensitive4.ics")[0];
            Assert.AreEqual("2.5", iCal.Version);
        }

        [Test, Category("Serialization")]
        public void Categories1_2()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\Categories1.ics")[0];
            ProgramTest.TestCal(iCal);
            var evt = iCal.Events.First();

            var items = new ArrayList();
            items.AddRange(new[]
            {
                "One", "Two", "Three",
                "Four", "Five", "Six",
                "Seven", "A string of text with nothing less than a comma, semicolon; and a newline\n."
            });

            var found = new Hashtable();

            foreach (var s in evt.Categories.Where(s => items.Contains(s)))
            {
                found[s] = true;
            }

            foreach (string item in items)
                Assert.IsTrue(found.ContainsKey(item), "Event should contain CATEGORY '" + item + "', but it was not found.");
        }

        [Test, Category("Serialization")]
        public void EmptyLines1()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\EmptyLines1.ics")[0];
            Assert.AreEqual(2, iCal.Events.Count, "iCalendar should have 2 events");
        }

        [Test, Category("Serialization")]
        public void EmptyLines2()
        {
            var calendars = Calendar.LoadFromFile(@"Calendars\Serialization\EmptyLines2.ics");
            Assert.AreEqual(2, calendars.Count);
            Assert.AreEqual(2, calendars[0].Events.Count, "iCalendar should have 2 events");
            Assert.AreEqual(2, calendars[1].Events.Count, "iCalendar should have 2 events");
        }

        /// <summary>
        /// Verifies that blank lines between components are allowed
        /// (as occurs with some applications/parsers - i.e. KOrganizer)
        /// </summary>
        [Test, Category("Serialization")]
        public void EmptyLines3()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\EmptyLines3.ics")[0];
            Assert.AreEqual(1, iCal.Todos.Count, "iCalendar should have 1 todo");
        }

        /// <summary>
        /// Similar to PARSE4 and PARSE5 tests.
        /// </summary>
        [Test, Category("Serialization")]
        public void EmptyLines4()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\EmptyLines4.ics")[0];
            Assert.AreEqual(28, iCal.Events.Count);
        }

        [Test]
        public void Encoding2()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\Encoding2.ics")[0];
            ProgramTest.TestCal(iCal);
            var evt = iCal.Events.First();

            Assert.AreEqual(
"This is a test to try out base64 encoding without being too large.\r\n" +
"This is a test to try out base64 encoding without being too large.\r\n" +
"This is a test to try out base64 encoding without being too large.\r\n" +
"This is a test to try out base64 encoding without being too large.\r\n" +
"This is a test to try out base64 encoding without being too large.\r\n" +
"This is a test to try out base64 encoding without being too large.\r\n" +
"This is a test to try out base64 encoding without being too large.\r\n" +
"This is a test to try out base64 encoding without being too large.\r\n" +
"This is a test to try out base64 encoding without being too large.\r\n" +
"This is a test to try out base64 encoding without being too large.\r\n" +
"This is a test to try out base64 encoding without being too large.\r\n" +
"This is a test to try out base64 encoding without being too large.",
                evt.Attachments[0].Value,
                "Attached value does not match.");
        }

        [Test]
        public void Encoding3()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\Encoding3.ics")[0];
            ProgramTest.TestCal(iCal);
            var evt = iCal.Events.First();

            Assert.AreEqual("uuid1153170430406", evt.Uid, "UID should be 'uuid1153170430406'; it is " + evt.Uid);
            Assert.AreEqual(1, evt.Sequence, "SEQUENCE should be 1; it is " + evt.Sequence);
        }

        [Test, Category("Serialization")]
        public void Event8()
        {
            var sr = new StringReader(@"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Apple Computer\, Inc//iCal 1.0//EN
CALSCALE:GREGORIAN
BEGIN:VEVENT
CREATED:20070404T211714Z
DTEND:20070407T010000Z
DTSTAMP:20070404T211714Z
DTSTART:20070406T230000Z
DURATION:PT2H
RRULE:FREQ=WEEKLY;UNTIL=20070801T070000Z;BYDAY=FR
SUMMARY:Friday Meetings
DTSTAMP:20040103T033800Z
SEQUENCE:1
UID:fd940618-45e2-4d19-b118-37fd7a8e3906
END:VEVENT
BEGIN:VEVENT
CREATED:20070404T204310Z
DTEND:20070416T030000Z
DTSTAMP:20070404T204310Z
DTSTART:20070414T200000Z
DURATION:P1DT7H
RRULE:FREQ=DAILY;COUNT=12;BYDAY=SA,SU
SUMMARY:Weekend Yea!
DTSTAMP:20040103T033800Z
SEQUENCE:1
UID:ebfbd3e3-cc1e-4a64-98eb-ced2598b3908
END:VEVENT
END:VCALENDAR
");
            var iCal = Calendar.LoadFromStream(sr)[0];
            Assert.IsTrue(iCal.Events.Count == 2, "There should be 2 events in the parsed calendar");
            Assert.IsNotNull(iCal.Events["fd940618-45e2-4d19-b118-37fd7a8e3906"], "Event fd940618-45e2-4d19-b118-37fd7a8e3906 should exist in the calendar");
            Assert.IsNotNull(iCal.Events["ebfbd3e3-cc1e-4a64-98eb-ced2598b3908"], "Event ebfbd3e3-cc1e-4a64-98eb-ced2598b3908 should exist in the calendar");
        }

        [Test]
        public void GeographicLocation1_2()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\GeographicLocation1.ics")[0];
            ProgramTest.TestCal(iCal);
            var evt = iCal.Events.First();

            Assert.AreEqual(37.386013, evt.GeographicLocation.Latitude, "Latitude should be 37.386013; it is not.");
            Assert.AreEqual(-122.082932, evt.GeographicLocation.Longitude, "Longitude should be -122.082932; it is not.");
        }

        [Test, Category("Serialization")]
        public void Google1()
        {
            var tzId = "Europe/Berlin";
            var iCal = Calendar.LoadFromFile(@"Calendars/Serialization/Google1.ics")[0];
            var evt = iCal.Events["594oeajmftl3r9qlkb476rpr3c@google.com"];
            Assert.IsNotNull(evt);

            IDateTime dtStart = new CalDateTime(2006, 12, 18, tzId);
            IDateTime dtEnd = new CalDateTime(2006, 12, 23, tzId);
            var occurrences = iCal.GetOccurrences(dtStart, dtEnd).OrderBy(o => o.Period.StartTime).ToList();

            var dateTimes = new[]
            {
                new CalDateTime(2006, 12, 18, 7, 0, 0, tzId),
                new CalDateTime(2006, 12, 19, 7, 0, 0, tzId),
                new CalDateTime(2006, 12, 20, 7, 0, 0, tzId),
                new CalDateTime(2006, 12, 21, 7, 0, 0, tzId),
                new CalDateTime(2006, 12, 22, 7, 0, 0, tzId)
            };

            for (var i = 0; i < dateTimes.Length; i++)
                Assert.AreEqual(dateTimes[i], occurrences[i].Period.StartTime, "Event should occur at " + dateTimes[i]);

            Assert.AreEqual(dateTimes.Length, occurrences.Count, "There should be exactly " + dateTimes.Length + " occurrences; there were " + occurrences.Count);
        }

        /// <summary>
        /// Tests that valid RDATE properties are parsed correctly.
        /// </summary>
        [Test, Category("Serialization")]
        public void RecurrenceDates1()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\RecurrenceDates1.ics")[0];
            Assert.AreEqual(1, iCal.Events.Count);
            Assert.AreEqual(3, iCal.Events.First().RecurrenceDates.Count);
            
            Assert.AreEqual((CalDateTime)new DateTime(1997, 7, 14, 12, 30, 0, DateTimeKind.Utc), iCal.Events.First().RecurrenceDates[0][0].StartTime);
            Assert.AreEqual((CalDateTime)new DateTime(1996, 4, 3, 2, 0, 0, DateTimeKind.Utc), iCal.Events.First().RecurrenceDates[1][0].StartTime);
            Assert.AreEqual((CalDateTime)new DateTime(1996, 4, 3, 4, 0, 0, DateTimeKind.Utc), iCal.Events.First().RecurrenceDates[1][0].EndTime);
            Assert.AreEqual(new CalDateTime(1997, 1, 1), iCal.Events.First().RecurrenceDates[2][0].StartTime);
            Assert.AreEqual(new CalDateTime(1997, 1, 20), iCal.Events.First().RecurrenceDates[2][1].StartTime);
            Assert.AreEqual(new CalDateTime(1997, 2, 17), iCal.Events.First().RecurrenceDates[2][2].StartTime);
            Assert.AreEqual(new CalDateTime(1997, 4, 21), iCal.Events.First().RecurrenceDates[2][3].StartTime);
            Assert.AreEqual(new CalDateTime(1997, 5, 26), iCal.Events.First().RecurrenceDates[2][4].StartTime);
            Assert.AreEqual(new CalDateTime(1997, 7, 4), iCal.Events.First().RecurrenceDates[2][5].StartTime);
            Assert.AreEqual(new CalDateTime(1997, 9, 1), iCal.Events.First().RecurrenceDates[2][6].StartTime);
            Assert.AreEqual(new CalDateTime(1997, 10, 14), iCal.Events.First().RecurrenceDates[2][7].StartTime);
            Assert.AreEqual(new CalDateTime(1997, 11, 28), iCal.Events.First().RecurrenceDates[2][8].StartTime);
            Assert.AreEqual(new CalDateTime(1997, 11, 29), iCal.Events.First().RecurrenceDates[2][9].StartTime);
            Assert.AreEqual(new CalDateTime(1997, 12, 25), iCal.Events.First().RecurrenceDates[2][10].StartTime);
        }

        /// <summary>
        /// Tests that valid REQUEST-STATUS properties are parsed correctly.
        /// </summary>
        [Test, Category("Serialization")]
        public void RequestStatus1()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\RequestStatus1.ics")[0];
            Assert.AreEqual(1, iCal.Events.Count);
            Assert.AreEqual(4, iCal.Events.First().RequestStatuses.Count);

            var rs = iCal.Events.First().RequestStatuses[0];
            Assert.AreEqual(2, rs.StatusCode.Primary);
            Assert.AreEqual(0, rs.StatusCode.Secondary);
            Assert.AreEqual("Success", rs.Description);
            Assert.IsNull(rs.ExtraData);

            rs = iCal.Events.First().RequestStatuses[1];
            Assert.AreEqual(3, rs.StatusCode.Primary);
            Assert.AreEqual(1, rs.StatusCode.Secondary);
            Assert.AreEqual("Invalid property value", rs.Description);
            Assert.AreEqual("DTSTART:96-Apr-01", rs.ExtraData);

            rs = iCal.Events.First().RequestStatuses[2];
            Assert.AreEqual(2, rs.StatusCode.Primary);
            Assert.AreEqual(8, rs.StatusCode.Secondary);
            Assert.AreEqual(" Success, repeating event ignored. Scheduled as a single event.", rs.Description);
            Assert.AreEqual("RRULE:FREQ=WEEKLY;INTERVAL=2", rs.ExtraData);

            rs = iCal.Events.First().RequestStatuses[3];
            Assert.AreEqual(4, rs.StatusCode.Primary);
            Assert.AreEqual(1, rs.StatusCode.Secondary);
            Assert.AreEqual("Event conflict. Date/time is busy.", rs.Description);
            Assert.IsNull(rs.ExtraData);
        }

        /// <summary>
        /// Tests that string escaping works with Text elements.
        /// </summary>
        [Test, Category("Serialization")]
        public void String2()
        {
            var serializer = new StringSerializer();
            var value = @"test\with\;characters";
            var unescaped = (string)serializer.Deserialize(new StringReader(value));

            Assert.AreEqual(@"test\with;characters", unescaped, "String unescaping was incorrect.");

            value = @"C:\Path\To\My\New\Information";
            unescaped = (string)serializer.Deserialize(new StringReader(value));
            Assert.AreEqual("C:\\Path\\To\\My\new\\Information", unescaped, "String unescaping was incorrect.");

            value = @"\""This\r\nis\Na\, test\""\;\\;,";
            unescaped = (string)serializer.Deserialize(new StringReader(value));

            Assert.AreEqual("\"This\\r\nis\na, test\";\\;,", unescaped, "String unescaping was incorrect.");
        }

        [Test, Category("Serialization")]
        public void Transparency2()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\Transparency2.ics")[0];

            Assert.AreEqual(1, iCal.Events.Count);
            var evt = iCal.Events.First();

            Assert.AreEqual(TransparencyType.Transparent, evt.Transparency);
        }

        /// <summary>
        /// Tests that DateTime values that are out-of-range are still parsed correctly
        /// and set to the closest representable date/time in .NET.
        /// </summary>
        [Test, Category("Serialization")]
        public void DateTime1()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\DateTime1.ics")[0];
            Assert.AreEqual(6, iCal.Events.Count);

            var evt = iCal.Events["nc2o66s0u36iesitl2l0b8inn8@google.com"];
            Assert.IsNotNull(evt);

            // The "Created" date is out-of-bounds.  It should be coerced to the
            // closest representable date/time.
            Assert.AreEqual(DateTime.MinValue, evt.Created.Value);
        }

        [Test, Category("Serialization")]
        public void Language3_1()
        {
            var calendarPath = Path.Combine(Environment.CurrentDirectory, "Calendars");
            calendarPath = Path.Combine(calendarPath, "Serialization");

            var russia1 = Calendar.LoadFromUri(new Uri("http://www.mozilla.org/projects/calendar/caldata/RussiaHolidays.ics"))[0];
            var russia2 = Calendar.LoadFromFile(Path.Combine(calendarPath, "Language3.ics"))[0];

            CompareCalendars(russia1, russia2);
        }

        [Test, Category("Serialization")]
        public void Language4()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars/Serialization/Language4.ics")[0];
            Assert.IsNotNull(iCal);
        }

        [Test, Category("Serialization")]
        public void Outlook2007_LineFolds1()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars/Serialization/Outlook2007LineFolds.ics")[0];
            var events = iCal.GetOccurrences(new CalDateTime(2009, 06, 20), new CalDateTime(2009, 06, 22));
            Assert.AreEqual(1, events.Count);
        }

        [Test, Category("Serialization")]
        public void Outlook2007_LineFolds2()
        {
            var longName = "The Exceptionally Long Named Meeting Room Whose Name Wraps Over Several Lines When Exported From Leading Calendar and Office Software Application Microsoft Office 2007";
            var iCal = Calendar.LoadFromFile(@"Calendars/Serialization/Outlook2007LineFolds.ics")[0];
            var events = iCal.GetOccurrences<Event>(new CalDateTime(2009, 06, 20), new CalDateTime(2009, 06, 22)).OrderBy(o => o.Period.StartTime).ToList();
            Assert.AreEqual(longName, ((IEvent)events[0].Source).Location);
        }

        /// <summary>
        /// Tests that multiple parameters are allowed in iCalObjects
        /// </summary>
        [Test, Category("Serialization")]
        public void Parameter1()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\Parameter1.ics")[0];

            var evt = iCal.Events.First();
            IList<ICalendarParameter> parms = evt.Properties["DTSTART"].Parameters.AllOf("VALUE").ToList();
            Assert.AreEqual(2, parms.Count);
            Assert.AreEqual("DATE", parms[0].Values.First());
            Assert.AreEqual("OTHER", parms[1].Values.First());
        }

        /// <summary>
        /// Tests that empty parameters are allowed in iCalObjects
        /// </summary>
        [Test, Category("Serialization")]
        public void Parameter2()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\Parameter2.ics")[0];
            Assert.AreEqual(2, iCal.Events.Count);
        }

        /// <summary>
        /// Tests a calendar that should fail to properly parse.
        /// </summary>
        [Test, Category("Serialization"), ExpectedException("antlr.MismatchedTokenException")]
        public void Parse1()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\Parse1.ics")[0];
            Assert.IsNotNull(iCal);
        }

        /// <summary>
        /// Tests that multiple properties are allowed in iCalObjects
        /// </summary>
        [Test, Category("Serialization")]
        public void Property1()
        {
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\Property1.ics")[0];

            IList<ICalendarProperty> props = iCal.Properties.AllOf("VERSION").ToList();
            Assert.AreEqual(2, props.Count);

            for (var i = 0; i < props.Count; i++)
                Assert.AreEqual("2." + i, props[i].Value);
        }

        /// <summary>
        /// Tests that line/column numbers are correctly tracked for
        /// parsed (deserialized) calendars.
        /// </summary>
        [Test, Category("Serialization")]
        public void LineColumns1()
        {
            var ctx = new SerializationContext();

            var settings = ctx.GetService(typeof(ISerializationSettings)) as ISerializationSettings;
            settings.EnsureAccurateLineNumbers = true;

            var serializer = new CalendarSerializer
            {
                SerializationContext = ctx
            };
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\EmptyLines1.ics", Encoding.UTF8, serializer)[0];

            Assert.AreEqual(2, iCal.Events.Count);
            Assert.AreEqual(4, iCal.Events.First().Line);
            Assert.AreEqual(18, iCal.Events[1].Line);
            Assert.AreEqual(5, iCal.Events.First().Properties["CREATED"].Line);
            Assert.AreEqual(6, iCal.Events.First().Properties["LAST-MODIFIED"].Line);
            Assert.AreEqual(7, iCal.Events.First().Properties["DTSTAMP"].Line);
            Assert.AreEqual(8, iCal.Events.First().Properties["UID"].Line);
            Assert.AreEqual(9, iCal.Events.First().Properties["SUMMARY"].Line);
            Assert.AreEqual(10, iCal.Events.First().Properties["CLASS"].Line);
            Assert.AreEqual(11, iCal.Events.First().Properties["DTSTART"].Line);
            Assert.AreEqual(12, iCal.Events.First().Properties["DTEND"].Line);
            Assert.AreEqual(13, iCal.Events.First().Properties["CATEGORIES"].Line);
            Assert.AreEqual(14, iCal.Events.First().Properties["X-MOZILLA-ALARM-DEFAULT-LENGTH"].Line);
            Assert.AreEqual(15, iCal.Events.First().Properties["LOCATION"].Line);
        }

        /// <summary>
        /// Tests that line/column numbers are correctly tracked for
        /// parsed (deserialized) calendars.
        /// </summary>
        [Test, Category("Serialization")]
        public void LineColumns2()
        {
            var ctx = new SerializationContext();

            var settings = ctx.GetService(typeof(ISerializationSettings)) as ISerializationSettings;
            settings.EnsureAccurateLineNumbers = true;

            var serializer = new CalendarSerializer
            {
                SerializationContext = ctx
            };
            var iCal = Calendar.LoadFromFile(@"Calendars\Serialization\Calendar1.ics", Encoding.UTF8, serializer)[0];

            Assert.IsNotNull(iCal.Todos["2df60496-1e73-11db-ba96-e3cfe6793b5f"]);
            Assert.IsNotNull(iCal.Todos["4836c236-1e75-11db-835f-a024e2a6131f"]);
            Assert.AreEqual(110, iCal.Todos["4836c236-1e75-11db-835f-a024e2a6131f"].Properties["LOCATION"].Line);
            Assert.AreEqual(123, iCal.Todos["2df60496-1e73-11db-ba96-e3cfe6793b5f"].Properties["UID"].Line);
        }
    }
}
