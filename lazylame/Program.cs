using Luminescence.Xiph;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace lazylame
{
    internal class Program
    {
        static public string FLACPath = "";
        static public string LAMEPath = "";

        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("No path supplied.");

                return;
            }

            FLACPath = Application.StartupPath + "\\flac.exe";

            LAMEPath = Application.StartupPath + "\\lame.exe";

            if (File.Exists(FLACPath))
            {
                Console.WriteLine("FLAC found...");
            }
            else
            {
                Console.WriteLine("FLAC not found! Exiting...");

                return;
            }

            if (File.Exists(LAMEPath))
            {
                Console.WriteLine("LAME found...");
            }
            else
            {
                Console.WriteLine("LAME not found! Exiting...");

                return;
            }

            if (args[0] == "-a" || args[0] == "-A")
            {
                string WorkingDirectory = "";

                if (args.Length == 1)
                {
                    WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath);
                }
                else
                {
                    //may wanna change this to make it an unlimited list of folders to search through
                    if (args.Length == 2)
                    {
                        WorkingDirectory = args[1];
                    }
                }

                string[] Folders = System.IO.Directory.GetDirectories(WorkingDirectory, "*", System.IO.SearchOption.AllDirectories);

                foreach (string Folder in Folders)
                {
                    ConvertSongsInFolder(Folder);
                }
            }
            else
            {
                foreach (string Path in args)
                {
                    ConvertSongsInFolder(Path);
                }
            }
        }

        private static void ConvertSongsInFolder(string Path)
        {
            if (!Directory.Exists(Path))
            {
                return;
            }

            DirectoryInfo DI = new DirectoryInfo(Path);

            List<FileInfo> Files = new List<FileInfo>();

            Files.AddRange(DI.GetFiles());

            Files = Files.FindAll(delegate(FileInfo f) { return f.Extension.ToLower() == ".flac"; });

            int Count = 0;

            foreach (FileInfo File in Files)
            {
                Count++;

                Console.WriteLine(File.FullName);

                FlacTagger Tags = new FlacTagger(File.FullName);

                string Album = Tags.Album;

                string Artist = Tags.Artist;

                string Date = Tags.Date;

                string Genre = Tags.Genre;

                string Title = Tags.Title;

                string TrackNumber = Tags.TrackNumber;

                string TempName = Path;

                if (TempName[TempName.Length - 1] != '\\')
                {
                    TempName += "\\";
                }

                TempName += System.Guid.NewGuid().ToString("N") + ".wav";

                System.Diagnostics.ProcessStartInfo FLACPSI = new System.Diagnostics.ProcessStartInfo(FLACPath, "-d " + "\"" + File.FullName + "\" --output-name=\"" + TempName + "\"");

                FLACPSI.UseShellExecute = false;

                FLACPSI.WorkingDirectory = Path;

                FLACPSI.ErrorDialog = false;

                FLACPSI.CreateNoWindow = true;

                FLACPSI.RedirectStandardOutput = true;

                try
                {
                    Process P = System.Diagnostics.Process.Start(FLACPSI);

                    P.WaitForExit();

                    System.IO.StreamReader OutputReader = P.StandardOutput;

                    string Output = OutputReader.ReadToEnd();

                    OutputReader.Close();

                    Console.WriteLine(Output);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                string LAMEArgs = "-V 0 --vbr-new --silent --add-id3v2 --ignore-tag-errors --tc \"LAME 3.99r V0\"";

                if (Album != "" && Album != null)
                {
                    Console.WriteLine(Album);

                    LAMEArgs += " --tl \"" + Album + "\"";
                }

                if (Artist != "" && Artist != null)
                {
                    Console.WriteLine(Artist);

                    LAMEArgs += " --ta \"" + Artist + "\"";
                }

                if (Date != "" && Date != null)
                {
                    Console.WriteLine(Date);

                    LAMEArgs += " --ty \"" + Date + "\"";
                }

                if (Genre != "" && Genre != null)
                {
                    Console.WriteLine(Genre);

                    LAMEArgs += " --tg \"" + Genre + "\"";
                }

                if (Title != "" && Title != null)
                {
                    Console.WriteLine(Title);

                    LAMEArgs += " --tt \"" + Title + "\"";
                }

                if (TrackNumber == "" || TrackNumber == null)
                {
                    TrackNumber = Count.ToString();
                }

                if (TrackNumber != "" && TrackNumber != null)
                {
                    if (TrackNumber.Length == 1)
                    {
                        TrackNumber = "0" + TrackNumber;
                    }

                    string NumberOfTracks = Files.Count.ToString();

                    if (NumberOfTracks.Length == 1)
                    {
                        NumberOfTracks = "0" + NumberOfTracks;
                    }

                    Console.WriteLine(TrackNumber);

                    LAMEArgs += " --tn \"" + TrackNumber + "/" + NumberOfTracks + "\"";
                }

                LAMEArgs += " \"" + TempName + "\"";

                string DestinationName = Path;

                if (DestinationName[DestinationName.Length - 1] != '\\')
                {
                    DestinationName += "\\";
                }

                if ((TrackNumber != "") && (Title != ""))
                {
                    DestinationName += TrackNumber + " " + Title + ".mp3";
                }
                else
                {
                    DestinationName += File.Name + ".mp3";
                }

                int FileNamePosition = DestinationName.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1;

                System.Text.StringBuilder SB = new System.Text.StringBuilder();

                SB.Append(DestinationName.Substring(0, FileNamePosition));

                for (int i = FileNamePosition; i < DestinationName.Length; i++)
                {
                    char FileNameChar = DestinationName[i];

                    if ((FileNameChar.Equals('~')) || (FileNameChar.Equals('〜')))
                    {
                        FileNameChar = '_';
                    }
                    else
                    {
                        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                        {
                            if (FileNameChar.Equals(c))
                            {
                                FileNameChar = '_';

                                break;
                            }
                        }
                    }

                    SB.Append(FileNameChar);
                }

                DestinationName = SB.ToString();

                LAMEArgs += " \"" + DestinationName + "\"";

                System.Diagnostics.ProcessStartInfo LAMEPSI = new System.Diagnostics.ProcessStartInfo(LAMEPath, LAMEArgs);

                LAMEPSI.UseShellExecute = false;

                LAMEPSI.WorkingDirectory = Path;

                LAMEPSI.ErrorDialog = false;

                LAMEPSI.CreateNoWindow = true;

                LAMEPSI.RedirectStandardOutput = true;

                try
                {
                    Process P = System.Diagnostics.Process.Start(LAMEPSI);

                    P.WaitForExit();

                    System.IO.StreamReader OutputReader = P.StandardOutput;

                    string Output = OutputReader.ReadToEnd();

                    OutputReader.Close();

                    Console.WriteLine(Output);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                try
                {
                    System.IO.File.Delete(TempName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}