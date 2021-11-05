using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Xperience.Jira.Models
{
    /// <summary>
    /// A Jira issue type
    /// </summary>
    public class JiraIssueType
    {
        public string Id { get; set; }

        public string Description { get; set; }

        public string IconUrl { get; set; }

        public string Name { get; set; }

        public JObject Fields { get; set; }

        /// <summary>
        /// Returns the <see cref="Fields"/> property value as a List of
        /// <see cref="JiraIssueField"/>
        /// </summary>
        /// <returns></returns>
        public List<JiraIssueField> GetFields()
        {
            return JsonConvert.DeserializeObject<List<JiraIssueField>>(JsonConvert.SerializeObject(Fields.Values()));
        }
    }
}