//************************************************************************************************
// Copyright © 2017 Steven M. Cohn. All Rights Reserved.
//************************************************************************************************

namespace iTuner
{
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Windows;
	using Forms = System.Windows.Forms;


	//********************************************************************************************
	// NotifyIconExtensions
	//********************************************************************************************

	internal static class NotifyIconExtensions
	{

		public static Forms.Screen GetScreen (this Forms.NotifyIcon icon)
		{
			Interop.NOTIFYICONIDENTIFIER identifier;

			Forms.Screen screen = null;

			screen = TryGetNotifyIconIdentifier(icon, out identifier)
				? Forms.Screen.FromHandle(identifier.hWnd) 
				: Application.Current.MainWindow.GetScreen();

			if (screen == null)
			{
				Logger.WriteLine(Logger.Level.Debug, "NotifyIcon",
					"Cannot get screen from NotifyIcon so defaulting to PrimaryScreen");

				screen = Forms.Screen.PrimaryScreen;
			}

			return screen;
		}


		public static Rect GetRectangle (this Forms.NotifyIcon notifyIcon)
		{
			Interop.NOTIFYICONIDENTIFIER identifier;
			if (TryGetNotifyIconIdentifier(notifyIcon, out identifier))
			{
				Interop.RECT iconLocation;
				var result = Interop.Shell_NotifyIconGetRect(ref identifier, out iconLocation);

				if ((result == 0) || (result == 1))
				{
					return iconLocation;
				}
			}

			return Rect.Empty;
		}


		private static bool TryGetNotifyIconIdentifier (
			Forms.NotifyIcon notifyIcon, out Interop.NOTIFYICONIDENTIFIER identifier)
		{
			identifier = new Interop.NOTIFYICONIDENTIFIER
			{
				cbSize = (uint) Marshal.SizeOf(typeof(Interop.NOTIFYICONIDENTIFIER))
			};

			int id;
			if (!TryGetFieldValue(notifyIcon, "id", out id))
				return false;

			Forms.NativeWindow window;
			if (!TryGetFieldValue(notifyIcon, "window", out window))
				return false;

			identifier.uID = (uint) id;
			identifier.hWnd = window.Handle;
			return true;
		}


		private static bool TryGetFieldValue<T> (object instance, string fieldName, out T fieldValue)
		{
			fieldValue = default(T);

			var fieldInfo = instance.GetType()
				.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

			if (fieldInfo == null)
				return false;

			var value = fieldInfo.GetValue(instance);
			if (!(value is T))
				return false;

			fieldValue = (T) value;
			return true;
		}
	}
}