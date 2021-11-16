using CMS.Helpers;
using System;
using System.Web.UI.WebControls;

namespace Kentico.Xperience.Jira.Controls
{
    /// <summary>
    /// A form control for selecting a Jira workflow transition. Relies on an <see cref="IssueSelector"/> to
    /// be present in the same form, as the only way to get transitions from the Jira API is to provide an
    /// existing issue.
    /// </summary>
    public partial class TransitionSelector : JiraFormControl
    {
        private string loadedValue;

        public override object Value
        {
            get
            {
                return drpTransitions.SelectedValue;
            }
            set
            {
                loadedValue = ValidationHelper.GetString(value, "");
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            LoadTransitions();
        }

        private void LoadTransitions()
        {
            if (String.IsNullOrEmpty(Issue))
            {
                return;
            }

            drpTransitions.Items.Clear();
            drpTransitions.Items.Add(new ListItem("(select transition)", ""));

            var transitions = JiraApiHelper.GetTransitions(Issue);
            if (transitions == null)
            {
                ShowInformation("Unable to load transitions. Please select another issue.");
                return;
            }

            foreach (var trans in transitions)
            {
                drpTransitions.Items.Add(new ListItem(trans.Value<string>("name"), trans.Value<string>("id")));
            }

            if (!String.IsNullOrEmpty(loadedValue) && !IsPostBack)
            {
                drpTransitions.SelectedValue = loadedValue;
            }
        }
    }
}