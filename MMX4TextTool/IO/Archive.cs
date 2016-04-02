using System.IO;

namespace MMX4TextTool.IO
{
    /// <summary>
    ///     Handles the Mega Man X4 ARChives.
    /// </summary>
    class Archive
    {
        public ArchiveFile[] Files;

        /// <summary>
        ///     Gets the data from a Archive.
        /// </summary>
        /// <param name="FileName">Full path to the Archive</param>
        /// <returns>The contained data</returns>
        public static Archive FromFile(string FileName)
        {
            Archive Output = new Archive();

            using (FileStream Input = new FileStream(FileName, FileMode.Open))
            {
                BinaryReader Reader = new BinaryReader(Input);

                int TotalFiles = Reader.ReadInt32();
                uint ArchiveLength = Reader.ReadUInt32();
                Output.Files = new ArchiveFile[TotalFiles];

                int Offset = 0x800;
                for (int i = 0; i < TotalFiles; i++)
                {
                    ArchiveFile File = new ArchiveFile();
                    Input.Seek(8 + (i << 3), SeekOrigin.Begin);

                    File.Value = Reader.ReadUInt32();
                    int Length = Reader.ReadInt32();

                    Input.Seek(Offset, SeekOrigin.Begin);
                    Offset = Align(Offset + Length);
                    File.Data = new byte[Length];
                    Input.Read(File.Data, 0, Length);

                    Output.Files[i] = File;
                }
            }

            return Output;
        }

        /// <summary>
        ///     Creates a new Archive.
        /// </summary>
        /// <param name="FileName">The output file path</param>
        /// <param name="Data">The data to be stored</param>
        public static void ToFile(string FileName, Archive Data)
        {
            using (FileStream Output = new FileStream(FileName, FileMode.Create))
            {
                MemoryStream HeaderSect = new MemoryStream();
                MemoryStream DataSect = new MemoryStream();
                BinaryWriter Writer = new BinaryWriter(HeaderSect);

                Writer.Write(Data.Files.Length);
                Writer.Write(0u);

                for (int i = 0; i < Data.Files.Length; i++)
                {
                    byte[] Buffer = Data.Files[i].Data;
                    DataSect.Write(Buffer, 0, Buffer.Length);
                    Align(DataSect);

                    Writer.Write(Data.Files[i].Value);
                    Writer.Write(Buffer.Length);
                }

                Align(HeaderSect);
                HeaderSect.Seek(4, SeekOrigin.Begin);
                int TotalLength = (int)(HeaderSect.Length + DataSect.Length);
                Writer.Write(TotalLength);

                Output.Write(HeaderSect.ToArray(), 0, (int)HeaderSect.Length);
                Output.Write(DataSect.ToArray(), 0, (int)DataSect.Length);

                HeaderSect.Dispose();
                DataSect.Dispose();
            }
        }

        private static int Align(int Value)
        {
            if ((Value & 0x7ff) == 0) return Value;
            return (Value & ~0x7ff) + 0x800;
        }

        private static void Align(Stream Stream)
        {
            while ((Stream.Position & 0x7ff) != 0) Stream.WriteByte(0);
        }
    }
}
