
namespace Microsoft.Azure.Storage.Core.Executor
{
internal enum ExecutorOperation
{
    NotStarted,
    BeginOperation,
    BeginGetRequestStream,
    EndGetRequestStream,
    BeginUploadRequest,
    EndUploadRequest,
    BeginGetResponse,
    EndGetResponse,
    PreProcess,
    GetResponseStream,
    BeginDownloadResponse,
    EndDownloadResponse,
    PostProcess,
    EndOperation,
}

}