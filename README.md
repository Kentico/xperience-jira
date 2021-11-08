[![Nuget](https://img.shields.io/nuget/v/Xperience.Jira)](https://www.nuget.org/packages/Xperience.Jira) [![Stack Overflow](https://img.shields.io/badge/Stack%20Overflow-ASK%20NOW-FE7A16.svg?logo=stackoverflow&logoColor=white)](https://stackoverflow.com/tags/kentico)

# Xperience Jira integration

This package contains custom [Workflow](https://docs.xperience.io/managing-website-content/working-with-pages/using-workflows) and [Marketing automation](https://docs.xperience.io/on-line-marketing-features/managing-your-on-line-marketing-features/marketing-automation) actions for managing Jira issues. [Jira webhooks](https://developer.atlassian.com/server/jira/platform/webhooks/) can also be created automatically to manage Xperience objects when actions are performed in Jira.

## Installing and enabling

1. Download the export package located in the [CMSSiteUtils/Export](/CMSSiteUtils/Export) folder
2. In the __Sites application__, [import](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects) the package
3. Go to __Modules > Membership > Classes tab > User class__
4. Add a new field named __JiraApiToken__:

![User class](/assets/user-module.png)

5. Logged into Jira as an admin, go to https://id.atlassian.com/manage-profile/security/api-tokens and generate a new API token
6. In the __Users application__ locate the default _Global Administrator_ account whose ID is set in __Settings > System > Default user ID__
7. Ensure that the user's email matches the Jira admin's email, and save the generated API token on the __Custom fields tab__
8. (optional) Repeat this process for other Xperience users by having them generate and API token and ensuring their emails match. This allows Jira actions like creating new issues and commenting appear under the correct account. Otherwise, they will be created using the Global Administrator's account

### Settings

To finish setting up the integration, go to the __Settings application > Integration > Jira__ section:

- __Enabled__: This can be unchecked to temporarily prevent the creation of new Jira issues, webhooks, etc.
- __Delete webhooks after first run__: By default, webhooks created by this integration are deleted when they execute for the first time. If this is unchecked, webhooks will remain active in Jira
- __Server__: The base URL of your Jira server, e.g. _https://xperience.atlassian.net_

## Custom actions

There are 3 custom actions provided by this package for both [Workflows](https://docs.xperience.io/managing-website-content/working-with-pages/using-workflows) and [Marketing automation processes](https://docs.xperience.io/on-line-marketing-features/managing-your-on-line-marketing-features/marketing-automation):

- __Create Jira issue__
- __Create Jira webhook__
- __Jira transition workflow__

### Creating Jira issues

To create a new Jira issue, add the __Create Jira issue__ step to your workflow or automation process. When this step is reached, a new Jira issue will be created and the workflow will continue to the next step.

You will need to select the appropriate __Project__ and __Issue type__ for the new issue using the drop-downs. Multiple fields will then appear under the __Fields__ property which you can use to specify the values for the new issue:

![Create issue](/assets/create-issue.png)

The __Summary__ field (the issue title) is required, and your company may have added custom fields to the issue type that are required as well. You can use macros in these fields such as `{%NodeAliasPath%}` which will be resolved according to the page that is using the workflow.

> :warning: Macros in issue fields only resolve properly for the __Workflow__ action. When adding the __Create Jira issue__ to Marketing automation processes, macros cannot be used.

#### Linking issues and objects

Using the __Create Jira issue__ action "links" the page or automation process to the newly-created Jira issue and its project. This is done by storing the IDs of the issue and project in the `DocumentCustomData` column for workflows and `StateCustomData` for automation processes. If you need to retrieve these values for any reason (e.g. within a custom handler), you can use [`JiraHelper.GetLinkedIssue()`](/Old_App_Code/CMSModules/Xperience.Jira/JiraHelper.cs#L562) or [`JiraHelper.GetLinkedProject()`](/Old_App_Code/CMSModules/Xperience.Jira/JiraHelper.cs#L583).

### Creating webhooks

The __Create Jira webhook__ action can be used to create new webhooks which appear in Jira under __Settings > System > WebHooks__. These webhooks will execute when the specified action (provided by you) is taken in Jira. By default, the page/process will be moved to the next step, and the Jira webhook will be deleted. However, you can override the default behavior by registering an [event handler](#custom-webhook-handling).

There are 2 required properties and one optional property:

![Create webhook](/assets/create-webhook.png)

- __Name__: An arbirary name for the webhook which will appear in Jira
- __Events__: A comma-separated list of Jira events that will trigger the webhook. See [Registering events for a webhook](https://developer.atlassian.com/server/jira/platform/webhooks/#registering-events-for-a-webhook) for a list of valid events
- __Scope__ (optional): One or more JQL queries that will filter when the webhook triggers. For example, the `jira:issue_updated` event will trigger for all issues unless you specify certain issues, projects, resolutions, etc.. See [Constructing JQL queries](https://confluence.atlassian.com/jirasoftwareserver/advanced-searching-939938733.html#Advancedsearching-ConstructingJQLqueries) for more information on writing queries  
If the page/process has been [linked](#linking-issues-and-objects) to an issue, you can use the macros `##LinkedIssue##` and `##LinkedProject##` in the scope to reference the linked values

When the webhook is created, the workflow will move to the next step. If you'd like the current workflow to "wait" for the webhook to trigger, add a __Standard__ step after it for workflows or __Approve progress__ for automation processes. Take the following Marketing automation process as an example:

![Process example](/assets/process-example.png)

Your website has a "Contact us" form which your customers submit with questions about your products. This automation process will create a new issue for your marketers to call the customer, then create a webhook with the following properties:

- __Events__: _jira:issue_updated_
- __Scope__: _issue-related-events-section:issue = ##LinkedIssue## AND resolution = Done_

The process will stop at the "Wait for webhook" step. When the linked Jira issue is moved to a step with the "Done" resolution, the webhook will trigger and the process will be moved to the next step, setting a custom contact property and then finishing.

#### Custom webhook handling

If you want to prevent the default webhook functionality which moves the workflow to the next step, or run your own code before/after that occurs, you can register [custom event handlers](https://docs.xperience.io/custom-development/handling-global-events):

- __JiraEvents.WebhookTriggered.Before__
- __JiraEvents.WebhookTriggered.After__

The event handlers are provided with the body of the webhook request, the linked object of the workflow (a `TreeNode` or `AutomationStateInfo`), and the ID of the Jira issue which triggered it. You can prevent the default webhook functionality in the `Before` handler by calling `Cancel()`:

```cs
JiraEvents.WebhookTriggered.Before += (object sender, WebhookTriggeredArgs e) =>
{
    // Your code here
    e.Cancel();
};
```