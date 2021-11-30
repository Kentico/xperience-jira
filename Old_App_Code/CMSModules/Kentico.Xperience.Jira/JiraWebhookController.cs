using CMS;
using CMS.Automation;
using CMS.Base;
using CMS.ContactManagement;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.Membership;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Kentico.Xperience.Jira.Events;

namespace Kentico.Xperience.Jira
{
    /// <summary>
    /// A Web API controller for handling webhook POSTS from Jira.
    /// </summary>
    public class JiraWebhookController : ApiController
    {
        /// <summary>
        /// The endpoint which Jira POSTS to when actions are performed in Jira, as specified by
        /// <see cref="JiraHelper.GetCallbackUrl(BaseInfo)"/>.
        /// </summary>
        /// <remarks>
        /// The default functionality of this
        /// endpoint moves the corresponding object to the next step in its workflow, but the functionality
        /// can be modified by registering a custom <see cref="JiraEvents.WebhookTriggered"/> handler.
        /// </remarks>
        /// <param name="type">The object class name that was registered during creation of the webhook.</param>
        /// <param name="issueId">The internal ID of the Jira issue which triggered the webhook.</param>
        [HttpPost]
        public HttpResponseMessage Run(string type, string issueId)
        {
            var deleteWebhook = SettingsKeyInfoProvider.GetBoolValue("JiraDeleteWebhooks");
            var content = JObject.Parse(Request.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult());

            BaseInfo infoObj = null;
            if (type.ToLower() == TreeNode.TYPEINFO.ObjectClassName.ToLower())
            {
                infoObj = GetRelatedTreeNode(issueId);
            }
            else if (type.ToLower() == AutomationStateInfo.TYPEINFO.ObjectClassName.ToLower())
            {
                infoObj = GetRelatedAutomationState(issueId);
            }

            using (var h = JiraEvents.WebhookTriggered.StartEvent(content, infoObj, issueId))
            {
                if (h.CanContinue())
                {
                    if (infoObj is TreeNode)
                    {
                        var webhook = MovePageToNextStep(infoObj as TreeNode);
                        if (!String.IsNullOrEmpty(webhook) && deleteWebhook)
                        {
                            JiraApiHelper.DeleteWebhook(webhook);
                        }
                    }
                    else if (infoObj is AutomationStateInfo)
                    {
                        var webhook = MoveProcessToNextStep(infoObj as AutomationStateInfo);
                        if (!String.IsNullOrEmpty(webhook) && deleteWebhook)
                        {
                            JiraApiHelper.DeleteWebhook(webhook);
                        }
                    }
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private AutomationStateInfo GetRelatedAutomationState(string issue)
        {
            return AutomationStateInfo.Provider.Get()
                .WhereLike(nameof(AutomationStateInfo.StateCustomData), $"%<{JiraHelper.LinkedIssueKey}>{issue}</{JiraHelper.LinkedIssueKey}>%")
                .TopN(1)
                .TypedResult
                .FirstOrDefault();
        }

        private TreeNode GetRelatedTreeNode(string issue)
        {
            return DocumentHelper.GetDocuments()
                .OnCurrentSite()
                .WhereLike(nameof(TreeNode.DocumentCustomData), $"%<{JiraHelper.LinkedIssueKey}>{issue}</{JiraHelper.LinkedIssueKey}>%")
                .TopN(1)
                .TypedResult
                .FirstOrDefault();
        }

        private string MoveProcessToNextStep(AutomationStateInfo state)
        {
            if (state != null)
            {
                var manager = AutomationManager.GetInstance(UserInfoProvider.AdministratorUser);
                var contact = ContactInfo.Provider.Get(state.StateObjectID);

                manager.MoveToNextStep(contact, state, "Process moved to next step by Jira webhook");
                return JiraHelper.GetLinkedWebhook(state);
            }

            return string.Empty;
        }

        private string MovePageToNextStep(TreeNode node)
        {
            if (node != null)
            {
                using (new CMSActionContext(UserInfoProvider.AdministratorUser))
                {
                    node.MoveToNextStep("Workflow moved to next step by Jira webhook");
                    return JiraHelper.GetLinkedWebhook(node);
                }
            }

            return string.Empty;
        }
    }
}