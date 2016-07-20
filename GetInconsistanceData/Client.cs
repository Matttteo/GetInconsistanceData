using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using HtmlAgilityPack;
// https://www.nuget.org/packages/Microsoft.TeamFoundationServer.Client/
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

// https://www.nuget.org/packages/Microsoft.VisualStudio.Services.InteractiveClient/
using Microsoft.VisualStudio.Services.Client;

// https://www.nuget.org/packages/Microsoft.VisualStudio.Services.Client/
using Microsoft.VisualStudio.Services.Common;

using System.Configuration;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
namespace GetInconsistanceData
{
    class Client
    {
        private static string siteUrl;
        private static string teamProjectName;
        private static string userName;
        private static string password;
        private static string queryName;
        private static List<string> itemUrls;
        private static List<int> itemIds;
        private static string fileName;
        private static VssConnection connection;
        private static WorkItemTrackingHttpClient witClient;
        public Client()
        {
            init();
        }
        public static void init()
        {
            siteUrl = ConfigurationManager.AppSettings.Get("projectUrl");
            teamProjectName = ConfigurationManager.AppSettings.Get("projectName");
            userName = ConfigurationManager.AppSettings.Get("userName");
            password = ConfigurationManager.AppSettings.Get("password");
            queryName = ConfigurationManager.AppSettings.Get("queryName");
            fileName = ConfigurationManager.AppSettings.Get("fileName");
            itemUrls = new List<string>();
            itemIds = new List<int>();
            // Create a connection object, which we will use to get httpclient objects.  This is more robust
            // then newing up httpclient objects directly.  Be sure to send in the full collection uri.
            // For example:  http://myserver:8080/tfs/defaultcollection
            // We are using default VssCredentials which uses NTLM against a Team Foundation Server.  See additional provided
            // examples for creating credentials for other types of authentication.
            connection = new VssConnection(new Uri(siteUrl), new VssCredentials(new WindowsCredential(new NetworkCredential(userName, password))));

            // Create instance of WorkItemTrackingHttpClient using VssConnection
            witClient = connection.GetClient<WorkItemTrackingHttpClient>();
        }
        public  static void getItemUrls()
        {
            init();
            List<QueryHierarchyItem> queryHierarchyItems = witClient.GetQueriesAsync(teamProjectName, depth: 2).Result;

            // Search for 'My Queries' folder
            QueryHierarchyItem myQueriesFolder = queryHierarchyItems.FirstOrDefault(qhi => qhi.Name.Equals("My Queries"));
            if (myQueriesFolder != null)
            {

                // See if our 'REST Sample' query already exists under 'My Queries' folder.
                QueryHierarchyItem newBugsQuery = null;
                if (myQueriesFolder.Children != null)
                {
                    newBugsQuery = myQueriesFolder.Children.FirstOrDefault(qhi => qhi.Name.Equals(queryName));
                }
                if (newBugsQuery == null)
                {
                    // if the 'REST Sample' query does not exist, create it.
                    System.Console.WriteLine("query folder didn't exist ");
                }
                // run the queryName query
                WorkItemQueryResult result = witClient.QueryByIdAsync(newBugsQuery.Id).Result;
                if (result.WorkItems.Any())
                {
                    int skip = 0;
                    const int batchSize = 100;
                    IEnumerable<WorkItemReference> workItemRefs;
                    do
                    {
                        workItemRefs = result.WorkItems.Skip(skip).Take(batchSize);
                        if (workItemRefs.Any())
                        {
                            // get details for each work item in the batch
                            List<WorkItem> workItems = witClient.GetWorkItemsAsync(workItemRefs.Select(wir => wir.Id)).Result;
                            foreach (WorkItem workItem in workItems)
                            {
                                itemUrls.Add(workItem.Url);
                                itemIds.Add(workItem.Id.Value);
                            }
                        }
                        skip += batchSize;
                    }
                    while (workItemRefs.Count() == batchSize);
                }
                else
                {
                    Console.WriteLine("No work items were returned from query.");
                }
            }
        }
        public static string getHistoryUrl(string text)
        {
            const string re = @"(""workItemHistory"":{""href"":"")(https:\/\/[\w.\/]+)";
            Regex reg = new Regex(re);
            Match urlMatch = reg.Match(text);
            if(urlMatch.Success && urlMatch.Groups.Count > 0)
            {
                string url = urlMatch.Groups[2].Value;
                return url;
            }
            else return "";
        }
        public static string getText(string itemDoc)
        {
            //TODO

            return itemDoc;
        }
        public static void writeFile(string text, int id)
        {
            //TODO
            string filename = fileName + id;
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(filename))
            {
                file.WriteLine(text);
            }
        }
        public void getHistoryText()
        {
            getItemUrls();
            WebClient webClient = new WebClient();

            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + ":" + password));
            webClient.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
            for (int i = 0; i < itemUrls.Count; ++i)
            {
                string url = itemUrls[i];
                int id = itemIds[i];
                string text = webClient.DownloadString(url);
                string historyUrl = getHistoryUrl(text);
                if (historyUrl.Length == 0)
                {
                    Console.WriteLine("can't find history url.");
                    return;
                }
                string historyHtml = webClient.DownloadString(historyUrl);
                string rawText = getText(historyHtml);
                Console.WriteLine("Get histroy text of ID:{0} .", id);
                writeFile(rawText, id);
            }
        }
        public void BasicAuthRestSample()
        {
            // Create instance of VssConnection using basic auth credentials. 
            // For security, ensure you are connecting to an https server, since credentials get sent in plain text.
            VssConnection connection = new VssConnection(new Uri(siteUrl), new VssCredentials(new WindowsCredential(new NetworkCredential(userName, password))));

            WorkItemTrackingHttpClient witClient = connection.GetClient<WorkItemTrackingHttpClient>();
            List<QueryHierarchyItem> items = witClient.GetQueriesAsync(teamProjectName).Result;


        }
        public  void SampleREST()
        {

            // Get 2 levels of query hierarchy items
            List<QueryHierarchyItem> queryHierarchyItems = witClient.GetQueriesAsync(teamProjectName, depth: 2).Result;

            // Search for 'My Queries' folder
            QueryHierarchyItem myQueriesFolder = queryHierarchyItems.FirstOrDefault(qhi => qhi.Name.Equals("My Queries"));
            if (myQueriesFolder != null)
            {
                string queryName = "queryInconsistant";

                // See if our 'REST Sample' query already exists under 'My Queries' folder.
                QueryHierarchyItem newBugsQuery = null;
                if (myQueriesFolder.Children != null)
                {
                    newBugsQuery = myQueriesFolder.Children.FirstOrDefault(qhi => qhi.Name.Equals(queryName));
                }
                if (newBugsQuery == null)
                {
                    // if the 'REST Sample' query does not exist, create it.
                    newBugsQuery = new QueryHierarchyItem()
                    {
                        Name = queryName,
                        Wiql = "SELECT [System.Id],[System.WorkItemType],[System.Title],[System.AssignedTo],[System.State],[System.Tags] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.WorkItemType] = 'Bug' AND [System.State] = 'New'",
                        IsFolder = false
                    };
                    newBugsQuery = witClient.CreateQueryAsync(newBugsQuery, teamProjectName, myQueriesFolder.Name).Result;
                }

                // run the 'REST Sample' query
                WorkItemQueryResult result = witClient.QueryByIdAsync(newBugsQuery.Id).Result;

                if (result.WorkItems.Any())
                {
                    int skip = 0;
                    const int batchSize = 100;
                    IEnumerable<WorkItemReference> workItemRefs;
                    do
                    {
                        workItemRefs = result.WorkItems.Skip(skip).Take(batchSize);
                        if (workItemRefs.Any())
                        {
                            // get details for each work item in the batch
                            List<WorkItem> workItems = witClient.GetWorkItemsAsync(workItemRefs.Select(wir => wir.Id)).Result;
                            foreach (WorkItem workItem in workItems)
                            {
                               Console.WriteLine("{0}", workItem.Id);
                            }
                        }
                        skip += batchSize;
                    }
                    while (workItemRefs.Count() == batchSize);
                }
                else
                {
                    Console.WriteLine("No work items were returned from query.");
                }
            }
        }
    }
}
