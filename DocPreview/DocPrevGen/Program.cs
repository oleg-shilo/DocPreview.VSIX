using DocPrevGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

//Special thanks to Marek Stój for his excellent ImmDoc.NET (https://github.com/marek-stoj/ImmDoc.NET)
//DocPreview uses the following ImmDoc.NET components:
//   - XML processing algorithm
//   - Lists.xslt
//   - Default HTML page layout

namespace XmlDocumentation
{
    static class Program
    {
        // prevent the impact of accidental formatting of the template by CodeMaid
        public static string CleanTemplate(this string template)
            => template.Replace("        {$", "{$");

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            string signature = File.ReadAllText(@"..\..\signature.txt");
            string xml = File.ReadAllText(@"..\..\doc.xml");
            string title = "Class Method";

            string html = DocPreview.GenerateHtml(title, signature, xml);

            File.WriteAllText(@"..\..\docpreview.html", html);
        }
    }

    public static class DocPreview
    {
        static DocPreview()
        {
            GetHtmlResourcesDir();
        }

        public static bool IsFirstEverRun = false;
        public static bool IsVersionFirstRun = false;

        static public string GetHtmlResourcesDir()
        {
            //<user>\AppData\Roaming\DocPreview\1.0.0.0\css
            if (!Directory.Exists(htmlResourcesDir))
            {
                //<user>\AppData\Roaming\DocPreview
                if (!Directory.Exists(Path.GetDirectoryName(Path.GetDirectoryName(htmlResourcesDir))))
                {
                    IsFirstEverRun = true;
                }

                IsVersionFirstRun = true;

                Directory.CreateDirectory(htmlResourcesDir);
                SetContentTheme(false);
                File.WriteAllText(Path.Combine(htmlResourcesDir, "ContentsMerged.css"), Resource1.ContentsMerged);
                Resource1.BigSquareExpanded.Save(Path.Combine(htmlResourcesDir, "BigSquareExpanded.gif"));
                Resource1.SmallSquareExpanded.Save(Path.Combine(htmlResourcesDir, "SmalSquareExpanded.gif"));
            }

            if (!File.Exists(CustomCss))
            {
                Directory.CreateDirectory(htmlCustomResourcesDir);
                File.WriteAllText(CustomCss, Resource1.Contents);
            }
            return htmlResourcesDir;
        }

        public static void SetContentTheme(bool dark)
        {
            if (Directory.Exists(htmlResourcesDir))
            {
                if (dark)
                    File.WriteAllText(Path.Combine(htmlResourcesDir, "Contents.css"), Resource1.Contents_dark);
                else
                    File.WriteAllText(Path.Combine(htmlResourcesDir, "Contents.css"), Resource1.Contents);
            }
        }

        public static void SetContentCustomTheme(string cssFile)
        {
            if (File.Exists(cssFile))
            {
                var content = File.ReadAllText(cssFile);
                File.WriteAllText(Path.Combine(htmlResourcesDir, "Contents.css"), content);
            }
        }

        public static string htmlResourcesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                             "DocPreview",
                                                             Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                                                             "css");

        public static string htmlCustomResourcesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                                                             "DocPreview", "Custom", "css");

        public static string CustomCss = Path.Combine(XmlDocumentation.DocPreview.htmlCustomResourcesDir, "custom_theme.css");

        public static string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocPreview");

        public static string GenerateDefaultHtml()
            => GenerateErrorHtml(@"<span style='font-style: italic;'><br> <span style='color: red;'>
                                       Place cursor/caret at the C# XML documentation comment region and click
                                       'Refresh' icon.
                                   </span></span><br>");

        public static string GenerateErrorHtml(string errorText = "")
        {
            string content = Resource1.template_light.CleanTemplate();

            bool noUserContent = errorText == "";

            content = content.Replace("{$title}", "")
                             .Replace("{$syntax}", noUserContent ? syntaxGroup : "")
                             .Replace("{$signature}", syntaxGroupClosure)
                             .Replace("{$params}", "")
                             .Replace("{$value}", "")
                             .Replace("{$typeparams}", "")
                             .Replace("{$return}", "")
                             .Replace("{$exception}", "")
                             .Replace("{$permission}", "")
                             .Replace("{$remarks}", "")
                             .Replace("{$example}", "")
                             .Replace("{$summary}", errorText)
                             .Replace("{$css_folder}", htmlResourcesDir);

            return content;
        }

        public static string GenerateHtml(string title, string signature, string xml)
        {
            return GenerateHtml(title, signature, xml, Resource1.template_light.CleanTemplate());
        }

        public static string GenerateRawHtml(string title, string signature, string xml)
        {
            return GenerateHtml(title, signature, xml, Resource1.template_raw.CleanTemplate());
        }

        public static string HtmlDecorateMembers(params string[] content)
        {
            var html = @"<!DOCTYPE html PUBLIC ""-//W3C//DTD XHTML 1.0 Strict//EN"" ""http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd"">
<html xmlns=""http://www.w3.org/1999/xhtml"" xml:lang=""en"" lang=""en"">
<head>
    <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
    <link rel=""stylesheet"" type=""text/css"" href=""{$css_folder}/ContentsMerged.css"" />
    <title>Class GenericClass</title>
</head>
<body>"
                + "<p>" + string.Join(Environment.NewLine + "</p><hr></hr><p>", content) + "</p>" +
@"
</body>
</html>";
            return html.Replace("{$css_folder}", htmlResourcesDir);
        }

        /// <summary>
        /// Generates the HTML.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="signature">The signature.</param>
        /// <param name="xml">The XML.</param>
        /// <param name="template">The template.</param>
        /// <returns></returns>
        /// <exception cref="ApplicationException">Error parsing \"{title}: '{signature}'\". Ensure XML for this member " +
        ///                     $"compiles with the XML specification.</exception>
        public static string GenerateHtml(string title, string signature, string xml, string template)
        {
            try
            {
                string content = template;

                //replace '{' with '<' in "<see cref="StringEnum{T}"/>"
                xml = xml.Convert(@"<see cref="".*\{\{*.*\}""/>", m => m.Value.Replace("{", "&lt;").Replace("}", "&gt;"));

                var doc = XDocument.Parse($"<member>{xml}</member>").Root; //input element doesn't have a single root

                FixLists(doc);

                var summary = doc.Descendants("summary").First();
                var parameters = doc.Descendants("param");
                var typeparams = doc.Descendants("typeparam");
                var returns = doc.Descendants("returns").FirstOrDefault();
                var examples = doc.Descendants("example");
                var value = doc.Descendants("value").FirstOrDefault();
                var remarks = doc.Descendants("remarks");
                var exceptions = doc.Descendants("exception");
                var permissions = doc.Descendants("permission");

                content = content.Replace("{$title}", title)
                                 .Replace("{$syntax}", syntaxGroup)
                                 .Replace("{$signature}", HttpUtility.HtmlEncode(signature) + syntaxGroupClosure)
                                 .Replace("{$value}", value.ToHtml())
                                 .Replace("{$params}", parameters.ToHtml())
                                 .Replace("{$typeparams}", typeparams.ToHtml())
                                 .Replace("{$return}", returns.ToHtml())
                                 .Replace("{$exception}", exceptions.ToHtml())
                                 .Replace("{$permission}", permissions.ToHtml())
                                 .Replace("{$remarks}", remarks.ToHtml())
                                 .Replace("{$example}", examples.ToHtml())
                                 .Replace("{$summary}", summary.ToHtml())
                                 .Replace("{$css_folder}", htmlResourcesDir);

                return content;
            }
            catch
            {
                throw new ApplicationException($"Error parsing \"{title}: '{signature}'\". Ensure XML for this member " +
                    $"compiles with the XML specification.");
            }
        }

        static void FixLists(XElement element)
        {
            //ImmDoc doesn't like items without descriptions (while Sandcastle is OK) so add them
            element.Descendants("item")
                   .Where(x => x.Parent.Name == "list" && !x.HasElements)
                   .ToList()
                   .ForEach(item =>
                   {
                       string value = item.Value;
                       item.Add(new XElement("description", value));
                   });
        }

        static string ProcessSection(XElement[] nodes, string title)
        {
            string nodeName = nodes.First().Name.LocalName;
            var html = new StringBuilder();

            html.AppendLine("")
                .AppendLine("<div id='SectionHeader1' class='SectionHeader'>")
                .AppendLine("   <img id='SectionExpanderImg1' src='{$css_folder}/BigSquareExpanded.gif' alt='Collapse/Expand' /> ")
                .AppendLine("   <span class='SectionHeader'>")
                .AppendLine("       <span class='ArrowCursor'>")
                .AppendLine("       " + title)
                .AppendLine("       </span>")
                .AppendLine("   </span>")
                .AppendLine("</div>")
                .AppendLine("<div id='SectionContainerDiv1' class='SectionContainer'>");

            if (nodeName == "exception" || nodeName == "permission")
            {
                string column1 = "Exception";
                string column2 = "Condition";

                if (nodeName == "permission")
                {
                    column1 = "Permission";
                    column2 = "Description";
                }

                html.AppendLine("<table class='MembersTable'>")
                    .AppendLine("<col width='25%' />")
                    .AppendLine("<col width='75%' />")
                    .AppendLine("<tr>")
                    .AppendLine($"<th>{column1}</th>")
                    .AppendLine($"<th>{column2}</th></tr>");
                foreach (var item in nodes)
                {
                    string excType = item.Attribute("cref")?.Value.Replace("T:", "");

                    html.AppendLine("<tr>")
                        .AppendLine($"<td><span class='PseudoLink'>{excType}</span></td>")
                        .AppendLine($"<td>{item.ToHtml()}</td>")
                        .AppendLine("</tr>");
                }
                html.AppendLine("</table");
            }
            else
            {
                foreach (var item in nodes)
                    html.AppendLine(item.ToHtml());
            }

            html.AppendLine("</div>");
            html.AppendLine("</div>");

            return html.ToString();
        }

        static string ProcessParams(XElement[] nodes, string title)
        {
            var html = new StringBuilder();
            html.AppendLine($"<div class='CommentHeader'>{title}</div>");
            foreach (var item in nodes)
            {
                html.AppendFormat($"<div class='CommentParameterName'>{item.Attribute("name").Value}</div>")
                    .AppendLine("<div class='ParameterCommentContainer'>")
                    .AppendLine(item.ToHtml())
                    .AppendLine("</div>");
            }
            return html.ToString();
        }

        static string ProcessReturns(XElement nodes, string content)
        {
            var html = new StringBuilder();
            html.AppendLine("<div class='CommentHeader'>Return Value</div>")
                .AppendLine("<div class='ParameterCommentContainer'>")
                .AppendLine(content)
                .AppendLine("</div>");
            return html.ToString();
        }

        static string ProcessFieldValue(XElement nodes, string content)
        {
            var html = new StringBuilder();
            html.AppendLine("<div class='CommentHeader'>Field Value</div>")
                .AppendLine("<div class='ParameterCommentContainer'>")
                .AppendLine(content)
                .AppendLine("</div>");
            return html.ToString();
        }

        public static string ToHtml(this IEnumerable<XElement> nodes)
        {
            if (nodes.Any())
            {
                if (nodes.First().Name == "typeparam")
                    return ProcessParams(nodes.ToArray(), "Type Parameters");
                if (nodes.First().Name == "param")
                    return ProcessParams(nodes.ToArray(), "Parameters");
                if (nodes.First().Name == "remarks")
                    return ProcessSection(nodes.ToArray(), "Remarks");
                if (nodes.First().Name == "exception")
                    return ProcessSection(nodes.ToArray(), "Exceptions");
                if (nodes.First().Name == "permission")
                    return ProcessSection(nodes.ToArray(), ".NET Framework Security");
                if (nodes.First().Name == "example")
                    return ProcessSection(nodes.ToArray(), "Examples");
            }
            return "";
        }

        public static string ToHtml(this XElement node)
        {
            if (node == null)
                return null;

            var r = node.CreateReader();
            r.MoveToContent();
            var xml = r.ReadInnerXml();
            var content = ImmDocNET.ProcessComment(xml);

            if (node.Name == "returns")
                return ProcessReturns(node, content);
            else if (node.Name == "value")
                return ProcessFieldValue(node, content);

            return content;
        }

        static string Convert(this string text, string pattern, Func<Match, string> convert, RegexOptions options = RegexOptions.None)
        {
            return new Regex(pattern, options).Replace(text, new MatchEvaluator(convert));
        }

        /// <summary>
        /// Documentation XML parser/processor based on https://github.com/marek-stoj/ImmDoc.NET.
        /// </summary>
        class ImmDocNET
        {
            // Excellent XMD doc reference: http://web.archive.org/web/20080623060531/http://thoughtpad.net/alan-dean/cs-xml-documentation.html
            static Regex codePattern = new Regex("<code>(?<Contents>(.|\r|\n)*?)</code>", RegexOptions.Multiline | RegexOptions.Compiled);

            // static Regex seePattern = new Regex("((<see cref=\"(?<XmlMemberId>.*?)\"[ ]?/>)|" +
            //                                     "(<see cref=\"(?<XmlMemberId>.*?)|" +
            //                                     "(<seealso cref=\"(?<XmlMemberId>.*?)\"[ ]?/>)|" +
            //                                     "(<seealso cref=\"(?<XmlMemberId>.*?))" +
            //                                     "\">(?<Contents>.*?)" +
            //                                     "((</see>)|(</seealso>))", RegexOptions.Multiline | RegexOptions.Compiled);

            static Regex seePattern = new Regex("(<see cref=\"(?<XmlMemberId>.*?)\"[ ]?/>)|(<see cref=\"(?<XmlMemberId>.*?)\">(?<Contents>.*?)</see>)", RegexOptions.Multiline | RegexOptions.Compiled);
            static Regex seePattern2 = new Regex("(<see langword=\"(?<XmlMemberId>.*?)\"[ ]?/>)|(<see langword=\"(?<XmlMemberId>.*?)\">(?<Contents>.*?)</see>)", RegexOptions.Multiline | RegexOptions.Compiled);
            static Regex seealsoPattern = new Regex("(<seealso cref=\"(?<XmlMemberId>.*?)\"[ ]?/>)|(<seealso cref=\"(?<XmlMemberId>.*?)\">(?<Contents>.*?)</seealso>)", RegexOptions.Multiline | RegexOptions.Compiled);

            static Regex paramrefPattern = new Regex("<paramref name=\"(?<ParamName>.*?)\" ?/>", RegexOptions.Multiline | RegexOptions.Compiled);
            static Regex typeparamrefPattern = new Regex("<typeparamref name=\"(?<TypeParamName>.*?)\" ?/>", RegexOptions.Multiline | RegexOptions.Compiled);

            static MatchEvaluator codeRegexEvaluator = new MatchEvaluator(OnCodePatternMatch);
            static MatchEvaluator paramrefRegexEvaluator = new MatchEvaluator(OnParamrefPatternMatch);
            static MatchEvaluator typeparamrefRegexEvaluator = new MatchEvaluator(OnTypeparamrefPatternMatch);
            static MatchEvaluator seeRegexEvaluator = new MatchEvaluator(OnSeePatternMatch);
            static MatchEvaluator seeRegexEvaluator2 = new MatchEvaluator(OnSeePatternMatch2);
            static MatchEvaluator seealsoRegexEvaluator = new MatchEvaluator(OnSeePatternMatch);

            private static string OnCodePatternMatch(Match match)
            {
                string contents = match.Groups["Contents"].Value.Trim('\r', '\n');
                int index = 0;

                while (index < contents.Length && contents[index] == ' ')
                    index++;

                if (index > 0)
                {
                    Regex pattern = new Regex("^" + CreateNSpaces(index), RegexOptions.Multiline);
                    contents = pattern.Replace(contents, "");
                }

                return CreateCodeBlockTable("C#", "<pre>\r\n" + contents + "</pre>", true);
            }

            static string CreateNSpaces(int n)
            {
                var sb = new StringBuilder(n);

                for (int i = 0; i < n; i++)
                {
                    sb.Append(' ');
                }

                return sb.ToString();
            }

            static string CreateCodeBlockTable(string language, string contents, bool inExampleSetion)
            {
                return String.Format("<table class=\"{0}\"><col width=\"100%\" /><tr class=\"CodeTable\"><th class=\"CodeTable\">{1}</th></tr><tr class=\"CodeTable\"><td class=\"CodeTable\">{2}</td></tr></table>",
                                     inExampleSetion ? "ExampleCodeTable" : "CodeTable",
                                     language,
                                     contents);
            }

            static string CreateCodeBlockTable(string language, string contents)
            {
                return CreateCodeBlockTable(language, contents, false);
            }

            static string OnParamrefPatternMatch(Match match)
            {
                string paramName = match.Groups["ParamName"].Value;

                return String.Format("<span class=\"Code\">{0}</span>", paramName);
            }

            static string OnTypeparamrefPatternMatch(Match match)
            {
                string typeParamName = match.Groups["TypeParamName"].Value;

                return String.Format("<span class=\"Code\">{0}</span>", typeParamName);
            }

            static string OnSeePatternMatch(Match match)
            {
                //<see cref="T:csscript.CSharpParser.DirectiveDelimiters"/>
                //<see cref="DirectiveDelimiters"/>
                string contents = match.Groups["Contents"].Value;
                if (string.IsNullOrEmpty(contents))
                    contents = match.Groups["XmlMemberId"].Value;

                string name = contents.Split('=')
                                      .Last()
                                      .Replace("\"", "")
                                      .Replace("/>", "")
                                      .Split('.')
                                      .Last();

                return "<a href=\"\" >" + name + "</a>";
            }

            static string OnSeePatternMatch2(Match match)
            {
                //<see cref="T:csscript.CSharpParser.DirectiveDelimiters"/>
                //<see cref="DirectiveDelimiters"/>
                string contents = match.Groups["Contents"].Value;
                if (string.IsNullOrEmpty(contents))
                    contents = match.Groups["XmlMemberId"].Value;

                string name = contents.Split('=')
                                      .Last()
                                      .Replace("\"", "")
                                      .Replace("/>", "")
                                      .Split('.')
                                      .Last();

                return "<strong>" + name + "</strong>";
            }

            static XslCompiledTransform listsXslt;
            static XmlReaderSettings xslReaderSettings;
            static XmlReaderSettings listsXmlReaderSettings;
            static XmlWriterSettings xmlWriterSettings;

            static string ProcessListsInComment(string contents)
            {
                try
                {
                    if (listsXslt == null)
                    {
                        // lazy loading of XSLT
                        listsXslt = new XslCompiledTransform();

                        xslReaderSettings = new XmlReaderSettings()
                        {
                            IgnoreWhitespace = true,
                            DtdProcessing = DtdProcessing.Prohibit
                        };
                        listsXmlReaderSettings = new XmlReaderSettings();
                        listsXmlReaderSettings.ConformanceLevel = ConformanceLevel.Fragment;
                        listsXmlReaderSettings.IgnoreWhitespace = true;

                        xmlWriterSettings = new XmlWriterSettings();
                        xmlWriterSettings.ConformanceLevel = ConformanceLevel.Fragment;
                        xmlWriterSettings.Encoding = Encoding.UTF8;
                        xmlWriterSettings.Indent = true;
                        xmlWriterSettings.IndentChars = "    ";

                        using (TextReader xslTextReader = new StringReader(Resource1.Lists)) //Lists.xslt
                        using (XmlReader xslXmlReader = XmlReader.Create(xslTextReader, xslReaderSettings))
                            listsXslt.Load(xslXmlReader);
                    }

                    StringBuilder htmlOutput = new StringBuilder();
                    using (TextReader xmlTextReader = new StringReader(contents))
                    {
                        using (XmlReader xmlReader = XmlReader.Create(xmlTextReader, listsXmlReaderSettings))
                        {
                            using (XmlWriter xmlWriter = XmlWriter.Create(htmlOutput, xmlWriterSettings))
                            {
                                listsXslt.Transform(xmlReader, xmlWriter);
                            }
                        }
                    }

                    return htmlOutput.ToString();
                }
                catch (Exception)
                {
                    return contents;
                }
            }

            public static string ProcessComment(string contents)
            {
                if (contents.Contains("<list")) // process lists only if there's at least one
                {
                    contents = ProcessListsInComment(contents);
                }

                contents = contents.Replace("<para>", "<p>").Replace("</para>", "</p>").Replace("<c>", "<span class=\"Code\">").Replace("</c>", "</span>");

                contents = codePattern.Replace(contents, codeRegexEvaluator);
                contents = seePattern.Replace(contents, seeRegexEvaluator);
                contents = seePattern2.Replace(contents, seeRegexEvaluator2);
                contents = seealsoPattern.Replace(contents, seealsoRegexEvaluator);
                contents = paramrefPattern.Replace(contents, paramrefRegexEvaluator);
                contents = typeparamrefPattern.Replace(contents, typeparamrefRegexEvaluator);

                return contents;
            }
        }

        static string syntaxGroupClosure = @"</pre></td></tr></tbody></table>";

        static string syntaxGroup =
@"    <div id='SectionHeader0' class='SectionHeader'>
        <img id='SectionExpanderImg0' src='{$css_folder}/BigSquareExpanded.gif' alt='Collapse/Expand' >
        <span class='SectionHeader'>
            <span class='ArrowCursor' >
            Syntax
            </span>
        </span>
    </div>

    <div id='SectionContainerDiv0' class='SectionContainer'>
    <table class='CodeTable'>
    <colgroup><col width='100%'></colgroup>
    <tbody><tr class='CodeTable'><th class='CodeTable'>C#</th></tr><tr class='CodeTable'><td class='CodeTable'><pre style='margin-left: 2px;'>";
    }
}