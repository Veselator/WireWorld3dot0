using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WireWorld3dot0
{
    public static class LogManager
    {
        private static string _logs = string.Empty;
        private static int _logsCount = 0;
        private const int _maxLogCount = 100;

        public static void addNote(string note)
        {
            if (_logsCount > _maxLogCount) return;
            _logsCount++;
            _logs += $" [{DateTime.Now.ToString("HH:mm:ss")}] {note}\n";
        }

        public static void printLogs()
        {
            if (_logsCount > _maxLogCount) _logs += $" Частина логів обрізана через максимальний ліміт в {_maxLogCount}";
            using (StreamWriter sw = new StreamWriter("Logs.txt"))
            {
                sw.Write(_logs);
            }
        }
    }
}
