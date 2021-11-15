using System.Collections.Generic;

namespace Kentico.Xperience.Jira.Models
{
    /// <summary>
    /// A Jira project containing a list of valid <see cref="JiraIssueType"/>
    /// for that project.
    /// </summary>
    public class JiraProject
    {
        /// <summary>
        /// Deserialized data from Jira API. The internal ID of the project.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. The key of the project used
        /// to prefix issues of the project.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. The user-friendly name of the project.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. A collection of the allowed
        /// <see cref="JiraIssueType"/>s that can be created under this project.
        /// </summary>
        public IEnumerable<JiraIssueType> IssueTypes { get; set; }
    }
}