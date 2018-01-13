﻿using MSI.Afterburner;
using OpenHardwareMonitor.Hardware;
using StreamDeckSharp;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace StreamDeckMonitor
{
    class SDMonitor
    {
        static void Main(string[] args)
        {
            //define font settings file
            var FontIni = new IniParser("FontSettings.ini");

            //read and store the font settings
            var fontTypeHeaders = FontIni.Read("fontType", "Font_Headers");
            var fontColorHeaders = FontIni.Read("fontColor", "Font_Headers");
            var fontTypeValues = FontIni.Read("fontType", "Font_Values");
            var fontColorValues = FontIni.Read("fontColor", "Font_Values");

            /////////////////////////////
            ////   -key locations-   ////
            ////    4  3  2  1  0    ////
            ////    9  8  7  6  5    ////
            ////   14 13 12 11 10    ////
            /////////////////////////////

            //define key locations
            var key_location_framerate = 0;
            var key_location_cpu_temp = 7;
            var key_location_gpu_temp = 12;
            var key_location_cpu_load = 6;
            var key_location_gpu_load = 11;
            var key_location_header_cpu = 8;
            var key_location_header_gpu = 13;
            var key_location_header_time = 4;
            var key_location_blank2 = 1;
            var key_location_blank3 = 9;
            var key_location_blank4 = 5;
            var key_location_blank5 = 3;
            var key_location_blank6 = 14;
            var key_location_blank7 = 10;
            var key_location_blank8 = 2;

            // add CPU and GPU as hardware
            // note that, CPU temperature data requires 'highestAvailable' permission.
            Computer computer = new Computer() { CPUEnabled = true, GPUEnabled = true };
            computer.Open();

            //Open the Stream Deck device
            using (var deck = StreamDeck.FromHID())
            {
                //create all necessary header template images 
                Process_Header_Images();

                //Set static images
                Set_Static("cpu", key_location_header_cpu);
                Set_Static("gpu", key_location_header_gpu);
                Set_Static("blank", key_location_blank2);
                Set_Static("blank", key_location_blank3);
                Set_Static("blank", key_location_blank4);
                Set_Static("blank", key_location_blank5);
                Set_Static("blank", key_location_blank6);
                Set_Static("blank", key_location_blank7);
                Set_Static("blank", key_location_blank8);

                //Set loop
                while (true)
                {
                    try
                    {
                        //Connect to MSI Afterburner MACM shared memory
                        HardwareMonitor mahm = new HardwareMonitor();
                        //Get monitoring data
                        HardwareMonitorEntry framerate = mahm.GetEntry(HardwareMonitor.GPU_GLOBAL_INDEX, MONITORING_SOURCE_ID.FRAMERATE);

                        //Get values for framerate and pass to Set_Value to process
                        int framerateInt = (int)Math.Round(framerate.Data);
                        string dataValue = framerateInt.ToString();
                        string type = "f";
                        int location = key_location_framerate;

                        Set_Value(dataValue, type, location);
                    }

                    finally
                    {
                        //Get and set time 
                        string time_output = DateTime.Now.ToString("hh:mm");

                        //check if hours start with "0"
                        if (time_output.StartsWith("0"))
                        {
                            //remove starting "0" from string
                            time_output = time_output.Remove(0, 1);
                        }

                        Set_Value(time_output, "ti", key_location_header_time);
                    }

                    //Search hardware
                    foreach (IHardware hardware in computer.Hardware)
                    {
                        hardware.Update();

                        //Check for gpu sensor
                        if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAti)
                        {
                            foreach (ISensor sensor in hardware.Sensors)
                            {
                                //Search for temp sensor
                                if (sensor.SensorType == SensorType.Temperature)
                                {
                                    string dataValue = sensor.Value.ToString() + "c";
                                    string type = "t";
                                    int location = key_location_gpu_temp;

                                    Set_Value(dataValue, type, location);
                                }

                                //Search for load sensor
                                if (sensor.SensorType == SensorType.Load)
                                {
                                    //Add gpu load sensors to list
                                    string get_values = sensor.Name + sensor.Value.ToString();
                                    List<string> value_list = new List<string>
                                {
                                    get_values
                                };
                                    //Get values for gpu and pass to Set_Value to process
                                    foreach (string value in value_list)
                                    {
                                        if (value.Contains("GPU Core"))
                                        {
                                            //Get values for gpu and pass to Set_Value to process
                                            int gpuloadInt = (int)Math.Round(sensor.Value.Value);
                                            string dataValue = gpuloadInt.ToString() + "%";
                                            string type = "l";
                                            int location = key_location_gpu_load;

                                            Set_Value(dataValue, type, location);
                                        }
                                    }
                                }
                            }
                        }

                        //Check for cpu sensor
                        if (hardware.HardwareType == HardwareType.CPU)
                        {
                            foreach (ISensor sensor in hardware.Sensors)
                            {
                                if (sensor.SensorType == SensorType.Temperature)
                                {
                                    //Add cpu temp sensors to list
                                    string get_values = sensor.Name + sensor.Value.ToString();
                                    List<string> value_list = new List<string>
                                {
                                    get_values
                                };
                                    //Get values for cpu and pass to Set_Value to process
                                    foreach (string value in value_list)
                                    {
                                        if (value.Contains("Core #1"))
                                        {
                                            string result_package = value.Substring(Math.Max(0, value.Length - 2));
                                            if (!result_package.Contains("#"))
                                            {
                                                string dataValue = result_package.ToString() + "c";
                                                string type = "t";
                                                int location = key_location_cpu_temp;

                                                Set_Value(dataValue, type, location);
                                            }
                                        }
                                    }
                                }
                                //Search for load sensor
                                if (sensor.SensorType == SensorType.Load)
                                {
                                    //Add cpu load sensors to list
                                    string get_values = sensor.Name + sensor.Value.ToString();
                                    List<string> value_list = new List<string>
                                {
                                    get_values
                                };
                                    //Get values for cpu and change Stream Deck image
                                    foreach (string value in value_list)
                                    {
                                        if (value.Contains("CPU Total"))
                                        {
                                            //Get values for cpu and pass to Set_Value to process
                                            int cpuloadInt = (int)Math.Round(sensor.Value.Value);
                                            string dataValue = cpuloadInt.ToString() + "%";
                                            string type = "l";
                                            int location = key_location_cpu_load;

                                            Set_Value(dataValue, type, location);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //Wait 1 second before restarting loop
                    System.Threading.Thread.Sleep(1000);

                    //Check for key presses, if pressed send exit command 
                    deck.KeyPressed += Deck_KeyPressed;

                    void Set_Value(string dataValue, string type, int location)
                    {
                        if (!dataValue.Equals(null))
                        {
                            if (type.Equals("f"))
                            {
                                string imagefilepath = ("img\\fps.png");
                                string tempimagefilepath = ("img\\f.png");
                                Process_Image(imagefilepath, tempimagefilepath);
                            }

                            if (type.Equals("t"))
                            {
                                string imagefilepath = ("img\\temp.png");
                                string tempimagefilepath = ("img\\t.png");
                                Process_Image(imagefilepath, tempimagefilepath);
                            }

                            if (type.Equals("l"))
                            {
                                string imagefilepath = ("img\\load.png");
                                string tempimagefilepath = ("img\\l.png");
                                Process_Image(imagefilepath, tempimagefilepath);
                            }


                            if (type.Equals("ti"))
                            {
                                string imagefilepath = ("img\\time.png");
                                string tempimagefilepath = ("img\\ti.png");
                                Process_Image(imagefilepath, tempimagefilepath);
                            }

                            void Process_Image(string imagefilepath, string tempimagefilepath)
                            {
                                PointF dataLocation = new PointF(36f, 50f);
                                String typeimage = imagefilepath;
                                String typeimagetempfilepath = tempimagefilepath;
                                Bitmap bitmap = (Bitmap)Image.FromFile(typeimage);//load the image file

                                using (Graphics graphics = Graphics.FromImage(bitmap))
                                {
                                    using (Font font = new Font(fontTypeValues, 20))
                                    {
                                        StringFormat format = new StringFormat
                                        {
                                            LineAlignment = StringAlignment.Center,
                                            Alignment = StringAlignment.Center
                                        };

                                        Brush myBrush = new SolidBrush(Color.FromName(fontColorValues.ToString()));
                                        graphics.DrawString(dataValue, font, myBrush, dataLocation, format);
                                    }
                                }

                                bitmap.Save(typeimagetempfilepath);//save the image file

                                var value_bitmap = StreamDeckKeyBitmap.FromFile(typeimagetempfilepath);
                                deck.SetKeyBitmap(location, value_bitmap);
                            }
                        }
                    }

                    void Deck_KeyPressed(object sender, StreamDeckKeyEventArgs e)
                    {
                        //Kill console App
                        Environment.Exit(0);
                    }
                }

                void Process_Header_Images()
                {
                    //define image locations
                    string cpu = ("img\\cpu.png");
                    string gpu = ("img\\gpu.png");
                    string fps = ("img\\fps.png");
                    string temp = ("img\\temp.png");
                    string load = ("img\\load.png");
                    string time = ("img\\time.png");

                    //start the image header creation
                    Create_Image("FPS", fps, 18, 36f, 18f);
                    Create_Image("Temp", temp, 18, 36f, 18f);
                    Create_Image("Load", load, 18, 36f, 18f);
                    Create_Image("Time ", time, 18, 36f, 18f);
                    Create_Image("CPU: ", cpu, 23, 36f, 36f);
                    Create_Image("GPU: ", gpu, 23, 36f, 36f);

                    void Create_Image(string text, string filename, int textsize, Single x, Single y)
                    {
                        PointF textLocation = new PointF(x, y);
                        Bitmap bitmap = new Bitmap(72, 72);
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            using (Font font = new Font(fontTypeHeaders, textsize))
                            {
                                StringFormat format = new StringFormat
                                {
                                    LineAlignment = StringAlignment.Center,
                                    Alignment = StringAlignment.Center
                                };

                                Brush myBrush = new SolidBrush(Color.FromName(fontColorHeaders.ToString()));
                                graphics.DrawString(text, font, myBrush, textLocation, format);
                                bitmap.Save(filename);//save the image file 
                            }
                        }
                    }
                }

                void Set_Static(string header_type, int header_location)
                {
                    var value_bitmap = StreamDeckKeyBitmap.FromFile("img\\" + header_type + ".png");
                    deck.SetKeyBitmap(header_location, value_bitmap);
                }
            }
        }
    }
}