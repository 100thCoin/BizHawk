#nullable disable

using System;

namespace BizHawk.Emulation.Common
{
	public interface IHotSwap : IEmulatorService
	{

		/// <summary>
		/// Swaps ROMs without clearing ram
		/// </summary>
		void HotSwap(string FilePath);
	}
}
