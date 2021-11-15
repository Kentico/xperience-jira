using CMS.Automation;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.MacroEngine;
using CMS.Membership;
using CMS.SiteProvider;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using Xperience.Jira.Models;

namespace Xperience.Jira
{
    /// <summary>
    /// Provides methods for interfacing with the Jira REST API as well as other
    /// Jira-related tasks
    /// </summary>
    public class JiraHelper
    {
        private static UserInfo mUser;

        private static readonly string GET_PROJECTS = BaseUrl + "project",
                                       GET_ISSUE_TYPES = BaseUrl + "issue/createmeta?projectIds={0}",
                                       GET_ISSUE_CREATE_META = BaseUrl + "issue/createmeta?projectIds={0}&issuetypeIds={1}&expand=projects.issuetypes.fields",
                                       GET_ISSUES = BaseUrl + "issue/picker?query={0}&currentProjectId={1}",
                                       GET_TRANSITIONS = BaseUrl + "issue/{0}/transitions";

        private readonly string DO_TRANSITION = BaseUrl + "issue/{0}/transitions",
                                ADD_COMMENT = BaseUrl + "issue/{0}/comment",
                                CREATE_POST = BaseUrl + "issue";

        #region Properties

        /// <summary>
        /// The key for storing and retriving an object's linked Jira webhook
        /// ID from its custom data column
        /// </summary>
        public static string LinkedWebhookKey
        {
            get
            {
                return "LinkedJiraWebhook";
            }
        }

        /// <summary>
        /// The key for storing and retriving an object's linked Jira project
        /// ID from its custom data column
        /// </summary>
        public static string LinkedProjectKey
        {
            get
            {
                return "LinkedJiraProject";
            }
        }

        /// <summary>
        /// The key for storing and retriving an object's linked Jira issue
        /// ID from its custom data column
        /// </summary>
        public static string LinkedIssueKey
        {
            get
            {
                return "LinkedJiraIssue";
            }
        }

        /// <summary>
        /// A macro that can be used within webhook scopes that is replaced
        /// by the object's linked issue
        /// </summary>
        public static string LinkedIssueMacro
        {
            get
            {
                return "##LinkedIssue##";
            }
        }

        /// <summary>
        /// A macro that can be used within webhook scopes that is replaced
        /// by the object's linked project
        /// </summary>
        public static string LinkedProjectMacro
        {
            get
            {
                return "##LinkedProject##";
            }
        }

        /// <summary>
        /// The Jira server domain and path for standard Jira REST requests
        /// </summary>
        public static string BaseUrl
        {
            get
            {
                var jiraDomain = SettingsKeyInfoProvider.GetValue("JiraServer");
                return $"{jiraDomain}/rest/api/latest/";
            }
        }

        /// <summary>
        /// The Jira server domain and path for Jira webhook REST requests
        /// </summary>
        public static string WebhookBaseUrl
        {
            get
            {
                var jiraDomain = SettingsKeyInfoProvider.GetValue("JiraServer");
                return $"{jiraDomain}/rest/webhooks/1.0/webhook";
            }
        }

        /// <summary>
        /// Returns the Base64 converted email and API token for the REST API Authorization header.
        /// Attempts to get the email address and API token from the set <see cref="User"/> which
        /// defaults to the Global Administrator
        /// </summary>
        private static string BasicAuthorization
        {
            get
            {
                var email = User.Email;
                var token = User.GetValue("JiraApiToken", "");
                if (String.IsNullOrEmpty(email) || String.IsNullOrEmpty(token))
                {
                    // Set (non-GA) user may not have these properties set, try to get from Global Administrator
                    var admin = UserInfoProvider.AdministratorUser;
                    email = admin.Email;
                    token = admin.GetValue("JiraApiToken", "");
                }

                if (String.IsNullOrEmpty(email) || String.IsNullOrEmpty(token))
                {
                    throw new NullReferenceException("The email and/or JiraApiToken properties have not been set.");
                }

                var bytes = Encoding.UTF8.GetBytes($"{email}:{token}");
                return Convert.ToBase64String(bytes);
            }
        }

        /// <summary>
        /// Returns whether the Jira integration is enabled in the Xperience Settings application
        /// </summary>
        private bool IsEnabled
        {
            get
            {
                return SettingsKeyInfoProvider.GetBoolValue("EnableJira");
            }
        }

