﻿using System;
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
        static Encoding text_counter = Encoding.GetEncoding("Shift_JIS");

        static void Main(string[] args)
        {
            TwitterLibrary twitter = new TwitterLibrary("","");
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
                        Thread.Sleep(1000);
                } while (url == "");

                Process.Start(url);

                Console.WriteLine();
                Console.Write("PIN : ");
                twitter.Pin = Console.ReadLine();

                do
                {
                    twitter.Login();

                    if (twitter.Status != TwitterLibraryStatus.Authorized)
                        Thread.Sleep(1000);
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

            while (true)
            {
                string tweet = "";

                Console.SetCursorPosition(show_text.Length, Console.CursorSize - 1);
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
                if (tweet == "") continue;

                if ((tweet[0] == '-' || tweet[0] == '/') && (tweet.Length == 1 || (tweet.Length >= 2 && (tweet[1] != '-' && tweet[1] != '/'))))
                {
                    // イベントが処理されたかどうか。
                    bool Handled = true;
                    string[] split = tweet.Split(" ".ToCharArray(), 3);
                    int id = 0;

                    split[0] = split[0].Replace('-', '/');

                    if (split.Length < 2 || !int.TryParse(split[1], out id))
                    {
                        if (split[0].Length > 1 && int.TryParse(split[0].Substring(1), out id))
                        {
                            if (id > 0)
                            {
                                split = new string[3];

                                if (tweet.IndexOf(" ") != -1)
                                    split[2] = tweet.Substring(tweet.IndexOf(" ") + 1);
                                split[0] = "/rep";
                                split[1] = id.ToString();
                            }
                        }
                    }

                    TweetInfo tweetinfo = null;
                    if (id > 0 && id <= tweet_list.Count)
                        tweetinfo = tweet_list[id - 1];

                    switch (split[0].ToLower())
                    {
                        case "/rep":
                            if (tweetinfo != null)
                            {
                                if (split.Length > 2)
                                    twitter.Update("@" + tweetinfo.ScreenName + " " + split[2], tweetinfo.StatusID);
                                else
                                    twitter.Update("@" + tweetinfo.ScreenName, tweetinfo.StatusID);
                            }
                            else
                                twitter.GetMentions();
                            break;

                        case "/fav":
                            if (tweetinfo != null)
                                twitter.Fav(tweetinfo.StatusID);
                            else
                                twitter.GetFavorites();
                            break;

                        case "/unfav":
                            if (tweetinfo != null)
                                twitter.Unfav(tweetinfo.StatusID);
                            else
                                Handled = false;
                            break;

                        case "/rt":
                            if (tweetinfo != null)
                                twitter.Retweet(tweetinfo.StatusID);
                            else
                                Handled = false;
                            break;

                        case "/del":
                            if (tweetinfo != null)
                                twitter.Remove(tweetinfo.StatusID);
                            else
                                Handled = false;
                            break;

                        case "/find":
                            if (tweetinfo != null && split.Length >= 2)
                                Handled = false;
                            else
                                twitter.Search(tweet.Substring(tweet.IndexOf(" ") + 1));
                            break;

                        case "/get":
                            if (split.Length >= 2)
                                twitter.GetUserTimeline(split[1]);
                            else
                                twitter.GetTimeline();
                            break;

                        case "/exit":
                            Environment.Exit(0);
                            break;

                        default:
                            Handled = false;
                            break;

                    
                    // 以下、入力間違いの補助
                        case "/reply":
                            goto case "/rep";

                        case "/replies":
                            goto case "/rep";

                        case "/re":
                            goto case "/rep";

                        case "/favourite":
                            goto case "/fav";

                        case "/favourites":
                            goto case "/fav";

                        case "/favorite":
                            goto case "/fav";

                        case "/favorites":
                            goto case "/fav";

                        case "/faves":
                            goto case "/fav";

                        case "/fl":
                            goto case "/fav";

                        case "/unfavourite":
                            goto case "/unfav";

                        case "/unfavorite":
                            goto case "/unfav";

                        case "/unfavorites":
                            goto case "/unfav";

                        case "/unfavourites":
                            goto case "/unfav";

                        case "/delete":
                            goto case "/del";

                        case "/search":
                            goto case "/find";

                        case "/quit":
                            goto case "/exit";

                        case "/again":
                            goto case "/get";

                        case "/a":
                            goto case "/get";

                        case "/refresh":
                            goto case "/get";

                        case "/r":
                            goto case "/get";
                    }

                    if (!Handled)
                        PrintError("[UNKNOWN COMMAND] - REF: /rep, /fav, /unfav, /rt, /del, /find, /get");
                }
                else
                {
                    twitter.Update(tweet, null);
                }
            }
        }

        static void PrintError(string error)
        {
            Console.Write(error);
            Thread.Sleep(3000);
        }

        /// <summary>
        /// ユーザストリームを取得する為のコールバック
        /// </summary>
        static void GetUserStream(string statusid,string screenname,string name,string text)
        {
            if (Console.CursorLeft == show_text.Length)
            {
                lock (tweet_list)
                {
                    if (screenname == null && name == null && text == null)
                        tweet_list.RemoveAll(e => e.StatusID == statusid);
                    else
                        tweet_list.Add(new TweetInfo(statusid, screenname, name, text));
                    
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

        /// <summary>
        /// 画面上にTL を表示するためのメソッド
        /// </summary>
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
                        List<string> remove_list = new List<string>();

                        remove_list.AddRange(temporary_tweet_list.Select(x => x.ScreenName == null ? x.StatusID : null));
                        temporary_tweet_list.RemoveAll(x => x.ScreenName == null);

                        tweet_list.AddRange(temporary_tweet_list);
                        tweet_list.RemoveAll(x => remove_list.Contains(x.StatusID));

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
