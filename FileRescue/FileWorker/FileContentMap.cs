using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Djs.Tools.CopyFile
{
    #region class FileContentMap
    /// <summary>
    /// Mapa postupu kopírování jednoho souboru
    /// </summary>
    public class FileContentMap
    {
        public FileContentMap(string sourceFile, string destinationFile)
        {
            this.FileName = destinationFile;
            this.TotalFileSize = new System.IO.FileInfo(sourceFile).Length;
            this._Init();
        }
        public FileContentMap(string destinationFile, long totalFileSize)
        {
            this.FileName = destinationFile;
            this.TotalFileSize = totalFileSize;
            this._Init();
        }
        private void _Init()
        {
            this.Name = System.IO.Path.GetFileName(this.FileName);
            this.FileBlocks = new Dictionary<long, BlockInfo>();
            this._LoadMetadata();
        }
        public override string ToString()
        {
            return $"{FileName}; Total: {TotalFileSize:N0}; Processed: {Processed:N0} ({ProcessedPct}); Errors: {FileBlocks.Count}";
        }
        /// <summary>
        /// Plné jméno cílového datového souboru
        /// </summary>
        public string FileName { get; private set; }
        /// <summary>
        /// Holé jméno cílového datového souboru bez adresáře
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Celková délka Byte
        /// </summary>
        public long TotalFileSize { get; private set; }
        /// <summary>
        /// Kolik je zpracováno Byte
        /// </summary>
        public long Processed { get; set; }
        /// <summary>
        /// Kolik je zpracováno Procent
        /// </summary>
        public double ProcessedPct { get { return (TotalFileSize <= 0L ? 0d : Math.Round((100d * (double)Processed / (double)TotalFileSize), 1)); } }
        /// <summary>
        /// Chybné bloky, klíčem je pozice počátku
        /// </summary>
        public Dictionary<long, BlockInfo> FileBlocks { get; private set; }
        /// <summary>
        /// Pozice, na které by mělo začít další kopírování v režimu FastCopy
        /// </summary>
        public long StartPositionFastCopy { get { return _GetStartPositionFastCopy(); } }
        /// <summary>
        /// Vrátí hodnotu <see cref="StartPositionFastCopy"/>
        /// </summary>
        /// <returns></returns>
        private long _GetStartPositionFastCopy()
        {
            if (this.FileBlocks.Count == 0) return 0L;
            var maxEnd = this.FileBlocks.Values
                .Where(b => b.IsCorrect)
                .Max(b => b.BlockStartPosition);
            return maxEnd;
        }
        /// <summary>
        /// Vrátí velikost bufferu, pokud čtení začne na dané pozici a buffer má mít nejvýše danou velikost
        /// </summary>
        /// <param name="position"></param>
        /// <param name="blockSize"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal int GetRealBufferSize(long position, int blockSize)
        {
            var remainingLength = this.TotalFileSize - position;
            if (remainingLength <= 0L) return 0;
            if (remainingLength <= blockSize) return (int)remainingLength;
            return blockSize;
        }

        internal void AddBlock(long position, long bufferSize, BlockStateType state, double timeMilisec)
        {
            var blockInfo = new BlockInfo(state, position, bufferSize, timeMilisec);
            AddBlock(blockInfo);
        }

        internal void AddBlock(BlockInfo blockInfo)
        {
            if (!FileBlocks.ContainsKey(blockInfo.BlockStartPosition))
                FileBlocks.Add(blockInfo.BlockStartPosition, blockInfo);
            else
                FileBlocks[blockInfo.BlockStartPosition] = blockInfo;
        }

        #region Metadata o cílovém souboru = popisují uložené bloky, chybějící bloky atd
        /// <summary>
        /// Načte metadata o souboru
        /// </summary>
        private void _LoadMetadata()
        {
            this.MetadataFileName = this.FileName + ".metadata";
            var metaFile = this.MetadataFileName;
            if (!System.IO.File.Exists(metaFile)) return;
            var content = System.IO.File.ReadAllLines(metaFile);
            foreach (var line in content)
            {
                if (TryParseLine(line, 2, out var values))
                {
                    switch (values[0])
                    {
                        case "File":
                            this.FileName = values[1];
                            break;
                        case "Length":
                            if (Int64.TryParse(values[1], out var length))
                                this.TotalFileSize = length;
                            break;
                        case "Processed":
                            if (Int64.TryParse(values[1], out var processed))
                                this.Processed = processed;
                            break;
                        case BlockInfo.MetadataCode:
                            if (BlockInfo.TryParse(values, out var blockInfo))
                                this.AddBlock(blockInfo);
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void FlushMetadata()
        {
            SaveMetadata();
        }
        /// <summary>
        /// Uloží metadata
        /// </summary>
        public void SaveMetadata()
        {
            var tab = TAB;
            var lines = new List<string>();
            lines.Add($"File{tab}{FileName}");
            lines.Add($"Length{tab}{TotalFileSize}");
            lines.Add($"Processed{tab}{Processed}");

            foreach (var block in FileBlocks.Values)
                lines.Add(block.MetadataLine);

            var metaFile = this.MetadataFileName;
            System.IO.File.WriteAllLines(metaFile, lines);
        }
        /// <summary>
        /// Rozdělí text <paramref name="line"/> na prvky v místě delimiteru TAB, ověří že výsledek má nejméně <paramref name="minParts"/> částí, části předá v <paramref name="values"/> a vrátí true.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="minParts"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static bool TryParseLine(string line, int minParts, out string[] values)
        {
            values = null;

            var tab = TAB;
            if (String.IsNullOrEmpty(line) || line.Length <= 10 || !line.Contains(tab)) return false;
            var parts = line.Split(tab);
            if (parts.Length < minParts) return false;
            values = parts;
            return true;
        }
        /// <summary>
        /// Plné jméno souboru descriptoru
        /// </summary>
        public string MetadataFileName { get; private set; }
        /// <summary>
        /// Oddělovač
        /// </summary>
        private const char TAB = '\t';
        #endregion
        #region class BlockInfo : Data o jednom bloku
        /// <summary>
        /// Data o jednom bloku
        /// </summary>
        public class BlockInfo
        {
            public BlockInfo(BlockStateType state, long start, long length, double timeMilisec)
            {
                this.State = state;
                this.BlockStartPosition = start;
                this.BlockLength = length;
                this.TimeMilisec = timeMilisec;
            }
            public static bool TryParse(string[] values, out BlockInfo blockInfo)
            {
                if (values != null && values.Length == 5 && values[0] == MetadataCode)
                {
                    bool hasBlockStartPosition = Int64.TryParse(values[1], out var blockStartPosition);
                    bool hasBlockLength = Int64.TryParse(values[2], out var blockLength);
                    bool hasState = Enum.TryParse< BlockStateType >(values[3], out BlockStateType state);
                    bool hasTimeMilisec = Double.TryParse(values[4], out var timeMilisec);

                    if (hasBlockStartPosition && hasBlockLength && hasState && hasTimeMilisec)
                    {
                        blockInfo = new BlockInfo(state, blockStartPosition, blockLength, timeMilisec);
                        return true;
                    }
                }
                blockInfo = null;
                return false;
            }
            public override string ToString()
            {
                return $"Start: {BlockStartPosition:N0}; Length: {BlockLength:N0}; Result: {State}";
            }
            public string MetadataLine
            {
                get
                {
                    var tab = TAB;
                    return $"{MetadataCode}{tab}{BlockStartPosition}{tab}{BlockLength}{tab}{State}{tab}{TimeMilisec:F3}";
                }
            }
            public const string MetadataCode = "Block";


            /// <summary>
            /// Stav tohoto bloku
            /// </summary>
            public BlockStateType State { get; private set; }
            /// <summary>
            /// Obsahuje true pro blok, který je korektní = jeho <see cref="State"/> == <see cref="BlockStateType.ContainsData"/>.
            /// </summary>
            public bool IsCorrect { get { return (State == BlockStateType.ContainsData); } }
            public long BlockStartPosition { get; private set; }
            public long BlockLength { get; private set; }
            /// <summary>
            /// Pozice konce tohoto bloku
            /// </summary>
            public long BlockEndLength { get { return BlockStartPosition + BlockEndLength; } }
            public double TimeMilisec { get; private set; }
        }
        #endregion

    }
    public enum BlockStateType
    {
        None,
        ContainsData,
        ContainsError,
        FillEmpty
    }
    #endregion
}
