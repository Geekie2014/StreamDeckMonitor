﻿using System;
using StreamDeckSharp;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Accord.Video.FFMPEG;
using System.IO;
using System.Drawing.Imaging;

namespace SharedManagers
{
    class ImageManager
    {
        //define StreamDeck
        public static IStreamDeck deck = StreamDeck.FromHID();

        //define StreamDeck icon dimensions
        public static int dimens = deck.IconSize;

        //flag for stopping animations before closing for a clean exit 
        public static bool exitflag = false;

        //process header images and display
        public static void ProcessHeaderImages()
        {
            //create working dir
            Directory.CreateDirectory(SharedSettings.generatedDir);

            //header text locations
            Single xAxis = 35;
            Single yAxis = 35;
            Single xAxis2 = 35;
            Single yAxis2 = 18;

            //start the image header creation
            CreateImage("Cpu:", "header1", SettingsManagerSDM.ImageLocCpu, SettingsManagerSDM.headerFontSize1, xAxis, yAxis);
            CreateImage("Gpu:", "header1", SettingsManagerSDM.ImageLocGpu, SettingsManagerSDM.headerFontSize1, xAxis, yAxis);
            CreateImage("F/sec", "header2", SettingsManagerSDM.ImageLocFps, SettingsManagerSDM.headerFontSize2, xAxis2, yAxis2);
            CreateImage("Temp", "header2", SettingsManagerSDM.ImageLocTemp, SettingsManagerSDM.headerFontSize2, xAxis2, yAxis2);
            CreateImage("Load", "header2", SettingsManagerSDM.ImageLocLoad, SettingsManagerSDM.headerFontSize2, xAxis2, yAxis2);
            CreateImage("Time", "header2", SettingsManagerSDM.ImageLocTime, SettingsManagerSDM.headerFontSize2, xAxis2, yAxis2);
            CreateImage(":", "time", SettingsManagerSDM.ImageLocColon, SettingsManagerSDM.timeFontSize, xAxis, yAxis);
            CreateImage("", "header1", SettingsManagerSDM.ImageLocBlank, SettingsManagerSDM.headerFontSize1, xAxis, yAxis);

            void CreateImage(string text, string type, string filename, int textSize, Single x, Single y)
            {
                Font font;
                Brush myBrushText;
                PointF textLocation = new PointF(x, y);
                Bitmap bitmap = new Bitmap(dimens, dimens);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    //Some nice defaults for better quality (StreamDeckSharp.Examples.Drawing)
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                    //background fill color
                    Brush myBrushFill = SettingsManagerSDM.BackgroundBrush;
                    graphics.FillRectangle(myBrushFill, 0, 0, dimens, dimens);

                    if (type == "header1")
                    {
                        font = new Font(SettingsManagerSDM.myFontHeader1, textSize);
                        myBrushText = SettingsManagerSDM.HeaderBrush1;
                    }
                    if (type == "header2")
                    {
                        font = new Font(SettingsManagerSDM.myFontHeader2, textSize);
                        myBrushText = SettingsManagerSDM.HeaderBrush2;
                    }
                    if (type == "time")
                    {
                        font = new Font(SettingsManagerSDM.timeFont, textSize);
                        myBrushText = SettingsManagerSDM.TimeBrush;
                    }
                    else
                    {
                        font = new Font(SettingsManagerSDM.myFontHeader1, textSize);
                        myBrushText = SettingsManagerSDM.HeaderBrush1;
                    }

                    using (font)
                    {
                        StringFormat format = new StringFormat
                        {
                            LineAlignment = StringAlignment.Center,
                            Alignment = StringAlignment.Center
                        };
                        graphics.DrawString(text, font, myBrushText, textLocation, format);
                        bitmap.Save(filename);
                    }
                }
            }
        }

        //process video frames for animation
        public static void StartAnimation()
        {
            while (true)
            {
                //create instance of video reader and open video file
                VideoFileReader vidReader = new VideoFileReader();
                string vidFile = SharedSettings.animationImgDir + SettingsManagerSDM.animName + ".mp4";
                vidReader.Open(vidFile);

                int frameCount = Convert.ToInt32(vidReader.FrameCount);
                int adjustedCount;

                if (frameCount >= SettingsManagerSDM.framesToProcess)
                {
                    adjustedCount = SettingsManagerSDM.framesToProcess;
                }
                else
                {
                    adjustedCount = frameCount;
                }

                for (int i = 0; i < adjustedCount; i++)
                {
                    using (var vidStream = new MemoryStream())
                    {
                        //resize and save frames to MemoryStream
                        Bitmap videoFrame = new Bitmap(vidReader.ReadVideoFrame(), new Size(dimens, dimens));
                        videoFrame.Save(vidStream, ImageFormat.Png);

                        //dispose the video frame
                        videoFrame.Dispose();

                        //display animation from stream
                        vidStream.Seek(0, SeekOrigin.Begin);
                        var animStream = StreamDeckKeyBitmap.FromStream(vidStream);
                        ShowAnim(animStream);
                        vidStream.Close();
                    }
                }

                vidReader.Close();

                //display animation
                void ShowAnim(StreamDeckKeyBitmap animStream)
                {
                    foreach (var button in SettingsManagerSDM.BgButtonList())
                    {
                        if (exitflag) break;
                        deck.SetKeyBitmap(button, animStream);
                    }

                    //frametime delay
                    int frametime = SettingsManagerSDM.FrametimeValue();
                    System.Threading.Thread.Sleep(frametime);
                }
            }
        }

