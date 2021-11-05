namespace Xperience.Jira.Models
{
    /// <summary>
    /// The schema of a Jira issue field
    /// </summary>
    public class JiraIssueFieldSchema
    {
        public string Type { get; set; }

        public string Items { get; set; }

        public string System { get; set; }

        public string Custom { get; set; }

        public int CustomId { get; set; }
    }
}