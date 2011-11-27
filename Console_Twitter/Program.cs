using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;

namespace Console_Twitter
{
    class Program
    {
        class TweetInfo
        {
            public string StatusID;
            public string ScreenName;
            public string Name;
            public string Text;

            public TweetInfo(string _statusid, string _screenname, string _name, string _text)
            {
                this.StatusID = _statusid;
                this.ScreenName = _screenname;
                this.Name = _name;
                this.Text = _text;
            }
        }

        static List<TweetInfo> tweet_list = new List<TweetInfo>();
        static List<TweetInfo> temporary_tweet_list = new List<TweetInfo>();

        static string show_text = "What's happening? : ";
        static Encoding text_counter = Encoding.GetEncoding("SHIFT-JIS");

        static void Main(string[] args)
        {
            TwitterLibrary twitter = new TwitterLibrary("Consumer Key", "Consumer Secret");
            Settings.Load();

            twitter.AccessToken = Settings.AccessToken;
            twitter.AccessTokenSecret = Settings.AccessSecret;

            twitter.Login();

            Console.Write(((AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(),typeof(AssemblyTitleAttribute))).Title);
            Console.Write(" Version : " + ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyFileVersionAttribute))).Version);

            Console.WriteLine();
            Console.WriteLine("-----------------------------------------------------------");
            if (twitter.Status == TwitterLibraryStatus.NeedPinOrToken)
            {
                string url = "";
                Console.WriteLine();
                Console.WriteLine("Please wait for get a authorization key....");

                do
                {
                    url = twitter.GetPinURL();
                    if (url == "" || twitter.Status == TwitterLibraryStatus.AuthorizationFailed)
                    {
                        Thread.Sleep(1000);
                    }
                } while (url == "");

                Process.Start(url);

                Console.WriteLine();
                Console.Write("PIN : ");
                twitter.Pin = Console.ReadLine();

                do
                {
                    twitter.Login();
                    if (twitter.Status != TwitterLibraryStatus.Authorized)
                    {
                        Thread.Sleep(1000);
                    }
                } while (twitter.Status != TwitterLibraryStatus.Authorized);

                Settings.AccessToken = twitter.AccessToken;
                Settings.AccessSecret = twitter.AccessTokenSecret;

                Settings.Save();
            }

            Console.WriteLine();
            Console.WriteLine("UserStream Connecting..");
            twitter.UserStreams += new Twitter_UserStream(GetUserStream);
            twitter.BeginUserStream();

            twitter.GetTimeline();

            Console.SetCursorPosition(show_text.Length, Console.CursorSize - 1);

            while (true)
            {
                string tweet = "";

                ShowUserStream();

                tweet = Console.ReadLine();

                if (tweet == null)
                {
                    twitter.EndUserStream();
                    Console.Clear();
                    Console.WriteLine("Bye.");

                    Environment.Exit(0);
                    break;
                }

                if (tweet[0] == '/')
                {
                    int id;
                    bool isDisposal = false;

                    string[] split = tweet.Split(" ".ToCharArray(), 3);
                    if (split.Length >= 2)
                    {
                        if (int.TryParse(split[1], out id))
                        {
                            var tweetinfo = tweet_list[id - 1];

                            isDisposal = true;

                            // COMMANDs.
                            switch (split[0])
                            {
                                case "/rep":
                                    twitter.Update("@" + tweetinfo.ScreenName + " " + split[2], tweetinfo.StatusID);
                                    break;

                                case "/fav":
                                    twitter.Fav(tweetinfo.StatusID);
                                    break;

                                case "/unfav":
                                    twitter.Unfav(tweetinfo.StatusID);
                                    break;

                                case "/del":
                                    twitter.Remove(tweetinfo.StatusID);
                                    break;

                                case "/rt":
                                    twitter.Retweet(tweetinfo.StatusID);
                                    break;

                                default:
                                    isDisposal = false;
                                    break;
                            }
                        }
                        else
                        {
                            switch (split[0])
                            {
                                case "/find":
                                    twitter.Search(split[1]);
                                    break;

                                default:
                                    isDisposal = false;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        isDisposal = true;
                        switch (split[0])
                        {
                            case "/rep":
                                twitter.GetMentions();
                                break;

                            case "/fav":
                                twitter.GetFavorites();
                                break;

                            case "/get":
                                twitter.GetTimeline();
                                break;

                            default:
                                isDisposal = false;
                                break;
                        }

                    }
                    if (!isDisposal)
                    {
                        if (tweet.Length > 1 && tweet[1] == '/')
                        {
                            twitter.Update(tweet.Substring(1), null);
                        }
                        else
                        {
                            Console.Write("[UNKNOWN COMMAND] - REF: /rep, /fav, /unfav, /rt, /del, /find");
                            Thread.Sleep(3000);
                        }
                    }

                }
                else
                {
                    twitter.Update(tweet, null);
                }
                Console.SetCursorPosition(show_text.Length, Console.CursorSize - 1);
            }
        }

        static void GetUserStream(string statusid,string screenname,string name,string text)
        {
            if (Console.CursorLeft == show_text.Length)
            {
                lock (tweet_list)
                {
                    tweet_list.Add(new TweetInfo(statusid,screenname,name,text));
                }
                ShowUserStream();
            }
            else
            {
                lock (temporary_tweet_list)
                {
                    temporary_tweet_list.Add(new TweetInfo(statusid, screenname, name, text));
                }
            }
        }

        static void ShowUserStream()
        {
            lock (tweet_list)
            {
                if (Console.CursorLeft == show_text.Length)
                {
                    int start = 0;
                    int count = 0;

                    Console.Clear();
                    Console.SetCursorPosition(0, Console.WindowTop + 1);

                    if (temporary_tweet_list.Count != 0)
                    {
                        tweet_list.AddRange(temporary_tweet_list);
                        temporary_tweet_list.Clear();
                    }

                    start = 0; count = 0;
                    for (int i = tweet_list.Count - 1; i >= 0; i--)
                    {
                        count += (text_counter.GetByteCount(tweet_list[i].Text) / Console.WindowWidth) + 2 + 1;
                        count += tweet_list[i].Text.Count(x => x == '\n');

                        if (count > (Console.WindowHeight - 3)) break;

                        start++;
                    }

                    if (tweet_list.Count != start)
                    {
                        tweet_list.RemoveRange(0, tweet_list.Count - start);
                    }

                    count = 0;
                    foreach (var i in tweet_list)
                    {
                        count++;
                        Console.WriteLine("["+count.ToString("00")+"] -- "+i.Name + " [ @" + i.ScreenName + " ]\n  " + i.Text);
                        Console.WriteLine("--------");
                    }

                    Console.SetCursorPosition(0, Console.WindowHeight - 1);
                    Console.Write(show_text);
                    Console.SetCursorPosition(show_text.Length, Console.WindowHeight - 1);
                }
            }
        }
    }
}
