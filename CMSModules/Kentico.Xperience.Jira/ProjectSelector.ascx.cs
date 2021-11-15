using CMS.Helpers;
using System;
using System.Linq;
using System.Web.UI.WebControls;

namespace Kentico.Xperience.Jira.Controls
{
    /// <summary>
    /// A form control for selecting a Jira project
    /// </summary>
    public partial class ProjectSelector : JiraFormControl
    {
        private string loadedValue;

        public override object Value
        {
            get
            {
                return drpProjects.SelectedValue;
            }
            set
            {
                loadedValue = ValidationHelper.GetString(value, "");
                Project = loadedValue;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            LoadProjects();
            SaveControlUniqueId(drpProjects.UniqueID);
        }

        private void LoadProjects()
        {
            drpProjects.Items.Clear();
            drpProjects.Items.Add(new ListItem("(select project)", ""));

            var projects = JiraHelper.GetProjects();
            var orderedProjects = projects.OrderBy(p => p.Name);

            foreach (var proj in orderedProjects)
            {
                drpProjects.Items.Add(new ListItem(proj.Name, proj.Id));
            }

            if (!String.IsNullOrEmpty(loadedValue) && !IsPostBack)
            {
                drpProjects.SelectedValue = loadedValue;
            }
        }
    }
}