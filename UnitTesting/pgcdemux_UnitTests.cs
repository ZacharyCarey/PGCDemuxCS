using PgcDemuxCS;
using PgcDemuxCS;
using PgcDemuxCS.DVD;
using System.Diagnostics;

namespace UnitTesting
{
    public struct TestParams
    {
        public string IfoFileName;
        public ModeType Mode; //PGC, VID, CID
        public int ID; // PGC, VID, CID
        public int? CID = null;
        public DomainType Domain;
        public int? Angle = null;
        public bool ExtractVideo = false;
        public bool ExtractAudio = false;
        public bool ExtractSubs = false;
        public bool VOB = false;
        public bool CustomVOB = false;

        /// <summary>
        /// When true, splits the VOB every GB. False will keep as one big file.
        /// </summary>
        public bool SplitVOB = false;
        public bool GenerateCellt = false;
        public bool GenerateLog = false;
        
        public TestParams()
        {

        }

        private string GetModeSwitch()
        {
            switch(this.Mode)
            {
                case ModeType.PGC: return "-pgc";
                case ModeType.VID: return "-vid";
                case ModeType.CID: return "-cid";
                default: throw new Exception("Invalid mode.");
            }
        }

        private string GetDomain()
        {
            switch(this.Domain)
            {
                case DomainType.Menus: return "-menu";
                case DomainType.Titles: return "-title";
                default: throw new Exception("Invalid domain");
            }
        }

        public string ExpectedArgs
        {
            get
            {
                List<string> args = new();
                args.Add(GetModeSwitch());
                args.Add(ID.ToString());
                if (CID != null) args.Add(CID.Value.ToString());
                args.Add(GetDomain());
                if (Angle != null) args.Add($"-ang {Angle}");
                args.Add(ExtractVideo ? "-m2v" : "-nom2v");
                args.Add(ExtractAudio ? "-aud" : "-noaud");
                args.Add(ExtractSubs ? "-sub" : "-nosub");
                args.Add(VOB ? "-vob" : "-novob");
                if (CustomVOB)
                {
                    args.Add("-customvob");
                    args.Add($"{(SplitVOB ? "b" : "")}nvasl");
                }
                args.Add(GenerateCellt ? "-cellt" : "-nocellt");
                args.Add(GenerateLog ? "-log" : "-nolog");
                args.Add("${DVD_ROOT}\\VIDEO_TS\\" + IfoFileName);
                args.Add("${DEST}");

                return string.Join(" ", args);
            }
        }

        /*public Action<IIfoFileReader, int, string> ActualArgs
        {
            get
            {
                if (Mode == ModeType.PGC)
                {
                    if (CustomVOB)
                    {
                        return (IIfoFileReader reader, int title, string output) => PgcDemux.ExtractPgc(reader, title, Domain == DomainType.Menus, ID, Angle, output);
                    }
                }

                return new Action(() =>
                {
                    if (Mode == ModeType.PGC)
                    {

                    }

                    PgcDemux.ExtractPgc
                });

                PgcDemuxOptions options = new(IfoFileName, "${DEST}");
                options.Mode = this.Mode;
                if (this.Mode == ModeType.PGC)
                {
                    options.SelectedPGC = this.ID;
                }
                else if (this.Mode == ModeType.VID)
                {
                    options.SelectedVID = this.ID;
                }
                else
                {
                    options.SelectedVID = this.ID;
                    options.SelectedCID = this.CID.Value;
                }
                options.Domain = this.Domain;
                if (this.Angle != null) options.SelectedAngle = this.Angle.Value;
                options.ExtractVideoStream = this.ExtractVideo;
                options.ExtractAudioStream = this.ExtractAudio;
                options.ExtractSubtitleStream = this.ExtractSubs;
                options.ExportVOB = this.CustomVOB;

                return options;
            }
        }*/
    }

    public class FileManager
    {
        public readonly string ActualPath;
        public readonly string ExpectedPath;
        public bool DeleteExpectedCache = true;
        public readonly bool NewFolder;

        public FileManager(string expectedPath, string actualPath)
        {
            NewFolder = !Directory.Exists(expectedPath);
            Directory.CreateDirectory(expectedPath);

            try
            {
                Directory.Delete(actualPath, true);
            }
            catch (Exception ex) { }
            Directory.CreateDirectory(actualPath);

            ActualPath = actualPath;
            ExpectedPath = expectedPath;
        }

