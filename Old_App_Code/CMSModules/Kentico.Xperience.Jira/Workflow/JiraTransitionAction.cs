using CMS.DocumentEngine;
using System;

namespace Kentico.Xperience.Jira.Workflow
{
    /// <summary>
    /// A workflow action which moves a Jira issue to a new workflow step. Logs a new
    /// Jira comment to the issue if the Xperience user provided a comment, or logs a default
    /// comment
    /// </summary>
    public class JiraTransitionAction : DocumentWorkflowAction
    {
        public override void Execute()
        {
            var issueId = JiraHelper.GetLinkedIssue(Node);
            var transition = GetResolvedParameter("Transition", "");

            if (!String.IsNullOrEmpty(issueId) && !String.IsNullOrEmpty(transition))
            {
                var jiraHelper = new JiraHelper(User);
                var comment = String.IsNullOrEmpty(Comment) ? $"Workflow changed automatically by Xperience workflow '{Workflow.WorkflowDisplayName}' for page '{Node.NodeAliasPath}'" : Comment;
                comment = this.MacroResolver.ResolveMacros(comment);

                jiraHelper.AddComment(issueId, comment);
                jiraHelper.DoTransition(issueId, transition);
            }
            else
            {
                throw new NullReferenceException("Linked Jira issue not found in the document's custom data");
            }
        }
    }
}