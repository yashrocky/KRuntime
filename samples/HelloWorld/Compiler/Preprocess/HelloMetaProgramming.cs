using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Framework.Runtime;

namespace HelloWorld.Compiler.Preprocess
{
    class FooRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var index = 0;
            var statements = node.Body.Statements;
            foreach (var parameter in node.ParameterList.Parameters)
            {
                var isNotNull = parameter.AttributeLists.SelectMany(l => l.Attributes)
                                                        .Any(a => ((IdentifierNameSyntax)a.Name).Identifier.Value.Equals("NotNull"));
                if (isNotNull)
                {
                    var ifStatement = SyntaxFactory.ParseStatement(string.Format(
@"#line hidden
if ({0} == null) {{ throw new {1}(nameof({0})); }}
", parameter.Identifier, typeof(ArgumentNullException).FullName));
                    if (index == 0)
                    {
                        // We need to inject a #line <line number> before the first statement in the original method body so that the
                        // debugger matches up to the unmodified source file.
                        var statement = statements[0];
                        // Roslyn line numbers are 0-based, but VS uses 1-based line numbers.
                        var lineNumber = statement.GetLocation().GetMappedLineSpan().StartLinePosition.Line + 1;
                        var lineDefault = SyntaxFactory.ParseLeadingTrivia("#line " + lineNumber + Environment.NewLine);
                        if (statement.HasLeadingTrivia)
                        {
                            lineDefault = lineDefault.AddRange(statement.GetLeadingTrivia());
                        }

                        statements = statements.Replace(statement, statement.WithLeadingTrivia(lineDefault));
                    }

                    statements = statements.Insert(index++, ifStatement);
                }
            }

            if (index > 0)
            {
                node = node.WithBody(node.Body.WithStatements(statements));
            }

            return base.VisitMethodDeclaration(node);
        }
    }

    public class HelloMetaProgramming : ICompileModule
    {
        public HelloMetaProgramming(IServiceProvider services)
        {
        }

        public void BeforeCompile(IBeforeCompileContext context)
        {
            var rewriter = new FooRewriter();
            var syntraxTrees = context.CSharpCompilation.SyntaxTrees;
            foreach (var item in syntraxTrees)
            {
                var replaced = rewriter.Visit(item.GetRoot());
                context.CSharpCompilation = context.CSharpCompilation.ReplaceSyntaxTree(item,
                                                                                        item.WithRootAndOptions(replaced, item.Options));
            }
        }

        public void AfterCompile(IAfterCompileContext context)
        {

        }
    }
}

namespace Microsoft.Framework.Runtime
{
    [AssemblyNeutral]
    public class AssemblyNeutralAttribute : Attribute
    {
    }

    /// <summary>
    /// Summary description for ICompileModule
    /// </summary>
    [AssemblyNeutral]
    public interface ICompileModule
    {
        void BeforeCompile(IBeforeCompileContext context);

        void AfterCompile(IAfterCompileContext context);
    }

    [AssemblyNeutral]
    public interface IBeforeCompileContext
    {
        CSharpCompilation CSharpCompilation { get; set; }

        IList<ResourceDescription> Resources { get; }

        IList<Diagnostic> Diagnostics { get; }
    }

    [AssemblyNeutral]
    public interface IAfterCompileContext
    {
        CSharpCompilation CSharpCompilation { get; set; }

        IList<Diagnostic> Diagnostics { get; }
    }
}


