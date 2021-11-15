using CMS.Base.Web.UI;
using CMS.FormEngine.Web.UI;
using CMS.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Kentico.Xperience.Jira.Models;

namespace Kentico.Xperience.Jira.Controls
{
    /// <summary>
    /// A form control which dynamically generates controls to populate a Jira issue's custom fields.
    /// Relies on a <see cref="ProjectSelector"/> and <see cref="IssueTypeSelector"/> to be present in
    /// the same form.
    /// </summary>
    public partial class MetaFieldInputs : JiraFormControl
    {
        private string loadedValue;

        /// <summary>
        /// Meta fields to hide from the dynamically generated issue fields.
        /// </summary>
        private List<string> disabledMetaFields = new List<string>() { "issuetype", "project", "attachment" };

        /// <summary>
        /// The fields of the selected Jira issue type which were added to the dynamic layout.
        /// </summary>
        private List<JiraIssueField> CachedFields
        {
            get => (List<JiraIssueField>)(Session["JiraCachedIssueFields"] = (Session["JiraCachedIssueFields"] as List<JiraIssueField>) ?? new List<JiraIssueField>());
            set => Session["JiraCachedIssueFields"] = value;
        }

        public override object Value
        {
            get
            {
                return MakeValue();
            }
            set
            {
                loadedValue = ValidationHelper.GetString(value, "");
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!GetResolvedValue<bool>("ShowDescription", true))
            {
                disabledMetaFields.Add("description");
            }

            LoadIssueMetaFields();
        }

        /// <summary>
        /// Constructs the value to be saved to the database by checking Request.Form.
        /// </summary>
        /// <returns></returns>
        private string MakeValue()
        {
            var values = new List<string>();
            foreach(var field in CachedFields)
            {
                var formSubmittedData = Request.Form[field.ControlUID];
                if (!String.IsNullOrEmpty(formSubmittedData))
                {
                    var encodedValue = HttpUtility.UrlEncode(formSubmittedData);
                    values.Add($"{field.Key};{encodedValue}");
                }

            }

            return values.Join("|");
        }

        /// <summary>
        /// Populates the dynamic meta fields with data from the database.
        /// </summary>
        /// <param name="value"></param>
        private void LoadExistingValue()
        {
            var fields = loadedValue.Split('|');
            foreach (var field in fields)
            {
                var key = field.Split(';')[0];
                var value = field.Split(';')[1];
                value = HttpUtility.UrlDecode(value);

                var control = issueMetaPlaceholder.FindControl(key);
                if (control != null)
                {
                    if (control is TextBoxWithPlaceholder)
                    {
                        (control as TextBoxWithPlaceholder).Text = value;
                    }
                    else if (control is CMSTextArea)
                    {
                        (control as CMSTextArea).Text = value;
                    }
                    else if (control is CMSDropDownList)
                    {
                        (control as CMSDropDownList).SelectedValue = value;
                    }
                }
            }
        }

        private void LoadIssueMetaFields()
        {
            if(String.IsNullOrEmpty(Project) || String.IsNullOrEmpty(IssueType))
            {
                return; 
            }

            var project = JiraHelper.GetProjectWithCreateSchema(Project, IssueType);
            if (project == null)
            {
                ShowInformation("Selected project couldn't be found. Please select another.");
                return;
            }


            var issueType = project.IssueTypes.Where(i => i.Id == IssueType).FirstOrDefault();
            if (issueType == null)
            {
                ShowInformation("Issue type wasn't found in selected project.");
                return;
            }

            CachedFields.Clear();
            issueMetaPlaceholder.Controls.Clear();
            var fields = issueType.GetFields();

            foreach (var field in fields)
            {
                if (disabledMetaFields.Contains(field.Key))
                {
                    continue;
                }

                var control = field.MakeEditingControl();
                if (control != null)
                {
                    issueMetaPlaceholder.Controls.Add(control);

                    string uid;
                    if (control is TextBoxWithPlaceholder)
                    {
                        uid = (control as TextBoxWithPlaceholder).TextBox.UniqueID;
                    }
                    else
                    {
                        uid = control.UniqueID;
                    }

                    field.ControlUID = uid;

                    CachedFields.Add(field);
                }
            }

            if (!String.IsNullOrEmpty(loadedValue) && !IsPostBack)
            {
                LoadExistingValue();
            }
        }
    }
}