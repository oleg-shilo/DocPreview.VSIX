// using ICSharpCode.NRefactory.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;

// using Microsoft.CodeAnalysis;

// using Microsoft.CodeAnalysis.CSharp;

// using Microsoft.CodeAnalysis.CSharp.Syntax;

// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using ms_CodeAnalysis = Microsoft.CodeAnalysis;

using ms_Syntax = Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DocPreview
{
    public static partial class Parser
    {
        public class Result
        {
            public bool Success;
            public string MemberTitle;
            public string MemberDefinition; //signature
            public string XmlDocumentation;
            public string MemberName;
            public MemberDeclarationType MemberDeclarationType;
            public string MemberBaseType;
            public string[] InheritanceChain;
            public string[] MemberModuleUsings;
        }

        public static IEnumerable<Result> FindAllDocumentation(string code, string language = "CSharp")
        {
            var result = new List<Result>();

            string[] lines = code.GetLines().Where(x => x != null).ToArray();

            int docStart = -1;

            string xmlDocPreffix = GetXmlDocPrefix(language);

            for (int line = 0; line < lines.Length; line++)
            {
                bool isDoc = lines[line].TrimStart().StartsWith(xmlDocPreffix);

                if (isDoc)
                {
                    if (docStart == -1)
                        docStart = line;
                }
                else
                {
                    if (docStart != -1)
                    {
                        var info = FindMemberDocumentation(code, line, language);
                        if (info.Success)
                            result.Add(info);
                    }
                    docStart = -1;
                }
            }

            return result;
        }

        static string GetXmlDocPrefix(string langage)
        {
            return langage == "Basic" ? "'''" : "///";
        }

        public static Result FindMemberDocumentation(string code, int caretLine, string language = "CSharp")
        {
            Result result = FindMemberDocumentationSimple(code, caretLine, language);

            // process <inheritdoc ...> with Roslyn, which is only integrated with the extension for C#
            if (result.Success)
            {
                if (result.XmlDocumentation.Contains("<inheritdoc"))
                {
                    if (language == "CSharp")
                    {
                        // TODO
                        // <inheritdoc> spec: https://tunnelvisionlabs.github.io/SHFB/docs-master/SandcastleBuilder/html/79897974-ffc9-4b84-91a5-e50c66a0221d.htm
                        // + Multiple source files
                        // - Custom source name in the <inheritdoc...>
                        //   - limitations: no external assembly support;
                        //   + overloaded signatures in cref should support the resolution priority
                        // + Custom path in the <inheritdoc...>
                        // - Root level inheriting is not supported
                        // + Class Members vs Class

                        Result detailedResult = code.FindMemberTypeFromPosition(caretLine);

                        var xmlDoc = detailedResult.XmlDocumentation.ParseAsMultirootXml();
                        var newXmlDoc = "".ParseAsMultirootXml();

                        foreach (var item in xmlDoc.Elements())
                        {
                            if (item.Name.LocalName == "inheritdoc")
                            {
                                var crefName = item.Attribute("cref")?.Value;
                                var selectPath = item.Attribute("select")?.Value;

                                var allSourceFiles = Runtime.Ide.GetCodeBaseFiles();

                                var possiblePrefixes = new List<string>();
                                possiblePrefixes.AddRange(detailedResult.MemberName.GetParentNamespaces());
                                possiblePrefixes.AddRange(detailedResult.MemberModuleUsings);
                                possiblePrefixes.ForEach(x => Debug.WriteLine("   " + x));

                                string inheritedDocXml = null;

                                if (crefName?.Contains(".") == true) // explicit base type definition i.e. cref="Test.foo"
                                {
                                    inheritedDocXml = ($"{crefName}").FindMemberDocumentationForType(possiblePrefixes.ToArray(), allSourceFiles)?.XmlDocumentation;
                                }
                                else
                                {
                                    string srcName = crefName ?? detailedResult.MemberName.Split('.').Last();

                                    var queue = new Queue<string>();
                                    var processed = new List<string>();

                                    detailedResult.InheritanceChain.ToList().ForEach(queue.Enqueue);

                                    while (queue.Any())
                                    {
                                        var type = queue.Dequeue();

                                        if (processed.Contains(type))
                                            continue;

                                        var lookupName = (detailedResult.MemberDeclarationType == MemberDeclarationType.member) ?
                                                         $"{type}.{srcName}" :  // get XML doc from a base type member (e.g. Test.foo)
                                                         type;                  // get XML doc from a base type definition

                                        Debug.WriteLine("--------------");
                                        Debug.WriteLine("member: " + lookupName);
                                        Debug.WriteLine("prefixes:");

                                        var lookupResult = lookupName.FindMemberDocumentationForType(possiblePrefixes.ToArray(), allSourceFiles);

                                        inheritedDocXml = lookupResult?.XmlDocumentation;

                                        if (inheritedDocXml.HasText())
                                            break;

                                        lookupResult?.InheritanceChain?.ToList().ForEach(queue.Enqueue);
                                    }
                                }

                                if (!inheritedDocXml.HasText())
                                    inheritedDocXml = $"<summary>{new XText(result.XmlDocumentation)}</summary>";

                                var inheritedDoc = inheritedDocXml.ParseAsMultirootXml();

                                if (selectPath.HasText())
                                {
                                    var selectedXml = "";

                                    using (var sr = new StringReader($"<root>{inheritedDocXml}</root>"))
                                    {
                                        var nav = new XPathDocument(sr).CreateNavigator();
                                        nav.MoveToFirstChild();
                                        var path_result = nav.Evaluate(selectPath);
                                        foreach (XPathNavigator n in nav.Select(selectPath))
                                            selectedXml += n.OuterXml;
                                    }
                                    inheritedDoc = selectedXml.ParseAsMultirootXml();
                                }

                                foreach (var i_item in inheritedDoc.Elements())
                                    newXmlDoc.Add(i_item);
                            }
                            else
                                newXmlDoc.Add(item);
                        }

                        result.XmlDocumentation = newXmlDoc.ToString().Replace("<data>", "").Replace("</data>", "").Trim();
                    }
                    else
                    {
                        var xmlDoc = result.XmlDocumentation.ParseAsMultirootXml();
                        var newXmlDoc = "".ParseAsMultirootXml();

                        foreach (XElement element in xmlDoc.Elements())
                        {
                            if (element.Name.LocalName == "inheritdoc")
                                newXmlDoc.Add(new XText(element.ToString()));
                            else

                                newXmlDoc.Add(element);
                        }
                        result.XmlDocumentation = newXmlDoc.ToString().Replace("<data>", "<summary>").Replace("</data>", "</summary>").Trim();
                    }
                }
            }
            return result;
        }

        private static Result FindMemberDocumentationSimple(string code, int caretLine, string language)
        {
            string[] statementDelimiters = new string[] { ";", "{" };

            string xmlDocPreffix = GetXmlDocPrefix(language);

            var result = new Result();

            int fromLine = caretLine - 1;

            //poor man C# syntax parser. Too bad NRefactory conflicts with Xamarin
            string[] lines = code.GetLines().ToArray();
            if (lines.Any() && lines.Last() == null)
                lines = lines.Take(lines.Count() - 1).ToArray();

            if (lines.Length > fromLine && lines[fromLine].Trim().StartsWith(xmlDocPreffix)) // it is XML doc comment line
            {
                result.MemberTitle = "Member";
                result.MemberDefinition = "[some member]";

                var content = new StringBuilder();

                for (int i = fromLine; i >= 0; i--)
                {
                    var line = lines[i].Trim();
                    if (line.HasText())
                    {
                        if (line.StartsWith(xmlDocPreffix))
                            content.PrependLine(line.Substring(xmlDocPreffix.Length));
                        else
                            break;
                    }
                }

                int endOfDoc = -1;
                for (int i = fromLine + 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.HasText())
                    {
                        if (line.StartsWith(xmlDocPreffix))
                            content.AppendLine(line.Substring(xmlDocPreffix.Length));
                        else
                        {
                            endOfDoc = i;
                            break;
                        }
                    }
                }

                if (endOfDoc != -1)
                {
                    char endofDeclChar = (char)0;

                    var declaration = new StringBuilder();
                    for (int i = endOfDoc; i < lines.Length; i++)
                    {
                        var line = lines[i].TrimEnd();
                        var endofDecl = line.IndexOfAny(statementDelimiters);
                        if (endofDecl == -1)
                        {
                            var temp = line.Trim();

                            if (temp.StartsWith("/// <summary>")) // start of the next member                            }
                                break;

                            if (!temp.EndsWith("[") && !temp.EndsWith("]")) //not an attribute declaration
                            {
                                declaration.AppendLine(line);
                                if (language != "CSharp")
                                    break;
                            }
                        }
                        else
                        {
                            endofDeclChar = line[endofDecl];
                            declaration.AppendLine(line.Substring(0, endofDecl));
                            break;
                        }
                    }

                    if (declaration.Length > 0)
                    {
                        result.MemberDefinition = declaration.ToString().FormatApiSignature();

                        if (language == "CSharp")
                        {
                            result.MemberTitle = (result.MemberDefinition + " " + endofDeclChar).ToMemberTitle();
                        }
                        else
                        {
                            if (result.MemberDefinition.Contains("delegate "))
                                result.MemberTitle = "Delegate";
                            else if (result.MemberDefinition.Contains("class "))
                                result.MemberTitle = "Class";
                            else if (result.MemberDefinition.Contains("event "))
                                result.MemberTitle = "Event";
                            else if (result.MemberDefinition.Contains("struct "))
                                result.MemberTitle = "Struct";
                            else if (result.MemberDefinition.Contains("enum "))
                                result.MemberTitle = "Enum";
                            else if (endofDeclChar == '{')
                            {
                                if (result.MemberDefinition.Last() == ')')
                                    result.MemberTitle = "Method";
                                else
                                    result.MemberTitle = "Property";
                            }
                        }
                    }
                }

                result.XmlDocumentation = content.ToString().Trim();
                result.Success = true;
            }

            return result;
        }
    }

    public static class Extensions
    {
        public static string ToMemberTitle(this string definition)
        {
            try
            {
                definition = definition.ToLogicalSignature();

                if (definition.StartsWith("delegate "))
                {
                    //delegate int GetTest() where T: new();
                    return "Delegate " + definition.Words()[2].TrimInvokeParams();
                }
                else if (definition.StartsWith("class "))
                {
                    //class AnotherClass where T: new(){
                    return "Class " + definition.Words()[1].TrimStatement();
                }
                else if (definition.StartsWith("interface "))
                {
                    //interface ITest<T> where T: new(){
                    return "Interface " + definition.Words()[1].TrimStatement();
                }
                else if (definition.Contains("operator "))
                {
                    //operator DBBool(bool x){
                    return "Operator " + definition.Words()[1].TrimInvokeParams();
                }
                else if (definition.Contains("event "))
                {
                    //event Action myEvent;
                    return "Event " + definition.Words()[2].Split(';').FirstOrDefault();
                }
                else if (definition.StartsWith("struct "))
                {
                    //struct MyStruct{
                    return "Struct " + definition.Words()[1].TrimStatement();
                }
                else if (definition.StartsWith("enum "))
                {
                    //enum TestEnum{
                    return "Enum " + definition.Words()[1].TrimStatement();
                }
                else if (definition.Contains('('))
                {
                    var words = definition.Words();
                    if (words.First().Contains('('))
                        return "Constructor " + words[0].TrimInvokeParams();
                    else
                        return "Method " + words[1].TrimInvokeParams();
                }
                else if (definition.EndsWith("{"))
                {
                    return "Property " + definition.Words()[1].Trim('{');
                }
                else if (definition.EndsWith(";"))
                {
                    return "Field " + definition.Words()[1].Trim(';');
                }
            }
            catch { }

            return "Member";
        }

        public static string ToLogicalSignature(this string input)
        {
            var result = input.TrimTypeParams()
                              .Deflate()
                              .TrimAccessModifiers();
            return result;
        }

        static string TrimInvokeParams(this string input)
        {
            return input.Split('(').FirstOrDefault();
        }

        static string TrimStatement(this string input)
        {
            return input.Split(new char[] { '(', ';' }).FirstOrDefault();
        }

        static string TrimAccessModifiers(this string input)
        {
            return (" " + input).Replace(" public ", " ")
                                .Replace(" internal ", " ")
                                .Replace(" protected ", " ")
                                .Replace(" private ", " ")
                                .Replace(" static ", " ")
                                .Replace(" abstract ", " ")
                                .Replace(" override ", " ")
                                .Replace(" explicit ", " ")
                                .Replace(" implicit ", " ")
                                .Trim();
        }

        public static string TrimTypeParams(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var buffer = new StringBuilder();

            //example: public List<T> GetLogFile<T,V>
            int bracketsCount = 0;
            foreach (var item in input)
            {
                if (item == '<')
                    bracketsCount++;
                else if (item == '>')
                    bracketsCount--;
                else if (bracketsCount == 0)
                    buffer.Append(item);
            }
            return buffer.ToString();
        }

        static char[] wordDelimiters = "\r\t\n ".ToCharArray();

        public static string[] GetParentNamespaces(this string @namespace)
        {
            var result = new List<string>();
            var parts = @namespace.Split('.');

            foreach (var item in parts.Take(parts.Length - 1))
            {
                if (result.Any())
                    result.Add(result.Last() + "." + item);
                else
                    result.Add(item);
            }
            return result.ToArray();
        }

        public static string[] Words(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return new string[0];

            return input.Split(wordDelimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        public static StringBuilder PrependLine(this StringBuilder sb, string content)
        {
            return sb.Insert(0, content + Environment.NewLine);
        }

        public static string FormatApiSignature(this string text)
        {
            return text.Trim().Trim(',');
        }

        public static string Deflate(this string text)
        {
            //inefficient but adequate
            return text.Replace("\n", "")
                       .Replace("\r", "")
                       .Replace("\t", " ")
                       .Trim()
                       .Replace("    ", " ")
                       .Replace("   ", " ")
                       .Replace("  ", " ")
                       .Replace("  ", " ");
        }

        public static string GetFieldName(this string code, int startLine, int endLine)
        {
            var lines = code.GetLines().Skip(startLine).Take(endLine - startLine + 1).ToArray();
            var declaration = string.Join("", lines).Split(';').First().Trim();

            var pos = declaration.Replace("\t", " ").LastIndexOf(' ');

            if (pos == -1)
                return "[someField]";
            else
                return declaration.Substring(pos).Trim();
        }

        public static XElement ParseAsMultirootXml(this string xml)
            => XDocument.Parse($"<data>{xml}</data>").Root;

        public static bool HasText(this string text)
        {
            return !string.IsNullOrEmpty(text);
        }

        public static T[] ToSingleItemArray<T>(this T text) => new T[] { text };

        public static string JoinBy(this IEnumerable<string> lines, string separator)
            => string.Join(separator, lines);

        public static string[] GetLines(this string text)
            => text.Replace(Environment.NewLine, "\n").Split('\n');

        public static int GetLineFromPosition(this string text, int position)
            => text.Substring(0, position).GetLines().Count();
    }
}