using CMS.Base.Web.UI;
using CMS.FormEngine.Web.UI;
using Kentico.Xperience.Jira.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Kentico.Xperience.Jira.Models
{
    /// <summary>
    /// An individual field of a <see cref="JiraIssue"/> containing the field's
    /// <see cref="JiraIssueFieldSchema"/>.
    /// </summary>
    public class JiraIssueField
    {
        /// <summary>
        /// Deserialized data from Jira API. True if the field is required when
        /// creating a new issue.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. Information about the field's data type.
        /// </summary>
        public JiraIssueFieldSchema Schema { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. The user-friendly name of the field.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. The name of the field used during issue
        /// creation.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. True if the field has a default value stored
        /// in the <see cref="DefaultValue"/> (e.g. for drop-downs).
        /// </summary>
        public bool HasDefaultValue { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. Acceptable values for the field during issue
        /// creation, used in controls such as drop-downs.
        /// </summary>
        public JArray AllowedValues { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. The default value of the field.
        /// </summary>
        public JToken DefaultValue { get; set; }

        /// <summary>
        /// The full client ID of the editing control for this field when rendered via
        /// <see cref="MetaFieldInputs"/>.
        /// </summary>
        public string ControlUID { get; set; }

        /// <summary>
        /// Returns a bool indicating whether this field should be displayed in an
        /// editing form using a drop-down control.
        /// </summary>
        public bool IsDropdown
        {
            get { return AllowedValues != null && AllowedValues.Count > 0; }
        }

        /// <summary>
        /// Returns a bool indicating whether the value of this field can be displayed
        /// as a simple key/value pair, or the value is an object.
        /// </summary>
        public bool IsComplexValue
        {
            get
            {
                return AllowedValues != null && AllowedValues.Count > 0 && AllowedValues.First["id"] != null;
            }
        }

        /// <summary>
        /// Returns the field's <see cref="AllowedValues"/> for use in a drop-down control.
        /// </summary>
        /// <returns></returns>
        public ListItem[] GetDropdownItems()
        {
            var allowedValues = new List<ListItem>() {
                new ListItem($"({Name})", "")
            };

            foreach (var value in AllowedValues)
            {
                var text = value.Value<string>("name");
                if (String.IsNullOrEmpty(text))
                {
                    text = value.Value<string>("value");
                }
                allowedValues.Add(new ListItem(text, value.Value<string>("id")));
            }

            return allowedValues.ToArray();
        }

        /// <summary>
        /// Generates a <see cref="Control"/> for an editing form, depending on the
        /// <see cref="Key"/> or <see cref="JiraIssueFieldSchema.Type"/> of the field.
        /// </summary>
        public Control MakeEditingControl()
        {
            if (IsDropdown)
            {
                var dropdown = new CMSDropDownList();
                dropdown.ID = Key;
                dropdown.Items.AddRange(GetDropdownItems());
                dropdown.CssClass = "radio";

                return dropdown;
            }
            else if (Key == "description" || Key == "summary")
            {
                var textarea = new CMSTextArea();
                textarea.ID = Key;
                textarea.CssClass = "radio";
                textarea.WatermarkText = Name;

                return textarea;
            }
            else if (Schema.Type == "string" || Schema.Type == "any" || Key == "duedate")
            {
                var textbox = new TextBoxWithPlaceholder();
                textbox.ID = Key;
                textbox.PlaceholderText = Name;
                textbox.CssClass = "radio";

                if (Key == "duedate")
                {
                    textbox.TextBox.WatermarkText = "Number of days after this step runs that the issue should be completed";
                }

                return textbox;
            }

            return null;
        }
    }
}