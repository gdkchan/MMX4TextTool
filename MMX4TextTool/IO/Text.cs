using System;
using System.Globalization;
using System.IO;
using System.Text;

using MMX4TextTool.Properties;

namespace MMX4TextTool.IO
{
    /// <summary>
    ///     Handles the Mega Man X4 text format.
    /// </summary>
    class Text
    {
        const string StartOfText = "\r\n[text]";
        const string EndOfText = "\r\n[/text]\r\n";
        const string LineBreak = "\r\n";
        const string EndOfDialog = "------\r\n";

        /// <summary>
        ///     Gets a text string from a Mega Man X4 binary text file.
        /// </summary>
        /// <param name="Data">The binary text file</param>
        /// <returns>The text dump as a string</returns>
        public static string FromBytes(byte[] Data)
        {
            string[] Table = GetTable();
            StringBuilder Output = new StringBuilder();

            using (MemoryStream Input = new MemoryStream(Data))
            {
                BinaryReader Reader = new BinaryReader(Input);
                int DialogsCount = Reader.ReadUInt16() >> 1;
                Input.Seek(0, SeekOrigin.Begin);

                for (int i = 0; i < DialogsCount; i++)
                {
                    Input.Seek(i << 1, SeekOrigin.Begin);
                    ushort Offset = Reader.ReadUInt16();
                    Input.Seek(Offset, SeekOrigin.Begin);

                    int Flag = (Reader.ReadUInt16() >> 11) & 1;
                    Input.Seek(-2, SeekOrigin.Current);
                    Output.Append(string.Format("[{0}]", Flag));

                    bool OldText = false;
                    while (true)
                    {
                        ushort Value = Reader.ReadUInt16();
                        int Character = Value & 0x7ff;
                        int ControlCode = Value >> 11;

                        if (Table[Character] == null)
                            Output.Append(string.Format("\\x{0:X4}", Value));
                        else
                            Output.Append(Table[Character]);

                        bool IsText = (ControlCode & 2) != 0;
                        if (IsText && !OldText) Output.Append(StartOfText);
                        OldText = IsText;

                        if ((ControlCode & 4) != 0) Output.Append(EndOfText);
                        if ((ControlCode & 8) != 0) Output.Append(LineBreak);
                        if ((ControlCode & 0x10) != 0)
                        {
                            Output.Append(EndOfDialog);
                            break;
                        }
                    }
                }
            }

            return Output.ToString();
        }

        /// <summary>
        ///     Encodes a text dump from a string into a binary Mega Man X4 text.
        /// </summary>
        /// <param name="Text">The text to be encoded</param>
        /// <returns>The encoded binary text</returns>
        public static byte[] ToBytes(string Text)
        {
            using (MemoryStream Output = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(Output);

                string[] Table = GetTable();
                string[] Dialogs = Text.Split(new string[] { EndOfDialog }, StringSplitOptions.RemoveEmptyEntries);
                ushort TextOffset = (ushort)(Dialogs.Length << 1);

                for (int i = 0; i < Dialogs.Length; i++)
                {
                    bool IsText = false;
                    bool OldText = false;
                    Text = Dialogs[i];
                    Output.Seek(TextOffset, SeekOrigin.Begin);

                    int Flag = 0;
                    int Position = 0;
                    if (Text.StartsWith("[") && Text.Substring(2, 1) == "]")
                    {
                        Flag = int.Parse(Text.Substring(1, 1));
                        Position += 3;
                    }

                    while (Position < Text.Length)
                    {
                        if (IsTag(Text, "\\x", Position))
                        {
                            string Hex = Text.Substring(Position + 2, 4);
                            ushort Value = ushort.Parse(Hex, NumberStyles.HexNumber);

                            Writer.Write(Value);

                            Position += 6;
                        }
                        else
                        {
                            int ControlCode = Flag;

                            int Value = Array.IndexOf(Table, Text.Substring(Position++, 1));
                            if (Value == -1) Value = Array.IndexOf(Table, "?");

                            if (IsTag(Text, StartOfText, Position))
                            {
                                IsText = true;
                                Position += StartOfText.Length;
                            }

                            if (IsTag(Text, EndOfText, Position))
                            {
                                IsText = false;
                                Position += EndOfText.Length;
                            }

                            if (IsTag(Text, LineBreak, Position))
                            {
                                ControlCode |= 8;
                                Position += LineBreak.Length;
                            }

                            if (IsText) ControlCode |= 2;
                            if (!IsText && OldText) ControlCode |= 4;
                            if (Position == Text.Length) ControlCode |= 0x10;
                            Value |= (ControlCode << 11);
                            OldText = IsText;

                            Writer.Write((ushort)Value);
                        }
                    }

                    ushort NewOffset = (ushort)Output.Position;
                    Output.Seek(i << 1, SeekOrigin.Begin);
                    Writer.Write(TextOffset);
                    TextOffset = NewOffset;
                }

                Output.Seek(TextOffset, SeekOrigin.Begin);
                Writer.Write((ushort)0);

                return Output.ToArray();
            }
        }

        private static bool IsTag(string Text, string Tag, int Position)
        {
            if (Position + Tag.Length > Text.Length) return false;
            return Text.Substring(Position, Tag.Length) == Tag;
        }

        private static string[] GetTable()
        {
            string[] Table = new string[0x10000];
            string[] LineBreaks = new string[] { "\n", "\r\n" };
            string[] TableElements = Resources.CharacterTable.Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries);

            foreach (string Element in TableElements)
            {
                int Position = Element.IndexOf("=");
                int Value = Convert.ToInt32(Element.Substring(0, Position), 16);
                string Character = Element.Substring(Position + 1, Element.Length - Position - 1);

                Table[Value] = Character;
            }

            return Table;
        }
    }
}
