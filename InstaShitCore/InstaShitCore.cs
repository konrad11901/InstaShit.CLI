// InstaShit - Bot for Instaling which automatically solves daily tasks
// Created by Konrad Krawiec
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace InstaShitCore
{
    public abstract class InstaShitCore
    {
        private HttpClientHandler handler;
        private HttpClient client;
        private HttpClient synonymsAPIClient;
        private Random rndGenerator;
        private Settings settings;
        private string childID;
        private Dictionary<string, int> sessionCount;
        private Dictionary<string, string> words;
        private Dictionary<string, int> wordsCount;
        private List<List<int>> mistakesCount;
        public InstaShitCore(bool ignoreSettings = false)
        {
            handler = new HttpClientHandler();
            client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://instaling.pl")
            };
            synonymsAPIClient = new HttpClient()
            {
                BaseAddress = new Uri("https://api.datamuse.com")
            };
            rndGenerator = new Random();
            this.settings = GetSettings(ignoreSettings);
            if (File.Exists(GetFileLocation("wordsHistory.json")))
                sessionCount = JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(GetFileLocation("wordsHistory.json")));
            else
                sessionCount = new Dictionary<string, int>();
            if (File.Exists(GetFileLocation("wordsDictionary.json")))
                words = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(GetFileLocation("wordsDictionary.json")));
            else
                words = new Dictionary<string, string>();
            wordsCount = new Dictionary<string, int>();
            mistakesCount = new List<List<int>>();
            for (int i = 0; i < settings.IntelligentMistakesData.Count; i++)
            {
                mistakesCount.Add(new List<int>());
                for (int j = 0; j < settings.IntelligentMistakesData[i].Count; j++)
                    mistakesCount[i].Add(0);

            }
        }
        /// <summary>
        /// Gets the location of specified file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>The location of specified file.</returns>
        protected abstract string GetFileLocation(string fileName);
        /// <summary>
        /// Writes the specified string value to the trace listeners if debug mode is turned on.
        /// </summary>
        /// <param name="text">The value to write.</param>
        protected virtual void Debug(string text)
        {
            if(DebugMode)
                System.Diagnostics.Debug.WriteLine(text);
        }
        protected bool DebugMode => settings.Debug;
        /// <summary>
        /// Creates a new, not correct word based on the specified string value.
        /// </summary>
        /// <param name="word">The word to process.</param>
        /// <returns>A word with a mistake.</returns>
        private async Task<string> GetWrongWord(string word)
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
                if (!settings.AllowTypo) return "";
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
                if (!settings.AllowSynonym) return "";
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
        /// Gets the InstaShit's settings from settings file.
        /// </summary>
        /// <returns>The object of Settings class with loaded values.</returns>
        protected virtual Settings GetSettings(bool ignoreSettings)
        {
            if (!ignoreSettings && File.Exists(GetFileLocation("settings.json")))
                return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(GetFileLocation("settings.json")));
            return null;

        }
        /// <summary>
        /// Sends the POST request to the specified URL and returns the result of this request as a string value.
        /// </summary>
        /// <param name="requestUri">The request URL></param>
        /// <param name="content">The content of this POST Request</param>
        /// <returns>Result of POST request.</returns>
        private async Task<string> GetPostResultAsync(string requestUri, HttpContent content)
        {
            var result = await client.PostAsync(requestUri, content);
            return await result.Content.ReadAsStringAsync();
        }
        /// <summary>
        /// Gets results of today's training.
        /// </summary>
        /// <param name="childID">ID of the child.</param>
        /// <returns>Results of today's training.</returns>
        public async Task<ChildResults> GetResultsAsync()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("child_id", childID),
                new KeyValuePair<string, string>("date", GetJSTime().ToString())
            });
            var result = await client.PostAsync("/ling2/server/actions/grade_report.php", content);
            var JSONResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(await result.Content.ReadAsStringAsync());
            ChildResults childResults = new ChildResults();
            if (JSONResponse.ContainsKey("prev_mark"))
                childResults.PreviousMark = JSONResponse["prev_mark"].ToString();
            childResults.DaysOfWork = JSONResponse["work_week_days"].ToString();
            childResults.ExtraParentWords = JSONResponse["parent_words_extra"].ToString();
            childResults.TeacherWords = JSONResponse["teacher_words"].ToString();
            childResults.ParentWords = JSONResponse["parent_words"].ToString();
            childResults.CurrrentMark = JSONResponse["current_mark"].ToString();
            childResults.WeekRemainingDays = JSONResponse["week_remaining_days"].ToString();
            return childResults;
        }
        /// <summary>
        /// Attempts to login to InstaLing.
        /// </summary>
        /// <returns>True if the attempt to login was successful; otherwise, false.</returns>
        public async Task<bool> TryLoginAsync()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("action", "login"),
                new KeyValuePair<string, string>("from", ""),
                new KeyValuePair<string, string>("log_email", settings.Login),
                new KeyValuePair<string, string>("log_password", settings.Password)
            });
            string resultString = await GetPostResultAsync("/teacher.php?page=teacherActions", content);
            Debug("Successfully posted to /teacher.php?page=teacherActions");
            Debug($"Result from /learning/student.php: {resultString}");
            if (!resultString.Contains("<title>insta.ling</title>"))
                return false;
            childID = resultString.Substring(resultString.IndexOf("child_id=", StringComparison.Ordinal) + 9, 6);
            Debug($"childID = {childID}");
            return true;
        }
        /// <summary>
        /// Checks if the currrent session is new.
        /// </summary>
        /// <returns>True if the currentt session is new; otherwise, false.</returns>
        public async Task<bool> IsNewSession()
        {
            if (childID == null)
                throw new InvalidOperationException("User is not logged in");
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("child_id", childID),
                new KeyValuePair<string, string>("repeat", ""),
                new KeyValuePair<string, string>("start", ""),
                new KeyValuePair<string, string>("end", "")
            });
            string resultString = await GetPostResultAsync("/ling2/server/actions/init_session.php", content);
            var JSONResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString);
            Debug("JSONResponse from POST /ling2/server/actions/init_session.php: " + resultString);
            if ((bool)JSONResponse["is_new"])
                return true;
            return false;
        }
        /// <summary>
        /// Generates the next word.
        /// </summary>
        /// <returns>A Dictionary which contains information about generated word.</returns>
        private async Task<Dictionary<string, object>> GenerateNextWordAsync()
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("child_id", childID),
                new KeyValuePair<string, string>("date", GetJSTime().ToString())
            });
            string resultString = await GetPostResultAsync("/ling2/server/actions/generate_next_word.php", content);
            Debug("Result from generate_next_word.php: " + resultString);
            var JSONResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString);
            return JSONResponse;
        }
        /// <summary>
        /// Attempts to answer the question.
        /// </summary>
        /// <param name="answer">Information about the answer.</param>
        /// <returns>True if the attempt to answer the question was successful; otherwise, false.</returns>
        public async Task<bool> TryAnswerQuestion(Answer answer)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("child_id", childID),
                new KeyValuePair<string, string>("word_id", answer.WordID),
                new KeyValuePair<string, string>("version", "43yo4ihw"),
                new KeyValuePair<string, string>("answer", answer.AnswerWord)
            });
            var resultString = await GetPostResultAsync("/ling2/server/actions/save_answer.php", content);
            var JSONResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString);
            if ((JSONResponse["grade"].ToString() == "1" && answer.Word == answer.AnswerWord)
                || ((JSONResponse["grade"].ToString() == "0" || JSONResponse["grade"].ToString() == "2") && answer.Word != answer.AnswerWord))
                return true;
            else
                return false;
        }
        /// <summary>
        /// Checks if the answer to the question about the specified word should be correct or not.
        /// </summary>
        /// <param name="wordID">ID of the word to check.</param>
        /// <returns>True if the answer should be correct; otherwise, false.</returns>
        private bool AnswerCorrectly(string wordID)
        {
            if (sessionCount[wordID] != -1 && wordsCount[wordID] != -1 && sessionCount[wordID] < settings.IntelligentMistakesData.Count
                && wordsCount[wordID] < settings.IntelligentMistakesData[sessionCount[wordID]].Count
                && (settings.IntelligentMistakesData[sessionCount[wordID]][wordsCount[wordID]].MaxNumberOfMistakes == -1
                || mistakesCount[sessionCount[wordID]][wordsCount[wordID]] < settings.IntelligentMistakesData[sessionCount[wordID]][wordsCount[wordID]].MaxNumberOfMistakes))
            {
                int rndPercentage = rndGenerator.Next(1, 101);
                if (rndPercentage <= settings.IntelligentMistakesData[sessionCount[wordID]][wordsCount[wordID]].RiskPercentage)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Gets the time to wait before continuing.
        /// </summary>
        /// <returns>The number of miliseconds to wait.</returns>
        public int GetSleepTime()
        {
            return rndGenerator.Next(settings.MinimumSleepTime, settings.MaximumSleepTime + 1);
        }
        /// <summary>
        /// Gets the information about the answer to the question.
        /// </summary>
        /// <returns>Data about the answer.</returns>
        public async Task<Answer> GetAnswer()
        {
            var wordData = await GenerateNextWordAsync();
            if (wordData.ContainsKey("summary"))
                return null;
            var word = wordData["word"].ToString();
            var wordID = wordData["id"].ToString();
            Answer answer = new Answer
            {
                WordID = wordID,
                Word = word
            };
            if (!wordsCount.ContainsKey(wordID))
                wordsCount.Add(wordID, 0);
            if (!words.ContainsKey(word))
                words.Add(word, wordData["translations"].ToString());
            if (!sessionCount.ContainsKey(wordID))
                sessionCount.Add(wordID, 0);
            bool correctAnswer = AnswerCorrectly(wordID);
            if (!correctAnswer)
            {
                mistakesCount[sessionCount[wordID]][wordsCount[wordID]]++;
                wordsCount[wordID]++;
                answer.AnswerWord = await GetWrongWord(word);
            }
            else
            {
                if (wordsCount[wordID] == 0)
                    sessionCount[wordID] = -1;
                wordsCount[wordID] = -1;
                answer.AnswerWord = word;
            }
            return answer;
        }
        /// <summary>
        /// Saves the current session's data.
        /// </summary>
        public void SaveSessionData()
        {
            foreach (var key in sessionCount.Keys.ToList())
                if (sessionCount[key] != -1)
                    sessionCount[key]++;
            File.WriteAllText(GetFileLocation("wordsHistory.json"), JsonConvert.SerializeObject(sessionCount, Formatting.Indented));
            File.WriteAllText(GetFileLocation("wordsDictionary.json"), JsonConvert.SerializeObject(words, Formatting.Indented));
        }
    }
}