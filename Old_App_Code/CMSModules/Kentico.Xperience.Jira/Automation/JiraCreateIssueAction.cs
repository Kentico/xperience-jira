using CMS.Automation;
using CMS.ContactManagement;
using CMS.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Linq;

namespace Kentico.Xperience.Jira.Automation
{
    /// <summary>
    /// A Marketing automation action which creates a new issue in Jira. The created issue
    /// and its project are linked to the <see cref="AutomationStateInfo"/> via
    /// <see cref="JiraHelper.LinkJiraIssue(CMS.DataEngine.BaseInfo, string, string)"/>.
    /// </summary>
    public class JiraCreateIssueAction : AutomationAction
    {
        public override void Execute()
        {
            var project = GetResolvedParameter("Project", "");
            var issueType = GetResolvedParameter("IssueType", "");
            var metadata = GetResolvedParameter("MetaFields", "");

            if (String.IsNullOrEmpty(project) || String.IsNullOrEmpty(issueType))
            {
                throw new NullReferenceException("Project or issue type was not found in the automation step configuration.");
            }

            // Convert stored metadata string into properties for creation
            var selectedProject = JiraApiHelper.GetProjectWithCreateSchema(project, issueType);
            var issueFields = selectedProject.IssueTypes.Where(i => i.Id == issueType).FirstOrDefault().GetFields();
            var properties = JiraHelper.CreateIssueProperties(issueFields, metadata, this.MacroResolver);

            // Add description to properties
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
            properties.Add(new JProperty("description", description));

            
            var response = JiraApiHelper.CreateIssue(properties, project, issueType, User);
            var createdIssue = JObject.Parse(response).Value<string>("id");

            JiraHelper.LinkJiraIssue(StateObject, createdIssue, project);
        }
    }
}
