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
        public static string getHistoryUrl(HtmlDocument itemDoc)
        {
            //TODO
            return "";
        }
        public static string getText(HtmlDocument itemDoc)
        {
            //TODO
            return "";
        }
        public static void writeFile(string text)
        {
            //TODO
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(@fileName))
            {
                file.WriteLine(text);
            }
        }
        public static void getHistoryText()
        {
            getItemUrls();
            //using (WebClient client = new WebClient())
            //{
            //    foreach(string url in itemUrls)
            //    {
            //        string htmlCode = client.DownloadString(url);
            //        string historyUrl = getHistoryUrl(htmlCode);
            //        string historyText = client.DownloadString(historyUrl);

            //    }
            //}
            foreach(string url in itemUrls)
            {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument itemDoc = web.Load(url);
                string historyUrl = getHistoryUrl(itemDoc);
                HtmlDocument historyDoc = web.Load(historyUrl);
                string text = getText(historyDoc);
                writeFile(text);
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
