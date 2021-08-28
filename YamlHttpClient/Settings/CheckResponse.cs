using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace YamlHttpClient.Settings
{
    /// <summary/>
    public class CheckResponse
    {
        /// <summary/>
        [YamlMember(Alias = "throw_exception_if_body_contains_any")]
        public List<string>? ThrowExceptionIfBodyContainsAny { get; set; }

        /// <summary/>
        [YamlMember(Alias = "throw_exception_if_body_not_contains_all")]
        public List<string>? ThrowExceptionIfBodyNotContainAll { get; set; }
    }
}