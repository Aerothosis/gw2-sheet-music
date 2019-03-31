using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WindowsInput;

namespace GW2Music
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private InputSimulator inputSimulator;
        private SongLibrary library;
        private string songName = "";
        private bool runScript = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadLibrary();
            inputSimulator = new InputSimulator();

            songDD.SelectionChanged += LoadSelectedSong;
            List<string> meterOptions = new List<string>
            {
                "1/1",
                "1/2",
                "1/4",
                "1/8",
                "1/16",
            };
            meterDD.ItemsSource = meterOptions;
            meterDD.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    meterDD.SelectedItem = "1/4";
                }));
        }

        private void startBTN_Click(object sender, RoutedEventArgs e)
        {
            runScript = true;
            Song s = RetrieveSong();
            int counter = 0;
            // Tempo items are not needed
            string tempoStr = tempoTB.Text;
            int tempo = 60;
            try
            {
                tempo = int.Parse(tempoStr);
            }
            catch (FormatException) { }
            int fullBar = (int)(60 * 1000);
            int noteDelay = (int)(fullBar / (tempo * s.Meter));
            Thread t = new Thread(() =>
            {
                foreach (Command c in s.Notes)
                {
                    if(!runScript)
                    {
                        break;
                    }
                    switch (c.Type)
                    {
                        case CommandType.KeyUp:
                            PlayKeyUp(c.Key);
                            break;
                        case CommandType.KeyDown:
                            PlayKeyDown(c.Key);
                            break;
                        case CommandType.Key:
                            PlayKey(c.Key);
                            break;
                        case CommandType.Sleep:
                            Thread.Sleep(c.Delay);
                            break;
                    }
                    if (c.UseNoteDelay)
                    {
                        Thread.Sleep(noteDelay);
                    }
                    counter++;
                    SetStatus("Line " + counter + "/" + s.Notes.Count);
                }
                SetStatus("Done. Played " + counter + "/" + s.Notes.Count + " lines");
            });
            Thread timer = new Thread(() =>
            {
                int ctr = 3;
                while(ctr >= 0)
                {
                    SetStatus("Starting in " + ctr + "..." + " Delay: " + noteDelay);
                    ctr--;
                    Thread.Sleep(1000); 
                }
                t.Start();
            });
            timer.Start();
        }

        private void endBTN_Click(object sender, RoutedEventArgs e)
        {
            runScript = false;
        }

        private void saveScriptBTN_Click(object sender, RoutedEventArgs e)
        {
            Song s = ParseSong(scriptTB.Text);
            string name = nameTB.Text;
            string tempoStr = tempoTB.Text;
            int tempo = 60;
            if(name.Length > 0)
            {
                s.Name = name;
                try
                {
                    tempo = int.Parse(tempoStr);
                } catch(FormatException) { }
                s.Tempo = tempo;
                if(library.AddSong(name, s))
                {
                    JsonHandler.SaveLibrary(library);
                    LoadLibrary();
                }
            }
            scriptTB.Text = "";
            nameTB.Text = "";
        }

        private void LoadLibrary()
        {
            library = JsonHandler.LoadLibrary();
            //Song s = TestSong();
            //library.AddSong(s.Name, s);

            //JsonHandler.SaveLibrary(library);

            PopulateSavedSongs();
        }

        private void PopulateSavedSongs()
        {
            songDD.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    songDD.Items.Clear();
                    foreach(Song s in library.Library.Values)
                    {
                        songDD.Items.Add(s.Name);
                    }
                }));
        }

        public void LoadSelectedSong(object o, SelectionChangedEventArgs e)
        {
            string sel = (string)songDD.SelectedItem;
            string songText = "";
            Song s = new Song();
            if(sel != null)
            {
                if (sel.Length > 0)
                {
                    if (library.Library.ContainsKey(sel))
                    {
                        s = library.Library[sel];
                        foreach (Command c in s.Notes)
                        {
                            songText += c.CommandLine + Environment.NewLine;
                        }
                    }
                    else
                    {
                        songText = "SelItem name: " + sel;
                    }

                    scriptTB.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            scriptTB.Text = songText;
                        }));

                    tempoTB.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            tempoTB.Text = s.Tempo + "";
                        }));

                    meterDD.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            string m = "1/" + s.Meter;
                            meterDD.SelectedItem = m;
                        }));
                }
            }
        }

        private void SetStatus(string txt)
        {
            statusLBL.Dispatcher.Invoke(DispatcherPriority.Normal,
                new Action(() =>
                {
                    statusLBL.Content = txt;
                }));
        }

        private Song RetrieveSong()
        {
            if(songName.Length > 0)
            {
                if(library.Library.ContainsKey(songName))
                {
                    return library.Library[songName];
                } else
                {
                    songName = "";
                    return RetrieveSong();
                }
            } else
            {
                return ParseSong(scriptTB.Text);
            }
        }

        private Song ParseSong(string txt)
        {
            Song s = new Song();
            s.Name = "Temp Song";
            List<Command> c = new List<Command>();
            txt.Replace('{', '(');
            txt.Replace('}', ')');
            string[] lines = txt.Split(
                new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach(string l in lines)
            {
                if(l.Length > 0)
                {
                    // Not a blank line
                    c.Add(ParseCommand(l));
                }
            }

            s.Notes = c;
            string tempoStr = tempoTB.Text;
            int tempo = 60;
            try
            {
                tempo = int.Parse(tempoStr);
            } catch(FormatException) { }
            s.Tempo = tempo;

            string meterStr = (string)meterDD.SelectedItem;
            string[] meterStrSpl = meterStr.Split('/');
            int meter = int.Parse(meterStrSpl[1]);
            s.Meter = meter;

            return s;
        }

        private Command ParseCommand(string line)
        {
            Command c = new Command();
            c.CommandLine = line;
            if(line.Contains("SendInput"))
            {
                if(line.Contains("down"))
                {
                    c.Type = CommandType.KeyDown;
                } else if(line.Contains("up"))
                {
                    c.Type = CommandType.KeyUp;
                } else
                {
                    c.Type = CommandType.Key;
                }
                c.Key = ParseKey(line);
                c.UseNoteDelay = c.Key != WindowsInput.Native.VirtualKeyCode.NUMPAD0 && c.Key != WindowsInput.Native.VirtualKeyCode.NUMPAD9;
            } else if(line.Contains("Sleep"))
            {
                c.Type = CommandType.Sleep;
                string[] cmds = line.Split(',');
                if(cmds.Length == 2)
                {
                    int duration = 0;
                    try
                    {
                        duration = int.Parse(cmds[1]);
                    } catch(FormatException) { }
                    c.Delay = duration;
                }
            }
            return c;
        }

        private WindowsInput.Native.VirtualKeyCode ParseKey(string cmd)
        {
            WindowsInput.Native.VirtualKeyCode output = WindowsInput.Native.VirtualKeyCode.NUMPAD1;
            if (cmd.Contains("Numpad1"))
            {
                output = WindowsInput.Native.VirtualKeyCode.NUMPAD1;
            } else if(cmd.Contains("Numpad2"))
            {
                output = WindowsInput.Native.VirtualKeyCode.NUMPAD2;
            } else if(cmd.Contains("Numpad3"))
            {
                output = WindowsInput.Native.VirtualKeyCode.NUMPAD3;
            } else if (cmd.Contains("Numpad4"))
            {
                output = WindowsInput.Native.VirtualKeyCode.NUMPAD4;
            } else if (cmd.Contains("Numpad5"))
            {
                output = WindowsInput.Native.VirtualKeyCode.NUMPAD5;
            } else if(cmd.Contains("Numpad6"))
            {
                output = WindowsInput.Native.VirtualKeyCode.NUMPAD6;
            } else if(cmd.Contains("Numpad7"))
            {
                output = WindowsInput.Native.VirtualKeyCode.NUMPAD7;
            } else if(cmd.Contains("Numpad8"))
            {
                output = WindowsInput.Native.VirtualKeyCode.NUMPAD8;
            } else if(cmd.Contains("Numpad9"))
            {
                output = WindowsInput.Native.VirtualKeyCode.NUMPAD9;
            } else if(cmd.Contains("Numpad0"))
            {
                output = WindowsInput.Native.VirtualKeyCode.NUMPAD0;
            }
            return output;
        }
        
        private void PlayKey(WindowsInput.Native.VirtualKeyCode k)
        {
            inputSimulator.Keyboard.KeyPress(k);
        }

        private void PlayKeyDown(WindowsInput.Native.VirtualKeyCode k)
        {
            inputSimulator.Keyboard.KeyDown(k);
        }

        private void PlayKeyUp(WindowsInput.Native.VirtualKeyCode k)
        {
            inputSimulator.Keyboard.KeyUp(k);
        }
        
    }
}