        /// <summary>
        /// The user in which Jira operations are performed. Only pertinent for POST operations in which
        /// the correct Jira user should be displayed in issue history. For GET operations, the user can
        /// be anyone with the proper permissions
        /// </summary>
        private static UserInfo User
        {
            get
            {
                if (mUser == null)
                {
                    return UserInfoProvider.AdministratorUser;
                }
                else
                {
                    return mUser;
                }
            }
            set
            {
                mUser = value;
            }
        }

        public JiraHelper(UserInfo user = null)
        {
            User = user;
        }

        #endregion

        #region GET methods

        /// <summary>
        /// Returns a list of Jira projects. The returned <see cref="JiraProject"/> objects
        /// do not contain <see cref="JiraIssueType"/> objects or their schemas
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<JiraProject> GetProjects()
        {
            var response = DoRequest(GET_PROJECTS, HttpMethod.Get);
            return JsonConvert.DeserializeObject<IEnumerable<JiraProject>>(response);
        }

        /// <summary>
        /// Returns a list of valid workflow transitions for the provided issue
        /// </summary>
        /// <param name="issue"></param>
        /// <returns></returns>
        public static JArray GetTransitions(string issue)
        {
            var url = string.Format(GET_TRANSITIONS, issue);
            var response = JObject.Parse(DoRequest(url, HttpMethod.Get));
            return response.Value<JArray>("transitions");
        }

        /// <summary>
        /// Returns a list of Jira issues within the project which match the query
        /// </summary>
        /// <param name="project"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static JToken[] GetIssues(string project, string query)
        {
            var url = string.Format(GET_ISSUES, query, project);
            var response = JObject.Parse(DoRequest(url, HttpMethod.Get));
            return response.SelectToken("$.sections[0].issues").ToArray();
        }

        /// <summary>
        /// Returns a list of Jira projects. The returned <see cref="JiraProject"/> objects
        /// contain <see cref="JiraIssueType"/> objects and their schemas
        /// </summary>
        /// <returns></returns>
        public static JiraProject GetProjectWithCreateSchema(string project, string issueType)
        {
            var url = string.Format(GET_ISSUE_CREATE_META, project, issueType);
            var response = JObject.Parse(DoRequest(url, HttpMethod.Get));
            var projectsArray = response.Value<JArray>("projects");
            var projects = JsonConvert.DeserializeObject<IEnumerable<JiraProject>>(projectsArray.ToString());
            return projects.Where(p => p.Id == project).FirstOrDefault();
        }

        /// <summary>
        /// Returns a Jira project with its issue types
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public static JiraProject GetProjectWithIssueTypes(string project)
        {
            var url = string.Format(GET_ISSUE_TYPES, project);
            var response = JObject.Parse(DoRequest(url, HttpMethod.Get));
            var projectToken = response.SelectToken("$.projects");
            var projects = JsonConvert.DeserializeObject<IEnumerable<JiraProject>>(JsonConvert.SerializeObject(projectToken));
            return projects.Where(p => p.Id == project).FirstOrDefault();
        }

        #endregion

        #region POST methods

        /// <summary>
        /// Attempts to transition a Jira issue to a new workflow step. This may fail
        /// if the Jira issue has been moved into a workflow step in which the transition
        /// is no longer valid
        /// </summary>
        /// <param name="issue"></param>
        /// <param name="transition"></param>
        /// <returns></returns>
        public string DoTransition(string issue, string transition)
        {
            if (!IsEnabled)
            {
                throw new Exception("Jira integration is not enabled");
            }

            var data = $"{{ transition: {{ id:\"{transition}\" }} }}";
            var obj = JsonConvert.DeserializeObject<JObject>(data);
            var url = string.Format(DO_TRANSITION, issue);

            return DoRequest(url, HttpMethod.Post, obj);
        }

