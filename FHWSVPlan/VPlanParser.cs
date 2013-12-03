using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebBrowser = System.Windows.Controls.WebBrowser;

namespace FHWSVPlan
{
    struct VPlanElementRect
    {
        public readonly int Left;
        public readonly int Right;
        public readonly int Top;
        public readonly int Bottom;

        public VPlanElementRect(dynamic jsResult)
        {
            if (jsResult == null)
                throw new ArgumentNullException("jsResult");

            Left = jsResult.left;
            Right = jsResult.right;
            Top = jsResult.top;
            Bottom = jsResult.bottom;
        }
    }

    class VPlanMetadata
    {
        public readonly int FileVersion;

        public DateTime LastUpdated;
        public DateTime Created;

        public VPlanMetadata()
        {
            FileVersion = 1;
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

    class VPlanParser
    {
        private const string fileUrl = "http://www.welearn.de/fileadmin/share/vlplan/BaInf3_2013ws.html";
        private const string fileUrl2 = "http://www.welearn.de/fileadmin/share/vlplan/BaWinf2_2013ws.html";
        private const string fileUrl3 = "http://www.welearn.de/fileadmin/share/vlplan/BaInf7TI_2013ws.html";
        
        private const string jsFxName = "test";
        private const string jsFxElementBoundsByXPath = "elementBoundsByXPath";
        private const string dateFormatRfc3339 = "yyyy-MM-dd'T'HH:mm:ssK"; // RFC-3339 format string

        private WebBrowser webBrowser;
        private HtmlDocument htmlDoc;
        private string documentFilename;

        public VPlanParser(WebBrowser browser)
        {
            if (browser == null)
                throw new ArgumentNullException("browser");

            webBrowser = browser;
        }

        public void Start()
        {
            ParseVPlanFromUrl(new Uri(fileUrl));
        }

        private void ParseVPlanFromUrl(Uri url)
        {
            htmlDoc = DownloadHtmlDocument(url);
            documentFilename = System.IO.Path.GetFileNameWithoutExtension(url.AbsolutePath);

            InjectJavascriptFunction(htmlDoc);

            using (System.IO.StringWriter sw = new System.IO.StringWriter())
            {
                htmlDoc.Save(sw);

                webBrowser.LoadCompleted += OnWebBrowserLoadedInjectedDocument;
                webBrowser.NavigateToString(sw.ToString());
                // logic continues in OnWebBrowserLoadedInjectedDocument
            }
        }

        void OnWebBrowserLoadedInjectedDocument(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            webBrowser.LoadCompleted -= OnWebBrowserLoadedInjectedDocument;

            var tableNodes = htmlDoc.DocumentNode.SelectNodes("//table");
            var headlines1 = htmlDoc.DocumentNode.SelectNodes("//div[@class='w1']");
            var headlines2 = htmlDoc.DocumentNode.SelectNodes("//div[@class='w2']");
            var footer = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='fzl']");

            if (tableNodes.Count != headlines1.Count || tableNodes.Count != headlines2.Count)
                throw new Exception();

            // parse footer
            VPlanMetadata metaData = ParseFooter(footer);


            // parse readings
            List<VPlanReading> readings = new List<VPlanReading>();
            Dictionary<string, string> courseIdMap = new Dictionary<string, string>();

            for (int i = 0; i < tableNodes.Count; i++)
            {
                foreach (var newReading in ParseWeek(tableNodes[i], headlines1[i], headlines2[i]))
                {
                    readings.Add(newReading);

                    if (!courseIdMap.ContainsValue(newReading.Title))
                    {
                        string hash = MD5Hash(newReading.Title);
                        courseIdMap.Add(hash, newReading.Title);
                        newReading.CourseID = hash;
                        newReading.Title = null;
                    }
                }
            }

            // write all in json file
            var jsonObj = new { metadata = metaData, courses = courseIdMap, readings = readings };
            SerializeCourses(jsonObj);
        }

        private void SerializeCourses(object obj)
        {
            Newtonsoft.Json.JsonSerializerSettings set = new Newtonsoft.Json.JsonSerializerSettings();
            set.DateFormatString = dateFormatRfc3339;
            set.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented, set);
            System.IO.File.WriteAllText(string.Format("{0}.json", documentFilename), json);
        }

