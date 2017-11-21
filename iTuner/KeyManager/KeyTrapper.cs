//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;


	/// <summary>
	/// Specialized key hook for the HotKeyEditor window.  Filters special system keys, such
	/// as the arrow keys and Escape.
	/// </summary>
	/// <remarks>
	/// Be sure to call Dispose() when done with the instance.
	/// </remarks>

	internal class KeyTrapper : IDisposable
	{

		[StructLayout(LayoutKind.Sequential)]
		private struct KBHookStruct
		{
			public int vkCode;
			public int scanCode;
			public int flags;
			public int time;
			public int dwExtraInfo;
		}


		private delegate IntPtr HookHandler (int nCode, IntPtr wParam, ref KBHookStruct lParam);

		private const int WH_KEYBOARD_LL = 13;
		private const int WM_KEYDOWN = 0x100;
		private const int WM_KEYUP = 0x101;
		private const int WM_SYSKEYDOWN = 0x104;
		private const int WM_SYSKEYUP = 0x105;
		private const int KEY_Fn = 255;
		private const int SCAN_Fn = 99;

		private HookHandler hook;
		private IntPtr hookPtr = IntPtr.Zero;
		private KeyModifier mods = KeyModifier.None;
		private bool isDisposed;						// true if disposed


		#region Interop

		/// <summary>
		/// The CallNextHookEx function passes the hook information to the next hook procedure
		/// in the current hook chain. A hook procedure can call this function either before or
		/// after processing the hook information. 
		/// </summary>
		/// <param name="hhk">Ignored</param>
		/// <param name="nCode">
		/// Specifies the hook code passed to the current hook procedure. The next hook procedure
		/// uses this code to determine how to process the hook information.
		/// </param>
		/// <param name="wParam">
		/// Specifies the wParam value passed to the current hook procedure. The meaning of this
		/// parameter depends on the type of hook associated with the current hook chain.
		/// </param>
		/// <param name="lParam">
		/// Specifies the lParam value passed to the current hook procedure. The meaning of
		/// this parameter depends on the type of hook associated with the current hook chain
		/// </param>
		/// <returns>
		/// This value is returned by the next hook procedure in the chain. The current hook
		/// procedure must also return this value. The meaning of the return value depends on
		/// the hook type. For more information, see the descriptions of the individual hook
		/// procedures.
		/// </returns>

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr CallNextHookEx (
			IntPtr hhk, int nCode, IntPtr wParam, ref KBHookStruct lParam);


		/// <summary>
		/// The SetWindowsHookEx function installs an application-defined hook procedure into
		/// a hook chain. You would install a hook procedure to monitor the system for certain
		/// types of events. These events are associated either with a specific thread or with
		/// all threads in the same desktop as the calling thread. 
		/// </summary>
		/// <param name="idHook">
		///  Specifies the type of hook procedure to be installed. This parameter can be one of
		///  the following values (WH_KEYBOARD_LL)
		/// </param>
		/// <param name="hook">
		/// Pointer to the hook procedure. If the dwThreadId parameter is zero or specifies the
		/// identifier of a thread created by a different process, the lpfn parameter must point
		/// to a hook procedure in a DLL. Otherwise, lpfn can point to a hook procedure in the
		/// code associated with the current process. 
		/// </param>
		/// <param name="hInstance">
		/// Handle to the DLL containing the hook procedure pointed to by the lpfn parameter.
		/// The hMod parameter must be set to NULL if the dwThreadId parameter specifies a
		/// thread created by the current process and if the hook procedure is within the code
		/// associated with the current process. 
		/// </param>
		/// <param name="dwThreadId">
		/// Specifies the identifier of the thread with which the hook procedure is to be
		/// associated. If this parameter is zero, the hook procedure is associated with all
		/// existing threads running in the same desktop as the calling thread. 
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is the handle to the hook procedure. 
		/// </returns>

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx (
			int idHook, HookHandler hook, IntPtr hInstance, uint dwThreadId);


		/// <summary>
		/// The UnhookWindowsHookEx function removes a hook procedure installed in a hook
		/// chain by the SetWindowsHookEx function. 
		/// </summary>
		/// <param name="hhk">
		/// Handle to the hook to be removed. This parameter is a hook handle obtained by a
		/// previous call to SetWindowsHookEx. 
		/// </param>
		/// <returns>
		/// If the function succeeds, the return value is nonzero.
		/// </returns>

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool UnhookWindowsHookEx (IntPtr hhk);

		#endregion Interop


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// 
		/// </summary>

		public KeyTrapper ()
		{
			this.hook = new HookHandler(Hooker);

			// Note: This does not work in the VS host environment.  To run in debug mode:
			// Project -> Properties -> Debug -> Uncheck "Enable the Visual Studio hosting process"
			IntPtr hInstance = Marshal.GetHINSTANCE(System.Windows.Application.Current.GetType().Module);
			this.hookPtr = SetWindowsHookEx(WH_KEYBOARD_LL, hook, hInstance, 0);
		}


		/// <summary>
		/// Destructor.
		/// </summary>

		~KeyTrapper ()
		{
			Dispose(false);
		}


		/// <summary>
		/// 
		/// </summary>

		public void Dispose ()
		{
			if (!isDisposed)
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>

		protected virtual void Dispose (bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
					if (hookPtr != IntPtr.Zero)
					{
						bool ok = UnhookWindowsHookEx(hookPtr);
						hookPtr = IntPtr.Zero;
					}
				}

				isDisposed = true;
			}
		}


		//========================================================================================
		// Implementation
		//========================================================================================

		/// <summary>
		/// This event fires when a registered hot key sequence is pressed.
		/// </summary>

		public event HotKeyHandler KeyPressed;


		/// <summary>
		/// Interprets the current state of the keyboard and fires the KeyPressed event
		/// when a full key sequence is recognized.  Since the low level keyboard hook
		/// only notifies us of single key presses, we need to keep track of the state
		/// of the modifier keys: Alt, Ctrl, Shift, and Win, enabling them on key down
		/// and disabling them on key up.  Then when another key is pressed, we can
		/// combine the known function key state with the new key press.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="msg"></param>
		/// <param name="data"></param>
		/// <returns></returns>

		private IntPtr Hooker (int code, IntPtr msg, ref KBHookStruct data)
		{
			if (code != 0)
			{
				// allow next registered hook to handle message
				return CallNextHookEx(hookPtr, code, msg, ref data);
			}

			int msgID = msg.ToInt32();

			if ((msgID == WM_KEYDOWN) || (msgID == WM_SYSKEYDOWN))
			{
				Keys kCode = (Keys)data.vkCode;
				switch (kCode)
				{
					case Keys.LMenu:
					case Keys.RMenu:
						mods |= KeyModifier.Alt;
						break;

					case Keys.LControlKey:
					case Keys.RControlKey:
						mods |= KeyModifier.Ctrl;
						break;

					case Keys.LShiftKey:
					case Keys.RShiftKey:
						mods |= KeyModifier.Shift;
						break;

					case Keys.LWin:
					case Keys.RWin:
						mods |= KeyModifier.Win;
						break;

					default:
						// ignore the Fn key, least on my Lenovo T400 laptop
						if ((data.vkCode == KEY_Fn) && (data.scanCode == SCAN_Fn))
							break;

						// ignore special keys needed for basic minimal functionality
						if (mods == KeyModifier.None)
						{
							bool keeper = false;
							switch (kCode)
							{
								// we do not trap Keys.Back here because we want to let the
								// editor use that to clear out Action line

								case Keys.CapsLock:
								case Keys.Down:
								case Keys.Enter:
								case Keys.Escape:
								case Keys.Left:
								case Keys.Right:
								case Keys.Up:
									keeper = true;
									break;
							}

							if (keeper)
							{
								break;
							}
						}

						//Logger.WriteLine(String.Format(
						//    "... KeyTrapper code {0}, mods {1}", data.vkCode, mods));

						if (KeyPressed != null)
						{
							KeyPressed(new HotKey(HotKeyAction.None, kCode, mods));
						}

						// return without passing on message to next registered hook
						return new IntPtr(1);
				}
			}
			else if ((msgID == WM_KEYUP) || (msgID == WM_SYSKEYUP))
			{
				switch ((Keys)data.vkCode)
				{
					case Keys.LMenu:
					case Keys.RMenu:
						mods &= ~KeyModifier.Alt;
						break;

					case Keys.LControlKey:
					case Keys.RControlKey:
						mods &= ~KeyModifier.Ctrl;
						break;

					case Keys.LShiftKey:
					case Keys.RShiftKey:
						mods &= ~KeyModifier.Shift;
						break;

					case Keys.LWin:
					case Keys.RWin:
						mods &= ~KeyModifier.Win;
						break;
				}
			}

			// allow next registered hook to handle message
			return CallNextHookEx(hookPtr, code, msg, ref data);
		}
	}
}
