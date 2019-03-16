// InstaShit - Bot for Insta.Ling which automatically solves daily tasks
// Created by Konrad Krawiec
using System;
using System.Collections.Generic;
using InstaShitCore;
using System.IO;
using Newtonsoft.Json;
using static InstaShit.UserInput;
using System.Reflection;

namespace InstaShit
{
    public class InstaShit : InstaShitCore.InstaShitCore
    {
        private static readonly string baseLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public InstaShit(bool ignoreSettings)
            : base(GetSettings(ignoreSettings), GetWordsDictionary(baseLocation), GetWordsHistory(baseLocation))
        {

        }

        private static string GetFileLocation(string fileName) => Path.Combine(baseLocation, fileName);
        /// <summary>
        /// Gets the InstaShit's settings from settings file or user's input.
        /// </summary>
        /// <param name="ignoreSettings">Specifies if the settings file should be ignored.</param>
        /// <returns>The object of Settings class with loaded values.</returns>
        public static Settings GetSettings(bool ignoreSettings)
        {
            if (ignoreSettings || !File.Exists(GetFileLocation("settings.json")))
                return GetSettingsFromUser(ignoreSettings);
            return GetSettings(baseLocation);
        }
        /// <summary>
        /// Gets the InstaShit's settings from user's input.
        /// </summary>
        /// <param name="ignoreSettings">Specifies if the settings file should be ignored.</param>
        /// <returns>The object of Settings class with loaded values.</returns>
        private static Settings GetSettingsFromUser(bool ignoreSettings)
        {
            Console.WriteLine(ignoreSettings
                ? "Please enter the following values:"
                : "Can't find settings file, please enter the following values:");
            var settings = new Settings
            {
                Login = GetStringFromUser("Login"),
                Password = GetStringFromUser("Password"),
                MinimumSleepTime = GetIntFromUser("Minimum sleep time (in milliseconds)", 0, Int32.MaxValue),
                IntelligentMistakesData = new List<List<IntelligentMistakesDataEntry>>()
            };
            settings.MaximumSleepTime = GetIntFromUser("Maximum sleep time (in milliseconds)", settings.MinimumSleepTime, Int32.MaxValue);
            Console.Write("Specify IntelligentMistakesData for session number 1 (y/n)? ");
            if (CanContinue())
            {
                do
                {
                    var i = settings.IntelligentMistakesData.Count;
                    settings.IntelligentMistakesData.Add(new List<IntelligentMistakesDataEntry>());
                    do
                    {
                        Console.WriteLine($"IntelligentMistakeDataEntry number {settings.IntelligentMistakesData[i].Count + 1}");
                        var entry = new IntelligentMistakesDataEntry
                        {
                            RiskPercentage = GetIntFromUser("Risk of making the mistake (0-100)", 0, 100),
                            MaxNumberOfMistakes = GetIntFromUser("Maximum number of mistakes (-1 = unlimited)", -1, Int32.MaxValue)
                        };
                        settings.IntelligentMistakesData[i].Add(entry);
                        Console.Write("Add another entry (y/n)? ");
                    } while (CanContinue());
                    Console.Write($"Specify IntelligentMistakesData for session number {i + 2} (y/n)? ");
                } while (CanContinue());
                Console.Write("Allow typo in answer (y/n)? ");
                if (!CanContinue())
                    settings.AllowTypo = false;
                Console.Write("Allow synonyms (y/n)? ");
                if (!CanContinue())
                    settings.AllowSynonym = false;
            }
            if (!ignoreSettings)
            {
                Console.Write("Save these settings (y/n)? ");
                if (CanContinue())
                    File.WriteAllText(GetFileLocation("settings.json"), JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            return settings;
        }

        public void SaveSessionData()
        {
            SaveSessionData(baseLocation);
        }

        /// <summary>
        /// Writes the specified string value to the standard output stream if debug mode is turned on.
        /// </summary>
        /// <param name="text">The value to write.</param>
        protected override void Debug(string text)
        { 
            if(DebugMode)
            {
                Console.WriteLine(text);
            }
            base.Debug(text);
        }
    }
}
