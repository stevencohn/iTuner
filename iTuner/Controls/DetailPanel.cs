//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner
{
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;
	using System.Windows.Shapes;


	/// <summary>
	/// This is a content control similar to the "tan gradient" track info control in iTunes.
	/// It is used for our track information panel and various header lines in other windows.
	/// </summary>
	/// <remarks>
	/// While this UserControl relies on the hosting Xaml file to include DetailPanelStyles.xaml
	/// it muts not be directly associated with that file as other user controls are to their xaml
	/// files (dependently bound in the Visual Studio Solution Explorer).  This will break the
	/// ability to name contained elements with x:Name.
	/// </remarks>

	[TemplatePart(Name = "PART_TopGloss", Type = typeof(Rectangle))]
	[TemplatePart(Name = "PART_BottomGloss", Type = typeof(Rectangle))]
	[TemplatePart(Name = "PART_Body", Type = typeof(ContentControl))]
	internal partial class DetailPanel : UserControl
	{
		private Rectangle topGloss;
		private Rectangle bottomGloss;


		public override void OnApplyTemplate ()
		{
			base.OnApplyTemplate();

			topGloss = GetTemplateChild("PART_TopGloss") as Rectangle;
			bottomGloss = GetTemplateChild("PART_BottomGloss") as Rectangle;
		}


		/// <summary>
		/// Adjust the top and bottom gloss rectangles to fit the new geometry of the control.
		/// </summary>
		/// <param name="sizeInfo">The new geometry information.</param>

		protected override void OnRenderSizeChanged (SizeChangedInfo sizeInfo)
		{
			if (DesignerProperties.GetIsInDesignMode(this))
			{
				return;
			}

			base.OnRenderSizeChanged(sizeInfo);

			double height = sizeInfo.NewSize.Height;
			double width = sizeInfo.NewSize.Width;

			// topGloss should be half the height of the container
			topGloss.Height = height / 2;
			topGloss.Width = width;

			// bottomGloss should be 40% of the height of the container and
			// its top set at the 40% Y value of the height of the container
			Thickness margin = new Thickness();
			margin.Left = bottomGloss.Margin.Left;
			margin.Top = height * 0.35;
			margin.Right = bottomGloss.Margin.Right;
			margin.Bottom = bottomGloss.Margin.Bottom;

			bottomGloss.Height = height * 0.45;
			bottomGloss.Width = width;
			bottomGloss.Margin = margin;

			// clip off the top rounded corners
			bottomGloss.Clip = new RectangleGeometry(
				new Rect(0, 3, bottomGloss.Width, bottomGloss.Height - 3));
		}
	}
}
