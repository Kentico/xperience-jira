namespace Kentico.Xperience.Jira.Models
{
    /// <summary>
    /// The schema of a Jira issue field.
    /// </summary>
    public class JiraIssueFieldSchema
    {
        /// <summary>
        /// Deserialized data from Jira API. The data type expected of the field
        /// value when creating a new issue.
        /// </summary>
        public string Type { get; set; }
    }
}