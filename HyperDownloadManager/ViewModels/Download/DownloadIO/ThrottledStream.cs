using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace HyperDownloadManager.ViewModels.Download.DownloadIO
{
    public class ThrottledStream : Stream
    {
        private readonly Stream parent;
        public readonly long maxBytesPerSecond;
        public readonly long maxFileSize;
        private readonly IScheduler scheduler;
        private readonly IStopwatch stopwatch;
        public bool Disposed { get; set; }
        private long processed;
        private bool throttle = true;
        private bool checkFileSize = true;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public string FilePath { get; set; }
        private EventHandler<StreamEventArgs>? _onMaxFile = null;
        public event EventHandler<StreamEventArgs> OnMaxFile
        {
            add
            {
                _onMaxFile = null;
                _onMaxFile = value;
            }
            remove
            {
                _onMaxFile -= value;
            }
        }
        private EventHandler<StreamEventArgs>? _onStreamTermination = null;
        public event EventHandler<StreamEventArgs> OnStreamTermination
        {
            add
            {
                _onStreamTermination = null;
                _onStreamTermination += value;
            }
            remove
            {
                _onStreamTermination -= value;
            }
        }
        public ThrottledStream(Stream parent, long maxBytesPerSecond, long maxFile, string filePath, IScheduler scheduler)
        {
            this.FilePath = filePath;
            this.maxBytesPerSecond = maxBytesPerSecond;
            if (maxBytesPerSecond == 0)
            {
                this.throttle = false;
            }
            this.maxFileSize = maxFile;
            if (this.maxFileSize <= 0)
            {
                this.checkFileSize = false;
            }

            this.parent = parent;
            this.scheduler = scheduler;
            stopwatch = scheduler.StartStopwatch();
            processed = 0;
        }

        public ThrottledStream(Stream parent, long maxBytesPerSecond, long maxFile, string filePath)
            : this(parent, maxBytesPerSecond, maxFile, filePath, Scheduler.Default)
        {
        }
        protected void Throttle(int bytes)
        {
            if (throttle)
            {
                processed += bytes;
                var targetTime = TimeSpan.FromSeconds((double)processed / maxBytesPerSecond);
                var actualTime = stopwatch.Elapsed;
                var sleep = targetTime - actualTime;
                if (sleep > TimeSpan.Zero)
                {
                    using (var waitHandle = new AutoResetEvent(initialState: false))
                    {
                        scheduler.Sleep(sleep, cancellationTokenSource.Token).GetAwaiter().OnCompleted(() => waitHandle.Set());
                        waitHandle.WaitOne();
                    }
                }
            }
        }
        public void Terminate()
        {
            this.cancellationTokenSource.Cancel();
            parent.Flush();
            this.cancellationTokenSource = new CancellationTokenSource();
            if (_onMaxFile != null)
            {
                _onStreamTermination(this, new StreamEventArgs(0, 0));
            }
        }
        public bool Validate()
        {
            //validate function should check on the Throttlers and the cancel token and the users
            if (this.parent != null)
                return true;
            else
                return false;
        }
        public override bool CanRead
        {
            get { return parent.CanRead; }
        }

        public override bool CanSeek
        {
            get { return parent.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return parent.CanWrite; }
        }

        public override void Flush()
        {
            parent.Flush();
        }

        public override long Length
        {
            get { return parent.Length; }
        }

        public override long Position
        {
            get
            {
                return parent.Position;
            }
            set
            {
                parent.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = parent.Read(buffer, offset, count);
            return read;
        }
        public override void Close()
        {
            this.Terminate();
            cancellationTokenSource.Cancel();
            parent?.Flush();
            parent?.Close();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return parent.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            parent.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Throttle(count);
            if (Length <= maxFileSize || !checkFileSize)
            {
                parent.Write(buffer, offset, count);
            }
            else
            {
                if (_onMaxFile != null)
                {
                    _onMaxFile(this, new StreamEventArgs(0, parent.Length));
                }
            }
        }
    }
}
