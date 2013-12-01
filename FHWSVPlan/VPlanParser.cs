using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net;
using WebBrowser = System.Windows.Controls.WebBrowser;
using System.Diagnostics;


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

    class VPlanCourse
    {
        public DateTime Start;
        public DateTime End;

        public string Type;
        public string Title;
        public string Lecturer;
        public string Room;
    }

    class VPlanParser
    {
        private const string fileUrl = "http://www.welearn.de/fileadmin/share/vlplan/BaInf3_2013ws.html";
        private const string jsFxName = "test";
        private const string jsFxElementBoundsByXPath = "elementBoundsByXPath";

        private WebBrowser webBrowser;
        private HtmlDocument htmlDoc;

        public VPlanParser(WebBrowser browser)
        {
            if (browser == null)
                throw new ArgumentNullException("browser");

            webBrowser = browser;
        }

        public void Start()
        {
            htmlDoc = DownloadHtmlDocument(fileUrl);

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

            if (tableNodes.Count != headlines1.Count || tableNodes.Count != headlines2.Count)
                throw new Exception();

            List<VPlanCourse> courses = new List<VPlanCourse>();

            for (int i = 0; i < tableNodes.Count; i++)
            {
                courses.AddRange(ParseWeek(tableNodes[i], headlines1[i], headlines2[i]));
            }
        }

        private IEnumerable<VPlanCourse> ParseWeek(HtmlNode table, HtmlNode w1, HtmlNode w2)
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

            foreach (var kvp in FindAllCourseNodesInWeek(table))
            {
                VPlanCourse course = new VPlanCourse();

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

                var innerTexts = kvp.Value.ChildNodes.Where(c => c.NodeType == HtmlNodeType.Text).Select(c => c.InnerText).ToArray();

                // find start/end datetime
                MatchCollection timeMatches = Regex.Matches(innerTexts[0], @"[0-9]{1,2}:[0-9]{2}");

                DateTime courseStartTime = DateTime.Parse(timeMatches[0].Value);
                DateTime courseEndTime = DateTime.Parse(timeMatches[1].Value);

                course.Start = CombineDateWithTime(weekStart.AddDays(dayOffset), courseStartTime);
                course.End = CombineDateWithTime(weekStart.AddDays(dayOffset), courseEndTime);

                // course type, title, lecture, room
                course.Type = innerTexts[1].Substring(0, 1);
                course.Title = innerTexts[1].Substring(2);

                if (LooksLikeRoomString(innerTexts[2]))
                {
                    course.Room = innerTexts[2];
                }
                else
                {
                    course.Lecturer = innerTexts[2];
                    course.Room = innerTexts[3];
                }

                Debug.WriteLine("{0}: {1}", dayOffset, kvp.Value.InnerText);

                yield return course;
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

        private bool LooksLikeRoomString(string s)
        {
            return Regex.IsMatch(s, @"(H|I)\.[0-9].[0-9]{1,2}");
        }

        private Dictionary<string, HtmlNode> FindAllCourseNodesInWeek(HtmlNode table)
        {
            var result = from td in table.SelectNodes("tr/td")
                         where td.Attributes["id"] != null
                         select new { Key = td.Attributes["id"].Value, Value = td };

            return result.ToDictionary(x => x.Key, x => x.Value);
        }

        private static HtmlDocument DownloadHtmlDocument(string url)
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
            return new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
        }
    }
}
