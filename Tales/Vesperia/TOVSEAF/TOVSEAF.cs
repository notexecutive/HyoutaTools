﻿using System;
using System.Collections.Generic;
using System.IO;
using HyoutaTools.Tales.Vesperia.ItemDat;
using HyoutaTools.Tales.Vesperia.TSS;
using System.Text;

namespace HyoutaTools.Tales.Vesperia.TOVSEAF {
	public class TOVSEAF {
		// Search Point definition file
		public TOVSEAF( String filename ) {
			using ( Stream stream = new System.IO.FileStream( filename, FileMode.Open ) ) {
				if ( !LoadFile( stream ) ) {
					throw new Exception( "Loading TOVSEAF failed!" );
				}
			}
		}

		public TOVSEAF( Stream stream ) {
			if ( !LoadFile( stream ) ) {
				throw new Exception( "Loading TOVSEAF failed!" );
			}
		}

		public List<SearchPointDefinition> SearchPointDefinitions;
		public List<SearchPointContent> SearchPointContents;
		public List<SearchPointItem> SearchPointItems;

		private bool LoadFile( Stream stream ) {
			string magic = stream.ReadAscii( 8 );
			uint fileSize = stream.ReadUInt32().SwapEndian();
			uint definitionsStart = stream.ReadUInt32().SwapEndian();
			uint definitionsCount = stream.ReadUInt32().SwapEndian(); // 64 bytes per entry
			uint contentsStart = stream.ReadUInt32().SwapEndian();
			uint contentsCount = stream.ReadUInt32().SwapEndian(); // 16 bytes per entry
			uint itemsStart = stream.ReadUInt32().SwapEndian();
			uint itemsCount = stream.ReadUInt32().SwapEndian(); // 8 bytes per entry
			uint refStringStart = stream.ReadUInt32().SwapEndian();

			stream.Position = definitionsStart;
			SearchPointDefinitions = new List<SearchPointDefinition>( (int)definitionsCount );
			for ( uint i = 0; i < definitionsCount; ++i ) {
				SearchPointDefinitions.Add( new SearchPointDefinition( stream ) );
			}

			stream.Position = contentsStart;
			SearchPointContents = new List<SearchPointContent>( (int)contentsCount );
			for ( uint i = 0; i < contentsCount; ++i ) {
				SearchPointContents.Add( new SearchPointContent( stream ) );
			}

			stream.Position = itemsStart;
			SearchPointItems = new List<SearchPointItem>( (int)itemsCount );
			for ( uint i = 0; i < itemsCount; ++i ) {
				SearchPointItems.Add( new SearchPointItem( stream ) );
			}

			return true;
		}

		public System.Drawing.Bitmap GenerateMap( System.Drawing.Bitmap background = null ) {
			int minx = int.MaxValue;
			int maxx = int.MinValue;
			int miny = int.MaxValue;
			int maxy = int.MinValue;
			int minz = int.MaxValue;
			int maxz = int.MinValue;
			foreach ( var spd in SearchPointDefinitions ) {
				minx = Math.Min( minx, spd.CoordX );
				miny = Math.Min( miny, spd.CoordY );
				minz = Math.Min( minz, spd.CoordZ );
				maxx = Math.Max( maxx, spd.CoordX );
				maxy = Math.Max( maxy, spd.CoordY );
				maxz = Math.Max( maxz, spd.CoordZ );
			}

			int extentx = maxx - minx;
			int extenty = maxy - miny;
			int extentz = maxz - minz;
			double factor = 0.05115;
			int padx = 222;
			int pady = 185;

			System.Drawing.Bitmap bmp;
			if ( background == null ) {
				bmp = new System.Drawing.Bitmap( (int)( extentx * factor + 1 + padx * 2 ), (int)( extentz * factor + 1 + pady * 2 ) );
			} else {
				bmp = new System.Drawing.Bitmap( background );
			}

			int idx = 1;
			foreach ( var spd in SearchPointDefinitions ) {
				if ( spd.Unknown11 != 1 ) { continue; } // not sure what these mean exactly but only the ones with an '1' here show up in game
				System.Drawing.Color color = System.Drawing.Color.Black;
				System.Drawing.Color border = System.Drawing.Color.White;
				switch ( spd.SearchPointType ) {
					case 0: color = System.Drawing.Color.SpringGreen; border = System.Drawing.Color.Black; break; // tree stump
					case 1: // shells
						if ( spd.CoordY < 0 ) {
							color = System.Drawing.Color.Red; // in water
							border = System.Drawing.Color.White;
						} else {
							color = System.Drawing.Color.Aqua; // on beach
							border = System.Drawing.Color.Black;
						}
						break;
					case 2: color = System.Drawing.Color.FromArgb( 212, 212, 0 ); border = System.Drawing.Color.Black; break; // bones
					case 3: color = System.Drawing.Color.DarkBlue; border = System.Drawing.Color.White; break; // seagulls
				}
				//SetPixelArea( bmp, (int)( ( spd.CoordX - minx ) * factor + padx ), (int)( ( extentz - ( spd.CoordZ - minz ) ) * factor + pady ), color, border );

				System.Drawing.Graphics g = System.Drawing.Graphics.FromImage( bmp );
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				System.Drawing.StringFormat fmt = new System.Drawing.StringFormat( System.Drawing.StringFormatFlags.NoClip ) { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center };
				//System.Drawing.Font font = new System.Drawing.Font( "Gentium Book", 32.0f, System.Drawing.GraphicsUnit.Pixel );
				int x = (int)( ( spd.CoordX - minx ) * factor + padx );
				int y = (int)( ( extentz - ( spd.CoordZ - minz ) ) * factor + pady );
				System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
				path.AddString( idx.ToString(), new System.Drawing.FontFamily( "Gentium Book" ), (int)System.Drawing.FontStyle.Regular, 80.0f, new System.Drawing.Point( x, y + 4 ), fmt );
				g.DrawPath( new System.Drawing.Pen( border, 8 ), path );
				g.FillPath( new System.Drawing.SolidBrush( color ), path );
				g.Flush();
				++idx;
			}

			return bmp;
		}

