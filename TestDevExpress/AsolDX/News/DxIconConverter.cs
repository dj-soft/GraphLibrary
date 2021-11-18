using System;
using System.IO;
using System.Collections;
using System.Drawing;

using BYTE = System.Byte;
using WORD = System.UInt16;
using DWORD = System.UInt32;
using LONG = System.Int32;

// Created by Joshua Flanagan
// http://flimflan.com/blog
// May 2004
//
// You may freely use this code as you wish, I only ask that you retain my name in the source code

namespace Noris.Clients.Win.Components.AsolDX
{
	/// <summary>
	/// Provides methods for converting between the bitmap and icon formats
	/// </summary>
	public class ImageIconConverter
	{
		private ImageIconConverter() { }
		public static Icon BitmapToIcon(Bitmap b)
		{
			IconHolder ico = BitmapToIconHolder(b);
			Icon newIcon;
			using (BinaryWriter bw = new BinaryWriter(new MemoryStream()))
			{
				ico.Save(bw);
				bw.BaseStream.Position = 0;
				newIcon = new Icon(bw.BaseStream);
			}
			return newIcon;
		}

		public static IconHolder BitmapToIconHolder(Bitmap b)
		{
			BitmapHolder bmp = new BitmapHolder(); ;
			using (MemoryStream stream = new MemoryStream())
			{
				b.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
				stream.Position = 0;
				bmp.Open(stream);
			}
			return BitmapToIconHolder(bmp);
		}

