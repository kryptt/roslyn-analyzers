﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using System.Data;
using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using Philips.CodeAnalysis.Test;
using System.Text.RegularExpressions;
using Moq;
using Castle.Core.Internal;
using System.Collections.Immutable;
using Philips.CodeAnalysis.Test.Verifiers;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Maintainability.Maintainability
{
	[TestClass]
	public class ReduceCognitiveLoadAnalyzerTest : DiagnosticVerifier
	{
		private string MakeRegex(int expectedLoad)
		{
			return $"Load of {expectedLoad} ";
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			Mock<AdditionalFilesHelper> _mockAdditionalFilesHelper = new(new AnalyzerOptions(ImmutableArray.Create<AdditionalText>()), null);
			_mockAdditionalFilesHelper.Setup(c => c.GetValueFromEditorConfig(It.IsAny<string>(), It.IsAny<string>())).Returns("1");
			return new ReduceCognitiveLoadAnalyzer(_mockAdditionalFilesHelper.Object);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadGeneratedCodeIgnoredTest()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {} // over the threshold
	}
}
";
			VerifySuccessfulCompilation(template, "Test.Designer");
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoad1()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
	}
}
";
			VerifySuccessfulCompilation(template);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadIf()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1) {}
	}
}
";
			string regex = MakeRegex(2);
			VerifyDiagnostic(template, regex: regex);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadExpressionBody()
		{
			const string template = @"
class Foo
{
	private string MakeString() => new string(""MyString"");
}
";
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadIfNotEqual()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1!=1) {}
	}
}
";
			string regex = MakeRegex(3);
			VerifyDiagnostic(template, regex: regex);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadIfReturn()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1) {return;}
	}
}
";
			string regex = MakeRegex(2);
			VerifyDiagnostic(template, regex: regex);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadIfLogicalOr()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1 || 2==2) {}
	}
}
";
			string regex = MakeRegex(3);
            VerifyDiagnostic(template, regex: regex);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadIfLogicalAnd()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1 && 2==2) {}
	}
}
";
			string regex = MakeRegex(3);
			VerifyDiagnostic(template, regex: regex);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadBreak()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		while (1<2) { break; }
	}
}
";
			string regex = MakeRegex(3);
            VerifyDiagnostic(template, regex: regex);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadBang()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (!(1==1)) {}
	}
}
";
			string regex = MakeRegex(3);
            VerifyDiagnostic(template, regex: regex);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadNested1()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1)
		{
			// Nested block statement counts double
			if (2 == 2) {}
		}
	}
}
";
			string regex = MakeRegex(4);
			VerifyDiagnostic(template, regex: regex);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadNested2()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1)
		{
			// Nested block statement counts double
			if (2 == 2) {}
			// Nested block statement counts double
			if (3 == 3) {}
		}
	}
}
";
			string regex = MakeRegex(6);
			VerifyDiagnostic(template, regex: regex);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadNested3()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1==1)
		{
			// Nested block statement counts double
			if (2 == 2)
			{
				// Nested block statement counts more than triple - it's exponential
				if (3 == 3) {}
			}
		}
	}
}
";
			string regex = MakeRegex(8);
			VerifyDiagnostic(template, regex: regex);
		}



		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadCombo()
		{
			const string template = @"
class Foo
{
	private void Test()
	{ //1
		if(1==1)
		{ //2
			if(2==2) {return;} //3,4
		}
		if(3==3)
		{ return;} // 5
	}
}
";
			string regex = MakeRegex(5);
			VerifyDiagnostic(template, regex: regex);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoad3()
		{
			const string template = @"
class Foo
{
	private void Test()
	{ //1
		int dynamicDataCount = 0;
		int dataRowCount = 0;
		bool hasTestSource = false;
		foreach (AttributeSyntax attribute in methodDeclaration.AttributeLists.SelectMany(x => x.Attributes))
		{ //2
			if (Helper.IsDataRowAttribute(attribute, context))
			{ //3,4
				dataRowCount++;
				continue; //5
			}
			if (Helper.IsAttribute(attribute, context, MsTestFrameworkDefinitions.DynamicDataAttribute, out _, out _))
			{ //6,7
				dynamicDataCount++;
				continue; //8
			}
			SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(attribute);
			if (symbol.Symbol is IMethodSymbol method)
			{ //9,10
				if (method.ContainingType.AllInterfaces.Contains(Definitions.ITestSourceSymbol))
				{ //11,12,13,14
					hasTestSource = true;
				}
			}
		}
		if (isDataTestMethod)
		{ //15
			if (dataRowCount != 0 && dynamicDataCount == 0) //16,17
			{ // 18,19
				return;
			}
			if (dataRowCount == 0 && dynamicDataCount == 1) // 20
			{ //21,22
				return;
			}
			if (dataRowCount != 0 && dynamicDataCount != 0) //23,24,25
			{ //26,27
				context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodDeclaration.Identifier.ToString(), dataRowCount, dynamicDataCount));
			}
			else if (!hasTestSource) // 28
			{ //39,30
				context.ReportDiagnostic(Diagnostic.Create(RuleShouldBeTestMethod, methodDeclaration.Identifier.GetLocation()));
			}
		}
		else
		{ //31
			if (dataRowCount == 0 && dynamicDataCount == 0) //32
			{ //33,34
				return;
			}
			context.ReportDiagnostic(Diagnostic.Create(RuleShouldBeDataTestMethod, methodDeclaration.Identifier.GetLocation()));
		}
	}
}
";
			string regex = MakeRegex(34);
			VerifyDiagnostic(template, regex: regex);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoad4()
		{
			const string template = @"
class Foo
{			
	private void Test()
	{																							// 1
		PropertyDeclarationSyntax property = (PropertyDeclarationSyntax)context.Node;
		if (property.Type.ToString() != @""TestContext"")										// 2
		{																						// 3
			return;																				
		}
																								// 4,5
		if ((context.SemanticModel.GetSymbolInfo(property.Type).Symbol is not ITypeSymbol symbol) || (symbol.ToString() != @""Microsoft.VisualStudio.TestTools.UnitTesting.TestContext""))
		{																						// 6
			return;																				
		}
		string varName = string.Empty;
		string propName = property.Identifier.ToString();
		IEnumerable<SyntaxNode> propNodes = context.Node.DescendantNodes();
		IEnumerable<ReturnStatementSyntax> returnNodes = propNodes.OfType<ReturnStatementSyntax>();
		if (returnNodes.Any())
		{																						// 7
			ReturnStatementSyntax returnStatement = returnNodes.First();
			if (returnStatement != null)														// 8
			{																					// 9,10
				if (returnStatement.Expression is IdentifierNameSyntax returnVar)
				{																				// 11,12,13,14
					varName = returnVar.Identifier.ToString();
				}
			}
		}
		// find out if the property or its underlying variable is actually used
		foreach (IdentifierNameSyntax identifier in context.Node.Parent.DescendantNodes().OfType<IdentifierNameSyntax>())
		{																									// 15
			if ((identifier.Identifier.ToString() == propName) && (identifier.Parent != property) &&		// 16,17
				context.SemanticModel.GetSymbolInfo(identifier).Symbol is not ITypeSymbol)
			{																								// 18,19
				// if we find the same identifier as the propery and it's not a type or the original instance, it's used
				return;																						
			}
			if ((identifier.Identifier.ToString() == varName)
				&& identifier.Parent is not VariableDeclarationSyntax										// 20
				&& !propNodes.Contains(identifier)															// 21,22
				&& context.SemanticModel.GetSymbolInfo(identifier).Symbol is not ITypeSymbol)				// 23
			{																								// 24,25
				// if we find the same identifier as the variable and it's not a type, the original declaration, or part of the property, it's used
				return;																						
			}
		}
		// if not, report a diagnostic error
		Diagnostic diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation());
		context.ReportDiagnostic(diagnostic);
		return;																								
	}
}
";
			string regex = MakeRegex(26);
			VerifyDiagnostic(template, regex: regex);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoad5()
		{
			const string template = @"
class Foo
{
	private void Test()
	{	//1
		SyntaxNode rootNode = await document.GetSyntaxRootAsync(cancellationToken);
		// find the underlying variable
		IEnumerable<SyntaxNode> propNodes = declaration.DescendantNodes();
		ReturnStatementSyntax returnStatement = propNodes.OfType<ReturnStatementSyntax>().First();
		string varName = string.Empty;
		if (returnStatement != null) //2
		{ //3
			if (returnStatement.Expression is IdentifierNameSyntax returnVar)
			{ //4,5
				varName = returnVar.Identifier.ToString();
			}
		}
		// remove the property
		if (rootNode != null) //6
		{ //7,8
			rootNode = rootNode.RemoveNode(declaration, SyntaxRemoveOptions.KeepNoTrivia);
			if (!string.IsNullOrEmpty(varName)) //9
			{ //10,11
				foreach (VariableDeclarationSyntax varDeclaration in rootNode.DescendantNodes()
					.OfType<VariableDeclarationSyntax>())
				{ // 12,13,14,15
					if (varDeclaration.Variables[0].Identifier.ToString() == varName)
					{ //16,17,18,19,20,21,22,23
						// remove the underlying variable
						if (varDeclaration.Parent != null) // 24
						{ 23+16 = 39
							rootNode = rootNode.RemoveNode(varDeclaration.Parent, SyntaxRemoveOptions.KeepNoTrivia);
						}
						break; //40
					}
				}
			}
			document = document.WithSyntaxRoot(rootNode);
		}
		return document;
	}
}";
			string regex = MakeRegex(48);
			VerifyDiagnostic(template, regex: regex);
		}
	}

	[TestClass]
	public class ReduceCognitiveLoadAnalyzerInvalidInitializationTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			const string InvalidMaxLoad = @"1000";
			Mock<AdditionalFilesHelper> _mockAdditionalFilesHelper = new(new AnalyzerOptions(ImmutableArray.Create<AdditionalText>()), null);
			_mockAdditionalFilesHelper.Setup(c => c.GetValueFromEditorConfig(It.IsAny<string>(), It.IsAny<string>())).Returns(InvalidMaxLoad);
			return new ReduceCognitiveLoadAnalyzer(_mockAdditionalFilesHelper.Object);
		}


		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadInitPassTest()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
	}
}
";
			VerifySuccessfulCompilation(template);
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadInitFailTest()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {} // over the threshold
	}
}
";
			VerifyDiagnostic(template, regex: "Load of 27 ");
		}
	}

	[TestClass]
	public class ReduceCognitiveLoadAnalyzerDefaultInitializationTest : DiagnosticVerifier
	{
		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new ReduceCognitiveLoadAnalyzer();
		}

		[TestMethod]
		[TestCategory(TestDefinitions.UnitTests)]
		public void CognitiveLoadInitPassTest()
		{
			const string template = @"
class Foo
{
	private void Test()
	{
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
		if (1!=1) {}
	}
}
";
			VerifySuccessfulCompilation(template);
		}
	}
}
