using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static DocPreview.PreviewWindowControl;

namespace DocPreview.Test
{
    public class ParserTest : TestBase
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
        public void InheritedDoc_Single()
        {
            //  /// <inheritdoc />
            //  int foo { }
            var result = Parser.FindMemberDocumentation(base.Ide.GenericClass_code, 271);
            Assert.True(result.Success);
            Assert.NotEmpty(result.XmlDocumentation);
        }

        [Fact]
        public void InheritedDoc_CustomSource()
        {
            //   /// <inheritdoc cref="DocPreview.Test.ParentClass.Test.foo2"/>
            //   /// <remarks>This is a dummy class and always returns null.</remarks>
            //   int foo3 { }
            var result = Parser.FindMemberDocumentation(base.Ide.GenericClass_code, 278);
            Assert.True(result.Success);
            Assert.NotEmpty(result.XmlDocumentation);
            Assert.Contains("FOO2", result.XmlDocumentation);
        }

        [Fact]
        public void InheritedDoc_ExternalSource()
        {
            //    /// <inheritdoc cref="DocPreview.Test.TestBase.TestBase"/>
            //    struct StructClass : StructBase, ITest

            var result = Parser.FindMemberDocumentation(base.Ide.GenericClass_code, 312);
            Assert.True(result.Success);
            Assert.NotEmpty(result.XmlDocumentation);
            Assert.Contains("instance of the <see cref=\"TestBase\"", result.XmlDocumentation);
        }

        [Fact]
        public void InheritedDoc_Complex()
        {
            //   /// <inheritdoc />
            //   /// <remarks>This is a dummy class and always returns null.</remarks>
            //   int foo2 { }
            var result = Parser.FindMemberDocumentation(base.Ide.GenericClass_code, 274);
            Assert.True(result.Success);
            Assert.NotEmpty(result.XmlDocumentation);
            Assert.Contains("dummy class and always", result.XmlDocumentation);
        }

        [Fact]
        public void InheritedDoc_ComplexDeepInheritance()
        {
            //  /// <inheritdoc />
            //  int foo3(int arg1, string arg2, string arg3) { }

            var result = Parser.FindMemberDocumentation(base.Ide.GenericClass_code, 339);
            Assert.True(result.Success);
            Assert.NotEmpty(result.XmlDocumentation);
            Assert.Contains("Fooes the specified arg1-3", result.XmlDocumentation);
        }

        [Fact]
        public void InheritedDoc_OverloadedSource()
        {
            //    /// <inheritdoc cref="foo" />
            //    int foo(int arg1, string arg2) { }

            var result = Parser.FindMemberDocumentation(base.Ide.GenericClass_code, 315);
            Assert.True(result.Success);
            Assert.NotEmpty(result.XmlDocumentation);
            // Assert.Contains("specified arg1, arg2", result.XmlDocumentation);
        }

        [Fact]
        public void FindMemberDocumentationForType()
        {
            var file = Path.GetFullPath(@"..\..\GenericClass.cs");

            var result = Parser.FindMemberDocumentationForType(
                "DocPreview.Test.ParentClass.Test.foo",
                "DocPreview.Test.ParentClass.Test".GetLines(),
                new[] { file });
        }

        [Fact]
        public void FindTypeMemberFromCaret()
        {
            var code = File.ReadAllText(@"..\..\GenericClass.cs");

            var result = code.FindMemberTypeFromPosition(268);
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
            int line = 80;

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
            int line = 65;

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
            int line = 110;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Class AnotherClass", result.MemberTitle);
            Assert.Equal("class AnotherClass<T> where T : new()", result.MemberDefinition);
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
            int line = 70;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Method GetLogFile", result.MemberTitle);
            Assert.Equal("List<T> GetLogFile<T, V>(List<Dictionary<T, V>> options) where T : class", result.MemberDefinition);
            Assert.Equal("<summary> Gets the log file. </summary> <param name=\"options\">The options.</param>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseInterfaceSummary()
        {
            int line = 118;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Interface ITest", result.MemberTitle);
            Assert.Equal("interface ITest", result.MemberDefinition);
            Assert.Equal("<summary> ITest doc </summary>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseInterfaceProperty()
        {
            int line = 124;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Property MyProperty", result.MemberTitle);
            Assert.Equal("int MyProperty", result.MemberDefinition);
            Assert.Equal("<summary> Gets or sets my property. </summary> <value>My property.</value>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseInterfaceEvent()
        {
            int line = 129;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Event MyEvent", result.MemberTitle);
            Assert.Equal("event Action MyEvent", result.MemberDefinition);
            Assert.Equal("<summary> Occurs when [my event]. </summary>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseInterfaceMethod()
        {
            int line = 137;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.Equal("Method GetCount", result.MemberTitle);
            Assert.Equal("int GetCount<T>(T data) where T : new()", result.MemberDefinition);
            Assert.Equal("<summary> Gets the count. </summary> <typeparam name=\"T\">The type of the T.</typeparam> <param name=\"data\">The data.</param> <returns></returns>", result.XmlDocumentation.Deflate());
        }

        [Fact]
        public void ParseGenericMethod()
        {
            int line = 136;

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

            Assert.Equal(22, result.Count());
        }
    }
}