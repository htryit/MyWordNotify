using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace MyWordNotify
{
    public class MyWords
    {
        List<WordEntity> WordList;
        string WordsFile;
        List<int> HasNotifiedList;
        int CurrentIndex;
        int Total;

        public MyWords()
        {
            WordsFile = $"{AppDomain.CurrentDomain.BaseDirectory}MyWords.json";
            InitWordList();
            HasNotifiedList = new List<int>();
        }

        public List<WordEntity> GetWords(bool IncludeMaster = false)
        {
            return WordList.Where(t => t.Status != EnumWordStatus.Master).ToList();
        }


        public void SaveWordsToFile()
        {
            File.WriteAllText(WordsFile, JsonConvert.SerializeObject(WordList));
        }

        /// <summary>
        /// Initialize word list
        /// </summary>
        /// <exception cref="Exception"></exception>
        void InitWordList()
        {
            if (!File.Exists(WordsFile))
            {
                throw new Exception("No word file.");
            }

           
            var WordJson = File.ReadAllText(WordsFile);
            WordList = JsonConvert.DeserializeObject<List<WordEntity>>(WordJson);
            CurrentIndex = 0;
            Total = WordList.Count;
        }

        public void UpdateWordStatus()
        {
            SaveWordsToFile();
        }

        public string GetIndexInfo()
        {
            return $" - {CurrentIndex}/{Total}";
        }

        public WordEntity GetWord()
        {
            Random rnd = new Random();

            bool IsGet = false;
            int RndNum = -1;
            for (int i = 0; i < WordList.Count; i++)
            {
                while (true)
                {
                    RndNum = rnd.Next(0, WordList.Count - 1);
                    if (!HasNotifiedList.Contains(RndNum) && WordList[RndNum].Status != EnumWordStatus.Master)
                    {
                        HasNotifiedList.Add(RndNum);
                        IsGet = true;
                        CurrentIndex = RndNum;
                        break;
                    }
                }

                if (IsGet)
                {
                    break;
                }
            }

            if (RndNum != -1)
            {
                return WordList[RndNum];
            }

            return null;
        }

        public void TempCovertIntoJsonFile()
        {
            var csvpath = $"{AppDomain.CurrentDomain.BaseDirectory}CEFR0-128.csv";
            var jsonpath = $"{AppDomain.CurrentDomain.BaseDirectory}MyWords.json";
            var wordlist = new List<WordEntity>();
            if (File.Exists(csvpath))
            {

                var ArLines = File.ReadLines(csvpath);
                foreach (var line in ArLines)
                {
                    var ArItem = line.Split(',');
                    var Word = new WordEntity()
                    {
                        Word = ArItem[0],
                        Explain = ArItem[1],
                        Status = EnumWordStatus.Undefined,
                        Audio = ""
                    };

                    wordlist.Add(Word);
                }
            }

            File.WriteAllText(jsonpath, JsonConvert.SerializeObject(wordlist));
            Console.WriteLine("OK");
        }
    }

    public class WordEntity
    {
        public string Word { get; set; }
        public string Explain { get; set; }
        public string Audio { get; set; }
        public EnumWordStatus Status { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum EnumWordStatus
    {
        /// <summary>
        /// 未分类
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// 掌握
        /// </summary>
        Master = 1,

        /// <summary>
        /// 不认识
        /// </summary>
        UnKnown = 2,

        /// <summary>
        /// 不会写
        /// </summary>
        NotSpell = 3,

        /// <summary>
        /// 不会读
        /// </summary>
        NotPronounce = 4
    }

}
