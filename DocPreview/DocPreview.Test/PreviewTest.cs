using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using xdoc = XmlDocumentation;

namespace DocPreview.Test
{
    public class PreviewTest
    {
        static string code = File.ReadAllText("GenericClass.cs");

        [Fact]
        public void PreviewEnum()
        {
            int line = 200 - 1;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.True(result.Success);

            var html = XmlDocumentation.DocPreview
                                       .GenerateHtml(result.MemberTitle,
                                                     result.MemberDefinition,
                                                     result.XmlDocumentation);

            Assert.NotEmpty(html);
        }

        [Fact]
        public void PreviewMethod()
        {
            int line = 19 - 1;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.True(result.Success);

            var html = XmlDocumentation.DocPreview
                                       .GenerateHtml(result.MemberTitle,
                                                     result.MemberDefinition,
                                                     result.XmlDocumentation);

            Assert.NotEmpty(html);
        }

        [Fact]
        public void Issue_19()
        {
            int line = 224 - 1;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.True(result.Success);

            var html = XmlDocumentation.DocPreview
                                       .GenerateHtml(result.MemberTitle,
                                                     result.MemberDefinition,
                                                     result.XmlDocumentation);

            Assert.NotEmpty(html);
        }

        [Fact]
        public void ParseExceptionInfo()
        {
            int line = 184;

            var result = Parser.FindMemberDocumentation(code, line);

            Assert.True(result.Success);

            var html = XmlDocumentation.DocPreview
                                       .GenerateHtml(result.MemberTitle,
                                                     result.MemberDefinition,
                                                     result.XmlDocumentation);

            Assert.NotEmpty(html);
        }

        [Fact]
        public void PreviewFile()
        {
            var result = Parser.FindAllDocumentation(code).ToArray();

            Assert.True(result.Any());

            var content = result.Select(r => xdoc.DocPreview
                                                 .GenerateRawHtml(r.MemberTitle,
                                                                  r.MemberDefinition,
                                                                  r.XmlDocumentation)).ToArray();

            var html = xdoc.DocPreview.HtmlDecorateMembers(content);

            Assert.NotEmpty(html);
        }
    }
}