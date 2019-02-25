using System.IO;
using System.IO.Pipes;

namespace LargeFileReplacer2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public abstract class AnonymousePipeliner : Pipeliner
    {
        public string ClientHandleString { get { return pipedServer.GetClientHandleAsString(); } }
        AnonymousPipeServerStream pipedServer;
        protected AnonymousePipeliner(string handleString)
        {
            SetReader(new StreamReader(new AnonymousPipeClientStream(PipeDirection.In, handleString)));
            SetWriter(new StreamWriter(pipedServer = new AnonymousPipeServerStream(PipeDirection.Out)));
        }
    }
}