		public static IconHolder BitmapToIconHolder(BitmapHolder bmp)
		{
			bool mapColors = (bmp.info.infoHeader.biBitCount <= 24);
			int maximumColors = 1 << bmp.info.infoHeader.biBitCount;
			//Hashtable uniqueColors = new Hashtable(maximumColors);
			// actual colors is probably nowhere near maximum, so dont try to initialize the hashtable
			Hashtable uniqueColors = new Hashtable();

			int sourcePosition = 0;
			int numPixels = bmp.info.infoHeader.biHeight * bmp.info.infoHeader.biWidth;
			byte[] indexedImage = new byte[numPixels];
			byte colorIndex;

			if (mapColors)
			{
				for (int i = 0; i < indexedImage.Length; i++)
				{
					//TODO: currently assumes source bitmap is 24bit color
					//read 3 bytes, convert to color
					byte[] pixel = new byte[3];
					Array.Copy(bmp.imageData, sourcePosition, pixel, 0, 3);
					sourcePosition += 3;

					RGBQUAD color = new RGBQUAD(pixel);
					if (uniqueColors.Contains(color))
					{
						colorIndex = Convert.ToByte(uniqueColors[color]);
					}
					else
					{
						if (uniqueColors.Count > byte.MaxValue)
						{
							throw new NotSupportedException(String.Format("The source image contains more than {0} colors.", byte.MaxValue));
						}
						colorIndex = Convert.ToByte(uniqueColors.Count);
						uniqueColors.Add(color, colorIndex);
					}
					// store pixel as an index into the color table
					indexedImage[i] = colorIndex;
				}
			}
			else
			{
				// added by Pavel Janda on 14/11/2006
				if (bmp.info.infoHeader.biBitCount == 32)
				{
					for (int i = 0; i < indexedImage.Length; i++)
					{
						//TODO: currently assumes source bitmap is 32bit color with alpha set to zero
						//ignore first byte, read another 3 bytes, convert to color
						byte[] pixel = new byte[4];
						Array.Copy(bmp.imageData, sourcePosition, pixel, 0, 4);
						sourcePosition += 4;

						RGBQUAD color = new RGBQUAD(pixel[0], pixel[1], pixel[2], pixel[3]);
						if (uniqueColors.Contains(color))
						{
							colorIndex = Convert.ToByte(uniqueColors[color]);
						}
						else
						{
							if (uniqueColors.Count > byte.MaxValue)
							{
								throw new NotSupportedException(String.Format("The source image contains more than {0} colors.", byte.MaxValue));
							}
							colorIndex = Convert.ToByte(uniqueColors.Count);
							uniqueColors.Add(color, colorIndex);
						}
						// store pixel as an index into the color table
						indexedImage[i] = colorIndex;
					}
					// end of addition
				}
				else
				{
					//TODO: implement converting an indexed bitmap
					throw new NotImplementedException("Unable to convert indexed bitmaps.");
				}
			}

			ushort bitCount = getBitCount(uniqueColors.Count);
			// *** Build Icon ***
			IconHolder ico = new IconHolder();
			ico.iconDirectory.Entries = new ICONDIRENTRY[1];
			//TODO: is it really safe to assume the bitmap width/height are bytes?
			ico.iconDirectory.Entries[0].Width = (byte)bmp.info.infoHeader.biWidth;
			ico.iconDirectory.Entries[0].Height = (byte)bmp.info.infoHeader.biHeight;
			ico.iconDirectory.Entries[0].BitCount = bitCount; // maybe 0?
			ico.iconDirectory.Entries[0].ColorCount = (uniqueColors.Count > byte.MaxValue) ? (byte)0 : (byte)uniqueColors.Count;
			//HACK: safe to assume that the first imageoffset is always 22
			ico.iconDirectory.Entries[0].ImageOffset = 22;
			ico.iconDirectory.Entries[0].Planes = 0;
			ico.iconImages[0].Header.biBitCount = bitCount;
			ico.iconImages[0].Header.biWidth = ico.iconDirectory.Entries[0].Width;
			// height is doubled in header, to account for XOR and AND
			ico.iconImages[0].Header.biHeight = ico.iconDirectory.Entries[0].Height << 1;
			ico.iconImages[0].XOR = new byte[ico.iconImages[0].numBytesInXor()];
			ico.iconImages[0].AND = new byte[ico.iconImages[0].numBytesInAnd()];
			ico.iconImages[0].Header.biSize = 40; // always
			ico.iconImages[0].Header.biSizeImage = (uint)ico.iconImages[0].XOR.Length;
			ico.iconImages[0].Header.biPlanes = 1;
			ico.iconImages[0].Colors = buildColorTable(uniqueColors, bitCount);
			//BytesInRes = biSize + colors * 4 + XOR + AND
			ico.iconDirectory.Entries[0].BytesInRes = (uint)(ico.iconImages[0].Header.biSize
				+ (ico.iconImages[0].Colors.Length * 4)
				+ ico.iconImages[0].XOR.Length
				+ ico.iconImages[0].AND.Length);

			// copy image data
			int bytePosXOR = 0;
			int bytePosAND = 0;
			byte transparentIndex = 0;
			transparentIndex = indexedImage[0];
			//initialize AND
			ico.iconImages[0].AND[0] = byte.MaxValue;

			int pixelsPerByte;
			int bytesPerRow; // must be a long boundary (multiple of 4)
			int[] shiftCounts;

			switch (bitCount)
			{
				case 1:
					pixelsPerByte = 8;
					shiftCounts = new int[] { 7, 6, 5, 4, 3, 2, 1, 0 };
					break;
				case 4:
					pixelsPerByte = 2;
					shiftCounts = new int[] { 4, 0 };
					break;
				case 8:
					pixelsPerByte = 1;
					shiftCounts = new int[] { 0 };
					break;
				default:
					throw new NotSupportedException("Bits per pixel must be 1, 4, or 8");
			}
			bytesPerRow = ico.iconDirectory.Entries[0].Width / pixelsPerByte;
			int padBytes = bytesPerRow % 4;
			if (padBytes > 0)
				padBytes = 4 - padBytes;

			byte currentByte;
			sourcePosition = 0;
			for (int row = 0; row < ico.iconDirectory.Entries[0].Height; ++row)
			{
				for (int rowByte = 0; rowByte < bytesPerRow; ++rowByte)
				{
					currentByte = 0;
					for (int pixel = 0; pixel < pixelsPerByte; ++pixel)
					{
						byte index = indexedImage[sourcePosition++];
						byte shiftedIndex = (byte)(index << shiftCounts[pixel]);
						currentByte |= shiftedIndex;
					}
					ico.iconImages[0].XOR[bytePosXOR] = currentByte;
					++bytePosXOR;
				}
				// make sure each scan line ends on a long boundary
				bytePosXOR += padBytes;
			}

			for (int i = 0; i < indexedImage.Length; i++)
			{
				byte index = indexedImage[i];
				int bitPosAND = 128 >> (i % 8);
				if (index != transparentIndex)
					ico.iconImages[0].AND[bytePosAND] ^= (byte)bitPosAND;    // DAJ tady došlo k chybě OutOfRange, protože pole AND mělo málo prvků...
				if (bitPosAND == 1)
				{
					// need to start another byte for next pixel
					if (bytePosAND % 2 == 1)
					{
						//TODO: fix assumption that icon is 16px wide
						//skip some bytes so that scanline ends on a long barrier
						bytePosAND += 3;
					}
					else
					{
						bytePosAND += 1;
					}
					if (bytePosAND < ico.iconImages[0].AND.Length)
						ico.iconImages[0].AND[bytePosAND] = byte.MaxValue;
				}
			}
			return ico;
		}

