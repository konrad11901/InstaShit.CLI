﻿using System.Collections.Generic;

namespace InstaShitCore
{
    public class Settings
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public int MinimumSleepTime { get; set; }
        public int MaximumSleepTime { get; set; }
        public List<List<IntelligentMistakesDataEntry>> IntelligentMistakesData { get; set; }
        public bool AllowTypo { get; set; } = true;
        public bool AllowSynonym { get; set; } = true;
        public bool Debug { get; set; } = false;
    }
}