[![Stack Overflow](https://img.shields.io/badge/Stack%20Overflow-ASK%20NOW-FE7A16.svg?logo=stackoverflow&logoColor=white)](https://stackoverflow.com/tags/kentico)

# Kentico Xperience Jira integration module

This repository contains the source code of the custom Kentico Xperience [Workflow](https://docs.xperience.io/managing-website-content/working-with-pages/using-workflows) and [Marketing automation](https://docs.xperience.io/on-line-marketing-features/managing-your-on-line-marketing-features/marketing-automation) actions for managing [Jira issues](https://support.atlassian.com/jira-software-cloud/docs/what-is-an-issue/) and creating [Jira webhooks](https://developer.atlassian.com/server/jira/platform/webhooks/). Jira webhooks can be used to manage Xperience objects when specific actions are performed in Jira.

The module is supported for Kentico Xperience 13.

## Requirements and prerequisites

* *Kentico Xperience 13* installed. Both ASP.NET Core and ASP.NET MVC 5 development models are supported.
* *Xperience Enterprise* license edition for your site, as the integration uses on-line marketing features (e.g. marketing automation).
* The *Enable on-line marketing* setting needs to be selected in the *Settings* application.
* You need to have a [Jira (Atlassian)](https://www.atlassian.com/software/jira) account with admin permissions (you need to be able to create new [API tokens](https://id.atlassian.com/manage-profile/security/api-tokens)).

> This module serves primarily as a demonstration of an Kentico Xperience integration with an external issue tracking service and shows the potential capabilities of such integration. The current implementation may not fully reflect all possible scenarios, and further development is needed for production usage that covers all use cases.

## Set up the environment
### Import the custom module

1. Download the latest export package from the [/CMSSiteUtils/Export](/CMSSiteUtils/Export) folder.
2. In the Xperience adminstration, open the __Sites__ application.
3. [Import](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects) the downloaded package with the __Import files__ and __Import code files__ [settings](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects#Importingasiteorobjects-Import-Objectselectionsettings) enabled.
4. Perform the [necessary steps](https://docs.xperience.io/deploying-websites/exporting-and-importing-sites/importing-a-site-or-objects#Importingasiteorobjects-Importingpackageswithfiles) to include the following imported folders in your project:
   -  `/CMSModules/Kentico.Xperience.Jira`
   -  `/Old_App_Code/CMSModules/Kentico.Xperience.Jira`

### Enable the integration

1. Log into Jira as an administrator and [generate a new API token](https://id.atlassian.com/manage-profile/security/api-tokens).
2. In the Xperience administration, open the __Modules__ application.
3. Edit the __Membership__ module → __Classes__ tab __→ User__ class __→ Fields__ tab.
4. Add a __New field__ named __JiraApiToken__ with the following properties:

![User class](/assets/user-module.png)

5. Open the __Users__ application and edit the default _Global administrator_ user.
   - You can set the default global administrator in the __Settings__ application __→ System → Default user ID__.
6. Make sure that the user's email matches the email of the Jira administrator.
   - This allows Jira actions like creating new issues and commenting appear under the correct account.
7. Switch to the __Custom fields__ tab and save the generated __Jira API token__.
8. (Optional) Repeat the steps 1-7 also for other Xperience users by having them generate a Jira API token and ensuring their Jira and Xperience emails match.  In case the emails won't match, Jira actions will be logged under the global administrator's account.

### Configure the settings

You can configure the integration module in the __Settings__ application __→ Integration → Jira__. The following settings are available:

- __Enabled__ — Indicates if the integration is enabled or not. Disable the setting to temporarily prevent the creation of new Jira issues, webhooks, etc.
- __Delete webhooks after first run__ — If enabled, Jira webhooks created by this integration are deleted after their first execution. If disabled, webhooks will remain active in Jira even after their execution.
- __Server__ — The base URL of your Jira server, e.g. _https://mycompany.atlassian.net_.

## Custom actions

This integration provides 3 custom actions for both [Workflows](https://docs.xperience.io/managing-website-content/working-with-pages/using-workflows) and [Marketing automation processes](https://docs.xperience.io/on-line-marketing-features/managing-your-on-line-marketing-features/marketing-automation/working-with-marketing-automation-processes):

- __[Create Jira issue](#create-jira-issues)__
- __[Jira transition workflow](#transition-jira-issues-between-workflow-steps)__
- __[Create Jira webhook](#create-jira-webhooks)__

### Create Jira issues

You can create new Jira issues using the __Create Jira issue__ step in workflows or in marketing automation processes. When this step is reached in the workflow or in the process, a new Jira issue is created and the workflow continues to the next step. If an Xperience user that moves the workflow or automation process into this step provides a [comment](https://docs.xperience.io/managing-website-content/working-with-pages/using-workflows#Usingworkflows-Submittingapageforapprovalwithacomment), the comment is also added to the Jira issue.

To create new Jira issues:

1. Add the __Create Jira issue__ step into your workflow or marketing automation process.
2. Configure the __Project__ and __Issue type__ properties of the custom step using the drop-down menus. New created Jira issues will be of the selected issue type.
3. Based on the selected project and issue type, configure the appropriate fields in the __Fields__ property.
   - Configure the required __Summary__ field, which corresponds to the issue title in Jira, and any other custom fields that are required by your company.
  
![Create issue](/assets/create-issue.png)

When configuring properties of the _Create Jira issue_ step in a workflow, you can use macros, such as `{%NodeAliasPath%}`. The macros will be resolved according to the page that is using the workflow. Macros cannot be used in marketing automation process fields.

#### Link Jira issues and Xperience objects

The __Create Jira issue__ custom action "links" the page or marketing automation process to the newly-created Jira issue and its project. This is achieved by storing the Jira issue and project IDs in the Xperience database. Specifically, in the `DocumentCustomData` column for workflows and `StateCustomData` column for automation processes. 

If you need to retrieve the Jira issue and project IDs, e.g. within a custom handler, you can use the [`JiraHelper.GetLinkedIssue()`](/Old_App_Code/CMSModules/Kentico.Xperience.Jira/JiraHelper.cs#L257) or [`JiraHelper.GetLinkedProject()`](/Old_App_Code/CMSModules/Kentico.Xperience.Jira/JiraHelper.cs#L279) methods.

### Transition Jira issues between workflow steps

You can use the __Jira transition workflow__ step in workflows or in marketing automation processes to move the [linked](#linking-jira-issues-and-xperience-objects) Jira issue to another [workflow step](https://confluence.atlassian.com/adminjiraserver/working-with-workflows-938847362.html). When this step is reached, the Jira issue configured in the step properties is automatically transitioned to another preconfigured workflow step. If an Xperience user that moves the workflow or automation process into this step provides a comment, the comment is also added to the Jira issue.

To transition a Jira issue to another Jira workflow step:
1. Add the __Jira transition workflow__ step into your Xperience workflow or marketing automation process.
2. Configure the __Project__ and __Issue type__ properties of the custom step by selecting an existing Jira project and issue.
   - For example, if the issue should move from the _In Progress_ to the _Done_ step, select an issue that is already in the _In Progress_ step.
3. Lastly, configure the __Transition__ property. The drop-down menu will list all valid transitions of the selected issue.

![Transition](/assets/transition.png)


### Create Jira webhooks

You can use the __Create Jira webhook__ step in workflows or in marketing automation processes to create new [Jira webhooks](https://developer.atlassian.com/server/jira/platform/webhooks/). After webhook creation, the Xperience workflow or marketing automation process is moved to the next step, by default. You can override the default behavior that occurs after (or before) the webhook creation by registering an [event handler](#custom-webhook-handling).

The webhooks execute when a specific action (specified by you) is taken in Jira. After the execution, the Jira webhook is deleted. If you do not wish the webhooks to be deleted after their execution, disable the __Delete webhooks after first run__ [setting](#configure-the-settings).

You can access all your current webhooks in the Jira administration under __Settings → System → WebHooks__.

To create a Jira webhook:
1. Add the __Create Jira webhook__ step into your Xperience workflow or marketing automation process.
2. Configure the following properties of the custom step:
   - __Name__ — An arbitrary name for the webhook which will appear in Jira.
   - __Administraion URL__ — The absolute URL to the Xperience administraion website.
   - __Events__ — A comma-separated list of Jira events that will trigger the webhook. See [Registering events for a webhook](https://developer.atlassian.com/server/jira/platform/webhooks/#registering-events-for-a-webhook) for a list of valid events.
   - __Scope__ (optional) — One or more JQL queries that will filter when the webhook triggers. For example, the `jira:issue_updated` event will trigger for all issues unless you specify certain issues, projects, resolutions, etc. See [Constructing JQL queries](https://confluence.atlassian.com/jirasoftwareserver/advanced-searching-939938733.html#Advancedsearching-ConstructingJQLqueries) for more information on writing JQL queries.
     - If the page or automation process is [linked](#linking-issues-and-objects) to a Jira issue and a project, you can use the `##LinkedIssue##` and `##LinkedProject##` macros in the scope to reference the respective linked values.

![Create webhook](/assets/create-webhook.png)

When the webhook is created, the workflow or automation process moves to the next step. If you'd like the current workflow to wait until the webhook is triggered:
- For workflows, add the __Standard__ step after the __Create Jira webhook__ step 
- For automation processes, add the __Approve progress__ step after the __Create Jira webhook__ step . 

#### Example automation process

The following marketing automation process serves as an example of how to use the __Create Jira webhook__ step:

![Process example](/assets/process-example.png)

The example process is configured for a website that uses a "Contact us" form. Customers submit the form with questions about certain products and the automation process creates a new issue for marketers to call the customers. The process then creates a webhook with the following properties:

- __Events__: _jira:issue_updated_
- __Scope__: _issue-related-events-section:issue = ##LinkedIssue## AND resolution = Done_

The process stops at the _Wait for webhook_ step. When the linked Jira issue is moved to a step with the _Done_ resolution, the webhook is executed and the process moves to the next step, setting a custom contact property before finishing.

#### Customize webhook handling

If you want to prevent the default webhook functionality that moves the workflow or automation process to the next step, or run your own code before or after the transition, you can register the following [custom event handlers](https://docs.xperience.io/custom-development/handling-global-events):

- __JiraEvents.WebhookTriggered.Before__
- __JiraEvents.WebhookTriggered.After__

The event handlers are provided with the body of the webhook request, the linked object of the workflow (a `TreeNode` or `AutomationStateInfo`), and the ID of the Jira issue which triggered it. You can prevent the default webhook functionality in the `Before` handler by calling the `Cancel()` method:

```cs
JiraEvents.WebhookTriggered.Before += (object sender, WebhookTriggeredArgs e) =>
{
    // Your code here
    e.Cancel();
};
```

## Questions & Support

See the [Kentico home repository](https://github.com/Kentico/Home/blob/master/README.md) for more information about the product(s) and general advice on submitting questions.
