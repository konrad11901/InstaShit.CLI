// InstaShit - Bot for Insta.Ling which automatically solves daily tasks
// Created by Konrad Krawiec
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using InstaShitCore;
using static InstaShit.UserInput;

namespace InstaShit
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("InstaShit - Bot for Insta.Ling which automatically solves daily tasks");
            Console.WriteLine("Created by Konrad Krawiec\n");
            bool ignoreSettings = false, noUserInteraction = false;
            foreach(var arg in args)
            {
                switch (arg.ToLower())
                {
                    case "-ignore-settings":
                    case "--ignore-settings":
                    case "-i":
                        ignoreSettings = true;
                        break;
                    case "-no-user-interaction":
                    case "--no-user-interaction":
                    case "-q":
                        noUserInteraction = true;
                        break;
                    default:
                        Console.WriteLine($"Unknown argument: {arg.ToLower()}");
                        break;
                }
            }
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri("https://instashit.pl")
            };
            try
            {
                string latestVersionString = await client.GetStringAsync("/cli.version");
                Version latestVersion = new Version(latestVersionString);
                var localVersion = Assembly.GetEntryAssembly().GetName().Version;
                if (latestVersion > localVersion)
                {
                    Console.WriteLine("A new version of InstaShit.CLI is available. It is very important " +
                        "to keep all InstaShit products updated, since Insta.Ling creators sometimes make " +
                        "changes that require updating InstaShit.");
                    Console.WriteLine($"Your current version is {localVersion}, while the newest version is {latestVersion}.");
                    Console.WriteLine("Visit https://instashit.pl to download newest InstaShit.CLI.");
                    if (!noUserInteraction)
                    {
                        Console.Write("Continue without updating? (y/n) ");
                        if (!CanContinue())
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while trying to check for updates: " + ex);
            }
            try
            {
                var instaShit = new InstaShit(ignoreSettings);
                if (await instaShit.TryLoginAsync())
                    Console.WriteLine("Successfully logged in!");
                else
                {
                    Console.WriteLine("Login failed!");
                    return;
                }
                if (instaShit.LatestInstaLingVersion != InstaShitCore.InstaShitCore.DefaultInstaLingVersion)
                    Console.WriteLine("Warning: Insta.Ling has been updated since last InstaShit.CLI update. " +
                        "There might be some problems with solving the session.");
                if (await instaShit.IsNewSessionAsync())
                    Console.WriteLine("Starting new session");
                else
                {
                    Console.WriteLine(
                        "It looks like the session has already been started. Intelligent mistake making may be inaccurate.");
                    if (!noUserInteraction)
                    {
                        Console.Write("Continue (y/n)? ");
                        if (!CanContinue())
                            return;
                    }
                }
                while (true)
                {
                    var answer = await instaShit.GetAnswerAsync();
                    if (answer == null)
                        break;
                    var sleepTime = instaShit.SleepTime;
                    Console.WriteLine($"Sleeping... ({sleepTime}ms)");
                    await Task.Delay(sleepTime);
                    var correctAnswer = answer.Word == answer.AnswerWord;
                    Console.Write(correctAnswer
                        ? "Attempting to answer"
                        : $"Attempting to incorrectly answer (\"{answer.AnswerWord}\")");
                    Console.WriteLine($" question about word \"{answer.Word}\" with id {answer.WordId}");
                    if (await instaShit.TryAnswerQuestionAsync(answer))
                        Console.WriteLine("Success!");
                    else
                    {
                        Console.WriteLine("Oops, something went wrong while trying to answer the question.\nPlease report this error to the bot's author.");
                        return;
                    }
                }
                Console.WriteLine("Session successfully finished.");
                PrintResults(await instaShit.GetResultsAsync());
                Console.WriteLine("Saving session data...");
                instaShit.SaveSessionData();
                Console.WriteLine("FINISHED");
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"An error occured while connecting to Insta.Ling. Please check your network connection ({e.Message}).");
            }
            Console.WriteLine("Press any key to close InstaShit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Prints results of today's training.
        /// </summary>
        /// <param name="childResults">Results of today's training.</param>
        private static void PrintResults(ChildResults childResults)
        {
            Console.WriteLine();
            if (childResults.PreviousMark != "NONE")
                Console.WriteLine("Mark from previous week: " + childResults.PreviousMark);
            Console.WriteLine("Days of work in this week: " + childResults.DaysOfWork);
            Console.WriteLine("From extracurricular words: +" + childResults.ExtraParentWords);
            Console.WriteLine("Teacher's words: " + childResults.TeacherWords);
            Console.WriteLine("Extracurricular words in current edition: " + childResults.ParentWords);
            Console.WriteLine("Mark as of today at least: " + childResults.CurrrentMark);
            Console.WriteLine("Days until the end of this week: " + childResults.WeekRemainingDays);
            Console.WriteLine();
        }
    }
}
