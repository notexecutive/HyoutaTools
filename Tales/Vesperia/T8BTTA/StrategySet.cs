﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HyoutaTools.Tales.Vesperia.T8BTTA {
	public class StrategySet {
		public uint NameStringDicID;
		public uint DescStringDicID;

		public uint ID;
		public uint ID2;
		public uint[,] StrategyDefaults;
		public float[] UnknownFloats1;
		public float[] UnknownFloats2;

		public string RefString;
		public StrategySet( System.IO.Stream stream, uint refStringStart ) {
			uint[] Data;
			uint entrySize = stream.PeekUInt32().SwapEndian();

			Data = new uint[entrySize / 4];
			for ( int i = 0; i < Data.Length; ++i ) {
				Data[i] = stream.ReadUInt32().SwapEndian();
			}

			ID = Data[1];
			uint refStringLocation = Data[2];
			NameStringDicID = Data[3];
			DescStringDicID = Data[4];

			StrategyDefaults = new uint[8, 9];
			for ( uint x = 0; x < 8; ++x ) {
				for ( uint y = 0; y < 9; ++y ) {
					StrategyDefaults[x, y] = Data[x * 9 + y + 5];
				}
			}

			ID2 = Data[77];

			UnknownFloats1 = new float[9];
			for ( int i = 0; i < UnknownFloats1.Length; ++i ) {
				UnknownFloats1[i] = Data[78 + i].UIntToFloat();
			}
			UnknownFloats2 = new float[9];
			for ( int i = 0; i < UnknownFloats2.Length; ++i ) {
				UnknownFloats2[i] = Data[87 + i].UIntToFloat();
			}

			long pos = stream.Position;
			stream.Position = refStringStart + refStringLocation;
			RefString = stream.ReadAsciiNullterm();
			stream.Position = pos;
		}

		public override string ToString() {
			return RefString;
		}

		public string GetDataAsHtml( GameVersion version, T8BTTA strategy, TSS.TSSFile stringDic, Dictionary<uint, TSS.TSSEntry> inGameIdDict ) {
			StringBuilder sb = new StringBuilder();
			//sb.Append( RefString );
			sb.Append( "<tr>" );
			sb.Append( "<td colspan=\"5\">" );
			sb.Append( "<span class=\"itemname\">" );
			sb.Append( inGameIdDict[NameStringDicID].StringJpnHtml( version ) );
			sb.Append( "</span>" );
			sb.Append( "<br>" );
			sb.Append( inGameIdDict[DescStringDicID].StringJpnHtml( version ) );
			sb.Append( "</td>" );
			sb.Append( "<td colspan=\"5\">" );
			sb.Append( "<span class=\"itemname\">" );
			sb.Append( inGameIdDict[NameStringDicID].StringEngHtml( version ) );
			sb.Append( "</span>" );
			sb.Append( "<br>" );
			sb.Append( inGameIdDict[DescStringDicID].StringEngHtml( version ) );
			sb.Append( "</td>" );
			sb.Append( "</tr>" );

			sb.Append( "<tr>" );
			sb.Append( "<td>" );
			sb.Append( "</td>" );
			for ( int i = 0; i < StrategyDefaults.GetLength( 1 ); ++i ) {
				sb.Append( "<td class=\"strategychar\">" );
				Website.GenerateWebsite.AppendCharacterBitfieldAsImageString( sb, version, 1u << i );
				sb.Append( "</td>" );
			}
			sb.Append( "</tr>" );
			for ( uint xRaw = 0; xRaw < StrategyDefaults.GetLength( 0 ); ++xRaw ) {
				uint x = xRaw;
				// swap around OVL and FS because they're stored the wrong way around compared to how they show up ingame
				if ( x == 6 ) { x = 7; } else if ( x == 7 ) { x = 6; }
				sb.Append( "<tr>" );
				sb.Append( "<td>" );
				sb.Append( "<span class=\"strategycat\">" );
				sb.Append( GetCategoryName( x, version, inGameIdDict ) );
				sb.Append( "</span>" );
				sb.Append( "</td>" );
				for ( uint y = 0; y < StrategyDefaults.GetLength( 1 ); ++y ) {
					if ( version == GameVersion.X360 && y == 8 ) { continue; }
					sb.Append( "<td>" );
					var option = strategy.StrategyOptionDict[StrategyDefaults[x, y]];
					sb.Append( inGameIdDict[option.NameStringDicID].StringEngOrJpnHtml( version ) );
					sb.Append( "</td>" );
				}
				sb.Append( "</tr>" );
			}

			//sb.Append( "<td>" );
			//for ( int i = 0; i < UnknownFloats1.Length; ++i ) {
			//    sb.Append( UnknownFloats1[i] + " / " );
			//}
			//sb.Append( "<br>" );
			//for ( int i = 0; i < UnknownFloats2.Length; ++i ) {
			//    sb.Append( UnknownFloats2[i] + " / " );
			//}
			//sb.Append( "</td>" );

			return sb.ToString();
		}

		public static string GetCategoryName( uint cat, GameVersion version, Dictionary<uint, TSS.TSSEntry> inGameIdDict ) {
			switch ( cat ) {
				case 6: return inGameIdDict[33912145u].StringEngOrJpnHtml( version );
				case 7: return inGameIdDict[33912144u].StringEngOrJpnHtml( version );
				case 8: return inGameIdDict[33912162u].StringEngOrJpnHtml( version );
				default: return inGameIdDict[33912138u + cat].StringEngOrJpnHtml( version );
			}
		}
	}
}
