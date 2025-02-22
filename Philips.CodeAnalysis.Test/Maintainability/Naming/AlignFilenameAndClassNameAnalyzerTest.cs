﻿// © 2023 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Naming;
using Philips.CodeAnalysis.Test.Helpers;
using Philips.CodeAnalysis.Test.Verifiers;

namespace Philips.CodeAnalysis.Test.Maintainability.Naming
{
	/// <summary>
	/// Test class for <see cref="AlignFilenameAndClassNameAnalyzer"/>.
	/// </summary>
	[TestClass]
	public class AlignFilenameAndClassNameAnalyzerTest : DiagnosticVerifier
	{
		private const string SourceCodeTemplate = @"
namespace AlignFilenameAndClassName {{
    {0} Program {{
    }}
}}";

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow("class", "Program"),
		 DataRow("class", "Program.Part"),
		 DataRow("struct", "Program"),
		 DataRow("enum", "Program.Part.Of.Many.Things")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenTestCodeIsValidNoDiagnosticIsTriggered(string typeKind, string filePath)
		{
			VerifySuccessfulCompilation(string.Format(SourceCodeTemplate, typeKind), filePath);
		}

		/// <summary>
		/// No diagnostics expected to show up
		/// </summary>
		[DataTestMethod]
		[DataRow("Program"),
		 DataRow("Program{T}")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenGenericTestCodeIsValidNoDiagnosticIsTriggered(string filePath)
		{
			const string sourceCode = @"
namespace AlignFilenameAndClassName {{
    class Program<T> {{
    }}
}}";
			VerifySuccessfulCompilation(sourceCode, filePath);
		}


		/// <summary>
		/// Diagnostics should show up hare.
		/// </summary>
		[DataTestMethod]
		[DataRow("class", "Program2"),
		 DataRow("struct", "SomethingElse"),
		 DataRow("enum", "Prog")]
		[TestCategory(TestDefinitions.UnitTests)]
		public void WhenNamesDontAlignDiagnosticIsRaised(string typeKind, string filePath)
		{
			VerifyDiagnostic(string.Format(SourceCodeTemplate, typeKind), DiagnosticId.AlignFilenameAndClassName, filePath);
		}

		protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
		{
			return new AlignFilenameAndClassNameAnalyzer();
		}
	}
}
