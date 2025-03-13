using System;
using System.Collections.Generic;

namespace FlexPE.Utils;

/// <summary>
/// Utils for <see cref="SectionHeader"/>
/// </summary>
public static class SectionHeadersUtils {
	/// <summary>
	/// Gets the last <see cref="SectionHeader"/> whose <see cref="SectionHeader.SizeOfRawData"/> is not zero
	/// </summary>
	/// <param name="sectionHeaders"></param>
	/// <param name="fromIndex"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static SectionHeader? LastHasRawSize(this IList<SectionHeader> sectionHeaders, int fromIndex = -1) {
		if (sectionHeaders is null)
			throw new ArgumentNullException(nameof(sectionHeaders));

		if (fromIndex == -1)
			fromIndex = sectionHeaders.Count - 1;
		for (int i = fromIndex; i >= 0; i--) {
			if (sectionHeaders[i].SizeOfRawData != 0)
				return sectionHeaders[i];
		}
		return null;
	}
}
