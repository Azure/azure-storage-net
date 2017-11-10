using System.Text;
namespace Microsoft.Azure.Storage.Core
{
internal class CanonicalizedString
{
    private const int DefaultCapacity = 300;
    private const char ElementDelimiter = '\n';

    public CanonicalizedString(string initialElement)
      : this(initialElement, 300)
    {
        throw new System.NotImplementedException();
    }
    public CanonicalizedString(string initialElement, int capacity)
    {
        throw new System.NotImplementedException();
    }
    public void AppendCanonicalizedElement(string element)
    {
        throw new System.NotImplementedException();
    }
    public override string ToString()
    {
        throw new System.NotImplementedException();
    }
}

}