        ~FileManager()
        {
            if (DeleteExpectedCache)
            {
                try
                {
                    Directory.Delete(ExpectedPath, true);
                }
                catch (Exception ex) { }
            }

            try
            {
                Directory.Delete(ActualPath, true);
            }
            catch (Exception ex) { }
        }
    }

    public abstract class BaseTest
    {
        private const string PgcDemuxPath = "C:\\Users\\Zack\\Downloads\\PgcDemux_1205_exe\\PgcDemux.exe";
        private const string DvdBackupPath = "C:\\Users\\Zack\\Videos";
        private const string TestCachePath = "C:\\Users\\Zack\\Downloads\\UnitTestCache\\";
        private const bool ReadISO = true;

        private readonly string DvdName;
        private readonly lsdvd Info;

        protected BaseTest(string dvdName, string infoFileName)
        {
            this.DvdName = dvdName;
            this.Info = lsdvd.Load(infoFileName);
        }

        private string FillVariables(string input, string dest, params string[] vars)
        {
            input = input.Replace("${DEST}", dest);
            input = input.Replace("${DVD_ROOT}", $"{DvdBackupPath}\\{DvdName}");

            // Replace optional args
            for (int i = 0; i < vars.Length; i += 2)
            {
                input = input.Replace(vars[i], vars[i + 1]);
            }

            return input;
        }

        private void CompareOutput(string cacheFolder, string expectedCommand, Func<IIfoFileReader, string, bool> actualCommand)
        {
            FileManager cache = new FileManager(Path.Combine(TestCachePath, cacheFolder, "Expected"), Path.Combine(TestCachePath, cacheFolder, "Actual"));

            // Replace variables in command
            expectedCommand = FillVariables(expectedCommand, cache.ExpectedPath);
            //FillVariables(actualCommand, cache.ActualPath);

            // Run PgcDemux.exe
            Process? proc = null;
            if (cache.NewFolder)
            {
                if (ReadISO) throw new Exception("Can't run PgcDemux using the ISO.");
                proc = Process.Start(PgcDemuxPath, expectedCommand);
            }

            // Run PgcDemuxCS
            if (ReadISO)
            {
                var reader = new DiscReader(Path.Combine(DvdBackupPath, $"{DvdName}.iso"));
                Assert.IsTrue(actualCommand(reader, cache.ActualPath));
                reader.Dispose();
            }
            else
            {
                var reader = new SimpleIfoReader(Path.Combine(DvdBackupPath, DvdName));
                Assert.IsTrue(actualCommand(reader, cache.ActualPath));
            }

            if (proc != null) proc.WaitForExit();


            // Verify files are the same
            string[] expectedFiles = Directory.GetFiles(cache.ExpectedPath).Where(x => Path.GetExtension(x) != ".sup").ToArray();
            string[] actualFiles = Directory.GetFiles(cache.ActualPath);
            Assert.AreEqual(expectedFiles.Length, actualFiles.Length, "Number of generated files did not match.");

            // Check file contents
            foreach (var expectedFilePath in expectedFiles)
            {
                string fileName = Path.GetFileName(expectedFilePath);
                var actualFilePath = Path.Combine(cache.ActualPath, fileName);
                Assert.IsTrue(File.Exists(actualFilePath), $"Could not find expected file '{fileName}'.");

                string errMsg = $"Actual file {fileName} did not match the expected file.";
                byte[] expectedBuffer = new byte[4096];
                byte[] actualBuffer = new byte[4096];
                using (Stream expectedStream = File.OpenRead(expectedFilePath))
                {
                    using (Stream actualStream = File.OpenRead(actualFilePath))
                    {
                        Assert.AreEqual(expectedStream.Length, actualStream.Length, "File sizes did not match.");
                        bool eof = false;
                        while (!eof)
                        {
                            int expectedCount = expectedStream.Read(expectedBuffer, 0, expectedBuffer.Length);
                            int actualCount = actualStream.Read(actualBuffer, 0, actualBuffer.Length);
                            Assert.IsTrue(expectedCount == actualCount, "Read different amount of bytes from the files.");
                            if (expectedCount == 0 || actualCount == 0) eof = true;
                            Assert.IsTrue(expectedBuffer.SequenceEqual(actualBuffer), errMsg);
                        }
                    }
                }
            }

            cache.DeleteExpectedCache = false;
        }

