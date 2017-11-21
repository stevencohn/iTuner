//************************************************************************************************
// Copyright © 2010 Steven M. Cohn. All Rights Reserved.
//
//************************************************************************************************

namespace iTuner.iTunes
{
	using System;


	/// <summary>
	/// Specifies the kind of playlist.
	/// </summary>

	internal enum PlaylistKind
	{
		Unknown,
		Library,
		User,
		CD,
		Device,
		RadioTuner,

		// ituner-specific...

		None,
		Folder,
		Purchased,
		Smart
	}
}