        //set the static headers
        public static void SetStaticHeaders()
        {
            SetStaticImg("cpu", SettingsManagerSDM.KeyLocCpuHeader);
            SetStaticImg("gpu", SettingsManagerSDM.KeyLocGpuHeader);
        }

        //process static images and display
        public static void SetStaticImg(string headerType, int headerLocation)
        {
            string bitmapLocation;
            if (headerType == SettingsManagerSDM.imageName)
            {
                bitmapLocation = SharedSettings.staticImgDir + headerType + ".png";
            }
            else
            {
                bitmapLocation = SharedSettings.generatedDir + headerType + ".png";
            }
            var staticBitmap = StreamDeckKeyBitmap.FromFile(bitmapLocation);
            deck.SetKeyBitmap(headerLocation, staticBitmap);
        }

        //process data images and display
        public static void ProcessValueImg(string dataValue, string type, int location)
        {
            Brush myBrush = SettingsManagerSDM.ValuesBrush;
            PointF dataLocation = new PointF(36f, 50f);
            Font font = new Font(SettingsManagerSDM.myFontValues, SettingsManagerSDM.valueFontSize);

            if (!dataValue.Equals(null))
            {
                if (type.Equals("f"))
                {
                    ProcessImage(SettingsManagerSDM.ImageLocFps);
                }
                if (type.Equals("t"))
                {
                    ProcessImage(SettingsManagerSDM.ImageLocTemp);
                }
                if (type.Equals("l"))
                {
                    ProcessImage(SettingsManagerSDM.ImageLocLoad);
                }
                if (type.Equals("ti"))
                {
                    ProcessImage(SettingsManagerSDM.ImageLocTime);
                }
                if (type.Equals("bl"))
                {
                    dataLocation = new PointF(35f, 35f);
                    myBrush = SettingsManagerSDM.TimeBrush;
                    font = new Font(SettingsManagerSDM.myFontTime, SettingsManagerSDM.timeFontSize);
                    ProcessImage(SettingsManagerSDM.ImageLocBlank);
                }
                if (type.Equals("bl-sm"))
                {
                    dataLocation = new PointF(35f, 35f);
                    myBrush = SettingsManagerSDM.DateBrush;
                    font = new Font(SettingsManagerSDM.myFontDate, SettingsManagerSDM.dateFontSize);
                    ProcessImage(SettingsManagerSDM.ImageLocBlank);
                }

                void ProcessImage(string imagefilepath)
                {
                    String typeImage = imagefilepath;
                    Bitmap bitmap = (Bitmap)Image.FromFile(typeImage);

                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        //Some nice defaults for better quality (StreamDeckSharp.Examples.Drawing)
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                        using (font)
                        {
                            StringFormat format = new StringFormat
                            {
                                LineAlignment = StringAlignment.Center,
                                Alignment = StringAlignment.Center
                            };

                            graphics.DrawString(dataValue, font, myBrush, dataLocation, format);
                        }
                    }

                    using (var valuesStream = new MemoryStream())
                    {
                        bitmap.Save(valuesStream, ImageFormat.Png);
                        bitmap.Dispose();

                        //display values using stream
                        valuesStream.Seek(0, SeekOrigin.Begin);
                        var valStream = StreamDeckKeyBitmap.FromStream(valuesStream);
                        deck.SetKeyBitmap(location, valStream);
                        valuesStream.Close();
                    }
                }
            }
        }

        public static void ClockState(string hours, string minutes)
        {
            string isCompact = SharedSettings.CompactView();
            string showDate = SharedSettings.ShowDate();

            DateTime today = DateTime.Today;

            string dayString = today.ToString("ddd");
            string dateString = today.ToString("dd");
            string monthString = today.ToString("MMM");

            //compact clock view
            if (isCompact == "True")
            {
                var locationHours = 8;
                var locationMinutes = 6;
                ProcessValueImg(hours, "bl", locationHours);
                ProcessValueImg(minutes, "bl", locationMinutes);
            }
            //expanded clock view
            else
            {
                var locationHours1 = 9;
                var locationHours2 = 8;
                var locationMinutes1 = 6;
                var locationMinutes2 = 5;

                string hours1 = hours[0].ToString();
                string hours2 = hours[1].ToString();
                string minutes1 = minutes[0].ToString();
                string minutes2 = minutes[1].ToString();

                ProcessValueImg(hours1, "bl", locationHours1);
                ProcessValueImg(hours2, "bl", locationHours2);
                ProcessValueImg(minutes1, "bl", locationMinutes1);
                ProcessValueImg(minutes2, "bl", locationMinutes2);
            }

            if (showDate == "True")
            {
                var locationDayOfWeek = 13;
                var locationDate = 12;
                var locationMonth = 11;

                ProcessValueImg(dayString, "bl-sm", locationDayOfWeek);
                ProcessValueImg(dateString, "bl-sm", locationDate);
                ProcessValueImg(monthString, "bl-sm", locationMonth);
            }
        }

        public static void StartAnimClock()
        {
            var locationColon = 7;

            //start loop
            while (true)
            {
                if (exitflag) break;
                var loc = StreamDeckKeyBitmap.FromFile(SettingsManagerSDM.ImageLocColon);
                deck.SetKeyBitmap(locationColon, loc);

                //animate clock colon every second
                System.Threading.Thread.Sleep(1000);
                deck.ClearKey(7);
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}