		private static ushort getBitCount(int uniqueColorCount)
		{
			if (uniqueColorCount <= 2)
			{
				return 1;
			}
			if (uniqueColorCount <= 16)
			{
				return 4;
			}
			if (uniqueColorCount <= 256)
			{
				return 8;
			}
			return 24;
		}

		private static RGBQUAD[] buildColorTable(Hashtable colors, ushort bpp)
		{
			//RGBQUAD[] colorTable = new RGBQUAD[colors.Count];
			//HACK: it looks like the color array needs to be the max size based on bitcount
			int numColors = 1 << bpp;
			RGBQUAD[] colorTable = new RGBQUAD[numColors];
			foreach (RGBQUAD color in colors.Keys)
			{
				int colorIndex = Convert.ToInt32(colors[color]);
				colorTable[colorIndex] = color;
			}
			return colorTable;
		}
	}

	public struct ICONIMAGE
	{
		/// <summary>
		/// icHeader: DIB format header
		/// </summary>
		public BITMAPINFOHEADER Header;
		/// <summary>
		/// icColors: Color table
		/// </summary>
		public RGBQUAD[] Colors;
		/// <summary>
		/// icXOR: DIB bits for XOR mask
		/// </summary>
		public BYTE[] XOR;
		/// <summary>
		/// icAND: DIB bits for AND mask
		/// </summary>
		public BYTE[] AND;

		public void Populate(BinaryReader br)
		{
			// read in the header
			this.Header.Populate(br);
			this.Colors = new RGBQUAD[Header.biClrUsed];
			// read in the color table
			for (int i = 0; i < this.Header.biClrUsed; ++i)
			{
				this.Colors[i].Populate(br);
			}
			// read in the XOR mask
			this.XOR = br.ReadBytes(numBytesInXor());
			// read in the AND mask
			this.AND = br.ReadBytes(numBytesInAnd());
		}

		public void Save(BinaryWriter bw)
		{
			Header.Save(bw);
			for (int i = 0; i < Colors.Length; i++)
				Colors[i].Save(bw);
			bw.Write(XOR);
			bw.Write(AND);
		}

		#region byte count calculation functions
		public int numBytesInXor()
		{
			// number of bytes per pixel depends on bitcount
			int bytesPerLine = Convert.ToInt32(Math.Ceiling((Header.biWidth * Header.biBitCount) / 8.0));
			// If necessary, a scan line must be zero-padded to end on a 32-bit boundary.			
			// so there will be some padding, if the icon is less than 32 pixels wide
			int padding = (bytesPerLine % 4);
			if (padding > 0)
				bytesPerLine += (4 - padding);
			return bytesPerLine * (Header.biHeight >> 1);

		}
		public int numBytesInAnd()
		{
			// each byte can hold 8 pixels (1bpp)
			// check for a remainder
			int bytesPerLine = Convert.ToInt32(Math.Ceiling(Header.biWidth / 8.0));
			// If necessary, a scan line must be zero-padded to end on a 32-bit boundary.			
			// so there will be some padding, if the icon is less than 32 pixels wide
			int padding = (bytesPerLine % 4);
			if (padding > 0)
				bytesPerLine += (4 - padding);
			return bytesPerLine * (Header.biHeight >> 1);
		}
		#endregion
	}

