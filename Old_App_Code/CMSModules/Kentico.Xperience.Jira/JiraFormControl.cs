using CMS.FormEngine.Web.UI;
using CMS.Helpers;
using System;
using System.Web;
using Kentico.Xperience.Jira.Controls;

namespace Kentico.Xperience.Jira
{
    /// <summary>
    /// A class ensuring that multiple Jira-related controls can be used on the same form and retrieve
    /// each others values appropriately
    /// </summary>
    public class JiraFormControl : FormEngineUserControl
    {
        private readonly string CONTROL_UID_PROJECT = "JiraCachedProjectUid";
        private readonly string CONTROL_UID_ISSUE = "JiraCachedIssueUid";
        private readonly string CONTROL_UID_ISSUETYPE = "JiraCachedIssueTypeUid";

        public override object Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// Gets or sets a Jira project ID provided by a <see cref="ProjectSelector"/>. During postbacks,
        /// this will return the ID from the form data. Otherwise, the session will be used.
        /// </summary>        
        protected string Project
        {
            get
            {
                if (IsPostBack)
                {
                    var controlId = HttpContext.Current.Session[CONTROL_UID_PROJECT].ToString();
                    return Request.Form[controlId];
                }
                else
                {
                    return ValidationHelper.GetString(HttpContext.Current.Session["JiraCachedProjectValue"], "");
                }
            }
            set
            {
                HttpContext.Current.Session["JiraCachedProjectValue"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a Jira issue type ID provided by a <see cref="IssueTypeSelector"/>. During postbacks,
        /// this will return the ID from the form data. Otherwise, the session will be used.
        /// </summary>  
        protected string IssueType
        {
            get
            {
                if (IsPostBack)
                {
                    var controlId = HttpContext.Current.Session[CONTROL_UID_ISSUETYPE].ToString();
                    return Request.Form[controlId];
                }
                else
                {
                    return ValidationHelper.GetString(HttpContext.Current.Session["JiraCachedIssueTypeValue"], "");
                }
            }
            set
            {
                HttpContext.Current.Session["JiraCachedIssueTypeValue"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a Jira issue ID provided by a <see cref="IssueSelector"/>. During postbacks,
        /// this will return the ID from the form data. Otherwise, the session will be used.
        /// </summary>  
        protected string Issue
        {
            get
            {
                if (IsPostBack)
                {
                    var controlId = HttpContext.Current.Session[CONTROL_UID_ISSUE].ToString();
                    return Request.Form[controlId];
                }
                else
                {
                    return ValidationHelper.GetString(HttpContext.Current.Session["JiraCachedIssueValue"], "");
                }
            }
            set
            {
                HttpContext.Current.Session["JiraCachedIssueValue"] = value;
            }
        }

        /// <summary>
        /// Saves the current control's unique ID to session so that the submitted form value
        /// can be found on postback
        /// </summary>
        /// <param name="uid"></param>
        protected void SaveControlUniqueId(string uid)
        {
            if (this is ProjectSelector)
            {
                HttpContext.Current.Session[CONTROL_UID_PROJECT] = uid;
            }
            else if (this is IssueSelector)
            {
                HttpContext.Current.Session[CONTROL_UID_ISSUE] = uid;
            }
            else if (this is IssueTypeSelector)
            {
                HttpContext.Current.Session[CONTROL_UID_ISSUETYPE] = uid;
            }
        }
    }
}