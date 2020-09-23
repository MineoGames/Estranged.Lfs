using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Estranged.Lfs.Api;
using Estranged.Lfs.Adapter.S3;
using Amazon.S3;
using Estranged.Lfs.Data;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using Estranged.Lfs.Authenticator.GitHub;
using Estranged.Lfs.Authenticator.BitBucket;

namespace Estranged.Lfs.Hosting.Lambda
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            const string LfsBucketVariable = "LFS_BUCKET";
            const string LfsUsernameVariable = "LFS_USERNAME";
            const string LfsPasswordVariable = "LFS_PASSWORD";
            const string GitHubOrganisationVariable = "GITHUB_ORGANISATION";
            const string GitHubRepositoryVariable = "GITHUB_REPOSITORY";
            const string BitBucketWorkspaceVariable = "BITBUCKET_WORKSPACE";
            const string BitBucketRepositoryVariable = "BITBUCKET_REPOSITORY";
            const string S3AccelerationVariable = "S3_ACCELERATION";
            const string S3Region = "S3_REGION";
            const string S3AccessKey = "S3_ACCESS_KEY";
            const string S3AccessSecret = "S3_ACCESS_SECRET";


            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            string lfsBucket = config[LfsBucketVariable] ?? throw new InvalidOperationException($"Missing environment variable {LfsBucketVariable}");
            string lfsUsername = config[LfsUsernameVariable];
            string lfsPassword = config[LfsPasswordVariable];
            string gitHubOrganisation = config[GitHubOrganisationVariable];
            string gitHubRepository = config[GitHubRepositoryVariable];
            string bitBucketWorkspace = config[BitBucketWorkspaceVariable];
            string bitBucketRepository = config[BitBucketRepositoryVariable];
            bool s3Acceleration = bool.Parse(config[S3AccelerationVariable] ?? "false");
            string s3Region = config[S3Region];
            string s3AccessKey = config[S3AccessKey];
            string s3AccessSecret = config[S3AccessSecret];

            bool isDictionaryAuthentication = !string.IsNullOrWhiteSpace(lfsUsername) && !string.IsNullOrWhiteSpace(lfsPassword);
            bool isGitHubAuthentication = !string.IsNullOrWhiteSpace(gitHubOrganisation) && !string.IsNullOrWhiteSpace(gitHubRepository);
            bool isBitBucketAuthentication = !string.IsNullOrWhiteSpace(bitBucketWorkspace) && !string.IsNullOrWhiteSpace(bitBucketRepository);

            // If all authentication mechanims are set, or none are set throw an error
            if (new[] {isDictionaryAuthentication, isGitHubAuthentication, isBitBucketAuthentication}.Count(x => x) != 1)
            {
                throw new InvalidOperationException($"Unable to detect authentication mechanism. Please set {LfsUsernameVariable} and {LfsPasswordVariable} for simple user/password auth" +
                                                    $" or {GitHubOrganisationVariable} or {GitHubRepositoryVariable} for authentication against that repository on GitHub");
            }

            if (isDictionaryAuthentication)
            {
                services.AddLfsDictionaryAuthenticator(new Dictionary<string, string> { { lfsUsername, lfsPassword } });
            }

            if (isGitHubAuthentication)
            {
                services.AddLfsGitHubAuthenticator(new GitHubAuthenticatorConfig { Organisation = gitHubOrganisation, Repository = gitHubRepository });
            }

            if (isBitBucketAuthentication)
            {
                services.AddLfsBitBucketAuthenticator(new BitBucketAuthenticatorConfig { Workspace = bitBucketWorkspace, Repository = bitBucketRepository });
            }

            // Check S3 config
            if (string.IsNullOrWhiteSpace(s3Region) || string.IsNullOrWhiteSpace(s3AccessKey) || string.IsNullOrWhiteSpace(s3AccessSecret))
            {
                throw new InvalidOperationException($"Uncomplete S3 configuration. Please set S3_REGION : Object Storage OVH public region in lower case without number (ex: sbg). Please set S3_ACCESS_KEY: from RC file. Please set S3_ACCESS_SECRET: from RC file. See https://docs.ovh.com/gb/en/public-cloud/access_and_security_in_horizon/");
            }

            services.AddLfsS3Adapter(new S3BlobAdapterConfig { Bucket = lfsBucket }, new AmazonS3Client(s3AccessKey, s3AccessSecret, new AmazonS3Config { UseAccelerateEndpoint = s3Acceleration, ServiceURL = $"https://s3.{s3Region}.cloud.ovh.net", AuthenticationRegion = $"{s3Region}", SignatureVersion = "V4" }));
            services.AddLfsApi();

            services.AddLogging(x => x.AddLambdaLogger());
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
