using CMS.Automation;
using CMS.ContactManagement;
using CMS.EventLog;
using CMS.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text;

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
                throw new InvalidOperationException("Project or issue type was not found in the automation step configuration.");
            }

            try
            {
                // Convert stored metadata string into properties for creation
                var selectedProject = JiraApiHelper.GetProjectWithCreateSchema(project, issueType);
                if (selectedProject == null)
                {
                    return;
                }

                var issueFields = selectedProject.IssueTypes.Where(i => i.Id == issueType).FirstOrDefault().GetFields();
                var properties = JiraHelper.CreateIssueProperties(issueFields, metadata, this.MacroResolver);

                // Add description to properties
                var contact = InfoObject as ContactInfo;
                var description = new StringBuilder();
                foreach (var key in contact.ColumnNames)
                {
                    var value = ValidationHelper.GetString(contact.GetProperty(key), "");
                    if (String.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    description.Append($"\\\\*{key}:* {value}");
                }
                properties.Add(new JProperty("description", description.ToString()));


                var response = JiraApiHelper.CreateIssue(properties, project, issueType, User);
                var content = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    var createdIssue = JObject.Parse(content).Value<string>("id");
                    JiraHelper.LinkJiraIssue(StateObject, createdIssue, project);
                }
                else
                {
                    LogMessage(EventType.ERROR, nameof(Execute), content, StateObject);
                }
            }
            catch (ArgumentNullException ex)
            {
                LogMessage(EventType.ERROR, nameof(Execute), ex.Message, StateObject);
            }

        }
    }
}
