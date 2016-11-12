using Luminescence.Xiph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace LazyLame
{
    internal class Program
    {
        private static string flacPath = "";
        private static string lamePath = "";

        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("No path supplied.");

                return;
            }

            flacPath = Application.StartupPath + "\\flac.exe";

            lamePath = Application.StartupPath + "\\lame.exe";

            if (File.Exists(flacPath))
            {
                Console.WriteLine("FLAC found...");
            }
            else
            {
                Console.WriteLine("FLAC not found! Exiting...");

                return;
            }

            if (File.Exists(lamePath))
            {
                Console.WriteLine("LAME found...");
            }
            else
            {
                Console.WriteLine("LAME not found! Exiting...");

                return;
            }

            if (args[0].ToLower() == "-a")
            {
                string workingDirectory = "";

                if (args.Length == 1)
                {
                    workingDirectory = Path.GetDirectoryName(Application.ExecutablePath);
                }
                else
                {
                    // May wanna change this to make it an unlimited list of folders to search through.
                    if (args.Length == 2)
                    {
                        workingDirectory = args[1];
                    }
                }

                string[] folders = System.IO.Directory.GetDirectories(workingDirectory, "*", System.IO.SearchOption.AllDirectories);

                foreach (string folder in folders)
                {
                    ConvertSongsInFolder(folder);
                }
            }
            else
            {
                foreach (string path in args)
                {
                    ConvertSongsInFolder(path);
                }
            }
        }

        private static void ConvertSongsInFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            List<FileInfo> files = new List<FileInfo>();

            files.AddRange(directoryInfo.GetFiles());

            files = files.FindAll(delegate(FileInfo f) { return f.Extension.ToLower() == ".flac"; });

            int count = 0;

            foreach (FileInfo file in files)
            {
                count++;

                Console.WriteLine(file.FullName);

                FlacTagger tags = new FlacTagger(file.FullName);

                string album = tags.Album;

                string artist = tags.Artist;

                string date = tags.Date;

                string genre = tags.Genre;

                string title = tags.Title;

                string trackNumber = tags.TrackNumber;

                string wavTempName = path;

                if (wavTempName[wavTempName.Length - 1] != '\\')
                {
                    wavTempName += "\\";
                }

                wavTempName += System.Guid.NewGuid().ToString("N");

                string flacTempName = wavTempName;

                wavTempName += ".wav";

                flacTempName += ".flac";

                System.IO.File.Copy(file.FullName, flacTempName);

                System.Diagnostics.ProcessStartInfo flacProcessStartInfo = new System.Diagnostics.ProcessStartInfo(flacPath, "--silent -d " + "\"" + flacTempName + "\" --output-name=\"" + wavTempName + "\"");

                flacProcessStartInfo.UseShellExecute = false;

                flacProcessStartInfo.WorkingDirectory = path;

                flacProcessStartInfo.ErrorDialog = false;

                flacProcessStartInfo.CreateNoWindow = true;

                flacProcessStartInfo.RedirectStandardOutput = true;

                flacProcessStartInfo.RedirectStandardError = true;

                try
                {
                    Process flacProcess = System.Diagnostics.Process.Start(flacProcessStartInfo);

                    flacProcess.WaitForExit();

                    System.IO.StreamReader flacStdoutReader = flacProcess.StandardOutput;

                    string flacStdout = flacStdoutReader.ReadToEnd();

                    flacStdoutReader.Close();

                    Console.WriteLine(flacStdout);

                    System.IO.StreamReader flacStderrReader = flacProcess.StandardError;

                    string flacStderr = flacStderrReader.ReadToEnd();

                    flacStderrReader.Close();

                    Console.WriteLine(flacStderr);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                try
                {
                    System.IO.File.Delete(flacTempName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                string lameArgs = "-V 0 --vbr-new --silent --add-id3v2 --ignore-tag-errors --tc \"LAME 3.99r V0\"";

                if (album != "" && album != null)
                {
                    Console.WriteLine(album);

                    lameArgs += " --tl \"" + album + "\"";
                }

                if (artist != "" && artist != null)
                {
                    Console.WriteLine(artist);

                    lameArgs += " --ta \"" + artist + "\"";
                }

                if (date != "" && date != null)
                {
                    Console.WriteLine(date);

                    lameArgs += " --ty \"" + date + "\"";
                }

                if (genre != "" && genre != null)
                {
                    Console.WriteLine(genre);

                    lameArgs += " --tg \"" + genre + "\"";
                }

                if (title != "" && title != null)
                {
                    Console.WriteLine(title);

                    lameArgs += " --tt \"" + title + "\"";
                }

                if (trackNumber == "" || trackNumber == null)
                {
                    trackNumber = count.ToString();
                }

                if (trackNumber != "" && trackNumber != null)
                {
                    if (trackNumber.Length == 1)
                    {
                        trackNumber = "0" + trackNumber;
                    }

                    string numberOfTracks = files.Count.ToString();

                    if (numberOfTracks.Length == 1)
                    {
                        numberOfTracks = "0" + numberOfTracks;
                    }

                    Console.WriteLine(trackNumber);

                    lameArgs += " --tn \"" + trackNumber + "/" + numberOfTracks + "\"";
                }

                lameArgs += " \"" + wavTempName + "\"";

                string destinationName = path;

                if (destinationName[destinationName.Length - 1] != '\\')
                {
                    destinationName += "\\";
                }

                if ((trackNumber != "") && (title != ""))
                {
                    destinationName += trackNumber + " " + title + ".mp3";
                }
                else
                {
                    destinationName += file.Name + ".mp3";
                }

                int fileNamePosition = destinationName.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1;

                System.Text.StringBuilder destinationNameBuilder = new System.Text.StringBuilder();

                destinationNameBuilder.Append(destinationName.Substring(0, fileNamePosition));

                for (int i = fileNamePosition; i < destinationName.Length; i++)
                {
                    char fileNameChar = destinationName[i];

                    if (fileNameChar.Equals('~') || fileNameChar.Equals('〜'))
                    {
                        fileNameChar = '_';
                    }
                    else
                    {
                        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                        {
                            if (fileNameChar.Equals(c))
                            {
                                fileNameChar = '_';

                                break;
                            }
                        }
                    }

                    destinationNameBuilder.Append(fileNameChar);
                }

                destinationName = destinationNameBuilder.ToString();

                lameArgs += " \"" + destinationName + "\"";

                System.Diagnostics.ProcessStartInfo lameProcessStartInfo = new System.Diagnostics.ProcessStartInfo(lamePath, lameArgs);

                lameProcessStartInfo.UseShellExecute = false;

                lameProcessStartInfo.WorkingDirectory = path;

                lameProcessStartInfo.ErrorDialog = false;

                lameProcessStartInfo.CreateNoWindow = true;

                lameProcessStartInfo.RedirectStandardOutput = true;

                try
                {
                    Process lameProcess = System.Diagnostics.Process.Start(lameProcessStartInfo);

                    lameProcess.WaitForExit();

                    System.IO.StreamReader lameOutputReader = lameProcess.StandardOutput;

                    string lameOutput = lameOutputReader.ReadToEnd();

                    lameOutputReader.Close();

                    Console.WriteLine(lameOutput);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                try
                {
                    System.IO.File.Delete(wavTempName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}