	public struct ICONDIR
	{
		/// <summary>
		/// idReserved: Always 0
		/// </summary>
		public WORD Reserved;   // Reserved
		/// <summary>
		/// idType: Resource type (Always 1 for icons)
		/// </summary>
		public WORD ResourceType;
		/// <summary>
		/// idCount: Number of images in directory
		/// </summary>
		public WORD EntryCount;
		/// <summary>
		/// idEntries: Directory entries for each image
		/// </summary>
		public ICONDIRENTRY[] Entries;

		public void Save(BinaryWriter bw)
		{
			bw.Write(Reserved);
			bw.Write(ResourceType);
			bw.Write(EntryCount);
			for (int i = 0; i < Entries.Length; ++i)
				Entries[i].Save(bw);
		}

		public void Populate(BinaryReader br)
		{
			Reserved = br.ReadUInt16();
			ResourceType = br.ReadUInt16();
			EntryCount = br.ReadUInt16();
			Entries = new ICONDIRENTRY[this.EntryCount];
			for (int i = 0; i < Entries.Length; i++)
			{
				Entries[i].Populate(br);
			}
		}
	}

	public struct ICONDIRENTRY
	{
		/// <summary>
		/// bWidth: In pixels.  Must be 16, 32, or 64
		/// </summary>
		public BYTE Width;
		/// <summary>
		/// bHeight: In pixels.  Must be 16, 32, or 64
		/// </summary>
		public BYTE Height;
		/// <summary>
		/// bColorCount: Number of colors in image (0 if >=8bpp)
		/// </summary>
		public BYTE ColorCount;
		/// <summary>
		/// bReserved: Must be zero
		/// </summary>
		public BYTE Reserved;
		/// <summary>
		/// wPlanes: Number of color planes in the icon bitmap
		/// </summary>
		public WORD Planes;
		/// <summary>
		/// wBitCount: Number of bits in each pixel of the icon.  Must be 1,4,8, or 24
		/// </summary>
		public WORD BitCount;
		/// <summary>
		/// dwBytesInRes: Number of bytes in the resource
		/// </summary>
		public DWORD BytesInRes;
		/// <summary>
		/// dwImageOffset: Number of bytes from the beginning of the file to the image
		/// </summary>
		public DWORD ImageOffset;

		public void Save(BinaryWriter bw)
		{
			bw.Write(Width);
			bw.Write(Height);
			bw.Write(ColorCount);
			bw.Write(Reserved);
			bw.Write(Planes);
			bw.Write(BitCount);
			bw.Write(BytesInRes);
			bw.Write(ImageOffset);
		}

		public void Populate(BinaryReader br)
		{
			Width = br.ReadByte();
			Height = br.ReadByte();
			ColorCount = br.ReadByte();
			Reserved = br.ReadByte();
			Planes = br.ReadUInt16();
			BitCount = br.ReadUInt16();
			BytesInRes = br.ReadUInt32();
			ImageOffset = br.ReadUInt32();
		}
	}


	public struct BITMAPFILEHEADER
	{
		public WORD Type;
		public DWORD Size;
		public WORD Reserved1;
		public WORD Reserved2;
		public DWORD OffBits;

		public void Populate(BinaryReader br)
		{
			Type = br.ReadUInt16();
			Size = br.ReadUInt32();
			Reserved1 = br.ReadUInt16();
			Reserved2 = br.ReadUInt16();
			OffBits = br.ReadUInt32();
		}

		public void Save(BinaryWriter bw)
		{
			bw.Write(Type);
			bw.Write(Size);
			bw.Write(Reserved1);
			bw.Write(Reserved2);
			bw.Write(OffBits);
		}

	}
	public struct BITMAPINFO
	{
		public BITMAPINFOHEADER infoHeader;
		public RGBQUAD[] colorMap;

