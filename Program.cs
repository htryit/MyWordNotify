using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using NAudio;
using NAudio.Wave;
using System.Threading;
using System.Timers;

namespace MyWordNotify
{
    public class Program
    {
        static HttpHelper Http;
        static MyWords MyWordHande;
        static WordEntity CurWord;

        static string AudioFolderPath;

        static System.Timers.Timer MyTimer;

        static EnumRunStatus RunStatus;

        /// <summary>
        /// Application Status
        /// </summary>
        enum EnumRunStatus
        {
            Running = 0,
            Pause = 1
        }

        static void Main()
        {
            Console.WriteLine("My Word Notify is running.");
            Console.WriteLine("Type:\r\n\t exit for quit application.");
            Console.WriteLine("\t go for continue.");
            Console.WriteLine("\t pause for pause.");

            RunStatus = EnumRunStatus.Running;

            Http = new HttpHelper(Encoding.UTF8);

            MyWordHande = new MyWords();
            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;

            MyTimer = new System.Timers.Timer(5000);
            MyTimer.Elapsed += MyTimer_Elapsed;


            AudioFolderPath = $"{AppDomain.CurrentDomain.BaseDirectory}audio";
            if (!System.IO.Directory.Exists(AudioFolderPath))
            {
                Directory.CreateDirectory(AudioFolderPath);
            }

            SendToast(MyWordHande.GetWord());
            while (true)
            {
                var line = Console.ReadLine();
                if (line == "exit")
                {
                    Exit();
                    break;
                }
                else if (line == "go")
                {
                    RunStatus = EnumRunStatus.Running;
                    SendToast(MyWordHande.GetWord());
                }
                else if (line == "pause")
                {
                    RunStatus = EnumRunStatus.Pause;
                    Exit();
                }
            }
        }

        private static void Exit()
        {
            MyTimer.Stop();
            ToastNotificationManagerCompat.History.Clear();
        }

        private static void MyTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (RunStatus == EnumRunStatus.Running)
            {
                NextWord();
            }
        }

        static void SendToast(WordEntity Word)
        {

            ToastNotificationManagerCompat.History.Clear();

            if (Word != null)
            {
                CurWord = Word;
            }
            else
            {
                Console.WriteLine("Completed.");
                return;
            }

            var WordStatus = "";
            switch (Word.Status)
            {
                case EnumWordStatus.Undefined:
                    WordStatus = "Undefined";
                    break;
                case EnumWordStatus.NotSpell:
                    WordStatus = "Not Spell";
                    break;
                case EnumWordStatus.UnKnown:
                    WordStatus = "Don't Known";
                    break;
                case EnumWordStatus.NotPronounce:
                    WordStatus = "Not Pronounce";
                    break;
            }

            WordStatus = $"{WordStatus}{MyWordHande.GetIndexInfo()}";

            new ToastContentBuilder()
                .AddArgument("MyNotifyId", 20230314)
                .AddText(Word.Word)
                .AddText(WordStatus)
                .AddText(Word.Explain)
                .AddButton(new ToastButton()
                    .SetContent("UnKnown")
                    .AddArgument("action", "2")
                    .SetBackgroundActivation()
                    )
                .AddButton(new ToastButton()
                    .SetContent("Spell")
                    .AddArgument("action", "3")
                    .SetBackgroundActivation()
                    )

                .AddButton(new ToastButton()
                    .SetContent("Pronunce")
                    .AddArgument("action", "4")
                    .SetBackgroundActivation()
                    )
                .AddButton(new ToastButton()
                    .SetContent("Master")
                    .AddArgument("action", "1")
                    .SetBackgroundActivation()
                 )
                .Show();



            var AudioFilePath = $"{AudioFolderPath}\\{Word.Word}.mp3";
            if (string.IsNullOrEmpty(Word.Audio))
            {
                //download and fill the entity
                var AudioUrl = $"https://dict.youdao.com/dictvoice?type=0&audio={Word.Word}";
                Http.UrlDownFile(AudioUrl, AudioFilePath);

                if (System.IO.File.Exists(AudioFilePath))
                {
                    Word.Audio = $"{Word.Word}.mp3";
                }
            }


            if (!string.IsNullOrEmpty(Word.Audio))
            {
                if (System.IO.File.Exists(AudioFilePath))
                {
                    Thread.Sleep(1500);

                    IWavePlayer waveOutDevice = new WaveOut();
                    AudioFileReader audioFileReader = new AudioFileReader(AudioFilePath);

                    waveOutDevice.Init(audioFileReader);
                    waveOutDevice.Play();
                    Thread.Sleep(2000);
                }
            }

            if (RunStatus == EnumRunStatus.Running)
            {
                MyTimer.Start();
            }
        }

        private static void NextWord()
        {
            MyTimer.Stop();
            SendToast(MyWordHande.GetWord());
        }

        private static void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {

            MyTimer.Stop();

            ToastArguments Args = ToastArguments.Parse(e.Argument);
            if (Args.TryGetValue("action", out string Act))
            {
                var UserAction = int.Parse(Act);


                if (CurWord != null)
                {
                    if (CurWord.Status != (EnumWordStatus)UserAction)
                    {
                        CurWord.Status = (EnumWordStatus)UserAction;
                        MyWordHande.UpdateWordStatus();
                    }
                }

                SendToast(MyWordHande.GetWord());
            }
            else
            {
                RunStatus = EnumRunStatus.Pause;
            }
        }
    }

}
