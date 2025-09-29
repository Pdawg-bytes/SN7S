using SN7S.Core;
using System.Diagnostics;

namespace SN7S
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            using BinaryReader br = new(File.OpenRead("cocktail hour sn7.vgm"));

            VGMHeader header = VGMParser.ParseHeader(br);
            Console.WriteLine($"Ident: {header.Ident}, Version: {header.Version:X}, PSG Clock: {header.SN76489Clock}");

            List<VGMCommand> cmds = VGMParser.ParseCommands(br, header);

            List<short> samples = [];
            SN76489 psg = new(clockRate: header.SN76489Clock, sampleRate: 44100, lsfrSize: 15);

            Stopwatch sw = Stopwatch.StartNew();
            foreach (var cmd in cmds)
            {
                switch (cmd.Type)
                {
                    case VGMCommandType.PSGWrite:
                        psg.Write(cmd.Data);
                        break;

                    case VGMCommandType.Wait:
                        for (int i = 0; i < cmd.Value; i++)
                            samples.Add(psg.GenerateSample());
                        break;

                    default:
                        break;
                }
            }
            sw.Stop();
            Console.WriteLine($"\nRender time {sw.ElapsedMilliseconds}ms");

            WAVWriter writer = new(44100);
            writer.Write("test.wav", samples);
        }
    }
}