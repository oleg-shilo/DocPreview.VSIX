using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DocPreview.Test
{
    public class ParserTest
    {
        static string code = File.ReadAllText("GenericClass.cs");

        [Fact]
        public void ParseEvent()
        {
            int line = 17;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Event myEvent", result.MemberTitle);
            Assert.Equal("public event Action myEvent", result.MemberDefinition);
            Assert.Equal("<summary> Occurs when [my event]. </summary>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseField()
        {
            int line = 23;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Field myField", result.MemberTitle);
            Assert.Equal("public int myField", result.MemberDefinition);
            Assert.Equal("<summary> My field </summary>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseFullProp()
        {
            int line = 29;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Property MyProperty2", result.MemberTitle);
            Assert.Equal("public int MyProperty2", result.MemberDefinition);
            Assert.Equal("<summary> Gets or sets my property2. </summary> <value>My property2.</value>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseAutoProp()
        {
            int line = 82;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Property MyAutoProperty", result.MemberTitle);
            Assert.Equal("public int MyAutoProperty", result.MemberDefinition);
            Assert.Equal("<summary> Gets or sets my auto property. </summary> <value>My auto property.</value>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseAutoPropWithInit()
        {
            int line = 105;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Property Name", result.MemberTitle);
            Assert.Equal("public string Name", result.MemberDefinition);
            Assert.Equal("<summary> Gets or sets my auto property. </summary> <value>My auto property.</value>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseEnum()
        {
            int line = 42;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Enum TestEnum", result.MemberTitle);
            Assert.Equal("enum TestEnum", result.MemberDefinition);
            Assert.Equal("<summary> Enum doc </summary>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseDelegate()
        {
            int line = 67;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Delegate GetTest", result.MemberTitle);
            Assert.Equal("delegate int GetTest()", result.MemberDefinition);
            Assert.Equal("<summary> Delegate doc </summary>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseStruct()
        {
            int line = 59;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Struct MyStruct", result.MemberTitle);
            Assert.Equal("struct MyStruct", result.MemberDefinition);
            Assert.Equal("<summary> Struct doc </summary>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseClass()
        {
            int line = 11;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Class GenericClass", result.MemberTitle);
            Assert.Equal("class GenericClass", result.MemberDefinition);
            Assert.Equal("<summary> Class documentation </summary>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseGenericClass()
        {
            int line = 113;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Class AnotherClass", result.MemberTitle);
            Assert.Equal("class AnotherClass<T>  where T: new()", result.MemberDefinition);
            var ttt = result.XmlDocumentation.Deflate();
            Assert.Equal("<summary> AnotherClass doc </summary> <typeparam name=\"T\">The type of the T.</typeparam>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseConstructor()
        {
            int line = 52;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Constructor GenericClass", result.MemberTitle);
            Assert.Equal("public GenericClass()", result.MemberDefinition);
            Assert.Equal("<summary> Initializes a new instance of the <see cref=\"GenericClass\"/> class. </summary>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseMethod()
        {
            int line = 72;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Method GetLogFile", result.MemberTitle);
            Assert.Equal("List<T> GetLogFile<T,V>(List<Dictionary<T,V>> options)  where T: class", result.MemberDefinition);
            Assert.Equal("<summary> Gets the log file. </summary> <param name=\"options\">The options.</param>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseInterfaceSummary()
        {
            int line = 120;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Interface ITest", result.MemberTitle);
            Assert.Equal("interface ITest", result.MemberDefinition);
            Assert.Equal("<summary> ITest doc </summary>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseInterfaceProperty()
        {
            int line = 126;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Property MyProperty", result.MemberTitle);
            Assert.Equal("int MyProperty", result.MemberDefinition);
            Assert.Equal("<summary> Gets or sets my property. </summary> <value>My property.</value>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseInterfaceEvent()
        {
            int line = 131;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Event MyEvent", result.MemberTitle);
            Assert.Equal("event Action MyEvent", result.MemberDefinition);
            Assert.Equal("<summary> Occurs when [my event]. </summary>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseInterfaceMethod()
        {
            int line = 139;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Method GetCount", result.MemberTitle);
            Assert.Equal("int GetCount<T>(T data) where T : new()", result.MemberDefinition);
            Assert.Equal("<summary> Gets the count. </summary> <typeparam name=\"T\">The type of the T.</typeparam> <param name=\"data\">The data.</param> <returns></returns>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseGenericMethod()
        {
            int line = 169;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Method GetCount", result.MemberTitle);
            Assert.Equal("int GetCount<T>(T data) where T : new()", result.MemberDefinition);
            Assert.Equal("<summary> Gets the count. </summary> <typeparam name=\"T\">The type of the T.</typeparam> <param name=\"data\">The data.</param> <returns></returns>", result.XmlDocumentation.Deflate());
        }

        //[Fact]
        //public void ParseSeeRefEscping()
        //{
        //    int line = 144;

        //    var result = Parser.FindMemberDocumentation(code, line);

        //    Assert.Equal("Method GetCount", result.MemberTitle);
        //    Assert.Equal("int GetCount<T>(T data) where T : new()", result.MemberDefinition);
        //    Assert.Equal("<summary> Gets the count. </summary> <typeparam name=\"T\">The type of the T.</typeparam> <param name=\"data\">The data.</param> <returns></returns>", result.XmlDocumentation.Deflate());
        //}

        [Fact]
        public void ParseWholeFile()
        {
            var result = Parser.FindAllDocumentation(code);

            Assert.Equal(18, result.Count());
        }
    }
}