		public void Populate(BinaryReader br)
		{
			infoHeader.Populate(br);
			colorMap = new RGBQUAD[getNumberOfColors()];
			// read in the color table
			for (int i = 0; i < colorMap.Length; ++i)
			{
				colorMap[i].Populate(br);
			}
		}
		public void Save(BinaryWriter bw)
		{
			infoHeader.Save(bw);
			for (int i = 0; i < colorMap.Length; i++)
				colorMap[i].Save(bw);
		}

		private uint getNumberOfColors()
		{
			if (infoHeader.biClrUsed > 0)
			{
				// number of colors is specified
				return infoHeader.biClrUsed;
			}
			else
			{
				// number of colors is based on the bitcount
				switch (infoHeader.biBitCount)
				{
					case 1:
						return 2;
					case 4:
						return 16;
					case 8:
						return 256;
					default:
						return 0;
				}
			}
		}
	}

	/// <summary>
	/// Describes the format of the bitmap image
	/// </summary>
	/// <remarks>
	/// BITMAPHEADERINFO struct
	/// referenced by http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnwui/html/msdn_icons.asp
	/// defined by http://www.whisqu.se/per/docs/graphics52.htm
	/// Only the following members are used: biSize, biWidth, biHeight, biPlanes, biBitCount, biSizeImage. All other members must be 0. The biHeight member specifies the combined height of the XOR and AND masks. The members of icHeader define the contents and sizes of the other elements of the ICONIMAGE structure in the same way that the BITMAPINFOHEADER structure defines a CF_DIB format DIB
	/// </remarks>
	public struct BITMAPINFOHEADER
	{
		public const int Size = 40;
		public DWORD biSize;
		public LONG biWidth;
		/// <summary>
		/// Height of bitmap.  For icons, this is the height of XOR and AND masks together. Divide by 2 to get true height.
		/// </summary>
		public LONG biHeight;
		public WORD biPlanes;
		public WORD biBitCount;
		public DWORD biCompression;
		public DWORD biSizeImage;
		public LONG biXPelsPerMeter;
		public LONG biYPelsPerMeter;
		public DWORD biClrUsed;
		public DWORD biClrImportant;

		public void Save(BinaryWriter bw)
		{
			bw.Write(biSize);
			bw.Write(biWidth);
			bw.Write(biHeight);
			bw.Write(biPlanes);
			bw.Write(biBitCount);
			bw.Write(biCompression);
			bw.Write(biSizeImage);
			bw.Write(biXPelsPerMeter);
			bw.Write(biYPelsPerMeter);
			bw.Write(biClrUsed);
			bw.Write(biClrImportant);
		}

		public void Populate(BinaryReader br)
		{
			biSize = br.ReadUInt32();
			biWidth = br.ReadInt32();
			biHeight = br.ReadInt32();
			biPlanes = br.ReadUInt16();
			biBitCount = br.ReadUInt16();
			biCompression = br.ReadUInt32();
			biSizeImage = br.ReadUInt32();
			biXPelsPerMeter = br.ReadInt32();
			biYPelsPerMeter = br.ReadInt32();
			biClrUsed = br.ReadUInt32();
			biClrImportant = br.ReadUInt32();
		}
	}

	// RGBQUAD structure changed by Pavel Janda on 14/11/2006
	public struct RGBQUAD
	{
		public const int Size = 4;
		public BYTE blue;
		public BYTE green;
		public BYTE red;
		public BYTE alpha;

		public RGBQUAD(BYTE[] bgr) : this(bgr[0], bgr[1], bgr[2]) { }

		public RGBQUAD(BYTE blue, BYTE green, BYTE red)
		{
			this.blue = blue;
			this.green = green;
			this.red = red;
			this.alpha = 0;
		}

		public RGBQUAD(BYTE blue, BYTE green, BYTE red, BYTE alpha)
		{
			this.blue = blue;
			this.green = green;
			this.red = red;
			this.alpha = alpha;
		}

