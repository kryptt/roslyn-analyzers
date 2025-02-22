﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Philips.CodeAnalysis.Common
{
	public class GeneratedCodeDetector
	{
		private readonly AttributeHelper _attributeHelper = new();

		private const string AttributeName = @"GeneratedCode";
		private const string FullAttributeName = @"System.CodeDom.Compiler.GeneratedCodeAttribute";

		private bool HasGeneratedCodeAttribute(SyntaxNode inputNode, Func<SemanticModel> getSemanticModel)
		{
			SyntaxNode node = inputNode;
			while (node != null)
			{
				SyntaxList<AttributeListSyntax> attributes;
				switch (node)
				{
					case ClassDeclarationSyntax cls:
						attributes = cls.AttributeLists;
						break;
					case StructDeclarationSyntax st:
						attributes = st.AttributeLists;
						break;
					case MethodDeclarationSyntax method:
						attributes = method.AttributeLists;
						break;
					default:
						node = node.Parent;
						continue;
				}

				if (_attributeHelper.HasAttribute(attributes, getSemanticModel, AttributeName, FullAttributeName, out _, out _))
				{
					return true;
				}

				node = node.Parent;
			}

			return false;
		}


		public bool IsGeneratedCode(OperationAnalysisContext context)
		{
			string myFilePath = context.Operation.Syntax.SyntaxTree.FilePath;
			return IsGeneratedCode(myFilePath) || HasGeneratedCodeAttribute(context.Operation.Syntax, () => { return context.Operation.SemanticModel; });
		}


		public bool IsGeneratedCode(SyntaxNodeAnalysisContext context)
		{
			string myFilePath = context.Node.SyntaxTree.FilePath;
			return IsGeneratedCode(myFilePath) || HasGeneratedCodeAttribute(context.Node, () => { return context.SemanticModel; });
		}

		public bool IsGeneratedCode(SyntaxTreeAnalysisContext context)
		{
			string myFilePath = context.Tree.FilePath;
			return IsGeneratedCode(myFilePath);
		}

		public bool IsGeneratedCode(string filePath)
		{
			Helper helper = new();
			string fileName = helper.GetFileName(filePath);
			// Various Microsoft tools generate files with this postfix.
			bool isDesignerFile = fileName.EndsWith(@".Designer.cs", StringComparison.OrdinalIgnoreCase);
			// WinForms generate files with this postfix.
			bool isGeneratedFile = fileName.EndsWith(@".g.cs", StringComparison.OrdinalIgnoreCase);
			// Visual Studio generates SuppressMessage attributes in this file.
			bool isSuppressionsFile = fileName.EndsWith(@"GlobalSuppressions.cs", StringComparison.OrdinalIgnoreCase);
			return isDesignerFile || isGeneratedFile || isSuppressionsFile;
	}

	}
}
