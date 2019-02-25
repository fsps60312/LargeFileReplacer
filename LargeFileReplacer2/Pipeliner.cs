using System;
using System.IO;
using System.Diagnostics;

namespace LargeFileReplacer2
{
    public enum PipeStatus { NotStarted, Running, Finished }
    public abstract class Pipeliner
    {
        #region Status Indicator
        public PipeStatus Status=PipeStatus.NotStarted;
        public Exception Exception=null;
        public long? Progress = null, TotalProgress = null;
        public string StatusString = null;
        #endregion
        protected const int chunkSize = 100000;
        StreamReader reader;
        StreamWriter writer;
        protected string ReadLine() { return reader.ReadLine(); }
        protected string ReadToEnd() { return reader.ReadToEnd(); }
        protected int Read(char[] buffer, int index, int count) {return reader.Read(buffer, index, count); }
        protected int Read() { return reader.Read(); }
        protected void Write(char value) { writer.Write(value); }
        protected void Write(string value) { writer.Write(value); }
        protected void Write(char[]buffer,int index,int count) { writer.Write(buffer, index, count); }
        protected void SetReader(StreamReader reader) { this.reader = reader; }
        protected void SetWriter(StreamWriter writer) { this.writer = writer; }
        protected Pipeliner() { }
        public void Start()
        {
            Trace.Assert(Status == PipeStatus.NotStarted);
            Status = PipeStatus.Running;
            try { Run(); }
            catch (Exception error) { Exception = error; }
            finally { Status = PipeStatus.Finished; }
        }
        protected virtual void Run()
        {
            var buffer = new char[chunkSize];
            while (true)
            {
                var n = Read(buffer, 0, buffer.Length);
                if (n == 0) break;
                Progress += n;
                Write(buffer, 0, n);
            }
        }
        ~Pipeliner() { reader?.Dispose(); writer?.Dispose(); }
    }
}
