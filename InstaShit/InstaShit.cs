﻿using System;
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
        public InstaShit(bool ignoreSettings = false) : base(ignoreSettings)
        {

        }
        /// <summary>
        /// Gets the InstaShit's settings from settings file or user's input.
        /// </summary>
        /// <param name="ignoreSettings">Specifies if the settings file should be ignored.</param>
        /// <returns>The object of Settings class with loaded values.</returns>
        protected override Settings GetSettings(bool ignoreSettings)
        {
            if (ignoreSettings || !File.Exists(GetFileLocation("settings.json")))
                return GetSettingsFromUser(ignoreSettings);
            return base.GetSettings(ignoreSettings);
        }
        /// <summary>
        /// Gets the InstaShit's settings from user's input.
        /// </summary>
        /// <param name="ignoreSettings">Specifies if the settings file should be ignored.</param>
        /// <returns>The object of Settings class with loaded values.</returns>
        private Settings GetSettingsFromUser(bool ignoreSettings)
        {
            if (ignoreSettings)
                Console.WriteLine("Please enter the folllowing values:");
            else
                Console.WriteLine("Can't find settings file, please enter the following values:");
            Settings settings = new Settings
            {
                Login = GetStringFromUser("Login"),
                Password = GetStringFromUser("Password"),
                MinimumSleepTime = GetIntFromUser("Minimum sleep time (in miliseconds)", 0, Int32.MaxValue),
                IntelligentMistakesData = new List<List<IntelligentMistakesDataEntry>>()
            };
            settings.MaximumSleepTime = GetIntFromUser("Maximum sleep time (in miliseconds)", settings.MinimumSleepTime, Int32.MaxValue);
            Console.Write("Specify IntelligentMistakesData for session number 1 (y/n)? ");
            if (CanContinue())
            {
                do
                {
                    int i = settings.IntelligentMistakesData.Count;
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
        protected override string GetFileLocation(string fileName)
        {
            string assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            return Path.Combine(assemblyLocation, fileName);
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