        private IEnumerable<VPlanReading> ParseWeek(HtmlNode table, HtmlNode w1, HtmlNode w2)
        {
            // w2 format
            // 9. Studienwoche, 48. KW: 25.11. - 01.12.2013

            string kwPart = w2.InnerText.Substring(w2.InnerText.IndexOf("KW: "));
            MatchCollection matches = Regex.Matches(kwPart, @"([0-9]{2})\.([0,1][0-9]\.)?([0-9]{4})?");

            if (matches.Count != 2)
                throw new Exception();

            DateTime weekEnd = DateTime.ParseExact(matches[1].Value, "dd.MM.yyyy", null); // sunday
            DateTime weekStart; // monday
            if (matches[0].Value.Length == 3)
            {
                int day = int.Parse(matches[0].Value.TrimEnd('.'));
                weekStart = new DateTime(weekEnd.Year, weekEnd.Month, day);
            }
            else
            {
                DateTime tmp = DateTime.ParseExact(matches[0].Value, "dd.MM.", null);
                weekStart = new DateTime(weekEnd.Year, tmp.Month, tmp.Day);
            }

            VPlanElementRect[] courseRanges = FindColumnRanges(table);

            foreach (var kvp in FindAllReadingNodesInWeek(table))
            {
                VPlanReading reading = new VPlanReading();

                int jsResultXPos = (int)webBrowser.InvokeScript(jsFxName, kvp.Key);
                int dayOffset = -1;

                for (int i = 0; i < courseRanges.Length; i++)
                {
                    if (jsResultXPos >= courseRanges[i].Left && jsResultXPos <= courseRanges[i].Right)
                    {
                        dayOffset = i;
                        break;
                    }
                }

                if (dayOffset == -1)
                    throw new Exception(string.Format("JavaScript returned unknown value: {0} with id: {1}", jsResultXPos, kvp.Key));

                // select all text from node and decode html umlaute (&Uuml; etc)
                var innerTexts = (from c in kvp.Value.ChildNodes
                                  where c.NodeType == HtmlNodeType.Text
                                  select System.Web.HttpUtility.HtmlDecode(c.InnerText)).ToArray();


                // find start/end datetime
                MatchCollection timeMatches = Regex.Matches(innerTexts[0], @"[0-9]{1,2}:[0-9]{2}");

                DateTime readingStartTime = DateTime.Parse(timeMatches[0].Value);
                DateTime readingEndTime = DateTime.Parse(timeMatches[1].Value);

                reading.Start = CombineDateWithTime(weekStart.AddDays(dayOffset), readingStartTime);
                reading.End = CombineDateWithTime(weekStart.AddDays(dayOffset), readingEndTime);

                // course type, title, lecture, room
                reading.Type = innerTexts[1].Substring(0, 1);
                reading.Title = innerTexts[1].Substring(2);

                reading.HtmlNodeId = kvp.Key;

                if (LooksLikeRoomString(innerTexts[2]))
                {
                    // no lecture only room number
                    reading.Room = innerTexts[2];
                }
                else
                {
                    reading.Lecturer = innerTexts[2];
                    reading.Room = innerTexts[3];
                }

                Debug.WriteLine("{0}: {1}", dayOffset, reading);

                yield return reading;
            }
        }

        private VPlanElementRect[] FindColumnRanges(HtmlNode table)
        {
            var tr = table.SelectSingleNode("//tr");
            var colHeader = from n in tr.ChildNodes
                            where n.GetAttributeValue("class", null) == "t"
                            select n;

            if (colHeader.Count() != 6)
                throw new Exception("Expected 6 col headers");

            List<VPlanElementRect> result = new List<VPlanElementRect>();

            foreach (HtmlNode td in colHeader)
            {
                string xpath = td.XPath;
                xpath = xpath.Replace("/html[1]/body[1]", "/html/body");
                xpath = xpath.Insert(xpath.IndexOf("/tr"), "/tbody");

                dynamic jsResult = webBrowser.InvokeScript(jsFxElementBoundsByXPath, xpath);

                result.Add(new VPlanElementRect(jsResult));
            }

            return result.ToArray();
        }

        private VPlanMetadata ParseFooter(HtmlNode footer)
        {
            VPlanMetadata result = new VPlanMetadata();
            result.Created = DateTime.Now;

            string txt = footer.ChildNodes[2].InnerText;
            var matches = Regex.Matches(txt, @"[0-9]{2}\.[0-9]{2}\.[0-9]{4}, [0-9]{1,2}:[0-9]{2}");

            string updatedStr = matches[matches.Count - 1].Value;
            var tmp = DateTime.Parse(updatedStr);
            result.LastUpdated = new DateTime(tmp.Ticks, DateTimeKind.Local);

            return result;
        }

        private static bool LooksLikeRoomString(string s)
        {
            return Regex.IsMatch(s, @"(H|I)\.[0-9].[0-9]{1,2}");
        }

        private static Dictionary<string, HtmlNode> FindAllReadingNodesInWeek(HtmlNode table)
        {
            var result = from td in table.SelectNodes("tr/td")
                         where td.Attributes["id"] != null
                         select new { Key = td.Attributes["id"].Value, Value = td };

            return result.ToDictionary(x => x.Key, x => x.Value);
        }

        private static HtmlDocument DownloadHtmlDocument(Uri url)
        {
            WebClient c = new WebClient();
            string content = c.DownloadString(url);

            using (System.IO.StringReader sr = new System.IO.StringReader(content))
            {
                HtmlDocument doc = new HtmlDocument();
                doc.Load(sr);

                return doc;
            }
        }

        private static void InjectJavascriptFunction(HtmlDocument doc)
        {
            string jsStr = string.Format(
                @"<script type='text/javascript'>
                    {0}

                    function {1} (id) {{
                            var e = document.getElementById(id);
                            return e.getBoundingClientRect().left;
                    }};	

                    function {2} (xpath) {{
		                    var e = document.evaluate(xpath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                            return e.getBoundingClientRect();
                    }};	
                </script>", Properties.Resources.XPath, jsFxName, jsFxElementBoundsByXPath);



            HtmlNode jsNode = HtmlNode.CreateNode(jsStr);
            HtmlNode cssNode = doc.DocumentNode.SelectSingleNode("html/head/style");

            cssNode.ParentNode.InsertAfter(jsNode, cssNode);
        }

        private static DateTime CombineDateWithTime(DateTime date, DateTime time)
        {
            // the kind parameters is important in this place
            // if not set to DateTimeKind.Local the json wouldn't contain time zone information
            return new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second, DateTimeKind.Local);
        }

        private static string MD5Hash(string input)
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] textToHash = Encoding.Default.GetBytes(input);
            byte[] result = md5.ComputeHash(textToHash);

            StringBuilder sb = new StringBuilder(result.Length*2);
            foreach (byte b in result)
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }
}
