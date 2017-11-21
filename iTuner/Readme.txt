
========================================================================================
iTuner Version 1.9.6234 Release
22 Jan 2017

Copyright © 2010-2017 Steven M. Cohn.  All Rights Reserved.
========================================================================================

System Requirements
-------------------

	1.4.4322 and later compatible with iTunes 10.5 and later
	- Tested with 10.5.0.142

	1.2.3767 and later compatible with iTunes 9.1 and later
	- Tested with 9.1.0.79

	All previous version compatible with iTunes 9.0.x version only
	- Tested with 9.0.2.25 OK
	- Tested with 9.0.3.15 OK
	
	Windows 7 or Windows Vista
	Microsoft .NET Framework 4.x


Change Log
----------

1.9.6234 (22 Jan 2017)

	- Correct popup positioning for HighDPI monitor

1.9.5368 (12 Sep 2014)

	- Additional fine-tuning of #9801
	- Repaired HTML parsing of Lyrics007 provider
	- Deprecated MP3Lyrics.org provider

1.9.5366 (10 Sep 2014)

	- Fix #9801 iTuner opens below taskbar
	- Various disposables now properly disposed

1.8.4627 Beta (01 Sep 2012)

	- Double-quote post-build step to allow spaces in path

	- Verify and fix errors in track info using MB
	    - recommended only for unidentified tracks
	- Upgrade to V2 of MusicBrainz XML Web service
	- New application icon and updated iTunes icon
	- Click Artist to open artist Web site
	- Scanners are now prioritized in queue
	- 64-bit Installer
	- Fix when saving artwork to invalid file name
	- Fix to properly save artwork scanner option
	- Fix to initialize Controller timer earlier
	- Fix #6518 Need Higher Res Icon
	- Fix #9375 Constantly Crashing
	- Fix #9376 Higher Quality Artwork
	- Fix #9503 Crashes... (dup of 9375)
	- Fix #9508 Cannot build solution

1.7.4500 (27 Apr 2012)

	- Added Artwork scanner to retrieve album atwork
	- Added Artwork scanner as a user option
	- Added album artwork display on tracker window
	- NOTE artwork scanner requires personal AWS keys
	- Update lyrics providers to accomodate format changes
	- Fix to properly sync access to all IiTunes properties
	- Fix to dismiss rogue iTuner instance upon startup

1.7.4497 Beta (24 Apr 2012)

	- Added Artwork scanner to retrieve album atwork
	- Added Artwork scanner as a user option
	- Display album artwork on tracker window
	- Artwork scanner requires personal AWS keys

1.6.4492 (19 Apr 2012)

	- Added Import Playlist feature
	- Modified Export Playlist to allow list-only option
	- Fix exception while unhooking event handlers

1.5.4475 (03 Apr 2012)

	- Fix to parse empty playlists in iTunes Library

1.4.4380 (29 Dec 2011)

	- Update German translations, courtesy of Flagbug
	- Recompile under .NET Client Profile
	- Attempt to resolve "missing window" problem
	- Handle iTunes open-dialog condition

1.4.4322 (01 Nov 2011)

	- Added German (unverified, apologies if incorrect)
	- Properly source invariant resources with correct resIDs
	- Replaced obsolete lyric providers with working providers
	- Fix Pseudolater to correctly morph every third char
	- Fix null reference in CatalogBase

1.3.3964 (04 Nov 2010)

	- Targetted .NET 4.0 Release
	- Added context tooltips to Librarian scanners
	- Added Disconnected config option for debugging
	- Fix #6653 error in directory name characters 
	- Fix #6857 to adjust tracker window fade-out timer 
	- Fix #7042 skip Protected AAC exports, avoid iTunes prompt
	- Fix to prevent tracker window from stealling focus

1.2.3782 (10 May 2010)

	- Added Options dialog, initially for auto-scanners
	- Fix #6533 to handle key sequence conflicts
	- Fix #6557, duplicate of #6533
	- Fix to TerseCatalog to ignore DTD when offline
	- Fix to recognize incompatible iTunes versions

