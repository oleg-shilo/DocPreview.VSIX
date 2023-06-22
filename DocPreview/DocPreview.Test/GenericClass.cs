using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocPreview.Test
{
    /// <summary>
    /// Class documentation
    /// </summary>
    class GenericClass
    {
        public int MyProperty { get; set; }

        /// <summary>
        /// Occurs when [my event].
        /// </summary>
        public event Action myEvent;

        /// <summary>
        /// My field
        /// </summary>
        public int myField;

        /// <summary>
        /// Gets or sets my property2.
        /// </summary>
        /// <value>My property2.</value>
        public int MyProperty2
        {
            get { return myField; }

            set
            {
                myField = value;
            }
        }

        /// <summary>
        /// Enum doc
        /// </summary>
        enum TestEnum
        {
            valA,
            valB,
            valC
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericClass"/> class.
        /// </summary>
        public GenericClass()
        {
        }

        /// <summary>
        /// Struct doc
        /// </summary>
        struct MyStruct
        {
        }

        /// <summary>
        /// Delegate doc
        /// </summary>
        delegate int GetTest();

        /// <summary>
        /// Gets the log file.
        /// </summary>
        /// <param name="options">The options.</param>
        List<T> GetLogFile<T, V>(List<Dictionary<T, V>> options) where T : class
        {
            return null;
        }

        /// <summary>
        /// Gets or sets my auto property.
        /// </summary>
        /// <value>My auto property.</value>
        public int MyAutoProperty { get; set; }

        /// <summary>
        /// Gets the name of the log file.
        /// </summary>
        /// <returns>File name</returns>
        string GetLogFileName()
        {
            return null;
        }

        /// <summary>
        /// Throws the <paramref name="message" /> exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="T:System.ApplicationException">Will throw exception on app error</exception><exception cref="!:JustBecauseException">Throw exception anytime</exception>
        public void ThrowException(string message)
        {
        }

        /// <summary>
        /// Gets or sets my auto property.
        /// </summary>
        /// <value>My auto property.</value>
        public string Name { get; set; } = new string('d', 10);
    }

    /// <summary>
    /// AnotherClass doc
    /// </summary>
    /// <typeparam name="T">The type of the T.</typeparam>
    class AnotherClass<T> where T : new()
    {
    }

    /// <summary>
    /// ITest doc
    /// </summary>
    interface ITest
    {
        /// <summary>
        /// Gets or sets my property.
        /// </summary>
        /// <value>My property.</value>
        int MyProperty { get; set; }

        /// <summary>
        /// Occurs when [my event].
        /// </summary>
        event Action MyEvent;

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <typeparam name="T">The type of the T.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        int GetCount<T>(T data) where T : new();

        /// <summary>
        /// Initializes a new instance of the <see cref="Enum{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        object ToEnum(string value);
    }

    /// <summary>
    /// Gets or sets the Patient Code.
    /// <list type="bullet">
    /// <listheader><description>Validation:</description></listheader>
    ///    <item><description> <see cref="WebServer"/> Field Max Length: 25</description></item>
    ///    <item><description>Field Required</description></item>
    /// </list>
    /// </summary>
    /// <value>The region reference key.</value>
    class MyClass3
    {
    }

    class Issue_1
    {
        /// <summary>
        /// dsfadsgfdfgdfsgd
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="volumeSpec"></param>
        /// <param name="constraints"></param>
        /// <param name="chunking"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Task<List<List<T>>> CreateChunkedVolumeFromVolumeSpec<T>(List<int> volumeSpec, [SpawnId] int constraints, List<List<int>> chunking, AppDomain context)
        {
            return null;
        }
    }

    class SpawnIdAttribute : Attribute
    {
    }

    class t1
    {
        /// <summary>
        /// ttt
        /// </summary>
        ///  <exception cref="T:System.ArgumentNullException">The <paramref name="innerException"/> argument
        /// is null.</exception>
        void test()
        {
        }
    }

    /// <summary>
    /// Test Enum
    /// </summary>
    enum TestEnum
    {
        /// <summary>
        /// value #1
        /// </summary>
        value1,

        /// <summary>
        /// value #2
        /// </summary>
        value2,

        /// <summary>
        /// value #3
        /// </summary>
        value3,
    }

    class ParentClass
    {
        /// <summary>
        /// Test class definition
        /// </summary>
        class Test
        {
            /// <summary>
            /// FOO <b>Evaluate</b> the expression associated with the target property specified.
            /// </summary>
            /// <returns>
            /// A <see cref="JValue"/> containing the value of the expression evaluated. The following rules apply to the
            /// return values:
            /// <list type="bullet">
            /// <item>
            /// If the target property is found and the expression evaluates to a valid primitive value, then a JValue
            /// is returned that includes the result of the expression.
            /// </item>
            /// <item><description>
            /// If the target property is found <see cref="member">Link text</see> and evaluates to <b>null</b> then a JValue containing null is returned.
            /// </description>
            /// </item>
            /// <item>
            /// If the target property is not found, then null is returned.
            /// </item>
            /// <item>
            ///  <term><b>Assembly</b></term>
            ///  <description>The library or executable built from a compilation.</description>
            /// </item>
            /// </list>
            /// </returns>
            /// <code></code>
            int foo { }

            /// <summary>
            /// FOO2 <list type="number">
            /// <listheader>
            ///     <term>term</term>
            ///     <description>description</description>
            /// </listheader>
            /// <item>
            ///     <term>Assembly</term>
            ///     <description>The library or executable built from a compilation.
            ///     </description>
            /// </item>
            /// </list>
            /// </summary>
            /// <value>
            /// The foo2.
            /// </value>
            int foo2 { }
        }

        /// <inheritdoc />
        class TestAgain : Test, ITest
        {
            /// <inheritdoc />
            int foo { }

            /// <inheritdoc />
            /// <remarks>This is a dummy class and always returns null.</remarks>
            int foo2 { }

            /// <inheritdoc cref="DocPreview.Test.ParentClass.Test.foo2"/>
            /// <remarks>This is a dummy class and always returns null.</remarks>
            int foo3 { }

            /// <inheritdoc cref="Object.ToString" />
            public override string ToString()
            {
                return base.ToString();
            }
        }

        struct StructBase
        {
            /// <summary>
            /// Fooes the specified arg1-3.
            /// </summary>
            /// <param name="arg1">The arg1.</param>
            /// <param name="arg2">The arg2.</param>
            /// <param name="arg3">The arg3.</param>
            /// <returns></returns>
            int foo3(int arg1, string arg2, string arg3) { }
        }

        /// <inheritdoc cref="DocPreview.Test.TestBase"/>
        struct StructClass : StructBase, ITest
        {
            /// <inheritdoc cref="DocPreview.Test.TestBase.TestBase()"/>
            public StructClass()
            {
            }
            /// <summary>
            /// Fooes the specified arg1.
            /// </summary>
            /// <param name="arg1">The arg1.</param>
            /// <returns></returns>
            int foo(int arg1) { }

            /// <summary>
            /// Fooes the specified arg1, arg2.
            /// </summary>
            /// <param name="arg1">The arg1.</param>
            /// <param name="arg2">The arg2.</param>
            /// <returns></returns>
            int foo(int arg1, string arg2) { }

            /// <summary>
            /// This overloaded method does something
            /// </summary>
            /// <param name="p1">The string parameter p1</param>
            /// <overloads>
            /// <summary>There are three overloads for this method.</summary>
            /// <remarks>These remarks are from the overloads tag on the
            /// first version.</remarks>
            /// </overloads>
            public void OverloadedMethod(string p1)
            {
            }
        }

        struct StructSuperClass : StructClass, ITest
        {
            /// <inheritdoc cref="foo(int, string)" />
            int foo(int arg1, string arg2) { }

            /// <inheritdoc />
            int foo3(int arg1, string arg2, string arg3) { }

            /// <inheritdoc />
            int foo(int arg1) { }

            /// <inheritdoc cref="OverloadedMethod(string)"
            ///     select="param|overloads/*" />
            /// <param name="x">An integer parameter</param>
            public void OverloadedMethod(string p1, int x)
            {
            }

            delegate int Deltest();
        }
    }