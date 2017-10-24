// InstaShit - Bot for Instaling which automatically solves daily tasks
// Created by Konrad Krawiec
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;

namespace Instashit
{
    class Program
    {
        // Static variables
        static HttpClientHandler handler;
        static HttpClient client;
        static HttpClient synonymsAPIClient;
        static string assemblyLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        static Random rndGenerator;
        static Settings settings;

        /// <summary>
        /// Writes the specified string value to the standard output stream if debug mode is turned on.
        /// </summary>
        /// <param name="text">The value to write.</param>
        static void Debug(string text)
        {
            if (settings.Debug)
                Console.WriteLine(text);
        }

        /// <summary>
        /// Creates a new, not correct word based on the specified string value.
        /// </summary>
        /// <param name="word">The word to process.</param>
        /// <returns>A word with a mistake.</returns>
        static async Task<string> GetWrongWord(string word)
        {
            //Three possible mistakes:
            //0 - no answer
            //1 - answer with a typo (TODO)
            //2 - synonym
            int mistakeType = rndGenerator.Next(0, 3);
            if (mistakeType == 0)
                return "";
            else if (mistakeType == 1)
            {
                for (int i = 0; i < word.Length - 1; i++)
                {
                    if (word[i] == word[i + 1])
                        return word.Remove(i, 1);
                }
                //This doesn't seem to work well, so it's disabled for now
                /*
                string stringToReplace = "";
                string newString;
                if (word.Contains("c"))
                {
                    stringToReplace = "c";
                    newString = "k";
                }
                else if (word.Contains("t"))
                {
                    stringToReplace = "t";
                    newString = "d";
                }
                else if (word.Contains("d"))
                {
                    stringToReplace = "d";
                    newString = "t";
                }
                else
                    return "";
                var regex = new Regex(Regex.Escape(stringToReplace));
                return regex.Replace(word, newString, 1);
                */
                return "";
            }
            else
            {
                var result = await synonymsAPIClient.GetAsync("/words?ml=" + word);
                var synonyms = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(await result.Content.ReadAsStringAsync());
                if (synonyms.Count == 0)
                    return "";
                if (synonyms.Count == 1)
                    return synonyms[0]["word"].ToString();
                int maxRnd;
                if (synonyms.Count == 2)
                    maxRnd = 2;
                else
                    maxRnd = 3;
                return synonyms[rndGenerator.Next(0, maxRnd)]["word"].ToString();
            }
        }
        /// <summary>
        /// Gets the number of miliseconds since 1970/01/01 (equivalent of JavaScript GetTime() function).
        /// </summary>
        /// <returns>The number of miliseconds since 1970/01/01.</returns>
        static Int64 GetJSTime()
        {
            DateTime dateTime = new DateTime(1970, 1, 1);
            TimeSpan timeSpan = DateTime.Now.ToUniversalTime() - dateTime;
            return (Int64)timeSpan.TotalMilliseconds;
        }
        /// <summary>
        /// Gets the InstaShit's settings from settings file or user's input.
        /// </summary>
        /// <returns>The object of Settings class with loaded values.</returns>
        static Settings GetSettings()
        {
            if (File.Exists(Path.Combine(assemblyLocation, "settings.json")))
                return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Path.Combine(assemblyLocation, "settings.json")));
            Console.WriteLine("Can't find settings file, please enter the following values:");
            Settings settings = new Settings
            {
                Login = GetStringFromUser("Login"),
                Password = GetStringFromUser("Password"),
                MinimumSleepTime = GetIntFromUser("Minimum sleep time (in miliseconds)", 0, Int32.MaxValue)
            };
            settings.MaximumSleepTime = GetIntFromUser("Maximum sleep time (in miliseconds)", settings.MinimumSleepTime, Int32.MaxValue);
            settings.IntelligentMistakesData = new List<List<IntelligentMistakesDataEntry>>();
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

            }
            Console.Write("Save these settings (y/n)? ");
            if (CanContinue())
                File.WriteAllText(Path.Combine(assemblyLocation, "settings.json"), JsonConvert.SerializeObject(settings, Formatting.Indented));
            return settings;
        }
        /// <summary>
        /// Gets the integer from user's input.
        /// </summary>
        /// <param name="valueName">The name of value to get.</param>
        /// <param name="minValue">Minimum accepted value.</param>
        /// <param name="maxValue">Maximum accepted value.</param>
        /// <returns>The integer.</returns>
        static int GetIntFromUser(string valueName, int minValue, int maxValue)
        {
            while (true)
            {
                Console.Write($"{valueName}: ");
                if (!Int32.TryParse(Console.ReadLine(), out int value) || value < minValue || value > maxValue)
                {
                    Console.WriteLine("Wrong input, try again.");
                    continue;
                }
                return value;
            }
        }
        /// <summary>
        /// Gets the string from user's input.
        /// </summary>
        /// <param name="valueName">The name of value to get.</param>
        /// <returns>The string.</returns>
        static string GetStringFromUser(string valueName)
        {
            Console.Write($"{valueName}: ");
            return Console.ReadLine();
        }
        /// <summary>
        /// Checks if the program can continue with its work.
        /// </summary>
        /// <returns>The boolean which specifies if the program can continue.</returns>
        static bool CanContinue()
        {
            string input = Console.ReadLine();
            if (input == "y") return true;
            return false;
        }
        static async Task Main(string[] args)
        {
            Console.WriteLine("InstaShit - Bot for Instaling which automatically solves daily tasks");
            Console.WriteLine("Created by Konrad Krawiec\n");
            settings = GetSettings();
            rndGenerator = new Random();
            handler = new HttpClientHandler();
            client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://instaling.pl")
            };
            synonymsAPIClient = new HttpClient()
            {
                BaseAddress = new Uri("https://api.datamuse.com")
            };
            Debug("Created HttpClients for instaling and datamuse");
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("action", "login"),
                new KeyValuePair<string, string>("from", ""),
                new KeyValuePair<string, string>("log_email", settings.Login),
                new KeyValuePair<string, string>("log_password", settings.Password)
            });
            var result = await client.PostAsync("/teacher.php?page=teacherActions", content);
            Debug("Successfully posted to /teacher.php?page=teacherActions");
            var resultString = await result.Content.ReadAsStringAsync();
            Debug($"Result from /learning/student.php: {resultString}");
            if (resultString.Contains("<title>insta.ling</title>"))
                Console.WriteLine("Successfully logged in!");
            else
            {
                Console.WriteLine("Login failed!");
                return;
            }
            string childID = resultString.Substring(resultString.IndexOf("child_id=", StringComparison.Ordinal) + 9, 6);
            Debug($"childID = {childID}");
            content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("child_id", childID),
                new KeyValuePair<string, string>("repeat", ""),
                new KeyValuePair<string, string>("start", ""),
                new KeyValuePair<string, string>("end", "")
            });
            result = await client.PostAsync("/ling2/server/actions/init_session.php", content);
            var JSONResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(await result.Content.ReadAsStringAsync());
            Debug("JSONResponse from POST /ling2/server/actions/init_session.php: " + await result.Content.ReadAsStringAsync());
            if ((bool)JSONResponse["is_new"])
                Console.WriteLine("Starting new session");
            else
            {
                Console.Write("It looks like session was already started. Inteligent mistake making may be inaccurate.\nContinue (y/n)? ");
                if (!CanContinue()) return;
            }
            Dictionary<string, int> sessionCount;
            if (File.Exists(Path.Combine(assemblyLocation, "wordsHistory.json")))
                sessionCount = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(Path.Combine(assemblyLocation, "wordsHistory.json")));
            else
                sessionCount = new Dictionary<string, int>();
            var wordsCount = new Dictionary<string, int>();
            var mistakesCount = new List<List<int>>();
            for (int i = 0; i < settings.IntelligentMistakesData.Count; i++)
            {
                mistakesCount.Add(new List<int>());
                for (int j = 0; j < settings.IntelligentMistakesData[i].Count; j++)
                    mistakesCount[i].Add(0);

            }
            while (true)
            {
                content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("child_id", childID),
                    new KeyValuePair<string, string>("date", GetJSTime().ToString())
                });
                result = await client.PostAsync("/ling2/server/actions/generate_next_word.php", content);
                Debug(await result.Content.ReadAsStringAsync());
                JSONResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(await result.Content.ReadAsStringAsync());
                if (JSONResponse.ContainsKey("summary"))
                    break;
                string wordID = JSONResponse["id"].ToString();
                string word = JSONResponse["word"].ToString();
                if (!wordsCount.ContainsKey(wordID))
                    wordsCount.Add(wordID, 0);
                bool correctAnswer = true;
                if (!sessionCount.ContainsKey(wordID))
                    sessionCount.Add(wordID, 0);
                if (sessionCount[wordID] != -1 && wordsCount[wordID] != -1 && sessionCount[wordID] < settings.IntelligentMistakesData.Count
                    && wordsCount[wordID] < settings.IntelligentMistakesData[sessionCount[wordID]].Count
                    && (settings.IntelligentMistakesData[sessionCount[wordID]][wordsCount[wordID]].MaxNumberOfMistakes == -1
                    || mistakesCount[sessionCount[wordID]][wordsCount[wordID]] < settings.IntelligentMistakesData[sessionCount[wordID]][wordsCount[wordID]].MaxNumberOfMistakes))
                {
                    int rndPercentage = rndGenerator.Next(1, 101);
                    if (rndPercentage <= settings.IntelligentMistakesData[sessionCount[wordID]][wordsCount[wordID]].RiskPercentage)
                    {
                        correctAnswer = false;
                        mistakesCount[sessionCount[wordID]][wordsCount[wordID]]++;
                        wordsCount[wordID]++;
                    }
                    else
                    {
                        if (wordsCount[wordID] == 0)
                            sessionCount[wordID] = -1;
                        wordsCount[wordID] = -1;
                    }
                }
                var pairsList = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("child_id", childID),
                    new KeyValuePair<string, string>("word_id", wordID),
                    new KeyValuePair<string, string>("version", "43yo4ihw")
                };
                int sleepTime = rndGenerator.Next(settings.MinimumSleepTime, settings.MaximumSleepTime + 1);
                Console.WriteLine($"Sleeping... ({sleepTime}ms)");
                System.Threading.Thread.Sleep(sleepTime);
                if (correctAnswer)
                {
                    pairsList.Add(new KeyValuePair<string, string>("answer", word));
                    Console.Write("Attempting to answer ");
                }
                else
                {
                    string incorrectWord = await GetWrongWord(word);
                    pairsList.Add(new KeyValuePair<string, string>("answer", incorrectWord));
                    Console.Write($"Attempting to incorrectly answer (\"{incorrectWord}\") ");
                }
                content = new FormUrlEncodedContent(pairsList);
                Console.WriteLine($"question about word \"{word}\" with id {wordID}");
                result = await client.PostAsync("/ling2/server/actions/save_answer.php", content);
                JSONResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(await result.Content.ReadAsStringAsync());
                if ((JSONResponse["grade"].ToString() == "1" && correctAnswer) || (JSONResponse["grade"].ToString() == "0" && !correctAnswer))
                    Console.WriteLine("Success!");
                else if (JSONResponse["grade"].ToString() == "2" && !correctAnswer)
                {
                    Console.WriteLine("Success! (synonym)");
                    continue;
                }
                else
                {
                    Console.WriteLine("Oops, something went wrong :( \n Please report this error to the bot's author.");
                    break;
                }
            }
            Console.WriteLine("Saving session data...");
            foreach (var key in sessionCount.Keys.ToList())
                if (sessionCount[key] != -1)
                    sessionCount[key]++;
            File.WriteAllText(Path.Combine(assemblyLocation, "wordsHistory.json"), JsonConvert.SerializeObject(sessionCount, Formatting.Indented));
            Console.WriteLine("FINISHED");
            Console.ReadKey();
        }
    }
}
