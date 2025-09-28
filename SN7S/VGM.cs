using System.Text;

namespace SN7S
{
    public enum VGMCommandType
    {
        PSGWrite,   // 0x50
        Wait,       // 0x61, 0x62, 0x63, 0x70-0x7F
        EndOfData,  // 0x66
        Other       // Any other command
    }

    public record struct VGMCommand(VGMCommandType Type, int Value, byte Data);

    public class VGMHeader
    {
        public string Ident { get; set; } = "";
        public uint EofOffset { get; set; }
        public uint Version { get; set; }
        public uint SN76489Clock { get; set; }
        public uint GD3Offset { get; set; }
        public uint DataOffset { get; set; }
    }

    public class VGMParser
    {
        public static VGMHeader ParseHeader(BinaryReader br)
        {
            br.BaseStream.Seek(0, SeekOrigin.Begin);

            var ident = Encoding.ASCII.GetString(br.ReadBytes(4));
            if (ident != "Vgm ")
                throw new Exception("Not a VGM file");

            var header = new VGMHeader
            {
                Ident = ident,
                EofOffset = br.ReadUInt32(),
                Version = br.ReadUInt32(),
                SN76489Clock = br.ReadUInt32()
            };

            br.BaseStream.Seek(0x14, SeekOrigin.Begin);
            header.GD3Offset = br.ReadUInt32();
            br.BaseStream.Seek(0x34, SeekOrigin.Begin);
            header.DataOffset = br.ReadUInt32();

            if (header.DataOffset == 0)
                header.DataOffset = 0x40;

            return header;
        }

        public static List<VGMCommand> ParseCommands(BinaryReader br, VGMHeader header)
        {
            List<VGMCommand> cmds = [];

            br.BaseStream.Seek(header.DataOffset + 0x34, SeekOrigin.Begin);
            while (true)
            {
                byte cmd = br.ReadByte();
                switch (cmd)
                {
                    case 0x50:
                        byte data = br.ReadByte();
                        cmds.Add(new VGMCommand(VGMCommandType.PSGWrite, 0, data));
                        break;

                    case 0x61:
                        ushort n = br.ReadUInt16();
                        cmds.Add(new VGMCommand(VGMCommandType.Wait, n, 0));
                        break;

                    case 0x62:
                        cmds.Add(new VGMCommand(VGMCommandType.Wait, 735, 0));
                        break;

                    case 0x63:
                        cmds.Add(new VGMCommand(VGMCommandType.Wait, 882, 0));
                        break;

                    case 0x66:
                        cmds.Add(new VGMCommand(VGMCommandType.EndOfData, 0, 0));
                        return cmds;

                    default:
                        if (cmd >= 0x70 && cmd <= 0x7F)
                            cmds.Add(new VGMCommand(VGMCommandType.Wait, (cmd & 0x0F) + 1, 0));
                        else
                            cmds.Add(new VGMCommand(VGMCommandType.Other, cmd, 0));
                        break;
                }
            }
        }
    }
}