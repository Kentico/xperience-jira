using CMS.Automation;
using CMS.Membership;
using System;

namespace Kentico.Xperience.Jira.Automation
{
    /// <summary>
    /// A Marketing automation action which moves a Jira issue to a new workflow step. Logs a
    /// new Jira comment to the issue if the Xperience user provided a comment, or logs a default
    /// comment.
    /// </summary>
    public class JiraTransitionAction : AutomationAction
    {
        public override void Execute()
        {
            var issueId = JiraHelper.GetLinkedIssue(StateObject);
            var transition = GetResolvedParameter("Transition", "");

            if (String.IsNullOrEmpty(issueId) || String.IsNullOrEmpty(transition))
            {
                throw new NullReferenceException("Linked Jira issue not found in the automation state's custom data.");
            }

            var user = User;
            var comment = $"Workflow changed automatically by Xperience workflow '{Workflow.WorkflowDisplayName}.'";

            // Get user and comment from history
            var previousStepHistoryId = AutomationManager.GetPreviousStepInfo(InfoObject, StateObject).RelatedHistoryID;
            var history = AutomationHistoryInfo.Provider.Get(previousStepHistoryId);
            if (history != null)
            {
                user = UserInfo.Provider.Get(history.HistoryApprovedByUserID);
                if (!String.IsNullOrEmpty(history.HistoryComment))
                {
                    comment = history.HistoryComment;
                }
            }

            var jiraHelper = new JiraHelper(user);
            comment = this.MacroResolver.ResolveMacros(comment);

            jiraHelper.AddComment(issueId, comment);
            jiraHelper.DoTransition(issueId, transition);
        }
    }
}