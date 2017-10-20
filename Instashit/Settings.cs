using System;
using System.Collections.Generic;
using System.Text;

namespace Instashit
{
    public class Settings
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public int MinimumSleepTime { get; set; } = 3000;
        public int MaximumSleepTime { get; set; } = 30000;
        public List<List<IntelligentMistakesDataEntry>> IntelligentMistakesData { get; set; }
        public bool Debug { get; set; } = false;
    }
}
