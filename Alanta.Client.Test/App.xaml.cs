using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using Alanta.Client.Common.Logging;
using Alanta.Client.Data;
using Alanta.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Alanta.Client.Test
{
	public partial class App : Alanta.Client.UI.Desktop.App
	{
		public App()
		{
			Startup += Application_Startup;
			Exit += Application_Exit;
			UnhandledException += Application_UnhandledException;
			InitializeComponent();
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			try
			{
				TestGlobals.InitialPage = DataGlobals.GetStringConfigValue(e.InitParams, Constants.InitialPageReference, "");
				Page page = null;
				if (!string.IsNullOrEmpty(TestGlobals.InitialPage))
				{
					var assembly = Assembly.GetExecutingAssembly();
					page = (Page)assembly.CreateInstance(TestGlobals.InitialPage);
				}
				if (page == null)
				{
					page = new MainPage();
				}
				RootVisual = page;
				DataGlobals.LoadGlobalParams(e.InitParams);
				Uri uri = HtmlPage.Document.DocumentUri;
				DataGlobals.BaseServiceUri = new Uri(uri.Scheme + "://" + uri.Host + ":" + uri.Port + "/");
				DataGlobals.MediaServerHost = uri.Host;
			}
			catch (Exception ex)
			{
				ClientLogger.Debug(ex.ToString());
			}
		}

		private static void Application_Exit(object sender, EventArgs e)
		{

		}

		private static void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is AssertFailedException)
			{
				return;
			}
			ClientLogger.Debug("Error: Unhandled exception: " + e.ExceptionObject);
			e.Handled = true;
			// If the app is running outside of the debugger then report the exception using
			// the browser's exception mechanism. On IE this will display it a yellow alert 
			// icon in the status bar and Firefox will display a script error.
			//if (!System.Diagnostics.Debugger.IsAttached)
			//{

			//    // NOTE: This will allow the application to continue running after an exception has been thrown
			//    // but not handled. 
			//    // For production applications this error handling should be replaced with something that will 
			//    // report the error to the website and stop the application.
			//    e.Handled = true;
			//    Deployment.Current.Dispatcher.BeginInvoke(() => ReportErrorToDOM(e));
			//}
		}
		private static void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
		{
			try
			{
				string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
				errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");
				HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application " + errorMsg + "\");");
			}
			catch (Exception ex)
			{
				// Suppress any errors.
				Debug.WriteLine(ex.ToString());
			}
		}
	}
}
