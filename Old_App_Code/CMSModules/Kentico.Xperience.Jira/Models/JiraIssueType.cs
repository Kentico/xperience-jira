using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Kentico.Xperience.Jira.Models
{
    /// <summary>
    /// A Jira issue type.
    /// </summary>
    public class JiraIssueType
    {
        /// <summary>
        /// Deserialized data from Jira API. The internal ID of the issue type.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. The user-friendly description of
        /// the issue type.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. An absolute URL pointing to the icon
        /// for this issue type.
        /// </summary>
        public string IconUrl { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. The user-friendly name of the
        /// issue type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Deserialized data from Jira API. A collection of objects representing
        /// the fields that can be populated when creating new issues of this type.
        /// </summary>
        /// <remarks>
        /// Use <see cref="GetFields()"/> to generate strongly-typed <see cref="JiraIssueField"/>
        /// objects.
        /// </remarks>
        public JObject Fields { get; set; }

        /// <summary>
        /// Returns the <see cref="Fields"/> property value as a List of <see cref="JiraIssueField"/>
        /// </summary>
        public List<JiraIssueField> GetFields()
        {
            return JsonConvert.DeserializeObject<List<JiraIssueField>>(JsonConvert.SerializeObject(Fields.Values()));
        }
    }
}