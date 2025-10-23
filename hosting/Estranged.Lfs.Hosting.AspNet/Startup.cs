using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Estranged.Lfs.Api;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Estranged.Lfs.Adapter.S3;
using Estranged.Lfs.Data;
using System.Collections.Generic;

namespace Estranged.Lfs.Hosting.AspNet
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            //IConfiguration credentials = new ConfigurationBuilder().AddJsonFile("credentials.json").Build();
            var myEndpointRegion = "sbg";

            services.AddLogging(x =>
            {
                x.AddConsole();
                x.AddDebug();
            });

            services.AddSingleton<IAmazonS3, AmazonS3Client>();
            services.AddLfsS3Adapter(new S3BlobAdapterConfig { Bucket = "test-ovh" }, new AmazonS3Client("MyAccesKeyAWS", "MyAccesSecretAWS", new AmazonS3Config { ServiceURL = $"https://s3.{myEndpointRegion}.io.cloud.ovh.net", AuthenticationRegion = myEndpointRegion, SignatureVersion = "V4" }));
            services.AddSingleton<IAuthenticator>(x => new DictionaryAuthenticator(new Dictionary<string, string> { { "userAskedBySourceTree", "passwordAskedBySourceTree" } }));
            services.AddLfsApi();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}