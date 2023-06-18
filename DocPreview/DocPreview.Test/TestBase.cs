using System;
using System.IO;
using static DocPreview.PreviewWindowControl;

namespace DocPreview.Test
{
    public class TestBase
    {
        object ideLock = new object();
        protected TestIdeServices Ide => (TestIdeServices)Runtime.Ide;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestBase"/> class.
        /// </summary>
        public TestBase()
        {
            Runtime.InitAssemblyProbing(true);

            lock (ideLock)
            {
                if (Runtime.Ide == null)
                    Runtime.Ide = new TestIdeServices();
            }
        }
    }

    public class TestIdeServices : IIdeServices
    {
        public string file;
        public int caretLine = 0;

        public TestIdeServices()
        {
            file = Path.GetFullPath(@"..\..\GenericClass.cs");
        }

        public bool IsCurrentViewValid => throw new NotImplementedException();

        public string[] GetCodeBaseFiles()
        {
            return new[]
            {
                   Path.GetFullPath(@"..\..\GenericClass.cs"),
                   Path.GetFullPath(@"..\..\TestBase.cs"),
                };
        }

        public string GetCurrentViewLanguage() => "CSharp";

        public int? GetCurrentCaretLine() => caretLine;

        public string GetCurrentFileName() => file;

        public string GetCurrentViewText() => File.ReadAllText(file);

        public string GenericClass_code => GetCurrentViewText();
    }
}