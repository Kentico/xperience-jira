using CMS.DocumentEngine;
using CMS.EventLog;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Kentico.Xperience.Jira.Workflow
{
    /// <summary>
    /// A workflow action which creates a new issue in Jira. The created issue
    /// and its project are linked to the <see cref="TreeNode"/> via
    /// <see cref="JiraHelper.LinkJiraIssue(CMS.DataEngine.BaseInfo, string, string)"/>.
    /// </summary>
    public class JiraCreateIssueAction : DocumentWorkflowAction
    {
        public override void Execute()
        {
            var project = GetResolvedParameter("Project", "");
            var issueType = GetResolvedParameter("IssueType", "");
            var metadata = GetResolvedParameter("MetaFields", "");

            if (String.IsNullOrEmpty(project) || String.IsNullOrEmpty(issueType))
            {
                throw new InvalidOperationException("Project or issue type was not found in the workflow step configuration.");
            }

            // Convert stored metadata string into properties for creation
            var selectedProject = JiraApiHelper.GetProjectWithCreateSchema(project, issueType);
            var issueFields = selectedProject.IssueTypes.Where(i => i.Id == issueType).FirstOrDefault().GetFields();
            var properties = JiraHelper.CreateIssueProperties(issueFields, metadata, this.MacroResolver);

            var response = JiraApiHelper.CreateIssue(properties, project, issueType, User);
            var content = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                var createdIssue = JObject.Parse(content).Value<string>("id");
                JiraHelper.LinkJiraIssue(StateObject, createdIssue, project);

                // Get workflow comment
                var comment = String.IsNullOrEmpty(Comment) ? $"Issue created automatically by Xperience workflow '{Workflow.WorkflowDisplayName}' for page '{Node.NodeAliasPath}.'" : Comment;
                comment = this.MacroResolver.ResolveMacros(comment);
                JiraApiHelper.AddComment(createdIssue, comment, User);
            }
            else
            {
                LogMessage(EventType.ERROR, nameof(Execute), content, Node);
            }
        }
    }
}
