//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Data;


	/// <summary>
	/// A simple hack for allowsing the user to display a TextBlock that, with a double-click,
	/// can be edited by the user.
	/// </summary>

	internal partial class EditBlock : UserControl
	{

		private string savetext;				// text prior to entering edit mode


		//========================================================================================
		// Constructor
		//========================================================================================

		/// <summary>
		/// Initializes a new EditBlock, with no text and in read mode.
		/// </summary>

		public EditBlock ()
		{
			InitializeComponent();
			base.Focusable = true;
			base.FocusVisualStyle = null;
		}


		//========================================================================================
		// Properties
		//========================================================================================

		/// <summary>
		/// This bubbling routed event is raised when this instance enters edit mode.
		/// </summary>

		public static readonly RoutedEvent BeginEditEvent
			= EventManager.RegisterRoutedEvent(
				"BeginEdit", RoutingStrategy.Bubble,
				typeof(RoutedEventHandler), typeof(EditBlock));


		/// <summary>
		/// Add or remove a handler for the BeginEdit routed event.  Provides .NET
		/// access to the event.
		/// </summary>

		public event RoutedEventHandler BeginEdit
		{
			add { AddHandler(BeginEditEvent, value); }
			remove { RemoveHandler(BeginEditEvent, value); }
		}


		/// <summary>
		/// This bubbling routed event is raised when this instance completes edit mode.
		/// </summary>

		public static readonly RoutedEvent CompleteEditEvent
			= EventManager.RegisterRoutedEvent(
				"CompleteEdit", RoutingStrategy.Bubble,
				typeof(RoutedEventHandler), typeof(EditBlock));


		/// <summary>
		/// Add or remove a handler for the CompleteEdit routed event.  Provides .NET
		/// access to the event.
		/// </summary>

		public event RoutedEventHandler CompleteEdit
		{
			add { AddHandler(CompleteEditEvent, value); }
			remove { RemoveHandler(CompleteEditEvent, value); }
		}


		/// <summary>
		/// 
		/// </summary>

		public static readonly DependencyProperty IsInEditModeProperty
			= DependencyProperty.Register(
				"IsInEditMode", typeof(bool), typeof(EditBlock), new PropertyMetadata(false));


		/// <summary>
		/// 
		/// </summary>

		public bool IsInEditMode
		{
			get
			{
				return (bool)GetValue(IsInEditModeProperty);
			}

			set
			{
				if (value)
				{
					savetext = Text;
					RaiseEvent(new RoutedEventArgs(EditBlock.BeginEditEvent));
				}
				else
				{
					RaiseEvent(new RoutedEventArgs(EditBlock.CompleteEditEvent));
				}

				SetValue(IsInEditModeProperty, value);
			}
		}


		/// <summary>
		/// 
		/// </summary>

		public static readonly DependencyProperty TextProperty
			= DependencyProperty.Register(
				"Text", typeof(string), typeof(EditBlock), new PropertyMetadata(""));


		/// <summary>
		/// 
		/// </summary>

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}


		/// <summary>
		/// 
		/// </summary>

		public static readonly DependencyProperty TextTrimmingProperty
			= DependencyProperty.Register(
				"TextTrimming", typeof(TextTrimming), typeof(EditBlock),
				new PropertyMetadata(TextTrimming.None));


		/// <summary>
		/// 
		/// </summary>

		public TextTrimming TextTrimming
		{
			get { return (TextTrimming)GetValue(TextTrimmingProperty); }
			set { SetValue(TextTrimmingProperty, value); }
		}



		//========================================================================================
		// Methods
		//========================================================================================

		/// <summary>
		/// On a mouse double-click, enters edit mode.
		/// </summary>
		/// <param name="e"></param>

		protected override void OnMouseDoubleClick (MouseButtonEventArgs e)
		{
			if (!IsInEditMode)
			{
				IsInEditMode = true;
				e.Handled = true;
			}

			base.OnMouseDoubleClick(e);
		}

	
		/// <summary>
		/// Invoked when the text box is loaded, entering edit mode.
		/// </summary>
		/// <param name="sender">The TextBox.</param>
		/// <param name="e"></param>

		private void DoTextBoxLoaded (object sender, RoutedEventArgs e)
		{
			// ensure textbox has input focus
			TextBox box = (TextBox)sender;
			box.Focus();
			box.SelectAll();
		}


		/// <summary>
		/// Invoked when the text box exist edit mode.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoTextBoxLostFocus (object sender, RoutedEventArgs e)
		{
			IsInEditMode = false;
		}


		/// <summary>
		/// While in edit mode, monitors keystrokes to recognize when to commit
		/// changes and exit edit mode.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>

		private void DoTextBoxKeyDown (object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				IsInEditMode = false;
				e.Handled = true;
			}
			else if (e.Key == Key.Escape)
			{
				IsInEditMode = false;
				Text = savetext;
				e.Handled = true;
			}
		}
	}
}