        /// <summary>
        /// Creates a new Jira webhook which triggers on the specified event(s) if the event
        /// meets optional filters specified in the scope. When the webhook is triggered, the
        /// request will be sent to the <see cref="JiraWebhookController"/>
        /// </summary>
        /// <param name="infoObj"></param>
        /// <param name="name"></param>
        /// <param name="events"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        public static string CreateWebhook(BaseInfo infoObj, string name, string events, string scope)
        {
            var data = $"{{ name: '{name}', url: '{GetCallbackUrl(infoObj)}', excludeBody: false }}";
            var obj = JsonConvert.DeserializeObject<JObject>(data);

            var eventsArray = events.Split(',');
            obj["events"] = new JArray(eventsArray);

            if (!String.IsNullOrEmpty(scope))
            {
                var filters = new JObject();
                var filterArray = scope.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

                foreach (var filter in filterArray)
                {
                    var arr = filter.Split(':');
                    var value = arr[1];

                    if (value.Contains(LinkedIssueMacro))
                    {
                        if (String.IsNullOrEmpty(GetLinkedIssue(infoObj)))
                        {
                            throw new ArgumentException($"Jira webhook scopes contained a filter with '{LinkedIssueMacro}', but no linked issue was found.");
                        }

                        value = value.Replace(LinkedIssueMacro, GetLinkedIssue(infoObj));
                    }
                    if (value.Contains(LinkedProjectMacro))
                    {
                        if (String.IsNullOrEmpty(GetLinkedProject(infoObj)))
                        {
                            throw new ArgumentException($"Jira webhook scopes contained a filter with '{LinkedProjectMacro}', but no linked project was found.");
                        }

                        value = value.Replace(LinkedProjectMacro, GetLinkedProject(infoObj));
                    }

                    filters.Add(new JProperty(arr[0], value));
                }

                obj["filters"] = filters;
            }

            return DoRequest(WebhookBaseUrl, HttpMethod.Post, obj);
        }

        /// <summary>
        /// Creates a new comment on a Jira issue using the identity of the <see cref="User"/>
        /// </summary>
        /// <param name="issue"></param>
        /// <param name="comment"></param>
        /// <returns></returns>
        public string AddComment(string issue, string comment)
        {
            if (!IsEnabled)
            {
                throw new Exception("Jira integration is not enabled");
            }

            comment = HttpUtility.JavaScriptStringEncode(HTMLHelper.HTMLDecode(comment));

            var data = JObject.Parse($"{{ body:\"{comment}\" }}");
            var url = string.Format(ADD_COMMENT, issue);
            return DoRequest(url, HttpMethod.Post, data);
        }

        /// <summary>
        /// Creates a new issue in Jira
        /// </summary>
        /// <param name="metaFields">The fields of the issue to be set</param>
        /// <param name="project">The project ID to create the issue under</param>
        /// <param name="issueType">The ID of the issue type to create</param>
        /// <param name="resolver">A resolver used to resolve macros in the metadata</param>
        /// <returns></returns>
        public string CreateIssue(Hashtable metaFields, string project, string issueType, MacroResolver resolver)
        {
            if (!IsEnabled)
            {
                throw new Exception("Jira integration is not enabled");
            }

            var data = $"{{ fields: {{ project: {{ id: \"{project}\" }}, issuetype: {{id: \"{issueType}\" }} }}";
            var obj = JsonConvert.DeserializeObject<JObject>(data);

            var selectedProject = GetProjectWithCreateSchema(project, issueType);
            var issueFields = selectedProject.IssueTypes.Where(i => i.Id == issueType).FirstOrDefault().GetFields();

            foreach (string key in metaFields.Keys)
            {
                var value = ValidationHelper.GetString(metaFields[key], "");
                value = resolver.ResolveMacros(HttpUtility.UrlDecode(value));

                if (!String.IsNullOrEmpty(value))
                {
                    if (key == "duedate")
                    {
                        var days = ValidationHelper.GetInteger(value, 0);
                        value = DateTime.Now.AddDays(days).ToString("yyyy-MM-dd");
                    }

                    var field = issueFields.Where(f => f.Key == key).FirstOrDefault();
                    if (field.IsComplexValue)
                    {
                        var jObject = new JObject(new JProperty("id", value));
                        obj["fields"][key] = jObject;
                    }
                    else
                    {
                        obj["fields"][key] = value;
                    }
                }
            }

            return DoRequest(CREATE_POST, HttpMethod.Post, obj);
        }

        #endregion

        #region DELETE methods

        /// <summary>
        /// Deletes a Jira webhook
        /// </summary>
        /// <param name="webhook"></param>
        /// <returns></returns>
        public static string DeleteWebhook(string webhook)
        {
            var url = WebhookBaseUrl + $"/{webhook}";
            return DoRequest(url, HttpMethod.Delete);
        }

        #endregion

        #region Other methods

