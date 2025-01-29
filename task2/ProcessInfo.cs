using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace task2
{
    public class ProcessInfo
    {
        public string Name { get; }
        public string WindowTitle { get; }
        public long Memory { get; }
        public DateTime? StartTime { get; }
        public int Priority { get; }
        public int Threads { get; }
        public int Id { get; }

        public ProcessInfo(Process process)
        {
            Name = process.ProcessName;
            WindowTitle = process.MainWindowTitle;
            Memory = process.WorkingSet64 / 1024 / 1024;
            StartTime = SafeGetStartTime(process);
            Priority = process.BasePriority;
            Threads = process.Threads.Count;
            Id = process.Id;
        }

        private static DateTime? SafeGetStartTime(Process process)
        {
            try { return process.StartTime; }
            catch { return null; }
        }
    }
}
