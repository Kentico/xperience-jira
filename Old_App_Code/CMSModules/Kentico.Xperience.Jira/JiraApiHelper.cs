using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;
using CMS.SiteProvider;
using Kentico.Xperience.Jira.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;

namespace Kentico.Xperience.Jira
{
    /// <summary>
    /// Provides methods for interfacing with the Jira REST API.
    /// </summary>
    public class JiraApiHelper
    {
        private static readonly string DO_TRANSITION = BaseUrl + "issue/{0}/transitions";
        private static readonly string ADD_COMMENT = BaseUrl + "issue/{0}/comment";
        private static readonly string CREATE_POST = BaseUrl + "issue";

        private static readonly string GET_PROJECTS = BaseUrl + "project";
        private static readonly string GET_ISSUE_TYPES = BaseUrl + "issue/createmeta?projectIds={0}";
        private static readonly string GET_ISSUE_CREATE_META = BaseUrl + "issue/createmeta?projectIds={0}&issuetypeIds={1}&expand=projects.issuetypes.fields";
        private static readonly string GET_ISSUES = BaseUrl + "issue/picker?query={0}&currentProjectId={1}";
        private static readonly string GET_TRANSITIONS = BaseUrl + "issue/{0}/transitions";

        /// <summary>
        /// The Jira server domain and path for standard Jira REST requests.
        /// </summary>
        private static string BaseUrl
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
        private static string WebhookBaseUrl
        {
            get
            {
                var jiraDomain = SettingsKeyInfoProvider.GetValue("JiraServer");
                return $"{jiraDomain}/rest/webhooks/1.0/webhook";
            }
        }

        private static string GetBasicAuthorization(UserInfo user = null)
        {
            if(user == null)
            {
                user = UserInfoProvider.AdministratorUser;
            }

            var email = user.Email;
            var token = user.GetValue("JiraApiToken", "");
            if (String.IsNullOrEmpty(email) || String.IsNullOrEmpty(token))
            {
                // Provided user doesn't have these properties set, try to get from Global Administrator
                user = UserInfoProvider.AdministratorUser;
                email = user.Email;
                token = user.GetValue("JiraApiToken", "");
            }

            if (String.IsNullOrEmpty(email) || String.IsNullOrEmpty(token))
            {
                throw new NullReferenceException("The email and/or JiraApiToken properties have not been set.");
            }

            var bytes = Encoding.UTF8.GetBytes($"{email}:{token}");
            return Convert.ToBase64String(bytes);
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
            var url = String.Format(GET_TRANSITIONS, issueId);
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
            var url = String.Format(GET_ISSUES, query, projectId);
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
            var url = String.Format(GET_ISSUE_CREATE_META, projectId, issueTypeId);
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
            var url = String.Format(GET_ISSUE_TYPES, projectId);
            var response = JObject.Parse(DoRequest(url, HttpMethod.Get));
            var projectToken = response.SelectToken("$.projects");
            var projects = JsonConvert.DeserializeObject<IEnumerable<JiraProject>>(JsonConvert.SerializeObject(projectToken));

            return projects.Where(p => p.Id == projectId).FirstOrDefault();
        }

        /// <summary>
        /// Attempts to transition a Jira issue to a new workflow step. This may fail
        /// if the Jira issue has been moved into a workflow step in which the transition
        /// is no longer valid.
        /// </summary>
        /// <param name="issueId">The internal ID of the issue to transition.</param>
        /// <param name="transitionId">The internal ID of the transition to use.</param>
        public static void DoTransition(string issueId, string transitionId, UserInfo user)
        {
            if (!JiraHelper.IsEnabled)
            {
                throw new Exception("Jira integration is not enabled.");
            }

            var data = $"{{ transition: {{ id:\"{transitionId}\" }} }}";
            var obj = JsonConvert.DeserializeObject<JObject>(data);
            var url = String.Format(DO_TRANSITION, issueId);

            DoRequest(url, HttpMethod.Post, user, obj);
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
                obj["filters"] = JiraHelper.GetFiltersFromScope(infoObj, scope);
            }

            return DoRequest(WebhookBaseUrl, HttpMethod.Post, data: obj);
        }

        /// <summary>
        /// Creates a new comment on a Jira issue using the identity of the <see cref="User"/>.
        /// </summary>
        /// <param name="issueId">The internal ID of the issue to comment on.</param>
        /// <param name="comment">The comment to add to the issue.</param>
        /// <param name="user">The Xperience user whose API token will be used in the request.</param>
        public static void AddComment(string issueId, string comment, UserInfo user)
        {
            if (!JiraHelper.IsEnabled)
            {
                throw new Exception("Jira integration is not enabled.");
            }

            comment = HttpUtility.JavaScriptStringEncode(HTMLHelper.HTMLDecode(comment));

            var data = JObject.Parse($"{{ body:\"{comment}\" }}");
            var url = String.Format(ADD_COMMENT, issueId);

            DoRequest(url, HttpMethod.Post, user, data);
        }

        /// <summary>
        /// Creates a new issue in Jira.
        /// </summary>
        /// <param name="properties">The fields of the issue to be set.</param>
        /// <param name="projectId">The internal project ID to create the issue under.</param>
        /// <param name="issueTypeId">The internal ID of the issue type to create.</param>
        /// <param name="user">The Xperience user whose API token will be used in the request.</param>
        /// <returns>The response from the Jira server after the issue is created, containing
        /// information about the created issue.</returns>
        public static string CreateIssue(IEnumerable<JProperty> properties, string projectId, string issueTypeId, UserInfo user)
        {
            if (!JiraHelper.IsEnabled)
            {
                throw new Exception("Jira integration is not enabled.");
            }

            var data = $"{{ fields: {{ project: {{ id: \"{projectId}\" }}, issuetype: {{id: \"{issueTypeId}\" }} }}";
            var obj = JsonConvert.DeserializeObject<JObject>(data);

            foreach(var field in properties)
            {
                obj["fields"][field.Name] = field.Value;
            }

            return DoRequest(CREATE_POST, HttpMethod.Post, user, obj);
        }

        /// <summary>
        /// Deletes a Jira webhook.
        /// </summary>
        /// <param name="webhookId">The internal ID of the webhook to delete.</param>
        public static void DeleteWebhook(string webhookId)
        {
            var url = WebhookBaseUrl + $"/{webhookId}";
            DoRequest(url, HttpMethod.Delete);
        }

        private static string DoRequest(string url, HttpMethod method, UserInfo user = null, JObject data = null)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Authorization", $"Basic {GetBasicAuthorization(user)}");

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
                        throw new Exception($"Error requesting {url}: {content}.");
                    }
                }
            }

            return String.Empty;
        }
    }
}