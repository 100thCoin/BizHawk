using System;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		/// <summary>
		/// Only goes to go to the frame if it is an event before current emulation, otherwise it is just a future event that can freely be edited
		/// </summary>
		private void GoToLastEmulatedFrameIfNecessary(int frame, bool OnLeftMouseDown = false)
		{
			if (frame != Emulator.Frame) // Don't go to a frame if you are already on it!
			{
				if (frame <= Emulator.Frame)
				{
					if ((MainForm.EmulatorPaused || !MainForm.IsSeeking)
						&& !CurrentTasMovie.LastPositionStable && !_playbackInterrupted)
					{
						LastPositionFrame = Emulator.Frame;
						CurrentTasMovie.LastPositionStable = true; // until new frame is emulated
					}

					GoToFrame(frame, false, false, OnLeftMouseDown);
				}
				else
				{
					_triggerAutoRestore = false;
				}
			}
		}

		public void GoToFrame(int frame, bool fromLua = false, bool fromRewinding = false, bool OnLeftMouseDown = false)
		{
			// If seeking to a frame before or at the end of the movie, use StartAtNearestFrameAndEmulate
			// Otherwise, load the latest state (if not already there) and seek while recording.
			WasRecording = CurrentTasMovie.IsRecording() || WasRecording;

			CheckForHotSwap(frame);

			if (frame <= CurrentTasMovie.InputLogLength)
			{
				// Get as close as we can then emulate there
				StartAtNearestFrameAndEmulate(frame, fromLua, fromRewinding);



				if (!OnLeftMouseDown) { MaybeFollowCursor(); }			
			}
			else // Emulate to a future frame
			{
				if (frame == Emulator.Frame + 1) // We are at the end of the movie and advancing one frame, therefore we are recording, simply emulate a frame
				{
					bool wasPaused = MainForm.EmulatorPaused;
					MainForm.FrameAdvance();
					if (!wasPaused)
					{
						MainForm.UnpauseEmulator();
					}
				}
				else
				{
					TastudioPlayMode();

					var lastState = GetPriorStateForFramebuffer(frame);
					if (lastState.Key > Emulator.Frame)
					{
						LoadState(lastState);
					}

					StartSeeking(frame);
				}
			}
		}

		public void GoToPreviousFrame()
		{
			if (Emulator.Frame > 0)
			{
				GoToFrame(Emulator.Frame - 1);
			}
		}

		public void GoToPreviousMarker()
		{
			if (Emulator.Frame > 0)
			{
				var prevMarker = CurrentTasMovie.Markers.Previous(Emulator.Frame);
				var prev = prevMarker?.Frame ?? 0;
				GoToFrame(prev);
			}
		}

		public void GoToNextMarker()
		{
			var nextMarker = CurrentTasMovie.Markers.Next(Emulator.Frame);
			var next = nextMarker?.Frame ?? CurrentTasMovie.InputLogLength - 1;
			GoToFrame(next);
		}

		/// <summary>
		/// Makes the given frame visible. If no frame is given, makes the current frame visible.
		/// </summary>
		public void SetVisibleFrame(int? frame = null)
		{
			if (TasView.AlwaysScroll && _leftButtonHeld)
			{
				return;
			}

			TasView.ScrollToIndex(frame ?? Emulator.Frame);
		}

		private void MaybeFollowCursor()
		{
			if (TasPlaybackBox.FollowCursor)
			{
				SetVisibleFrame();
			}
		}

		public string GetLoadedRomOnFrame(int frame)
		{
			string rom = "";
			int CheckFrame = frame; 
			if (CheckFrame >= CurrentTasMovie.FrameCount)
			{
				CheckFrame = CurrentTasMovie.FrameCount - 1;
			}
			while (CheckFrame > 0)
			{
				
				rom = CurrentTasMovie.GetInputState(CheckFrame).HotSwapFilePath;

				if(rom != null && rom != "")
				{
					// Due to how loading a state works, we need to be absolutely certain this state is using the correct ROM.
					// for instance, if we load the frame with the file path on it, we need to find the ROM used before this frame.
					// and when actually loading the state, it loads the state from the previous frame, which needs to be the correct ROM as well.
					// basically, if the frame we're loading is too close to a cart swap we run some extra checks.

					if(frame - CheckFrame == 0)
					{
						CheckFrame--;
						continue;
					}

					return rom;
				}

				CheckFrame--;
			}

			if (MainForm.CurrentlyOpenRom.Contains(AppContext.BaseDirectory))
			{ 
				rom = MainForm.CurrentlyOpenRom.Remove(0, AppContext.BaseDirectory.Length);
			}
			else
			{
				rom = MainForm.CurrentlyOpenRom;
				// Have some sort of warning that this ROM won't work for the tas?
			}
			return rom;

	
		}

		void CheckForHotSwap(int frame)
		{
			string RomAtCurrentFrame = GetLoadedRomOnFrame(Emulator.Frame);
			string RomAtCurrentFrameMinus1 = GetLoadedRomOnFrame(Emulator.Frame - 2);
			string RomAtTargetFrame = GetLoadedRomOnFrame(frame);
			string RomAtTargetFrameMinus1 = GetLoadedRomOnFrame(frame - 2);

			if (HotSwapper != null) // We need to hotswap the ROM before going to that frame
			{
				if (RomAtCurrentFrame != RomAtTargetFrame)
				{
					if (RomAtTargetFrame != RomAtTargetFrameMinus1)
					{
						if (RomAtCurrentFrame != RomAtTargetFrameMinus1)
						{
							HotSwapper.HotSwap(RomAtTargetFrameMinus1); //The first frame after the swap. Load previous ROM.
						}
					}
					else
					{
						HotSwapper.HotSwap(RomAtTargetFrame); //Load the ROM at target frame
					}
				}
				else if (RomAtTargetFrame != RomAtTargetFrameMinus1)
				{
					HotSwapper.HotSwap(RomAtTargetFrameMinus1); //The first frame after the swap. Load previous ROM.
				}
				else //No need to swap because the desired ROM is already loaded. unless?
				{
					if (RomAtCurrentFrameMinus1 != RomAtCurrentFrame) //If we're currently looking at the frame after the swap, let's force the swap.
					{
						if (RomAtTargetFrame != RomAtTargetFrameMinus1)
						{
							if (RomAtCurrentFrame != RomAtTargetFrameMinus1)
							{
								HotSwapper.HotSwap(RomAtTargetFrameMinus1); //The first frame after the swap. Load previous ROM.
							}
						}
						else
						{
							HotSwapper.HotSwap(RomAtTargetFrame); //Load the ROM at target frame
						}
					}
				}
			}
		}

	}
}
