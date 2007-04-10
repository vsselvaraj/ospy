﻿//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using oSpy.Util;
using System.Drawing.Extended;

namespace oSpy
{
    public partial class TimelineNavigatorForm : Form
    {
        protected Timeline timeline;
        public Timeline Timeline
        {
            get { return timeline; }
            set { timeline = value; }
        }

        protected const int defaultMaxWH = 640;
        protected Size maxSize;

        public TimelineNavigatorForm(Timeline timeline)
        {
            InitializeComponent();

            this.timeline = timeline;

            timeline.LayoutChanged += new EventHandler(timeline_LayoutChanged);
            timeline.SizeChanged += new EventHandler(timeline_SizeChanged);
            timeline.Scroll += new ScrollEventHandler(timeline_Scroll);

            maxSize.Width = maxSize.Height = defaultMaxWH;
        }

        private int prevWidth, prevHeight;

        protected override void OnResizeBegin(EventArgs e)
        {
            base.OnResizeBegin(e);

            prevWidth = ClientRectangle.Width;
            prevHeight = ClientRectangle.Height;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            UpdateNavBitmap();
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);

            if (Width > maxSize.Width || Height > maxSize.Height)
            {
                Width = maxSize.Width;
                Height = maxSize.Height;
            }

            Rectangle fullRect = timeline.GetRealSize();
            if (fullRect.Width <= 0 || fullRect.Height <= 0)
                return;

            UpdateNavBitmap();
        }

        private Bitmap maxScaleBitmap, navBitmap;

        private void timeline_LayoutChanged(object sender, EventArgs e)
        {
            UpdateBitmaps();
        }

        private void timeline_SizeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void timeline_Scroll(object sender, ScrollEventArgs e)
        {
            Invalidate();
        }

        private void UpdateBitmaps()
        {
            try
            {
                Rectangle fullRect = timeline.GetRealSize();
                if (fullRect.Width <= 0 || fullRect.Height <= 0)
                    return;

                float ratio = (float)fullRect.Width / (float)fullRect.Height;

                maxSize.Width = (int)((float)defaultMaxWH * ratio);
                maxSize.Height = defaultMaxWH;

                maxScaleBitmap = new Bitmap(maxSize.Width, maxSize.Height);

                timeline.DrawToBitmapScaled(maxScaleBitmap);

                SetClientSizeCore(130, 130);

                int w = (int)((float)ClientRectangle.Width * ratio);
                int h = ClientRectangle.Height;
                SetClientSizeCore(w, h);
            }
            finally
            {
                UpdateNavBitmap();
            }
        }

        private void UpdateNavBitmap()
        {
            if (maxScaleBitmap == null || ClientRectangle.Width <= 0 || ClientRectangle.Height <= 0)
            {
                navBitmap = null;
                return;
            }

            navBitmap = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
            Graphics g = Graphics.FromImage(navBitmap);
            g.FillRectangle(new SolidBrush(timeline.BackColor), 0, 0, navBitmap.Width, navBitmap.Height);
            g.DrawImage(maxScaleBitmap, 0, 0, navBitmap.Width, navBitmap.Height);

            Invalidate();
        }
        
        private void GetRatios(out float x, out float y)
        {
            Rectangle fullRect = timeline.GetRealSize();
            if (fullRect.Width <= 0 || fullRect.Height <= 0)
            {
                x = y = 1.0f;
                return;
            }
            
            x = (float)ClientRectangle.Width / (float)fullRect.Width;
            y = (float)ClientRectangle.Height / (float)fullRect.Height;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            if (navBitmap == null)
            {
                g.FillRectangle(new SolidBrush(Color.White), e.ClipRectangle);
            }
            else
            {
                g.DrawImage(navBitmap, 0, 0);
            }

            // Find the current viewport
            int x = StaticUtils.GetScrollPos(timeline.Handle, StaticUtils.SB_HORZ);
            int y = StaticUtils.GetScrollPos(timeline.Handle, StaticUtils.SB_VERT);

            if (x >= 0 && y >= 0)
            {
                float ratioX, ratioY;
                GetRatios(out ratioX, out ratioY);

                int w = timeline.ClientRectangle.Width;
                int h = timeline.ClientRectangle.Height;

                Pen pen = new Pen(ForeColor);

                // Scale it down to our miniature
                x = (int) ((float)x * ratioX);
                y = (int) ((float)y * ratioY);

                w = (int) ((float)w * ratioX);
                if (w >= ClientRectangle.Width)
                    w = ClientRectangle.Width - (int) pen.Width;

                h = (int) ((float)h * ratioY);
                if (h >= ClientRectangle.Height)
                    h = ClientRectangle.Height - (int) pen.Width;

                ExtendedGraphics eg = new ExtendedGraphics(g);
                eg.FillRoundRectangle(new SolidBrush(Color.FromArgb(127, 0x9B, 0x57, 0x9F)),
                                      x, y, w, h, 3);
                eg.DrawRoundRectangle(pen, x, y, w, h, 3);
            }
        }

        private void NavigateToPoint(int destX, int destY)
        {
            float ratioX, ratioY;
            GetRatios(out ratioX, out ratioY);

            // Transform x and y into upper left from center
            int x = (int)(destX - (((float)timeline.ClientRectangle.Width * ratioX) / 2.0f));
            if (x < ClientRectangle.X)
                x = ClientRectangle.X;

            int y = (int)(destY - (((float)timeline.ClientRectangle.Height * ratioY) / 2.0f));
            if (y < ClientRectangle.Y)
                y = ClientRectangle.Y;

            float relativeX = (float)x / (float)ClientRectangle.Width;
            float relativeY = (float)y / (float)ClientRectangle.Height;

            int horzMin, horzMax;
            int vertMin, vertMax;
            StaticUtils.GetScrollRange(timeline.Handle, StaticUtils.SB_HORZ, out horzMin, out horzMax);
            StaticUtils.GetScrollRange(timeline.Handle, StaticUtils.SB_VERT, out vertMin, out vertMax);

            int horzNew = (int)((float)horzMax * relativeX);
            int vertNew = (int)((float)vertMax * relativeY);

            timeline.SetScrollPosition(horzNew, vertNew);

            Invalidate();
        }

        private bool mouseDown = false;

        private void TimelineNavigatorForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            NavigateToPoint(e.X, e.Y);

            mouseDown = true;
        }

        private void TimelineNavigatorForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            mouseDown = false;
        }

        private void TimelineNavigatorForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (!mouseDown)
                return;

            NavigateToPoint(e.X, e.Y);
        }
    }
}