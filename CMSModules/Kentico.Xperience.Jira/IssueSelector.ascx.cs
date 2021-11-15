using CMS.Helpers;
using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace Kentico.Xperience.Jira.Controls
{
    /// <summary>
    /// A form control for selecting a Jira issue. Relies on a <see cref="ProjectSelector"/> to
    /// be present in the same form.
    /// </summary>
    public partial class IssueSelector : JiraFormControl
    {
        private string loadedValue;

        public override object Value
        {
            get
            {
                return drpIssues.SelectedValue;
            }
            set
            {
                loadedValue = ValidationHelper.GetString(value, "");
                Issue = loadedValue;
            }
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            LoadIssues();
            SaveControlUniqueId(drpIssues.UniqueID);
        }

        private void LoadIssues()
        {
            if (String.IsNullOrEmpty(Project))
            {
                return;
            }

            drpIssues.Items.Clear();
            drpIssues.Items.Add(new ListItem("(select issue)", ""));

            var issues = JiraHelper.GetIssues(Project, txtSearch.Text);
            var orderedIssues = issues.OrderBy(i => i.Value<string>("summaryText"));

            foreach (var issue in orderedIssues)
            {
                drpIssues.Items.Add(new ListItem(issue.Value<string>("summaryText"), issue.Value<string>("id")));
            }

            if (drpIssues.Items.FindByValue(Issue) != null)
            {
                drpIssues.SelectedValue = Issue;
            }
        }
    }
}