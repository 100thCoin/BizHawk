﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public abstract class GUIDrawPluginBase : PluginLibraryBase
	{
		[RequiredService]
		protected IEmulator Emulator { get; set; }

		public GUIDrawPluginBase() : base()
		{ }

		public bool HasGUISurface = false;

		protected Color _defaultForeground = Color.White;
		protected Color? _defaultBackground;
		protected Color? _defaultTextBackground = Color.FromArgb(128, 0, 0, 0);
		protected int _defaultPixelFont = 1; // gens
		protected Padding _padding = new Padding(0);

		#region Gui API
		public virtual void Dispose()
		{
			foreach (var brush in _solidBrushes.Values)
			{
				brush.Dispose();
			}

			foreach (var brush in _pens.Values)
			{
				brush.Dispose();
			}
		}

		public abstract void DrawNew(string name, bool? clear = true);
		public abstract void DrawFinish();
		#endregion

		#region Helpers
		protected readonly Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();

		protected readonly Dictionary<Color, SolidBrush> _solidBrushes = new Dictionary<Color, SolidBrush>();
		protected readonly Dictionary<Color, Pen> _pens = new Dictionary<Color, Pen>();
		protected SolidBrush GetBrush(Color color)
		{
			SolidBrush b;
			if (!_solidBrushes.TryGetValue(color, out b))
			{
				b = new SolidBrush(color);
				_solidBrushes[color] = b;
			}

			return b;
		}

		protected Pen GetPen(Color color)
		{
			Pen p;
			if (!_pens.TryGetValue(color, out p))
			{
				p = new Pen(color);
				_pens[color] = p;
			}

			return p;
		}

		protected abstract Graphics GetGraphics();
		public void SetPadding(int all)
		{
			_padding = new Padding(all);
		}
		public void SetPadding(int x, int y)
		{
			_padding = new Padding(x / 2, y / 2, x / 2 + x & 1, y / 2 + y & 1);
		}
		public void SetPadding(int l,int t,int r, int b)
		{
			_padding = new Padding(l, t, r, b);
		}
		public Padding GetPadding()
		{
			return _padding;
		}
		#endregion

		public abstract void AddMessage(string message);

		public abstract void ClearGraphics();

		public abstract void ClearText();

		public void SetDefaultForegroundColor(Color color)
		{
			_defaultForeground = color;
		}

		public void SetDefaultBackgroundColor(Color color)
		{
			_defaultBackground = color;
		}

		public void SetDefaultTextBackground(Color color)
		{
			_defaultTextBackground = color;
		}

		public void SetDefaultPixelFont(string fontfamily)
		{
			switch (fontfamily)
			{
				case "fceux":
				case "0":
					_defaultPixelFont = 0;
					break;
				case "gens":
				case "1":
					_defaultPixelFont = 1;
					break;
				default:
					Console.WriteLine($"Unable to find font family: {fontfamily}");
					return;
			}
		}

		public void DrawBezier(Point p1, Point p2, Point p3, Point p4, Color? color = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawBezier(GetPen(color ?? _defaultForeground), p1, p2, p3, p4);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void DrawBeziers(Point[] points, Color? color = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawBeziers(GetPen(color ?? _defaultForeground), points);
				}
				catch (Exception)
				{
					return;
				}
			}
		}
		public void DrawBox(int x, int y, int x2, int y2, Color? line = null, Color? background = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					float w;
					float h;
					if (x < x2)
					{
						w = x2 - x;
					}
					else
					{
						x2 = x - x2;
						x -= x2;
						w = Math.Max(x2, 0.1f);
					}

					if (y < y2)
					{
						h = y2 - y;
					}
					else
					{
						y2 = y - y2;
						y -= y2;
						h = Math.Max(y2, 0.1f);
					}

					g.DrawRectangle(GetPen(line ?? _defaultForeground), x, y, w, h);

					var bg = background ?? _defaultBackground;
					if (bg.HasValue)
					{
						g.FillRectangle(GetBrush(bg.Value), x + 1, y + 1, Math.Max(w - 1, 0), Math.Max(h - 1, 0));
					}
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		public void DrawEllipse(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					var bg = background ?? _defaultBackground;
					if (bg.HasValue)
					{
						var brush = GetBrush(bg.Value);
						g.FillEllipse(brush, x, y, width, height);
					}

					g.DrawEllipse(GetPen(line ?? _defaultForeground), x, y, width, height);
				}
				catch (Exception)
				{
					// need to stop the script from here
					return;
				}
			}
		}

		public void DrawIcon(string path, int x, int y, int? width = null, int? height = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					if (!File.Exists(path))
					{
						AddMessage("File not found: " + path);
						return;
					}

					Icon icon;
					if (width.HasValue && height.HasValue)
					{
						icon = new Icon(path, width.Value, height.Value);
					}
					else
					{
						icon = new Icon(path);
					}

					g.DrawIcon(icon, x, y);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void DrawImage(string path, int x, int y, int? width = null, int? height = null, bool cache = true)
		{
			if (!File.Exists(path))
			{
				Console.WriteLine("File not found: " + path);
				return;
			}

			using (var g = GetGraphics())
			{
				Image img;
				if (_imageCache.ContainsKey(path))
				{
					img = _imageCache[path];
				}
				else
				{
					img = Image.FromFile(path);
					if (cache)
					{
						_imageCache.Add(path, img);
					}
				}

				g.DrawImage(img, x, y, width ?? img.Width, height ?? img.Height);
			}
		}

		public void ClearImageCache()
		{
			foreach (var image in _imageCache)
			{
				image.Value.Dispose();
			}

			_imageCache.Clear();
		}

		public void DrawImageRegion(string path, int source_x, int source_y, int source_width, int source_height, int dest_x, int dest_y, int? dest_width = null, int? dest_height = null)
		{
			if (!File.Exists(path))
			{
				Console.WriteLine("File not found: " + path);
				return;
			}

			using (var g = GetGraphics())
			{
				Image img;
				if (_imageCache.ContainsKey(path))
				{
					img = _imageCache[path];
				}
				else
				{
					img = Image.FromFile(path);
					_imageCache.Add(path, img);
				}

				var destRect = new Rectangle(dest_x, dest_y, dest_width ?? source_width, dest_height ?? source_height);

				g.DrawImage(img, destRect, source_x, source_y, source_width, source_height, GraphicsUnit.Pixel);
			}
		}

		public void DrawLine(int x1, int y1, int x2, int y2, Color? color = null)
		{
			using (var g = GetGraphics())
			{
				g.DrawLine(GetPen(color ?? _defaultForeground), x1, y1, x2, y2);
			}
		}

		public void DrawAxis(int x, int y, int size, Color? color = null)
		{
			DrawLine(x + size, y, x - size, y, color ?? _defaultForeground);
			DrawLine(x, y + size, x, y - size, color ?? _defaultForeground);
		}

		public void DrawPie(int x, int y, int width, int height, int startangle, int sweepangle, Color? line = null, Color? background = null)
		{
			using (var g = GetGraphics())
			{
				var bg = background ?? _defaultBackground;
				if (bg.HasValue)
				{
					var brush = GetBrush(bg.Value);
					g.FillPie(brush, x, y, width, height, startangle, sweepangle);
				}

				g.DrawPie(GetPen(line ?? _defaultForeground), x + 1, y + 1, width - 1, height - 1, startangle, sweepangle);
			}
		}

		public void DrawPixel(int x, int y, Color? color = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawLine(GetPen(color ?? _defaultForeground), x, y, x + 0.1F, y);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void DrawPolygon(Point[] points, Color? line = null, Color? background = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					g.DrawPolygon(GetPen(line ?? _defaultForeground), points);
					var bg = background ?? _defaultBackground;
					if (bg.HasValue)
					{
						g.FillPolygon(GetBrush(bg.Value), points);
					}
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public void DrawRectangle(int x, int y, int width, int height, Color? line = null, Color? background = null)
		{
			using (var g = GetGraphics())
			{
				var w = Math.Max(width, 0.1F);
				var h = Math.Max(height, 0.1F);
				g.DrawRectangle(GetPen(line ?? _defaultForeground), x, y, w, h);
				var bg = background ?? _defaultBackground;
				if (bg.HasValue)
				{
					g.FillRectangle(GetBrush(bg.Value), x + 1, y + 1, Math.Max(w - 1, 0), Math.Max(h - 1, 0));				}
			}
		}

		public void DrawString(int x, int y, string message, Color? forecolor = null, Color? backcolor = null, int? fontsize = null, 
							   string fontfamily = null, string fontstyle = null, string horizalign = null, string vertalign = null)
		{
			using (var g = GetGraphics())
			{
				try
				{
					var family = FontFamily.GenericMonospace;
					if (fontfamily != null)
					{
						family = new FontFamily(fontfamily);
					}

					var fstyle = FontStyle.Regular;
					if (fontstyle != null)
					{
						switch (fontstyle.ToLower())
						{
							default:
							case "regular":
								break;
							case "bold":
								fstyle = FontStyle.Bold;
								break;
							case "italic":
								fstyle = FontStyle.Italic;
								break;
							case "strikethrough":
								fstyle = FontStyle.Strikeout;
								break;
							case "underline":
								fstyle = FontStyle.Underline;
								break;
						}
					}

					// The text isn't written out using GenericTypographic, so measuring it using GenericTypographic seemed to make it worse.
					// And writing it out with GenericTypographic just made it uglier. :p
					var f = new StringFormat(StringFormat.GenericDefault);
					var font = new Font(family, fontsize ?? 12, fstyle, GraphicsUnit.Pixel);
					Size sizeOfText = g.MeasureString(message, font, 0, f).ToSize();
					if (horizalign != null)
					{
						switch (horizalign.ToLower())
						{
							default:
							case "left":
								break;
							case "center":
								x -= sizeOfText.Width / 2;
								break;
							case "right":
								x -= sizeOfText.Width;
								break;
						}
					}

					if (vertalign != null)
					{
						switch (vertalign.ToLower())
						{
							default:
							case "bottom":
								break;
							case "middle":
								y -= sizeOfText.Height / 2;
								break;
							case "top":
								y -= sizeOfText.Height;
								break;
						}
					}

					var bg = backcolor ?? _defaultBackground;
					if (bg.HasValue)
					{
						for (var xd = -1; xd <= 1; xd++)
						{
							for (var yd = -1; yd <= 1; yd++)
							{
								g.DrawString(message, font, GetBrush(bg.Value), x+xd, y+yd);
							}
						}
					}
					g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
					g.DrawString(message, font, GetBrush(forecolor ?? _defaultForeground), x, y);
				}
				catch (Exception)
				{
					return;
				}
			}
		}

		public abstract void DrawText(int x, int y, string message, Color? forecolor = null, Color? backcolor = null, string fontfamily = null);
		public abstract void Text(int x, int y, string message, Color? forecolor = null, string anchor = null);
	}
}