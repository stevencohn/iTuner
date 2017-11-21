//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Input;
	using iTuner.iTunes;
	using Resx = Properties.Resources;
	using WinForms = System.Windows.Forms;


	/// <summary>
	/// Interaction logic for AppWindow.xaml
	/// </summary>

	internal partial class AppWindow : FadingWindow, IDisposable
	{
		private class MenuMap : Dictionary<string, IconMenuItem> { }

		private WinForms.NotifyIcon trayIcon;
		private WinForms.Timer trayIconTimer;
		private int trayIconClickCount;

		private AboutBox aboutBox;
		private TrackerWindow tracker;
		private Controller controller;
		private Librarian librarian;
		private KeyManager manager;
		private SplashWindow splash;
		private bool isDisposed;
		private object syncdis;

		private ExportDialog exportDialog;
		private ImportDialog importDialog;
		private SynchronizeDialog syncDialog;


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// Initialize a new instance including the internal iTunes controller and
		/// hot key manager.
		/// </summary>

		public AppWindow ()
		{
			InitializeComponent();

			// since this window instantiates other windows, we must set the shutdown mode
			// otherwise the application will hang and not shutdown properly
			Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

			// FadingWindow initialization
			mainBorder.Opacity = 0.0;
			AnimatedElement = mainBorder;
			Visibility = Visibility.Hidden;

			if (DesignerProperties.GetIsInDesignMode(this))
			{
				// if in VS or Blend designers do not start loading window or iTunes
				return;
			}

			trayIcon = new WinForms.NotifyIcon()
			{
				Text = Resx.I_ApplicationTitle,
				ContextMenu = CreateContextMenu(),
				Visible = true
			};

			trayIcon.MouseDown += DoTrayIconMouseDown;

			if (Controller.IsHostRunning)
			{
				// since iTunes is running, we can instantiate its controller very quickly
				// without blocking the UI thread, so do it explicitly here

				DoLoadingWork(null, null);
				DoLoadingCompleted(null, null);
			}
			else
			{
				// by instantiating iTunes in a background thread, we are able to show animated
				// progress in SplashWindow; otherwise, creating a new Controller blocks
				// the UI thread

				splash = new SplashWindow();
				splash.Show();

				BackgroundWorker worker = new BackgroundWorker();
				worker.DoWork += DoLoadingWork;
				worker.RunWorkerCompleted += DoLoadingCompleted;
				worker.RunWorkerAsync();
			}
		}


		#region Lifecycle

		/// <summary>
		/// If iTunes is not currently running then we start it in a background thread
		/// so we don't block the UI thread.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoLoadingWork (object sender, DoWorkEventArgs e)
		{
			try
			{
				controller = new Controller();
				librarian = controller.Librarian;
			}
			catch (IncompatibleException exc)
			{
				MessageBox.Show(exc.Message, "Error starting iTuner",
					MessageBoxButton.OK, MessageBoxImage.Error);

				controller = null;
				throw;
			}
		}


		/// <summary>
		/// Now that iTunes is started, we can continue initializing the iTuner UI
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoLoadingCompleted (object sender, RunWorkerCompletedEventArgs e)
		{
			if (controller == null)
			{
				Close();
				return;
			}

			controller.LyricsProgressReport += DoLyricsProgressReport;
			controller.LyricsUpdated += DoLyricsUpdated;
			controller.Quiting += DoQuiting;
			controller.TrackPlaying += DoTrackPlaying;
			controller.TrackStopped += DoTrackStopped;

			librarian.CollectionChanged += DoTaskCollectionChanged;

			if (splash != null)
			{
				splash.Hide();
				splash = null;
			}

			DataContext = controller;

			manager = new KeyManager();
			manager.KeyPressed += ExecuteHotKeyAction;

			trayIconTimer = new WinForms.Timer
			{
				Interval = WinForms.SystemInformation.DoubleClickTime
			};

			trayIconTimer.Tick += DoTrayIconTick;
			trayIconClickCount = 0;

			tracker = new TrackerWindow(controller);

			EventManager.RegisterClassHandler(
			typeof(EditBlock), EditBlock.BeginEditEvent,
			new RoutedEventHandler(DoBeginEdit));

			EventManager.RegisterClassHandler(
				typeof(EditBlock), EditBlock.CompleteEditEvent,
				new RoutedEventHandler(DoCompleteEdit));

			isDisposed = false;
			syncdis = new object();

			SetNotifyIcon(controller.CurrentTrack);
		}


		/// <summary>
		/// Dispose of all resources in an orderly fashion.
		/// </summary>

		public override void Dispose ()
		{
			if (aboutBox != null)
			{
				aboutBox.Dispose();
				aboutBox = null;
			}

			if (tracker != null)
			{
				tracker.Dispose();
				tracker = null;
			}

			if (librarian != null)
			{
				librarian.CollectionChanged -= DoTaskCollectionChanged;

				librarian.Dispose();
				librarian = null;
			}

			if (controller != null)
			{
				controller.LyricsProgressReport -= DoLyricsProgressReport;
				controller.LyricsUpdated -= DoLyricsUpdated;
				controller.Quiting -= DoQuiting;
				controller.TrackPlaying -= DoTrackPlaying;
				controller.TrackStopped -= DoTrackStopped;

				// if running from IDE force iTunes to shutdown, force COM detach. Poking around
				// in the debugger tends to destabalize the COM interface so we shut it down when
				// invoked from the Debugger to avoid any confusion.
				controller.Dispose(System.Diagnostics.Debugger.IsAttached);

				controller = null;
			}

			if (manager != null)
			{
				manager.KeyPressed -= ExecuteHotKeyAction;
				manager.Dispose();
				manager = null;
			}

			if (trayIcon != null)
			{
				trayIcon.ContextMenu.Popup -= SetMenuItemStates;
				trayIcon.ContextMenu.Dispose();
				trayIcon.ContextMenu = null;

				trayIcon.MouseDown -= DoTrayIconMouseDown;
				trayIcon.Dispose();
				trayIcon = null;
			}

			base.Dispose();
			isDisposed = true;
		}

		#endregion Lifecycle

		#region NotifyIcon management

		private void DoTrayIconTick (object sender, EventArgs e)
		{
			trayIconTimer.Stop();
			trayIconClickCount = 0;

			// perform single-click action
			ShowAppWindow();
		}

		private void DoTrayIconMouseDown (object sender, WinForms.MouseEventArgs e)
		{
			if (e.Button == WinForms.MouseButtons.Left)
			{
				trayIconClickCount++;

				if (trayIconClickCount > 1)
				{
					trayIconTimer.Stop();
					trayIconClickCount = 0;

					// perform double-click action
					DoPlayPause(sender, e);
				}
				else
				{
					trayIconTimer.Start();
				}
			}
		}

		#endregion NotifyIcon management

		#region Context menu management

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>

		private WinForms.ContextMenu CreateContextMenu ()
		{
			WinForms.ContextMenu menu = new WinForms.ContextMenu
			{
				Tag = new MenuMap()
			};

			menu.MenuItems.Add(new IconMenuItem(
				Resx.iTunes16, Resx.ActionShowiTunes, DoShowiTunes));

			menu.MenuItems.Add(new IconMenuItem(
				Resx.iTuner16, Resx.ActionShowiTuner, DoShowiTuner));

			menu.MenuItems.Add(new IconMenuItem("-"));

			menu.MenuItems.Add(new IconMenuItem(
				Resx.Library, Resx.MenuClean, new[]
				{
					MakeMenuItem("cleanthis", menu.Tag,
						Resx.Library, Resx.MenuCleanAlbum, DoCleanContext),

						new IconMenuItem("-"),

					MakeMenuItem("scandead", menu.Tag,
						Resx.Phantom, Resx.MenuCleanPhantoms, DoPhantomScanner),

					MakeMenuItem("scandup", menu.Tag,
						Resx.Duplicates, Resx.MenuCleanDuplicates, DoDuplicateScanner),

					MakeMenuItem("scanempty", menu.Tag,
						Resx.EmptyDirectory, Resx.MenuCleanEmpty, DoEmptyScanner)
				}));

			menu.MenuItems.Add(new IconMenuItem(
				Resx.ID3, Resx.MenuFix, new[]
				{
					MakeMenuItem("fixtrack", menu.Tag,
						Resx.Track, Resx.MenuFixTrack, DoFixTrack),

						MakeMenuItem("fixalbum", menu.Tag,
						Resx.Album, Resx.MenuFixAlbum, DoFixAlbum),
				}));

			menu.MenuItems.Add(new IconMenuItem(
				Resx.Export, Resx.MenuExport, new[]
				{
					MakeMenuItem("exalbum", menu.Tag,
						Resx.Album, Resx.MenuExportAlbum, DoExportAlbum),

					MakeMenuItem("exartist", menu.Tag,
						Resx.Artist, Resx.MenuExportArtist, DoExportArtist),

					MakeMenuItem("explaylist", menu.Tag,
						Resx.Playlist, Resx.MenuExportPlaylist, DoExportPlaylist)
				}));

			menu.MenuItems.Add(MakeMenuItem("import", menu.Tag,
				Resx.Import, Resx.MenuImport, DoImportPlaylist));

			menu.MenuItems.Add(MakeMenuItem("sync", menu.Tag,
				Resx.Sync, Resx.MenuSync, DoSync));

			menu.MenuItems.Add(new IconMenuItem("-"));

			menu.MenuItems.Add(MakeMenuItem("mute", menu.Tag,
				Resx.Mute, Resx.ActionMute, DoToggleMute));

			menu.MenuItems.Add(new IconMenuItem(
				Resx.PrevTrack, Resx.MenuPrevTrack, DoPrevTrack));

			menu.MenuItems.Add(new IconMenuItem(
				Resx.NextTrack, Resx.MenuNextTrack, DoNextTrack));

			menu.MenuItems.Add(MakeMenuItem("play", menu.Tag,
				Resx.Play, Resx.ActionPlayPause, DoPlayPause));

			menu.MenuItems.Add(MakeMenuItem("shuffle", menu.Tag,
				Resx.Shuffle, Resx.MenuShuffle, DoToggleShuffle));

			menu.MenuItems.Add(new IconMenuItem("-"));
			menu.MenuItems.Add(new IconMenuItem(Resx.MenuAbout, DoAbout));
			menu.MenuItems.Add(new IconMenuItem(Resx.MenuOptions, DoOptions));
			menu.MenuItems.Add(new IconMenuItem(Resx.MenuExitiTunes, DoQuitingAll));
			menu.MenuItems.Add(new IconMenuItem(Resx.MenuExit, DoQuiting));


			menu.Popup += SetMenuItemStates;
			return menu;
		}


		/// <summary>
		/// Construct a new IconMenuItem and add it to the MenuMap attached to the Context
		/// Menu.  This menu map provides a quick and easy way to lookup and configure
		/// menu items in SetMenuitemStates. 
		/// </summary>
		/// <param name="key"></param>
		/// <param name="tag"></param>
		/// <param name="icon"></param>
		/// <param name="text"></param>
		/// <param name="handler"></param>
		/// <returns></returns>

		private IconMenuItem MakeMenuItem (
			string key, object tag, System.Drawing.Icon icon, string text, EventHandler handler)
		{
			var item = new IconMenuItem(icon, text, handler)
			{
				Name = key
			};

			((MenuMap)tag).Add(key, item);

			return item;
		}


		/// <summary>
		/// Set the menu item states just prior to displaying the context menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void SetMenuItemStates (object sender, EventArgs e)
		{
			WinForms.ContextMenu menu = sender as WinForms.ContextMenu;
			if (menu == null) return;

			MenuMap map = (MenuMap)menu.Tag;

			// main context menu items
			map["mute"].Checked = controller.IsMuted;
			map["shuffle"].Checked = controller.Shuffle;
			map["play"].Icon = (controller.IsPlaying ? Resx.Pause : Resx.Play);

			// library menu items

			if (librarian.IsActive(Resx.I_ScanContextDuplicates) ||
				librarian.IsActive(Resx.I_ScanContextPhantoms) ||
				(controller.CurrentContext == Controller.PlayerContext.None))
			{
				map["cleanthis"].Enabled = false;
			}
			else
			{
				map["cleanthis"].Enabled = true;

				map["cleanthis"].Text = controller.IsMusicalPlaylist
					? Resx.MenuCleanAlbum
					: Resx.MenuCleanPlaylist;
			}

			map["scandead"].Enabled = !librarian.IsActive(Resx.I_ScanPhantoms);
			map["scandup"].Enabled = !librarian.IsActive(Resx.I_ScanDuplicates);
			map["scanempty"].Enabled = !librarian.IsActive(Resx.I_ScanEmptyDirectories);

			var isMusic = (controller != null)
						   && controller.IsMusicalPlaylist
						   && (controller.CurrentTrack != null)
						   && controller.Librarian?.Catalog != null;

			var hasPlaylist = (controller.CurrentPlaylist != null);
			var noDialogs = (syncDialog == null) && (exportDialog == null);
			var notXporting = !librarian.IsActive(Resx.I_ScanExport) && !librarian.IsActive(Resx.I_ScanImportPlaylist);

			// fix menu items

			map["fixalbum"].Enabled = isMusic;
			map["fixtrack"].Enabled = isMusic;

			// export menu items

			map["exalbum"].Enabled = isMusic && noDialogs && notXporting;
			map["exartist"].Enabled = isMusic && noDialogs && notXporting;
			map["explaylist"].Enabled = isMusic && hasPlaylist && noDialogs && notXporting;

			// synchronize item

			map["sync"].Enabled = isMusic && noDialogs;
		}

		#endregion Context menu management


		//========================================================================================
		// Handlers
		//========================================================================================

		#region Commands and Handlers

		private void ExecuteHotKeyAction (IHotKey key)
		{
			switch (key.Action)
			{
				case HotKeyAction.PlayPause:
					DoPlayPause(null, null);
					break;

				case HotKeyAction.NextTrack:
					DoNextTrack(null, null);
					break;

				case HotKeyAction.PrevTrack:
					DoPrevTrack(null, null);
					break;

				case HotKeyAction.VolumeUp:
					DoVolumeUp(null, null);
					break;

				case HotKeyAction.VolumeDown:
					DoVolumeDown(null, null);
					break;

				case HotKeyAction.Mute:
					DoToggleMute(null, null);
					break;

				case HotKeyAction.ShowiTunes:
					DoShowiTunes(null, null);
					break;

				case HotKeyAction.ShowiTuner:
					DoShowiTuner(null, null);
					break;

				case HotKeyAction.ShowLyrics:
					DoShowLyrics(null, null);
					break;
			}
		}


		private void DoAbout (object sender, EventArgs e)
		{
			if (IsOpaque && !IsPinned)
			{
				Hide();
			}

			if (aboutBox == null)
			{
				aboutBox = new AboutBox();
			}

			var taskbar = new Taskbar();
			var point = taskbar.GetTangentPosition(trayIcon);
			aboutBox.SetPositionRelativeTo(point, taskbar.Edge);
			aboutBox.Show();
		}


		/// <summary>
		/// Handler for the EditBlock BeginEdit event, pins the current window so it
		/// cannot fade out while editing.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoBeginEdit (object sender, RoutedEventArgs e)
		{
			IsPinned = true;
		}


		/// <summary>
		/// Handler for the EditBlock CompleteEdit event, unpins the current window so
		/// it can fade out.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoCompleteEdit (object sender, RoutedEventArgs e)
		{
			IsPinned = false;
		}


		/// <summary>
		/// Close the window when the Esc key is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoKeyDown (object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Hide();
			}
		}


		private void DoLyricsProgressReport (ISong song, int stage)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.BeginInvoke((Action)delegate
				{
					DoLyricsProgressReport(song, stage);
				});

				return;
			}

			ITrack track = controller.CurrentTrack;
			if (track?.GetHashCode() == song.GetHashCode())
			{
				if ((stage >= 0) && (stage <= 5))
				{
					taskPanel.LyricsState = stage.ToString();
				}
				else
				{
					taskPanel.LyricsState = string.Empty;
				}
			}
		}


		private void DoLyricsUpdated (ITrack song)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.BeginInvoke((Action)delegate
				{
					DoLyricsUpdated(song);
				});

				return;
			}

			ITrack track = controller.CurrentTrack;
			if (track != null)
			{
				bool hasLyrics = !string.IsNullOrEmpty(track.Lyrics);
				taskPanel.LyricsState = (hasLyrics ? string.Empty : "0");
			}
			else
			{
				taskPanel.LyricsState = "0";
			}
		}


		private void DoOptions (object sender, EventArgs e)
		{
			OptionsDialog dialog = new OptionsDialog();
			dialog.ShowDialog();
		}


		private void DoPlayPause (object sender, EventArgs e)
		{
			controller.TogglePlayPause();
		}


		private void DoNextTrack (object sender, EventArgs e)
		{
			controller.NextTrack();
		}


		private void DoPrevTrack (object sender, EventArgs e)
		{
			controller.PreviousTrack();
		}


		private void DoShowiTunes (object sender, EventArgs e)
		{
			controller.ShowiTunes();
		}


		private void DoShowiTuner (object sender, EventArgs e)
		{
			ShowAppWindow();
		}


		private void DoShowLyrics (object sender, RoutedEventArgs e)
		{
			ITrack track = controller.CurrentTrack;
			var lyrics = track?.Lyrics;
			if (string.IsNullOrEmpty(lyrics)) return;
			var path = System.IO.Path.GetTempFileName();
			System.IO.File.WriteAllText(path, track.MakeLyricReportHeader() + lyrics);
			System.Diagnostics.Process.Start("Notepad.exe", path);
		}


		private void DoToggleMute (object sender, EventArgs e)
		{
			controller.ToggleMute();
			controlPanel.IsMuted = controller.IsMuted;
		}


		private void DoToggleShuffle (object sender, EventArgs e)
		{
			Logger.WriteLine(Logger.Level.Debug, "App", "DoToggleShuffle");
			controller.Shuffle = !controller.Shuffle;
		}


		private void DoTrackPlaying (ITrack track)
		{
			if (!tracker.Dispatcher.CheckAccess())
			{
				//Logger.WriteLine(Logger.Level.Debug, "DEBUG", "DoTrackPlaying.BeginInvoke");
				tracker.Dispatcher.BeginInvoke((Action)delegate
				{
					DoTrackPlaying(track);
				});

				return;
			}

			//Logger.WriteLine(Logger.Level.Debug, "DEBUG", "DoTrackPlaying");

			// TODO: why do we need to do this?  the 'track' parameter is not alway populated
			// for some reason.  Is this when we BeginInvoke?  Why?
			track = controller.CurrentTrack;

			//Logger.WriteLine(Logger.Level.Debug, "DEBUG", String.Format("track is null? ({0})", track == null ? "Yes" : "No"));

			taskPanel.LyricsState = string.IsNullOrEmpty(track?.Lyrics) ? "0" : string.Empty;

			SetNotifyIcon(track);

			// if already showing main app window, don't need to show tracker
			if (IsOpaque)
			{
				return;
			}

			if (Properties.Settings.Default.TrackerEnabled)
			{
				var taskbar = new Taskbar();
				Point point = taskbar.GetTangentPosition(trayIcon);
				tracker.SetPositionRelativeTo(point, taskbar.Edge);
				tracker.ShowActivated = false;
				tracker.Show();
			}
		}


		private void DoTrackStopped (ITrack track)
		{
			SetNotifyIcon(track);
		}


		private void DoVolumeUp (object sender, EventArgs e)
		{
			controller.VolumeUp();
		}


		private void DoVolumeDown (object sender, EventArgs e)
		{
			controller.VolumeDown();
		}


		private void DoQuiting (object sender, EventArgs e)
		{
			lock (syncdis)
			{
				if (!isDisposed)
				{
					Logger.WriteLine(Resx.I_ApplicationProduct, "Quiting");

					trayIcon.Visible = false;
					Close();
					Dispose();
				}
			}
		}


		private void DoQuitingAll (object sender, EventArgs e)
		{
			controller.Dispose(true);
			DoQuiting(sender, e);
		}

		#endregion Commands and Handlers

		#region Export Handlers

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoExportAlbum (object sender, EventArgs e)
		{
			string album = controller.CurrentTrack.Album;
			string artist = controller.CurrentTrack.Artist;

			PersistentIDCollection list = librarian.Catalog.FindTracksByAlbum(album, artist);
			list.Album = album;
			list.Artist = artist;
			list.Name = album;

			DoExport(list);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoExportArtist (object sender, EventArgs e)
		{
			string artist = controller.CurrentTrack.Artist;

			PersistentIDCollection list = librarian.Catalog.FindTracksByArtist(artist);
			list.Artist = artist;
			list.Name = artist;

			DoExport(list);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoExportPlaylist (object sender, EventArgs e)
		{
			using (var playlist = controller.CurrentPlaylist)
			{
				PersistentIDCollection list =
					librarian.Catalog.FindTracksByPlaylist(playlist.PersistentID);

				list.Name = controller.CurrentPlaylist.Name;

				DoExport(list);

				list.Clear();
			}
		}


		private void DoExport (PersistentIDCollection list)
		{
			if (IsOpaque && !IsPinned)
			{
				Hide();
			}

			try
			{
				exportDialog = new ExportDialog(controller, list);
				exportDialog.ShowDialog();
			}
			finally
			{
				exportDialog = null;
			}
		}


		private void DoImportPlaylist (object sender, EventArgs e)
		{
			if (IsOpaque && !IsPinned)
			{
				Hide();
			}

			try
			{
				importDialog = new ImportDialog(controller);
				importDialog.ShowDialog();
			}
			finally
			{
				exportDialog = null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoSync (object sender, EventArgs e)
		{
			if (IsOpaque && !IsPinned)
			{
				Hide();
			}

			syncDialog = new SynchronizeDialog(controller);
			syncDialog.ShowDialog();

			syncDialog = null;
		}

		#endregion Export Handlers

		#region Scanner Handlers

		private void DoCleanContext (object sender, EventArgs e)
		{
			librarian.CleanContext(controller.IsMusicalPlaylist);
		}


		private void DoEmptyScanner (object sender, EventArgs e)
		{
			librarian.ScanEmptyFolders();
		}


		private void DoDuplicateScanner (object sender, EventArgs e)
		{
			librarian.ScanDuplicates();
		}


		private void DoFixAlbum (object sender, EventArgs e)
		{
			string album = controller.CurrentTrack.Album;
			string artist = controller.CurrentTrack.Artist;

			PersistentIDCollection list = librarian.Catalog.FindTracksByAlbum(album, artist);
			list.Album = album;
			list.Artist = artist;
			list.Name = album;

			librarian.FixInformation(list);
		}


		private void DoFixTrack (object sender, EventArgs e)
		{
			string album = controller.CurrentTrack.Album;
			string artist = controller.CurrentTrack.Artist;

			var list = new PersistentIDCollection
			{
				controller.CurrentTrack.PersistentID
			};

			list.Album = album;
			list.Artist = artist;
			list.Name = album;

			librarian.FixInformation(list);
		}


		private void DoPhantomScanner (object sender, EventArgs e)
		{
			librarian.ScanPhantomTracks();
		}

		#endregion Scanner Handlers


		//========================================================================================
		// Methods
		//========================================================================================

		#region Window Control Methods

		/// <summary>
		/// Ensures that the play panel is displayed the next time the window is shown.
		/// </summary>

		protected override void OnHideCompleted ()
		{
			trackPanel.ResetView();
			taskPanel.ResetView();
		}


		private void ShowAppWindow ()
		{
			if ((tracker != null) && tracker.IsOpaque)
			{
				tracker.Hide();
			}

			if ((aboutBox != null) && aboutBox.IsOpaque)
			{
				aboutBox.Hide();
			}

			// there is no event to indicate when Shuffled changes so we need to peek
			taskPanel.IsShuffled = controller.Shuffle;

			var taskbar = new Taskbar();
			Point point = taskbar.GetTangentPosition(trayIcon);
			SetPositionRelativeTo(point, taskbar.Edge);
			Show();
		}


		private void ShowKeyEditor (object sender, RoutedEventArgs e)
		{
			using (var editor = new HotKeyEditor(manager))
			{
				var taskbar = new Taskbar();
				switch (taskbar.Edge)
				{
					case ScreenEdge.Bottom:
					case ScreenEdge.Right:
						editor.Top = Top - (editor.Height * 0.50);
						editor.Left = Left - (editor.Width * 0.60);
						break;

					case ScreenEdge.Top:
						editor.Top = Top + (Height * 0.50);
						editor.Left = Left - (editor.Width * 0.80);
						break;

					case ScreenEdge.Left:
						editor.Top = Top - (editor.Height * 0.50);
						editor.Left = Left + (Width * 0.30);
						break;
				}

				IsPinned = true;
				manager.IsEnabled = false;

				editor.ShowDialog();
			}

			IsPinned = false;
			manager.IsEnabled = true;

			taskPanel.ResetEditKeys();
		}


		/// <summary>
		/// When a task is added to or removed from the task list, we want to update
		/// the notify icon to indicate whether a task is running.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoTaskCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.BeginInvoke((Action)delegate
				{
					DoTaskCollectionChanged(sender, e);
				});

				return;
			}

			SetNotifyIcon(controller.CurrentTrack);
		}


		/// <summary>
		/// 
		/// </summary>

		private void SetNotifyIcon (ITrack track)
		{
			if ((librarian == null) || (controller == null) || (trayIcon == null))
			{
				// TODO: not sure why but some have seen exceptions coming into this method from
				// DoTaskCollectionChanged so this null-check and subsequent return is meant to
				// avoid that until I figure out why it's happening on a few machines.
				return;
			}

			var isActive = librarian.ActiveCount > 0;

			if (controller.IsPlaying)
			{
				trayIcon.Icon = isActive ? Resx.PlayActive : Resx.Play;
			}
			else
			{
				trayIcon.Icon = isActive ? Resx.PauseActive : Resx.Pause;
			}

			if (track == null)
			{
				trayIcon.Text = Resx.I_ApplicationTitle;
				//Logger.WriteLine(Logger.Level.Debug, "DEBUG", "SetNotifyIcon track is null");
			}
			else
			{
				var text = $"{track.Title}\n{track.Artist}\n{track.Album}".Trim();

				//Logger.WriteLine(Logger.Level.Debug, "DEBUG", String.Format("SetNotifyIcon.Text ({0})", text));

				if (text.Length == 0)
				{
					text = Resx.I_ApplicationTitle;
				}
				else if (text.Length > 63)
				{
					// NotifyIcon.Text cannot be longer than 64 characters
					text = text.Substring(0, 60).Trim() + "...";
				}

				trayIcon.Text = text;
			}
		}

		#endregion Window Control Methods
	}
}
