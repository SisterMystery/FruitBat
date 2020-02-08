namespace FruitBat.PSCmdlets
{
    using System.Management.Automation;
    using Authentication;
    using Utility;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class ParallelRequestCmdlet : PSCmdlet
    {
        public async Task<string[]> ParallelRequestAsync(string host, string apiSubPath, ParallelRequestParams parallelParams)
        {
            var requestTasks = parallelParams.GenerateArguments().
                Select(requestArgs => RequestHelper.BuildRequestUri(host, apiSubPath, requestArgs)).
                Select(async requestUri => await RequestHelper.SendRequestAsync(requestUri));
            return await Task.WhenAll(requestTasks);
        }
    }

    [Cmdlet("Get", "Builds")]
    public class GetBuilds : ParallelRequestCmdlet
    {
        [Parameter(Mandatory = false, ParameterSetName = "Query")]
        public string BranchName;
        
        [Parameter(Mandatory = false, ParameterSetName = "BuildId")]
        public string BuildId;

        [Parameter(Mandatory = false, ParameterSetName = "Query")]
        public string RepositoryId = "d0618add-9da3-4bfc-ab49-fbd949db993c";

        [Parameter(Mandatory = false, ParameterSetName = "Query")]
        public string RepositoryType = "tfsgit";

        [Parameter(Mandatory = false, ParameterSetName = "Query")]
        public string ResultFilter = "succeeded";

        [Parameter(Mandatory = false, ParameterSetName = "Query")]
        public DateTime? MinTime;

        protected override void ProcessRecord()
        {
            IDictionary<string, string> arguments = null;
            string subPath; 

            if (BuildId != null)
            {
                subPath = $"DefaultCollection/One/_apis/build/builds/{BuildId}";
            } else {

                subPath = $"DefaultCollection/One/_apis/build/builds";
                arguments = new Dictionary<string, string>
                {
                    {"BranchName", BranchName },
                    {"RepositoryId", RepositoryId },
                    {"RepositoryType", RepositoryType },
                    {"ResultFilter", ResultFilter },
                    {"MinTime", MinTime?.ToString()}
                };
            }
            
            var uriPath = RequestHelper.BuildRequestUri(VssAuthenticator.AzureDevOpsBuildsHost, subPath, arguments);
            WriteObject(RequestHelper.SendRequestAsync(uriPath).Result);
        }
    }

    [Cmdlet("ParallelGet", "Builds")]
    public class ParallelGetBuilds : GetBuilds
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string[] BranchNames;

        public IDictionary<string, string> BaseParameters;

        protected override void ProcessRecord()
        {
            BaseParameters = new Dictionary<string, string>
            {
                {"BranchName", BranchName },
                {"RepositoryId", RepositoryId },
                {"RepositoryType", RepositoryType },
                {"ResultFilter", ResultFilter },
                {"MinTime", MinTime?.ToString()}
            };

            var parameters = new ParallelRequestParams(
                "BranchName",
                BranchNames,
                BaseParameters
            );

            var requestTasks = ParallelRequestAsync(VssAuthenticator.AzureDevOpsBuildsHost, "DefaultCollection/One/_apis/build/builds", parameters);
            WriteObject(requestTasks.Result);
        }
    }

    [Cmdlet("Get", "BuildArtifacts")]
    public class GetBuildArtifacts : PSCmdlet
    {
        [Parameter(Mandatory = false)]
        public string BuildId;
    }

    [Cmdlet("ParallelGet","BuildArtifacts")]
    public class ParallelGetBuildArtifacts : GetBuildArtifacts
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string[] BuildIds;

        protected override void ProcessRecord()
        {
            var requestTasks = BuildIds.
                Select(buildId => RequestHelper.BuildRequestUri(VssAuthenticator.AzureDevOpsBuildsHost, $"DefaultCollection/One/_apis/build/builds/{buildId}/Artifacts")).
                Select(async requestUri => await RequestHelper.SendRequestAsync(requestUri));

            WriteObject(Task.WhenAll(requestTasks).Result);
        }
    }

    [Cmdlet("Get", "Release")]
    public class GetReleases : ParallelRequestCmdlet
    {
        [Parameter(Mandatory = false)]
        public string ReleaseId;

        [Parameter(Mandatory = false)]
        public string SourceId;

        [Parameter(Mandatory = false)]
        public string ArtifactTypeId;

        [Parameter(Mandatory = false)]
        public string ArtifactVersionId;

        [Parameter(Mandatory = false)]
        public DateTime? MaxCreatedTime;

        [Parameter(Mandatory = false)]
        public DateTime? MinCreatedTime;

        [Parameter(Mandatory = false)]
        public int? DefinitionId;

        [Parameter(Mandatory = false)]
        public string QueryOrder = "Descending";

        [Parameter(Mandatory = false)]
        public string[] ExpandProperties;

        protected override void ProcessRecord()
        {

            var requestUri = RequestHelper.BuildRequestUri(VssAuthenticator.AzureDevOpsHost, $"DefaultCollection/One/_apis/release/releases/{ReleaseId}");
            WriteObject(RequestHelper.SendRequestAsync(requestUri).Result);
        }

    }

    [Cmdlet("ParallelGet", "Releases")]
    public class ParallelGetReleases : GetReleases
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public string[] ArtifactVersionIds;

        private IDictionary<string, string> BaseParameters;  
        protected override void ProcessRecord()
        {
            BaseParameters = new Dictionary<string, string>
            {
                {"SourceId", SourceId},
                {"ArtifactTypeId", ArtifactTypeId},
                {"ArtifactVersionId", ArtifactVersionId},
                {"MaxCreatedTime", MaxCreatedTime?.ToString()},
                {"MinCreatedTime", MinCreatedTime?.ToString()},
                {"DefinitionId", DefinitionId?.ToString()},
                {"QueryOrder", QueryOrder },
                {"$Expand", ExpandProperties != null ? string.Join(",",ExpandProperties) : null}
            };

            var parameters = new ParallelRequestParams(
                "ArtifactVersionId",
                ArtifactVersionIds,
                BaseParameters
            );

            var requestTasks = ParallelRequestAsync(VssAuthenticator.AzureDevOpsHost, "DefaultCollection/One/_apis/release/releases", parameters);
            WriteObject(requestTasks.Result);
        }
    }

    public class ParallelRequestParams
    {
        public string ParallelParamKey;
        public string[] ParallelParamInstances;
        public IDictionary<string, string> BaseArguments;

        public ParallelRequestParams(string key, string[] instances, IDictionary<string, string> baseArguments)
        {
            ParallelParamKey = key;
            ParallelParamInstances = instances;
            BaseArguments = baseArguments;
            BaseArguments.Remove(ParallelParamKey);
        }

        public IEnumerable<IDictionary<string, string>> GenerateArguments()
        {
            return ParallelParamInstances.Select(paramInstance => new Dictionary<string, string>(BaseArguments)
                {
                    {ParallelParamKey, paramInstance}
                });
        }
    }


    [Cmdlet("Get", "ReleaseDefinitions")]
    public class GetReleaseDefinitions : PSCmdlet
    {
        [Parameter(Mandatory = false)]
        public string SearchText = "Geneva%20Actions";


        protected override void ProcessRecord()
        {
            var arguments = new Dictionary<string,string>
            {
                {"SearchText", SearchText}
            };

            var uriPath = RequestHelper.BuildRequestUri(VssAuthenticator.AzureDevOpsHost, "DefaultCollection/One/_apis/release/definitions", arguments);
            WriteObject(RequestHelper.SendRequestAsync(uriPath).Result);
        }
    }

}
