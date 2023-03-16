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
using System.Windows.Forms;
using System.Reflection;
using Application = System.Windows.Forms.Application;

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

        static NotifyIcon MyNotifyIcon;

        /// <summary>
        /// Application Status
        /// </summary>
        enum EnumRunStatus
        {
            Running = 0,
            Pause = 1,
            Exit = 2
        }

        static void Main()
        {
            Console.WriteLine("My Word Notify is running.");

            InitTray();


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
                Application.DoEvents();

                if(RunStatus == EnumRunStatus.Exit)
                {
                    MyNotifyIcon.Visible = false;
                    break;
                }
               
            }
        }

        private static void InitTray()
        {
            MyNotifyIcon = new NotifyIcon();
            MyNotifyIcon.Icon = new System.Drawing.Icon("AppIcon.ico");
            MyNotifyIcon.Text = "MyWordNotify";
            MyNotifyIcon.Visible = true;

            ContextMenu menu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Text = "Stop";
            item.Index = 0;
            item.Click += MenumItem_Click;
            menu.MenuItems.Add(item);

            MenuItem item3 = new MenuItem();
            item3.Text = "Exit";
            item3.Index = 1;
            item3.Click += MenumItem_Click;
            menu.MenuItems.Add(item3);

            MyNotifyIcon.ContextMenu = menu;
        }

        private static void MenumItem_Click(object sender, EventArgs e)
        {
            var MenuItem = (MenuItem)sender;
            var MenuText = MenuItem.Text;
            switch (MenuText)
            {
                case "Exit":
                    Exit();
                    RunStatus = EnumRunStatus.Exit;
                    break;
                case "Stop":
                    RunStatus = EnumRunStatus.Pause;
                    MenuItem.Text = "Start";
                    Exit();
                    break;
                case "Start":
                    MenuItem.Text = "Stop";
                    RunStatus = EnumRunStatus.Running;
                    SendToast(MyWordHande.GetWord());
                    break;
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


            if (System.IO.File.Exists(AudioFilePath))
            {
                Thread.Sleep(1500);

                IWavePlayer waveOutDevice = new WaveOut();
                AudioFileReader audioFileReader = new AudioFileReader(AudioFilePath);

                waveOutDevice.Init(audioFileReader);
                waveOutDevice.Play();
                Thread.Sleep(2000);
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
