﻿using Microsoft.Samples.Kinect.VisualizadorMultimodal.db;
using Microsoft.Samples.Kinect.VisualizadorMultimodal.models;
using Microsoft.Samples.Kinect.VisualizadorMultimodal.views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Microsoft.Samples.Kinect.VisualizadorMultimodal.core
{
    public class IO
    {
        public static void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                DefaultExt = ".mvs",
                Filter = "Multimodal Visualizer Scene (*.mvs)|*.mvs"
            };
            
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                Kinect.Instance.Player.Close();
                if (File.Exists(Properties.Paths.CurrentKdvrFile)) File.Delete(Properties.Paths.CurrentKdvrFile);
                if (File.Exists(Properties.Paths.CurrentDataFile)) File.Delete(Properties.Paths.CurrentDataFile);

                ZipFile.ExtractToDirectory(dlg.FileName, Properties.Paths.CurrentSceneDirectory);

                Kinect.Instance.Player.OpenFile(Properties.Paths.CurrentKdvrFile);

                var db = BackupDataContext.CreateConnection(Properties.Paths.CurrentDataFile);
                Scene.CreateFromDbContext();

                MainWindow.Instance().FromSceneRadioButton.IsChecked = true;

                System.Windows.Forms.DialogResult dialogResult =
                    System.Windows.Forms.MessageBox.Show(
                        "Escena importada con éxito!",
                        "Importar escena");
            }
        }

        public static void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog()
            {
                FileName = DateTime.Now.ToString("yyyy-MM-dd _ hh-mm-ss"),
                DefaultExt = ".mvs",
                Filter = "Multimodal Visualizer Scene (*.mvs)|*.mvs"
            };

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                //string tmpScenePath = Properties.Paths.tmpDirectory + @"\exporting_scene";
                //Utils.DirectoryCopy(Properties.Paths.RecordedSceneDirectory, tmpScenePath);
                //if (File.Exists(dlg.FileName)) File.Delete(dlg.FileName);

                if (File.Exists(dlg.FileName))
                {
                    System.Windows.Forms.DialogResult dialogResult2 =
                    System.Windows.Forms.MessageBox.Show(
                        "Este archivo ya existe. Desea reemplazarlo?",
                        "Estás seguro?!",
                        System.Windows.Forms.MessageBoxButtons.YesNo);
                    if (dialogResult2 == System.Windows.Forms.DialogResult.No)
                    {
                        return;
                    }
                }

                bool wasOpened = false;
                if (Kinect.Instance.Player.IsOpen)
                {
                    Kinect.Instance.Player.Close();
                    wasOpened = true;
                }

                ZipFile.CreateFromDirectory(Properties.Paths.CurrentSceneDirectory, dlg.FileName);

                if (wasOpened)
                {
                    Kinect.Instance.Player.OpenFile(Properties.Paths.CurrentKdvrFile);
                }

                System.Windows.Forms.DialogResult dialogResult =
                    System.Windows.Forms.MessageBox.Show(
                        "Escena exportada con éxito!",
                        "Exportar escena");
            }
        }

        
    }
}
