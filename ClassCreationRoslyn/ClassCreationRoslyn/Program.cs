using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace ClassCreationRoslyn
{
    class Program
    {
        static void Main(string[] args)
        {



            StringBuilder _sb = new StringBuilder();
            _sb.AppendLine("using System;");
            _sb.AppendLine("using System.Collections.Generic;");
            _sb.Append(@"
           
                namespace MyNamespace
                {
                    public class DynamicClass
                    {
                        public string DynamicCode(Dictionary<string, string> sourceItem)
                        {
                           string x = sourceItem[""test3""];

                        
                      ");

            Program P = new Program();
            P.RunCode(_sb);

            _sb.Append(@"}
                    }
                }");

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(_sb.ToString());

            
            string assemblyName = Path.GetRandomFileName();
            MetadataReference[] references = new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location)
        };

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));



            // use the dynamic class
            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());

                    Type type = assembly.GetType("MyNamespace.DynamicClass");
                    object obj = Activator.CreateInstance(type);

                    Dictionary<string, string> newDictionary = new Dictionary<string, string>();
                    newDictionary.Add("test1", "test11");
                    newDictionary.Add("test2", "test22");
                    newDictionary.Add("test3", "test33");


                    var dynamicMethodResult = type.InvokeMember("DynamicCode", BindingFlags.Default | BindingFlags.InvokeMethod, null, obj, new object[] { newDictionary });
                }
            }

            Console.ReadLine();
        }

        private void RunCode(StringBuilder sb)
        {
            sb.Append($@" return ""says hi to ""; ");
        }
    }
}
