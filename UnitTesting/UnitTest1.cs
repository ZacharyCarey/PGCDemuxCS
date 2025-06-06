using PgcDemuxCS;
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

        public string ActualArgs
        {
            get
            {
                List<string> args = new();
                args.Add("pgcdemux");
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
                args.Add(IfoFileName);
                args.Add("${DEST}");

                return string.Join(" ", args);
            }
        }
    }

    public class FileManager
    {
        public readonly string ActualPath;
        public readonly string ExpectedPath;

        public FileManager()
        {
            ActualPath = Directory.CreateTempSubdirectory().FullName;
            ExpectedPath = Directory.CreateTempSubdirectory().FullName;
        }

        ~FileManager()
        {
            try
            {
                Directory.Delete(ExpectedPath, true);
            }
            catch (Exception ex) { }

            try
            {
                Directory.Delete(ActualPath, true);
            }catch(Exception ex) { }
        }
    }

    public abstract class BaseTest
    {
        private const string PgcDemuxPath = "C:\\Users\\Zack\\Downloads\\PgcDemux_1205_exe\\PgcDemux.exe";
        private const string DvdBackupPath = "C:\\Users\\Zack\\Videos";

        private readonly string DvdName;
        private readonly lsdvd Info;

        protected BaseTest(string dvdName, string infoFileName)
        {
            this.DvdName = dvdName;
            this.Info = lsdvd.Load(infoFileName);
        }

        private string FillVariables(string input, string dest, string[] vars)
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

        private void CompareOutput(string expectedCommand, string actualCommand, params string[] vars)
        {
            FileManager Files = new FileManager();

            // Replace variables in command
            expectedCommand = FillVariables(expectedCommand, Files.ExpectedPath, vars);
            actualCommand = FillVariables(actualCommand, Files.ActualPath, vars);

            // Run PgcDemux.exe
            var proc = Process.Start(PgcDemuxPath, expectedCommand);

            // Run PgcDemuxCS
            PgcDemux.Run(actualCommand, $"{DvdBackupPath}\\{DvdName}");

            proc.WaitForExit();

            // Verify files are the same
            string[] expectedFiles = Directory.GetFiles(Files.ExpectedPath);
            string[] actualFiles = Directory.GetFiles(Files.ActualPath);
            Assert.AreEqual(expectedFiles.Length, actualFiles.Length, "Number of generated files did not match.");

            // Check file contents
            foreach (var expectedFilePath in expectedFiles)
            {
                string fileName = Path.GetFileName(expectedFilePath);
                var actualFilePath = Path.Combine(Files.ActualPath, fileName);
                Assert.IsTrue(File.Exists(actualFilePath), $"Could not find expected file '{fileName}'.");

                byte[] expectedBytes = File.ReadAllBytes(expectedFilePath);
                byte[] actualBytes = File.ReadAllBytes(actualFilePath);
                Assert.IsTrue(expectedBytes.SequenceEqual(actualBytes), $"Actual file {fileName} did not match the expected file.");
            }
        }
        private void CompareOutput(TestParams test)
        {
            CompareOutput(test.ExpectedArgs, test.ActualArgs);
        }
        private void CompareOutput(TestParams[] tests)
        {
            foreach (var test in tests)
            {
                CompareOutput(test.ExpectedArgs, test.ActualArgs);
            }
        }

        [TestMethod]
        public void CustomVOB_PGC_Menu()
        {
            TestParams test = new();
            test.Mode = ModeType.PGC;
            test.Domain = DomainType.Menus;
            test.CustomVOB = true;
            
            foreach(var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach(var id in ifo.MenuPGCs)
                {
                    test.ID = id;
                    CompareOutput(test);
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

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.TitlePGCs)
                {
                    test.ID = id;
                    int angles = ifo.Angles.GetValueOrDefault(id.ToString(), 1);
                    for(int angle = 1; angle <= angles; angle++)
                    {
                        test.Angle = angle;
                        CompareOutput(test);
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

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.MenuVIDs)
                {
                    test.ID = id;
                    CompareOutput(test);
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

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.TitleVIDs)
                {
                    test.ID = id;
                    CompareOutput(test);
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

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var cell in ifo.MenuCIDs)
                {
                    test.ID = cell[0];
                    test.CID = cell[1];
                    CompareOutput(test);
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

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var cell in ifo.TitleCIDs)
                {
                    test.ID = cell[0];
                    test.CID = cell[1];
                    CompareOutput(test);
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

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.MenuPGCs)
                {
                    test.ID = id;
                    CompareOutput(test);
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
                        CompareOutput(test);
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

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.MenuVIDs)
                {
                    test.ID = id;
                    CompareOutput(test);
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

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var id in ifo.TitleVIDs)
                {
                    test.ID = id;
                    CompareOutput(test);
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

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var cell in ifo.MenuCIDs)
                {
                    test.ID = cell[0];
                    test.CID = cell[1];
                    CompareOutput(test);
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

            foreach (var ifo in Info.IfoFiles)
            {
                test.IfoFileName = ifo.Name;
                foreach (var cell in ifo.TitleCIDs)
                {
                    test.ID = cell[0];
                    test.CID = cell[1];
                    CompareOutput(test);
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