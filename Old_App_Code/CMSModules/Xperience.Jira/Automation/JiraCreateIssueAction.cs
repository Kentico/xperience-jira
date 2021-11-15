using CMS.Automation;
using CMS.ContactManagement;
using CMS.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;

namespace Xperience.Jira.Automation
{
    /// <summary>
    /// A Marketing automation action which creates a new issue in Jira. The created issue
    /// and its project are linked to the <see cref="AutomationStateInfo"/> via
    /// <see cref="JiraHelper.LinkJiraIssue(CMS.DataEngine.BaseInfo, string, string)"
    /// </summary>
    public class JiraCreateIssueAction : AutomationAction
    {
        public override void Execute()
        {
            var project = GetResolvedParameter("Project", "");
            var issueType = GetResolvedParameter("IssueType", "");
            var metadata = GetResolvedParameter("MetaFields", "");

            if (!String.IsNullOrEmpty(project) && !String.IsNullOrEmpty(issueType))
            {
                var metaFields = new Hashtable();
                foreach (var pair in metadata.Split('|'))
                {
                    var values = pair.Split(';');
                    metaFields[values[0]] = values[1];
                }

                // Create description
                var contact = InfoObject as ContactInfo;
                var description = "";
                foreach (var key in contact.ColumnNames)
                {
                    var value = ValidationHelper.GetString(contact.GetProperty(key), "");
                    if (String.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    description += $"\\\\*{key}:* {value}";
                }
                metaFields["description"] = description;

                var jiraHelper = new JiraHelper(User);
                var response = jiraHelper.CreateIssue(metaFields, project, issueType, this.MacroResolver);
                var createdIssue = JObject.Parse(response).Value<string>("id");

                JiraHelper.LinkJiraIssue(StateObject, createdIssue, project);
            }
            else
            {
                throw new NullReferenceException("Project or issue type was not found in the automation step configuration.");
            }
        }
    }
}
