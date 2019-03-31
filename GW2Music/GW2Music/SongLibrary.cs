using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GW2Music
{
    public enum CommandType
    {
        None,
        Sleep,
        KeyUp,
        KeyDown,
        Key,
    }

    class Command
    {
        public CommandType Type { get; set; } = CommandType.None;
        public int Delay { get; set; } = 0;
        public bool UseNoteDelay { get; set; } = false;
        public WindowsInput.Native.VirtualKeyCode Key { get; set; }
        public string CommandLine { get; set; }

        public Command() { }
    }

    class Song
    {
        public string Name { get; set; } = "Demo song name";
        public List<Command> Notes { get; set; }
        public int Tempo { get; set; } = 60;
        public int Meter { get; set; } = 1;

        public Song() { }
    }
    
    class SongLibrary
    {
        public Dictionary<string, Song> Library = new Dictionary<string, Song>();

        public SongLibrary() { }

        public bool AddSong(string name, Song song)
        {
            bool output = false;
            if(!Library.ContainsKey(name))
            {
                Library.Add(name, song);
                output = true;
            }
            return output;
        }

        public Song GetSong(string name)
        {
            if(Library.ContainsKey(name))
            {
                return Library[name];
            } else
            {
                return null;
            }
        }
    }
}
