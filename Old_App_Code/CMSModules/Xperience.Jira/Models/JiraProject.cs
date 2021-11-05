using System.Collections.Generic;

namespace Xperience.Jira.Models
{
    /// <summary>
    /// A Jira project containing a list of valid <see cref="JiraIssueType"/>
    /// for that project
    /// </summary>
    public class JiraProject
    {
        public string Id { get; set; }

        public string Key { get; set; }

        public string Name { get; set; }

        public IEnumerable<JiraIssueType> IssueTypes { get; set; }
    }
}