		public void Save(BinaryWriter bw)
		{
			bw.BaseStream.WriteByte(blue);
			bw.BaseStream.WriteByte(green);
			bw.BaseStream.WriteByte(red);
			bw.BaseStream.WriteByte(alpha);
		}

		public void Populate(BinaryReader br)
		{
			blue = br.ReadByte();
			green = br.ReadByte();
			red = br.ReadByte();
			alpha = br.ReadByte();
		}
	}

	/// <summary>
	/// Provides an in-memory representation of the device independent bitmap format
	/// </summary>
	/// <remarks>
	/// Based on documentation at http://www.whisqu.se/per/docs/graphics52.htm
	/// </remarks>
	public class BitmapHolder
	{
		public BITMAPFILEHEADER fileHeader;
		public BITMAPINFO info;
		public byte[] imageData;

		public void Open(string filename)
		{
			this.Open(File.OpenRead(filename));
		}

		public void Open(Stream stream)
		{
			using (BinaryReader br = new BinaryReader(stream))
			{
				fileHeader.Populate(br);
				info.Populate(br);
				if (info.infoHeader.biSizeImage > 0)
				{
					imageData = br.ReadBytes((int)info.infoHeader.biSizeImage);
				}
				else
				{
					// can be 0 if the bitmap is in the BI_RGB format
					// in which case you just read all of the remaining data
					imageData = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));
				}
			}
		}

		public void Save(string filename)
		{
			this.Save(File.OpenWrite(filename));
		}
		public void Save(Stream stream)
		{
			using (BinaryWriter bw = new BinaryWriter(stream))
			{
				fileHeader.Save(bw);
				info.Save(bw);
				bw.Write(imageData);
			}
		}
	}

	/// <summary>
	/// Provides an in-memory representation of the device independent bitmap format
	/// </summary>
	/// <remarks>
	/// Based on documentation at http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dnwui/html/msdn_icons.asp
	/// </remarks>
	public class IconHolder
	{
		public ICONDIR iconDirectory;
		public ICONIMAGE[] iconImages;

		public IconHolder()
		{
			iconDirectory.Reserved = 0;
			iconDirectory.ResourceType = 1;
			iconDirectory.EntryCount = 1;
			iconImages = new ICONIMAGE[] { new ICONIMAGE() };
		}

		public void Open(string filename)
		{
			this.Open(File.OpenRead(filename));
		}

		public void Open(Stream stream)
		{
			using (BinaryReader br = new BinaryReader(stream))
			{
				iconDirectory.Populate(br);
				iconImages = new ICONIMAGE[iconDirectory.EntryCount];
				// Loop through and read in each image
				for (int i = 0; i < iconImages.Length; i++)
				{
					// Seek to the location in the file that has the image
					//  SetFilePointer( hFile, pIconDir->idEntries[i].dwImageOffset, NULL, FILE_BEGIN );
					br.BaseStream.Seek(iconDirectory.Entries[i].ImageOffset, SeekOrigin.Begin);
					// Read the image data
					//  ReadFile( hFile, pIconImage, pIconDir->idEntries[i].dwBytesInRes, &dwBytesRead, NULL );
					// Here, pIconImage is an ICONIMAGE structure. Party on it :)
					iconImages[i] = new ICONIMAGE();
					iconImages[i].Populate(br);
				}
			}
		}
		public void Save(string filename)
		{
			using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(filename)))
			{
				this.Save(bw);
			}
		}
		public void Save(BinaryWriter bw)
		{
			iconDirectory.Save(bw);
			for (int i = 0; i < iconImages.Length; i++)
				iconImages[i].Save(bw);
		}
		public System.Drawing.Icon ToIcon()
		{
			System.Drawing.Icon newIcon;
			using (BinaryWriter bw = new BinaryWriter(new MemoryStream()))
			{
				this.Save(bw);
				bw.BaseStream.Position = 0;
				newIcon = new System.Drawing.Icon(bw.BaseStream);
			}
			return newIcon;
		}
	}

}