        // PGC is special since PgcDemux splits the output file into 1GB chunks.
        private void CompareVobOutput(string cacheFolder, string expectedCommand, Func<IIfoFileReader, string, bool> actualCommand, string outputName)
        {
            FileManager cache = new FileManager(Path.Combine(TestCachePath, cacheFolder, "Expected"), Path.Combine(TestCachePath, cacheFolder, "Actual"));

            // Replace variables in command
            expectedCommand = FillVariables(expectedCommand, cache.ExpectedPath);
            //FillVariables(actualCommand, cache.ActualPath);

            // Run PgcDemux.exe
            Process? proc = null;
            if (cache.NewFolder)
            {
                if (ReadISO) throw new Exception("Can't run PgcDemux using the ISO.");
                proc = Process.Start(PgcDemuxPath, expectedCommand);
            }

            // Run PgcDemuxCS
            string demuxPath = Path.Combine(cache.ActualPath, outputName);
            if (ReadISO)
            {
                var reader = new DiscReader(Path.Combine(DvdBackupPath, $"{DvdName}.iso"));
                Assert.IsTrue(actualCommand(reader, demuxPath));
                reader.Dispose();
            }
            else
            {
                var reader = new SimpleIfoReader(Path.Combine(DvdBackupPath, DvdName));
                Assert.IsTrue(actualCommand(reader, demuxPath));
            }

            if (proc != null) proc.WaitForExit();


            // Verify files are the same
            string[] expectedFiles = Directory.GetFiles(cache.ExpectedPath).Order().ToArray();
            string[] actualFiles = Directory.GetFiles(cache.ActualPath);
            Assert.AreEqual(1, actualFiles.Length, "Expected only a single output file.");

            // Check file contents
            string errMsg = $"Actual PGC file did not match the expected file.";
            byte[] expectedBuffer = new byte[4096];
            byte[] actualBuffer = new byte[4096];
            using (Stream expectedStream = new MultiFileStream(expectedFiles))
            {
                using (Stream actualStream = File.OpenRead(actualFiles[0]))
                {
                    Assert.AreEqual(expectedStream.Length, actualStream.Length, "File sizes did not match.");
                    bool eof = false;
                    while (!eof)
                    {
                        int expectedCount = expectedStream.Read(expectedBuffer, 0, expectedBuffer.Length);
                        int actualCount = actualStream.Read(actualBuffer, 0, actualBuffer.Length);
                        Assert.IsTrue(expectedCount == actualCount, "Read different amount of bytes from the files.");
                        if (expectedCount == 0 || actualCount == 0) eof = true;
                        Assert.IsTrue(expectedBuffer.SequenceEqual(actualBuffer), errMsg);
                    }
                }
            }

            cache.DeleteExpectedCache = false;
        }
        /*private void CompareOutput(TestParams test, string cacheFolder, string outputFile, Func<IIfoFileReader, bool> actualCmd, bool combineExpected)
        {
            CompareOutput(cacheFolder, test.ExpectedArgs, actualCmd, combineExpected);
        }
        private void CompareOutput(TestParams[] tests, string cacheFolder)
        {
            foreach (var test in tests)
            {
                CompareOutput(cacheFolder, test.ExpectedArgs, test.ActualArgs);
            }
        }*/

        private static int GetTitle(string name)
        {
            if (name == "VIDEO_TS.IFO")
            {
                return 0;
            } else
            {
                return int.Parse(name[4..6]);
            }
        }

        [TestMethod]
        public void CustomVOB_PGC_Menu()
        {
            TestParams test = new();
            test.Mode = ModeType.PGC;
            test.Domain = DomainType.Menus;
            test.CustomVOB = true;
            test.Angle = 1;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                var pgcInfo = PgcDemux.GetIfoInfo(new DiscReader(Path.Combine(DvdBackupPath, $"{DvdName}.iso")), GetTitle(ifo.Name));
                Assert.IsTrue(pgcInfo.MenuPGCs.Order().SequenceEqual(ifo.MenuPGCs.Order()));
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.MenuPGCs)
                {
                    test.ID = id;
                    index++;
                    string folder = Path.Combine(DvdName, "CustomVOB_PGC_Menu", $"Test-{index:0000}");
                    Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string demuxPath) =>
                    {
                        return PgcDemux.ExtractPgc(reader, GetTitle(ifo.Name), true, id, 1, demuxPath);
                    };

