using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FlexPE.Utils;

/// <summary>
/// Utils for <see cref="PEImage"/>
/// </summary>
public static class PEImageUtils {
	/// <summary>
	/// Gets the unaligned end of the whole PE headers
	/// </summary>
	/// <param name="peImage"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static FileOffset GetEndOfHeaders(this PEImage peImage) {
		if (peImage is null)
			throw new ArgumentNullException(nameof(peImage));

		var sectionHeaders = peImage.SectionHeaders;
		return sectionHeaders.Count != 0 ? sectionHeaders[^1].EndOffset : peImage.OptionalHeader.EndOffset;
	}

	/// <summary>
	/// Calculates the <see cref="OptionalHeader.SizeOfHeaders"/>
	/// </summary>
	/// <param name="peImage"></param>
	/// <param name="imageLayout"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static uint CalculateSizeOfHeaders(this PEImage peImage, ImageLayout imageLayout) {
		if (peImage is null)
			throw new ArgumentNullException(nameof(peImage));

		return peImage.AlignUp((uint)peImage.GetEndOfHeaders(), imageLayout);
	}

	/// <summary>
	/// Calculates the <see cref="OptionalHeader.SizeOfImage"/>
	/// </summary>
	/// <param name="peImage"></param>
	/// <param name="imageLayout">Image size in which PE image layout</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static uint CalculateSizeOfImage(this PEImage peImage, ImageLayout imageLayout) {
		if (peImage is null)
			throw new ArgumentNullException(nameof(peImage));

		var sectionHeaders = peImage.SectionHeaders;
		uint sizeOfImage;
		if (sectionHeaders.Count == 0)
			sizeOfImage = (uint)peImage.OptionalHeader.EndOffset;
		else if (imageLayout == ImageLayout.Memory) {
			var section = sectionHeaders[^1];
			sizeOfImage = (uint)section.VirtualAddress + section.VirtualSize;
		}
		else {
			var section = sectionHeaders.LastHasRawSize();
			sizeOfImage = section is not null ? (uint)section.PointerToRawData + section.SizeOfRawData : (uint)peImage.OptionalHeader.EndOffset;
		}
		return peImage.AlignUp(sizeOfImage, imageLayout);
	}

	/// <summary>
	/// Aligns up <paramref name="value"/>
	/// </summary>
	/// <param name="peImage"></param>
	/// <param name="value"></param>
	/// <param name="imageLayout">Alignment type</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static uint AlignUp(this PEImage peImage, uint value, ImageLayout imageLayout) {
		if (peImage is null)
			throw new ArgumentNullException(nameof(peImage));

		uint alignment = imageLayout == ImageLayout.File ? peImage.OptionalHeader.FileAlignment : peImage.OptionalHeader.SectionAlignment;
		return (value + alignment - 1) & ~(alignment - 1);
	}

	/// <summary>
	/// Aligns up <paramref name="value"/>
	/// </summary>
	/// <param name="peImage"></param>
	/// <param name="value"></param>
	/// <param name="imageLayout">Alignment type</param>
	/// <returns></returns>
	public static FileOffset AlignUp(this PEImage peImage, FileOffset value, ImageLayout imageLayout) {
		return (FileOffset)peImage.AlignUp((uint)value, imageLayout);
	}

	/// <summary>
	/// Aligns up <paramref name="value"/>
	/// </summary>
	/// <param name="peImage"></param>
	/// <param name="value"></param>
	/// <param name="imageLayout">Alignment type</param>
	/// <returns></returns>
	public static RVA AlignUp(this PEImage peImage, RVA value, ImageLayout imageLayout) {
		return (RVA)peImage.AlignUp((uint)value, imageLayout);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool UnsafeReload(this PEImage peImage, PESpan span) {
		var context = new LoadContext {
			Data = peImage.RawData[span.RawData.Index..],
			Next = true
		};
		span.Load(ref context);
		Debug.Assert(context.Next);
		return context.Next;
	}
}