        private static string DoRequest(string url, HttpMethod method, JObject data = null)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {BasicAuthorization}");

                HttpResponseMessage response = null;
                if (method == HttpMethod.Get)
                {
                    response = client.GetAsync(url).Result;
                }
                else if (method == HttpMethod.Post)
                {
                    response = client.PostAsJsonAsync(url, data).Result;
                }
                else if (method == HttpMethod.Delete)
                {
                    response = client.DeleteAsync(url).Result;
                }

                if (response != null)
                {
                    var content = response.Content.ReadAsStringAsync().Result;
                    client.Dispose();

                    if (response.IsSuccessStatusCode)
                    {
                        return content;
                    }
                    else
                    {
                        throw new Exception($"Error posting to {url}: {content}");
                    }
                }
            }

            return string.Empty;
        }

        private static string GetCallbackUrl(BaseInfo infoObj)
        {
            var site = SiteContext.CurrentSite;
            if (site == null)
            {
                site = infoObj.Site as SiteInfo;
            }
            if (site == null)
            {
                throw new Exception("Unable to retrieve current site.");
            }

            return $"https://{site.DomainName}/jiraapi/{infoObj.TypeInfo.ObjectClassName}/" + "${issue.id}";
        }

        /// <summary>
        /// Adds Jira issue and project info to a TreeNode's <see cref="TreeNode.DocumentCustomData"/>
        /// or an automation's <see cref="AutomationStateInfo.StateCustomData"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="issue"></param>
        /// <param name="project"></param>
        public static void LinkJiraIssue(BaseInfo obj, string issue, string project)
        {
            if (obj is AutomationStateInfo)
            {
                var state = obj as AutomationStateInfo;
                state.StateCustomData[LinkedIssueKey] = issue;
                state.StateCustomData[LinkedProjectKey] = project;
                state.Update();
            }
            if (obj is TreeNode)
            {
                var node = obj as TreeNode;
                node.DocumentCustomData[LinkedIssueKey] = issue;
                node.DocumentCustomData[LinkedProjectKey] = project;
                node.Update();
            }
        }

        /// <summary>
        /// Adds a Jira webhook ID to a TreeNode's <see cref="TreeNode.DocumentCustomData"/>
        /// or an automation's <see cref="AutomationStateInfo.StateCustomData"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="issue"></param>
        /// <param name="project"></param>
        public static void LinkJiraWebhook(BaseInfo obj, string webhook)
        {
            if (obj is AutomationStateInfo)
            {
                var state = obj as AutomationStateInfo;
                state.StateCustomData[LinkedWebhookKey] = webhook;
                state.Update();
            }
            if (obj is TreeNode)
            {
                var node = obj as TreeNode;
                node.DocumentCustomData[LinkedWebhookKey] = webhook;
                node.Update();
            }
        }

        /// <summary>
        /// Returns the Jira webhook ID that is linked to the object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetLinkedWebhook(BaseInfo obj)
        {
            if (obj is AutomationStateInfo)
            {
                var state = obj as AutomationStateInfo;
                return ValidationHelper.GetString(state.StateCustomData[LinkedWebhookKey], "");
            }
            if (obj is TreeNode)
            {
                var node = obj as TreeNode;
                return ValidationHelper.GetString(node.DocumentCustomData[LinkedWebhookKey], "");
            }

            return null;
        }

        /// <summary>
        /// Returns the Jira issue ID that is linked to the object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetLinkedIssue(BaseInfo obj)
        {
            if (obj is AutomationStateInfo)
            {
                var state = obj as AutomationStateInfo;
                return ValidationHelper.GetString(state.StateCustomData[LinkedIssueKey], "");
            }
            if (obj is TreeNode)
            {
                var node = obj as TreeNode;
                return ValidationHelper.GetString(node.DocumentCustomData[LinkedIssueKey], "");
            }

            return null;
        }

        /// <summary>
        /// Returns the Jira project ID that is linked to the object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetLinkedProject(BaseInfo obj)
        {
            if (obj is AutomationStateInfo)
            {
                var state = obj as AutomationStateInfo;
                return ValidationHelper.GetString(state.StateCustomData[LinkedProjectKey], "");
            }
            if (obj is TreeNode)
            {
                var node = obj as TreeNode;
                return ValidationHelper.GetString(node.DocumentCustomData[LinkedProjectKey], "");
            }

            return null;
        }

        #endregion
    }
}