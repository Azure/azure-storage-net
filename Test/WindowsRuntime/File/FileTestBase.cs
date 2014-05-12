using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.File
{
    public partial class FileTestBase : TestBase
    {
        public static async Task<List<string>> CreateFilesAsync(CloudFileShare share, int count)
        {
            string name;
            List<string> files = new List<string>();
            for (int i = 0; i < count; i++)
            {
                name = "ff" + Guid.NewGuid().ToString();
                CloudFile file = share.GetRootDirectoryReference().GetFileReference(name);
                await file.CreateAsync(0);
                files.Add(name);
            }
            return files;
        }

        public static async Task<IEnumerable<IListFileItem>> ListFilesAndDirectoriesAsync(CloudFileDirectory directory, int? maxResults, FileRequestOptions options, OperationContext operationContext)
        {
            List<IListFileItem> results = new List<IListFileItem>();
            FileContinuationToken token = null;
            do
            {
                FileResultSegment resultSegment = await directory.ListFilesAndDirectoriesSegmentedAsync(maxResults, token, options, operationContext);
                results.AddRange(resultSegment.Results);
                token = resultSegment.ContinuationToken;
            }
            while (token != null);
            return results;
        }
    }
}