		public static void SetPixelArea( System.Drawing.Bitmap bmp, int x, int y, System.Drawing.Color color, System.Drawing.Color border ) {
			int xext = 15, yext = 15;
			int bordersize = 3;
			for ( int i = x - xext; i < x + xext; ++i ) {
				bool isBorderX = i < x - xext + bordersize || i > x + xext - 1 - bordersize;
				for ( int j = y - yext; j < y + yext; ++j ) {
					bool isBorderY = j < y - yext + bordersize || j > y + yext - 1 - bordersize;
					if ( i >= 0 && i < bmp.Width && j >= 0 && j < bmp.Height ) {
						bmp.SetPixel( i, j, isBorderX || isBorderY ? border : color );
					}
				}
			}
		}
	}

	public class SearchPointDefinition {
		public uint Index;
		public uint ScenarioBegin; // shows up once scenario counter is this value, usually 1000999 (start of game)
		public uint ScenarioEnd; // disappears once scenario counter is this value, always 9999999 (which means none disappear)
		public uint SearchPointType; // range [0,3]

		public uint Unknown5; // always 0
		public int CoordX; // world XYZ positions, zero XY is the middle, north/east is positive
		public int CoordY; // zero Y is sea level, positive is higher
		public int CoordZ; // no clue why these are integers and not floats

		public uint Unknown9; // spawn chance, maybe? has numbers like 75, 80, 85, 90, etc, rarely lower, always in 5 increments
		public uint Unknown10; // always 10000
		public uint Unknown11; // usually 1, rarely 10
		public uint Unknown12; // always 0

		public uint Unknown13; // always 0
		public ushort Unknown14a; // range [2,7]
		public ushort Unknown14b; // always 100
		public uint SearchPointContentIndex;
		public uint SearchPointContentCount; // usually 5, rarely 3

		public SearchPointDefinition( System.IO.Stream stream ) {
			Index = stream.ReadUInt32().SwapEndian();
			ScenarioBegin = stream.ReadUInt32().SwapEndian();
			ScenarioEnd = stream.ReadUInt32().SwapEndian();
			SearchPointType = stream.ReadUInt32().SwapEndian();

			Unknown5 = stream.ReadUInt32().SwapEndian();
			CoordX = stream.ReadUInt32().SwapEndian().AsSigned();
			CoordY = stream.ReadUInt32().SwapEndian().AsSigned();
			CoordZ = stream.ReadUInt32().SwapEndian().AsSigned();

			Unknown9 = stream.ReadUInt32().SwapEndian();
			Unknown10 = stream.ReadUInt32().SwapEndian();
			Unknown11 = stream.ReadUInt32().SwapEndian();
			Unknown12 = stream.ReadUInt32().SwapEndian();

			Unknown13 = stream.ReadUInt32().SwapEndian();
			Unknown14a = stream.ReadUInt16().SwapEndian();
			Unknown14b = stream.ReadUInt16().SwapEndian();
			SearchPointContentIndex = stream.ReadUInt32().SwapEndian();
			SearchPointContentCount = stream.ReadUInt32().SwapEndian();
		}

