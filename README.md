# Xperience Jira integration

This package contains custom [Workflow](https://docs.xperience.io/managing-website-content/working-with-pages/using-workflows) and [Marketing automation](https://docs.xperience.io/on-line-marketing-features/managing-your-on-line-marketing-features/marketing-automation) actions for managing Jira issues. [Jira webhooks](https://developer.atlassian.com/server/jira/platform/webhooks/) can also be created automatically to manage Xperience objects when actions are performed in Jira.

## Installing and enabling

1. Install the [NuGet package](linkhere) in your Xperience CMS project
2. Go to __Modules > Membership > Classes tab > User class__
3. Add a new field named __JiraApiToken__:

![User class](/assets/user-module.png)

4. Logged into Jira as an admin, go to https://id.atlassian.com/manage-profile/security/api-tokens and generate a new API token
5. In the __Users application__ locate the default _Global Administrator_ account whose ID is set in __Settings > System > Default user ID__
6. Ensure that the user's email matches the Jira admin's email, and save the generated API token on the __Custom fields tab__
7. (optional) Repeat this process for other Xperience users by having them generate and API token and ensuring their emails match. This allows Jira actions like creating new issues and commenting appear under the correct account. Otherwise, they will be created using the Global Administrator's account

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