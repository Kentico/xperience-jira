﻿using CMS.Helpers;
using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace Kentico.Xperience.Jira.Controls
{
    /// <summary>
    /// A form control for selecting a Jira issue type. Relies on a <see cref="ProjectSelector"/> to
    /// be present in the same form.
    /// </summary>
    public partial class IssueTypeSelector : JiraFormControl
    {
        private string loadedValue;

        public override object Value
        {
            get
            {
                return drpIssueTypes.SelectedValue;
            }
            set
            {
                loadedValue = ValidationHelper.GetString(value, "");
                IssueType = loadedValue;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            LoadProjectIssueTypes();
            SaveControlUniqueId(drpIssueTypes.UniqueID);
        }

        private void LoadProjectIssueTypes()
        {
            if (String.IsNullOrEmpty(Project))
            {
                return;
            }

            drpIssueTypes.Items.Clear();
            drpIssueTypes.Items.Add(new ListItem("(select issue type)", ""));

            var project = JiraApiHelper.GetProjectWithIssueTypes(Project);
            if (project == null || project.IssueTypes.Count() == 0)
            {
                ShowInformation("Selected project has no issue types. Please select another.");
                return;
            }

            var orderedissueTypes = project.IssueTypes.OrderBy(i => i.Name);
            foreach (var i in orderedissueTypes)
            {
                drpIssueTypes.Items.Add(new ListItem(i.Name, i.Id));
            }

            if (drpIssueTypes.Items.FindByValue(IssueType) != null)
            {
                drpIssueTypes.SelectedValue = IssueType;
            }
        }
    }
}