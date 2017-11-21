//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.ComponentModel;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;


	/// <summary>
	/// Interaction logic for HotKeyEditor.xaml
	/// </summary>

	internal partial class HotKeyEditor : Window, IDisposable
	{

		private KeyManager manager;
		private KeyTrapper trapper;
		private HotKeyCollection map;
		private bool isDisposed;


		//========================================================================================
		// Constructors
		//========================================================================================

		/// <summary>
		/// Default constructor.  Needed for VS Designer and Blend.
		/// </summary>

		public HotKeyEditor ()
		{
			this.InitializeComponent();
		}


		/// <summary>
		/// Preferred constructor, initializes a new instance using the given key manager.
		/// </summary>
		/// <param name="manager">The KeyManager to edit.</param>

		public HotKeyEditor (KeyManager manager)
			: this()
		{
			this.manager = manager;
			this.manager.IsEnabled = false;
			this.map = this.manager.KeyMap;

			this.trapper = new KeyTrapper();
			this.trapper.KeyPressed += new HotKeyHandler(DoKeyPressed);

			this.editor.DataContext = this;

			this.editor.SelectedIndex = 0;
		}


		/// <summary>
		/// 
		/// </summary>

		public void Dispose ()
		{
			if (!isDisposed)
			{
				manager = null;

				trapper.Dispose();
				trapper = null;

				isDisposed = true;

				GC.SuppressFinalize(this);
			}
		}


		protected override void OnActivated (EventArgs e)
		{
			editor.Focus();
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// Provides a binding point for the editor.
		/// </summary>

		public HotKeyCollection KeyMap
		{
			get { return map; }
		}


		//========================================================================================
		// Methods
		//========================================================================================

		private void DoOK (object sender, RoutedEventArgs e)
		{
			manager.IsEnabled = true;
			manager.Update(map);
			manager.Save();

			map = manager.KeyMap;

			Close();
		}


		private void DoCancel (object sender, RoutedEventArgs e)
		{
			manager.IsEnabled = true;
			map = manager.KeyMap;
		}


		private void DoKeyPressed (HotKey key)
		{
			HotKey entry = editor.SelectedItem as HotKey;
			if (entry != null)
			{
				map.SetSequence(entry, key);
			}

			// resize the columns to fix any changes in content width
			GridView grid = editor.View as GridView;
			if (grid != null)
			{
				foreach (GridViewColumn column in grid.Columns)
				{
					column.Width = column.ActualWidth;
					column.Width = Double.NaN;
				}
			}
		}
	}
}