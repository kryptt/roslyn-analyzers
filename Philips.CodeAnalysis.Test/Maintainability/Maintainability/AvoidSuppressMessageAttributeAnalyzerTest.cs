﻿// © 2022 Koninklijke Philips N.V. See License.md in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Philips.CodeAnalysis.Common;
using Philips.CodeAnalysis.MaintainabilityAnalyzers.Maintainability;

namespace Philips.CodeAnalysis.Test
{
	[TestClass]
	public class AvoidSuppressMessageAttributeAnalyzerTest : DiagnosticVerifier
	{
		private const string allowedMethodName = @"Foo.AllowedInitializer()
Foo.AllowedInitializer(Bar)
Foo.WhitelistedFunction
";

		protected override (string name, string content)[] GetAdditionalTexts()
		{
			return new[] { ("NotFile.txt", "data"), (AvoidSuppressMessageAttributeAnalyzer.AvoidSuppressMessageAttributeWhitelist, allowedMethodName) };
		}
		
		[TestMethod]
		public void AvoidSuppressMessageNotRaisedInGeneratedCode()
		{
			string baseline = @"
#pragma checksum ""..\..\BedPosOverlayWindow.xaml"" ""{ ff1816ec - aa5e - 4d10 - 87f7 - 6f4963833460}"" ""B42AD704B6EC2B9E4AC053991400023FA2213654""
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

			using System;
			using System.Diagnostics;
			using System.Windows;
			using System.Windows.Automation;
			using System.Windows.Controls;
			using System.Windows.Controls.Primitives;
			using System.Windows.Data;
			using System.Windows.Documents;
			using System.Windows.Forms.Integration;
			using System.Windows.Ink;
			using System.Windows.Input;
			using System.Windows.Markup;
			using System.Windows.Media;
			using System.Windows.Media.Animation;
			using System.Windows.Media.Effects;
			using System.Windows.Media.Imaging;
			using System.Windows.Media.Media3D;
			using System.Windows.Media.TextFormatting;
			using System.Windows.Navigation;
			using System.Windows.Shapes;
			using System.Windows.Shell;
namespace WpfApp1 {
	
	
	/// <summary>
	/// WpfOverlayWindow
	/// </summary>
	public partial class WpfOverlayWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector, System.Windows.Markup.IStyleConnector {

		#line 4 ""..\..\BedPosOverlayWindow.xaml""
		[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute(""Microsoft.Performance"", ""CA1823:AvoidUnusedPrivateFields"")]

		internal WpfApp1.WpfOverlayWindow bedLocationWindow;
	}
}
			";

			VerifyCSharpDiagnostic(baseline, "BedPosOverlayWindow.g.i");
		}

		[DataRow("NotWhitelistedFunction", "using System.Diagnostics.CodeAnalysis;", "SuppressMessage")]
		[DataRow("NotWhitelistedFunction", "", "System.Diagnostics.CodeAnalysis.SuppressMessage")]
		[DataRow("NotWhitelistedFunction", "using SM = System.Diagnostics.CodeAnalysis;", "SM.SuppressMessage")]
		[DataRow("WhitelistedFunction", "using System.Diagnostics.CodeAnalysis;", "SuppressMessage")]
		[DataRow("WhitelistedFunction", "", "System.Diagnostics.CodeAnalysis.SuppressMessage")]
		[DataRow("WhitelistedFunction", "using SM = System.Diagnostics.CodeAnalysis;", "SM.SuppressMessage")]
		[DataTestMethod]
		public void AvoidSupressMessageRaisedInUserCode(string functionName, string usingStatement, string attribute)
		{
			string baseline = @"
				using Microsoft.VisualStudio.TestTools.UnitTesting;
				{0}
				class Foo 
				{{
				  [{1}(""Microsoft.Performance"", ""CA1801: ReviewUnusedParameters"", MessageId = ""isChecked"")]
				  public void {2}()
				  {{
				  }}
				}}
				";
			string givenText = string.Format(baseline, usingStatement, attribute, functionName);
			VerifyCSharpDiagnostic(givenText, DiagnosticResultHelper.Create(DiagnosticIds.AvoidSuppressMessage));
		}
		
		protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
		{
			return new AvoidSuppressMessageAttributeAnalyzer();
		}
	}
}