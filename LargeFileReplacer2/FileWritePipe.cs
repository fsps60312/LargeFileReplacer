using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace LargeFileReplacer2
{
    public class StreamWritePipe:Pipeliner
    {
        public StreamWritePipe(string handleString,Stream fileStream)
        {
            Trace.Assert(fileStream.CanWrite);
            SetReader(new StreamReader(new AnonymousPipeClientStream(PipeDirection.In, handleString)));
            SetWriter(new StreamWriter(fileStream));
        }
        protected override void Run()
        {
            base.Run();
            StatusString = "Write OK";
        }
    }
}
