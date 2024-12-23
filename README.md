# Estranged.Lfs ![Build status](https://github.com/mineogames/Estranged.Lfs/workflows/.NET%20Core/badge.svg)
A Git LFS backend which provides pluggable authentication and blob store adapters. It is designed to run in a serverless environment to be used in conjunction with a Git provider such as GitHub or BitBucket, or self hosted Git.
# This a modified version to work with OVH Object Storage instead of AWS S3.

## Basic Usage

1. Add the Git LFS services to your application:

```csharp
services.AddLfs();
```

2. Register an implementation for IBlobAdapter and IAuthenticator. Amazon AWS S3 and Azure Blob Storage are provided out of the box:

```csharp
var s3BlobConfig = new S3BlobAdapterConfig
{
    Bucket = "estranged-lfs-test"
};
services.AddLfsS3Adapter(s3BlobConfig, new AmazonS3Client());
services.AddLfsDictionaryAuthenticator(new Dictionary<string, string>{{"username","password"}});
```

Or use the following example for Azure Blob Storage.

> Note: Keep the naming rules for Azure Blob Storage in account, review them [here](https://docs.microsoft.com/en-us/rest/api/storageservices/Naming-and-Referencing-Containers--Blobs--and-Metadata).

```csharp
var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient("<your connection string here>");
var blobConfig = new AzureBlobAdapterConfig
{
    ContainerName = "estranged-lfs-test"
};
services.AddLfsAzureBlobAdapter(blobConfig, blobServiceClient);
services.AddLfsDictionaryAuthenticator(new Dictionary<string, string> {{"username","password"}});
```

### GitHub Authenticator

A GitHub authenticator implementation is provided out of the box. This authenticator takes the supplied username and password and makes a "get repository" call against the GitHub API. If the result is that the user has access, the LFS call succeeds, if the user does not have access, the LFS call fails with a 401 error.

To configure the GitHub authenticator, you need to register it with the `IServiceProvider`:

```csharp
// services.AddLfsDictionaryAuthenticator(new Dictionary<string, string>{{"username","password"}});
var ghAuthConfig = new GitHubAuthenticatorConfig
{
    Organisation = "alanedwardes",
    Repository = "Estranged.Lfs"
};
services.AddLfsGitHubAuthenticator(ghAuthConfig);
```

When LFS prompts you for credentials, enter your GitHub username, and a [personal access token](https://github.com/settings/tokens) to authenticate. Your token should have the "repository read" scope.

### BitBucket Authenticator

A BitBucket authenticator implementation is provided out of the box. This authenticator takes the supplied username and password and makes a "get repository" call against the BitBucket API. If the result is that the user has access, the LFS call succeeds, if the user does not have access, the LFS call fails with a 401 error.

To configure the BitBucket authenticator, you need to register it with the `IServiceProvider`:

```csharp
// services.AddLfsDictionaryAuthenticator(new Dictionary<string, string>{{"username","password"}});
var bbAuthConfig = new BitBucketAuthenticatorConfig
{
    Workspace = "alanedwardes",
    Repository = "Estranged.Lfs"
};
services.AddLfsBitBucketAuthenticator(bbAuthConfig);
```

When LFS prompts you for credentials, enter your BitBucket username, and a [personal access token](https://bitbucket.org/account/settings/app-passwords/) to authenticate. Your token should have the "repository read" scope.

## Extensibility

### Blob Adapter

Any blob store which generates pre-signed URLs can be used by implementing the interface IBlobAdapter:

```csharp
public interface IBlobAdapter
{
    Task<SignedBlob> UriForUpload(string oid, long size, CancellationToken token);
    Task<SignedBlob> UriForDownload(string oid, CancellationToken token);
}
```

An S3 implementation is included, which generates pre-signed GET and PUT requests. This can be used out of the box if desired.

### Authentication Adapter

Git LFS supports HTTP Basic authentication, the mechanics of which the library deals with but the authentication portion is exposed behind the IAuthenticator interface.

```csharp
public interface IAuthenticator
{
    Task Authenticate(string username, string password, LfsPermission requiredPermission, CancellationToken token);
}
```

A sample implementation exposing a dictionary of username => password is included as a reference.

## Example Deployables

There are currently two hosting examples:

- `Estranged.Lfs.Hosting.AspNet`
- `Estranged.Lfs.Hosting.Lambda`

The former is a simple example using only Asp.NET components, and the latter is an Asp.NET Lambda function which can be deployed directly to AWS Lambda, behind API Gateway.

### Asp.NET version

1. Edit this line to suit to your environment (using _aws_access_key_id_ and _aws_secret_access_key_ of the .aws/credential file > ovh profile)
`services.AddLfsS3Adapter(new S3BlobAdapterConfig{Bucket = "objectContainerName"}, new AmazonS3Client("MyAccesKeyAWS", "MyAccesSecretAWS", new AmazonS3Config { ServiceURL = "https://s3.MyPublicRegionLowerCase.cloud.ovh.net" }));`
           

2. It can be launched in VS by choosing _Estranged.Lfs.Hosting.AspNet_ (not the default _IIS Express_ option that doesnt work).

 ![image](https://user-images.githubusercontent.com/2952456/89800274-d82c9380-db2e-11ea-85bb-3fc8652e3e9d.png)
 
3. Or it can be published in folder, then launched with _Estranged.Lfs.Hosting.AspNet.exe_

4. This is a console application that is listening for HTTP LFS requests on https://localhost:5001

![image](https://user-images.githubusercontent.com/2952456/89800695-6739ab80-db2f-11ea-8641-0eab8c501381.png)

5. Change the .lfconfig to send request to the console app

```
[lfs]
url = https://localhost:5001/
```
6. From git repo Commit lfs file and Push, and enter when asked the user and password set by this line 
`  services.AddSingleton<IAuthenticator>(x => new DictionaryAuthenticator(new Dictionary<string, string> { { "userAskedBySourceTree", "passwordAskedBySourceTree" } }));`

7. The pushed file is now present in OVH Object Storage
![image](https://user-images.githubusercontent.com/2952456/89806464-5e4cd800-db37-11ea-85bd-9ce724e7ee0e.png)

#### Deploying to Lambda

1. Head over to the `Estranged.Lfs.Hosting.Lambda` project in the `hosting` folder.
2. Install the `dotnet-lambda` global tool from AWS: https://github.com/aws/aws-extensions-for-dotnet-cli
3. Edit the `aws-lambda-tools-defaults.json` file to suit your environment setup:
```javascript
{
  "configuration": "Release",
  "environment-variables": "LFS_BUCKET=OVH_ObjectStorage_Name;LFS_USERNAME=AWS_STACK_ParameterUsername;LFS_PASSWORD=AWS_STACK_ParameterPassword;S3_ACCESS_KEY=OVH_AccesKeyAWS;S3_ACCESS_SECRET=OVH_AccesSecretAWS;S3_REGION=OVH_Region", // can be found and changed in Lambda configuration UI
  "framework": "net6.0",
  "function-handler": "Estranged.Lfs.Hosting.Lambda::Estranged.Lfs.Hosting.Lambda.LambdaEntryPoint::FunctionHandlerAsync",
  "function-memory-size": 256,
  "function-name": "AWS_STACK_NAME", // lambda name must be same as stack name
  "function-runtime": "dotnet6",
  "function-timeout": 30,
  "profile": "default", // AWS connexion profile
  "region": "eu-west-1", // AWS public region
  "s3-bucket": "AWS_SHARED_BETWEEN_STACK_S3_NAME" // a S3 bucket is needed to upload the modele/output of the stack, must be outside of the stack (shared between all stacks) 
}
```
5. Run `dotnet lambda deploy-serverless AWS_STACK_NAME --template-parameters GitLfsUsername=AWS_STACK_ParameterUsername;GitLfsPassword=AWS_STACK_ParameterPassword -t modele.yaml ` to deploy the stack
6. Run `dotnet lambda deploy-function` to deploy the code of the lambda function
7. Change the .lfconfig of the GIT project to send requests to the lambda function (the URL was in 5. output)
```
[lfs]
url = https://EXAMPLE.execute-api.eu-west-1.amazonaws.com/lfs
```
8. Commit and push LFS files, when prompt enter AWS_STACK_ParameterUsername and AWS_STACK_ParameterPassword, the files can be seen in OHV Storage !

**Instead of using user/password authentication, it is possible to use Github or Bitbucket authentication.**

Edit the `aws-lambda-tools-defaults.json` file and redeploy the lambda (or edit directly in the lambda UI)

(example with github):

 ``` "environment-variables": "GITHUB_ORGANISATION=REPO_ORGANISATION,GITHUB_REPOSITORY=REPO_NAME,..."```
 
 (example with bitbucket):
 
 ``` "environment-variables": "BITBUCKET_WORKSPACE=REPO_WORKSPACE,BITBUCKET_REPOSITORY=REPO_NAME,..."```