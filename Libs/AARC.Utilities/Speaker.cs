using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace AARC.Utilities
{
    public static class Speaker
    {
        public static void Say(string text)
        {
            SpeechSynthesizer synthesizer = new SpeechSynthesizer();
            synthesizer.Speak(text);
        }
    }
}
