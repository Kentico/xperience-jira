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
using Kentico.Xperience.Jira.Models;

namespace Kentico.Xperience.Jira
{
    /// <summary>
    /// Provides methods for interfacing with the Jira REST API as well as other
    /// Jira-related tasks.
    /// </summary>
    public class JiraHelper
    {
        private static UserInfo mUser;

        private readonly string DO_TRANSITION = BaseUrl + "issue/{0}/transitions";
        private readonly string ADD_COMMENT = BaseUrl + "issue/{0}/comment";
        private readonly string CREATE_POST = BaseUrl + "issue";

        private static readonly string GET_PROJECTS = BaseUrl + "project";
        private static readonly string GET_ISSUE_TYPES = BaseUrl + "issue/createmeta?projectIds={0}";
        private static readonly string GET_ISSUE_CREATE_META = BaseUrl + "issue/createmeta?projectIds={0}&issuetypeIds={1}&expand=projects.issuetypes.fields";
        private static readonly string GET_ISSUES = BaseUrl + "issue/picker?query={0}&currentProjectId={1}";
        private static readonly string GET_TRANSITIONS = BaseUrl + "issue/{0}/transitions";

        #region Properties

        /// <summary>
        /// The key for storing and retriving an object's linked Jira webhook
        /// ID from its custom data column.
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
        /// ID from its custom data column.
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
        /// ID from its custom data column.
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
        /// by the object's linked issue.
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
        /// by the object's linked project.
        /// </summary>
        public static string LinkedProjectMacro
        {
            get
            {
                return "##LinkedProject##";
            }
        }

        /// <summary>
        /// The Jira server domain and path for standard Jira REST requests.
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
        /// The Jira server domain and path for Jira webhook REST requests.
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
        /// defaults to the Global Administrator.
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
        /// Returns whether the Jira integration is enabled in the Xperience Settings application.
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
        /// be anyone with the proper permissions.
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

        /// <summary>
        /// Constructor required for non-static method which depend on the provided <paramref name="user"/>
        /// to reflect the appropriate Jira accounts in issues and comments.
        /// </summary>
        /// <param name="user">The Xperience user whose Jira credentials will be used during any API action.
        /// If null, the default Global Administrator account will be used.</param>
        public JiraHelper(UserInfo user = null)
        {
            User = user;
        }

        #endregion

        #region GET methods

        /// <summary>
        /// Returns a list of Jira projects. The returned <see cref="JiraProject"/> objects
        /// do not contain <see cref="JiraIssueType"/> objects or their schemas.
        /// </summary>
        public static IEnumerable<JiraProject> GetProjects()
        {
            var response = DoRequest(GET_PROJECTS, HttpMethod.Get);
            return JsonConvert.DeserializeObject<IEnumerable<JiraProject>>(response);
        }

        /// <summary>
        /// Returns a list of valid workflow transitions for the provided issue.
        /// </summary>
        /// <param name="issueId">The internal ID of the issue to return transitions for.</param>
        public static JArray GetTransitions(string issueId)
        {
            var url = string.Format(GET_TRANSITIONS, issueId);
            var response = JObject.Parse(DoRequest(url, HttpMethod.Get));
            return response.Value<JArray>("transitions");
        }

        /// <summary>
        /// Returns a list of Jira issues within the project which match the query.
        /// </summary>
        /// <param name="projectId">The internal ID of the project to query issues of.</param>
        /// <param name="query">The text to search within summaries and descriptions of issues.</param>
        public static JToken[] GetIssues(string projectId, string query)
        {
            var url = string.Format(GET_ISSUES, query, projectId);
            var response = JObject.Parse(DoRequest(url, HttpMethod.Get));
            return response.SelectToken("$.sections[0].issues").ToArray();
        }

        /// <summary>
        /// Returns a project with the schema required to create a new issue of the specified type.
        /// </summary>
        /// <param name="projectId">The internal project ID of the project to retrieve.</param>
        /// <param name="issueTypeId">The internal ID of the issue type to retrieve the schema for.</param>
        public static JiraProject GetProjectWithCreateSchema(string projectId, string issueTypeId)
        {
            var url = string.Format(GET_ISSUE_CREATE_META, projectId, issueTypeId);
            var response = JObject.Parse(DoRequest(url, HttpMethod.Get));
            var projectsArray = response.Value<JArray>("projects");
            var projects = JsonConvert.DeserializeObject<IEnumerable<JiraProject>>(projectsArray.ToString());
            return projects.Where(p => p.Id == projectId).FirstOrDefault();
        }

