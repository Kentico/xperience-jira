using CMS.Automation;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.MacroEngine;
using Kentico.Xperience.Jira.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Kentico.Xperience.Jira
{
    /// <summary>
    /// Provides methods for common Jira related tasks.
    /// </summary>
    public class JiraHelper
    {
        /// <summary>
        /// The key for storing and retriving an object's linked Jira webhook
        /// ID from its custom data column.
        /// </summary>
        private static string LinkedWebhookKey
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
        private static string LinkedProjectKey
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
        private static string LinkedIssueMacro
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
        private static string LinkedProjectMacro
        {
            get
            {
                return "##LinkedProject##";
            }
        }

        /// <summary>
        /// Returns whether the Jira integration is enabled in the Xperience Settings application.
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
                return SettingsKeyInfoProvider.GetBoolValue("EnableJira");
            }
        }

        /// <summary>
        /// Converts a string of key/value pairs in the format "key;value|key;value" into a list of
        /// <see cref="JProperty"/> objects for Jira issue creation.
        /// </summary>
        /// <param name="issueFields">The issue type's fields with their schema.</param>
        /// <param name="metadata">The stored metadata string from the database.</param>
        /// <param name="resolver">A macro resolver to resolve field values.</param>
        /// <exception cref="ArgumentNullException">Thrown if the Jira issue type fields couldn't be loaded,
        /// or a <see cref="MacroResolver"/> wasn't provided.</exception>
        public static List<JProperty> CreateIssueProperties(IEnumerable<JiraIssueField> issueFields, string metadata, MacroResolver resolver)
        {
            if (issueFields == null)
            {
                throw new ArgumentNullException(nameof(issueFields));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            var properties = new List<JProperty>();
            var pairs = metadata.Split('|');

            foreach (string pair in pairs)
            {
                var values = pair.Split(';');
                var key = values[0];
                var value = ValidationHelper.GetString(values[1], "");
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
                        properties.Add(new JProperty(key, jObject));
                    }
                    else
                    {
                        properties.Add(new JProperty(key, value));
                    }
                }
            }

            return properties;
        }

        /// <summary>
        /// Converts one or more JQL queries (separated by a newline) into an object suitable
        /// for webhook creation.
        /// </summary>
        /// <remarks>See <see href="https://confluence.atlassian.com/jirasoftwareserver/advanced-searching-939938733.html#Advancedsearching-ConstructingJQLqueries"/></remarks>
        /// <param name="infoObj">The Xperience object that will be linked to the webhook, for
        /// retrieving the linked issue and project.</param>
        /// <param name="scope">A string containing JQL queries in the format "section:query\r\nsection:query."</param>
        /// <exception cref="InvalidOperationException">Thrown if the scope contains macros for linked Jira data, but the
        /// data wasn't found.</exception>
        public static JObject GetFiltersFromScope(BaseInfo infoObj, string scope)
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
                        throw new InvalidOperationException($"Jira webhook scopes contained a filter with '{LinkedIssueMacro}', but no linked issue was found.");
                    }

                    value = value.Replace(LinkedIssueMacro, GetLinkedIssue(infoObj));
                }
                if (value.Contains(LinkedProjectMacro))
                {
                    if (String.IsNullOrEmpty(GetLinkedProject(infoObj)))
                    {
                        throw new InvalidOperationException($"Jira webhook scopes contained a filter with '{LinkedProjectMacro}', but no linked project was found.");
                    }

                    value = value.Replace(LinkedProjectMacro, GetLinkedProject(infoObj));
                }

                filters.Add(new JProperty(arr[0], value));
            }

            return filters;
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
    }
}