		public string GetDataAsHtml( GameVersion version, ItemDat.ItemDat items, TSSFile stringDic, Dictionary<uint, TSSEntry> inGameIdDict, List<SearchPointContent> searchPointContents, List<SearchPointItem> searchPointItems, int index, bool phpLinks = false ) {
			StringBuilder sb = new StringBuilder();

			sb.Append( "<tr id=\"searchpoint").Append( Index ).Append( "\">" );
			sb.Append( "<td colspan=\"5\">" );
			sb.Append( "[#" ).Append( index ).Append( "] " );
			switch ( SearchPointType ) {
				case 0: sb.Append( "Tree Stump" ); break;
				case 1: sb.Append( "Shell" ); break;
				case 2: sb.Append( "Bones" ); break;
				case 3: sb.Append( "Seagulls" ); break;
				default: sb.Append( SearchPointType ); break;
			}
			if ( CoordY < 0 ) {
				sb.Append( " (Underwater)" );
			}
			if ( ScenarioBegin > 1000999 ) {
				switch ( ScenarioBegin ) {
					case 3000000: sb.Append( " (Chapter 3 only)" ); break;
					case 3590000: sb.Append( " (once Erealumen Crystallands accessible)" ); break;
					default: sb.Append( " (Appears after scenario " ).Append( ScenarioBegin ).Append( ")" ); break;
				}
			}
			//sb.Append( "<br>" )
			//sb.Append( "9: " ).Append( Unknown9 ).Append( "<br>" );
			//sb.Append( "11: " ).Append( Unknown11 ).Append( "<br>" );
			//sb.Append( "14a: " ).Append( Unknown14a ).Append( "<br>" );

			sb.Append( "</td>" );
			sb.Append( "</tr>" );

			sb.Append( "<tr>" );
			for ( uint i = 0; i < SearchPointContentCount; ++i ) {
				sb.Append( "<td>" );
				sb.Append( searchPointContents[(int)( SearchPointContentIndex + i )].GetDataAsHtml( version, items, stringDic, inGameIdDict, searchPointItems, phpLinks: phpLinks ) );
				sb.Append( "</td>" );
			}
			for ( uint i = SearchPointContentCount; i < 5; ++i ) {
				sb.Append( "<td>" );
				sb.Append( "</td>" );
			}
			sb.Append( "</tr>" );

			return sb.ToString();
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append( "#" ).Append( Index );
			sb.Append( " Type[" ).Append( SearchPointType ).Append( "]" );
			sb.Append( " Pos[" ).Append( CoordX ).Append( ", " ).Append( CoordY ).Append( ", " ).Append( CoordZ ).Append( "]" );
			sb.Append( " u11[" ).Append( Unknown11 ).Append( "]" );
			sb.Append( " u14a[" ).Append( Unknown14a ).Append( "]" );
			return sb.ToString();
		}
	}

	public class SearchPointContent {
		public uint Percentage;
		public uint SearchPointItemIndex;
		public uint SearchPointItemCount;
		public uint Padding;

		public SearchPointContent( System.IO.Stream stream ) {
			Percentage = stream.ReadUInt32().SwapEndian();
			SearchPointItemIndex = stream.ReadUInt32().SwapEndian();
			SearchPointItemCount = stream.ReadUInt32().SwapEndian();
			Padding = stream.ReadUInt32().SwapEndian();
		}

		public string GetDataAsHtml( GameVersion version, ItemDat.ItemDat items, TSSFile stringDic, Dictionary<uint, TSSEntry> inGameIdDict, List<SearchPointItem> searchPointItems, bool phpLinks = false ) {
			StringBuilder sb = new StringBuilder();
			//sb.Append( "Percentage: " ).Append( Percentage ).Append( "%" ).Append( "<br>" );
			for ( uint i = 0; i < SearchPointItemCount; ++i ) {
				//sb.Append( "Item #" ).Append( i ).Append( ":" );
				sb.Append( searchPointItems[(int)( SearchPointItemIndex + i )].GetDataAsHtml( version, items, stringDic, inGameIdDict, phpLinks: phpLinks ) );
				sb.Append( "<br>" );
			}
			return sb.ToString();
		}
	}

	public class SearchPointItem {
		public uint Item;
		public uint Count;

		public SearchPointItem( System.IO.Stream stream ) {
			Item = stream.ReadUInt32().SwapEndian();
			Count = stream.ReadUInt32().SwapEndian();
		}

		public string GetDataAsHtml( GameVersion version, ItemDat.ItemDat items, TSSFile stringDic, Dictionary<uint, TSSEntry> inGameIdDict, bool phpLinks = false ) {
			StringBuilder sb = new StringBuilder();
			var item = items.itemIdDict[Item];
			sb.Append( "<img src=\"item-icons/ICON" + item.Data[(int)ItemData.Icon] + ".png\" height=\"16\" width=\"16\"> " );
			sb.Append( "<a href=\"" + Website.GenerateWebsite.GetUrl( Website.WebsiteSection.Item, version, phpLinks, id: (int)item.Data[(int)ItemData.ID], icon: (int)item.Data[(int)ItemData.Icon] ) + "\">" );
			sb.Append( inGameIdDict[item.NamePointer].StringEngOrJpnHtml( version ) + "</a> x" + Count );
			return sb.ToString();
		}
	}
}
