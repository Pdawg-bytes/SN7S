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

            SN76489 psg = new(clockRate: header.SN76489Clock, sampleRate: 44100, lsfrSize: 15);

            int totalSamples = cmds
                .Where(c => c.Type == VGMCommandType.Wait)
                .Sum(c => c.Value);

            short[] samples = new short[totalSamples];
            int offset = 0;

            Stopwatch sw = Stopwatch.StartNew();
            foreach (var cmd in cmds)
            {
                switch (cmd.Type)
                {
                    case VGMCommandType.PSGWrite:
                        psg.Write(cmd.Data);
                        break;

                    case VGMCommandType.Wait:
                        {
                            var span = samples.AsSpan(offset, cmd.Value);
                            psg.GenerateSamples(span);
                            offset += cmd.Value;
                            break;
                        }

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