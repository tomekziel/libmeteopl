using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibMeteoPL
{
    class LibMeteoPLTranslatorToJava
    {
        String targetPath;
        String sourceFile;

        /*
        Java output will be splitted to two files: utils interface and parser class
        */
        public LibMeteoPLTranslatorToJava(String sourceFile, String targetPath)
        {
            this.targetPath = targetPath;
            this.sourceFile = sourceFile;
        }

        public void translate()
        {
            String src = File.ReadAllText(sourceFile);
            var tree = CSharpSyntaxTree.ParseText(src);
            var root = tree.GetRoot();

            // remove usings
            root = ((CompilationUnitSyntax)root).WithUsings(new SyntaxList<UsingDirectiveSyntax>());

            // run rewriter on all nodes
            root = new CustomRewriter().Visit(root);

            // get namespace
            var namespc =
                (
              from node in root.DescendantNodes()
              where node.IsKind(SyntaxKind.NamespaceDeclaration)
              select node).First();

            // get parser class node
            var classnode =
                (
              from node in root.DescendantNodes()
              where node.IsKind(SyntaxKind.ClassDeclaration)
              select node).First().WithLeadingTrivia(namespc.GetLeadingTrivia()).WithTrailingTrivia(namespc.GetTrailingTrivia()); ;

            // get utils interface node
            var interfacenode =
                (
              from node in root.DescendantNodes()
              where node.IsKind(SyntaxKind.InterfaceDeclaration)
              select node).First();


            // remove namespace
            root = root.ReplaceNode(namespc, new SyntaxNode[] { classnode });

            // header warning and Java package definition
            SyntaxTrivia[] trivia = new SyntaxTrivia[] {
                        SyntaxFactory.Comment("// THIS FILE IS GENERATED, ALL CHANGES WILL BE LOST"),
                        SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.CarriageReturnLineFeed,
                        SyntaxFactory.Comment("package com.pgssoft.meteopllibrary;"),
                        SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.CarriageReturnLineFeed };

            // add warning and print parser class using syntax walker
            using (StreamWriter file = new StreamWriter(targetPath+"ModelUM.java", false))
            {
                root = root.WithLeadingTrivia(trivia);
                var walker = new CustomWalker(file);
                walker.Visit(root);
            }

            // add warning and print utils interface using syntax walker
            using (StreamWriter file = new StreamWriter(targetPath + "Utils.java", false))
            {
                interfacenode = interfacenode.WithLeadingTrivia(trivia);
                var walker = new CustomWalker(file);
                walker.Visit(interfacenode);
            }
        }

        /*
        This is syntax walker we use to print source code.
        Please note - you have to call base.Visit... to go deeper (or not call to skip nodes/tokens below current)
        */
        public class CustomWalker : CSharpSyntaxWalker
        {
            static int Tabs = 0;
            StreamWriter file;

            public CustomWalker(StreamWriter file) : base(SyntaxWalkerDepth.Token)
            {
                this.file = file;
            }

            // changing C# keywords to Java equivalents
            public override void VisitToken(SyntaxToken token)
            {

                SyntaxToken t = (SyntaxToken)token;
                if (t.HasLeadingTrivia)
                {
                    file.Write(((SyntaxToken)t).LeadingTrivia);
                }

                if (t.IsKind(SyntaxKind.ConstKeyword))
                {
                    file.Write("final");
                }
                else if (t.IsKind(SyntaxKind.BoolKeyword))
                {
                    file.Write("boolean");
                }
                else
                {
                    file.Write(t.ToString());
                }

                if (t.HasTrailingTrivia)
                {
                    file.Write(((SyntaxToken)t).TrailingTrivia);
                }

            }

            // changing C# syntax nodes to Java equivalents
            public override void Visit(SyntaxNode node)
            {

                if (node.IsKind(SyntaxKind.InvocationExpression) && node.ToString().StartsWith("Math.Min"))
                {
                    InvocationExpressionSyntax ies = (InvocationExpressionSyntax)node;
                    file.Write("Math.min");
                    file.Write(ies.ArgumentList);
                }
                else if (node.IsKind(SyntaxKind.InvocationExpression) && node.ToString().StartsWith("Math.Max"))
                {
                    InvocationExpressionSyntax ies = (InvocationExpressionSyntax)node;
                    file.Write("Math.max");
                    file.Write(ies.ArgumentList);
                }
                else if (node.IsKind(SyntaxKind.InvocationExpression) && node.ToString().StartsWith("Math.Abs"))
                {
                    InvocationExpressionSyntax ies = (InvocationExpressionSyntax)node;
                    file.Write("Math.abs");
                    file.Write(ies.ArgumentList);
                }
                /*
                // Sample and useless transformation of node output
                // Takes IfStatementSyntax represented with snippet
                //   if (contition) { codeblock; }
                // and prints it like
                //   fi{ codeblock; }if(condition)
                //
                // Warning - as it is supposed to be simple example, "else" statement part is simply ignored

                else if (node.IsKind(SyntaxKind.IfStatement))
                {
                    IfStatementSyntax tif = (IfStatementSyntax)node;
                    file.Write("fi\n");
                    file.Write(tif.Statement);
                    file.Write("\nif(\n");
                    file.Write(tif.Condition);
                    file.Write("\n)");
                }
                */
                else
                {
                    base.Visit(node);
                }

            }

        }

        /*
        This is syntax rewriter we use to replace some nodes
        Please note - it always walks through all nodes
        */
        class CustomRewriter : CSharpSyntaxRewriter
        {

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node.Identifier.Text.Equals("Length"))
                {
                    return node.WithIdentifier(SyntaxFactory.Identifier("length"));
                }
                return base.VisitIdentifierName(node);
            }


        }


    }
}