        /// <summary>
        /// Returns a Jira project with its issue types.
        /// </summary>
        /// <param name="projectId">The internal ID of the project to retrieve.</param>
        public static JiraProject GetProjectWithIssueTypes(string projectId)
        {
            var url = string.Format(GET_ISSUE_TYPES, projectId);
            var response = JObject.Parse(DoRequest(url, HttpMethod.Get));
            var projectToken = response.SelectToken("$.projects");
            var projects = JsonConvert.DeserializeObject<IEnumerable<JiraProject>>(JsonConvert.SerializeObject(projectToken));
            return projects.Where(p => p.Id == projectId).FirstOrDefault();
        }

        #endregion

        #region POST methods

        /// <summary>
        /// Attempts to transition a Jira issue to a new workflow step. This may fail
        /// if the Jira issue has been moved into a workflow step in which the transition
        /// is no longer valid.
        /// </summary>
        /// <param name="issueId">The internal ID of the issue to transition.</param>
        /// <param name="transitionId">The internal ID of the transition to use.</param>
        public void DoTransition(string issueId, string transitionId)
        {
            if (!IsEnabled)
            {
                throw new Exception("Jira integration is not enabled.");
            }

            var data = $"{{ transition: {{ id:\"{transitionId}\" }} }}";
            var obj = JsonConvert.DeserializeObject<JObject>(data);
            var url = string.Format(DO_TRANSITION, issueId);

            DoRequest(url, HttpMethod.Post, obj);
        }

        /// <summary>
        /// Creates a new Jira webhook which triggers on the specified event(s) if the event
        /// meets optional filters specified in the scope. When the webhook is triggered, the
        /// request will be sent to the <see cref="JiraWebhookController"/>.
        /// </summary>
        /// <param name="infoObj">The Xperience object which triggered webhook creation and contains
        /// Jira issue and project information in a database column.</param>
        /// <param name="name">The display name for the created webhook.</param>
        /// <param name="events">One or more events which will cause the webhook to trigger.
        /// See <see href="https://developer.atlassian.com/server/jira/platform/webhooks/#registering-events-for-a-webhook"/>.</param>
        /// <param name="scope">One or more filters which are used to restrict when the webhook triggers.
        /// See <see href="https://confluence.atlassian.com/jirasoftwareserver/advanced-searching-939938733.html#Advancedsearching-ConstructingJQLqueries"/>.</param>
        /// <returns>The response from the Jira server after webhook creation, containing information
        /// about the created webhook.</returns>
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
        /// Creates a new comment on a Jira issue using the identity of the <see cref="User"/>.
        /// </summary>
        /// <param name="issueId">The internal ID of the issue to comment on.</param>
        /// <param name="comment">The comment to add to the issue.</param>
        public void AddComment(string issueId, string comment)
        {
            if (!IsEnabled)
            {
                throw new Exception("Jira integration is not enabled.");
            }

            comment = HttpUtility.JavaScriptStringEncode(HTMLHelper.HTMLDecode(comment));

            var data = JObject.Parse($"{{ body:\"{comment}\" }}");
            var url = string.Format(ADD_COMMENT, issueId);
            DoRequest(url, HttpMethod.Post, data);
        }

