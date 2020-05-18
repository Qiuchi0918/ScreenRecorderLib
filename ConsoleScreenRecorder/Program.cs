using ScreenRecorderLib;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ConsoleScreenRecorder
{
    class Program
    {
        private static Stopwatch _stopWatch;
        private static string _outputFolder, _outputFilename;
        static void Main(string[] args)
        {
            Console.Title = "Console Screen Recorder";
            bool restart = false;
            do
            {
                Console.Clear();
                _stopWatch = new Stopwatch();
                var inputDevices = Recorder.GetSystemAudioDevices(AudioDeviceSource.InputDevices);
                var opts = new RecorderOptions
                {
                    VideoOptions = new VideoOptions
                    {
                        Framerate = 60,
                        BitrateMode = BitrateControlMode.Quality,
                        Quality = 100,
                    },
                    AudioOptions = new AudioOptions
                    {
                        AudioInputDevice = inputDevices.First().Key,
                        IsAudioEnabled = true,
                        IsInputDeviceEnabled = true,
                        IsOutputDeviceEnabled = true,
                    }
                };

                Recorder rec = Recorder.CreateRecorder(opts);
                rec.OnRecordingFailed += Rec_OnRecordingFailed;
                rec.OnRecordingComplete += Rec_OnRecordingComplete;
                rec.OnStatusChanged += Rec_OnStatusChanged;

                AskOutputDirectory();
                rec.Record(_outputFolder + _outputFilename);

                ModifiedConsoleWrite(true,
                    new string[] { "Press ","[P]"," to pause\n",
                                   "      ","[R]"," to resume\n",
                                   "      ","[F]"," to finish"},
                    new ConsoleColor[] { ConsoleColor.Cyan, ConsoleColor.Cyan, ConsoleColor.Cyan },
                    new int[] { 1, 4, 7 });

                CancellationTokenSource cts = new CancellationTokenSource();
                var token = cts.Token;
                Task.Run(async () =>
                {
                    while (true)
                    {
                        if (token.IsCancellationRequested)
                            return;
                        if (_stopWatch.IsRunning)
                        {
                            Dispatcher.CurrentDispatcher.Invoke(() =>
                            {
                                DisplayLength(ConsoleColor.Red);
                            });
                        }
                        await Task.Delay(10);
                    }
                }, token);
                while (true)
                {
                    ConsoleKeyInfo info = Console.ReadKey(true);
                    switch (info.Key)
                    {
                        case ConsoleKey.F:
                            cts.Cancel();
                            rec.Stop();
                            DisplayLength(ConsoleColor.Green);
                            goto Stop;
                        case ConsoleKey.P:
                            if (rec.Status == RecorderStatus.Recording)
                            {
                                rec.Pause();
                                DisplayLength(ConsoleColor.Green);
                            }
                            break;
                        case ConsoleKey.R:
                            rec.Resume();
                            break;
                        default:
                            break;
                    }
                }
            Stop:;

                while (true)
                {
                    ConsoleKey consoleKey = Console.ReadKey(true).Key;
                    if (consoleKey == ConsoleKey.O)
                    {
                        Process.Start("explorer", "/select,\"" + _outputFolder + _outputFilename + "\"");
                    }
                    if (consoleKey == ConsoleKey.Enter)
                    {
                        restart = true;
                        break;
                    }
                    if (consoleKey == ConsoleKey.Escape)
                    {
                        restart = false;
                        break;
                    }
                }
                rec?.Dispose();
                rec = null;
            } while (restart);
        }

        private static void Rec_OnStatusChanged(object sender, RecordingStatusEventArgs e)
        {
            switch (e.Status)
            {
                case RecorderStatus.Idle:
                    break;
                case RecorderStatus.Recording:
                    _stopWatch.Start();
                    break;
                case RecorderStatus.Paused:
                    _stopWatch.Stop();
                    break;
                case RecorderStatus.Finishing:
                    break;
            }
        }

        private static void Rec_OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
        {
            _stopWatch?.Stop();

            Console.Clear();
            ModifiedConsoleWrite(true,
                new string[] {
                    "\nFile path: ",e.FilePath+'\n',
                    "Length: ",_stopWatch.Elapsed.ToString()+'\n',
                    "Press ","[O]"," to open output directory\n",
                    "      ","[Enter]"," to start another recording\n",
                    "      ","[ESC]"," to Exit\n"},
                new ConsoleColor[] { ConsoleColor.Cyan, ConsoleColor.Cyan, ConsoleColor.Cyan, ConsoleColor.Cyan },
                new int[] { 1, 5, 8,11 });
        }

        private static void Rec_OnRecordingFailed(object sender, RecordingFailedEventArgs e)
        {
            Console.WriteLine("Recording failed with: " + e.Error);
            _stopWatch?.Stop();
            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
        }

        private static void AskOutputDirectory()
        {
            ModifiedConsoleWrite(true,
                new string[] {
                    "Press ","[D]"," to use default directory ", "\"D:\\Library\\Captures\"\n",
                    "      ","[O]"," to store in a different directroy\n",
                    "      ","[S]"," to select from used directory"},
                new ConsoleColor[] { ConsoleColor.Cyan, ConsoleColor.Cyan, ConsoleColor.Cyan, ConsoleColor.Cyan },
                new int[] { 1, 3, 5, 8 });
            _outputFilename = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".mp4";
        askAgain:
            ConsoleKey option = Console.ReadKey(true).Key;


            string pathFilePath = Directory.GetCurrentDirectory() + "\\SavedPath.txt";
            if (!File.Exists(pathFilePath))
            {
                File.Create(pathFilePath).Close();
            }
            List<string> pathList = new List<string>();
            StreamReader sr = new StreamReader(pathFilePath);
            while (!sr.EndOfStream)
            {
                string curPath = sr.ReadLine();
                pathList.Add(curPath);
            }
            sr.Close();

            switch (option)
            {
                case ConsoleKey.D:
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        _outputFolder = "D:\\Library\\Captures\\";
                        Console.ResetColor();
                        break;
                    }
                case ConsoleKey.O:
                    {
                        Console.Write("Enter Output Directory: ");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        _outputFolder = Console.ReadLine() + '\\';
                        Console.ResetColor();
                        if (Path.IsPathRooted(_outputFolder))
                        {
                            if (pathList.Contains(_outputFolder))
                                break;
                            ModifiedConsoleWrite(true,
                                new string[] {"Press ","[Y]"," to remember the path\n",
                                              "      ","[N]"," otherwise"},
                                new ConsoleColor[] { ConsoleColor.Cyan, ConsoleColor.Cyan },
                            new int[] { 1, 4 });
                            ConsoleKey key = Console.ReadKey().Key;
                            if (key==ConsoleKey.Y)
                            {
                                StreamWriter sw = new StreamWriter(pathFilePath, true);
                                sw.WriteLine(_outputFolder);
                                sw.Close();
                            }
                            break;
                        }
                        else
                        {
                            Console.WriteLine("\rInvalid directory");
                            goto askAgain;
                        }
                    }
                case ConsoleKey.S:
                    {
                        if (pathList.Count==0)
                        {
                            Console.WriteLine("No avaliable path");
                            goto askAgain;
                        }
                        for (int i = 0; i < pathList.Count; i++)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write('[' + i.ToString() + ']');
                            Console.ResetColor();
                            Console.WriteLine(pathList[i]);
                        }
                        Console.WriteLine("Type in the index of the disired directory");
                        string input = Console.ReadLine();
                        int selectedIndex;
                        try
                        {
                            selectedIndex = Convert.ToInt32(input);
                        }
                        catch
                        {
                            Console.WriteLine("Invalid index");
                            goto askAgain;
                        }
                        if (selectedIndex < pathList.Count && Path.IsPathRooted(pathList[selectedIndex]))
                        {
                            _outputFolder = pathList[selectedIndex] + '\\';
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Invaild index or path");
                            goto askAgain;
                        }
                    }
                default:
                    {
                        goto askAgain;
                    }
            }
            Console.Clear();
        }

        private static void DisplayLength(ConsoleColor timeColor)
        {
            ModifiedConsoleWrite(false,
                new string[] { "\rLength: ", String.Format("{0}s:{1}ms        ", _stopWatch.Elapsed.Seconds, _stopWatch.Elapsed.Milliseconds) },
                new ConsoleColor[] { timeColor },
                new int[] { 1 });
        }

        private static void ModifiedConsoleWrite(bool writeline,string [] subStrings,ConsoleColor[] colors,int[] targetSubStringIndex)
        {
            int curColor = 0;
            for (int strIndex = 0; strIndex < subStrings.Length; strIndex++)
            {
                if (targetSubStringIndex.Contains(strIndex))
                {
                    Console.ForegroundColor = colors[curColor++];
                    Console.Write(subStrings[strIndex]);
                    Console.ResetColor();
                    continue;
                }
                else
                {
                    Console.Write(subStrings[strIndex]);
                    continue;
                }
            }
            Console.ResetColor();
            if (writeline)
            {
                Console.WriteLine();
            }
        }
    }
}
