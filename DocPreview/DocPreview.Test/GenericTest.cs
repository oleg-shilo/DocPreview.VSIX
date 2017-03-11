using System;
using System.Linq;
using DocPreview;
using Xunit;
using System.Diagnostics;

namespace DocPreview.Test
{
    public class GenericTest
    {
        [Fact]
        public void ToLogicalSignature()
        {
            var result = "public static implicit operator DBBool(bool x) ".ToLogicalSignature();
            Assert.Equal("operator DBBool(bool x)", result);

            result = "int GetCount<T>(T data) where T : new();".ToLogicalSignature();
            Assert.Equal("int GetCount(T data) where T : new();", result);

            result = " List<T> GetLogFile<T,V>(List<Dictionary<T,V>> options)  where T: class".ToLogicalSignature();
            Assert.Equal("List GetLogFile(List options) where T: class", result);

            result = "event Action MyEvent;".ToLogicalSignature();
            Assert.Equal("event Action MyEvent;", result);

            result = "public string Name { get; set; } = new string('d', 10);".ToLogicalSignature();
            Assert.Equal("string Name { get; set; } = new string('d', 10);", result);

            result = "public int MyAutoProperty { get; set; }".ToLogicalSignature();
            Assert.Equal("int MyAutoProperty { get; set; }", result);

            result = "public void ThrowException(string message)".ToLogicalSignature();
            Assert.Equal("void ThrowException(string message)", result);

            result = " delegate int GetTest();".ToLogicalSignature();
            Assert.Equal("delegate int GetTest();", result);

            result = " delegate int GetTest<T>() where T : new();".ToLogicalSignature();
            Assert.Equal("delegate int GetTest() where T : new();", result);

            result = "static class AnotherClass<T> where T : new()".ToLogicalSignature();
            Assert.Equal("class AnotherClass where T : new()", result);

            result = "public struct GenericClass".ToLogicalSignature();
            Assert.Equal("struct GenericClass", result);

            result = "public enum GenericClass".ToLogicalSignature();
            Assert.Equal("enum GenericClass", result);

            result = "public GenericClass()".ToLogicalSignature();
            Assert.Equal("GenericClass()", result);

            result = " public event Action myEvent;".ToLogicalSignature();
            Assert.Equal("event Action myEvent;", result);

            result = " public int myField;".ToLogicalSignature();
            Assert.Equal("int myField;", result);

            result = " public int MyProperty2{ ".ToLogicalSignature();
            Assert.Equal("int MyProperty2{", result);
        }

        [Fact]
        public void ToTitle()
        {
            var result = "public static implicit operator DBBool(bool x) {".ToMemberTitle();
            Assert.Equal("Operator DBBool", result);

            result = "int GetCount<T>(T data) where T : new();".ToMemberTitle();
            Assert.Equal("Method GetCount", result);

            result = " List<T> GetLogFile<T,V>(List<Dictionary<T,V>> options)  where T: class".ToMemberTitle();
            Assert.Equal("Method GetLogFile", result);

            result = "event Action MyEvent;".ToMemberTitle();
            Assert.Equal("Event MyEvent", result);

            result = "public string Name{".ToMemberTitle();
            Assert.Equal("Property Name", result);

            result = "public int MyAutoProperty{".ToMemberTitle();
            Assert.Equal("Property MyAutoProperty", result);

            result = "public void ThrowException(string message)".ToMemberTitle();
            Assert.Equal("Method ThrowException", result);

            result = " delegate int GetTest();".ToMemberTitle();
            Assert.Equal("Delegate GetTest", result);

            result = " delegate int GetTest<T>() where T : new();".ToMemberTitle();
            Assert.Equal("Delegate GetTest", result);

            result = "static class AnotherClass<T> where T : new()".ToMemberTitle();
            Assert.Equal("Class AnotherClass", result);

            result = "public struct GenericClass".ToMemberTitle();
            Assert.Equal("Struct GenericClass", result);

            result = "public enum GenericClass".ToMemberTitle();
            Assert.Equal("Enum GenericClass", result);

            result = "public GenericClass()".ToMemberTitle();
            Assert.Equal("Constructor GenericClass", result);

            result = " public event Action myEvent;".ToMemberTitle();
            Assert.Equal("Event myEvent", result);

            result = " public int myField;".ToMemberTitle();
            Assert.Equal("Field myField", result);

            result = " public int MyProperty2{ ".ToMemberTitle();
            Assert.Equal("Property MyProperty2", result);
        }
    }
}