        /// <summary>
        /// Creates a new issue in Jira.
        /// </summary>
        /// <param name="metaFields">The fields of the issue to be set.</param>
        /// <param name="projectId">The internal project ID to create the issue under.</param>
        /// <param name="issueTypeId">The internal ID of the issue type to create.</param>
        /// <param name="resolver">A resolver used to resolve macros in the metadata.</param>
        /// <returns>The response from the Jira server after the issue is created, containing
        /// information about the created issue.</returns>
        public string CreateIssue(Hashtable metaFields, string projectId, string issueTypeId, MacroResolver resolver)
        {
            if (!IsEnabled)
            {
                throw new Exception("Jira integration is not enabled.");
            }

            var data = $"{{ fields: {{ project: {{ id: \"{projectId}\" }}, issuetype: {{id: \"{issueTypeId}\" }} }}";
            var obj = JsonConvert.DeserializeObject<JObject>(data);

            var selectedProject = GetProjectWithCreateSchema(projectId, issueTypeId);
            var issueFields = selectedProject.IssueTypes.Where(i => i.Id == issueTypeId).FirstOrDefault().GetFields();

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
        /// Deletes a Jira webhook.
        /// </summary>
        /// <param name="webhookId">The internal ID of the webhook to delete.</param>
        public static void DeleteWebhook(string webhookId)
        {
            var url = WebhookBaseUrl + $"/{webhookId}";
            DoRequest(url, HttpMethod.Delete);
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
                        throw new Exception($"Error posting to {url}: {content}.");
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
        /// or an automation's <see cref="AutomationStateInfo.StateCustomData"/>.
        /// </summary>
        /// <param name="infoObj">The Xperience object which contains a custom data column to store
        /// Jira information within.</param>
        /// <param name="issueId">The internal ID of the issue to store in the object.</param>
        /// <param name="projectId">The internal ID of the project to store in the object.</param>
        public static void LinkJiraIssue(BaseInfo infoObj, string issueId, string projectId)
        {
            if (infoObj is AutomationStateInfo)
            {
                var state = infoObj as AutomationStateInfo;
                state.StateCustomData[LinkedIssueKey] = issueId;
                state.StateCustomData[LinkedProjectKey] = projectId;
                state.Update();
            }
            if (infoObj is TreeNode)
            {
                var node = infoObj as TreeNode;
                node.DocumentCustomData[LinkedIssueKey] = issueId;
                node.DocumentCustomData[LinkedProjectKey] = projectId;
                node.Update();
            }
        }

        /// <summary>
        /// Adds a Jira webhook ID to a TreeNode's <see cref="TreeNode.DocumentCustomData"/>
        /// or an automation's <see cref="AutomationStateInfo.StateCustomData"/>.
        /// </summary>
        /// <param name="infoObj">The Xperience object which contains a custom data column to store
        /// Jira information within.</param>
        /// <param name="webhookId">The internal ID of the webhook to store in the object.</param>
        public static void LinkJiraWebhook(BaseInfo infoObj, string webhookId)
        {
            if (infoObj is AutomationStateInfo)
            {
                var state = infoObj as AutomationStateInfo;
                state.StateCustomData[LinkedWebhookKey] = webhookId;
                state.Update();
            }
            if (infoObj is TreeNode)
            {
                var node = infoObj as TreeNode;
                node.DocumentCustomData[LinkedWebhookKey] = webhookId;
                node.Update();
            }
        }

        /// <summary>
        /// Returns the Jira webhook ID that is linked to the object.
        /// </summary>
        /// <param name="infoObj">The Xperience object which contains a custom data column storing
        /// a Jira webhook ID.</param>
        /// <returns>A Jira webhook ID, or null.</returns>
        public static string GetLinkedWebhook(BaseInfo infoObj)
        {
            if (infoObj is AutomationStateInfo)
            {
                var state = infoObj as AutomationStateInfo;
                return ValidationHelper.GetString(state.StateCustomData[LinkedWebhookKey], "");
            }
            if (infoObj is TreeNode)
            {
                var node = infoObj as TreeNode;
                return ValidationHelper.GetString(node.DocumentCustomData[LinkedWebhookKey], "");
            }

            return null;
        }

        /// <summary>
        /// Returns the Jira issue ID that is linked to the object.
        /// </summary>
        /// <param name="infoObj">The Xperience object which contains a custom data column storing
        /// a Jira issue ID.</param>
        /// <returns>A Jira issue ID, or null.</returns>
        public static string GetLinkedIssue(BaseInfo infoObj)
        {
            if (infoObj is AutomationStateInfo)
            {
                var state = infoObj as AutomationStateInfo;
                return ValidationHelper.GetString(state.StateCustomData[LinkedIssueKey], "");
            }
            if (infoObj is TreeNode)
            {
                var node = infoObj as TreeNode;
                return ValidationHelper.GetString(node.DocumentCustomData[LinkedIssueKey], "");
            }

            return null;
        }

        /// <summary>
        /// Returns the Jira project ID that is linked to the object.
        /// </summary>
        /// <param name="infoObj">The Xperience object which contains a custom data column storing
        /// a Jira project ID.</param>
        /// <returns>A Jira project ID, or null.</returns>
        public static string GetLinkedProject(BaseInfo infoObj)
        {
            if (infoObj is AutomationStateInfo)
            {
                var state = infoObj as AutomationStateInfo;
                return ValidationHelper.GetString(state.StateCustomData[LinkedProjectKey], "");
            }
            if (infoObj is TreeNode)
            {
                var node = infoObj as TreeNode;
                return ValidationHelper.GetString(node.DocumentCustomData[LinkedProjectKey], "");
            }

            return null;
        }

        #endregion
    }
}