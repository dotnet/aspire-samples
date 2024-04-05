using System.Text;
using Xunit.Abstractions;

namespace SamplesIntegrationTests;

internal class XunitTextWriter(ITestOutputHelper output) : TextWriter
{
    private readonly StringBuilder _sb = new();

    public override Encoding Encoding => Encoding.Unicode;

    public override void Write(char value)
    {
        if (value == '\r' || value == '\n')
        {
            if (_sb.Length > 0)
            {
                output.WriteLine(_sb.ToString());
                _sb.Clear();
            }
        }
        else
        {
            _sb.Append(value);
        }
    }
}
