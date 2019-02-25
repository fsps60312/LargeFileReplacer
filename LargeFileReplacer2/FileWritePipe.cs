using System.IO;
using System.IO.Pipes;
using System.Diagnostics;

namespace LargeFileReplacer2
{
    public class FileWritePipe:Pipeliner
    {
        public FileWritePipe(string handleString,FileStream fileStream)
        {
            Trace.Assert(fileStream.CanWrite);
            SetReader(new StreamReader(new AnonymousPipeClientStream(PipeDirection.In, handleString)));
            SetWriter(new StreamWriter(fileStream));
        }
    }
}
