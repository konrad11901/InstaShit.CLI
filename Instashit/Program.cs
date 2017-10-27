// InstaShit - Bot for Instaling which automatically solves daily tasks
// Created by Konrad Krawiec
using System;
using System.Threading.Tasks;
using InstaShitCore;

namespace InstaShit
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("InstaShit - Bot for Instaling which automatically solves daily tasks");
            Console.WriteLine("Created by Konrad Krawiec\n");
            InstaShit instaShit;
            if (args.Length == 1 && args[0].ToLower() == "-ignoresettings")
                instaShit = new InstaShit(true);
            else
                instaShit = new InstaShit();
            if (!await instaShit.TryLoginAsync())
            {
                Console.WriteLine("Login failed!");
                return;
            }
            if (await instaShit.IsNewSession())
                Console.WriteLine("Starting new session");
            else
            {
                Console.Write("It looks like session was already started. Inteligent mistake making may be inaccurate.\nContinue (y/n)? ");
                if (!UserInput.CanContinue()) return;
            }
            while(true)
            {
                Answer answer = await instaShit.GetAnswer();
                if (answer == null)
                    break;
                int sleepTime = instaShit.GetSleepTime();
                Console.WriteLine($"Sleeping... ({sleepTime}ms)");
                await Task.Delay(sleepTime);
                bool correctAnswer = answer.Word == answer.AnswerWord ? true : false;
                if(correctAnswer)
                {
                    Console.Write("Attempting to answer ");
                }
                else
                {
                    Console.Write($"Attempting to incorrectly answer (\"{answer.AnswerWord}\") ");
                }
                Console.WriteLine($"question about word \"{answer.Word}\" with id {answer.WordID}");
                if(await instaShit.TryAnswerQuestion(answer.WordID, answer.AnswerWord, correctAnswer))
                    Console.WriteLine("Success!");
                else
                {
                    Console.WriteLine("Oops, something went wrong :( \n Please report this error to the bot's author.");
                    return;
                }
            }
            Console.WriteLine("Session successfully finished.");
            PrintResults(await instaShit.GetResultsAsync());
            Console.WriteLine("Saving session data...");
            instaShit.SaveSessionData();
            Console.WriteLine("FINISHED");
            Console.WriteLine("Press any key to close InstaShit...");
            Console.ReadKey();
        }
        /// <summary>
        /// Prints results of today's training.
        /// </summary>
        /// <param name="childResults">Results of today's training.</param>
        static void PrintResults(ChildResults childResults)
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
