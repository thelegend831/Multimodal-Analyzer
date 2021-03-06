﻿using KinectEx;
using KinectEx.DVR;
using Microsoft.Samples.Kinect.VisualizadorMultimodal.db;
using Microsoft.Samples.Kinect.VisualizadorMultimodal.models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.Samples.Kinect.VisualizadorMultimodal.core
{
    public class Player
    {
        private KinectReplay            _replay;
        private bool                    _locationSetByHand  = false;
        private int                     StartFromMillis     = 200;

        private bool colorFrameEnable = true;
        private bool bodyFrameEnable = true;
        private bool viewEnable = true;

        private bool                    wasPlayingAtDisable = false;
        private ColorFrameBitmap        _colorBitmap        = null;
        private int                     lastCurrentSecondForTimeLineCursor = 0;
        private ImageSource             lastBodyFrame       = null;
        public Player(){}

        public TimeSpan Duration
        {
            get
            {
                return _replay.Duration;
            }
        }

        public bool IsOpen
        {
            get
            {
                return _replay!=null;
            }
        }

        #region Open - Close

        public void OpenFile(string fullPath)
        {
            
            this.Close();
            _replay = new KinectReplay(File.Open(fullPath, FileMode.Open, FileAccess.Read));
            _replay.PropertyChanged += _replay_PropertyChanged;
            if (_replay.HasBodyFrames)
            {
                _replay.BodyFrameArrived += _replay_BodyFrameArrived;
            }
            if (_replay.HasColorFrames)
            {
                _replay.ColorFrameArrived += _replay_ColorFrameArrived;
            }
            MainWindow.Instance().sceneDurationLabel.Content = _replay.Duration.ToString(@"hh\:mm\:ss");
            this.sendToStartLocation();
            //_replay.Start();
            //Thread.Sleep(150);
            //_replay.Stop();

        }

        public void Close()
        {
            if (_replay != null)
            {
                if (_replay.IsStarted)
                    _replay.Stop();

                _replay.PropertyChanged -= _replay_PropertyChanged;

                if (_replay.HasBodyFrames)
                    _replay.BodyFrameArrived -= _replay_BodyFrameArrived;
                if (_replay.HasColorFrames)
                    _replay.ColorFrameArrived -= _replay_ColorFrameArrived;
                _replay.Dispose();

                _replay = null;

            }

            _colorBitmap = null; // reset to force recreation for new file
        }

        #endregion

        
        #region Enable Disable Toggle

        public void Enable()
        {
            if (wasPlayingAtDisable)
            {
                _replay.Start();
                wasPlayingAtDisable = false;
            }
            //bodyFrameEnable = true;
            //colorFrameEnable = true;
            viewEnable = true;
            if(colorFrameEnable && _colorBitmap!=null) MainWindow.Instance().colorImageControl.Source = _colorBitmap.Bitmap;
            if(bodyFrameEnable) MainWindow.Instance().bodyImageControl.Source = this.lastBodyFrame;
        }

        public void Disable()
        {
            if (_replay.IsStarted)
            {
                wasPlayingAtDisable = true;
                _replay.Stop();
            }

            //bodyFrameEnable = false;
            //colorFrameEnable = false;
            viewEnable = false;
            if (bodyFrameEnable) this.lastBodyFrame = MainWindow.Instance().bodyImageControl.Source;

            MainWindow.Instance().colorImageControl.Source = null;
            MainWindow.Instance().bodyImageControl.Source = null;
        }
        
        public void ToggleColorFrameEnable()
        {
            colorFrameEnable = !colorFrameEnable;

            if (colorFrameEnable && _colorBitmap != null)
            {
                MainWindow.Instance().colorImageControl.Source = _colorBitmap.Bitmap;
            }
            else
            {
                MainWindow.Instance().colorImageControl.Source = null;
            }
        }

        public void ToggleBodyFrameEnable()
        {
            bodyFrameEnable = !bodyFrameEnable;

            if (bodyFrameEnable)
            {
                MainWindow.Instance().bodyImageControl.Source = this.lastBodyFrame;
            }
            else
            {
                this.lastBodyFrame = MainWindow.Instance().bodyImageControl.Source;
                MainWindow.Instance().bodyImageControl.Source = null;
            }
        }

        #endregion

        public void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_replay == null)
                return;
            
            if (_replay.IsStarted)
            {
                _replay.Stop();
            }
            MainWindow.Instance().playButton2.Content = Properties.Buttons.StartPlaying;
            this.sendToStartLocation();

        }

        public void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_replay == null)
                return;

            if (!_replay.IsStarted)
            {
                _replay.Start();
                MainWindow.Instance().playButton2.Content = Properties.Buttons.PausePlaying;
            }
            else
            {
                _replay.Stop();
                //PlayButton.Content = "Play";
                MainWindow.Instance().playButton2.Content = Properties.Buttons.StartPlaying;
            }
        }

        

        private void sendToStartLocation()
        {
            _replay.ScrubTo(TimeSpan.FromMilliseconds(StartFromMillis));
        }

        public void LocationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_locationSetByHand)
            {
                if (_replay != null)
                    _replay.ScrubTo(TimeSpan.FromMilliseconds((MainWindow.Instance().sceneSlider.Value / 100.0) * _replay.Duration.TotalMilliseconds));
            }
            else
            {
                _locationSetByHand = true;
            }

            int currentSecond = (int)_replay.Location.TotalSeconds;

            if (lastCurrentSecondForTimeLineCursor != currentSecond)
            {
                //Grid.SetColumn(MainWindow.Instance().lineCurrentTimeCursor, currentSecond); // 1seg = 1col
                Grid.SetColumn(MainWindow.Instance().lineCurrentTimeRulerCursor, currentSecond); // 1seg = 1col
                lastCurrentSecondForTimeLineCursor = currentSecond;
                MainWindow.Instance().sceneCurrentTimeLabel.Content = _replay.Location.ToString(@"hh\:mm\:ss");
            }
            
        }

        private void _replay_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == KinectReplay.IsFinishedPropertyName)
            {
                MainWindow.Instance().playButton2.Content = Properties.Buttons.StartPlaying;
            }
            else if (e.PropertyName == KinectReplay.LocationPropertyName)
            {
                _locationSetByHand = false;
                MainWindow.Instance().sceneSlider.Value = 100 - (100 * ((_replay.Duration.TotalMilliseconds - _replay.Location.TotalMilliseconds) / _replay.Duration.TotalMilliseconds));
            }
        }

        //private void _replay_BodyFrameArrived(object sender, ReplayFrameArrivedEventArgs<ReplayBodyFrame> e)
        //{
        //    if (!bodyFrameEnable || !viewEnable) return;
        //    MainWindow.Instance().bodyImageControl.Source = e.Frame.Bodies.GetBitmap(Colors.LightGreen, Colors.Yellow);
        //}

        private void _replay_BodyFrameArrived(object sender, ReplayFrameArrivedEventArgs<ReplayBodyFrame> e)
        {
            if (!bodyFrameEnable || !viewEnable || Scene.Instance == null) return;

            float _width = 512;
            float _height = 424;

            Color color;
            var bitmap = BitmapFactory.New((int)_width, (int)_height);
            //using (var context = bitmap.GetBitmapContext())
            //{
            foreach (var body in e.Frame.Bodies)
            {
                if (body.IsTracked)
                {
                    Person person = Scene.Instance.Persons.FirstOrDefault(p => p.TrackingId == (long)body.TrackingId);
                    color = (person.Color as SolidColorBrush).Color;
                    body.AddToBitmap(bitmap, color, color);
                }
            }
            MainWindow.Instance().bodyImageControl.Source = bitmap; // e.Frame.Bodies.GetBitmap(Colors.LightGreen, Colors.Yellow);
        }

        private void _replay_ColorFrameArrived(object sender, ReplayFrameArrivedEventArgs<ReplayColorFrame> e)
        {
            if (!colorFrameEnable || !viewEnable) return;
            if (_colorBitmap == null)
            {
                _colorBitmap = new ColorFrameBitmap(e.Frame);
                MainWindow.Instance().colorImageControl.Source = _colorBitmap.Bitmap;
                //_justColorFrameEnabled = false;
            }
            _colorBitmap.Update(e.Frame);
            
        }


    }
}
