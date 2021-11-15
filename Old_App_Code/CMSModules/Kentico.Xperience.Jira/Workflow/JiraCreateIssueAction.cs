using CMS.DocumentEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;

namespace Kentico.Xperience.Jira.Workflow
{
    /// <summary>
    /// A workflow action which creates a new issue in Jira. The created issue
    /// and its project are linked to the <see cref="TreeNode"/> via
    /// <see cref="JiraHelper.LinkJiraIssue(CMS.DataEngine.BaseInfo, string, string)"
    /// </summary>
    public class JiraCreateIssueAction : DocumentWorkflowAction
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

                var jiraHelper = new JiraHelper(User);
                var response = jiraHelper.CreateIssue(metaFields, project, issueType, this.MacroResolver);
                var createdIssue = JObject.Parse(response).Value<string>("id");

                JiraHelper.LinkJiraIssue(Node, createdIssue, project);

                // Get workflow comment
                var comment = String.IsNullOrEmpty(Comment) ? $"Issue created automatically by Xperience workflow '{Workflow.WorkflowDisplayName}' for page '{Node.NodeAliasPath}'" : Comment;
                comment = this.MacroResolver.ResolveMacros(comment);
                jiraHelper.AddComment(createdIssue, comment);
            }
            else
            {
                throw new NullReferenceException("Project or issue type was not found in the workflow step configuration.");
            }
        }
    }
}