                    CompareVobOutput(folder, test.ExpectedArgs, actual, $"Pgc_{id}.vob");
                }
            }
        }

        [TestMethod]
        public void CustomVOB_PGC_Title()
        {
            TestParams test = new();
            test.Mode = ModeType.PGC;
            test.Domain = DomainType.Titles;
            test.CustomVOB = true;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                var pgcInfo = PgcDemux.GetIfoInfo(new DiscReader(Path.Combine(DvdBackupPath, $"{DvdName}.iso")), GetTitle(ifo.Name));
                Assert.IsTrue(pgcInfo.TitlePGCs.Select(x => x.PGC).Order().SequenceEqual(ifo.TitlePGCs.Order()));
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.TitlePGCs)
                {
                    test.ID = id;
                    int angles = ifo.Angles.GetValueOrDefault(id.ToString(), 1);
                    Assert.AreEqual(angles, pgcInfo.TitlePGCs.First(x => x.PGC == id).NumberOfAngles);
                    for (int angle = 1; angle <= angles; angle++)
                    {
                        test.Angle = angle;
                        index++;
                        string folder = Path.Combine(DvdName, "CustomVOB_PGC_Title", $"Test-{index:0000}");
                        Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string demuxPath) =>
                        {
                            return PgcDemux.ExtractPgc(reader, GetTitle(ifo.Name), false, id, angle, demuxPath);
                        };

                        CompareVobOutput(folder, test.ExpectedArgs, actual, $"Pgc_{id}_angle_{angle}.vob");
                    }
                }
            }
        }
        
        [TestMethod]
        public void CustomVOB_VID_Menu()
        {
            TestParams test = new();
            test.Mode = ModeType.VID;
            test.Domain = DomainType.Menus;
            test.CustomVOB = true;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                var pgcInfo = PgcDemux.GetIfoInfo(new DiscReader(Path.Combine(DvdBackupPath, $"{DvdName}.iso")), GetTitle(ifo.Name));
                Assert.IsTrue(pgcInfo.MenuVIDs.Order().SequenceEqual(ifo.MenuVIDs.Order()));
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.MenuVIDs)
                {
                    test.ID = id;
                    index++;
                    string folder = Path.Combine(DvdName, "CustomVOB_VID_Menu", $"Test-{index:0000}");
                    Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string demuxPath) =>
                    {
                        return PgcDemux.ExtractVid(reader, GetTitle(ifo.Name), true, id, demuxPath);
                    };
                    CompareVobOutput(folder, test.ExpectedArgs, actual, $"Vid_{id}.vob");
                }
            }
        }
        
        [TestMethod]
        public void CustomVOB_VID_Title()
        {
            TestParams test = new();
            test.Mode = ModeType.VID;
            test.Domain = DomainType.Titles;
            test.CustomVOB = true;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                var pgcInfo = PgcDemux.GetIfoInfo(new DiscReader(Path.Combine(DvdBackupPath, $"{DvdName}.iso")), GetTitle(ifo.Name));
                Assert.IsTrue(pgcInfo.TitleVIDs.Order().SequenceEqual(ifo.TitleVIDs.Order()));
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.TitleVIDs)
                {
                    test.ID = id;
                    index++;
                    string folder = Path.Combine(DvdName, "CustomVOB_VID_Title", $"Test-{index:0000}");
                    Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string demuxPath) =>
                    {
                        return PgcDemux.ExtractVid(reader, GetTitle(ifo.Name), false, id, demuxPath);
                    };

                    CompareVobOutput(folder, test.ExpectedArgs, actual, $"Vid_{id}.vob");
                }
            }
        }
        
        [TestMethod]
        public void CustomVOB_Cell_Menu()
        {
            TestParams test = new();
            test.Mode = ModeType.CID;
            test.Domain = DomainType.Menus;
            test.CustomVOB = true;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var cell in ifo.MenuCIDs)
                {
                    test.ID = cell[0];
                    test.CID = cell[1];
                    index++;
                    string folder = Path.Combine(DvdName, "CustomVOB_Cell_Menu", $"Test-{index:0000}");
                    Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string demuxPath) =>
                    {
                        return PgcDemux.ExtractCid(reader, GetTitle(ifo.Name), true, cell[0], cell[1], demuxPath);
                    };

                    CompareVobOutput(folder, test.ExpectedArgs, actual, "cell.vob");
                }
            }
        }

        [TestMethod]
        public void CustomVOB_Cell_Title()
        {
            TestParams test = new();
            test.Mode = ModeType.CID;
            test.Domain = DomainType.Titles;
            test.CustomVOB = true;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var cell in ifo.TitleCIDs)
                {
                    test.ID = cell[0];
                    test.CID = cell[1];
                    index++;
                    string folder = Path.Combine(DvdName, "CustomVOB_Cell_Title", $"Test-{index:0000}");
                    Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string demuxPath) =>
                    {
                        return PgcDemux.ExtractCid(reader, GetTitle(ifo.Name), false, cell[0], cell[1], demuxPath);
                    };

                    CompareVobOutput(folder, test.ExpectedArgs, actual, "cell.vob");
                }
            }
        }
        
        [TestMethod]
        public void Extract_PGC_Menu()
        {
            TestParams test = new();
            test.Mode = ModeType.PGC;
            test.Domain = DomainType.Menus;
            test.ExtractVideo = true;
            test.ExtractAudio = true;
            test.ExtractSubs = true;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.MenuPGCs)
                {
                    test.ID = id;
                    index++;

                    string folder = Path.Combine(DvdName, "Extract_PGC_Menu", $"Test-{index:0000}");
                    Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string outputPath) =>
                    {
                        int title = GetTitle(ifo.Name);
                        var info = IfoBase.Open(reader, title);
                        if (!PgcDemux.ExtractPgcVideo(reader, title, true, id, 1, Path.Combine(outputPath, "VideoFile.m2v"))) return false;
                        if (info.MenuVobAudioAttributes != null)
                        {
                            if (!PgcDemux.ExtractPgcAudio(reader, title, true, id, 1, (int)info.MenuVobAudioAttributes.StreamID, Path.Combine(outputPath, $"AudioFile_{info.MenuVobAudioAttributes.StreamID:X2}.ac3"))) return false;
                        }

                        return true;
                    };

                    CompareOutput(folder, test.ExpectedArgs, actual);
                }
            }
        }
        
        [TestMethod]
        public void Extract_PGC_Title()
        {
            TestParams test = new();
            test.Mode = ModeType.PGC;
            test.Domain = DomainType.Titles;
            test.ExtractVideo = true;
            test.ExtractAudio = true;
            test.ExtractSubs = true;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.TitlePGCs)
                {
                    test.ID = id;
                    int angles = ifo.Angles.GetValueOrDefault(id.ToString(), 1);
                    for (int angle = 1; angle <= angles; angle++)
                    {
                        test.Angle = angle;
                        index++;

                        string folder = Path.Combine(DvdName, "Extract_PGC_Title", $"Test-{index:0000}");
                        Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string outputPath) =>
                        {
                            int title = GetTitle(ifo.Name);
                            if (title > 0)
                            {
                                var info = VtsIfo.Open(reader, title);
                                if (!PgcDemux.ExtractPgcVideo(reader, title, false, id, angle, Path.Combine(outputPath, "VideoFile.m2v"))) return false;
                                
                                foreach(var stream in info.TitlesVobAudioAttributes)
                                {
                                    if (!PgcDemux.ExtractPgcAudio(reader, title, false, id, angle, (int)stream.StreamID, Path.Combine(outputPath, $"AudioFile_{stream.StreamID:X2}.ac3"))) return false;
                                }
                            }

                            return true;
                        };

                        CompareOutput(folder, test.ExpectedArgs, actual);
                    }
                }
            }
        }
        
        [TestMethod]
        public void Extract_VID_Menu()
        {
            TestParams test = new();
            test.Mode = ModeType.VID;
            test.Domain = DomainType.Menus;
            test.ExtractVideo = true;
            test.ExtractAudio = true;
            test.ExtractSubs = true;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.MenuVIDs)
                {
                    test.ID = id;
                    index++;

                    string folder = Path.Combine(DvdName, "Extract_VID_Menu", $"Test-{index:0000}");
                    Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string outputPath) =>
                    {
                        int title = GetTitle(ifo.Name);
                        var info = IfoBase.Open(reader, title);
                        if (!PgcDemux.ExtractVidVideo(reader, title, true, id, Path.Combine(outputPath, "VideoFile.m2v"))) return false;
                        if (info.MenuVobAudioAttributes != null)
                        {
                            if (!PgcDemux.ExtractVidAudio(reader, title, true, id, (int)info.MenuVobAudioAttributes.StreamID, Path.Combine(outputPath, $"AudioFile_{info.MenuVobAudioAttributes.StreamID:X2}.ac3"))) return false;
                        }

                        return true;
                    };

                    CompareOutput(folder, test.ExpectedArgs, actual);
                }
            }
        }
        
        [TestMethod]
        public void Extract_VID_Title()
        {
            TestParams test = new();
            test.Mode = ModeType.VID;
            test.Domain = DomainType.Titles;
            test.ExtractVideo = true;
            test.ExtractAudio = true;
            test.ExtractSubs = true;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.TitleVIDs)
                {
                    test.ID = id;
                    index++;

                    string folder = Path.Combine(DvdName, "Extract_VID_Title", $"Test-{index:0000}");
                    Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string outputPath) =>
                    {
                        int title = GetTitle(ifo.Name);
                        if (title > 0)
                        {
                            var info = VtsIfo.Open(reader, title);
                            if (!PgcDemux.ExtractVidVideo(reader, title, false, id, Path.Combine(outputPath, "VideoFile.m2v"))) return false;

                            foreach (var stream in info.TitlesVobAudioAttributes)
                            {
                                if (!PgcDemux.ExtractVidAudio(reader, title, false, id, (int)stream.StreamID, Path.Combine(outputPath, $"AudioFile_{stream.StreamID:X2}.ac3"))) return false;
                            }
                        }

                        return true;
                    };

                    CompareOutput(folder, test.ExpectedArgs, actual);
                }
            }
        }
        
        [TestMethod]
        public void Extract_Cell_Menu()
        {
            TestParams test = new();
            test.Mode = ModeType.CID;
            test.Domain = DomainType.Menus;
            test.ExtractVideo = true;
            test.ExtractAudio = true;
            test.ExtractSubs = true;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var cell in ifo.MenuCIDs)
                {
                    test.ID = cell[0];
                    test.CID = cell[1];
                    index++;

                    string folder = Path.Combine(DvdName, "Extract_Cell_Menu", $"Test-{index:0000}");
                    Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string outputPath) =>
                    {
                        int title = GetTitle(ifo.Name);
                        var info = IfoBase.Open(reader, title);
                        if (!PgcDemux.ExtractCidVideo(reader, title, true, cell[0], cell[1], Path.Combine(outputPath, "VideoFile.m2v"))) return false;
                        if (info.MenuVobAudioAttributes != null)
                        {
                            if (!PgcDemux.ExtractCidAudio(reader, title, true, cell[0], cell[1], (int)info.MenuVobAudioAttributes.StreamID, Path.Combine(outputPath, $"AudioFile_{info.MenuVobAudioAttributes.StreamID:X2}.ac3"))) return false;
                        }

                        return true;
                    };

                    CompareOutput(folder, test.ExpectedArgs, actual);
                }
            }
        }

        [TestMethod]
        public void Extract_Cell_Title()
        {
            TestParams test = new();
            test.Mode = ModeType.CID;
            test.Domain = DomainType.Titles;
            test.ExtractVideo = true;
            test.ExtractAudio = true;
            test.ExtractSubs = true;
            int index = -1;

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var cell in ifo.TitleCIDs)
                {
                    test.ID = cell[0];
                    test.CID = cell[1];
                    index++;

                    string folder = Path.Combine(DvdName, "Extract_Cell_Title", $"Test-{index:0000}");
                    Func<IIfoFileReader, string, bool> actual = (IIfoFileReader reader, string outputPath) =>
                    {
                        int title = GetTitle(ifo.Name);
                        if (title > 0)
                        {
                            var info = VtsIfo.Open(reader, title);
                            if (!PgcDemux.ExtractCidVideo(reader, title, false, cell[0], cell[1], Path.Combine(outputPath, "VideoFile.m2v"))) return false;

                            foreach (var stream in info.TitlesVobAudioAttributes)
                            {
                                if (!PgcDemux.ExtractCidAudio(reader, title, false, cell[0], cell[1], (int)stream.StreamID, Path.Combine(outputPath, $"AudioFile_{stream.StreamID:X2}.ac3"))) return false;
                            }
                        }

                        return true;
                    };

                    CompareOutput(folder, test.ExpectedArgs, actual);
                }
            }
        }
    }

    [TestClass]
    public class WillyWonkaTest : BaseTest
    {
        public WillyWonkaTest() : base("WILLY_WONKA", "WillyWonka.json")
        { }
    }

    [TestClass]
    public class AnimusicTest : BaseTest
    {
        public AnimusicTest() : base("ANIMUSIC", "Animusic.json")
        { }
    }
}