using Microsoft.WindowsAzure.Storage.Facade.PhoneSilverlight81.Test.Resources;

namespace Microsoft.WindowsAzure.Storage.Facade.PhoneSilverlight81.Test
{
    /// <summary>
    /// Provides access to string resources.
    /// </summary>
    public class LocalizedStrings
    {
        private static AppResources _localizedResources = new AppResources();

        public AppResources LocalizedResources { get { return _localizedResources; } }
    }
}