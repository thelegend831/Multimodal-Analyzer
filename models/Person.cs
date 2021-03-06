﻿using Microsoft.Kinect;
using Microsoft.Samples.Kinect.VisualizadorMultimodal.analytics;
using Microsoft.Samples.Kinect.VisualizadorMultimodal.core;
using Microsoft.Samples.Kinect.VisualizadorMultimodal.db;
using Microsoft.Samples.Kinect.VisualizadorMultimodal.views;
using Microsoft.Samples.Kinect.VisualizadorMultimodal.windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Microsoft.Samples.Kinect.VisualizadorMultimodal.models
{
    public class Person
    {
        public Joint center;

        //[NotMapped]
        public int ListIndex { get; private set; }

        public int PersonId { get; set; }
        //public int BodyIndex { get; set; }
        //[Column(TypeName = "bigint")]
        public long TrackingId { get; set; }
        public string Name { get; set; }
        public GenderEnum Gender { get; set; }
        public int Age { get; set; }
        private double totalMinutesAverageInteraction;
        public List<PostureIntervalGroup> PostureIntervalGroups { get; set; }
        [NotMapped]
        public List<MicroPosture> MicroPostures { get; set; }

        public int SceneId { get; set; }
        public Scene Scene { get; set; }

        [NotMapped]
        public Brush Color { get; private set; }

        [NotMapped]
        public bool HasBeenTracked
        {
            get {
                return
                    (PostureIntervalGroups != null && PostureIntervalGroups.Count != 0) ||
                    (MicroPostures!=null && MicroPostures.Count != 0);
            }
        }

        //[NotMapped]
        //private GestureDetector GestureDetector = null;

        //[NotMapped]
        //private Body body = null;

        //[NotMapped]
        //public PosturesPersonView PosturesView { get; set; }

        [NotMapped]
        public PersonView View { get; private set; }

        [NotMapped]
        public static Brush[] colors = {
            Brushes.LightSalmon,
            Brushes.LightSeaGreen,
            Brushes.LightSkyBlue,
            Brushes.LightYellow,
            Brushes.Red,
            Brushes.Orange,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Indigo,
            Brushes.Violet
            
        };

        [NotMapped]
        private static int currentColorIndex = 0;
       
        public Person() {
            this.Color = colors[currentColorIndex++];
            if (currentColorIndex == colors.Count()) currentColorIndex = 0;
        }

        public enum GenderEnum { Masculino, Femenino };
        public Person(ulong trackingId, int listIndex/*int bodyIndex, string name, GenderEnum gender, int age*/)
        //public Person(int bodyIndex, string name)
        {
            totalMinutesAverageInteraction = -1;
            //this.BodyIndex = bodyIndex;
            this.TrackingId = (long)trackingId;
            this.ListIndex = listIndex;
            this.Name = "Persona "+ (listIndex + 1);
            //this.Gender = null;
            //this.Age = null;
            this.MicroPostures = new List<MicroPosture>();
            this.PostureIntervalGroups = new List<PostureIntervalGroup>();
            //this.PosturesView = null;
            this.Color = colors[currentColorIndex++];
            if (currentColorIndex == colors.Count()) currentColorIndex = 0;

            this.View = new PersonView(this);
            //this.GestureDetector = 
            //    new GestureDetector(bodyIndex, KinectBody.kinectSensor);


        }

        public void generateView()
        {
            this.View = new PersonView(this);
        }

        

        //public void showPersonInfo()
        //{
        //    IReadOnlyDictionary<PostureType, float> posturesAvg = this.calculatePosturesAverage();

        //    Console.WriteLine("Nombre: "+Name);
        //    Console.WriteLine("Genero: "+Gender.ToString("g"));
        //    Console.WriteLine("Edad: "+Age);

        //    foreach (PostureType posture in posturesAvg.Keys)
        //    {
        //        Console.WriteLine(posture.Name + ": " + posturesAvg[posture].ToString("0.00"));
        //    }

        //    ChartForm chartForm = new ChartForm(this);
        //    chartForm.Show();

        //}

        public void generatePostureIntervals()
        {
            if (this.MicroPostures.Count == 0) return;
            //Dictionary<string, string> dic = new Dictionary<string, string>();

            foreach (PostureType currentPostureType in PostureTypeContext.db.PostureType.ToList())
            {
                //System.Collections.IEnumerable microPostures = person.microPostures.Where(microPosture => microPosture.postureType.name == postureType.name);

                PostureIntervalGroup postureIntervalGroup = new PostureIntervalGroup(currentPostureType);
                MicroPosture initialMicroPosture = null, lastMicroPosture = null;
                TimeSpan threshold = TimeSpan.FromMilliseconds(Convert.ToDouble(Properties.Resources.PostureDurationDetectionThreshold));
                //bool moreThanOneInterval = false;
                //bool intervalOpen = false;

                foreach (MicroPosture microPosture in this.MicroPostures)
                {
                    if (microPosture.PostureType.Name != currentPostureType.Name) continue;

                    if (lastMicroPosture == null)
                    {
                        initialMicroPosture = microPosture;
                        //intervalOpen = true;
                    }
                    else if (microPosture.SceneLocationTime.Subtract(lastMicroPosture.SceneLocationTime) >= threshold)
                    {
                        postureIntervalGroup.addInterval(initialMicroPosture, lastMicroPosture);
                        initialMicroPosture = microPosture;
                        //intervalOpen = true;
                    }
                    lastMicroPosture = microPosture;

                }
                if (initialMicroPosture != null)
                {
                    postureIntervalGroup.addInterval(initialMicroPosture, lastMicroPosture);
                }
                if (postureIntervalGroup.Intervals.Count > 0) this.PostureIntervalGroups.Add(postureIntervalGroup);
            }
        }
       
        public double TotalMinutesAverageInteraction
        {
            get
            {
                
                //if ( totalMinutesAverageInteraction <= 0)
                //{
                //    var groups = Interaction.Instance.IntervalGroups.Where(g => g.Person1 == this || g.Person2 == this);
                //    double MinutesTotalDuration = 0;
                //    foreach (var group in groups)
                //    {
                //        MinutesTotalDuration += group.TotalDuration.TotalSeconds;
                //    }
                //    totalMinutesAverageInteraction = MinutesTotalDuration / groups.Count();
                //}

                var groups = Interaction.Instance.IntervalGroups.Where(g => g.Person1 == this || g.Person2 == this);
                double MinutesTotalDuration = 0;
                int cantIntervals = 0;
                foreach (var group in groups)
                {
                    MinutesTotalDuration += group.TotalDuration.TotalSeconds;
                    cantIntervals += group.RelationsIntervals.Count();
                }
                
                totalMinutesAverageInteraction = MinutesTotalDuration / cantIntervals;

                return totalMinutesAverageInteraction;
            }
           
        }

        override
        public string ToString()
        {
            return Name;
        }

    }
}