1.2.3769 Beta 3c (28 May 2010)

	- Added new hot key action to Show Lyrics, Alt+Win+L
	- Added double-click play/pause handler for NotifyIcon
	- Fix #6511 to properly handle unknown playlists
	- Fix #6512 to add missing MessageBox icons to source control
	- Fix to allow escaped spaces in music library path
	- Fix memory leak in Export/Synchronize dialogs
	- Fix memory leak in MovableWindow base class

1.2.3768 Beta 3b (26 Apr 2010)

	- Fix to reveal proper name of contextual scanners
	- Fix to disable automated scanners using config
	- Fix to cancel scenarios of export/sync dialogs

1.2.3767 Beta 3 (25 Apr 2010)

	- Complete rewrite of iTunes wrappers to handle COM interrupts
	- Automated import of files added to Library folders
	- Optimized catalog improve performance and memory use
	- Automated scanners can be disabled in configuration (UI coming)
	- Added Task panel behind main buttons to show running tasks
	- Added Check for Upgrades feature on startup and About box
	- Added custom Notify icons to indicate active background task
	- Improved MovableWindow, replacing custom code with DragMove
	- Issue: Export/Sync sometimes hangs or does not complete

1.2.3738 Beta 2 (27 Mar 2010)

	- Synchronize one or more iTunes playlists with a USB MP3 player
	- Fix #6240 to allow multiple playlists with the same name
	- Fix to auto-detect folder capabilities feature of synchronizer
	- Fix to properly position windows relative to docked taskbar
	- Fix to Synchronize dialog when using playlist in folder layout
	- Fix to Export dialog to properly italicize "No encoder" item
	- Fix to Export dialog to correctly interperet encoder ComboBox 

1.2.3735 Beta (24 Mar 2010)

	- USB MP3 Player / iTunes Playlist synchronization

1.1.3711 Release (01 Mar 2010)

	- Automated Librarian - albums cleaned as tracks are played
	- Export and convert tracks - by artist, album, or playlist
	- Fix to ChartLyricsLyricsProvider to govern request intervals
	- Fix to improve start-up time, deferred initialization to background
	- Fix to recognize musical playlists by scanning perceived file types

1.1.3707 Beta 3 (24 Feb 2010)

	- Automated Librarian
	- Export and convert tracks - by artist, album, or playlist
	- Fix to ChartLyricsLyricsProvider to govern request intervals
	- Fix to improve start-up time, deferred initialization to background
	- Fix to recognize musical playlists by scanning perceived file types

1.1.3703 Beta 2 (20 Feb 2010)

	- Use NetworkStatus to avoid online providers while machine is offline
	- Fix to CurrentTrack tracking status
	- Fix to avoid memory leak while switching tracks
	- Fix to avoid memory leak while running DuplicateScanner
	- Fix to hide windows from Alt-Tab program switcher control
	- Fix to avoid empty notify icon tooltip text
	- Fix to avoid double-drawing text in context menu

1.1.3699 Beta (16 Feb 2010)

	- Initial implementation of Librarian
	    - Remove dead phantom tracks
	    - Remove duplicate entires with the assistance of genpuid
	    - Remove empty directories left by iTunes Library Organizer
	    - "Clean" item added to notify icon context menu
	- Shows current track title, artist, and album name in Notify icon tooltip
	- Adding optional output logging... we recommend BareTail
	- Adding application configuration file
	- May drag and move ExceptionDialog
	- Enhanced About box

1.0.3692 Patch (09 Feb 2010)

	- Implementation of basic track information editor.  Double-click title, artist, or
	  album name from main window or popup window to modify the field value.  The edit
	  control was implemented by the new EditBlock UserControl.
	- Fix to FadingWindow to restart fading timers when window is unpinned while mouse
	  cursor is not hovering over window.
	- Fix to add localizable resources for lyrics report header.
	- Fix to SplashWindow to include full version information.

1.0.3690 Release (08 Feb 2010)

	- Initial release

.
