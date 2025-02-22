﻿// © 2019 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis;
using Philips.CodeAnalysis.Test.Helpers;

namespace Philips.CodeAnalysis.Test.Verifiers
{
	public abstract class AssertDiagnosticVerifier : DiagnosticVerifier
	{
		private readonly AssertCodeHelper _helper = new();

		protected void VerifyError(string methodBody, string expectedDiagnosticId)
		{
			var test = _helper.GetText(methodBody, string.Empty, string.Empty);

			var expected = 
				new DiagnosticResult()
				{
					Id = expectedDiagnosticId,
					Severity = DiagnosticSeverity.Error,
					Location = new DiagnosticResultLocation("Test0.cs", null, null),
				};
			VerifyDiagnostic(test, expected);
		}

		protected void VerifyNoError(string methodBody)
		{
			var test = _helper.GetText(methodBody, string.Empty, string.Empty);

			VerifySuccessfulCompilation(test);
		}
	}
}
