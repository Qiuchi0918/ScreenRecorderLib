using ScreenRecorderLib;
using System;
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
            bool restart = false;
            do
            {
                Console.Clear();
                _stopWatch = new Stopwatch();
                var inputDevices = Recorder.GetSystemAudioDevices(AudioDeviceSource.InputDevices);
                var opts = new RecorderOptions
                {
                    AudioOptions = new AudioOptions
                    {
                        AudioInputDevice = inputDevices.First().Key,
                        IsAudioEnabled = true,
                        IsInputDeviceEnabled = true,
                        IsOutputDeviceEnabled = true,
                    },
                };

                Recorder rec = Recorder.CreateRecorder(opts);
                rec.OnRecordingFailed += Rec_OnRecordingFailed;
                rec.OnRecordingComplete += Rec_OnRecordingComplete;
                rec.OnStatusChanged += Rec_OnStatusChanged;

                AskOutputDirectory();
                rec.Record(File.Create(_outputFolder + _outputFilename));
                Console.WriteLine("Press [P] to pause\n" +
                                  "      [R] to resume\n" +
                                  "      [F] to finish");
                Console.Write("Output filename: \"");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine( _outputFolder + _outputFilename + '\"');
                Console.ResetColor();
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
                                Console.ForegroundColor = ConsoleColor.Red;
                                DisplayLength();
                                Console.ResetColor();
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
                            Console.ForegroundColor = ConsoleColor.Green;
                            DisplayLength();
                            Console.ResetColor();
                            goto Stop;
                        case ConsoleKey.P:
                            if (rec.Status == RecorderStatus.Recording)
                            {
                                rec.Pause();
                                Console.ForegroundColor = ConsoleColor.Green;
                                DisplayLength();
                                Console.ResetColor();
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
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nRecording completed");
                Console.ResetColor();
                Console.WriteLine("Press [O] to open output directory\n" +
                                  "      [Enter] to start another recording\n" +
                                  "      [ESC] to Exit");
                while (true)
                {
                    ConsoleKey consoleKey = Console.ReadKey(true).Key;
                    if (consoleKey == ConsoleKey.O)
                    {
                        System.Diagnostics.Process.Start("explorer", "/select,\"" + _outputFolder + _outputFilename + "\"");
                    }
                    if (consoleKey== ConsoleKey.Enter)
                    {
                        restart = true;
                        break;
                    }
                    if (consoleKey== ConsoleKey.Escape)
                    {
                        restart = false;
                        break;
                    }
                }
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
            Console.Write    ("Press [Y] to use default directory ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\"D:\\Library\\Captures\"");
            Console.ResetColor();
            Console.WriteLine("      [N] to store in a different directroy");
            _outputFilename = string.Format("{0}_{1}_{2}_{3}_{4}_{5}.mp4", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
        askAgain:
            ConsoleKey option = Console.ReadKey(true).Key;
            switch (option)
            {
                case ConsoleKey.Y:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    _outputFolder = "D:\\Library\\Captures\\";
                    Console.ResetColor();
                    break;
                case ConsoleKey.N:
                    {
                        Console.Write("Enter Output Directory: ");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        _outputFolder = Console.ReadLine() + '\\';
                        Console.ResetColor();
                        if (Path.IsPathRooted(_outputFolder))
                            break;
                        else
                        {
                            Console.WriteLine("Invalid directory");
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

        private static void DisplayLength()
        {
            Console.Write(String.Format("\rLength: {0}s:{1}ms        ", _stopWatch.Elapsed.Seconds, _stopWatch.Elapsed.Milliseconds));
        }
    }
}
