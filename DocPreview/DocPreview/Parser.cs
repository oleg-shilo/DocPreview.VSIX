using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using ICSharpCode.NRefactory.CSharp;

namespace DocPreview
{
    public static class Parser
    {
        public class Result
        {
            public bool Success;
            public string MemberTitle;
            public string MemberDefinition; //signature
            public string XmlDocumentation;
        }

        static bool IsDefaultUsingsStyle(SyntaxTree syntaxTree)
        {
            var firstNamespace = syntaxTree.Children.DeepAll(x => x is NamespaceDeclaration)
                                                    .Cast<NamespaceDeclaration>()
                                                    .FirstOrDefault();

            if (firstNamespace != null)
            {
                return !syntaxTree.Children.DeepAll(x => x is UsingDeclaration)
                                           .Where(x => x.StartLocation.Line > firstNamespace.StartLocation.Line)
                                           .Any();
            }
            return true;
        }

        public static IEnumerable<Result> FindAllDocumentation(string code, string language = "CSharp")
        {
            var result = new List<Result>();

            string[] lines = code.Lines().Where(x => x != null).ToArray();

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
                        var info = FindMemberDocumentation(code, line, "C/C++");
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
            /*
            C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\Extensions\Xamarin\Xamarin\4.0.0.1689

            Quite shockingly Xamarin VS plugin is using ICSharpCode.NRefactory.dll - AssemblyFileVersion("5.5.1")
            While this plugin relies on NuGet assembly ICSharpCode.NRefactory.dll - AssemblyFileVersion("5.2.0").
            And the both assemblies are built as "Assembly ICSharpCode.NRefactory, Version 5.0.0.0"
            SHOCKING!!!!
            The assemblies are clearly different (file size and functionality wise) despite being both marked as the same version.

            CLR cannot distinguish them and assembly probing gets so screwed that UnitTesting loads
            one assembly file and VS (runtime) another one.

            Xamarin should never ever distribute a conflicting assembly.

            The outcome is - using NRefactory v5.0.0.0 is too risky as it's not clear when and where the asm probing
            will fail again. And also there is no warranty that Xamarin wouldn't release yet another edition of
            ICSharpCode.NRefactory v5.0.0.0.

            Forcing (somehow) DocPreview to be loaded before Xamarin will fix the problem but it will
            then screw Xamarin plugin.

            I had no choice but to implement my own "poor-man" parser.

            */
            //FindMemberDocumentationNRefactoryNew(code, caretLine);

            string[] statementDelimiters = new string[] { ";", "{" };

            string xmlDocPreffix = GetXmlDocPrefix(language);

            var result = new Result();

            int fromLine = caretLine - 1;

            //poor man C# syntax parser. Too bad NRefactory conflicts with Xamarin
            string[] lines = code.Lines().ToArray();
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

        public static Result FindMemberDocumentationNRefactoryNew(string code, int fromLine)
        {
            var result = new Result();

            var syntaxTree = new CSharpParser().Parse(code, "demo.cs");

            var comment = syntaxTree.Children
                                    .DeepAll(x => x is Comment)
                                    .Cast<Comment>()
                                    .Where(c => c.CommentType == CommentType.Documentation && c.StartLocation.Line <= fromLine && c.EndLocation.Line >= fromLine) //inside of the node
                                    .Select(t => new { Node = t, Size = t.EndLocation.Line - t.StartLocation.Line })
                                    .OrderBy(x => x.Size)
                                    .FirstOrDefault();

            if (comment != null)
            {
                var content = new StringBuilder();
                content.AppendLine(comment.Node.Content);

                var parent = comment.Node.Parent;
                while (parent != null && (parent.NodeType != NodeType.Member))
                {
                    parent = parent.Parent;
                }

                //parent is the declaration with the comments included shocking runtime difference
                //comparing to the unit test environment
                return null;
                ///////////////////////////////////
                var prevComment = comment.Node.PrevSibling as Comment;
                while (prevComment != null && prevComment.CommentType == CommentType.Documentation)
                {
                    content.PrependLine(prevComment.Content);
                    prevComment = prevComment.PrevSibling as Comment;
                }
                result.XmlDocumentation = content.ToString();

                var nextNode = comment.Node.NextSibling;
                while (nextNode != null && (nextNode.NodeType != NodeType.Member && nextNode.NodeType != NodeType.TypeDeclaration))
                {
                    nextNode = nextNode.NextSibling;
                }

                if (nextNode != null && (nextNode.NodeType == NodeType.Member || nextNode.NodeType == NodeType.TypeDeclaration || nextNode.NodeType == NodeType.TypeDeclaration))
                {
                    //<membertype>:<signature>
                    var signatureInfo = (nextNode.GetMemberSignature(code) ?? ":").Split(':');
                    result.MemberTitle = signatureInfo[0];
                    result.MemberDefinition = signatureInfo[1];
                }
                else
                {
                    result.MemberTitle = "Member";
                    result.MemberDefinition = "[some member]";
                }
                result.Success = true;
            }

            return result;
        }

        public static Result FindMemberDocumentationNRefactory(string code, int fromLine)
        {
            var result = new Result();

            var syntaxTree = new CSharpParser().Parse(code, "demo.cs");

            var comment = syntaxTree.Children
                                    .DeepAll(x => x is Comment)
                                    .Cast<Comment>()
                                    .Where(c => c.CommentType == CommentType.Documentation && c.StartLocation.Line <= fromLine && c.EndLocation.Line >= fromLine) //inside of the node
                                    .Select(t => new { Node = t, Size = t.EndLocation.Line - t.StartLocation.Line })
                                    .OrderBy(x => x.Size)
                                    .FirstOrDefault();

            if (comment != null)
            {
                var content = new StringBuilder();
                content.AppendLine(comment.Node.Content);

                var prevComment = comment.Node.PrevSibling as Comment;
                while (prevComment != null && prevComment.CommentType == CommentType.Documentation)
                {
                    content.PrependLine(prevComment.Content);
                    prevComment = prevComment.PrevSibling as Comment;
                }
                result.XmlDocumentation = content.ToString();

                var nextNode = comment.Node.NextSibling;
                while (nextNode != null && (nextNode.NodeType != NodeType.Member && nextNode.NodeType != NodeType.TypeDeclaration))
                {
                    nextNode = nextNode.NextSibling;
                }

                if (nextNode != null && (nextNode.NodeType == NodeType.Member || nextNode.NodeType == NodeType.TypeDeclaration || nextNode.NodeType == NodeType.TypeDeclaration))
                {
                    //<membertype>:<signature>
                    var signatureInfo = (nextNode.GetMemberSignature(code) ?? ":").Split(':');
                    result.MemberTitle = signatureInfo[0];
                    result.MemberDefinition = signatureInfo[1];
                }
                else
                {
                    result.MemberTitle = "Member";
                    result.MemberDefinition = "[some member]";
                }
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

        public static string[] Words(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return new string[0];

            return input.Split(wordDelimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        //public static string[] SplitByLast(this string input, char delimiter)
        //{
        //    return input.SplitBySingle(delimiter, true);
        //}

        //public static string[] SplitByFirst(this string input, char delimiter)
        //{
        //    return input.SplitBySingle(delimiter, false);
        //}

        //static string[] SplitBySingle(this string input, char delimiter, bool last)
        //{
        //    if (string.IsNullOrEmpty(input))
        //        return new string[0];

        //    int pos;

        //    if (last)
        //        pos = input.LastIndexOf(delimiter);
        //    else
        //        pos = input.IndexOf(delimiter);

        //    if (pos != -1)
        //        return new string[] { input.Substring(0, pos), input.Substring(pos) };

        //    return new string[] { input };
        //}

        //public static string[] SplitByFirst(this string input, string delimiter)
        //{
        //    return input.SplitBySingle(delimiter, false);
        //}

        //static string[] SplitBySingle(this string input, string delimiter, bool last)
        //{
        //    if (string.IsNullOrEmpty(input))
        //        return new string[0];

        //    int pos;

        //    if (last)
        //        pos = input.LastIndexOf(delimiter);
        //    else
        //        pos = input.IndexOf(delimiter);

        //    if (pos != -1)
        //    {
        //        if (pos == 0)
        //            return new string[] { input.Substring(pos + delimiter.Length) };
        //        else if (pos == input.Length - delimiter.Length - 1)
        //            return new string[] { input.Substring(pos) };
        //        else
        //            return new string[] { input.Substring(0, pos), input.Substring(pos + delimiter.Length) };

        //    }
        //    return new string[] { input };
        //}

        public static IEnumerable<string> Lines(this string input)
        {
            string line;
            using (var reader = new StringReader(input))
                while ((line = reader.ReadLine()) != null)
                    yield return line;

            yield return null;
        }

        public static StringBuilder PrependLine(this StringBuilder sb, string content)
        {
            return sb.Insert(0, content + Environment.NewLine);
        }

        public static string ToDisplayString(this IEnumerable<TypeParameterDeclaration> parameters)
        {
            return parameters.Any() ? "<...>" : "";
        }

        public static string ToDisplayString(this IEnumerable<ParameterDeclaration> parameters)
        {
            return parameters.Any() ? "..." : "";
        }

        public static string FormatApiSignature(this string text)
        {
            return text.Trim();
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

        public static string GetMemberSignature(this AstNode node, string code)
        {
            if (node is MethodDeclaration)
            {
                var info = (MethodDeclaration)node;
                return $"Method {info.Name}:{info.ReturnType} {info.Name}{info.TypeParameters.ToDisplayString()}({info.Parameters.ToDisplayString()})";
            }
            else if (node is TypeDeclaration)
            {
                var info = (TypeDeclaration)node;
                return $"{info.ClassType} {info.Name}:{info.ClassType.ToString().ToLower()} {info.Name}";
            }
            else if (node is DelegateDeclaration)
            {
                var info = (DelegateDeclaration)node;
                return $"Delegate {info.Name}:delegate {info.ReturnType} {info.Name}{info.TypeParameters.ToDisplayString()}({info.Parameters.ToDisplayString()})";
            }
            else if (node is ConstructorDeclaration)
            {
                var info = (ConstructorDeclaration)node;
                return $"Constructor {info.Name}:{info.Name}({info.Parameters.ToDisplayString()})";
            }
            else if (node is PropertyDeclaration)
            {
                var info = (PropertyDeclaration)node;
                return $"Property {info.Name}:{info.ReturnType} {info.Name};";
            }
            else if (node is FieldDeclaration)
            {
                var info = (FieldDeclaration)node;

                var name = info.Name;
                if (!name.HasText())
                    name = code.GetFieldName(info.StartLocation.Line - 1, info.EndLocation.Line - 1);

                return $"Field {name}:{info.ReturnType} {name}";
            }
            else if (node is EventDeclaration info)
            {
                var name = info.Name;
                if (!name.HasText())
                    name = code.GetFieldName(info.StartLocation.Line - 1, info.EndLocation.Line - 1);

                return $"Event {name}:event {info.ReturnType} {name}";
            }
            return null;
        }

        public static string GetFieldName(this string code, int startLine, int endLine)
        {
            var lines = code.Lines().Skip(startLine).Take(endLine - startLine + 1).ToArray();
            var declaration = string.Join("", lines).Split(';').First().Trim();

            var pos = declaration.Replace("\t", " ").LastIndexOf(' ');

            if (pos == -1)
                return "[someField]";
            else
                return declaration.Substring(pos).Trim();
        }

        public static bool HasText(this string text)
        {
            return !string.IsNullOrEmpty(text);
        }

        public static string[] GetLines(this string text)
        {
            return text.Replace(Environment.NewLine, "\n").Split('\n');
        }

        public static string GetNamespace(this EntityDeclaration node)
        {
            string result = "";

            var parent = node.Parent;
            while (parent != null)
            {
                if (parent is NamespaceDeclaration)
                {
                    if (result.HasText())
                        result += ".";
                    result += (parent as NamespaceDeclaration).Name;
                }
                parent = parent.Parent;
            }

            return result;
        }

        public static IEnumerable<AstNode> DeepAll(this IEnumerable<AstNode> collection, Func<AstNode, bool> selector)
        {
            //pseudo recursion
            var result = new List<AstNode>();
            var queue = new Queue<AstNode>(collection);

            while (queue.Count > 0)
            {
                AstNode node = queue.Dequeue();
                if (selector(node))
                    result.Add(node);

                foreach (var subNode in node.Children)
                {
                    queue.Enqueue(subNode);
                }
            }

            return result;
        }
    }
}